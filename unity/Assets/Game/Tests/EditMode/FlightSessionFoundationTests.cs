using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using VoarVR.Gameplay;

namespace VoarVR.Tests
{
    public class FlightSessionFoundationTests
    {
        private string temporaryRoot;

        [SetUp]
        public void SetUp()
        {
            temporaryRoot = Path.Combine(Path.GetTempPath(), "voarvr-session-tests-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
        }

        [Test]
        public void TrackerPartitionsTimeAndKeepsCoverageReasonsSeparate()
        {
            var tracker = NewTracker();
            var up = At(0, 0, 0, FlightSessionTimeCategory.ActiveHand);
            up.LeftHandVelocity = up.RightHandVelocity = Vector3.up;
            Assert.That(tracker.Observe(up), Is.True);
            var down = At(0, 0, 1, FlightSessionTimeCategory.ActiveHand);
            down.LeftHandVelocity = down.RightHandVelocity = Vector3.down;
            tracker.Observe(down);
            tracker.Observe(At(0, 0, 2, FlightSessionTimeCategory.QuietAirborne));

            var perched = At(0, 0, 2, FlightSessionTimeCategory.SupportedPerch);
            perched.Movement = FlightSessionMovement.None;
            tracker.Observe(perched);
            var paused = At(0, 0, 2, FlightSessionTimeCategory.Paused);
            paused.Movement = FlightSessionMovement.None;
            tracker.Observe(paused);
            var trackingGap = At(0, 0, 2, FlightSessionTimeCategory.ActiveHand);
            trackingGap.TrackingAvailable = false;
            tracker.Observe(trackingGap);
            var streamingGap = At(0, 0, 2, FlightSessionTimeCategory.QuietAirborne);
            streamingGap.StreamingReady = false;
            tracker.Observe(streamingGap);
            var invalid = At(0, 0, 2, FlightSessionTimeCategory.ActiveHand);
            invalid.SampleValid = false;
            tracker.Observe(invalid);

            var metrics = tracker.Snapshot().Metrics;
            Assert.That(metrics.ObservedSeconds, Is.EqualTo(.4d).Within(.00001));
            Assert.That(metrics.ActiveHandSeconds, Is.EqualTo(.1d).Within(.00001));
            Assert.That(metrics.QuietAirborneSeconds, Is.EqualTo(.05d).Within(.00001));
            Assert.That(metrics.SupportedPerchSeconds, Is.EqualTo(.05d).Within(.00001));
            Assert.That(metrics.PausedSeconds, Is.EqualTo(.05d).Within(.00001));
            Assert.That(metrics.ExcludedSeconds, Is.EqualTo(.15d).Within(.00001));
            Assert.That(metrics.TrackingGapSeconds, Is.EqualTo(.05d).Within(.00001));
            Assert.That(metrics.StreamingBlockedSeconds, Is.EqualTo(.05d).Within(.00001));
            Assert.That(metrics.InvalidSampleSeconds, Is.EqualTo(.05d).Within(.00001));
            Assert.That(metrics.EstimatedWingbeats, Is.EqualTo(1));
            Assert.That(metrics.IsValid(), Is.True);
            Assert.That(FlightSessionTracker.ClassifyAirborneHands(Vector3.zero, Vector3.zero),
                Is.EqualTo(FlightSessionTimeCategory.QuietAirborne));
            Assert.That(FlightSessionTracker.ClassifyAirborneHands(Vector3.right, Vector3.right),
                Is.EqualTo(FlightSessionTimeCategory.ActiveHand));
        }

        [Test]
        public void TrackerCountsARealFrameHitchWithoutMultiplyingItsTravel()
        {
            var tracker=NewTracker();
            tracker.Observe(At(0,0,0));
            var hitch=At(3,4,0);hitch.DeltaTime=.35f;
            Assert.That(tracker.Observe(hitch),Is.True);
            var metrics=tracker.Snapshot().Metrics;
            Assert.That(metrics.ObservedSeconds,Is.EqualTo(.4).Within(.00001));
            Assert.That(metrics.FlightDistanceMeters,Is.EqualTo(5).Within(.00001));
        }

        [Test]
        public void TrackerSeparatesFlightAndWalkingAndAccumulatesGrossVerticalTravel()
        {
            var tracker = NewTracker();
            var first = At(0, 0, 0);
            first.Velocity = new Vector3(3, 4, 0);
            first.WindVelocity = Vector3.right;
            tracker.Observe(first);
            var climb = At(3, 4, 0);
            climb.Velocity = Vector3.forward * 10;
            tracker.Observe(climb);
            var descend = At(6, 1, 0);
            descend.Velocity = Vector3.forward * 6;
            tracker.Observe(descend);
            var walkingAnchor = At(6, 1, 0, FlightSessionTimeCategory.SupportedPerch);
            walkingAnchor.Movement = FlightSessionMovement.Walking;
            walkingAnchor.MovementActive = true;
            tracker.Observe(walkingAnchor);
            var walk = At(10, 1, 0, FlightSessionTimeCategory.SupportedPerch);
            walk.Movement = FlightSessionMovement.Walking;
            walk.MovementActive = true;
            walk.Velocity = Vector3.right * 12;
            tracker.Observe(walk);

            var metrics = tracker.Snapshot().Metrics;
            Assert.That(metrics.FlightHorizontalMeters, Is.EqualTo(6d).Within(.00001));
            Assert.That(metrics.FlightDistanceMeters, Is.EqualTo(5d + Math.Sqrt(18d)).Within(.00001));
            Assert.That(metrics.WalkingHorizontalMeters, Is.EqualTo(4d).Within(.00001));
            Assert.That(metrics.WalkingDistanceMeters, Is.EqualTo(4d).Within(.00001));
            Assert.That(metrics.WalkingActiveSeconds, Is.EqualTo(.1d).Within(.00001));
            Assert.That(metrics.GrossAscentMeters, Is.EqualTo(4d).Within(.00001));
            Assert.That(metrics.GrossDescentMeters, Is.EqualTo(3d).Within(.00001));
            Assert.That(metrics.MaxGroundSpeedMps, Is.EqualTo(12d).Within(.00001));
            Assert.That(metrics.MaxAirSpeedMps, Is.EqualTo(10d).Within(.00001));
        }

        [Test]
        public void ResetBreaksDistanceWhileLogicalWorldRebaseStaysContinuous()
        {
            var tracker = NewTracker();
            tracker.Observe(At(100, 10, 100));
            tracker.Observe(At(101, 10, 100));
            tracker.MarkWorldRebaseContinuity();
            tracker.Observe(At(102, 10, 100));
            tracker.MarkResetDiscontinuity();
            tracker.Observe(At(1000, 100, 1000));
            tracker.Observe(At(1002, 100, 1000));

            var metrics = tracker.Snapshot().Metrics;
            Assert.That(metrics.FlightHorizontalMeters, Is.EqualTo(4d).Within(.00001));
            Assert.That(metrics.FlightDistanceMeters, Is.EqualTo(4d).Within(.00001));
            Assert.That(metrics.GrossAscentMeters, Is.Zero);
        }

        [Test]
        public void PreparedFinalDoesNotFreezeLiveMetricsBeforePersistenceCommits()
        {
            var tracker=NewTracker();
            tracker.Observe(At(0,0,0));
            var prepared=tracker.PrepareFinish("player-ended",
                new DateTime(2026,9,12,10,0,0,DateTimeKind.Utc));
            Assert.That(prepared.Finalized,Is.True);
            Assert.That(tracker.IsFinished,Is.False);
            Assert.That(tracker.Observe(At(0,0,2)),Is.True,
                "A failed storage write must leave the still-live session observable.");
            Assert.That(tracker.Snapshot().Metrics.FlightDistanceMeters,Is.EqualTo(2).Within(.001));
            var retry=tracker.PrepareFinish("player-ended",
                new DateTime(2026,9,12,10,1,0,DateTimeKind.Utc));
            Assert.That(retry.Metrics.FlightDistanceMeters,Is.EqualTo(2).Within(.001));
            Assert.That(tracker.CommitFinish(retry),Is.True);
            Assert.That(tracker.IsFinished,Is.True);
            Assert.That(tracker.Observe(At(0,0,3)),Is.False);
        }

        [Test]
        public void TrackerTalliesEventsAndFinalizesOnlyOnce()
        {
            var tracker = NewTracker();
            Assert.That(tracker.RecordCollectible("moth", 10, 2), Is.True);
            Assert.That(tracker.RecordCollectible("gold-moth", 100), Is.True);
            Assert.That(tracker.RecordTrick(2), Is.True);
            Assert.That(tracker.RecordCollision(), Is.True);
            Assert.That(tracker.RecordLanding(), Is.True);
            Assert.That(tracker.RecordTakeoff(2), Is.True);
            var ended = new DateTime(2026, 9, 12, 11, 0, 0, DateTimeKind.Utc);
            var summary = tracker.Finish("player-ended", ended);
            var repeated = tracker.Finish("ignored-retry", ended.AddMinutes(5));

            Assert.That(summary.IsValid(), Is.True);
            Assert.That(summary.Metrics.CatchCount, Is.EqualTo(3));
            Assert.That(summary.Metrics.CollectibleValue, Is.EqualTo(120));
            Assert.That(summary.Metrics.TrickCount, Is.EqualTo(2));
            Assert.That(summary.Metrics.CollisionCount, Is.EqualTo(1));
            Assert.That(summary.Metrics.LandingCount, Is.EqualTo(1));
            Assert.That(summary.Metrics.TakeoffCount, Is.EqualTo(2));
            Assert.That(summary.Collectibles[0].CollectibleId, Is.EqualTo("gold-moth"));
            Assert.That(summary.Collectibles[1].CollectibleId, Is.EqualTo("moth"));
            Assert.That(repeated.EndReason, Is.EqualTo("player-ended"));
            Assert.That(repeated.EndedUtc, Is.EqualTo(summary.EndedUtc));
            Assert.That(tracker.Observe(At(1, 1, 1)), Is.False);
            Assert.That(tracker.RecordCollision(), Is.False);
        }

        [Test]
        public void EditorDefaultStoresKeepLifecycleWritesInTheirIsolatedDirectories()
        {
            var priorProfiles=PlayerProfileCatalog.EditorDirectoryOverride;
            var priorHistory=FlightSessionHistoryStore.EditorDirectoryOverride;
            string profilesRoot=Path.Combine(temporaryRoot,"isolated-profiles");
            string historyRoot=Path.Combine(temporaryRoot,"isolated-history");
            PlayerProfileCatalog profiles;
            FlightSessionHistoryStore history;
            try
            {
                PlayerProfileCatalog.EditorDirectoryOverride=()=>profilesRoot;
                FlightSessionHistoryStore.EditorDirectoryOverride=()=>historyRoot;
                profiles=new PlayerProfileCatalog();history=new FlightSessionHistoryStore();
                Assert.That(profiles.RootDirectory,Is.EqualTo(profilesRoot));
                Assert.That(history.RootDirectory,Is.EqualTo(historyRoot));
                Assert.That(profiles.CreateProfile("Review pilot",out _),Is.True,profiles.Notice);
                Assert.That(history.SaveDraft(NewTracker().Snapshot()),Is.True,history.LastError);

                string explicitProfiles=Path.Combine(temporaryRoot,"explicit-profiles");
                string explicitHistory=Path.Combine(temporaryRoot,"explicit-history");
                Assert.That(new PlayerProfileCatalog(explicitProfiles).Profiles.Length,Is.EqualTo(1),
                    "An explicitly supplied store must not read the review catalog.");
                Assert.That(new FlightSessionHistoryStore(explicitHistory)
                    .TryLoadLatestDraft("default","session-1",out _),Is.False);
                PlayerProfileCatalog.EditorDirectoryOverride=()=>null;
                FlightSessionHistoryStore.EditorDirectoryOverride=()=>" ";
                Assert.Throws<InvalidOperationException>(()=>new PlayerProfileCatalog());
                Assert.Throws<InvalidOperationException>(()=>new FlightSessionHistoryStore());
            }
            finally
            {
                PlayerProfileCatalog.EditorDirectoryOverride=priorProfiles;
                FlightSessionHistoryStore.EditorDirectoryOverride=priorHistory;
            }
            // Existing save owners retain their directory even after factories are restored,
            // including destruction/focus callbacks that flush a previously created store.
            var final=NewTracker().Finish("player-ended",DateTime.UtcNow);
            Assert.That(history.Finalize(final),Is.True,history.LastError);
            Assert.That(new FlightSessionHistoryStore(historyRoot)
                .TryLoadFinal("default","session-1",out _),Is.True);
            Assert.That(new PlayerProfileCatalog(profilesRoot).Profiles.Length,Is.EqualTo(2));
        }

        [Test]
        public void HistoryRecoversOlderDraftAndWritesOneIdempotentFinal()
        {
            string historyRoot = Path.Combine(temporaryRoot, "history");
            var store = new FlightSessionHistoryStore(historyRoot);
            var tracker = NewTracker();
            Assert.That(store.SaveDraft(tracker.Snapshot()), Is.True, store.LastError);
            tracker.Observe(At(0, 0, 0));
            Assert.That(store.SaveDraft(tracker.Snapshot()), Is.True, store.LastError);
            string sessions = Path.Combine(historyRoot, "profiles", "default", "sessions");
            File.WriteAllText(Path.Combine(sessions, "session-1.draft.1.json"), "{interrupted");

            Assert.That(store.TryLoadLatestDraft("default", "session-1", out var recovered), Is.True,
                store.LastError);
            Assert.That(recovered.Metrics.ObservedSeconds, Is.Zero);
            var final = tracker.Finish("player-ended",
                new DateTime(2026, 9, 12, 11, 0, 0, DateTimeKind.Utc));
            Assert.That(store.Finalize(final), Is.True, store.LastError);
            Assert.That(store.Finalize(final), Is.True, store.LastError);
            Assert.That(Directory.GetFiles(sessions, "*.final.json").Length, Is.EqualTo(1));
            Assert.That(Directory.GetFiles(sessions, "*.draft.*.json").Length, Is.Zero);
            Assert.That(Directory.GetFiles(sessions, "*.tmp-*").Length, Is.Zero);
            Assert.That(store.TryLoadFinal("default", "session-1", out var loaded), Is.True, store.LastError);
            Assert.That(loaded.Finalized, Is.True);
            Assert.That(store.LoadFinals("default").Length, Is.EqualTo(1));
        }

        [Test]
        public void HistoryFinalizesReadableCrashDraftAsInterruptedOnNextVisit()
        {
            string historyRoot = Path.Combine(temporaryRoot, "history");
            var writer = new FlightSessionHistoryStore(historyRoot);
            var tracker = NewTracker();
            tracker.Observe(At(0, 0, 0));
            tracker.Observe(At(0, 2, 4));
            Assert.That(writer.SaveDraft(tracker.Snapshot()), Is.True, writer.LastError);
            string sessions = Path.Combine(historyRoot, "profiles", "default", "sessions");
            var lastPersistedAt = new DateTime(2026, 9, 12, 11, 42, 0, DateTimeKind.Utc);
            foreach(string draft in Directory.GetFiles(sessions, "*.draft.*.json"))
                File.SetLastWriteTimeUtc(draft,lastPersistedAt);

            var reader = new FlightSessionHistoryStore(historyRoot);
            var recoveredAt = new DateTime(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
            Assert.That(reader.RecoverInterruptedDrafts("default", recoveredAt), Is.EqualTo(1), reader.LastError);
            Assert.That(reader.TryLoadFinal("default", "session-1", out var recovered), Is.True, reader.LastError);
            Assert.That(recovered.Finalized, Is.True);
            Assert.That(recovered.EndReason, Is.EqualTo("interrupted"));
            Assert.That(recovered.EndedUtc,Is.EqualTo(lastPersistedAt.ToString("o")),
                "Recovered history should stop at the last persisted play sample, not app relaunch.");
            Assert.That(recovered.Metrics.FlightDistanceMeters, Is.EqualTo(Math.Sqrt(20d)).Within(.00001));
            Assert.That(Directory.GetFiles(sessions, "*.draft.*.json").Length, Is.Zero);
        }

        [Test]
        public void HistoryReturnsReadableFinalsButReportsPartialTotals()
        {
            string historyRoot = Path.Combine(temporaryRoot, "history");
            var store = new FlightSessionHistoryStore(historyRoot);
            var final = NewTracker().Finish("player-ended",
                new DateTime(2026, 9, 12, 11, 0, 0, DateTimeKind.Utc));
            Assert.That(store.Finalize(final), Is.True, store.LastError);
            string sessions = Path.Combine(historyRoot, "profiles", "default", "sessions");
            File.WriteAllText(Path.Combine(sessions, "unreadable.final.json"), "{interrupted");
            File.WriteAllText(Path.Combine(sessions, "future.final.json"), "{\"StorageVersion\":99}");

            var loaded = store.LoadFinals("default");

            Assert.That(loaded.Length, Is.EqualTo(1));
            Assert.That(loaded[0].Context.SessionId, Is.EqualTo("session-1"));
            Assert.That(store.LastError, Does.Contain("partial"));
            Assert.That(File.Exists(Path.Combine(sessions, "unreadable.final.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(sessions, "future.final.json")), Is.True);
        }

        [Test]
        public void HistoryPreservesUnknownDraftVersionAndRejectsTraversalIds()
        {
            string historyRoot = Path.Combine(temporaryRoot, "history");
            string sessions = Path.Combine(historyRoot, "profiles", "default", "sessions");
            Directory.CreateDirectory(sessions);
            string unknownPath = Path.Combine(sessions, "session-1.draft.0.json");
            const string unknown = "{\"StorageVersion\":99,\"Generation\":8}";
            File.WriteAllText(unknownPath, unknown);
            var store = new FlightSessionHistoryStore(historyRoot);

            Assert.That(store.SaveDraft(NewTracker().Snapshot()), Is.False);
            Assert.That(store.LastError, Does.Contain("newer"));
            Assert.That(File.ReadAllText(unknownPath), Is.EqualTo(unknown));
            Assert.That(store.TryLoadFinal("../outside", "session-1", out _), Is.False);
            Assert.That(Directory.Exists(Path.Combine(historyRoot, "outside")), Is.False);
        }

        [Test]
        public void HistoryPreservesFutureSummaryBesideOlderReadableDraft()
        {
            string historyRoot=Path.Combine(temporaryRoot,"history");
            var writer=new FlightSessionHistoryStore(historyRoot);
            var snapshot=NewTracker().Snapshot();
            Assert.That(writer.SaveDraft(snapshot),Is.True,writer.LastError);
            string sessions=Path.Combine(historyRoot,"profiles","default","sessions");
            string olderPath=Path.Combine(sessions,"session-1.draft.0.json");
            string futurePath=Path.Combine(sessions,"session-1.draft.1.json");
            string older=File.ReadAllText(olderPath);
            string future=older.Replace("\"Generation\":1","\"Generation\":8")
                .Replace("\"Summary\":{\"Version\":1","\"Summary\":{\"Version\":99");
            Assert.That(future,Is.Not.EqualTo(older));
            File.WriteAllText(futurePath,future);

            var reader=new FlightSessionHistoryStore(historyRoot);
            Assert.That(reader.TryLoadLatestDraft("default","session-1",out _),Is.False);
            Assert.That(reader.LastError,Does.Contain("unsupported"));
            Assert.That(reader.RecoverInterruptedDrafts("default",
                new DateTime(2026,9,12,12,0,0,DateTimeKind.Utc)),Is.Zero);
            Assert.That(File.Exists(Path.Combine(sessions,"session-1.final.json")),Is.False,
                "Recovery cannot finalize stale metrics while a newer summary generation exists.");
            Assert.That(reader.SaveDraft(snapshot),Is.False);
            Assert.That(File.ReadAllText(olderPath),Is.EqualTo(older));
            Assert.That(File.ReadAllText(futurePath),Is.EqualTo(future));
        }

        [Test]
        public void ProfileCatalogCreatesStableDefaultAndRecoversItsPreviousGeneration()
        {
            string profileRoot = Path.Combine(temporaryRoot, "profiles");
            DateTime now = new DateTime(2026, 9, 12, 10, 0, 0, DateTimeKind.Utc);
            var catalog = new PlayerProfileCatalog(profileRoot, () => "p-alice", () => now);
            Assert.That(catalog.CanWrite, Is.True, catalog.Notice);
            Assert.That(catalog.ActiveProfileId, Is.EqualTo(PlayerProfileCatalog.BuiltInProfileId));
            Assert.That(catalog.DefaultProfile.DisplayName, Is.EqualTo(PlayerProfileCatalog.BuiltInProfileName));
            Assert.That(catalog.CreateProfile("  Alice  ", out var alice), Is.True, catalog.Notice);
            Assert.That(alice.Id, Is.EqualTo("p-alice"));
            Assert.That(catalog.ActiveProfileId, Is.EqualTo("p-alice"));
            Assert.That(catalog.RenameProfile("p-alice", "Pilot Alice"), Is.True, catalog.Notice);
            File.WriteAllText(Path.Combine(profileRoot, "profiles.0.json"), "{interrupted");

            var recovered = new PlayerProfileCatalog(profileRoot, () => "unused", () => now.AddHours(1));
            Assert.That(recovered.CanWrite, Is.True);
            Assert.That(recovered.Notice, Does.Contain("Recovered"));
            Assert.That(recovered.ActiveProfile.DisplayName, Is.EqualTo("Alice"));
            Assert.That(recovered.DefaultProfileId, Is.EqualTo(PlayerProfileCatalog.BuiltInProfileId));
            Assert.That(recovered.SetDefaultProfile("p-alice"), Is.True, recovered.Notice);
            var reloaded = new PlayerProfileCatalog(profileRoot);
            Assert.That(reloaded.DefaultProfileId, Is.EqualTo("p-alice"));
            Assert.That(reloaded.Profiles.Length, Is.EqualTo(2));
        }

        [Test]
        public void ProfileCatalogPreservesUnknownStorageVersion()
        {
            string profileRoot = Path.Combine(temporaryRoot, "profiles");
            Directory.CreateDirectory(profileRoot);
            string path = Path.Combine(profileRoot, "profiles.0.json");
            const string unknown = "{\"StorageVersion\":42,\"Generation\":3}";
            File.WriteAllText(path, unknown);

            var catalog = new PlayerProfileCatalog(profileRoot);
            Assert.That(catalog.CanWrite, Is.False);
            Assert.That(catalog.Notice, Does.Contain("newer"));
            Assert.That(catalog.CreateProfile("Should Not Write", out _), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo(unknown));
        }

        private static FlightSessionTracker NewTracker()
        {
            return new FlightSessionTracker(new FlightSessionContext
            {
                SessionId = "session-1",
                ProfileId = "default",
                StartedUtc = "2026-09-12T10:00:00.0000000Z",
                BuildId = "test-build",
                CharacterId = "duck",
                ActivityId = "free-flight",
                ContentId = "world-1",
                ContentRevision = 1,
                ScoringRevision = 1
            });
        }

        private static FlightSessionObservation At(double x, double y, double z,
            FlightSessionTimeCategory category = FlightSessionTimeCategory.QuietAirborne)
        {
            var observation = FlightSessionObservation.Create(x, y, z, .05f);
            observation.TimeCategory = category;
            return observation;
        }
    }
}
