using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.Input;
using VoarVR.UI;
using Object = UnityEngine.Object;

namespace VoarVR.Editor
{
    // Explicit synthetic tracking review. Root installs storage isolation before
    // Play Mode. This helper never runs on import and never touches a headset.
    public static class HandFlightReview
    {
        public static string Folder = "../artifacts/reviews/hand-flight/round-01";
        public static string Status { get; private set; } = "Idle";
        private static bool running;
        private static Evidence evidence;

        public static string Run()
        {
            if (!Application.isPlaying || !CompetitionPreflightReview.IsolationInstalled)
                return "Install CompetitionPreflightReview isolation before Play Mode, then Run.";
            if (running) return Status;
            Directory.CreateDirectory(Folder);
            evidence = new Evidence { EditorVersion = Application.unityVersion, StartedUtc = DateTime.UtcNow.ToString("o") };
            var host = new GameObject("Explicit hand flight review"); Object.DontDestroyOnLoad(host);
            running = true; Status = "Starting synthetic hands runtime review.";
            host.AddComponent<HandFlightReviewRunner>().StartCoroutine(Guarded(Session(), host));
            return Status;
        }

        private static IEnumerator Guarded(IEnumerator routine, GameObject host)
        {
            var previousHands = HandInputSettings.EditorOverride;
            var previousProvider = HandInteraction.EditorProvider;
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
            HandInputSettings.EditorOverride = true; HandInteraction.EditorProvider = CompactHands;
            try
            {
                while (stack.Count > 0)
                {
                    object next = null; bool moved = false; Exception failure = null;
                    try { moved = stack.Peek().MoveNext(); if (moved) next = stack.Peek().Current; }
                    catch (Exception error) { failure = error; }
                    if (failure != null)
                    {
                        Status = "Failed: " + failure;
                        File.WriteAllText(Path.Combine(Folder, "failure.txt"), Status);
                        Debug.LogException(failure); yield break;
                    }
                    if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (next is IEnumerator nested) stack.Push(nested); else yield return next;
                }
                evidence.CompletedUtc = DateTime.UtcNow.ToString("o"); WriteReport();
                Status = "Finished: " + evidence.Frames.Count + " synthetic hands runtime captures in " + Folder;
                CompetitionPreflightReview.VerifyIsolation();
            }
            finally
            {
                foreach (var pending in stack) (pending as IDisposable)?.Dispose();
                HandInteraction.EditorProvider = previousProvider;
                HandInputSettings.EditorOverride = previousHands;
                running = false; Object.Destroy(host);
            }
        }

        private static HandTrackingFrame CompactHands()
        {
            double timestamp = Time.realtimeSinceStartupAsDouble;
            return new HandTrackingFrame
            {
                HeadPosition = new Vector3(0, 1.6f, 0), HeadOrientation = Quaternion.identity, HeadTracked = true,
                Left = Hand(new Vector3(-.43f, 1.25f, .3f), timestamp),
                Right = Hand(new Vector3(.43f, 1.25f, .3f), timestamp)
            };
        }

        private static WingInput Hand(Vector3 position, double timestamp) => new WingInput
        {
            Position = position, Orientation = Quaternion.identity, Tracked = true,
            Source = HandPoseSource.DirectHigh, SampleTimestamp = timestamp,
            HasUnextrapolatedPose = true, UnextrapolatedPosition = position,
            UnextrapolatedOrientation = Quaternion.identity, UnextrapolatedTimestamp = timestamp
        };

        private static IEnumerator Session()
        {
            CharacterSelection.Chosen = null;
            ActivitySelection.Chosen = FlightActivity.RouteHome; ActivitySelection.ResumeRequested = false;
            yield return SceneManager.LoadSceneAsync("CharacterSelect"); yield return null;
            var selection = Object.FindAnyObjectByType<CharacterSelectController>();
            Require(selection != null && selection.SelectedCharacter.name == "Magpie", "hands default selects Magpie");
            yield return Shot("01-preflight-magpie", "Actual hands preflight and default Magpie; synthetic direct hand samples.");
            Click(selection.PreflightPanel, "COURSES button");
            yield return Shot("02-preflight-courses", "Actual hands course selection, after invoking its real Button callback.");
            Click(selection.PreflightPanel, "A ROUTE HOME button");
            Click(selection.PreflightPanel, "BEGIN JOURNEY button");
            yield return Until(() => SceneManager.GetActiveScene().name == "BirdFlight", "BirdFlight scene", 10);
            yield return null;
            var bird = Object.FindAnyObjectByType<BirdFlightDriver>();
            Require(bird != null && bird.UsesHands, "live driver selected Meta hand adapter");
            var coach = bird.GetComponent<CalibrationCoach>();
            yield return Until(() => Ready(coach), "direct high-confidence warmup and ready compact coach", 10);
            Require(!bird.Calibration.Captured, "comfortable hand pose alone never confirms calibration");
            yield return Shot("03-initial-compact-calibration", "Actual initial coach after adapter warmup. Synthetic compact pose is ready; no automatic confirmation.", () => Ready(coach));
            // Synchronous readback can stall beyond a valid capture interval. Let
            // the live adapter observe that frame and regain readiness normally.
            yield return null;
            yield return Until(() => Ready(coach), "ready pose after screenshot readback", 10);
            Click(coach.Panel, "Confirm comfortable hand pose");
            yield return Until(() => bird.Calibration.Captured && !bird.RecoveryPending
                && bird.Controller.HasSupportedPerch, "native departure perch recovery", 20);
            Require(bird.Controller.TakeoffCount == 0, "stationary hands did not take off");
            yield return Shot("04-supported-departure", "Actual terrain-supported ridge-lookout departure after the real READY Button callback; stationary synthetic hands, zero takeoffs.");
            Invoke(bird, "OpenSessionMenu");
            var menu = bird.GetComponent<FlightMenu>(); Require(menu.Visible, "rest menu opens");
            yield return Shot("05-rest-menu", "Actual rest menu opened through the driver's normal session-menu method; this capture does not test recognition of the two-hand gesture.");
            Click(menu.Panel, "FlightSettings");
            yield return Shot("06-flight-settings", "Actual second settings page with live view, controls, wind and instrument labels; real Button callback.");
            Click(menu.Panel, "BackToRest"); Click(menu.Panel, "RecalibrateHere");
            yield return Until(() => Ready(coach), "redo calibration readiness", 10);
            yield return Shot("07-recalibration", "Actual in-place hands recalibration coach; previous supported flight is preserved.", () => Ready(coach));
            yield return null;
            yield return Until(() => Ready(coach), "redo readiness after screenshot readback", 10);
            Click(coach.Panel, "Confirm comfortable hand pose");
            yield return Until(() => menu.Visible && !coach.Visible, "return to rest after calibration", 10);
            Click(menu.Panel, "FinishSession");
            Require(menu.ResultsVisible, "session saved and results shown");
            yield return Shot("08-session-results", "Actual isolated session finalization and results return target; synthetic stationary tracking is not exercise or completion evidence.");
        }

