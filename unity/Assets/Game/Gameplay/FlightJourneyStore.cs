using System;
using UnityEngine;
using VoarVR.World;

namespace VoarVR.Gameplay
{
    [Serializable]
    public sealed class ChallengeCheckpoint
    {
        public FlightActivity Activity;
        public string ContentId;
        public int WorldSeed;
        public ChallengeStatus Status;
        public int Stage;
        public float ActiveSeconds, RestSeconds, StrokeSeconds, SoaringGain;
        public double HighestAltitude;
        public int CollisionCount, TechniqueCount, Score;
        public bool SeedCollected, HasSafePerch;
        public double PerchX, PerchY, PerchZ;
        public LogicalPosition SafePerch => new LogicalPosition(PerchX, PerchY, PerchZ);
        public void SetSafePerch(LogicalPosition perch)
        { HasSafePerch = true; PerchX = perch.X; PerchY = perch.Y; PerchZ = perch.Z; }
        public bool IsValid()
        {
            if (Activity == FlightActivity.FreeFlight || ContentId != FlightChallenge.ContentIdFor(Activity)
                || ContentId == null || !Enum.IsDefined(typeof(ChallengeStatus), Status) || Status == ChallengeStatus.Available)
                return false;
            int count = Activity == FlightActivity.Training ? 2 : Activity == FlightActivity.RidgeJourney || Activity == FlightActivity.RouteHome ? 5 : 4;
            if (Stage < 0 || Stage > count || (Status == ChallengeStatus.Active && Stage >= count)) return false;
            if (Status == ChallengeStatus.Completed && (Stage != (Activity == FlightActivity.Training ? count : count - 1) || Score <= 0)) return false;
            if (Status != ChallengeStatus.Completed && Score != 0) return false;
            if (Activity == FlightActivity.RouteHome && SeedCollected != (Stage >= 3)) return false;
            return Nonnegative(ActiveSeconds) && Nonnegative(RestSeconds) && Nonnegative(StrokeSeconds)
                && StrokeSeconds <= ActiveSeconds + .1f && Nonnegative(SoaringGain) && FlightChallenge.Finite(HighestAltitude)
                && CollisionCount >= 0 && TechniqueCount >= 0 && Score >= 0
                && (!HasSafePerch || FlightChallenge.Finite(PerchX) && FlightChallenge.Finite(PerchY) && FlightChallenge.Finite(PerchZ));
        }
        private static bool Nonnegative(float value) => FlightChallenge.Finite(value) && value >= 0;
    }

    [Serializable]
    public sealed class FlightJourneySave
    {
        public const int CurrentVersion = 2;
        public int Version = CurrentVersion;
        public FlightProgress LegacyBests = new FlightProgress();
        public int ForagingBest, TrainingEfficiencyBest;
        public bool RouteHomeCompleted, GardenRestored, SkywardCompleted, RidgeCompleted;
        public ChallengeCheckpoint[] Checkpoints = Array.Empty<ChallengeCheckpoint>();
        [NonSerialized] public bool CanWrite = true;
        [NonSerialized] public string SaveNotice = "";
        public bool RidgeUnlocked => LegacyBests.RidgeUnlocked || SkywardCompleted || RouteHomeCompleted;
        public ChallengeCheckpoint GetCheckpoint(FlightActivity activity)
        {
            if (Checkpoints == null) return null;
            foreach (var checkpoint in Checkpoints) if (checkpoint != null && checkpoint.Activity == activity) return checkpoint;
            return null;
        }
        public bool HasResume(FlightActivity activity) => GetCheckpoint(activity)?.Status == ChallengeStatus.Active;
        public void PutCheckpoint(ChallengeCheckpoint checkpoint)
        {
            if (checkpoint == null || !checkpoint.IsValid()) throw new ArgumentException("Invalid journey checkpoint.", nameof(checkpoint));
            if (Checkpoints == null) Checkpoints = Array.Empty<ChallengeCheckpoint>();
            for (int i = 0; i < Checkpoints.Length; i++)
                if (Checkpoints[i].Activity == checkpoint.Activity) { Checkpoints[i] = checkpoint; return; }
            Array.Resize(ref Checkpoints, Checkpoints.Length + 1);
            Checkpoints[Checkpoints.Length - 1] = checkpoint;
        }
        // Monotonic flags are the story reward. Repeated completion/save callbacks cannot
        // double-award currency or remove an already restored garden.
        public void ApplyCompletion(FlightChallenge challenge)
        {
            if (challenge == null || challenge.Status != ChallengeStatus.Completed) return;
            switch (challenge.Activity)
            {
                case FlightActivity.RouteHome: RouteHomeCompleted = GardenRestored = true; break;
                case FlightActivity.SkywardExpedition: SkywardCompleted = true; break;
                case FlightActivity.RidgeJourney: RidgeCompleted = true; break;
                case FlightActivity.Training: TrainingEfficiencyBest = Math.Max(TrainingEfficiencyBest, challenge.Score); break;
            }
        }
    }

