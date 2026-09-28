using System;

namespace VoarVR.Gameplay
{
    public enum ObjectiveProgressUnit
    {
        None = 0,
        Count = 1,
        Gates = 2,
        Metres = 3,
        Seconds = 4,
        Points = 5,
        Path = 6
    }

    public enum ObjectiveProgressState
    {
        Unavailable = 0,
        Active = 1,
        Completed = 2,
        Failed = 3
    }

    // A presentation-neutral contract shared by journeys, courses and future activities.
    public readonly struct ObjectiveProgressSnapshot
    {
        public readonly string TaskId;
        public readonly string Label;
        public readonly int StageIndex;
        public readonly int StageCount;
        public readonly double Current;
        public readonly double Required;
        public readonly ObjectiveProgressUnit Unit;
        public readonly ObjectiveProgressState State;

        public static ObjectiveProgressSnapshot Unavailable => default;
        public bool IsAvailable => State != ObjectiveProgressState.Unavailable;
        public int DisplayStage => IsAvailable ? StageIndex + 1 : 0;
        public double Normalized => !IsAvailable || Required <= 0 ? 0
            : State == ObjectiveProgressState.Completed ? 1 : Math.Max(0, Math.Min(1, Current / Required));

        public ObjectiveProgressSnapshot(string taskId, string label, int stageIndex, int stageCount,
            double current, double required, ObjectiveProgressUnit unit, ObjectiveProgressState state)
        {
            TaskId = taskId;
            Label = label;
            StageIndex = stageIndex;
            StageCount = stageCount;
            Current = current;
            Required = required;
            Unit = unit;
            State = state;
            ValidateOrThrow();
        }

        public void ValidateOrThrow()
        {
            if (!StableIdContract.IsValid(TaskId)) throw new ArgumentException("Invalid stable task ID.", nameof(TaskId));
            if (string.IsNullOrWhiteSpace(Label)) throw new ArgumentException("Objective label is required.", nameof(Label));
            if (StageCount <= 0 || StageIndex < 0 || StageIndex >= StageCount)
                throw new ArgumentOutOfRangeException(nameof(StageIndex), "Objective stage must be inside its stage count.");
            if (double.IsNaN(Current) || double.IsInfinity(Current) || Current < 0
                || double.IsNaN(Required) || double.IsInfinity(Required) || Required <= 0)
                throw new ArgumentOutOfRangeException(nameof(Current), "Objective values must be finite and nonnegative with a positive requirement.");
            if (!Enum.IsDefined(typeof(ObjectiveProgressUnit), Unit) || Unit == ObjectiveProgressUnit.None)
                throw new ArgumentOutOfRangeException(nameof(Unit));
            if (!Enum.IsDefined(typeof(ObjectiveProgressState), State) || State == ObjectiveProgressState.Unavailable)
                throw new ArgumentOutOfRangeException(nameof(State));
        }
    }
}
