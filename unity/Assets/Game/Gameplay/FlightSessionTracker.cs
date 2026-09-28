using System;
using System.Collections.Generic;
using UnityEngine;
using VoarVR.World;

namespace VoarVR.Gameplay
{
    public enum FlightSessionTimeCategory
    {
        ActiveHand,
        QuietAirborne,
        SupportedPerch,
        Paused,
        Excluded
    }

    public enum FlightSessionMovement
    {
        None,
        Flight,
        Walking
    }

    // One end-of-frame observation. The caller owns policy for the primary time category;
    // tracking, streaming and sample flags preserve why otherwise useful time was excluded.
    public struct FlightSessionObservation
    {
        public float DeltaTime;
        public LogicalPosition LogicalPosition;
        public Vector3 Velocity;
        public Vector3 WindVelocity;
        public Vector3 LeftHandVelocity;
        public Vector3 RightHandVelocity;
        public FlightSessionTimeCategory TimeCategory;
        public FlightSessionMovement Movement;
        public bool TrackingAvailable;
        public bool StreamingReady;
        public bool SampleValid;
        public bool MovementActive;

        public static FlightSessionObservation Create(double x, double y, double z, float deltaTime)
        {
            return new FlightSessionObservation
            {
                DeltaTime = deltaTime,
                LogicalPosition = new LogicalPosition(x, y, z),
                TimeCategory = FlightSessionTimeCategory.QuietAirborne,
                Movement = FlightSessionMovement.Flight,
                TrackingAvailable = true,
                StreamingReady = true,
                SampleValid = true
            };
        }
    }

    // Product-facing session accounting. This class has no Unity lifecycle, persistence or
    // wall-clock dependency, so the same observations produce the same summary in tests and play.
    public sealed class FlightSessionTracker
    {
        public const float MaximumObservationSeconds = .1f;
        public const float MaximumFrameSeconds = 5f;
        public const float ActiveHandSpeedMps = .35f;
        public const float WingbeatVerticalThresholdMps = .25f;
        public const float WingbeatCooldownSeconds = .25f;

        private readonly FlightSessionContext context;
        private readonly FlightSessionMetrics metrics = new FlightSessionMetrics();
        private readonly Dictionary<string, FlightSessionCollectibleTally> collectibleTallies =
            new Dictionary<string, FlightSessionCollectibleTally>(StringComparer.Ordinal);
        private bool hasPosition;
        private LogicalPosition previousPosition;
        private bool wingbeatArmed;
        private float wingbeatCooldown;
        private bool finished;
        private string endedUtc = "";
        private string endReason = "";

        public FlightSessionTracker(FlightSessionContext sessionContext)
        {
            if (sessionContext == null) throw new ArgumentNullException(nameof(sessionContext));
            context = Copy(sessionContext);
            if (!context.IsValid()) throw new ArgumentException("Session context is invalid.", nameof(sessionContext));
        }

        public bool IsFinished => finished;

        public bool Observe(FlightSessionObservation observation)
        {
            if (finished) return false;
            float dt = observation.DeltaTime;
            if (!Finite(dt) || dt <= 0f || dt > MaximumFrameSeconds)
            {
                MarkResetDiscontinuity();
                return false;
            }

            // Simulation uses capped steps, but product time comes from the real rendered
            // frame. Split a normal hitch into bounded accounting slices; only the first
            // slice can add the frame's position delta, while all slices retain elapsed time.
            bool accepted=true;
            while(dt>0f)
            {
                observation.DeltaTime=Mathf.Min(MaximumObservationSeconds,dt);
                accepted&=ObserveSlice(observation);
                dt-=observation.DeltaTime;
            }
            return accepted;
        }

