using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.Input;
using VoarVR.Telemetry;
using VoarVR.UI;
using VoarVR.World;
using Object = UnityEngine.Object;

namespace VoarVR.Editor
{
    // Explicit review only. InstallIsolation in idle Edit Mode, enter Play Mode without
    // domain reload, Run, leave Play Mode, then RestoreIsolation. Never runs on import.
    public static class CompetitionPreflightReview
    {
        public static string Folder = "../artifacts/reviews/competition-preflight/round-01";
        public static string Status { get; private set; } = "Idle";
        public static bool IsolationInstalled => scope != null;
        private static IsolationScope scope;
        private static bool running;
        private static Evidence report;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        public static string InstallIsolation()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Install isolation before entering Play Mode.");
            if (scope != null) return "Isolation already installed: " + scope.Directory;
            if (!EditorSettings.enterPlayModeOptionsEnabled
                || (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0)
                throw new InvalidOperationException("This explicit review requires Play Mode with domain reload disabled so installed storage overrides survive entry.");
            Directory.CreateDirectory(Folder);
            scope = new IsolationScope(Path.GetFullPath(Path.Combine(Folder, "isolated-storage-" + Guid.NewGuid().ToString("N"))));
            return Status = "Isolation installed: " + scope.Directory;
        }

        public static string VerifyIsolation()
        {
            if (scope == null) return "No isolation scope is installed.";
            string result = scope.Verify();
            File.WriteAllText(Path.Combine(Folder, "storage-isolation.txt"), result);
            return result;
        }

        public static string RestoreIsolation()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Leave Play Mode before restoring storage factories.");
            if (scope == null) return "No isolation scope is installed.";
            string result;
            try { result = VerifyIsolation(); }
            finally { scope.Dispose(); scope = null; running = false; }
            return Status = result + "\nPrevious factories, selections and UI preferences restored; isolated evidence retained.";
        }

        public static string Run()
        {
            if (!Application.isPlaying || scope == null)
                return "InstallIsolation in Edit Mode, then enter Play Mode before Run.";
            if (running) return Status;
            Directory.CreateDirectory(Folder);
            report = new Evidence { EditorVersion = Application.unityVersion, StartedUtc = DateTime.UtcNow.ToString("o") };
            running = true;
            var host = new GameObject("Explicit competition preflight review");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CompetitionPreflightReviewRunner>().StartCoroutine(Guarded(Session(), host));
            return Status = "Capturing staged runtime lifecycle fixtures; no physical course completion claim.";
        }

