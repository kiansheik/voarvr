using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace VoarVR.Gameplay
{
    // One immutable final record per session, plus two rotating draft generations. Draft slots
    // make an interrupted replacement recoverable; a final is never overwritten once readable.
    public sealed class FlightSessionHistoryStore
    {
        public const int CurrentStorageVersion = 1;
        public const string DefaultDirectoryName = "session-history-v1";

        [Serializable]
        private sealed class Envelope
        {
            public int StorageVersion = CurrentStorageVersion;
            public long Generation;
            public FlightSessionSummary Summary;
        }

        private enum ReadState { Missing, Valid, Corrupt, Unsupported }

        private sealed class DraftCandidate
        {
            public int Slot;
            public Envelope Envelope;
        }

        private readonly string rootDirectory;
        public string RootDirectory => rootDirectory;
        public string LastError { get; private set; } = "";

        public FlightSessionHistoryStore(string directory = null)
        {
            rootDirectory = string.IsNullOrWhiteSpace(directory)
                ? Path.Combine(Application.persistentDataPath, DefaultDirectoryName)
                : Path.GetFullPath(directory);
        }

        public bool SaveDraft(FlightSessionSummary summary, bool durable = true)
        {
            LastError = "";
            if (!Validate(summary, false)) return false;
            string directory = SessionDirectory(summary.Context.ProfileId);
            string finalPath = FinalPath(directory, summary.Context.SessionId);
            var finalState = Read(finalPath, summary.Context.ProfileId, summary.Context.SessionId, true, out _);
            if (finalState == ReadState.Valid) return true;
            if (finalState != ReadState.Missing)
                return Fail("The existing final session file is unreadable or uses a newer storage version.");

            var current = LatestDraft(directory, summary.Context.ProfileId, summary.Context.SessionId,
                out bool unsupported, out bool unrecoverable);
            if (unsupported) return Fail("A draft uses a newer or unsupported session format; it was preserved.");
            if (unrecoverable) return Fail("No readable draft generation is available; corrupt files were preserved.");
            long generation = current == null ? 1 : current.Envelope.Generation + 1;
            if (generation <= 0) return Fail("The draft generation counter overflowed.");
            int targetSlot = current == null ? 0 : 1 - current.Slot;
            var envelope = new Envelope { Generation = generation, Summary = Clone(summary) };
            return WriteReplacing(DraftPath(directory, summary.Context.SessionId, targetSlot), envelope, durable);
        }

        public bool Finalize(FlightSessionSummary summary)
        {
            LastError = "";
            if (!Validate(summary, true)) return false;
            string directory = SessionDirectory(summary.Context.ProfileId);
            string path = FinalPath(directory, summary.Context.SessionId);
            var state = Read(path, summary.Context.ProfileId, summary.Context.SessionId, true, out _);
            if (state == ReadState.Valid)
            {
                CleanupDrafts(directory, summary.Context.SessionId);
                return true;
            }
            if (state != ReadState.Missing)
                return Fail("The existing final session file is unreadable or uses a newer storage version.");
            var envelope = new Envelope { Generation = 1, Summary = Clone(summary) };
            if (!WriteOnce(path, envelope)) return false;
            CleanupDrafts(directory, summary.Context.SessionId);
            return true;
        }

        public bool TryLoadLatestDraft(string profileId, string sessionId, out FlightSessionSummary summary)
        {
            LastError = "";
            summary = null;
            if (!ValidIdentity(profileId, sessionId)) return false;
            var candidate = LatestDraft(SessionDirectory(profileId), profileId, sessionId,
                out bool unsupported, out bool unrecoverable);
            if (unsupported) return Fail("A draft uses a newer or unsupported session format.");
            if (candidate == null)
            {
                if (unrecoverable) Fail("No readable draft generation is available.");
                return false;
            }
            summary = Clone(candidate.Envelope.Summary);
            return true;
        }

        public bool TryLoadFinal(string profileId, string sessionId, out FlightSessionSummary summary)
        {
            LastError = "";
            summary = null;
            if (!ValidIdentity(profileId, sessionId)) return false;
            var state = Read(FinalPath(SessionDirectory(profileId), sessionId), profileId, sessionId, true,
                out var envelope);
            if (state == ReadState.Valid)
            {
                summary = Clone(envelope.Summary);
                return true;
            }
            if (state == ReadState.Unsupported) Fail("The final session uses a newer storage version.");
            else if (state == ReadState.Corrupt) Fail("The final session file is unreadable.");
            return false;
        }

        public FlightSessionSummary[] LoadFinals(string profileId)
        {
            LastError = "";
            if (!FlightSessionSummary.SafeId(profileId))
            {
                Fail("The profile id is invalid.");
                return Array.Empty<FlightSessionSummary>();
            }
            var summaries = new List<FlightSessionSummary>();
            string directory = SessionDirectory(profileId);
            bool incomplete = false;
            try
            {
                if (!Directory.Exists(directory)) return summaries.ToArray();
                foreach (string path in Directory.GetFiles(directory, "*.final.json"))
                {
                    string file = Path.GetFileName(path);
                    const string suffix = ".final.json";
                    string sessionId = file.Substring(0, file.Length - suffix.Length);
                    if (!FlightSessionSummary.SafeId(sessionId)) { incomplete = true; continue; }
                    if (Read(path, profileId, sessionId, true, out var envelope) == ReadState.Valid)
                        summaries.Add(Clone(envelope.Summary));
                    else incomplete = true;
                }
            }
            catch (Exception error)
            {
                Fail("Could not enumerate session history: " + error.Message);
            }
            summaries.Sort((a, b) => StringComparer.Ordinal.Compare(b.EndedUtc, a.EndedUtc));
            if (incomplete && string.IsNullOrEmpty(LastError))
                LastError = "Some session files were unreadable or from a newer version; totals are partial.";
            return summaries.ToArray();
        }

        // On the next player-select visit, durable draft generations from a killed app are
        // closed as interrupted sessions. Unknown/corrupt files are preserved and reported.
        public int RecoverInterruptedDrafts(string profileId, DateTime endedAtUtc)
        {
            LastError = "";
            if (!FlightSessionSummary.SafeId(profileId) || endedAtUtc.Kind != DateTimeKind.Utc)
            {
                Fail("A valid profile and UTC recovery time are required.");
                return 0;
            }
            string directory = SessionDirectory(profileId);
            if (!Directory.Exists(directory)) return 0;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool incomplete = false;
            int recovered = 0;
            string operationError = "";
            try
            {
                foreach (string path in Directory.GetFiles(directory, "*.draft.*.json"))
                {
                    string file = Path.GetFileName(path);
                    int marker = file.IndexOf(".draft.", StringComparison.Ordinal);
                    if (marker <= 0) { incomplete = true; continue; }
                    string id = file.Substring(0, marker);
                    if (FlightSessionSummary.SafeId(id)) ids.Add(id); else incomplete = true;
                }
                foreach (string sessionId in ids)
                {
                    var finalState = Read(FinalPath(directory, sessionId), profileId, sessionId, true, out _);
                    if (finalState == ReadState.Valid)
                    {
                        CleanupDrafts(directory, sessionId);
                        continue;
                    }
                    if (finalState != ReadState.Missing) { incomplete = true; continue; }
                    var candidate = LatestDraft(directory, profileId, sessionId,
                        out bool unsupported, out bool unrecoverable);
                    if (candidate == null || unsupported || unrecoverable) { incomplete = true; continue; }
                    var summary = Clone(candidate.Envelope.Summary);
                    summary.Finalized = true;
                    summary.EndedUtc = RecoveryEndTime(directory, sessionId, candidate,
                        summary, endedAtUtc).ToString("o");
                    summary.EndReason = "interrupted";
                    if (Finalize(summary)) recovered++;
                    else
                    {
                        incomplete = true;
                        if (!string.IsNullOrEmpty(LastError)) operationError = LastError;
                    }
                }
            }
            catch (Exception error)
            {
                Fail("Could not recover interrupted sessions: " + error.Message);
                return recovered;
            }
            LastError = operationError;
            if (incomplete && string.IsNullOrEmpty(LastError))
                LastError = "Some interrupted session drafts could not be recovered and were preserved.";
            return recovered;
        }

        private static DateTime RecoveryEndTime(string directory,string sessionId,
            DraftCandidate candidate,FlightSessionSummary summary,DateTime upperBoundUtc)
        {
            // The draft file time is the last persisted observation we actually know.
            // Using the later app relaunch time would falsely turn an overnight crash
            // into an overnight play session in chronological history.
            try
            {
                DateTime writtenUtc=File.GetLastWriteTimeUtc(DraftPath(directory,sessionId,candidate.Slot));
                if(DateTimeOffset.TryParse(summary.Context.StartedUtc,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AllowWhiteSpaces,out var started)
                    &&writtenUtc>=started.UtcDateTime&&writtenUtc<=upperBoundUtc)return writtenUtc;
            }
            catch(Exception) { }
            return upperBoundUtc;
        }

        private DraftCandidate LatestDraft(string directory, string profileId, string sessionId,
            out bool unsupported, out bool unrecoverable)
        {
            unsupported = false;
            unrecoverable = false;
            DraftCandidate best = null;
            bool corrupt = false;
            for (int slot = 0; slot < 2; slot++)
            {
                var state = Read(DraftPath(directory, sessionId, slot), profileId, sessionId, false,
                    out var envelope);
                if (state == ReadState.Unsupported) unsupported = true;
                else if (state == ReadState.Corrupt) corrupt = true;
                else if (state == ReadState.Valid && (best == null || envelope.Generation > best.Envelope.Generation))
                    best = new DraftCandidate { Slot = slot, Envelope = envelope };
            }
            unrecoverable = corrupt && best == null;
            return best;
        }

        private ReadState Read(string path, string profileId, string sessionId, bool requireFinal,
            out Envelope envelope)
        {
            envelope = null;
            if (!File.Exists(path)) return ReadState.Missing;
            try
            {
                envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path, Encoding.UTF8));
                if (envelope == null) return ReadState.Corrupt;
                if (envelope.StorageVersion != CurrentStorageVersion) return ReadState.Unsupported;
                // The envelope and summary evolve independently. Never let an older
                // executable treat a readable future summary as corrupt: rotating draft
                // writes and interrupted-session cleanup are both allowed to replace
                // corrupt generations, while unsupported generations must be preserved.
                if (envelope.Summary != null
                    && envelope.Summary.Version != FlightSessionSummary.CurrentVersion)
                    return ReadState.Unsupported;
                if (envelope.Generation <= 0 || envelope.Summary == null || !envelope.Summary.IsValid()
                    || envelope.Summary.Finalized != requireFinal
                    || !StringComparer.Ordinal.Equals(envelope.Summary.Context.ProfileId, profileId)
                    || !StringComparer.Ordinal.Equals(envelope.Summary.Context.SessionId, sessionId))
                    return ReadState.Corrupt;
                return ReadState.Valid;
            }
            catch
            {
                envelope = null;
                return ReadState.Corrupt;
            }
        }

        private bool WriteReplacing(string path, Envelope envelope, bool durable)
        {
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                WriteFlushed(temporary, JsonUtility.ToJson(envelope), durable);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporary, path);
                return true;
            }
            catch (Exception error)
            {
                return Fail("Could not save the session draft: " + error.Message);
            }
            finally
            {
                TryDelete(temporary);
            }
        }

        private bool WriteOnce(string path, Envelope envelope)
        {
            string temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                WriteFlushed(temporary, JsonUtility.ToJson(envelope), true);
                if (File.Exists(path))
                {
                    var state = Read(path, envelope.Summary.Context.ProfileId,
                        envelope.Summary.Context.SessionId, true, out _);
                    return state == ReadState.Valid || Fail("A conflicting final session file was preserved.");
                }
                File.Move(temporary, path);
                return true;
            }
            catch (Exception error)
            {
                return Fail("Could not finalize the session: " + error.Message);
            }
            finally
            {
                TryDelete(temporary);
            }
        }

        private static void WriteFlushed(string path, string json, bool durable)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(durable);
            }
        }

        private bool Validate(FlightSessionSummary summary, bool requireFinal)
        {
            if (summary == null || !summary.IsValid() || summary.Finalized != requireFinal)
                return Fail(requireFinal ? "A valid finalized session is required." : "A valid draft session is required.");
            return true;
        }

        private bool ValidIdentity(string profileId, string sessionId)
        {
            if (FlightSessionSummary.SafeId(profileId) && FlightSessionSummary.SafeId(sessionId)) return true;
            return Fail("The profile or session id is invalid.");
        }

        private bool Fail(string message)
        {
            LastError = message;
            return false;
        }

        private string SessionDirectory(string profileId) => Path.Combine(rootDirectory, "profiles", profileId, "sessions");
        private static string DraftPath(string directory, string sessionId, int slot) =>
            Path.Combine(directory, sessionId + ".draft." + slot + ".json");
        private static string FinalPath(string directory, string sessionId) =>
            Path.Combine(directory, sessionId + ".final.json");

        private static void CleanupDrafts(string directory, string sessionId)
        {
            TryDelete(DraftPath(directory, sessionId, 0));
            TryDelete(DraftPath(directory, sessionId, 1));
        }

        private static FlightSessionSummary Clone(FlightSessionSummary summary) =>
            JsonUtility.FromJson<FlightSessionSummary>(JsonUtility.ToJson(summary));

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
