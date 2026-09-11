using System;
using UnityEngine;
using VoarVR.World;

namespace VoarVR.Gameplay
{
    // Existing values are recorded in telemetry. Append new activities/objectives only.
    public enum FlightActivity { FreeFlight = 0, Training = 1, SkywardExpedition = 2, RidgeJourney = 3, RouteHome = 4 }
    public enum ObjectiveKind { DiscoverLift = 0, Soar = 1, Precision = 2, Land = 3, Acrobatic = 4, Migration = 5, CollectSeed = 6 }
    public enum ChallengeStatus { Available, Active, Completed, Failed }
    public static class ActivitySelection
    {
        public static FlightActivity Chosen = FlightActivity.RouteHome;
        public static bool ResumeRequested;
    }

    // This is the original score schema. Retain its records without comparing new rewards to them.
    [Serializable] public sealed class FlightProgress
    {
        public int Version = 1;
        public int SkywardBest, RidgeBest, TrainingBest, TrickBest;
        [NonSerialized] public bool JourneyRidgeUnlocked;
        public bool RidgeUnlocked => SkywardBest > 0 || JourneyRidgeUnlocked;
        public void Apply(FlightActivity activity, int score, int tricks)
        {
            if (activity == FlightActivity.SkywardExpedition) SkywardBest = Math.Max(SkywardBest, score);
            if (activity == FlightActivity.RidgeJourney) RidgeBest = Math.Max(RidgeBest, score);
            if (activity == FlightActivity.Training) TrainingBest = Math.Max(TrainingBest, score);
            TrickBest = Math.Max(TrickBest, tricks);
        }
    }

    public readonly struct ChallengeObservation
    {
        public readonly LogicalPosition Position;
        public readonly Vector3 Velocity;
        public readonly float VerticalAir, Stroke, Dt;
        public readonly bool Landed;
        public readonly int Collisions, Tricks;
        public readonly double SimulationTime;
        public ChallengeObservation(LogicalPosition p, Vector3 v, float air, float stroke, float dt, bool landed, int collisions, int tricks, double simulationTime = -1)
        { Position = p; Velocity = v; VerticalAir = air; Stroke = stroke; Dt = dt; Landed = landed; Collisions = collisions; Tricks = tricks; SimulationTime = simulationTime; }
    }

    public sealed class FlightChallenge
    {
        public FlightActivity Activity { get; }
        public ChallengeStatus Status { get; private set; } = ChallengeStatus.Active;
        public int Stage { get; private set; }
        public float Progress { get; private set; }
        public float ActiveSeconds { get; private set; }
        public float Elapsed => ActiveSeconds;
        public float RestSeconds { get; private set; }
        public int Score { get; private set; }
        public bool IsEfficiencyTrial => Activity == FlightActivity.Training;
        public int Medal => !IsEfficiencyTrial ? 0 : Score >= 850 ? 3 : Score >= 600 ? 2 : Score > 0 ? 1 : 0;
        public float SoaringGain { get; private set; }
        public double HighestAltitude { get; private set; }
        public float RequiredGain => Activity == FlightActivity.Training ? 30 : 100;
        public float RequiredAltitude => Activity == FlightActivity.Training ? 50 : 230;
        public float StrokeSeconds { get; private set; }
        public float MovementSeconds => StrokeSeconds;
        public int TechniqueCount { get; private set; }
        public int CollisionCount { get; private set; }
        public bool SeedCollected { get; private set; }
        public int StageCount => Activity == FlightActivity.Training ? 2 : Activity == FlightActivity.RidgeJourney || Activity == FlightActivity.RouteHome ? 5 : 4;
        public string ContentId => ContentIdFor(Activity);
        public readonly LogicalPosition Destination;
        private readonly int worldSeed;
        private LogicalPosition previous;
        private bool hasPrevious, previousLanded, countsInitialized, suppressNextObservation;
        private int previousCollisions, previousTricks;
        private double relocatedAltitudeOffset;

        public FlightChallenge(FlightActivity activity, int seed = 7319)
        {
            Activity = activity;
            worldSeed = seed;
            Destination = activity == FlightActivity.RidgeJourney ? FlightRegions.Island(seed, 1, 0) : FlightRegions.Island(seed, 0, 0);
            if (activity == FlightActivity.FreeFlight) Status = ChallengeStatus.Available;
        }