        private static IEnumerator Guarded(IEnumerator routine, GameObject host)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
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
                    if (next is IEnumerator nested) stack.Push(nested);
                    else yield return next;
                }
                report.CompletedUtc = DateTime.UtcNow.ToString("o"); WriteReport();
                Status = "Finished: " + report.Frames.Count + " labeled runtime fixtures in " + Folder;
                VerifyIsolation();
            }
            finally
            {
                foreach (var pending in stack) (pending as IDisposable)?.Dispose();
                running = false; Object.Destroy(host);
            }
        }

        private static IEnumerator Session()
        {
            ActivitySelection.Chosen = FlightActivity.RouteHome; ActivitySelection.ResumeRequested = false;
            CharacterSelection.Chosen = Character("Magpie");
            yield return SceneManager.LoadSceneAsync("CharacterSelect"); yield return null; yield return null;
            yield return Shot("selection-route-home-magpie", "Actual CharacterSelect UI; review stores contain no prior progress.");
            var selection = Object.FindAnyObjectByType<CharacterSelectController>();
            selection.ChooseActivity(FlightActivity.ObstacleCourse);
            foreach (var definition in CourseCatalog.All)
            {
                Invoke(selection, "ChooseCourse", definition.StableId);
                yield return Shot("selection-" + definition.StableId, "Actual course selection UI.");
            }
            foreach (var definition in CourseCatalog.All) yield return Course(definition.StableId);
            yield return FreeFlight();
            yield return ObjectiveFeedbackFixtures();
            yield return SceneManager.LoadSceneAsync("CharacterSelect"); yield return null; yield return null;
            yield return Shot("selection-after-session", "Actual profile history after isolated fixture session finalization.");
        }

        private static IEnumerator LoadFlight(FlightActivity activity, string courseId, string species)
        {
            ActivitySelection.Chosen = activity; ActivitySelection.ChosenCourseId = courseId;
            ActivitySelection.ResumeRequested = false; CharacterSelection.Chosen = Character(species);
            yield return SceneManager.LoadSceneAsync("BirdFlight"); yield return null; yield return null;
            var bird = Object.FindAnyObjectByType<BirdFlightDriver>();
            if (bird == null || bird.Controller == null) throw new InvalidOperationException("BirdFlight did not initialize.");
            bird.enabled = false;
            var camera = Object.FindAnyObjectByType<VoarVR.Core.FlightCamera>();
            if (camera != null) camera.enabled = false;
            WarmWorld(bird.Controller.State.Position);
            PlaceCamera(bird, bird.Controller.State.Position + Vector3.forward * 20);
        }

        private static IEnumerator Course(string id)
        {
            yield return LoadFlight(FlightActivity.ObstacleCourse, id, "Magpie");
            var bird = Object.FindAnyObjectByType<BirdFlightDriver>();
            var director = bird.ObstacleCourse;
            var clock = new ReviewClock { NowSeconds = 100 };
            Set(director, "clock", clock); director.ResetForFreshAttempt();
            director.Tick(.02f, FlightInputFrame.Neutral);
            for (int count = 3; count >= 1; count--)
            {
                Require(director.Runtime.CountdownNumber == count, id + " countdown " + count);
                yield return Shot(id + "-countdown-" + count, "Actual countdown UI; injected monotonic clock.");
                clock.NowSeconds += 1; director.Tick(.02f, FlightInputFrame.Neutral);
            }
            Require(director.Runtime.State == CourseState.Running, id + " GO");
            director.Runtime.MarkNonRanked(CourseNonRankedReason.ExplicitPractice);
            for (int index = 0; index < director.Definition.Tasks.Count; index++)
            {
                Require(director.Runtime.TaskIndex == index, id + " ordered task " + index);
                var task = director.Definition.Tasks[index];
                Vector3 target = Target(task, bird);
                // Quotas begin shortly after the preceding gate. Stage the bird beyond
                // that gate so the observer is not inside its frame, then use the same
                // chase pose and collision constraint as normal runtime play.
                float approach = task.Kind == CourseTaskKind.CollectibleQuota ? 6f : 12f;
                StageBird(bird, target - Vector3.forward * approach + Vector3.up * 2, target);
                Require(!bird.UsesXR, "desktop camera fixture has no live tracking input");
                var flightCamera = Object.FindAnyObjectByType<VoarVR.Core.FlightCamera>();
                Require(flightCamera != null, "runtime chase camera exists");
                bird.SetViewMode(FlightViewMode.ThirdPerson); Invoke(flightCamera, "ApplyPose");
                Invoke(director, "UpdateForagingObjective"); Invoke(director, "UpdatePresentation");
                yield return Shot(id + "-task-" + index + "-" + task.StableId,
                    "Staged task approach; actual FlightCamera.ApplyPose chase/collision path and runtime cues. Quota pose is beyond the prior gate. This is a stationary fixture, not physical course completion evidence.");
                clock.NowSeconds += 1; director.Runtime.Tick();
                CompleteTask(director, bird, task);
                Require(director.Runtime.CompletedTasks == index + 1, id + " task completion " + task.StableId);
            }
            Require(director.Runtime.State == CourseState.Finished, id + " fixture finished");
            director.Tick(.02f, FlightInputFrame.Neutral);
            Require(director.Runtime.State == CourseState.Results, id + " results");
            yield return Shot(id + "-results", "Actual practice results after injected task observations; no physical completion or ranking claim.");
            var neutral = FlightInputFrame.Neutral;
            director.HandleInput(neutral); var press = neutral; press.Tuck = 1;
            director.HandleInput(press);
            Require(director.Runtime.State == CourseState.Rest, id + " rest");
            director.Tick(.02f, neutral);
            yield return Shot(id + "-rest", "Actual released-trigger results action and enforced rest UI; review clock starts at zero rest elapsed.");
            clock.NowSeconds += ObstacleCourseDirector.RequiredRestSeconds - .1;
            director.Tick(.02f, neutral);
            Require(director.Runtime.State == CourseState.Rest, id + " rest cannot finish early");
            clock.NowSeconds += .2; director.Tick(.02f, neutral);
            Require(director.Runtime.State == CourseState.Ready, id + " retry ready");
            yield return Shot(id + "-retry-ready", "Actual retry-ready UI after six seconds on the injected clock.");
            director.HandleInput(neutral); director.HandleInput(press); director.Tick(.02f, neutral);
            Require(director.Runtime.State == CourseState.Countdown3, id + " retry countdown");
            yield return Shot(id + "-retry-countdown", "Actual released-trigger retry action.");
        }

        private static void CompleteTask(ObstacleCourseDirector director, BirdFlightDriver bird, CourseTaskDefinition task)
        {
            var runtime = director.Runtime; var a = task.PointA.Logical; var b = task.PointB.Logical;
            switch (task.Kind)
            {
                case CourseTaskKind.Gate:
                    runtime.Observe(new CourseObservation(new LogicalPosition(a.X, a.Y, a.Z - 2)));
                    runtime.Observe(new CourseObservation(new LogicalPosition(a.X, a.Y, a.Z + 2))); break;
                case CourseTaskKind.Corridor:
                    runtime.Observe(new CourseObservation(a)); runtime.Observe(new CourseObservation(b)); break;
                case CourseTaskKind.AltitudeBand:
                    runtime.Observe(new CourseObservation(new LogicalPosition(a.X, (task.MinimumAltitude + task.MaximumAltitude) * .5, a.Z))); break;
                case CourseTaskKind.Trick:
                    runtime.Observe(new CourseObservation(a, detectedTrick: task.RequiredTrick)); break;
                case CourseTaskKind.SupportedLanding:
                    runtime.Observe(new CourseObservation(a, supportedLanding: true, supportedSurfaceId: task.RequiredSurfaceId)); break;
                case CourseTaskKind.CollectibleQuota:
                    var forage = bird.GetComponent<SkyForaging>(); var catches = new List<CatchResult>();
                    for (int i = 0; i < task.RequiredCount; i++)
                    {
                        var caught = forage.Score.Catch(CollectibleCatalog.Find(task.CollectibleId), 10 + i,
                            CourseObjectiveIdentity.For(director.Definition, task));
                        catches.Add(caught); Invoke(bird, "OnCollectibleCaught", caught);
                    }
                    runtime.Observe(new CourseObservation(b, catchesSincePreviousObservation: catches)); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private static IEnumerator FreeFlight()
        {
            yield return LoadFlight(FlightActivity.FreeFlight, "moth-line", "Magpie");
            var bird = Object.FindAnyObjectByType<BirdFlightDriver>();
            var coach = bird.GetComponent<CalibrationCoach>();
            coach.Show(false); coach.UpdateReadiness(FlightInputFrame.Neutral, bird.Calibration);
            yield return Shot("calibration-first", "Actual controller calibration coach with staged readiness sample; no hand adapter claim.");
            coach.Show(true);
            yield return Shot("calibration-redo", "Actual recalibration coach view."); coach.Hide();
            Invoke(bird, "OpenSessionMenu");
            yield return Shot("session-help-menu", "Actual paused session menu, selected Magpie Free Flight.");
            Invoke(bird, "HandleMenuAction", FlightMenuAction.Resume);
            var forage = bird.GetComponent<SkyForaging>();
            foreach (var definition in CollectibleCatalog.All)
                Invoke(bird, "OnCollectibleCaught", forage.Score.Catch(definition, 10));
            StageBird(bird, new Vector3(0, 80, 0), new Vector3(0, 80, 40));
            bird.GetComponent<SkyRivals>()?.Tick(.02f);
            yield return Shot("typed-collectible-ribbon-and-rivals", "Typed catch totals are injected score events; rivals are actual bounded runtime objects.");
            var rivals = Object.FindObjectsByType<SphereCollider>(FindObjectsSortMode.None)
                .Where(c => c.name.StartsWith("Wind rival", StringComparison.Ordinal)).ToArray();
            if (rivals.Length > 0)
            {
                var center = rivals[0].transform.position;
                Camera.main.transform.SetPositionAndRotation(center + new Vector3(2, 1, -4),
                    Quaternion.LookRotation(center - (center + new Vector3(2, 1, -4)), Vector3.up));
                yield return Shot("rival-closeup", "Staged observer camera at an actual runtime rival; does not establish collision feel.");
                PlaceCamera(bird, bird.Controller.State.Position + Vector3.forward * 20);
            }
            Invoke(bird, "FinishSession");
            Require(bird.GetComponent<FlightMenu>().ResultsVisible, "session finalization");
            yield return Shot("session-results", "Actual finalization, file persistence and two-column results for injected catch events; no exercise measurement claim.");
        }

        private static IEnumerator Shot(string name, string kind)
        {
            Status = "Capturing " + name;
            var bird = Object.FindAnyObjectByType<BirdFlightDriver>();
            var ribbon = bird != null ? bird.GetComponent<GameplayRibbon>() : null;
            if (ribbon != null) { Set(ribbon, "nextUpdate", 0f); ribbon.TickRibbon(); }
            // FlightCard sizes its backing at a .2-second cadence. Capture after that
            // presentation interval so a newly longer label has its current backing.
            yield return new WaitForSecondsRealtime(.25f); yield return null;
            DuckReview.CaptureCurrent(Folder, name);
            var course = bird?.ObstacleCourse;
            var challenge = bird?.Expedition?.Challenge;
            report.Frames.Add(new Frame
            {
                File = name + ".png", Scene = SceneManager.GetActiveScene().path, EvidenceKind = kind,
                Course = course?.Definition.StableId, State = course?.Runtime.State.ToString(),
                TaskIndex = course?.Runtime.TaskIndex ?? -1, Seconds = course?.Runtime.ElapsedSeconds ?? 0,
                NonRankedReasons = course?.Runtime.NonRankedReasons.ToString(),
                BirdPosition = bird != null ? bird.Controller.State.Position : Vector3.zero,
                CameraPosition = Camera.main.transform.position, CameraEuler = Camera.main.transform.eulerAngles,
                CameraFieldOfView = Camera.main.fieldOfView, CameraNearClip = Camera.main.nearClipPlane,
                CameraFarClip = Camera.main.farClipPlane,
                Paused = bird != null && bird.Controller.IsPaused, RibbonVisible = ribbon != null && ribbon.Visible,
                HudVisible = bird != null && bird.GetComponent<FlightHud>()?.Visible == true,
                Activity = challenge?.Activity.ToString(), ChallengeState = challenge?.Status.ToString(),
                ChallengeKind = challenge?.Kind.ToString(), Stage = challenge?.Stage ?? -1,
                SoaringGain = challenge?.SoaringGain ?? 0, RequiredGain = challenge?.RequiredGain ?? 0,
                HighestAltitude = challenge?.HighestAltitude ?? 0, RequiredAltitude = challenge?.RequiredAltitude ?? 0,
                Score = challenge?.Score ?? 0, Medal = challenge?.Medal ?? 0,
                GardenRestored = bird?.Expedition?.Journey?.GardenRestored == true,
                Text = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled
                    && t.GetComponentInParent<Canvas>()?.enabled == true).Select(t => t.text)
                    .Concat(Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Where(t => t.gameObject.activeInHierarchy).Select(t => t.text)).ToArray()
            });
            WriteReport();
        }

        private static IEnumerator ObjectiveFeedbackFixtures()
        {
            yield return LoadFlight(FlightActivity.RouteHome, "moth-line", "Magpie");
            var bird = Object.FindAnyObjectByType<BirdFlightDriver>();
            bird.GetComponent<FlightHud>().SetVisible(false);
            var challenge = bird.Expedition.Challenge;
            var checkpoint = challenge.Capture(); checkpoint.Stage = 1;
            checkpoint.SoaringGain = 100; checkpoint.HighestAltitude = 180;
            Require(challenge.Restore(checkpoint), "staged soaring checkpoint");
            var space = Object.FindAnyObjectByType<WorldStreamer>().Space;
            var thermal = FlightRegions.DepartureThermal(100);
            StageBird(bird, space.ToLocal(thermal.X, 180, thermal.Z - 25), space.ToLocal(thermal.X, 230, thermal.Z));
            bird.GetComponent<JourneyPresentation>().Tick(bird.Controller.State.Position);
            yield return Shot("objective-soar-gain-met-altitude-missing",
                "Staged Route Home Soar checkpoint: 100/100 m climb, 180/230 m peak altitude. Actual runtime ribbon with instruments hidden; no flight progression claim.");
            checkpoint.SoaringGain = 99.9f; checkpoint.HighestAltitude = 229.9;
            Require(challenge.Restore(checkpoint), "staged almost-complete soaring checkpoint");
            yield return Shot("objective-soar-thresholds-unmet",
                "Staged 99.9/100 m climb and 229.9/230 m peak altitude; actual ribbon must show both unmet thresholds without rounding up.");

            yield return LoadFlight(FlightActivity.Training, "moth-line", "Magpie");
            bird = Object.FindAnyObjectByType<BirdFlightDriver>(); bird.GetComponent<FlightHud>().SetVisible(false);
            challenge = bird.Expedition.Challenge; var target = challenge.Target(0);
            challenge.Step(new ChallengeObservation(new LogicalPosition(target.X, 10, target.Z), Vector3.forward, 3, 0, 1, false, 0, 0));
            challenge.Step(new ChallengeObservation(new LogicalPosition(target.X, 61, target.Z), Vector3.forward, 3, 0, 1, false, 0, 0));
            Require(challenge.Status == ChallengeStatus.Completed && challenge.Medal == 3, "staged Training gold result");
            Require(bird.Expedition.SaveCheckpoint(), "isolated Training result save");
            space = Object.FindAnyObjectByType<WorldStreamer>().Space;
            StageBird(bird, space.ToLocal(target.X, 61, target.Z), space.ToLocal(target.X, 61, target.Z + 30));
            bird.GetComponent<JourneyPresentation>().Tick(bird.Controller.State.Position);
            yield return Shot("objective-training-gold-score",
                "Training completed through injected ChallengeObservations and saved in isolation; actual hidden-instrument ribbon displays earned GOLD and score. No physical completion claim.");

            yield return LoadFlight(FlightActivity.RouteHome, "moth-line", "Magpie");
            bird = Object.FindAnyObjectByType<BirdFlightDriver>(); bird.GetComponent<FlightHud>().SetVisible(false);
            challenge = bird.Expedition.Challenge; checkpoint = challenge.Capture();
            checkpoint.Stage = 4; checkpoint.SeedCollected = true; checkpoint.Status = ChallengeStatus.Completed; checkpoint.Score = 1000;
            Require(challenge.Restore(checkpoint), "staged Route Home completion");
            Require(bird.Expedition.SaveCheckpoint(), "isolated restored garden save");
            Require(new FlightJourneyStore(FlightJourneyStore.CreateDefaultStorage()).Load().GardenRestored,
                "restored garden survives isolated store reload");
            space = Object.FindAnyObjectByType<WorldStreamer>().Space; var garden = FlightRegions.Island(space.Seed, 0, 0);
            var crown = space.ToLocal(garden.X, garden.Y, garden.Z) + SkyArchipelago.SanctuaryCrownOffset;
            StageBird(bird, space.ToLocal(garden.X, garden.Y + 3, garden.Z - 18), crown);
            var presentation = bird.GetComponent<JourneyPresentation>();
            Set(bird.Controller, "simulationTime", 100f); presentation.Tick(bird.Controller.State.Position);
            Set(bird.Controller, "simulationTime", 104f); presentation.Tick(bird.Controller.State.Position);
            Require(presentation.BloomVisible, "restored garden visual");
            yield return Shot("objective-route-home-garden-restored",
                "Staged completed Route Home checkpoint; actual save/reload confirms isolated GardenRestored and actual runtime crown/ribbon show the payoff. Bloom clock is advanced four seconds. No physical journey or landing claim.");
        }

        private static Vector3 Target(CourseTaskDefinition task, BirdFlightDriver bird)
        {
            var space = Object.FindAnyObjectByType<WorldStreamer>().Space;
            if (task.Kind == CourseTaskKind.Trick) return bird.Controller.State.Position + Vector3.forward * 20;
            var a = task.PointA;
            return space.ToLocal(a.X, task.Kind == CourseTaskKind.AltitudeBand
                ? (task.MinimumAltitude + task.MaximumAltitude) * .5 : a.Y, a.Z);
        }
        private static void StageBird(BirdFlightDriver bird, Vector3 position, Vector3 target)
        {
            var rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(target - position, Vector3.up), Vector3.up);
            typeof(BirdFlightController).GetProperty("State").SetValue(bird.Controller,
                new BirdState { Position = position, Rotation = rotation, Velocity = rotation * Vector3.forward * 8, Phase = FlightPhase.Gliding });
            bird.transform.SetPositionAndRotation(position, rotation); Set(bird, "comfortYaw", rotation.eulerAngles.y);
            WarmWorld(position); PlaceCamera(bird, target); bird.Controller.StreamingBlocked = false;
        }
        private static void WarmWorld(Vector3 position)
        {
            var world = Object.FindAnyObjectByType<WorldStreamer>();
            for (int i = 0; i < 240; i++) world.TickStreaming(position);
            Object.FindAnyObjectByType<SkyArchipelago>()?.Tick(position);
            Object.FindAnyObjectByType<SkyWeatherPresentation>()?.Tick(position); Physics.SyncTransforms();
        }
        private static void PlaceCamera(BirdFlightDriver bird, Vector3 target)
        {
            var position = bird.transform.position - bird.transform.forward * 5 + Vector3.up * 2;
            Camera.main.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));
        }
        private static BirdCharacterDefinition Character(string name) => Resources.Load<BirdCharacterDefinition>("Characters/" + name)
            ?? throw new InvalidOperationException("Missing character: " + name);
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Private).SetValue(owner, value);
        private static object Invoke(object owner, string name, params object[] values) => owner.GetType().GetMethod(name, Private).Invoke(owner, values);
        private static void Require(bool condition, string label) { if (!condition) throw new InvalidOperationException("Fixture failed: " + label); }
        private static void WriteReport() => File.WriteAllText(Path.Combine(Folder, "runtime-fixtures.json"), JsonUtility.ToJson(report, true));

        private sealed class ReviewClock : IMonotonicClock { public double NowSeconds { get; set; } }
        [Serializable] private sealed class Evidence
        {
            public string EvidenceKind = "STAGED RUNTIME LIFECYCLE AND VISUAL FIXTURES. Course clock, positions, catches, trick and supported landing observations are injected. All attempts are explicitly practice. Actual scene components generate UI and persist only to isolated stores.";
            public string Limits = "Does not prove controller or hand input-only course completion, physical reachability, motion comfort, stereo readability, Quest performance or recording readiness. Use RouteHomeReview.RunSpecies for separate actual semantic-input Route Home evidence.";
            public string EditorVersion, StartedUtc, CompletedUtc;
            public List<Frame> Frames = new List<Frame>();
        }
        [Serializable] private sealed class Frame
        {
            public string File, Scene, EvidenceKind, Course, State, NonRankedReasons, Activity, ChallengeState, ChallengeKind;
            public int TaskIndex, Stage, Score, Medal;
            public double Seconds, HighestAltitude;
            public float SoaringGain, RequiredGain, RequiredAltitude, CameraFieldOfView, CameraNearClip, CameraFarClip;
            public Vector3 BirdPosition, CameraPosition, CameraEuler;
            public bool Paused, RibbonVisible, HudVisible, GardenRestored; public string[] Text;
        }

        private sealed class MemoryStorage : IFlightSaveStorage, IForagingBestStorage, ICourseLeaderboardStorage
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>(); private int best;
            public bool HasKey(string key) => values.ContainsKey(key);
            public string GetString(string key, string fallback = "") => values.TryGetValue(key, out var value) ? value : fallback;
            public int GetInt(string key, int fallback = 0) => int.TryParse(GetString(key), out var value) ? value : fallback;
            public void SetString(string key, string value) => values[key] = value;
            public void Save() { }
            public int Load() => best;
            public bool Save(int value) { best = Math.Max(best, value); return true; }
            public string Load(string key) => GetString(key);
            public bool Save(string key, string json) { SetString(key, json); return true; }
        }

        private sealed class IsolationScope : IDisposable
        {
            private static readonly HashSet<string> EngineSessionPreferenceKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                "PT_Run", "unity.player_session_count", "unity.player_sessionid",
                "unity_connect.mega_session_id", "unity_connect.session_id"
            };
            public readonly string Directory;
            private readonly Func<IFlightSaveStorage> journey = FlightJourneyStore.EditorStorageOverride;
            private readonly Func<IForagingBestStorage> forage = SkyForaging.EditorStorageOverride;
            private readonly Func<ICourseLeaderboardStorage> leaderboard = CourseLeaderboardStore.EditorStorageOverride;
            private readonly Func<string> profiles = PlayerProfileCatalog.EditorDirectoryOverride;
            private readonly Func<string> history = FlightSessionHistoryStore.EditorDirectoryOverride;
            private readonly bool? telemetry = FlightTelemetry.EditorEnabledOverride;
            private readonly FlightActivity activity = ActivitySelection.Chosen;
            private readonly string course = ActivitySelection.ChosenCourseId;
            private readonly bool resume = ActivitySelection.ResumeRequested;
            private readonly BirdCharacterDefinition character = CharacterSelection.Chosen;
            private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
            private readonly Dictionary<string, int?> integers = new Dictionary<string, int?>();
            private readonly Dictionary<string, string> files;

            public IsolationScope(string directory)
            {
                Directory = directory; System.IO.Directory.CreateDirectory(directory);
                foreach (string key in new[] { FlightJourneyStore.SaveKey, FlightJourneyStore.BackupKey,
                    FlightJourneyStore.RecoveryKey, FlightJourneyStore.LegacyKey }) SaveString(key);
                foreach (var definition in CourseCatalog.All)
                    foreach (string species in new[] { "duck", "magpie", "dragon" })
                        foreach (string mode in Enum.GetNames(typeof(FlightControlMode)))
                            foreach (string wind in Enum.GetNames(typeof(WindMode)))
                                foreach (string assistance in new[] { "assisted", "manual" })
                                    SaveString(new CourseResultKey(definition, species, mode.ToLowerInvariant(), wind.ToLowerInvariant(), assistance).StorageKey);
                foreach (string suffix in new[] { "FirstPersonStabilized", "GuidanceEnabled", "AudioEnabled", "HapticsEnabled" })
                    SaveInt(FlightPreferences.KeyPrefix + suffix);
                SaveInt(FlightJourneyStore.ForagingKey); SaveInt(PlayerPrefsForagingBestStorage.TypedForagingKey);
                files = SnapshotFiles();
                File.WriteAllLines(Path.Combine(Folder, "persistent-files-before.txt"), files.Select(p => p.Key + "=" + p.Value));
                var memory = new MemoryStorage();
                FlightJourneyStore.EditorStorageOverride = () => memory; SkyForaging.EditorStorageOverride = () => memory;
                CourseLeaderboardStore.EditorStorageOverride = () => memory;
                PlayerProfileCatalog.EditorDirectoryOverride = () => Path.Combine(directory, "profiles");
                FlightSessionHistoryStore.EditorDirectoryOverride = () => Path.Combine(directory, "history");
                FlightTelemetry.EditorEnabledOverride = false;
            }
            private void SaveString(string key) => strings[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            private void SaveInt(string key) => integers[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
            public string Verify()
            {
                var differences = new List<string>();
                foreach (var value in strings)
                    if ((PlayerPrefs.HasKey(value.Key) ? PlayerPrefs.GetString(value.Key) : null) != value.Value) differences.Add("PlayerPrefs changed: " + value.Key);
                foreach (var value in integers)
                    if ((PlayerPrefs.HasKey(value.Key) ? PlayerPrefs.GetInt(value.Key) : (int?)null) != value.Value) differences.Add("PlayerPrefs changed: " + value.Key);
                var after = SnapshotFiles();
                foreach (string key in files.Keys.Union(after.Keys))
                    if (!files.TryGetValue(key, out var before) || !after.TryGetValue(key, out var current) || before != current) differences.Add("Persistent file changed: " + key);
                File.WriteAllLines(Path.Combine(Folder, "persistent-files-after.txt"), after.Select(p => p.Key + "=" + p.Value));
                return (differences.Count == 0 ? "PASS: known gameplay PlayerPrefs and existing persistent profile/history/telemetry files unchanged." : "FAIL:\n" + string.Join("\n", differences))
                    + "\nChecked " + strings.Count + " string keys, " + integers.Count + " integer keys and " + files.Count + " existing file/preference entries."
                    + "\nProfile/history files use SHA-256; telemetry uses filename, length and modification time."
                    + "\nEvery macOS Unity plist key is compared semantically except these exact engine/test session keys: "
                    + string.Join(", ", EngineSessionPreferenceKeys.OrderBy(k => k, StringComparer.Ordinal)) + "."
                    + "\nTelemetry disabled; journey/foraging/leaderboards use memory; profile/session files use " + Directory;
            }
            private static Dictionary<string, string> SnapshotFiles()
            {
                var result = new Dictionary<string, string>();
                foreach (string name in new[] { PlayerProfileCatalog.DefaultDirectoryName, FlightSessionHistoryStore.DefaultDirectoryName, "telemetry" })
                {
                    string root = Path.Combine(Application.persistentDataPath, name);
                    if (!System.IO.Directory.Exists(root)) continue;
                    foreach (string path in System.IO.Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(p => p))
                    {
                        var info = new FileInfo(path);
                        result[path] = name == "telemetry" ? info.Length + ":" + info.LastWriteTimeUtc.Ticks : Hash(path);
                    }
                }
                string plist = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Preferences",
                    "unity." + Application.companyName + "." + Application.productName + ".plist");
                if (File.Exists(plist)) SnapshotPlist(plist, result);
                return result;
            }
            private static void SnapshotPlist(string path, IDictionary<string, string> result)
            {
                // Read-only conversion through stdout; binary plist bytes legitimately
                // change as the Editor updates its own session counters during Play Mode.
                var start = new System.Diagnostics.ProcessStartInfo("/usr/bin/plutil")
                {
                    Arguments = "-convert xml1 -o - -- \"" + path.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"",
                    UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
                    CreateNoWindow = true
                };
                string xml;
                using (var process = System.Diagnostics.Process.Start(start))
                {
                    if (process == null) throw new InvalidOperationException("Could not inspect the Unity preferences plist.");
                    xml = process.StandardOutput.ReadToEnd(); string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0) throw new InvalidOperationException("Read-only plist conversion failed: " + error);
                }
                var root = XDocument.Parse(xml).Root?.Element("dict");
                if (root == null) throw new InvalidOperationException("Unity preferences plist is not a dictionary.");
                result[path] = "semantic plist dictionary";
                foreach (var pair in PlistEntries(root))
                    if (!EngineSessionPreferenceKeys.Contains(pair.Key))
                    {
                        string canonical = CanonicalPlistValue(pair.Value).ToString(SaveOptions.DisableFormatting);
                        using (var sha = SHA256.Create())
                            result[path + "#" + pair.Key] = BitConverter.ToString(sha.ComputeHash(
                                System.Text.Encoding.UTF8.GetBytes(canonical))).Replace("-", "").ToLowerInvariant();
                    }
            }
            private static IEnumerable<KeyValuePair<string, XElement>> PlistEntries(XElement dictionary)
            {
                var entries = dictionary.Elements().ToArray();
                if (entries.Length % 2 != 0) throw new InvalidOperationException("Malformed preferences dictionary.");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < entries.Length; i += 2)
                {
                    string key = entries[i].Value;
                    if (entries[i].Name != "key" || !seen.Add(key))
                        throw new InvalidOperationException("Invalid or duplicate preferences key.");
                    yield return new KeyValuePair<string, XElement>(key, entries[i + 1]);
                }
            }
            private static XElement CanonicalPlistValue(XElement value)
            {
                if (value.Name == "dict")
                    return new XElement("dict", PlistEntries(value).OrderBy(p => p.Key, StringComparer.Ordinal)
                        .Select(p => new XElement("entry", new XAttribute("key", p.Key), CanonicalPlistValue(p.Value))));
                if (value.Name == "array") return new XElement("array", value.Elements().Select(CanonicalPlistValue));
                return new XElement(value.Name, value.Value);
            }
            private static string Hash(string path)
            {
                using (var sha = SHA256.Create()) using (var input = File.OpenRead(path))
                    return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
            }
            public void Dispose()
            {
                FlightJourneyStore.EditorStorageOverride = journey; SkyForaging.EditorStorageOverride = forage;
                CourseLeaderboardStore.EditorStorageOverride = leaderboard;
                PlayerProfileCatalog.EditorDirectoryOverride = profiles; FlightSessionHistoryStore.EditorDirectoryOverride = history;
                FlightTelemetry.EditorEnabledOverride = telemetry;
                ActivitySelection.Chosen = activity; ActivitySelection.ChosenCourseId = course;
                ActivitySelection.ResumeRequested = resume; CharacterSelection.Chosen = character;
                // Only UI preferences can be intentionally staged during a review; preserve their exact existence/value.
                foreach (var entry in integers.Where(p => p.Key.StartsWith(FlightPreferences.KeyPrefix, StringComparison.Ordinal)))
                    if ((PlayerPrefs.HasKey(entry.Key) ? PlayerPrefs.GetInt(entry.Key) : (int?)null) != entry.Value)
                    {
                        if (entry.Value.HasValue) PlayerPrefs.SetInt(entry.Key, entry.Value.Value);
                        else PlayerPrefs.DeleteKey(entry.Key);
                    }
            }
        }
    }

    public sealed class CompetitionPreflightReviewRunner : MonoBehaviour { }
}
