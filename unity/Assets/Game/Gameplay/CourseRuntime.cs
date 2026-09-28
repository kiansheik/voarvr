using System;
using System.Collections.Generic;
using System.Diagnostics;
using VoarVR.Flight;
using VoarVR.World;

namespace VoarVR.Gameplay
{
    public interface IMonotonicClock
    {
        double NowSeconds { get; }
    }

    public sealed class StopwatchMonotonicClock : IMonotonicClock
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        public double NowSeconds => stopwatch.Elapsed.TotalSeconds;
    }

    public enum CourseState
    {
        Calibration = 0,
        Ready = 1,
        Countdown3 = 2,
        Countdown2 = 3,
        Countdown1 = 4,
        Running = 5,
        Finished = 6,
        Failed = 7,
        Results = 8,
        Rest = 9
    }

    [Flags]
    public enum CourseNonRankedReason
    {
        None = 0,
        CalibrationLost = 1 << 0,
        TrackingLost = 1 << 1,
        Paused = 1 << 2,
        StreamingBlocked = 1 << 3,
        Reset = 1 << 4,
        Recovery = 1 << 5,
        ControlModeChanged = 1 << 6,
        WeatherChanged = 1 << 7,
        AssistanceChanged = 1 << 8,
        ClockInvalid = 1 << 9,
        ClockWentBackward = 1 << 10,
        ExplicitPractice = 1 << 11,
        Collision = 1 << 12,
        ApplicationSuspended = 1 << 13,
        FrameTiming = 1 << 14
    }

    public enum CourseFailureReason
    {
        None = 0,
        TimeLimit = 1,
        CorridorLeft = 2,
        Explicit = 3
    }

    public enum CourseTaskStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2
    }

    // Position and task points share a course-local logical frame selected by the adapter.
    // CatchesSincePreviousObservation is an event batch, not a lifetime tally.
    public readonly struct CourseObservation
    {
        public readonly LogicalPosition Position;
        public readonly bool HasPosition;
        public readonly bool CalibrationValid;
        public readonly bool TrackingValid;
        public readonly bool Paused;
        public readonly bool StreamingBlocked;
        public readonly bool SupportedLanding;
        public readonly FlightTrick DetectedTrick;
        public readonly IReadOnlyList<CatchResult> CatchesSincePreviousObservation;
        public readonly CourseNonRankedReason InvalidationReasons;
        public readonly int SupportedSurfaceId;

        public CourseObservation(LogicalPosition position, bool hasPosition = true,
            bool calibrationValid = true, bool trackingValid = true, bool paused = false,
            bool streamingBlocked = false, bool supportedLanding = false,
            FlightTrick detectedTrick = FlightTrick.None,
            IReadOnlyList<CatchResult> catchesSincePreviousObservation = null,
            CourseNonRankedReason invalidationReasons = CourseNonRankedReason.None,
            int supportedSurfaceId = 0)
        {
            Position = position;
            HasPosition = hasPosition;
            CalibrationValid = calibrationValid;
            TrackingValid = trackingValid;
            Paused = paused;
            StreamingBlocked = streamingBlocked;
            SupportedLanding = supportedLanding;
            DetectedTrick = detectedTrick;
            CatchesSincePreviousObservation = catchesSincePreviousObservation;
            InvalidationReasons = invalidationReasons;
            SupportedSurfaceId = supportedSurfaceId;
        }
    }

    public readonly struct CourseTaskEvaluation
    {
        public readonly CourseTaskStatus Status;
        public readonly double Current;
        public readonly double Required;
        public readonly ObjectiveProgressUnit Unit;
        public readonly CourseFailureReason FailureReason;

        public CourseTaskEvaluation(CourseTaskStatus status, double current, double required,
            ObjectiveProgressUnit unit, CourseFailureReason failureReason = CourseFailureReason.None)
        {
            Status = status;
            Current = current;
            Required = required;
            Unit = unit;
            FailureReason = failureReason;
        }
    }

    public static class CourseGeometry
    {
        private const double Epsilon = 1e-12;

        public static bool IsFinite(LogicalPosition point)
        {
            return Finite(point.X) && Finite(point.Y) && Finite(point.Z);
        }

        public static double SquaredDistance(LogicalPosition a, LogicalPosition b)
        {
            var x = a.X - b.X;
            var y = a.Y - b.Y;
            var z = a.Z - b.Z;
            return x * x + y * y + z * z;
        }

        public static bool PointInsideSphere(LogicalPosition point, LogicalPosition center, double radius)
        {
            return IsFinite(point) && IsFinite(center) && Finite(radius) && radius > 0
                && SquaredDistance(point, center) <= radius * radius;
        }

        public static bool SegmentTouchesSphere(LogicalPosition from, LogicalPosition to,
            LogicalPosition center, double radius)
        {
            if (!IsFinite(from) || !IsFinite(to) || !IsFinite(center) || !Finite(radius) || radius <= 0)
                return false;
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var dz = to.Z - from.Z;
            var lengthSquared = dx * dx + dy * dy + dz * dz;
            var t = lengthSquared > Epsilon
                ? Clamp01(((center.X - from.X) * dx + (center.Y - from.Y) * dy
                    + (center.Z - from.Z) * dz) / lengthSquared)
                : 0;
            var closest = new LogicalPosition(from.X + dx * t, from.Y + dy * t, from.Z + dz * t);
            return SquaredDistance(closest, center) <= radius * radius;
        }

        // Built-in gates face down-course (+Z). Completion requires crossing the
        // rendered plane in that direction and through its circular safe aperture;
        // merely brushing the surrounding volume cannot score a gate.
        public static bool SegmentCrossesForwardGate(LogicalPosition from, LogicalPosition to,
            LogicalPosition center, double radius)
        {
            if (!IsFinite(from) || !IsFinite(to) || !IsFinite(center) || !Finite(radius) || radius <= 0)
                return false;
            var dz = to.Z - from.Z;
            if (dz <= Epsilon || from.Z > center.Z || to.Z < center.Z) return false;
            var t = (center.Z - from.Z) / dz;
            if (t < 0 || t > 1) return false;
            var x = from.X + (to.X - from.X) * t - center.X;
            var y = from.Y + (to.Y - from.Y) * t - center.Y;
            return x * x + y * y <= radius * radius;
        }

        public static double DistanceToSegment(LogicalPosition point, LogicalPosition start,
            LogicalPosition finish)
        {
            var t = ProgressParameter(point, start, finish);
            var closest = Lerp(start, finish, t);
            return Math.Sqrt(SquaredDistance(point, closest));
        }

        public static double ProgressParameter(LogicalPosition point, LogicalPosition start,
            LogicalPosition finish)
        {
            var dx = finish.X - start.X;
            var dy = finish.Y - start.Y;
            var dz = finish.Z - start.Z;
            var lengthSquared = dx * dx + dy * dy + dz * dz;
            if (lengthSquared <= Epsilon) return 0;
            return Clamp01(((point.X - start.X) * dx + (point.Y - start.Y) * dy
                + (point.Z - start.Z) * dz) / lengthSquared);
        }

        public static bool SegmentTouchesAltitudeCylinder(LogicalPosition from, LogicalPosition to,
            LogicalPosition horizontalCenter, double minimumAltitude, double maximumAltitude,
            double radius)
        {
            if (!IsFinite(from) || !IsFinite(to) || !IsFinite(horizontalCenter)
                || !Finite(minimumAltitude) || !Finite(maximumAltitude)
                || minimumAltitude > maximumAltitude || !Finite(radius) || radius <= 0)
                return false;

            double first;
            double last;
            var dy = to.Y - from.Y;
            if (Math.Abs(dy) <= Epsilon)
            {
                if (from.Y < minimumAltitude || from.Y > maximumAltitude) return false;
                first = 0;
                last = 1;
            }
            else
            {
                var firstCrossing = (minimumAltitude - from.Y) / dy;
                var secondCrossing = (maximumAltitude - from.Y) / dy;
                first = Math.Max(0, Math.Min(firstCrossing, secondCrossing));
                last = Math.Min(1, Math.Max(firstCrossing, secondCrossing));
                if (first > last) return false;
            }

            var dx = to.X - from.X;
            var dz = to.Z - from.Z;
            var horizontalLengthSquared = dx * dx + dz * dz;
            var nearest = horizontalLengthSquared > Epsilon
                ? ((horizontalCenter.X - from.X) * dx + (horizontalCenter.Z - from.Z) * dz)
                    / horizontalLengthSquared
                : first;
            nearest = Math.Max(first, Math.Min(last, nearest));
            var x = from.X + dx * nearest - horizontalCenter.X;
            var z = from.Z + dz * nearest - horizontalCenter.Z;
            return x * x + z * z <= radius * radius;
        }

        private static LogicalPosition Lerp(LogicalPosition from, LogicalPosition to, double t)
        {
            return new LogicalPosition(from.X + (to.X - from.X) * t,
                from.Y + (to.Y - from.Y) * t, from.Z + (to.Z - from.Z) * t);
        }

        private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class CourseTaskEvaluator
    {
        private readonly CourseTaskDefinition definition;
        private readonly string expectedObjectiveId;
        private int collectibleCount;
        private bool corridorEntered;

        public CourseTaskDefinition Definition => definition;
        public CourseTaskEvaluation Current { get; private set; }

        public CourseTaskEvaluator(CourseTaskDefinition definition, string objectiveId = null)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            expectedObjectiveId = objectiveId;
            definition.ValidateOrThrow();
            Reset();
        }

        public void Reset()
        {
            collectibleCount = 0;
            corridorEntered = false;
            Current = Pending(0);
        }

        public CourseTaskEvaluation Evaluate(CourseObservation observation,
            bool hasPreviousPosition, LogicalPosition previousPosition)
        {
            if (Current.Status != CourseTaskStatus.Pending) return Current;
            switch (definition.Kind)
            {
                case CourseTaskKind.Gate:
                    return EvaluateGate(observation, hasPreviousPosition, previousPosition);
                case CourseTaskKind.CollectibleQuota:
                    return EvaluateCollectibles(observation.CatchesSincePreviousObservation);
                case CourseTaskKind.AltitudeBand:
                    return EvaluateAltitude(observation, hasPreviousPosition, previousPosition);
                case CourseTaskKind.Corridor:
                    return EvaluateCorridor(observation, hasPreviousPosition, previousPosition);
                case CourseTaskKind.Trick:
                    if (observation.DetectedTrick == definition.RequiredTrick)
                        Current = Completed(1);
                    return Current;
                case CourseTaskKind.SupportedLanding:
                    if (observation.HasPosition && observation.SupportedLanding
                        && (definition.RequiredSurfaceId == 0
                            || observation.SupportedSurfaceId == definition.RequiredSurfaceId)
                        && CourseGeometry.PointInsideSphere(observation.Position,
                            definition.PointA.Logical, definition.Radius))
                        Current = Completed(1);
                    return Current;
                default:
                    throw new InvalidOperationException("Unsupported course task kind.");
            }
        }

        private CourseTaskEvaluation EvaluateGate(CourseObservation observation,
            bool hasPreviousPosition, LogicalPosition previousPosition)
        {
            if (!observation.HasPosition || !hasPreviousPosition) return Current;
            if (CourseGeometry.SegmentCrossesForwardGate(previousPosition,
                    observation.Position, definition.PointA.Logical, definition.Radius))
                Current = Completed(1);
            return Current;
        }

        private CourseTaskEvaluation EvaluateCollectibles(IReadOnlyList<CatchResult> catches)
        {
            if (catches != null)
            {
                for (var i = 0; i < catches.Count; i++)
                {
                    var caught = catches[i];
                    if (caught.IsValid && caught.Type == definition.CollectibleType
                        && string.Equals(caught.CollectibleId, definition.CollectibleId,
                            StringComparison.Ordinal)
                        && (expectedObjectiveId == null || string.Equals(caught.ObjectiveId,
                            expectedObjectiveId, StringComparison.Ordinal)))
                        collectibleCount++;
                }
            }
            if (collectibleCount >= definition.RequiredCount)
                Current = Completed(definition.RequiredCount);
            else
                Current = Pending(collectibleCount);
            return Current;
        }

        private CourseTaskEvaluation EvaluateAltitude(CourseObservation observation,
            bool hasPreviousPosition, LogicalPosition previousPosition)
        {
            if (!observation.HasPosition) return Current;
            var center = definition.PointA.Logical;
            var currentInside = observation.Position.Y >= definition.MinimumAltitude
                && observation.Position.Y <= definition.MaximumAltitude
                && HorizontalSquaredDistance(observation.Position, center)
                    <= definition.Radius * definition.Radius;
            if (currentInside || hasPreviousPosition
                && CourseGeometry.SegmentTouchesAltitudeCylinder(previousPosition,
                    observation.Position, center, definition.MinimumAltitude,
                    definition.MaximumAltitude, definition.Radius))
                Current = Completed(1);
            return Current;
        }

        private CourseTaskEvaluation EvaluateCorridor(CourseObservation observation,
            bool hasPreviousPosition, LogicalPosition previousPosition)
        {
            if (!observation.HasPosition) return Current;
            var start = definition.PointA.Logical;
            var finish = definition.PointB.Logical;
            if (!corridorEntered)
            {
                corridorEntered = CourseGeometry.PointInsideSphere(observation.Position, start,
                        definition.Radius)
                    || hasPreviousPosition && CourseGeometry.SegmentTouchesSphere(previousPosition,
                        observation.Position, start, definition.Radius);
                if (!corridorEntered) return Current;
            }

            var length = Math.Sqrt(CourseGeometry.SquaredDistance(start, finish));
            var progress = CourseGeometry.ProgressParameter(observation.Position, start, finish) * length;
            if (CourseGeometry.PointInsideSphere(observation.Position, finish, definition.Radius)
                || hasPreviousPosition && CourseGeometry.SegmentTouchesSphere(previousPosition,
                    observation.Position, finish, definition.Radius))
            {
                Current = new CourseTaskEvaluation(CourseTaskStatus.Completed, length, length,
                    ObjectiveProgressUnit.Metres);
                return Current;
            }
            if (CourseGeometry.DistanceToSegment(observation.Position, start, finish)
                > definition.Radius)
            {
                Current = new CourseTaskEvaluation(CourseTaskStatus.Failed, progress, length,
                    ObjectiveProgressUnit.Metres, CourseFailureReason.CorridorLeft);
                return Current;
            }
            Current = new CourseTaskEvaluation(CourseTaskStatus.Pending, progress, length,
                ObjectiveProgressUnit.Metres);
            return Current;
        }

        private CourseTaskEvaluation Pending(double current)
        {
            return new CourseTaskEvaluation(CourseTaskStatus.Pending, current, Required,
                Unit);
        }

        private CourseTaskEvaluation Completed(double current)
        {
            return new CourseTaskEvaluation(CourseTaskStatus.Completed, current, Required,
                Unit);
        }

        private double Required
        {
            get
            {
                if (definition.Kind == CourseTaskKind.CollectibleQuota) return definition.RequiredCount;
                if (definition.Kind == CourseTaskKind.Corridor)
                    return Math.Sqrt(CourseGeometry.SquaredDistance(definition.PointA.Logical,
                        definition.PointB.Logical));
                return 1;
            }
        }

        private ObjectiveProgressUnit Unit
        {
            get
            {
                switch (definition.Kind)
                {
                    case CourseTaskKind.Gate: return ObjectiveProgressUnit.Gates;
                    case CourseTaskKind.CollectibleQuota: return ObjectiveProgressUnit.Count;
                    case CourseTaskKind.Corridor: return ObjectiveProgressUnit.Metres;
                    default: return ObjectiveProgressUnit.Count;
                }
            }
        }

        private static double HorizontalSquaredDistance(LogicalPosition a, LogicalPosition b)
        {
            var x = a.X - b.X;
            var z = a.Z - b.Z;
            return x * x + z * z;
        }
    }

    public readonly struct CourseAttemptResult
    {
        public readonly CourseResultKey Key;
        public readonly bool Completed;
        public readonly double DurationSeconds;
        public readonly int CompletedTasks;
        public readonly int TotalTasks;
        public readonly bool Ranked;
        public readonly CourseNonRankedReason NonRankedReasons;
        public readonly CourseFailureReason FailureReason;

        public CourseAttemptResult(CourseResultKey key, bool completed, double durationSeconds,
            int completedTasks, int totalTasks, bool ranked,
            CourseNonRankedReason nonRankedReasons, CourseFailureReason failureReason)
        {
            Key = key;
            Completed = completed;
            DurationSeconds = durationSeconds;
            CompletedTasks = completedTasks;
            TotalTasks = totalTasks;
            Ranked = ranked;
            NonRankedReasons = nonRankedReasons;
            FailureReason = failureReason;
        }
    }

    public sealed class CourseRuntime
    {
        private const CourseNonRankedReason PositionUntrustedReasons =
            CourseNonRankedReason.CalibrationLost | CourseNonRankedReason.TrackingLost
            | CourseNonRankedReason.Paused | CourseNonRankedReason.StreamingBlocked
            | CourseNonRankedReason.Reset | CourseNonRankedReason.Recovery
            | CourseNonRankedReason.Collision | CourseNonRankedReason.ApplicationSuspended;

        private readonly CourseDefinition definition;
        private readonly IMonotonicClock clock;
        private readonly CourseTaskEvaluator[] evaluators;
        private CourseState state = CourseState.Calibration;
        private CourseNonRankedReason nonRankedReasons;
        private CourseFailureReason failureReason;
        private int taskIndex;
        private bool hasPreviousPosition;
        private LogicalPosition previousPosition;
        private bool clockInitialized;
        private double lastClockSeconds;
        private double countdownStartedAt;
        private double runStartedAt;
        private double endedAt;
        private bool hasRunStarted;
        private bool attemptEnded;
        private bool attemptCompleted;

        public CourseDefinition Definition => definition;
        public CourseState State => state;
        public int CountdownNumber => state == CourseState.Countdown3 ? 3
            : state == CourseState.Countdown2 ? 2 : state == CourseState.Countdown1 ? 1 : 0;
        public int TaskIndex => taskIndex;
        public int CompletedTasks => Math.Min(taskIndex, evaluators.Length);
        public CourseNonRankedReason NonRankedReasons => nonRankedReasons;
        public CourseFailureReason FailureReason => failureReason;
        public bool RankedEligible => nonRankedReasons == CourseNonRankedReason.None;
        public bool HasEndedAttempt => attemptEnded;
        public double ElapsedSeconds => !hasRunStarted ? 0
            : Math.Max(0, (attemptEnded ? endedAt : lastClockSeconds) - runStartedAt);
        public double RemainingSeconds => Math.Max(0, definition.TimeLimitSeconds - ElapsedSeconds);

        public ObjectiveProgressSnapshot Progress
        {
            get
            {
                var index = Math.Min(taskIndex, evaluators.Length - 1);
                var task = evaluators[index];
                var evaluation = task.Current;
                var progressState = evaluation.Status == CourseTaskStatus.Completed
                    ? ObjectiveProgressState.Completed
                    : evaluation.Status == CourseTaskStatus.Failed
                        ? ObjectiveProgressState.Failed : ObjectiveProgressState.Active;
                return new ObjectiveProgressSnapshot(task.Definition.StableId, task.Definition.Label,
                    index, evaluators.Length, evaluation.Current, evaluation.Required,
                    evaluation.Unit, progressState);
            }
        }

        public event Action<CourseState> StateChanged;

        public CourseRuntime(CourseDefinition definition, IMonotonicClock clock)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            definition.ValidateOrThrow();
            evaluators = new CourseTaskEvaluator[definition.Tasks.Count];
            for (var i = 0; i < evaluators.Length; i++)
                evaluators[i] = new CourseTaskEvaluator(definition.Tasks[i],
                    definition.Tasks[i].Kind == CourseTaskKind.CollectibleQuota
                        ? CourseObjectiveIdentity.For(definition, definition.Tasks[i]) : null);
        }

        public void SetCalibrationReady(bool ready)
        {
            if (ready)
            {
                if (state == CourseState.Calibration) Transition(CourseState.Ready);
                return;
            }
            if (state == CourseState.Ready || IsCountdown(state))
            {
                Transition(CourseState.Calibration);
                return;
            }
            if (state == CourseState.Running) MarkNonRanked(CourseNonRankedReason.CalibrationLost);
        }

        public void BeginCountdown(bool rankedAttempt = true)
        {
            RequireState(CourseState.Ready);
            ResetAttempt();
            if (!rankedAttempt) MarkNonRanked(CourseNonRankedReason.ExplicitPractice);
            countdownStartedAt = ReadClock();
            Transition(CourseState.Countdown3);
        }

        public void Tick()
        {
            if (!IsCountdown(state) && state != CourseState.Running) return;
            var now = ReadClock();
            if (IsCountdown(state))
            {
                var countdownElapsed = Math.Max(0, now - countdownStartedAt);
                if (countdownElapsed >= 1 && state == CourseState.Countdown3)
                    Transition(CourseState.Countdown2);
                if (countdownElapsed >= 2 && state == CourseState.Countdown2)
                    Transition(CourseState.Countdown1);
                if (countdownElapsed >= 3 && state == CourseState.Countdown1)
                {
                    // Timing begins on the frame that can actually release flight. A hitch
                    // during the visible countdown cannot consume ranked course time while
                    // the controller is still authoritatively paused.
                    runStartedAt = now;
                    hasRunStarted = true;
                    hasPreviousPosition = false;
                    Transition(CourseState.Running);
                }
            }
            if (state == CourseState.Running
                && now - runStartedAt >= definition.TimeLimitSeconds)
                EndAttempt(false, CourseFailureReason.TimeLimit,
                    runStartedAt + definition.TimeLimitSeconds);
        }

        public void Observe(CourseObservation observation)
        {
            Tick();
            if (state != CourseState.Running) return;

            var invalidation = observation.InvalidationReasons;
            if (!observation.CalibrationValid) invalidation |= CourseNonRankedReason.CalibrationLost;
            if (!observation.TrackingValid || !observation.HasPosition
                || !CourseGeometry.IsFinite(observation.Position))
                invalidation |= CourseNonRankedReason.TrackingLost;
            if (observation.Paused) invalidation |= CourseNonRankedReason.Paused;
            if (observation.StreamingBlocked) invalidation |= CourseNonRankedReason.StreamingBlocked;
            // Ranking and geometric continuity are separate concerns. A long render frame,
            // control-mode change or weather change invalidates the leaderboard attempt,
            // but the controller's resulting world position is still authoritative. Keep
            // its swept segment so a practice run cannot permanently miss a one-way gate.
            var positionTrusted = (invalidation & PositionUntrustedReasons) == CourseNonRankedReason.None;
            if (invalidation != CourseNonRankedReason.None)
            {
                MarkNonRanked(invalidation);
                // Discrete events have already happened by the time the course observes
                // this frame. Keep them in a practice attempt so an objective moth or
                // trick cannot be consumed during a hitch/collision and soft-lock the
                // remaining course. Positional objectives reject only untrusted geometry.
                var kind = evaluators[taskIndex].Definition.Kind;
                if (!positionTrusted && kind != CourseTaskKind.CollectibleQuota
                    && kind != CourseTaskKind.Trick)
                    return;
            }

            var evaluation = evaluators[taskIndex].Evaluate(observation,
                positionTrusted && hasPreviousPosition, previousPosition);
            if (positionTrusted)
            {
                previousPosition = observation.Position;
                hasPreviousPosition = true;
            }
            else
                hasPreviousPosition = false;
            if (evaluation.Status == CourseTaskStatus.Failed)
            {
                EndAttempt(false, evaluation.FailureReason, lastClockSeconds);
                return;
            }
            if (evaluation.Status != CourseTaskStatus.Completed) return;
            taskIndex++;
            hasPreviousPosition = false;
            if (taskIndex == evaluators.Length)
                EndAttempt(true, CourseFailureReason.None, lastClockSeconds);
        }

        public void MarkNonRanked(CourseNonRankedReason reasons)
        {
            if (attemptEnded) return;
            nonRankedReasons |= reasons;
            if ((reasons & PositionUntrustedReasons) != CourseNonRankedReason.None)
                hasPreviousPosition = false;
        }

        public void Fail()
        {
            RequireState(CourseState.Running);
            EndAttempt(false, CourseFailureReason.Explicit, ReadClock());
        }

        public void ShowResults()
        {
            if (state != CourseState.Finished && state != CourseState.Failed)
                throw new InvalidOperationException("Results are available only after an attempt ends.");
            Transition(CourseState.Results);
        }

        public void BeginRest()
        {
            RequireState(CourseState.Results);
            Transition(CourseState.Rest);
        }

        public void CompleteRest(bool calibrationReady)
        {
            RequireState(CourseState.Rest);
            Transition(calibrationReady ? CourseState.Ready : CourseState.Calibration);
        }

        public CourseAttemptResult BuildResult(CourseResultKey key)
        {
            if (!attemptEnded) throw new InvalidOperationException("The course attempt has not ended.");
            if (!key.Matches(definition))
                throw new ArgumentException("Result key revisions do not match this course.", nameof(key));
            return new CourseAttemptResult(key, attemptCompleted, ElapsedSeconds, CompletedTasks,
                evaluators.Length, attemptCompleted && RankedEligible, nonRankedReasons,
                failureReason);
        }

        private void ResetAttempt()
        {
            taskIndex = 0;
            nonRankedReasons = CourseNonRankedReason.None;
            failureReason = CourseFailureReason.None;
            hasPreviousPosition = false;
            runStartedAt = 0;
            endedAt = 0;
            hasRunStarted = false;
            attemptEnded = false;
            attemptCompleted = false;
            for (var i = 0; i < evaluators.Length; i++) evaluators[i].Reset();
        }

        private void EndAttempt(bool completed, CourseFailureReason failure, double now)
        {
            attemptEnded = true;
            attemptCompleted = completed;
            failureReason = failure;
            endedAt = Math.Max(runStartedAt, now);
            Transition(completed ? CourseState.Finished : CourseState.Failed);
        }

        private double ReadClock()
        {
            var now = clock.NowSeconds;
            if (double.IsNaN(now) || double.IsInfinity(now))
            {
                MarkNonRanked(CourseNonRankedReason.ClockInvalid);
                return clockInitialized ? lastClockSeconds : 0;
            }
            if (!clockInitialized)
            {
                clockInitialized = true;
                lastClockSeconds = now;
                return now;
            }
            if (now < lastClockSeconds)
            {
                MarkNonRanked(CourseNonRankedReason.ClockWentBackward);
                return lastClockSeconds;
            }
            lastClockSeconds = now;
            return now;
        }

        private void Transition(CourseState next)
        {
            if (state == next) return;
            state = next;
            StateChanged?.Invoke(state);
        }

        private void RequireState(CourseState required)
        {
            if (state != required)
                throw new InvalidOperationException("Course state must be " + required + ".");
        }

        private static bool IsCountdown(CourseState value)
        {
            return value == CourseState.Countdown3 || value == CourseState.Countdown2
                || value == CourseState.Countdown1;
        }
    }
}