    public interface IFlightSaveStorage
    {
        bool HasKey(string key);
        string GetString(string key, string fallback = "");
        int GetInt(string key, int fallback = 0);
        void SetString(string key, string value);
        void Save();
    }
    public sealed class PlayerPrefsFlightSaveStorage : IFlightSaveStorage
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public string GetString(string key, string fallback = "") => PlayerPrefs.GetString(key, fallback);
        public int GetInt(string key, int fallback = 0) => PlayerPrefs.GetInt(key, fallback);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Save() => PlayerPrefs.Save();
    }

    // Two independently valid generations; invalid data is copied intact before replacement.
    // Unknown versions/content are read-only, so an older executable cannot destroy newer saves.
    public sealed class FlightJourneyStore
    {
#if UNITY_EDITOR
        // Editor test suites can replace persistence before any scene creates a save owner.
        public static Func<IFlightSaveStorage> EditorStorageOverride;
#endif
        public static IFlightSaveStorage CreateDefaultStorage()
        {
#if UNITY_EDITOR
            if (EditorStorageOverride != null) return EditorStorageOverride();
#endif
            return new PlayerPrefsFlightSaveStorage();
        }
        public const string SaveKey = "VoarVR.Journey.v2";
        public const string BackupKey = SaveKey + ".backup";
        public const string RecoveryKey = SaveKey + ".recovered";
        public const string LegacyKey = "VoarVR.FlightProgress.v1";
        public const string ForagingKey = "VoarVR.ForagingBest.v1";
        private readonly IFlightSaveStorage storage;
        private string lastGoodRaw, damagedRaw;
        private bool loaded;
        public FlightJourneySave Data { get; private set; }
        [Serializable] private sealed class VersionHeader { public int Version; }
        public FlightJourneyStore(IFlightSaveStorage storage) { this.storage = storage ?? throw new ArgumentNullException(nameof(storage)); }
        public FlightJourneySave Load()
        {
            if (loaded) return Data;
            loaded = true;
            try
            {
                string raw = storage.GetString(SaveKey);
                if (!string.IsNullOrEmpty(raw))
                {
                    var result = Decode(raw, out var parsed);
                    if (result == DecodeResult.Valid) { Data = parsed; lastGoodRaw = raw; }
                    else if (result == DecodeResult.Unsupported)
                    {
                        Data = LoadLegacy();
                        Data.CanWrite = false;
                        Data.SaveNotice = "This journey uses newer or unavailable content. Its save is preserved; this session cannot replace it.";
                    }
                    else
                    {
                        damagedRaw = raw;
                        string backup = storage.GetString(BackupKey);
                        var backupResult = Decode(backup, out var recovered);
                        if (backupResult == DecodeResult.Unsupported)
                        {
                            Data = LoadLegacy(); Data.CanWrite = false;
                            Data.SaveNotice = "The recovery save uses newer or unavailable content. Original data is preserved; saving is unavailable.";
                        }
                        else if (backupResult == DecodeResult.Valid)
                        { Data = recovered; lastGoodRaw = backup; Data.SaveNotice = "Recovered the previous journey checkpoint. The damaged save is preserved."; }
                        else
                        { Data = LoadLegacy(); Data.SaveNotice = "The journey save could not be read. Original data is preserved; a new journey can be saved."; }
                    }
                }
                else
                {
                    string backup = storage.GetString(BackupKey);
                    var backupResult = Decode(backup, out var recovered);
                    if (backupResult == DecodeResult.Unsupported)
                    {
                        Data = LoadLegacy(); Data.CanWrite = false;
                        Data.SaveNotice = "The recovery save uses newer or unavailable content. Original data is preserved; saving is unavailable.";
                    }
                    else if (backupResult == DecodeResult.Valid)
                    { Data = recovered; lastGoodRaw = backup; Data.SaveNotice = "Recovered the previous journey checkpoint."; }
                    else Data = LoadLegacy();
                }
                Data.ForagingBest = Math.Max(Data.ForagingBest, storage.GetInt(ForagingKey));
                // JsonUtility does not guarantee nonserialized field initializers on load.
                if (Data.SaveNotice == null) Data.SaveNotice = "";
                return Data;
            }
            catch (Exception e)
            {
                Data = new FlightJourneySave { CanWrite = false, SaveNotice = "Journey storage is unavailable: " + e.Message };
                return Data;
            }
        }
        public bool Save()
        {
            if (!loaded) Load();
            if (!Data.CanWrite) return false;
            try
            {
                if (!ValidData(Data)) throw new InvalidOperationException("Journey checkpoint validation failed.");
                // Re-read before writing: another owner/newer build may have saved since Load.
                string existing = storage.GetString(SaveKey);
                string existingBackup = storage.GetString(BackupKey);
                if (Decode(existing, out _) == DecodeResult.Unsupported || Decode(existingBackup, out _) == DecodeResult.Unsupported)
                {
                    Data.CanWrite = false;
                    Data.SaveNotice = "Newer or unavailable journey content was found. Its save is preserved; saving is unavailable.";
                    return false;
                }
                if (!string.IsNullOrEmpty(damagedRaw)) PreserveRaw(damagedRaw);
                if (!string.IsNullOrEmpty(existing) && Decode(existing, out _) == DecodeResult.Invalid) PreserveRaw(existing);
                if (!string.IsNullOrEmpty(existingBackup) && Decode(existingBackup, out _) == DecodeResult.Invalid) PreserveRaw(existingBackup);
                if (!string.IsNullOrEmpty(lastGoodRaw)) storage.SetString(BackupKey, lastGoodRaw);
                string raw = JsonUtility.ToJson(Data);
                storage.SetString(SaveKey, raw);
                storage.Save();
                lastGoodRaw = raw;
                damagedRaw = null;
                if (Data.SaveNotice.StartsWith("Journey could not be saved:", StringComparison.Ordinal)) Data.SaveNotice = "";
                return true;
            }
            catch (Exception e)
            {
                Data.SaveNotice = "Journey could not be saved: " + e.Message + ". The previous checkpoint is retained.";
                return false;
            }
        }
        private void PreserveRaw(string raw)
        {
            string key = RecoveryKey;
            int suffix = 0;
            while (storage.HasKey(key))
            {
                if (storage.GetString(key) == raw) return;
                key = RecoveryKey + "." + ++suffix;
            }
            storage.SetString(key, raw);
        }
        private FlightJourneySave LoadLegacy()
        {
            var save = new FlightJourneySave();
            try
            {
                string raw = storage.GetString(LegacyKey, "");
                if (!string.IsNullOrEmpty(raw))
                {
                    var header = JsonUtility.FromJson<VersionHeader>(raw);
                    var progress = JsonUtility.FromJson<FlightProgress>(raw);
                    if (header != null && header.Version == 1 && ValidLegacy(progress)) save.LegacyBests = progress;
                    else save.SaveNotice = "Legacy scores could not be read; their original data is preserved.";
                }
            }
            catch { save.SaveNotice = "Legacy scores could not be read; their original data is preserved."; }
            save.ForagingBest = Math.Max(0, storage.GetInt(ForagingKey));
            return save;
        }
        private enum DecodeResult { Invalid, Unsupported, Valid }
        private static DecodeResult Decode(string raw, out FlightJourneySave save)
        {
            save = null;
            if (string.IsNullOrWhiteSpace(raw)) return DecodeResult.Invalid;
            try
            {
                var header = JsonUtility.FromJson<VersionHeader>(raw);
                if (header == null || header.Version == 0) return DecodeResult.Invalid;
                if (header.Version != FlightJourneySave.CurrentVersion) return DecodeResult.Unsupported;
                save = JsonUtility.FromJson<FlightJourneySave>(raw);
                if (save?.Checkpoints != null)
                    foreach (var checkpoint in save.Checkpoints)
                        if (checkpoint != null && (FlightChallenge.ContentIdFor(checkpoint.Activity) == null
                            || checkpoint.ContentId != FlightChallenge.ContentIdFor(checkpoint.Activity))) return DecodeResult.Unsupported;
                if (!ValidData(save)) return DecodeResult.Invalid;
                save.CanWrite = true;
                save.SaveNotice = "";
                return DecodeResult.Valid;
            }
            catch { return DecodeResult.Invalid; }
        }
        private static bool ValidLegacy(FlightProgress progress) => progress != null && progress.Version == 1
            && progress.SkywardBest >= 0 && progress.RidgeBest >= 0 && progress.TrainingBest >= 0 && progress.TrickBest >= 0;
        private static bool ValidData(FlightJourneySave save)
        {
            if (save == null || save.Version != FlightJourneySave.CurrentVersion || !ValidLegacy(save.LegacyBests)
                || save.ForagingBest < 0 || save.TrainingEfficiencyBest < 0 || save.Checkpoints == null || save.Checkpoints.Length > 4
                || save.GardenRestored != save.RouteHomeCompleted) return false;
            int activityMask = 0;
            foreach (var checkpoint in save.Checkpoints)
            {
                if (checkpoint == null || !checkpoint.IsValid()) return false;
                int bit = 1 << (int)checkpoint.Activity;
                if ((activityMask & bit) != 0) return false;
                activityMask |= bit;
            }
            return true;
        }
    }
}
