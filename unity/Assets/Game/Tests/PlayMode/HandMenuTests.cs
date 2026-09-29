using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.Input;
using VoarVR.UI;

namespace VoarVR.Tests
{
    public sealed class HandMenuTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private bool? previousHands;
        private BirdCharacterDefinition previousCharacter;
        private FlightActivity previousActivity;
        private string previousCourse;

        [SetUp]
        public void SetUp()
        {
            previousHands = HandInputSettings.EditorOverride;
            HandInputSettings.EditorOverride = true;
            previousCharacter = CharacterSelection.Chosen;
            previousActivity = ActivitySelection.Chosen;
            previousCourse = ActivitySelection.ChosenCourseId;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            HandInputSettings.EditorOverride = previousHands;
            CharacterSelection.Chosen = previousCharacter;
            ActivitySelection.Chosen = previousActivity;
            ActivitySelection.ChosenCourseId = previousCourse;
            foreach (var obj in objects) if (obj != null) Object.Destroy(obj);
            objects.Clear(); yield return null; yield return null;
        }

        private GameObject Own(string name)
        { var obj = new GameObject(name); objects.Add(obj); return obj; }
        private static Ray LookAt(Camera camera, Transform target) =>
            new Ray(camera.transform.position, target.position - camera.transform.position);

        [Test]
        public void PinchCannotActivateOnArrivalRecoveryOrTargetChange()
        {
            var gate = new HandPinchGate();
            Assert.That(gate.Sample(true, true, 1), Is.False, "Already-held pinch on opening");
            gate.Sample(true, false, 1);
            Assert.That(gate.Sample(true, true, 1), Is.True);
            Assert.That(gate.Sample(true, true, 1), Is.False, "Holding never repeats");
            gate.Sample(true, false, 1);
            Assert.That(gate.Sample(true, true, 2), Is.False, "Gaze changed after release");
            gate.Sample(true, false, 2); gate.Sample(false, false, 0);
            Assert.That(gate.Sample(true, true, 2), Is.False, "Tracking recovery must release first");
            gate.Sample(true, false, 2); gate.RequireRelease();
            Assert.That(gate.Sample(true, true, 2), Is.False, "New modal starts disarmed");
        }

        [UnityTest]
        public IEnumerator HandPreflightDefaultsToMagpieAndGazeSelectsEveryCourse()
        {
            CharacterSelection.Chosen = null; ActivitySelection.Chosen = FlightActivity.RouteHome;
            yield return SceneManager.LoadSceneAsync("CharacterSelect"); yield return null;
            var selection = Object.FindAnyObjectByType<CharacterSelectController>();
            objects.Add(selection.gameObject);
            Assert.That(selection.SelectedCharacter.name, Is.EqualTo("Magpie"));
            var pointer = selection.PreflightPanel.GetComponent<HandGazePointer>();
            Assert.That(pointer, Is.Not.Null);
            var camera = selection.PreflightPanel.worldCamera;
            var courses = selection.PreflightPanel.transform.Find("COURSES button");
            var gaze = LookAt(camera, courses);
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
            Assert.That(ActivitySelection.Chosen, Is.EqualTo(FlightActivity.ObstacleCourse));
            string[] ids = { "moth-line", "canopy-weave", "ruin-windows", "thermal-ladder", "trick-and-perch" };
            var courseButtons = selection.PreflightPanel.transform.Find("Journey preview").GetComponentsInChildren<Button>();
            Assert.That(courseButtons.Length, Is.EqualTo(ids.Length));
            for (int i = 0; i < ids.Length; i++)
            {
                gaze = LookAt(camera, courseButtons[i].transform);
                pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
                Assert.That(ActivitySelection.ChosenCourseId, Is.EqualTo(ids[i]));
            }
            var labels = string.Join("\n", selection.PreflightPanel.GetComponentsInChildren<Text>().Select(t => t.text));
            Assert.That(labels, Does.Contain("PINCH THUMB + INDEX"));
            Assert.That(labels, Does.Not.Contain("RIGHT TRIGGER").And.Not.Contain("press A"));
        }