        public static string ContentIdFor(FlightActivity activity)
        {
            switch (activity)
            {
                case FlightActivity.FreeFlight: return "free-flight.v1";
                case FlightActivity.Training: return "training.v1";
                case FlightActivity.SkywardExpedition: return "skyward.v1";
                case FlightActivity.RidgeJourney: return "ridge.v1";
                case FlightActivity.RouteHome: return RouteHomeChapter.ContentId;
                default: return null;
            }
        }
        public ObjectiveKind Kind => Activity == FlightActivity.Training
            ? (Stage == 0 ? ObjectiveKind.DiscoverLift : ObjectiveKind.Soar)
            : Stage == 0 ? ObjectiveKind.DiscoverLift : Stage == 1 ? ObjectiveKind.Soar
            : Activity == FlightActivity.RouteHome ? (Stage == 2 ? ObjectiveKind.CollectSeed : Stage == 3 ? ObjectiveKind.Precision : ObjectiveKind.Land)
            : Stage == 2 ? (Activity == FlightActivity.RidgeJourney ? ObjectiveKind.Migration : ObjectiveKind.Precision)
            : Stage == 3 && Activity == FlightActivity.RidgeJourney ? ObjectiveKind.Precision : ObjectiveKind.Land;
        public string Title => Activity == FlightActivity.Training ? "LEARNING THE AIR" : Activity == FlightActivity.RidgeJourney ? "RIDGE JOURNEY" : Activity == FlightActivity.RouteHome ? "A ROUTE HOME" : "SKYWARD EXPEDITION";
        public string Instruction => Status == ChallengeStatus.Completed
            ? Activity == FlightActivity.RouteHome ? "The garden is growing again. Your seed has a home." : "RESULT SAVED · CONTINUE EXPLORING"
            : Kind == ObjectiveKind.DiscoverLift ? Activity == FlightActivity.RouteHome ? "The quiet garden needs a seed. Follow the rising leaves." : "Find the rising leaves above the valley"
            : Kind == ObjectiveKind.Soar ? Activity == FlightActivity.RouteHome ? "Rise above the clouds · flap or soar at your own pace" : "Circle in rising air · gain height with quiet wings"
            : Kind == ObjectiveKind.CollectSeed ? "Fly through the glowing seed, then carry it home"
            : Kind == ObjectiveKind.Migration ? "Follow the bright ridge around the rain front"
            : Kind == ObjectiveKind.Precision ? "Fly through the split stone arch"
            : Activity == FlightActivity.RouteHome ? "Bring the seed home · settle on the garden terrace" : "Land on the garden terrace";
        public LogicalPosition Target(double time) => Kind == ObjectiveKind.DiscoverLift || Kind == ObjectiveKind.Soar
            ? FlightRegions.DepartureThermal(time)
            : Kind == ObjectiveKind.CollectSeed ? RouteHomeChapter.SeedPosition
            : Kind == ObjectiveKind.Migration ? new LogicalPosition(770, Destination.Y + 20, 240)
            : Kind == ObjectiveKind.Precision ? new LogicalPosition(Destination.X, Destination.Y + 17, Destination.Z - 65) : Destination;

