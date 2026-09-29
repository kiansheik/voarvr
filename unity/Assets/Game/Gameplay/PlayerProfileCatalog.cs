using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace VoarVR.Gameplay
{
    [Serializable]
    public sealed class LocalPlayerProfile
    {
        public string Id = "";
        public string DisplayName = "";
        public string CreatedUtc = "";
        public string LastPlayedUtc = "";

        public bool IsValid()
        {
            return FlightSessionSummary.SafeId(Id) && PlayerProfileCatalog.ValidName(DisplayName)
                && FlightSessionSummary.ValidUtc(CreatedUtc) && FlightSessionSummary.ValidUtc(LastPlayedUtc);
        }
    }

    [Serializable]
    public sealed class PlayerProfileCatalogSnapshot
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public string DefaultProfileId = "";
        public string ActiveProfileId = "";
        public LocalPlayerProfile[] Profiles = Array.Empty<LocalPlayerProfile>();

        public bool IsValid()
        {
            if (Version != CurrentVersion || Profiles == null || Profiles.Length < 1 || Profiles.Length > 32
                || !FlightSessionSummary.SafeId(DefaultProfileId)
                || !FlightSessionSummary.SafeId(ActiveProfileId)) return false;
            bool foundDefault = false, foundActive = false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var profile in Profiles)
            {
                if (profile == null || !profile.IsValid() || !ids.Add(profile.Id)) return false;
                foundDefault |= StringComparer.Ordinal.Equals(profile.Id, DefaultProfileId);
                foundActive |= StringComparer.Ordinal.Equals(profile.Id, ActiveProfileId);
            }
            return foundDefault && foundActive;
        }
    }

    // Small local identity catalog. It intentionally owns no journey/foraging PlayerPrefs keys;
    // those save schemas remain independent and can be associated during later integration.
    public sealed class PlayerProfileCatalog
    {
#if UNITY_EDITOR
        // Install before loading scenes so their default catalogs cannot touch player data.
        public static Func<string> EditorDirectoryOverride;
#endif
        public const int CurrentStorageVersion = 1;
        public const string BuiltInProfileId = "default";
        public const string BuiltInProfileName = "Player 1";
        public const string DefaultDirectoryName = "player-profiles-v1";
        public const int MaximumDisplayNameLength = 40;

        [Serializable]
        private sealed class Envelope
        {
            public int StorageVersion = CurrentStorageVersion;
            public long Generation;
            public PlayerProfileCatalogSnapshot Catalog;
        }

        private enum ReadState { Missing, Valid, Corrupt, Unsupported }

        private readonly string rootDirectory;
        private readonly Func<string> idFactory;
        private readonly Func<DateTime> utcClock;
        private PlayerProfileCatalogSnapshot catalog;
        private long generation;
        private int activeSlot = -1;

        public string RootDirectory => rootDirectory;
        public bool CanWrite { get; private set; } = true;
        public string Notice { get; private set; } = "";
        public string DefaultProfileId => catalog.DefaultProfileId;
        public string ActiveProfileId => catalog.ActiveProfileId;
        public LocalPlayerProfile DefaultProfile => FindCopy(catalog.DefaultProfileId);
        public LocalPlayerProfile ActiveProfile => FindCopy(catalog.ActiveProfileId);
        public LocalPlayerProfile[] Profiles => Copy(catalog).Profiles;

        public PlayerProfileCatalog(string directory = null, Func<string> profileIdFactory = null,
            Func<DateTime> utcNow = null)
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(directory) && EditorDirectoryOverride != null)
            {
                directory = EditorDirectoryOverride();
                if (string.IsNullOrWhiteSpace(directory))
                    throw new InvalidOperationException("The editor profile directory override must return an isolated directory.");
            }
