using NUnit.Framework;
using UnityEngine;
using VoarVR.Gameplay;
using VoarVR.World;

namespace VoarVR.Tests
{
    public class JourneyChallengeTests
    {
        private static ChallengeObservation Observe(double x, double y, double z, float dt = .02f,
            float stroke = 0, bool landed = false, float air = 4, int tricks = 0, int collisions = 0)
            => new ChallengeObservation(new LogicalPosition(x, y, z), Vector3.forward * 8, air, stroke, dt, landed, collisions, tricks, 0);
        private static FlightChallenge ReachSeed(float stroke = 0, float rest = 0)
        {
            var challenge = new FlightChallenge(FlightActivity.RouteHome);
            challenge.Step(Observe(100, 30, 140, stroke: stroke));
            if (rest > 0) challenge.Step(Observe(100, 30, 140, dt: rest, landed: true));
            for (int y = 31; y <= 240; y++) challenge.Step(Observe(100, y, 140, stroke: stroke));
            Assert.That(challenge.Kind, Is.EqualTo(ObjectiveKind.CollectSeed));
            return challenge;
        }
        private static void CompleteRoute(FlightChallenge challenge)
        {
            challenge.Step(Observe(140, 245, 185));
            challenge.Step(Observe(140, 245, 215));
            Assert.That(challenge.SeedCollected, Is.True);
            challenge.Step(Observe(420, 287, 540));
            challenge.Step(Observe(420, 287, 570));
            Assert.That(challenge.Kind, Is.EqualTo(ObjectiveKind.Land));
            challenge.Step(Observe(420, 270, 620, landed: true));
            Assert.That(challenge.Status, Is.EqualTo(ChallengeStatus.Completed));
        }
        [Test] public void RecordedEnumValuesAreUnchangedAndStoryIsAppended()
        {
            Assert.That((int)FlightActivity.FreeFlight, Is.Zero);
            Assert.That((int)FlightActivity.Training, Is.EqualTo(1));
            Assert.That((int)FlightActivity.SkywardExpedition, Is.EqualTo(2));
            Assert.That((int)FlightActivity.RidgeJourney, Is.EqualTo(3));
            Assert.That((int)FlightActivity.RouteHome, Is.EqualTo(4));
            Assert.That((int)ObjectiveKind.Migration, Is.EqualTo(5));
            Assert.That((int)ObjectiveKind.CollectSeed, Is.EqualTo(6));
        }
        [Test] public void AdventureCanBeFlappedOrSoaredAndRestDoesNotChangeReward()
        {
            var soaring = ReachSeed(); var moving = ReachSeed(stroke: 2, rest: 1200);
            CompleteRoute(soaring); CompleteRoute(moving);
            Assert.That(moving.Score, Is.EqualTo(soaring.Score));
            Assert.That(moving.Medal, Is.Zero);
            Assert.That(moving.MovementSeconds, Is.GreaterThan(4));
            Assert.That(soaring.MovementSeconds, Is.Zero);
            Assert.That(moving.RestSeconds - soaring.RestSeconds, Is.EqualTo(1200).Within(.01f));
            Assert.That(moving.ActiveSeconds, Is.EqualTo(soaring.ActiveSeconds).Within(.001f));
        }
        [Test] public void QuietAirborneHandsAreActiveTimeAndPerchingIsRest()
        {
            var challenge = new FlightChallenge(FlightActivity.RouteHome);
            challenge.Step(Observe(0, 20, 0, dt: 5, air: 0));
            challenge.Step(Observe(0, 20, 0, dt: 60, landed: true, stroke: 2));
            Assert.That(challenge.ActiveSeconds, Is.EqualTo(5));
            Assert.That(challenge.RestSeconds, Is.EqualTo(60));
            Assert.That(challenge.MovementSeconds, Is.Zero);
            Assert.That(challenge.Stage, Is.Zero);
        }
        private static FlightChallenge Training(bool move, bool rest)
        {
            var challenge = new FlightChallenge(FlightActivity.Training);
            challenge.Step(Observe(100, 30, 140));
            challenge.Step(Observe(100, 30, 140, dt: 20, stroke: move ? 2 : 0));
            if (rest) challenge.Step(Observe(100, 30, 140, dt: 1800, landed: true));
            for (int y = 31; y <= 65; y++) challenge.Step(Observe(100, y, 140));
            Assert.That(challenge.Status, Is.EqualTo(ChallengeStatus.Completed));
            return challenge;
        }
        [Test] public void TrainingRetainsEfficiencyMedalsButLongRestHasNoPenalty()
        {
            var quiet = Training(false, false); var moving = Training(true, false); var rested = Training(true, true);
            Assert.That(quiet.Score, Is.GreaterThan(moving.Score));
            Assert.That(moving.Score, Is.EqualTo(rested.Score));
            Assert.That(rested.Medal, Is.GreaterThan(0));
            Assert.That(rested.ActiveSeconds, Is.EqualTo(moving.ActiveSeconds));
        }
        [Test] public void SeedRequiresRealSweptIntersectionAndCannotBeCollectedWhilePerched()
        {
            var challenge = ReachSeed();
            challenge.Step(Observe(140, 245, 185));
            challenge.Step(Observe(140, 245, 215, landed: true));
            Assert.That(challenge.SeedCollected, Is.False);
            challenge.Step(Observe(160, 245, 215)); challenge.Step(Observe(160, 245, 185));
            Assert.That(challenge.SeedCollected, Is.False);
            challenge.Step(Observe(140, 245, 185)); challenge.Step(Observe(140, 245, 215));
            Assert.That(challenge.SeedCollected, Is.True);
        }
        [Test] public void ResumePreservesMilestonesButTeleportAcrossSeedGivesNoPickup()
        {
            var challenge = ReachSeed();
            challenge.Step(Observe(140, 245, 185));
            var resumed = new FlightChallenge(FlightActivity.RouteHome);
            Assert.That(resumed.Restore(challenge.Capture()), Is.True);
            resumed.Step(Observe(140, 245, 215, tricks: 8));
            Assert.That(resumed.SeedCollected, Is.False);
            Assert.That(resumed.TechniqueCount, Is.Zero);
            Assert.That(resumed.SoaringGain, Is.EqualTo(challenge.SoaringGain));
            resumed.Step(Observe(140, 245, 185, tricks: 8));
            Assert.That(resumed.SeedCollected, Is.True, "A subsequent physical crossing is valid");
        }
        [Test] public void RecoveryCannotManufactureAltitudeOrSoaringGain()
        {
            var challenge = new FlightChallenge(FlightActivity.RouteHome);
            challenge.Step(Observe(100, 30, 140));
            challenge.Step(Observe(100, 40, 140));
            challenge.InvalidateObservation();
            challenge.Step(Observe(100, 500, 140));
            challenge.Step(Observe(100, 501, 140));
            Assert.That(challenge.HighestAltitude, Is.EqualTo(41));
            Assert.That(challenge.SoaringGain, Is.EqualTo(11));
            Assert.That(challenge.Stage, Is.EqualTo(1));
        }
        [Test] public void ResumeCannotCrossArchOrCompleteLandingOnItsAnchorFrame()
        {
            var challenge = ReachSeed();
            challenge.Step(Observe(140, 245, 185)); challenge.Step(Observe(140, 245, 215));
            challenge.Step(Observe(420, 287, 540));
            challenge.InvalidateObservation(); challenge.Step(Observe(420, 287, 570));
            Assert.That(challenge.Kind, Is.EqualTo(ObjectiveKind.Precision));
            challenge.Step(Observe(420, 287, 540));
            Assert.That(challenge.Kind, Is.EqualTo(ObjectiveKind.Land));
            challenge.InvalidateObservation(); challenge.Step(Observe(420, 270, 620, landed: true));
            Assert.That(challenge.Status, Is.EqualTo(ChallengeStatus.Active));
            challenge.Step(Observe(420, 270, 620, landed: true));
            Assert.That(challenge.Status, Is.EqualTo(ChallengeStatus.Active), "Remaining perched after recovery is not a new landing");
            challenge.Step(Observe(420, 272, 620));
            challenge.Step(Observe(420, 270, 620, landed: true));
            Assert.That(challenge.Status, Is.EqualTo(ChallengeStatus.Completed));
        }
        [Test] public void FreshRestartRetainsAbsoluteAltitudeThreshold()
        {
            var challenge = new FlightChallenge(FlightActivity.Training);
            challenge.InvalidateObservation();
            challenge.Step(Observe(100, 25, 140));
            challenge.Step(Observe(100, 26, 140));
            for (int y = 27; y <= 56; y++) challenge.Step(Observe(100, y, 140));
            Assert.That(challenge.HighestAltitude, Is.EqualTo(56));
            Assert.That(challenge.Status, Is.EqualTo(ChallengeStatus.Completed));
        }
        [Test] public void SnapshotRetainsActiveRestTechniqueAndCollisionAccounting()
        {
            var challenge = new FlightChallenge(FlightActivity.RouteHome);
            challenge.Step(Observe(0, 20, 0, air: 0, collisions: 4, tricks: 2));
            challenge.Step(Observe(0, 20, 0, dt: 10, stroke: 2, air: 0, collisions: 5, tricks: 3));
            challenge.Step(Observe(0, 20, 0, dt: 30, landed: true, air: 0, collisions: 5, tricks: 3));
            var restored = new FlightChallenge(FlightActivity.RouteHome);
            Assert.That(restored.Restore(challenge.Capture()), Is.True);
            restored.Step(Observe(0, 20, 0, air: 0));
            restored.Step(Observe(0, 20, 0, air: 0, tricks: 1, collisions: 1));
            Assert.That(restored.MovementSeconds, Is.EqualTo(10));
            Assert.That(restored.RestSeconds, Is.EqualTo(30));
            Assert.That(restored.TechniqueCount, Is.EqualTo(2));
            Assert.That(restored.CollisionCount, Is.EqualTo(2));
        }
        [Test] public void WrongWorldOrMissingContentCannotRestore()
        {
            var checkpoint = ReachSeed().Capture();
            Assert.That(new FlightChallenge(FlightActivity.RouteHome, 999).Restore(checkpoint), Is.False);
            checkpoint.ContentId = "route-home.v999";
            Assert.That(new FlightChallenge(FlightActivity.RouteHome).Restore(checkpoint), Is.False);
        }
    }
}