        [UnityTest]
        public IEnumerator GazeActivatesRealButtonsAndRejectsHiddenOrDisabledTargets()
        {
            var camera = Own("Hand test camera").AddComponent<Camera>();
            var root = Own("Hand test panel"); root.AddComponent<RectTransform>();
            var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(800, 500);
            rect.position = new Vector3(0, 0, 2); rect.localScale = Vector3.one * .001f;
            var target = new GameObject("Target", typeof(RectTransform), typeof(Image), typeof(Button));
            target.transform.SetParent(root.transform, false);
            ((RectTransform)target.transform).sizeDelta = new Vector2(300, 100);
            var button = target.GetComponent<Button>(); int calls = 0;
            button.onClick.AddListener(() => calls++);
            var pointer = root.AddComponent<HandGazePointer>(); pointer.Configure(canvas, camera);
            var gaze = LookAt(camera, target.transform);
            pointer.ProcessSample(gaze, true, true); Assert.That(calls, Is.Zero);
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
            Assert.That(calls, Is.EqualTo(1)); Assert.That(pointer.Hovered, Is.SameAs(button));
            button.interactable = false;
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
            Assert.That(calls, Is.EqualTo(1));
            button.interactable = true; pointer.ProcessSample(gaze, true, false);
            canvas.enabled = false; pointer.ProcessSample(gaze, true, false);
            canvas.enabled = true; pointer.ProcessSample(gaze, true, true);
            Assert.That(calls, Is.EqualTo(1), "Reopening cannot activate a held pinch");
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, false, false);
            pointer.ProcessSample(gaze, true, true); Assert.That(calls, Is.EqualTo(1));
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
            Assert.That(calls, Is.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HandPauseSettingsAndResultsReturnNeedSeparatePinches()
        {
            var camera = Own("Hand menu eye").AddComponent<Camera>();
            var menu = Own("Hand menu owner").AddComponent<FlightMenu>();
            var actions = new List<FlightMenuAction>(); menu.Configure(camera, actions.Add, () => "Resting");
            menu.Open(); var pointer = menu.Panel.GetComponent<HandGazePointer>();
            Assert.That(pointer, Is.Not.Null);
            Canvas.ForceUpdateCanvases();
            foreach (var label in menu.Panel.GetComponentsInChildren<Text>())
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    label.name + " must fit without truncating hands instructions");
            var view = menu.Panel.transform.Find("ToggleView"); Assert.That(view.GetComponent<Button>(), Is.Not.Null);
            Assert.That(view.gameObject.activeSelf, Is.False, "Settings start on a separate page");
            var settings = menu.Panel.transform.Find("FlightSettings");
            var gaze = LookAt(camera, settings);
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
            Assert.That(actions, Is.Empty, "Opening a page does not dispatch a flight action");
            Assert.That(view.gameObject.activeSelf, Is.True);
            Assert.That(settings.gameObject.activeSelf, Is.False);
            Assert.That(menu.Panel.GetComponentsInChildren<Button>().Length, Is.LessThanOrEqualTo(8),
                "Settings remain a readable separate page");
            gaze = LookAt(camera, view);
            pointer.ProcessSample(gaze, true, true); Assert.That(actions, Is.Empty, "Page opening cannot click through");
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
            Assert.That(actions.Single(), Is.EqualTo(FlightMenuAction.ToggleView));
            foreach (var action in new[] { FlightMenuAction.ToggleHud, FlightMenuAction.ToggleWind, FlightMenuAction.ToggleControlMode })
            {
                gaze = LookAt(camera, menu.Panel.transform.Find(action.ToString()));
                pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
                Assert.That(actions.Last(), Is.EqualTo(action));
            }
            gaze = LookAt(camera, menu.Panel.transform.Find("BackToRest"));
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
            Assert.That(actions.Count, Is.EqualTo(4)); Assert.That(settings.gameObject.activeSelf, Is.True);
            Assert.That(view.gameObject.activeSelf, Is.False);
            Assert.That(menu.Panel.transform.Find("ReturnToSelection").GetComponent<Button>(), Is.Not.Null);
            menu.ShowResults("SAVED");
            Assert.That(view.gameObject.activeSelf, Is.False, "Normal rows cannot click through results");
            var returnButton = menu.Panel.transform.Find("Return to player select");
            gaze = LookAt(camera, returnButton);
            pointer.ProcessSample(gaze, true, true); Assert.That(actions.Count, Is.EqualTo(4));
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
            Assert.That(actions.Last(), Is.EqualTo(FlightMenuAction.ReturnAfterResults));
            menu.Open(); Assert.That(settings.gameObject.activeSelf, Is.True);
            Assert.That(returnButton.gameObject.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RestCalibrationAndResultCardsDrawOverNearbyTerrain()
        {
            foreach (bool hands in new[] { false, true })
            {
                HandInputSettings.EditorOverride = hands;
                var camera = Own("Card eye " + hands).AddComponent<Camera>();
                var menu = Own("Card menu " + hands).AddComponent<FlightMenu>();
                menu.Configure(camera, _ => { }, () => "Resting");
                var coach = Own("Card coach " + hands).AddComponent<CalibrationCoach>();
                coach.Configure(camera);
                menu.Open(); coach.Show(false);
                yield return null;
                menu.ShowResults("SAVED");
                foreach (var card in new[] { menu.Panel, coach.Panel })
                {
                    Assert.That(card.sortingOrder, Is.EqualTo(WorldCardRendering.SortingOrder));
                    foreach (var graphic in card.GetComponentsInChildren<Graphic>(true))
                        Assert.That(WorldCardRendering.DrawsOverWorld(graphic), Is.True,
                            card.name + "/" + graphic.name + " must stay readable when a perch slope is inside reading distance");
                }
            }
        }

        [UnityTest]
        public IEnumerator CoachLearnsContextThenCountsDownWithoutAnyPoseBreakingPinch()
        {
            var camera = Own("Hand coach eye").AddComponent<Camera>();
            var coach = Own("Hand coach owner").AddComponent<CalibrationCoach>();
            coach.Configure(camera); coach.Show(false); int confirmed = 0;
            coach.ConfirmRequested += () => confirmed++;
            camera.transform.position = new Vector3(0, 4, 0);
            yield return null;
            var labels = string.Join("\n", coach.Panel.GetComponentsInChildren<Text>().Select(t => t.text));
            Assert.That(labels, Does.Contain("gliding pose"));
            Assert.That(labels, Does.Not.Contain("controllers").And.Not.Contain("Press A"));
            Assert.That(coach.Panel.transform.parent, Is.Null);
            Assert.That(Vector3.Distance(coach.Panel.transform.position, camera.transform.TransformPoint(CalibrationCoach.HandCardOffset)),
                Is.LessThan(.001f), "The card sits above the bird and follows the head during the pose steps");
            var start = coach.Panel.transform.Find("Confirm comfortable hand pose").GetComponent<Button>();
            Assert.That(start.gameObject.activeSelf, Is.False, "No button to aim at while holding the pose");
            var frame = FlightInputFrame.Neutral; frame.HeadTracked = true; frame.HeadPosition = new Vector3(0, 1.5f, 0);
            frame.LeftWing = new WingInput { Tracked = true, Source = HandPoseSource.DirectHigh, Orientation = Quaternion.identity, Position = new Vector3(-.64f, 1.35f, 0) };
            frame.RightWing = new WingInput { Tracked = true, Source = HandPoseSource.DirectHigh, Orientation = Quaternion.identity, Position = new Vector3(.64f, 1.35f, 0) };
            for (int i = 0; i < 60; i++) coach.UpdateHands(frame, frame, .02f);
            Assert.That(coach.Flow.Step, Is.EqualTo(HandCalibrationStep.LookLeft));
            var look = frame; look.HeadOrientation = Quaternion.Euler(0, -40, 0);
            for (int i = 0; i < 30; i++) coach.UpdateHands(look, look, .02f);
            look.HeadOrientation = Quaternion.Euler(0, 40, 0);
            for (int i = 0; i < 30; i++) coach.UpdateHands(look, look, .02f);
            Assert.That(coach.Flow.Step, Is.EqualTo(HandCalibrationStep.Start));
            Assert.That(start.gameObject.activeSelf && start.interactable, Is.True);
            yield return null;
            var pointer = coach.Panel.GetComponent<HandGazePointer>(); var gaze = LookAt(camera, start.transform);
            pointer.ProcessSample(gaze, true, true); Assert.That(coach.Flow.Step, Is.EqualTo(HandCalibrationStep.Start), "A held pinch cannot press START");
            pointer.ProcessSample(gaze, true, false); pointer.ProcessSample(gaze, true, true);
            Assert.That(coach.Flow.Step, Is.EqualTo(HandCalibrationStep.Return));
            Assert.That(confirmed, Is.Zero, "START never captures the pose itself");
            for (int i = 0; i < 170; i++) coach.UpdateHands(frame, frame, .02f);
            Assert.That(confirmed, Is.EqualTo(1), "The countdown completes hands-free, once");
            coach.Hide(); coach.Show(true);
            Assert.That(coach.Flow.Step, Is.EqualTo(HandCalibrationStep.Return), "A redo reuses the learned context");
            Assert.That(start.gameObject.activeSelf, Is.False);
            yield return null;
        }
    }
}