        private static bool Ready(CalibrationCoach coach) => coach != null && coach.Visible
            && coach.Panel.GetComponentsInChildren<Button>().Any(b => b.IsInteractable());

        private static IEnumerator Until(Func<bool> predicate, string label, double timeout)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeout;
            while (!predicate())
            {
                Require(Time.realtimeSinceStartupAsDouble < deadline, "timeout waiting for " + label);
                yield return null;
            }
        }

        private static void Click(Canvas panel, string name)
        {
            var target = panel.transform.Find(name);
            var button = target != null ? target.GetComponent<Button>() : null;
            Require(panel.enabled && button != null && button.isActiveAndEnabled && button.IsInteractable(), "available Button " + name);
            button.onClick.Invoke();
        }

        private static void Invoke(BirdFlightDriver bird, string name) => typeof(BirdFlightDriver)
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(bird, null);

        private static IEnumerator Shot(string name, string kind, Func<bool> ready = null)
        {
            Status = "Capturing " + name;
            yield return new WaitForSecondsRealtime(.25f); yield return null;
            if (ready != null) yield return Until(ready, "stable pose for " + name, 10);
            DuckReview.CaptureCurrent(Folder, name);
            var bird = Object.FindAnyObjectByType<BirdFlightDriver>();
            var eye = Camera.main;
            evidence.Frames.Add(new Frame
            {
                File = name + ".png", Scene = SceneManager.GetActiveScene().path, EvidenceKind = kind,
                UsesHands = bird != null && bird.UsesHands, Calibrated = bird != null && bird.Calibration.Captured,
                Supported = bird != null && bird.Controller.HasSupportedPerch,
                Phase = bird != null ? bird.Controller.State.Phase.ToString() : "Menu",
                Takeoffs = bird != null ? bird.Controller.TakeoffCount : 0,
                BirdPosition = bird != null ? bird.Controller.State.Position : Vector3.zero,
                BirdYaw = bird != null ? bird.Controller.State.Rotation.eulerAngles.y : 0,
                CameraPosition = eye.transform.position, CameraEuler = eye.transform.eulerAngles,
                Text = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled
                    && t.GetComponentInParent<Canvas>()?.enabled == true).Select(t => t.text).ToArray()
            });
            WriteReport();
        }

        private static void Require(bool valid, string label)
        { if (!valid) throw new InvalidOperationException("Hand review failed: " + label); }
        private static void WriteReport()
        {
            File.WriteAllText(Path.Combine(Folder, "runtime-fixtures.json"), JsonUtility.ToJson(evidence, true));
            File.WriteAllText(Path.Combine(Folder, "evidence-limits.txt"), evidence.EvidenceKind + "\n" + evidence.Limits);
        }

        [Serializable] private sealed class Evidence
        {
            public string EvidenceKind = "SYNTHETIC DIRECT HAND TRACKING; STAGED UI BUTTON CALLBACKS. Actual runtime hand adapter, calibration, native supported recovery and isolated persistence remain live.";
            public string Limits = "Desktop mono screenshots. Does not prove Quest tracking, physical hand or gaze recognition, stereo readability, wearer comfort, flight/course completion or Quest performance. No headset install, launch or recording occurs.";
            public string EditorVersion, StartedUtc, CompletedUtc;
            public List<Frame> Frames = new List<Frame>();
        }
        [Serializable] private sealed class Frame
        {
            public string File, Scene, EvidenceKind, Phase;
            public bool UsesHands, Calibrated, Supported;
            public int Takeoffs;
            public float BirdYaw;
            public Vector3 BirdPosition, CameraPosition, CameraEuler;
            public string[] Text;
        }
    }

    public sealed class HandFlightReviewRunner : MonoBehaviour { }
}
