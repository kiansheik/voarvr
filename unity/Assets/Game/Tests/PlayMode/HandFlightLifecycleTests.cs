using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.Input;
using VoarVR.UI;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class HandFlightLifecycleTests
    {
        private sealed class TrackedInput : ITrackedFlightInput
        {
            public FlightInputFrame Frame;
            public string Mode => "Hand lifecycle fixture";
            public FlightInputFrame LastDeviceFrame => Frame;
            public FlightInputFrame LastRawFrame => Frame;
            public bool LastWingsEnabled { get; private set; }
            public bool WingsEnabled { get; set; }
            public bool ResetEnabled { get; set; }
            public FlightInputFrame Sample(float dt) { LastWingsEnabled = WingsEnabled; return Frame; }
            public void ResetDerivatives() { }
            public void ResetTrackingOrigin() { }
            public void Dispose() { }
        }

        private BirdFlightDriver driver;
        private TrackedInput input;
        private bool? priorHands;
        private FlightActivity priorActivity;
        private bool priorResume;
        private System.Func<HandTrackingFrame> priorHandProvider;

        [UnitySetUp]
        public IEnumerator LoadHandJourney()
        {
            priorHands = HandInputSettings.EditorOverride;
            priorHandProvider = HandInteraction.EditorProvider;
            priorActivity = ActivitySelection.Chosen;
            priorResume = ActivitySelection.ResumeRequested;
            HandInputSettings.EditorOverride = true;
            ActivitySelection.Chosen = FlightActivity.RouteHome;
            ActivitySelection.ResumeRequested = false;
            CharacterSelection.Chosen = Resources.Load<BirdCharacterDefinition>("Characters/Magpie");
            yield return SceneManager.LoadSceneAsync("BirdFlight");
            yield return null;
            driver = Object.FindAnyObjectByType<BirdFlightDriver>();
            driver.enabled = false;
            Object.FindAnyObjectByType<WorldStreamer>().enabled = false;
            input = new TrackedInput { Frame = ComfortableHands() };
            HandInteraction.EditorProvider = () => new HandTrackingFrame
            {
                HeadPosition = input.Frame.HeadPosition,
                HeadOrientation = input.Frame.HeadOrientation,
                HeadTracked = input.Frame.HeadTracked,
                Left = input.Frame.LeftWing, Right = input.Frame.RightWing
            };
            var gate = new FlightActionGate(input);
            var controller = new BirdFlightController(gate, driver.Controller.State.Position,
                profile: Resources.Load<BirdCharacterDefinition>("Characters/Magpie").BuildProfile(),
                environment: driver.GetComponent<UnityFlightEnvironment>());
            Set("input", input); Set("actionGate", gate); Set("applicationFocused", true);
            typeof(BirdFlightDriver).GetProperty(nameof(BirdFlightDriver.Controller)).SetValue(driver, controller);
            typeof(BirdFlightDriver).GetProperty(nameof(BirdFlightDriver.UsesXR)).SetValue(driver, true);
            driver.Calibration.CompactHands = true;
            driver.GetComponent<CalibrationCoach>().Show(false);
            // The fixture replaced the controller after Start; perch the replacement the
            // same way a fresh hands session does before its coach is confirmed.
            var stage = typeof(BirdFlightDriver).GetNestedType("RecoveryStage", BindingFlags.NonPublic);
            var departure = typeof(BirdFlightDriver).GetMethod("DeparturePerch", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(driver, null);
            typeof(BirdFlightDriver).GetMethod("BeginPerchRecovery", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(driver, new[] { departure, System.Enum.Parse(stage, "Departure") });
        }

        [UnityTearDown]
        public IEnumerator RestoreHandSettings()
        {
            HandInputSettings.EditorOverride = priorHands;
            HandInteraction.EditorProvider = priorHandProvider;
            yield return SceneManager.LoadSceneAsync("CharacterSelect");
            yield return null;
            ActivitySelection.Chosen = priorActivity;
            ActivitySelection.ResumeRequested = priorResume;
        }

        [UnityTest]
        public IEnumerator HandsFreeCountdownCalibratesOnThePerchAndLaunchesAGlide()
        {
            // A fresh hands session calibrates already perched on the departure lookout.
            for (int i = 0; i < 400 && driver.RecoveryPending; i++) driver.Tick(.02f);
            Assert.That(driver.Controller.HasSupportedPerch, Is.True, "The coach is shown on the perch, not the spawn bowl.");
            var world = Object.FindAnyObjectByType<WorldStreamer>();
            var lookout = world.Space.ToLocal(FlightRegions.DepartureLookoutX, 0, FlightRegions.DepartureLookoutZ);
            var perched = driver.Controller.State.Position;
            Assert.That(Vector2.Distance(new Vector2(perched.x, perched.z), new Vector2(lookout.x, lookout.z)), Is.LessThan(1.5f),
                "Departure uses the ridge lookout rather than the enclosed spawn bowl.");
            Assert.That(Mathf.DeltaAngle(driver.Controller.State.Rotation.eulerAngles.y, FlightRegions.DepartureLookoutHeading),
                Is.InRange(-1f, 1f), "The perched bird faces the first lift.");
            yield return null;
            Assert.That(Camera.main.transform.position.y, Is.GreaterThan(driver.transform.position.y),
                "The hand camera must use the same tracked head pose as calibration, keeping the perched view above ground.");
            var eye = world.Space.ToLogical(Camera.main.transform.position);
            var ahead = Quaternion.Euler(0, FlightRegions.DepartureLookoutHeading, 0) * Vector3.forward;
            for (int distance = 10; distance <= 80; distance += 10)
                Assert.That(WorldTerrain.Elevation(world.Space.Seed, eye.X + ahead.x * distance, eye.Z + ahead.z * distance),
                    Is.LessThan(eye.Y), "The perched chase view looks over open ground, " + distance + "m ahead.");
            for (int i = 0; i < 300; i++) driver.Tick(.02f);
            Assert.That(driver.Calibration.Captured, Is.False, "Holding the pose alone never finishes a first calibration.");
            Assert.That(Coach.Flow.Step, Is.EqualTo(HandCalibrationStep.LookLeft), "It waits for the look-around.");
            Calibrate();
            Assert.That(driver.Calibration.Captured, Is.True);
            Assert.That(input.WingsEnabled, Is.True, "The shared tracked-input contract enables hand flight.");
            Assert.That(driver.RecoveryPending, Is.False, "Calibrates in place without re-streaming the world.");
            Assert.That(driver.CalibrationCoachVisible, Is.False);
            Assert.That(driver.Controller.IsPaused, Is.False);
            Assert.That(driver.Controller.State.Phase, Is.Not.EqualTo(FlightPhase.Perched), "The new neutral starts in a glide.");
            Assert.That(Vector3.Dot(driver.Controller.State.Velocity, ahead), Is.GreaterThan(3f));
            Assert.That(driver.Controller.TakeoffCount, Is.Zero, "A launch is not a scored flap takeoff.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnsupportedLookoutFallsBackToSpawnFloorAndStillResumesDeparture()
        {
            var world = Object.FindAnyObjectByType<WorldStreamer>();
            float radius = Resources.Load<BirdCharacterDefinition>("Characters/Magpie").BuildProfile().CollisionRadius;
            float ground = WorldTerrain.Elevation(world.Space.Seed, FlightRegions.DepartureLookoutX, FlightRegions.DepartureLookoutZ);
            var lookout = world.Space.ToLocal(FlightRegions.DepartureLookoutX, 0, FlightRegions.DepartureLookoutZ);
            // An unlandable collider across the recovery sweep stands in for any future
            // obstruction at the authored perch.
            var blocker = new GameObject("Unlandable lookout blocker") { layer = UnityFlightEnvironment.CollisionLayer };
            blocker.transform.position = new Vector3(lookout.x, ground + radius + FlightContactSolver.Skin + .75f, lookout.z);
            blocker.AddComponent<BoxCollider>().size = Vector3.one * 2;
            Physics.SyncTransforms();
            for (int i = 0; i < 800 && driver.RecoveryPending; i++) driver.Tick(.02f);
            Calibrate();
            Assert.That(driver.Calibration.Captured, Is.True);
            // Restarting the route runs the post-calibration departure with its automatic resume.
            driver.RestartAndCalibrate(input.Frame);
            for (int i = 0; i < 800 && driver.RecoveryPending; i++) driver.Tick(.02f);
            Assert.That(driver.RecoveryPending, Is.False);
            Assert.That(driver.Controller.HasSupportedPerch, Is.True);
            var perched = driver.Controller.State.Position;
            Assert.That(new Vector2(perched.x, perched.z).magnitude, Is.LessThan(1.5f), "The clear spawn floor is the last resort.");
            Assert.That(driver.Controller.IsPaused, Is.False,
                "A fallback continues the same departure instead of leaving an unexplained pause with no visible menu.");
            Assert.That(driver.FlightMenuVisible, Is.False);
            Object.Destroy(blocker);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpeningRestDuringDepartureRecoveryKeepsPauseEvenWhenHandsDisappear()
        {
            for (int i = 0; i < 400 && driver.RecoveryPending; i++) driver.Tick(.02f);
            Calibrate();
            var menu = driver.GetComponent<FlightMenu>();
            Invoke("OpenSessionMenu");
            Assert.That(menu.Visible, Is.True);
            driver.ReturnToCheckpoint();
            Assert.That(driver.RecoveryPending, Is.True);
            for (int i = 0; i < 400 && driver.RecoveryPending; i++) driver.Tick(.02f);
            Assert.That(driver.RecoveryPending, Is.False);
            Assert.That(driver.Controller.HasSupportedPerch, Is.True);
            Assert.That(driver.Controller.IsPaused, Is.True, "Completing recovery cannot override an open rest menu.");
            yield return null; yield return null;
            var eye = Camera.main.transform;
            var toCard = menu.Panel.transform.position - eye.position;
            Assert.That(toCard.magnitude, Is.LessThan(3f), "The rest card follows the view to the recovered perch.");
            Assert.That(Vector3.Angle(eye.forward, toCard), Is.LessThan(30f));
            input.Frame.LeftWing.Tracked = input.Frame.RightWing.Tracked = false;
            input.Frame.LeftWing.Source = input.Frame.RightWing.Source = HandPoseSource.Lost;
            input.Frame.LeftWing.MotionEstimated = input.Frame.RightWing.MotionEstimated = true;
            driver.Tick(.02f);
            Assert.That(menu.Visible, Is.True, "The rest card remains readable with arms down out of tracking.");
            Assert.That(driver.Controller.IsPaused, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FocusReturnShowsRestCardAndCalibrationPinchesNeverLeaveFlight()
        {
            for (int i = 0; i < 400 && driver.RecoveryPending; i++) driver.Tick(.02f);
            // Holding both pinches while fitting the pose must not exit to the preflight.
            input.Frame.CharacterSelectPressed = true; driver.Tick(.02f);
            input.Frame.CharacterSelectPressed = false; driver.Tick(.02f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BirdFlight"));
            Assert.That(driver.CalibrationCoachVisible, Is.True);
            Calibrate();
            Assert.That(driver.Controller.IsPaused, Is.False);
            var focus = typeof(BirdFlightDriver).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic);
            focus.Invoke(driver, new object[] { false });
            driver.Tick(.02f);
            Assert.That(driver.FlightMenuVisible, Is.False);
            focus.Invoke(driver, new object[] { true });
            driver.Tick(.02f);
            Assert.That(driver.Controller.IsPaused, Is.True);
            Assert.That(driver.FlightMenuVisible, Is.True, "Hands have no pause button: a paused flight always shows its rest card.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator HandRecenterWaitsForExplicitConfirmationAndKeepsEarnedJourney()
        {
            Assert.That(driver.Calibration.CaptureComfortableGlide(input.Frame), Is.True);
            driver.Controller.Calibrate(input.Frame);
            input.WingsEnabled = true;
            var checkpoint = driver.Expedition.Challenge.Capture();
            checkpoint.Stage = 3; checkpoint.SeedCollected = true;
            Assert.That(driver.Expedition.Challenge.Restore(checkpoint), Is.True);
            driver.NotifyTrackingOriginUpdated();
            var before = driver.Controller.State;
            for (int i = 0; i < 40; i++) driver.Tick(.02f);
            Assert.That(driver.Calibration.Captured, Is.False, "Platform recenter waits for the countdown pose.");
            Assert.That(driver.Controller.IsPaused, Is.True);
            Calibrate();
            Assert.That(driver.Calibration.Captured, Is.True);
            Assert.That(driver.FlightMenuVisible, Is.False, "The countdown resumes flight directly.");
            Assert.That(driver.Controller.IsPaused, Is.False);
            Assert.That(driver.Controller.State.Position, Is.EqualTo(before.Position));
            Assert.That(driver.Expedition.Challenge.Stage, Is.EqualTo(3));
            Assert.That(driver.Expedition.Challenge.SeedCollected, Is.True);
            yield return null;
        }

        private CalibrationCoach Coach => driver.GetComponent<CalibrationCoach>();

        // The real hands-free flow: hold the pose, look left and right, gaze-pinch START
        // (the button callback), then hold the pose through the 3-2-1 countdown.
        private void Calibrate()
        {
            var forward = input.Frame.HeadOrientation;
            for (int i = 0; i < 600 && Coach.Visible && Coach.Flow.Step != HandCalibrationStep.Done; i++)
            {
                var step = Coach.Flow.Step;
                input.Frame.HeadOrientation = step == HandCalibrationStep.LookLeft ? Quaternion.Euler(0, -40, 0) * forward
                    : step == HandCalibrationStep.LookRight ? Quaternion.Euler(0, 40, 0) * forward : forward;
                if (step == HandCalibrationStep.Start)
                {
                    var start = Coach.Panel.transform.Find("Confirm comfortable hand pose").GetComponent<Button>();
                    Assert.That(start.interactable, Is.True);
                    start.onClick.Invoke();
                }
                driver.Tick(.02f);
            }
            input.Frame.HeadOrientation = forward;
            Assert.That(Coach.Flow.Step, Is.EqualTo(HandCalibrationStep.Done), "The countdown completed");
        }

        private void Invoke(string method) => typeof(BirdFlightDriver)
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(driver, null);

        private void Set(string field, object value) => typeof(BirdFlightDriver)
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(driver, value);

        private static FlightInputFrame ComfortableHands()
        {
            var frame = FlightInputFrame.Neutral;
            frame.HeadTracked = true;
            frame.HeadPosition = new Vector3(0, 1.5f, 0);
            frame.LeftWing.Position = new Vector3(-.45f, 1.1f, .3f);
            frame.RightWing.Position = new Vector3(.45f, 1.1f, .3f);
            frame.LeftWing.Source = frame.RightWing.Source = HandPoseSource.DirectHigh;
            return frame;
        }
    }
}