        private bool ObserveSlice(FlightSessionObservation observation)
        {
            float dt=observation.DeltaTime;

            bool finiteSample = observation.SampleValid && Finite(observation.LogicalPosition)
                && Finite(observation.Velocity) && Finite(observation.WindVelocity)
                && Finite(observation.LeftHandVelocity) && Finite(observation.RightHandVelocity);
            var requestedCategory = ValidCategory(observation.TimeCategory)
                ? observation.TimeCategory : FlightSessionTimeCategory.Excluded;
            bool airborneCategory = requestedCategory == FlightSessionTimeCategory.ActiveHand
                || requestedCategory == FlightSessionTimeCategory.QuietAirborne;
            bool usableAirborne = finiteSample && observation.TrackingAvailable && observation.StreamingReady;
            var category = airborneCategory && !usableAirborne
                ? FlightSessionTimeCategory.Excluded : requestedCategory;

            metrics.ObservedSeconds += dt;
            AddPrimaryTime(category, dt);
            if (!observation.TrackingAvailable) metrics.TrackingGapSeconds += dt;
            if (!observation.StreamingReady) metrics.StreamingBlockedSeconds += dt;
            if (!finiteSample) metrics.InvalidSampleSeconds += dt;
            if (finiteSample && category == FlightSessionTimeCategory.SupportedPerch
                && observation.Movement == FlightSessionMovement.Walking && observation.MovementActive)
                metrics.WalkingActiveSeconds += dt;

            bool continuityEligible = finiteSample && category != FlightSessionTimeCategory.Paused
                && category != FlightSessionTimeCategory.Excluded;
            if (continuityEligible)
            {
                AccumulateMotion(observation);
                AccumulateWingbeat(observation, category, dt);
            }
            else
            {
                hasPosition = false;
                ResetWingbeatDetector();
            }
            return true;
        }

        // Player reset, teleport, respawn and load/resume are discontinuities. The next logical
        // position becomes a fresh anchor, preventing the jump from entering distance or ascent.
        public void MarkResetDiscontinuity()
        {
            hasPosition = false;
            ResetWingbeatDetector();
        }

        // Render-origin rebases are deliberately continuous. Feed logical positions before and
        // after the rebase; unlike a reset, no tracker state should be cleared.
        public void MarkWorldRebaseContinuity() { }

        public static FlightSessionTimeCategory ClassifyAirborneHands(Vector3 leftVelocity,
            Vector3 rightVelocity)
        {
            if (!Finite(leftVelocity) || !Finite(rightVelocity)) return FlightSessionTimeCategory.Excluded;
            float averageSpeed = (leftVelocity.magnitude + rightVelocity.magnitude) * .5f;
            return averageSpeed > ActiveHandSpeedMps
                ? FlightSessionTimeCategory.ActiveHand : FlightSessionTimeCategory.QuietAirborne;
        }