        // Call after repositioning or loss of continuous observations. The first new observation
        // is an anchor only: no altitude, crossing, seed, landing or trick reward can be fabricated.
        public void InvalidateObservation()
        {
            hasPrevious = false;
            countsInitialized = false;
            suppressNextObservation = true;
        }
        public void Step(ChallengeObservation o)
        {
            if (Status != ChallengeStatus.Active || o.Dt <= 0 || !Finite(o.Dt)
                || !Finite(o.Position.X) || !Finite(o.Position.Y) || !Finite(o.Position.Z)) return;
            if (!countsInitialized)
            {
                previousCollisions = o.Collisions;
                previousTricks = o.Tricks;
                countsInitialized = true;
            }
            int collisionsThisStep = Math.Max(0, o.Collisions - previousCollisions);
            CollisionCount += collisionsThisStep;
            TechniqueCount += Math.Max(0, o.Tricks - previousTricks);
            previousCollisions = o.Collisions;
            previousTricks = o.Tricks;
            if (suppressNextObservation)
            {
                suppressNextObservation = false;
                previous = o.Position;
                relocatedAltitudeOffset = ActiveSeconds > 0 ? Math.Max(0, o.Position.Y - HighestAltitude) : 0;
                previousLanded = o.Landed;
                hasPrevious = true;
                return;
            }
            if (o.Landed)
                RestSeconds += o.Dt;
            else
            {
                ActiveSeconds += o.Dt;
                if (o.Stroke > .5f) StrokeSeconds += o.Dt;
                HighestAltitude = Math.Max(HighestAltitude, o.Position.Y - relocatedAltitudeOffset);
            }
            double time = o.SimulationTime >= 0 ? o.SimulationTime : ActiveSeconds;
            if (Kind == ObjectiveKind.DiscoverLift)
            {
                if (HorizontalDistance(o.Position, FlightRegions.DepartureThermal(time)) < 95 && o.VerticalAir > 2 && !o.Landed)
                    AdvanceStage();
            }
            else if (Kind == ObjectiveKind.Soar)
            {
                if (hasPrevious && o.VerticalAir > 1.5f && (Activity == FlightActivity.RouteHome || o.Stroke < .5f) && !o.Landed)
                    SoaringGain += Mathf.Max(0, (float)(o.Position.Y - previous.Y));
                Progress = Mathf.Min(Mathf.Clamp01(SoaringGain / RequiredGain), Mathf.Clamp01((float)HighestAltitude / RequiredAltitude));
                if (SoaringGain >= RequiredGain && HighestAltitude >= RequiredAltitude)
                {
                    AdvanceStage();
                    if (Activity == FlightActivity.Training) Complete();
                }
            }
            else if (Kind == ObjectiveKind.CollectSeed && hasPrevious && !o.Landed)
            {
                if (Touches(previous, o.Position, RouteHomeChapter.SeedPosition, RouteHomeChapter.SeedRadius))
                { SeedCollected = true; AdvanceStage(); }
            }
            else if (Kind == ObjectiveKind.Migration)
            {
                Progress = Mathf.Clamp01(1 - (float)HorizontalDistance(o.Position, Target(time)) / 700);
                if (!o.Landed && HorizontalDistance(o.Position, Target(time)) < 45 && o.Position.Y > 230) AdvanceStage();
            }
            else if (Kind == ObjectiveKind.Precision && hasPrevious && !o.Landed)
            {
                var target = Target(time);
                double dz = o.Position.Z - previous.Z;
                if (Math.Abs(dz) > 1e-6)
                {
                    double t = (target.Z - previous.Z) / dz;
                    if (t >= 0 && t <= 1)
                    {
                        double x = previous.X + (o.Position.X - previous.X) * t - target.X;
                        double y = previous.Y + (o.Position.Y - previous.Y) * t - target.Y;
                        if (Math.Abs(x) < 13 && Math.Abs(y) < 13 && collisionsThisStep == 0) AdvanceStage();
                    }
                }
            }
            else if (Kind == ObjectiveKind.Land && hasPrevious && !previousLanded && o.Landed && HorizontalDistance(o.Position, Destination) < 25
                && Math.Abs(o.Position.Y - Destination.Y) < 4 && (Activity != FlightActivity.RouteHome || SeedCollected)) Complete();
            previous = o.Position;
            previousLanded = o.Landed;
            hasPrevious = true;
        }
        public void Fail() { if (Status == ChallengeStatus.Active) Status = ChallengeStatus.Failed; }
        private void AdvanceStage() { Stage++; Progress = 0; }
        private void Complete()
        {
            Status = ChallengeStatus.Completed;
            Progress = 1;
            // Adventures are untimed. Active movement, rest and technique are descriptive,
            // with no pressure to avoid flapping or postpone a break for a better story reward.
            Score = IsEfficiencyTrial
                ? Mathf.Max(100, 1000 - Mathf.RoundToInt(StrokeSeconds * 2) - CollisionCount * 40
                    - Mathf.RoundToInt(Mathf.Max(0, ActiveSeconds - 600) * .2f) + Mathf.Min(TechniqueCount, 3) * 75)
                : 1000;
        }
        public ChallengeCheckpoint Capture()
        {
            return new ChallengeCheckpoint
            {
                Activity = Activity, ContentId = ContentId, WorldSeed = worldSeed, Status = Status, Stage = Stage,
                ActiveSeconds = ActiveSeconds, RestSeconds = RestSeconds, StrokeSeconds = StrokeSeconds,
                SoaringGain = SoaringGain, HighestAltitude = HighestAltitude, SeedCollected = SeedCollected,
                CollisionCount = CollisionCount, TechniqueCount = TechniqueCount, Score = Score
            };
        }
        public bool Restore(ChallengeCheckpoint checkpoint)
        {
            if (checkpoint == null || !checkpoint.IsValid() || checkpoint.Activity != Activity
                || checkpoint.WorldSeed != worldSeed || checkpoint.ContentId != ContentId) return false;
            Stage = checkpoint.Stage; Status = checkpoint.Status; ActiveSeconds = checkpoint.ActiveSeconds;
            RestSeconds = checkpoint.RestSeconds; StrokeSeconds = checkpoint.StrokeSeconds;
            SoaringGain = checkpoint.SoaringGain; HighestAltitude = checkpoint.HighestAltitude;
            SeedCollected = checkpoint.SeedCollected; CollisionCount = checkpoint.CollisionCount;
            TechniqueCount = checkpoint.TechniqueCount; Score = checkpoint.Score;
            Progress = Status == ChallengeStatus.Completed ? 1 : Kind == ObjectiveKind.Soar
                ? Mathf.Min(Mathf.Clamp01(SoaringGain / RequiredGain), Mathf.Clamp01((float)HighestAltitude / RequiredAltitude)) : 0;
            InvalidateObservation();
            return true;
        }
        public static bool Touches(LogicalPosition from, LogicalPosition to, LogicalPosition target, double radius)
        {
            double x = to.X - from.X, y = to.Y - from.Y, z = to.Z - from.Z;
            double length = x * x + y * y + z * z;
            double t = length > 1e-12 ? ((target.X - from.X) * x + (target.Y - from.Y) * y + (target.Z - from.Z) * z) / length : 0;
            t = Math.Max(0, Math.Min(1, t));
            double dx = target.X - from.X - x * t, dy = target.Y - from.Y - y * t, dz = target.Z - from.Z - z * t;
            return dx * dx + dy * dy + dz * dz <= radius * radius;
        }
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static double HorizontalDistance(LogicalPosition a, LogicalPosition b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
    }
}
