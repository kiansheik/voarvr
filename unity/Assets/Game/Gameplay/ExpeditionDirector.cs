using UnityEngine;
using VoarVR.Flight;
using VoarVR.World;

namespace VoarVR.Gameplay
{
    public sealed class ExpeditionDirector : MonoBehaviour
    {
        public FlightChallenge Challenge { get; private set; }
        public FlightProgress Progress { get; private set; }
        public FlightJourneySave Journey { get; private set; }
        public ChallengeCheckpoint ResumeCheckpoint { get; private set; }
        public bool HasSafePerch { get; private set; }
        public LogicalPosition LastSupportedPerch { get; private set; }
        private FlightJourneyStore store;
        private BirdFlightDriver driver;
        private WorldSpace space;
        private TextMesh caption, beacon;
        private bool unlockedBefore, wasResting;
        private int previousBest;
        private float nextText;

        public static FlightJourneySave LoadJourney() => new FlightJourneyStore(FlightJourneyStore.CreateDefaultStorage()).Load();
        public static FlightProgress LoadProgress()
        {
            var journey = LoadJourney();
            journey.LegacyBests.JourneyRidgeUnlocked = journey.RidgeUnlocked;
            return journey.LegacyBests;
        }
        public void Configure(BirdFlightDriver bird, WorldSpace worldSpace, IFlightSaveStorage storage = null)
        {
            ResumeCheckpoint = null;
            HasSafePerch = false;
            LastSupportedPerch = default;
            wasResting = false;
            driver = bird;
            space = worldSpace;
            store = new FlightJourneyStore(storage ?? FlightJourneyStore.CreateDefaultStorage());
            Journey = store.Load();
            Progress = Journey.LegacyBests;
            Progress.JourneyRidgeUnlocked = Journey.RidgeUnlocked;
            unlockedBefore = Journey.RidgeUnlocked;
            previousBest = Journey.TrainingEfficiencyBest;
            Challenge = new FlightChallenge(ActivitySelection.Chosen, space != null ? space.Seed : 7319);
            var saved = Journey.GetCheckpoint(Challenge.Activity);
            if (ActivitySelection.ResumeRequested && saved?.Status == ChallengeStatus.Active)
            {
                if (Challenge.Restore(saved))
                {
                    ResumeCheckpoint = saved;
                    if (saved.HasSafePerch) { HasSafePerch = true; LastSupportedPerch = saved.SafePerch; }
                }
                else
                {
                    Journey.CanWrite = false;
                    Journey.SaveNotice = "The saved route uses a different world. Its checkpoint is preserved; this session starts at departure without saving.";
                }
            }
            ActivitySelection.ResumeRequested = false;
        }
        public void Restart()
        {
            if (Challenge == null) return;
            Challenge = new FlightChallenge(Challenge.Activity, space != null ? space.Seed : 7319);
            Challenge.InvalidateObservation();
            ResumeCheckpoint = null;
            HasSafePerch = false;
            wasResting = false;
            SaveCheckpoint();
        }
        public void InvalidateObservation() => Challenge?.InvalidateObservation();
        // Caller must have checked actual loaded support. This records a recovery candidate,
        // never teleports or asserts that the same collider will exist on a later launch.
        public void RecordSupportedPerch(LogicalPosition perch)
        {
            if (!FlightChallenge.Finite(perch.X) || !FlightChallenge.Finite(perch.Y) || !FlightChallenge.Finite(perch.Z)) return;
            LastSupportedPerch = perch;
            HasSafePerch = true;
        }
        public bool TryGetSafePerch(out LogicalPosition perch) { perch = LastSupportedPerch; return HasSafePerch; }
        public bool SaveCheckpoint()
        {
            if (store == null || Challenge == null || Journey == null || !Journey.CanWrite) return false;
            if (Challenge.Activity != FlightActivity.FreeFlight)
            {
                var checkpoint = Challenge.Capture();
                if (HasSafePerch) checkpoint.SetSafePerch(LastSupportedPerch);
                Journey.PutCheckpoint(checkpoint);
                Journey.ApplyCompletion(Challenge);
                Progress.JourneyRidgeUnlocked = Journey.RidgeUnlocked;
            }
            return store.Save();
        }
        public bool RecordForagingBest(int points)
        {
            if (Journey == null || points <= Journey.ForagingBest) return true;
            Journey.ForagingBest = points;
            return SaveCheckpoint();
        }
        public void Tick(float dt)
        {
            if (driver == null || Challenge == null || Challenge.Status != ChallengeStatus.Active) return;
            var controller = driver.Controller;
            if (controller.State.Phase == FlightPhase.Paused || controller.StreamingBlocked) return;
            if (controller.LastInput.ResetPressed) { Restart(); return; }
            if (driver.UsesXR && !driver.Calibration.Captured) return;
            var position = space != null ? space.ToLogical(controller.State.Position)
                : new LogicalPosition(controller.State.Position.x, controller.State.Position.y, controller.State.Position.z);
            int beforeStage = Challenge.Stage;
            var beforeStatus = Challenge.Status;
            bool resting = controller.State.Phase == FlightPhase.Perched;
            Challenge.Step(new ChallengeObservation(position, controller.State.Velocity, controller.WindVelocity.y,
                controller.StrokeForce.magnitude, dt, resting, controller.CollisionCount, controller.Tricks.Count, controller.SimulationTime));
            if (Challenge.Stage != beforeStage || Challenge.Status != beforeStatus || resting && !wasResting)
                SaveCheckpoint();
            wasResting = resting;
        }
        private void LateUpdate()
        {
            bool visible = driver != null && driver.ShowFlightText;
            if (caption != null) caption.gameObject.SetActive(visible);
            if (beacon != null) beacon.gameObject.SetActive(visible);
            if (!visible || Challenge == null || Challenge.Activity == FlightActivity.FreeFlight || Time.unscaledTime < nextText) return;
            nextText = Time.unscaledTime + .25f;
            if (caption == null && Camera.main != null)
            {
                var go = new GameObject("Expedition card"); go.transform.SetParent(Camera.main.transform, false);
                go.transform.localPosition = new Vector3(-.7f, .42f, 2); caption = go.AddComponent<TextMesh>();
                caption.fontSize = 40; caption.characterSize = .009f; caption.anchor = TextAnchor.UpperLeft;
                caption.color = new Color(.88f, .96f, 1); go.AddComponent<VoarVR.UI.FlightCard>();
            }
            if (caption == null || Camera.main == null) return;
            if (beacon == null)
            {
                var go = new GameObject("Expedition destination"); beacon = go.AddComponent<TextMesh>();
                beacon.fontSize = 48; beacon.characterSize = .08f; beacon.anchor = TextAnchor.MiddleCenter;
                beacon.color = new Color(1, .78f, .32f);
            }
            var target = Challenge.Target(driver.Controller.SimulationTime);
            var location = space != null ? space.ToLocal(target.X, target.Y, target.Z) : new Vector3((float)target.X, (float)target.Y, (float)target.Z);
            var eye = Camera.main;
            float distance = Vector3.Distance(eye.transform.position, location);
            beacon.transform.position = location + Vector3.up * (Challenge.Kind == ObjectiveKind.Land ? 2 : 7);
            beacon.transform.rotation = Quaternion.LookRotation(beacon.transform.position - eye.transform.position);
            beacon.transform.localScale = Vector3.one * Mathf.Clamp(distance / 80, .2f, 7);
            beacon.text = Challenge.Status == ChallengeStatus.Completed ? ""
                : Challenge.Kind == ObjectiveKind.DiscoverLift ? "RISING LEAVES"
                : Challenge.Kind == ObjectiveKind.Soar ? "RISE ABOVE THE CLOUDS"
                : Challenge.Kind == ObjectiveKind.CollectSeed ? "GARDEN SEED"
                : Challenge.Kind == ObjectiveKind.Migration ? "BRIGHT RIDGE"
                : Challenge.Kind == ObjectiveKind.Precision ? "SPLIT STONE ARCH" : "GARDEN TERRACE";
            string summary = Mathf.RoundToInt(Challenge.ActiveSeconds) + " s flying · " + Mathf.RoundToInt(Challenge.RestSeconds)
                + " s resting\n" + Mathf.RoundToInt(Challenge.MovementSeconds) + " s active strokes · " + Challenge.TechniqueCount + " tricks";
            if (Challenge.Status == ChallengeStatus.Completed)
            {
                caption.text = Challenge.Title + "\n" + (Challenge.IsEfficiencyTrial
                    ? new string('★', Challenge.Medal) + "  " + Challenge.Score + "  ·  BEST " + previousBest + "\nSOARING EFFICIENCY · Gold 850 / Silver 600"
                    : Challenge.Instruction) + "\n" + summary
                    + (!unlockedBefore && Journey.RidgeUnlocked ? "\nRidge Journey is now available" : "")
                    + (string.IsNullOrEmpty(Journey.SaveNotice) ? "\nSaved · rest, explore or leave whenever you like" : "\n" + Journey.SaveNotice);
            }
            else
            {
                caption.text = Challenge.Title + "  " + (Challenge.Stage + 1) + "/" + Challenge.StageCount + "\n" + Challenge.Instruction
                    + "\n" + Mathf.RoundToInt(distance) + " m · " + (Challenge.IsEfficiencyTrial ? "Soaring efficiency trial" : "Untimed adventure · breaks welcome")
                    + (Challenge.Kind == ObjectiveKind.Soar ? "\n" + Mathf.RoundToInt(Challenge.SoaringGain) + " / " + Challenge.RequiredGain
                        + (Challenge.Activity == FlightActivity.RouteHome ? " m rising-air climb" : " m quiet climb")
                        + " · High " + Mathf.RoundToInt((float)Challenge.HighestAltitude) + " / " + Challenge.RequiredAltitude + " m" : "")
                    + (driver.Controller.State.Phase == FlightPhase.Perched ? "\nResting · your progress is kept" : "");
            }
        }
        private void OnApplicationPause(bool paused) { if (paused) SaveCheckpoint(); }
        private void OnApplicationFocus(bool focused) { if (!focused) SaveCheckpoint(); }
        private void OnDisable() => SaveCheckpoint();
        private void OnDestroy()
        {
            if (caption != null) Destroy(caption.gameObject);
            if (beacon != null) Destroy(beacon.gameObject);
        }
    }
}