        public bool RecordCollectible(string collectibleId, int unitValue, int count = 1)
        {
            if (finished || !FlightSessionSummary.SafeId(collectibleId) || unitValue < 0 || count <= 0) return false;
            try
            {
                int addedValue = checked(unitValue * count);
                collectibleTallies.TryGetValue(collectibleId, out var tally);
                int nextTallyCount = checked((tally?.Count ?? 0) + count);
                int nextTallyValue = checked((tally?.Value ?? 0) + addedValue);
                int nextCatchCount = checked(metrics.CatchCount + count);
                int nextCollectibleValue = checked(metrics.CollectibleValue + addedValue);
                if (tally == null)
                {
                    tally = new FlightSessionCollectibleTally { CollectibleId = collectibleId };
                    collectibleTallies.Add(collectibleId, tally);
                }
                tally.Count = nextTallyCount;
                tally.Value = nextTallyValue;
                metrics.CatchCount = nextCatchCount;
                metrics.CollectibleValue = nextCollectibleValue;
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        public bool RecordTrick(int count = 1) => AddCount(ref metrics.TrickCount, count);
        public bool RecordCollision(int count = 1) => AddCount(ref metrics.CollisionCount, count);
        public bool RecordLanding(int count = 1) => AddCount(ref metrics.LandingCount, count);
        public bool RecordTakeoff(int count = 1) => AddCount(ref metrics.TakeoffCount, count);

        public FlightSessionSummary Snapshot()
        {
            var tallies = new List<FlightSessionCollectibleTally>(collectibleTallies.Count);
            foreach (var tally in collectibleTallies.Values)
                tallies.Add(new FlightSessionCollectibleTally
                    { CollectibleId = tally.CollectibleId, Count = tally.Count, Value = tally.Value });
            tallies.Sort((a, b) => StringComparer.Ordinal.Compare(a.CollectibleId, b.CollectibleId));
            return new FlightSessionSummary
            {
                Context = Copy(context),
                EndedUtc = endedUtc,
                EndReason = endReason,
                Finalized = finished,
                Metrics = Copy(metrics),
                Collectibles = tallies.ToArray()
            };
        }

        public FlightSessionSummary Finish(string reason, DateTime endedAtUtc)
        {
            if (finished) return Snapshot();
            CommitFinish(PrepareFinish(reason, endedAtUtc));
            return Snapshot();
        }

        // Build the immutable final record without stopping observation. The driver persists
        // this candidate first, then commits it, so a storage failure cannot silently freeze
        // metrics while the player resumes the still-live flight.
        public FlightSessionSummary PrepareFinish(string reason, DateTime endedAtUtc)
        {
            if (finished) return Snapshot();
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("An end reason is required.", nameof(reason));
            if (endedAtUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("The session end time must be UTC.", nameof(endedAtUtc));
            var summary = Snapshot();
            summary.EndReason = reason.Trim();
            summary.EndedUtc = endedAtUtc.ToString("o");
            summary.Finalized = true;
            return summary;
        }

        public bool CommitFinish(FlightSessionSummary prepared)
        {
            if (finished) return prepared != null
                && string.Equals(prepared.Context?.SessionId, context.SessionId, StringComparison.Ordinal);
            if (prepared == null || !prepared.Finalized || !prepared.IsValid()
                || !string.Equals(prepared.Context.ProfileId, context.ProfileId, StringComparison.Ordinal)
                || !string.Equals(prepared.Context.SessionId, context.SessionId, StringComparison.Ordinal)) return false;
            endReason = prepared.EndReason;
            endedUtc = prepared.EndedUtc;
            finished = true;
            return true;
        }

        private void AddPrimaryTime(FlightSessionTimeCategory category, float dt)
        {
            switch (category)
            {
                case FlightSessionTimeCategory.ActiveHand: metrics.ActiveHandSeconds += dt; break;
                case FlightSessionTimeCategory.QuietAirborne: metrics.QuietAirborneSeconds += dt; break;
                case FlightSessionTimeCategory.SupportedPerch: metrics.SupportedPerchSeconds += dt; break;
                case FlightSessionTimeCategory.Paused: metrics.PausedSeconds += dt; break;
                default: metrics.ExcludedSeconds += dt; break;
            }
        }

        private void AccumulateMotion(FlightSessionObservation observation)
        {
            double groundSpeed = observation.Velocity.magnitude;
            if (groundSpeed > metrics.MaxGroundSpeedMps) metrics.MaxGroundSpeedMps = groundSpeed;
            if (observation.Movement == FlightSessionMovement.Flight)
            {
                double airSpeed = (observation.Velocity - observation.WindVelocity).magnitude;
                if (airSpeed > metrics.MaxAirSpeedMps) metrics.MaxAirSpeedMps = airSpeed;
            }

            if (hasPosition)
            {
                double dx = observation.LogicalPosition.X - previousPosition.X;
                double dy = observation.LogicalPosition.Y - previousPosition.Y;
                double dz = observation.LogicalPosition.Z - previousPosition.Z;
                double horizontal = Math.Sqrt(dx * dx + dz * dz);
                double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (observation.Movement == FlightSessionMovement.Flight)
                {
                    metrics.FlightHorizontalMeters += horizontal;
                    metrics.FlightDistanceMeters += distance;
                    if (dy > 0d) metrics.GrossAscentMeters += dy;
                    else metrics.GrossDescentMeters -= dy;
                }
                else if (observation.Movement == FlightSessionMovement.Walking)
                {
                    metrics.WalkingHorizontalMeters += horizontal;
                    metrics.WalkingDistanceMeters += distance;
                }
            }
            previousPosition = observation.LogicalPosition;
            hasPosition = true;
        }

        private void AccumulateWingbeat(FlightSessionObservation observation,
            FlightSessionTimeCategory category, float dt)
        {
            if (category != FlightSessionTimeCategory.ActiveHand
                && category != FlightSessionTimeCategory.QuietAirborne)
            {
                ResetWingbeatDetector();
                return;
            }
            wingbeatCooldown = Mathf.Max(0f, wingbeatCooldown - dt);
            float vertical = (observation.LeftHandVelocity.y + observation.RightHandVelocity.y) * .5f;
            if (vertical > WingbeatVerticalThresholdMps) wingbeatArmed = true;
            if (wingbeatArmed && vertical < -WingbeatVerticalThresholdMps && wingbeatCooldown <= 0f)
            {
                metrics.EstimatedWingbeats++;
                wingbeatArmed = false;
                wingbeatCooldown = WingbeatCooldownSeconds;
            }
        }

        private bool AddCount(ref int target, int count)
        {
            if (finished || count <= 0) return false;
            try { target = checked(target + count); return true; }
            catch (OverflowException) { return false; }
        }

        private void ResetWingbeatDetector()
        {
            wingbeatArmed = false;
            wingbeatCooldown = 0f;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool ValidCategory(FlightSessionTimeCategory value)
        {
            return value >= FlightSessionTimeCategory.ActiveHand
                && value <= FlightSessionTimeCategory.Excluded;
        }
        private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(LogicalPosition value) => !double.IsNaN(value.X) && !double.IsInfinity(value.X)
            && !double.IsNaN(value.Y) && !double.IsInfinity(value.Y)
            && !double.IsNaN(value.Z) && !double.IsInfinity(value.Z);

        private static FlightSessionContext Copy(FlightSessionContext source)
        {
            return new FlightSessionContext
            {
                SessionId = source.SessionId,
                ProfileId = source.ProfileId,
                StartedUtc = source.StartedUtc,
                BuildId = source.BuildId,
                CharacterId = source.CharacterId,
                ActivityId = source.ActivityId,
                ContentId = source.ContentId,
                ContentRevision = source.ContentRevision,
                ScoringRevision = source.ScoringRevision
            };
        }

        private static FlightSessionMetrics Copy(FlightSessionMetrics source)
        {
            return new FlightSessionMetrics
            {
                ObservedSeconds = source.ObservedSeconds,
                ActiveHandSeconds = source.ActiveHandSeconds,
                QuietAirborneSeconds = source.QuietAirborneSeconds,
                SupportedPerchSeconds = source.SupportedPerchSeconds,
                PausedSeconds = source.PausedSeconds,
                ExcludedSeconds = source.ExcludedSeconds,
                TrackingGapSeconds = source.TrackingGapSeconds,
                StreamingBlockedSeconds = source.StreamingBlockedSeconds,
                InvalidSampleSeconds = source.InvalidSampleSeconds,
                WalkingActiveSeconds = source.WalkingActiveSeconds,
                FlightHorizontalMeters = source.FlightHorizontalMeters,
                FlightDistanceMeters = source.FlightDistanceMeters,
                WalkingHorizontalMeters = source.WalkingHorizontalMeters,
                WalkingDistanceMeters = source.WalkingDistanceMeters,
                GrossAscentMeters = source.GrossAscentMeters,
                GrossDescentMeters = source.GrossDescentMeters,
                MaxGroundSpeedMps = source.MaxGroundSpeedMps,
                MaxAirSpeedMps = source.MaxAirSpeedMps,
                EstimatedWingbeats = source.EstimatedWingbeats,
                CatchCount = source.CatchCount,
                CollectibleValue = source.CollectibleValue,
                TrickCount = source.TrickCount,
                CollisionCount = source.CollisionCount,
                LandingCount = source.LandingCount,
                TakeoffCount = source.TakeoffCount
            };
        }
    }
}