#endif
            rootDirectory = string.IsNullOrWhiteSpace(directory)
                ? Path.Combine(Application.persistentDataPath, DefaultDirectoryName)
                : Path.GetFullPath(directory);
            idFactory = profileIdFactory ?? (() => "p-" + Guid.NewGuid().ToString("N"));
            utcClock = utcNow ?? (() => DateTime.UtcNow);
            catalog = NewCatalog(NowUtc());
            Load();
        }

        public bool Load()
        {
            Notice = "";
            var state0 = Read(SlotPath(0), out var first);
            var state1 = Read(SlotPath(1), out var second);
            if (state0 == ReadState.Unsupported || state1 == ReadState.Unsupported)
            {
                AdoptBest(first, second);
                CanWrite = false;
                Notice = "A profile catalog uses a newer version; existing files were preserved.";
                return false;
            }
            Envelope best = null;
            int bestSlot = -1;
            if (state0 == ReadState.Valid) { best = first; bestSlot = 0; }
            if (state1 == ReadState.Valid && (best == null || second.Generation > best.Generation))
            { best = second; bestSlot = 1; }
            if (best != null)
            {
                catalog = Copy(best.Catalog);
                generation = best.Generation;
                activeSlot = bestSlot;
                CanWrite = true;
                if (state0 == ReadState.Corrupt || state1 == ReadState.Corrupt)
                    Notice = "Recovered the profile catalog from its other generation.";
                return true;
            }
            if (state0 == ReadState.Corrupt || state1 == ReadState.Corrupt)
            {
                catalog = NewCatalog(NowUtc());
                generation = 0;
                activeSlot = -1;
                CanWrite = false;
                Notice = "No readable profile catalog generation was found; corrupt files were preserved.";
                return false;
            }

            catalog = NewCatalog(NowUtc());
            generation = 0;
            activeSlot = -1;
            CanWrite = true;
            return Persist(catalog);
        }

        public bool CreateProfile(string displayName, out LocalPlayerProfile profile)
        {
            profile = null;
            if (!CanWrite || !TryNormalizeName(displayName, out string normalized)
                || catalog.Profiles.Length >= 32) return false;
            string id = "";
            for (int attempt = 0; attempt < 8; attempt++)
            {
                string candidate = idFactory();
                if (FlightSessionSummary.SafeId(candidate) && Find(candidate) == null)
                { id = candidate; break; }
            }
            if (string.IsNullOrEmpty(id))
            {
                Notice = "Could not create a unique profile id.";
                return false;
            }
            string now = NowUtc().ToString("o");
            var created = new LocalPlayerProfile
                { Id = id, DisplayName = normalized, CreatedUtc = now, LastPlayedUtc = now };
            var candidateCatalog = Copy(catalog);
            var profiles = new List<LocalPlayerProfile>(candidateCatalog.Profiles) { created };
            candidateCatalog.Profiles = profiles.ToArray();
            candidateCatalog.ActiveProfileId = id;
            if (!Persist(candidateCatalog)) return false;
            profile = Copy(created);
            return true;
        }

        public bool RenameProfile(string profileId, string displayName)
        {
            if (!CanWrite || !TryNormalizeName(displayName, out string normalized)) return false;
            var candidate = Copy(catalog);
            var profile = Find(candidate, profileId);
            if (profile == null) return false;
            profile.DisplayName = normalized;
            return Persist(candidate);
        }

        public bool SetActiveProfile(string profileId)
        {
            if (!CanWrite) return false;
            var candidate = Copy(catalog);
            var profile = Find(candidate, profileId);
            if (profile == null) return false;
            candidate.ActiveProfileId = profileId;
            profile.LastPlayedUtc = NowUtc().ToString("o");
            return Persist(candidate);
        }

        public bool SetDefaultProfile(string profileId)
        {
            if (!CanWrite || Find(profileId) == null) return false;
            var candidate = Copy(catalog);
            candidate.DefaultProfileId = profileId;
            return Persist(candidate);
        }

        private bool Persist(PlayerProfileCatalogSnapshot candidate)
        {
            if (!CanWrite || candidate == null || !candidate.IsValid()) return false;
            if (generation == long.MaxValue)
            {
                Notice = "The profile catalog generation counter overflowed.";
                return false;
            }
            int targetSlot = activeSlot == 0 ? 1 : 0;
            long targetGeneration = generation + 1;
            string path = SlotPath(targetSlot);
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(rootDirectory);
                var envelope = new Envelope
                    { Generation = targetGeneration, Catalog = Copy(candidate) };
                byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(envelope));
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
                catalog = Copy(candidate);
                generation = targetGeneration;
                activeSlot = targetSlot;
                Notice = "";
                return true;
            }
            catch (Exception error)
            {
                Notice = "Could not save the profile catalog: " + error.Message;
                return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch { }
            }
        }

        private ReadState Read(string path, out Envelope envelope)
        {
            envelope = null;
            if (!File.Exists(path)) return ReadState.Missing;
            try
            {
                envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path, Encoding.UTF8));
                if (envelope == null) return ReadState.Corrupt;
                if (envelope.StorageVersion != CurrentStorageVersion || envelope.Catalog == null
                    || envelope.Catalog.Version != PlayerProfileCatalogSnapshot.CurrentVersion)
                    return ReadState.Unsupported;
                if (envelope.Generation <= 0 || !envelope.Catalog.IsValid()) return ReadState.Corrupt;
                return ReadState.Valid;
            }
            catch
            {
                envelope = null;
                return ReadState.Corrupt;
            }
        }

        private void AdoptBest(Envelope first, Envelope second)
        {
            Envelope best = null;
            int slot = -1;
            if (first != null && first.StorageVersion == CurrentStorageVersion && first.Catalog != null
                && first.Catalog.IsValid()) { best = first; slot = 0; }
            if (second != null && second.StorageVersion == CurrentStorageVersion && second.Catalog != null
                && second.Catalog.IsValid() && (best == null || second.Generation > best.Generation))
            { best = second; slot = 1; }
            if (best == null)
            {
                catalog = NewCatalog(NowUtc());
                generation = 0;
                activeSlot = -1;
            }
            else
            {
                catalog = Copy(best.Catalog);
                generation = best.Generation;
                activeSlot = slot;
            }
        }

        private LocalPlayerProfile Find(string id) => Find(catalog, id);

        private static LocalPlayerProfile Find(PlayerProfileCatalogSnapshot source, string id)
        {
            if (source?.Profiles == null) return null;
            foreach (var profile in source.Profiles)
                if (profile != null && StringComparer.Ordinal.Equals(profile.Id, id)) return profile;
            return null;
        }

        private LocalPlayerProfile FindCopy(string id)
        {
            var found = Find(id);
            return found == null ? null : Copy(found);
        }

        private DateTime NowUtc()
        {
            DateTime value = utcClock();
            return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        }

        private string SlotPath(int slot) => Path.Combine(rootDirectory, "profiles." + slot + ".json");

        private static PlayerProfileCatalogSnapshot NewCatalog(DateTime utcNow)
        {
            string timestamp = utcNow.ToString("o");
            return new PlayerProfileCatalogSnapshot
            {
                DefaultProfileId = BuiltInProfileId,
                ActiveProfileId = BuiltInProfileId,
                Profiles = new[]
                {
                    new LocalPlayerProfile
                    {
                        Id = BuiltInProfileId,
                        DisplayName = BuiltInProfileName,
                        CreatedUtc = timestamp,
                        LastPlayedUtc = timestamp
                    }
                }
            };
        }

        internal static bool ValidName(string value) => TryNormalizeName(value, out _)
            && StringComparer.Ordinal.Equals(value, value.Trim());

        private static bool TryNormalizeName(string value, out string normalized)
        {
            normalized = value == null ? "" : value.Trim();
            if (normalized.Length < 1 || normalized.Length > MaximumDisplayNameLength) return false;
            foreach (char c in normalized)
                if (char.IsControl(c)) return false;
            return true;
        }

        private static LocalPlayerProfile Copy(LocalPlayerProfile source)
        {
            return new LocalPlayerProfile
            {
                Id = source.Id,
                DisplayName = source.DisplayName,
                CreatedUtc = source.CreatedUtc,
                LastPlayedUtc = source.LastPlayedUtc
            };
        }

        private static PlayerProfileCatalogSnapshot Copy(PlayerProfileCatalogSnapshot source)
        {
            var profiles = new LocalPlayerProfile[source.Profiles.Length];
            for (int i = 0; i < profiles.Length; i++) profiles[i] = Copy(source.Profiles[i]);
            return new PlayerProfileCatalogSnapshot
            {
                Version = source.Version,
                DefaultProfileId = source.DefaultProfileId,
                ActiveProfileId = source.ActiveProfileId,
                Profiles = profiles
            };
        }
    }
}
