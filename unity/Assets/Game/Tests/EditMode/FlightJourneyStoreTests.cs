using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VoarVR.Gameplay;
using VoarVR.World;

namespace VoarVR.Tests
{
    public class FlightJourneyStoreTests
    {
        private sealed class MemoryStorage : IFlightSaveStorage
        {
            public readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
            public readonly Dictionary<string, int> Integers = new Dictionary<string, int>();
            public int Flushes;
            public bool ThrowOnWrite;
            public bool HasKey(string key) => Strings.ContainsKey(key) || Integers.ContainsKey(key);
            public string GetString(string key, string fallback = "") => Strings.TryGetValue(key, out var value) ? value : fallback;
            public int GetInt(string key, int fallback = 0) => Integers.TryGetValue(key, out var value) ? value : fallback;
            public void SetString(string key, string value)
            { if (ThrowOnWrite) throw new InvalidOperationException("test storage is unavailable"); Strings[key] = value; }
            public void Save() { Flushes++; }
        }
        private static ChallengeCheckpoint SeedCheckpoint()
        {
            var challenge = new FlightChallenge(FlightActivity.RouteHome);
            challenge.Step(new ChallengeObservation(new LogicalPosition(100, 30, 140), Vector3.forward * 8, 4, 0, .02f, false, 0, 0));
            for (int y = 31; y <= 240; y++)
                challenge.Step(new ChallengeObservation(new LogicalPosition(100, y, 140), Vector3.forward * 8, 4, 0, .02f, false, 0, 0));
            var checkpoint = challenge.Capture();
            checkpoint.SetSafePerch(new LogicalPosition(12, 4, 18));
            return checkpoint;
        }
        [Test] public void LegacyMigrationPreservesOriginalScoresAndForagingWithoutReclassification()
        {
            var storage = new MemoryStorage();
            string legacy = JsonUtility.ToJson(new FlightProgress { SkywardBest = 877, RidgeBest = 920, TrainingBest = 950, TrickBest = 225 });
            storage.Strings[FlightJourneyStore.LegacyKey] = legacy;
            storage.Integers[FlightJourneyStore.ForagingKey] = 740;
            var store = new FlightJourneyStore(storage); var data = store.Load();
            Assert.That(data.LegacyBests.SkywardBest, Is.EqualTo(877));
            Assert.That(data.LegacyBests.RidgeBest, Is.EqualTo(920));
            Assert.That(data.LegacyBests.TrainingBest, Is.EqualTo(950));
            Assert.That(data.LegacyBests.TrickBest, Is.EqualTo(225));
            Assert.That(data.ForagingBest, Is.EqualTo(740));
            Assert.That(data.TrainingEfficiencyBest, Is.Zero, "New rest-aware scoring starts a separate record");
            Assert.That(data.RidgeUnlocked, Is.True);
            data.PutCheckpoint(SeedCheckpoint());
            Assert.That(store.Save(), Is.True);
            Assert.That(storage.Strings[FlightJourneyStore.LegacyKey], Is.EqualTo(legacy));
            Assert.That(storage.Integers[FlightJourneyStore.ForagingKey], Is.EqualTo(740));
            var restored = new FlightJourneyStore(storage).Load();
            Assert.That(restored.LegacyBests.TrainingBest, Is.EqualTo(950));
            Assert.That(restored.GetCheckpoint(FlightActivity.RouteHome).SafePerch.X, Is.EqualTo(12));
            Assert.That(restored.GetCheckpoint(FlightActivity.RouteHome).Stage, Is.EqualTo(2));
        }
        [Test] public void CheckpointsForDifferentActivitiesDoNotEraseEachOther()
        {
            var storage = new MemoryStorage(); var store = new FlightJourneyStore(storage); var data = store.Load();
            data.PutCheckpoint(SeedCheckpoint());
            data.PutCheckpoint(new FlightChallenge(FlightActivity.Training).Capture());
            Assert.That(store.Save(), Is.True);
            var restored = new FlightJourneyStore(storage).Load();
            Assert.That(restored.HasResume(FlightActivity.RouteHome), Is.True);
            Assert.That(restored.HasResume(FlightActivity.Training), Is.True);
            Assert.That(restored.GetCheckpoint(FlightActivity.RouteHome).Stage, Is.EqualTo(2));
        }
        [Test] public void UnknownSchemaPreservesPrimaryAndBackupAndRejectsWrites()
        {
            var storage = new MemoryStorage();
            const string unknown = "{\"Version\":19,\"Story\":\"future reward\"}";
            storage.Strings[FlightJourneyStore.SaveKey] = unknown;
            storage.Strings[FlightJourneyStore.BackupKey] = "untouched backup";
            var store = new FlightJourneyStore(storage); var data = store.Load();
            Assert.That(data.CanWrite, Is.False); Assert.That(data.SaveNotice, Is.Not.Empty);
            data.PutCheckpoint(SeedCheckpoint());
            Assert.That(store.Save(), Is.False);
            Assert.That(storage.Strings[FlightJourneyStore.SaveKey], Is.EqualTo(unknown));
            Assert.That(storage.Strings[FlightJourneyStore.BackupKey], Is.EqualTo("untouched backup"));
            Assert.That(storage.Flushes, Is.Zero);
        }
        [Test] public void UnknownContentPreservesRecoverableData()
        {
            var storage = new MemoryStorage(); var save = new FlightJourneySave();
            var checkpoint = SeedCheckpoint(); checkpoint.ContentId = "route-home.v45";
            save.Checkpoints = new[] { checkpoint };
            string raw = JsonUtility.ToJson(save); storage.Strings[FlightJourneyStore.SaveKey] = raw;
            var store = new FlightJourneyStore(storage);
            Assert.That(store.Load().CanWrite, Is.False);
            Assert.That(store.Save(), Is.False);
            Assert.That(storage.Strings[FlightJourneyStore.SaveKey], Is.EqualTo(raw));
        }
        [Test] public void UnknownRecoveryVersionCannotBeOverwrittenWhenPrimaryIsMissing()
        {
            var storage = new MemoryStorage(); storage.Strings[FlightJourneyStore.BackupKey] = "{\"Version\":40}";
            var store = new FlightJourneyStore(storage);
            Assert.That(store.Load().CanWrite, Is.False);
            Assert.That(store.Save(), Is.False);
            Assert.That(storage.HasKey(FlightJourneyStore.SaveKey), Is.False);
            Assert.That(storage.Strings[FlightJourneyStore.BackupKey], Is.EqualTo("{\"Version\":40}"));
        }
        [Test] public void DamagedPrimaryRecoversLastValidCheckpointAndArchivesExactBytes()
        {
            var storage = new MemoryStorage(); var first = new FlightJourneyStore(storage); var data = first.Load();
            data.PutCheckpoint(SeedCheckpoint()); Assert.That(first.Save(), Is.True);
            data.TrainingEfficiencyBest = 800; Assert.That(first.Save(), Is.True);
            const string damaged = "{broken save original bytes";
            storage.Strings[FlightJourneyStore.SaveKey] = damaged;
            var recovery = new FlightJourneyStore(storage); var restored = recovery.Load();
            Assert.That(restored.GetCheckpoint(FlightActivity.RouteHome).Stage, Is.EqualTo(2));
            Assert.That(restored.SaveNotice, Does.Contain("Recovered"));
            Assert.That(storage.Strings[FlightJourneyStore.SaveKey], Is.EqualTo(damaged), "Load is read-only");
            Assert.That(recovery.Save(), Is.True);
            Assert.That(storage.Strings[FlightJourneyStore.RecoveryKey], Is.EqualTo(damaged));
            Assert.That(new FlightJourneyStore(storage).Load().GetCheckpoint(FlightActivity.RouteHome).Stage, Is.EqualTo(2));
        }
        [Test] public void FullyCorruptSaveAndLegacyArePreservedBeforeFreshCheckpoint()
        {
            var storage = new MemoryStorage();
            storage.Strings[FlightJourneyStore.SaveKey] = "broken new";
            storage.Strings[FlightJourneyStore.LegacyKey] = "broken old";
            var store = new FlightJourneyStore(storage); var data = store.Load();
            Assert.That(data.CanWrite, Is.True); Assert.That(data.SaveNotice, Is.Not.Empty);
            data.PutCheckpoint(SeedCheckpoint()); Assert.That(store.Save(), Is.True);
            Assert.That(storage.Strings[FlightJourneyStore.RecoveryKey], Is.EqualTo("broken new"));
            Assert.That(storage.Strings[FlightJourneyStore.LegacyKey], Is.EqualTo("broken old"));
        }
        [Test] public void WriteFailureReportsFailureAndKeepsLastCheckpoint()
        {
            var storage = new MemoryStorage(); var store = new FlightJourneyStore(storage); var data = store.Load();
            data.PutCheckpoint(SeedCheckpoint()); Assert.That(store.Save(), Is.True);
            string saved = storage.Strings[FlightJourneyStore.SaveKey];
            storage.ThrowOnWrite = true; data.TrainingEfficiencyBest = 999;
            Assert.That(store.Save(), Is.False);
            Assert.That(data.SaveNotice, Does.Contain("could not be saved"));
            Assert.That(storage.Strings[FlightJourneyStore.SaveKey], Is.EqualTo(saved));
        }
        [Test] public void NewerSaveAppearingAfterLoadIsNeverReplaced()
        {
            var storage = new MemoryStorage(); var store = new FlightJourneyStore(storage); store.Load();
            storage.Strings[FlightJourneyStore.SaveKey] = "{\"Version\":99}";
            Assert.That(store.Save(), Is.False);
            Assert.That(storage.Strings[FlightJourneyStore.SaveKey], Is.EqualTo("{\"Version\":99}"));
        }
        [Test] public void StoryRewardIsIdempotentAndFreshReplayRetainsRestoration()
        {
            var checkpoint = SeedCheckpoint(); checkpoint.Stage = 4; checkpoint.SeedCollected = true;
            var challenge = new FlightChallenge(FlightActivity.RouteHome);
            Assert.That(challenge.Restore(checkpoint), Is.True);
            challenge.Step(new ChallengeObservation(new LogicalPosition(420, 272, 620), Vector3.zero, 0, 0, .02f, false, 0, 0));
            challenge.Step(new ChallengeObservation(new LogicalPosition(420, 270, 620), Vector3.zero, 0, 0, .02f, true, 0, 0));
            Assert.That(challenge.Status, Is.EqualTo(ChallengeStatus.Completed));
            var storage = new MemoryStorage(); var store = new FlightJourneyStore(storage); var data = store.Load();
            data.ApplyCompletion(challenge); data.ApplyCompletion(challenge);
            data.PutCheckpoint(challenge.Capture()); Assert.That(store.Save(), Is.True);
            data.PutCheckpoint(new FlightChallenge(FlightActivity.RouteHome).Capture()); Assert.That(store.Save(), Is.True);
            var restored = new FlightJourneyStore(storage).Load();
            Assert.That(restored.GardenRestored, Is.True); Assert.That(restored.RouteHomeCompleted, Is.True);
            Assert.That(restored.HasResume(FlightActivity.RouteHome), Is.True);
            Assert.That(restored.GetCheckpoint(FlightActivity.RouteHome).Stage, Is.Zero);
        }
        [Test] public void DirectorResumeIsExplicitConsumesRequestAndRetainsStageAcrossLeave()
        {
            var storage = new MemoryStorage(); var store = new FlightJourneyStore(storage);
            store.Load().PutCheckpoint(SeedCheckpoint()); store.Save();
            var selected = ActivitySelection.Chosen; var resume = ActivitySelection.ResumeRequested;
            var gameObject = new GameObject("Isolated journey test");
            try
            {
                ActivitySelection.Chosen = FlightActivity.RouteHome; ActivitySelection.ResumeRequested = true;
                var director = gameObject.AddComponent<ExpeditionDirector>(); director.Configure(null, null, storage);
                Assert.That(director.Challenge.Stage, Is.EqualTo(2)); Assert.That(director.ResumeCheckpoint, Is.Not.Null);
                Assert.That(ActivitySelection.ResumeRequested, Is.False);
                director.SaveCheckpoint();
                Assert.That(new FlightJourneyStore(storage).Load().GetCheckpoint(FlightActivity.RouteHome).Stage, Is.EqualTo(2));
                director.Configure(null, null, storage);
                Assert.That(director.Challenge.Stage, Is.Zero); Assert.That(director.ResumeCheckpoint, Is.Null);
                Assert.That(director.HasSafePerch, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(gameObject); ActivitySelection.Chosen = selected; ActivitySelection.ResumeRequested = resume; }
        }
        [Test] public void MismatchedWorldCheckpointSurvivesDirectorLifecycle()
        {
            var storage = new MemoryStorage(); var store = new FlightJourneyStore(storage);
            var checkpoint = SeedCheckpoint(); checkpoint.WorldSeed = 555;
            store.Load().PutCheckpoint(checkpoint); store.Save();
            string raw = storage.Strings[FlightJourneyStore.SaveKey];
            var selected = ActivitySelection.Chosen; var resume = ActivitySelection.ResumeRequested;
            var gameObject = new GameObject("Mismatched journey test");
            try
            {
                ActivitySelection.Chosen = FlightActivity.RouteHome; ActivitySelection.ResumeRequested = true;
                var director = gameObject.AddComponent<ExpeditionDirector>(); director.Configure(null, null, storage);
                Assert.That(director.Journey.CanWrite, Is.False); Assert.That(director.SaveCheckpoint(), Is.False);
                Assert.That(director.Journey.GetCheckpoint(FlightActivity.RouteHome).WorldSeed, Is.EqualTo(555));
            }
            finally { UnityEngine.Object.DestroyImmediate(gameObject); ActivitySelection.Chosen = selected; ActivitySelection.ResumeRequested = resume; }
            Assert.That(storage.Strings[FlightJourneyStore.SaveKey], Is.EqualTo(raw));
        }
    }
}
