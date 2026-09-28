using System;
using System.Collections.Generic;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.World;

namespace VoarVR.Gameplay
{
    [Serializable]
    public struct CoursePoint
    {
        [SerializeField] private double x;
        [SerializeField] private double y;
        [SerializeField] private double z;

        public double X => x;
        public double Y => y;
        public double Z => z;
        public LogicalPosition Logical => new LogicalPosition(x, y, z);
        public bool IsFinite => Finite(x) && Finite(y) && Finite(z);

        public CoursePoint(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public enum CourseTaskKind
    {
        Gate = 1,
        CollectibleQuota = 2,
        AltitudeBand = 3,
        Corridor = 4,
        Trick = 5,
        SupportedLanding = 6
    }

    [Serializable]
    public sealed class CourseTaskDefinition
    {
        [SerializeField] private string stableId;
        [SerializeField] private string label;
        [SerializeField] private CourseTaskKind kind;
        [SerializeField] private CoursePoint pointA;
        [SerializeField] private CoursePoint pointB;
        [SerializeField] private double radius;
        [SerializeField] private double minimumAltitude;
        [SerializeField] private double maximumAltitude;
        [SerializeField] private string collectibleId;
        [SerializeField] private CollectibleType collectibleType;
        [SerializeField] private int requiredCount;
        [SerializeField] private FlightTrick requiredTrick;
        [SerializeField] private int requiredSurfaceId;

        public string StableId => stableId;
        public string Label => label;
        public CourseTaskKind Kind => kind;
        public CoursePoint PointA => pointA;
        public CoursePoint PointB => pointB;
        public double Radius => radius;
        public double MinimumAltitude => minimumAltitude;
        public double MaximumAltitude => maximumAltitude;
        public string CollectibleId => collectibleId;
        public CollectibleType CollectibleType => collectibleType;
        public int RequiredCount => requiredCount;
        public FlightTrick RequiredTrick => requiredTrick;
        public int RequiredSurfaceId => requiredSurfaceId;

        private CourseTaskDefinition() { }

        public static CourseTaskDefinition Gate(string id, string label, CoursePoint center, double radius)
        {
            return Validated(new CourseTaskDefinition
            {
                stableId = id, label = label, kind = CourseTaskKind.Gate, pointA = center, radius = radius
            });
        }

        public static CourseTaskDefinition CollectibleQuota(string id, string label,
            CollectibleDefinition collectible, int requiredCount, CoursePoint start, CoursePoint finish)
        {
            if (collectible == null) throw new ArgumentNullException(nameof(collectible));
            return Validated(new CourseTaskDefinition
            {
                stableId = id, label = label, kind = CourseTaskKind.CollectibleQuota,
                collectibleId = collectible.StableId, collectibleType = collectible.Type,
                requiredCount = requiredCount, pointA = start, pointB = finish
            });
        }

        public static CourseTaskDefinition AltitudeBand(string id, string label, double minimum,
            double maximum, CoursePoint horizontalCenter, double radius)
        {
            return Validated(new CourseTaskDefinition
            {
                stableId = id, label = label, kind = CourseTaskKind.AltitudeBand,
                minimumAltitude = minimum, maximumAltitude = maximum,
                pointA = horizontalCenter, radius = radius
            });
        }

        public static CourseTaskDefinition Corridor(string id, string label, CoursePoint start,
            CoursePoint finish, double radius)
        {
            return Validated(new CourseTaskDefinition
            {
                stableId = id, label = label, kind = CourseTaskKind.Corridor,
                pointA = start, pointB = finish, radius = radius
            });
        }

        public static CourseTaskDefinition Trick(string id, string label, FlightTrick trick)
        {
            return Validated(new CourseTaskDefinition
            {
                stableId = id, label = label, kind = CourseTaskKind.Trick, requiredTrick = trick
            });
        }

        public static CourseTaskDefinition SupportedLanding(string id, string label,
            CoursePoint center, double radius, int requiredSurfaceId = 0)
        {
            return Validated(new CourseTaskDefinition
            {
                stableId = id, label = label, kind = CourseTaskKind.SupportedLanding,
                pointA = center, radius = radius, requiredSurfaceId = requiredSurfaceId
            });
        }

        public bool IsValid(out string error)
        {
            if (!StableIdContract.IsValid(stableId))
            {
                error = "Invalid stable course task ID.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(label))
            {
                error = "Course task label is required.";
                return false;
            }
            if (!Enum.IsDefined(typeof(CourseTaskKind), kind))
            {
                error = "Course task kind is invalid.";
                return false;
            }
            switch (kind)
            {
                case CourseTaskKind.Gate:
                case CourseTaskKind.SupportedLanding:
                    if (!pointA.IsFinite || !PositiveFinite(radius) || requiredSurfaceId < 0)
                    {
                        error = "Gate and landing tasks require a finite point and positive radius.";
                        return false;
                    }
                    break;
                case CourseTaskKind.CollectibleQuota:
                    if (!StableIdContract.IsValid(collectibleId) || collectibleType == CollectibleType.None
                        || !Enum.IsDefined(typeof(CollectibleType), collectibleType) || requiredCount <= 0
                        || requiredCount > SkyForaging.Capacity
                        || !pointA.IsFinite || !pointB.IsFinite
                        || requiredCount > 1 && CourseGeometry.SquaredDistance(pointA.Logical, pointB.Logical) < .0001)
                    {
                        error = "Collectible quota requires a stable collectible type and positive count.";
                        return false;
                    }
                    break;
                case CourseTaskKind.AltitudeBand:
                    if (!pointA.IsFinite || !Finite(minimumAltitude) || !Finite(maximumAltitude)
                        || minimumAltitude >= maximumAltitude || !PositiveFinite(radius))
                    {
                        error = "Altitude band requires finite ordered heights, center and radius.";
                        return false;
                    }
                    break;
                case CourseTaskKind.Corridor:
                {
                    var distanceSquared = CourseGeometry.SquaredDistance(pointA.Logical, pointB.Logical);
                    if (!pointA.IsFinite || !pointB.IsFinite || !PositiveFinite(radius)
                        || !Finite(distanceSquared) || distanceSquared < .0001)
                    {
                        error = "Corridor requires distinct finite endpoints and a positive radius.";
                        return false;
                    }
                    break;
                }
                case CourseTaskKind.Trick:
                    if (requiredTrick == FlightTrick.None || !Enum.IsDefined(typeof(FlightTrick), requiredTrick))
                    {
                        error = "Trick task requires a defined nonzero trick.";
                        return false;
                    }
                    break;
            }
            error = null;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out var error)) throw new ArgumentException(error, nameof(CourseTaskDefinition));
        }

        private static CourseTaskDefinition Validated(CourseTaskDefinition definition)
        {
            definition.ValidateOrThrow();
            return definition;
        }

        private static bool PositiveFinite(double value) => Finite(value) && value > 0;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    [Serializable]
    public sealed class CourseDefinition
    {
        [SerializeField] private string stableId;
        [SerializeField] private string title;
        [SerializeField] private int contentRevision;
        [SerializeField] private int scoringRevision;
        [SerializeField] private float expectedSeconds;
        [SerializeField] private float timeLimitSeconds;
        [SerializeField] private CourseTaskDefinition[] tasks;
        [NonSerialized] private IReadOnlyList<CourseTaskDefinition> readOnlyTasks;

        public string StableId => stableId;
        public string Title => title;
        public int ContentRevision => contentRevision;
        public int ScoringRevision => scoringRevision;
        public float ExpectedSeconds => expectedSeconds;
        public float TimeLimitSeconds => timeLimitSeconds;
        public IReadOnlyList<CourseTaskDefinition> Tasks => readOnlyTasks
            ?? (readOnlyTasks = Array.AsReadOnly(tasks ?? Array.Empty<CourseTaskDefinition>()));

        public CourseDefinition(string stableId, string title, int contentRevision, int scoringRevision,
            float expectedSeconds, float timeLimitSeconds, params CourseTaskDefinition[] tasks)
        {
            this.stableId = stableId;
            this.title = title;
            this.contentRevision = contentRevision;
            this.scoringRevision = scoringRevision;
            this.expectedSeconds = expectedSeconds;
            this.timeLimitSeconds = timeLimitSeconds;
            this.tasks = tasks == null ? null : (CourseTaskDefinition[])tasks.Clone();
            readOnlyTasks = this.tasks == null ? null : Array.AsReadOnly(this.tasks);
            ValidateOrThrow();
        }

        public bool IsValid(out string error)
        {
            if (!StableIdContract.IsValid(stableId))
            {
                error = "Invalid stable course ID.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(title) || contentRevision <= 0 || scoringRevision <= 0)
            {
                error = "Course title and positive content/scoring revisions are required.";
                return false;
            }
            if (!Finite(expectedSeconds) || expectedSeconds <= 0 || !Finite(timeLimitSeconds)
                || timeLimitSeconds < expectedSeconds)
            {
                error = "Course timing must be finite and the time limit must cover the expected time.";
                return false;
            }
            if (tasks == null || tasks.Length == 0)
            {
                error = "Course requires at least one ordered task.";
                return false;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var task in tasks)
            {
                if (task == null)
                {
                    error = "Course tasks cannot be null.";
                    return false;
                }
                if (!task.IsValid(out error)) return false;
                if (!ids.Add(task.StableId))
                {
                    error = "Course task IDs must be unique within a course.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out var error)) throw new ArgumentException(error, nameof(CourseDefinition));
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // Persistence is deliberately outside this type; this is only the comparison-key contract.
    public readonly struct CourseResultKey : IEquatable<CourseResultKey>
    {
        public const string Prefix = "VoarVR.CourseResult.v1";
        public readonly string CourseId;
        public readonly int ContentRevision;
        public readonly int ScoringRevision;
        public readonly string CharacterId;
        public readonly string ControlModeId;
        public readonly string WindModeId;
        public readonly string AssistanceProfileId;

        public CourseResultKey(CourseDefinition course, string characterId, string controlModeId,
            string windModeId, string assistanceProfileId)
        {
            if (course == null) throw new ArgumentNullException(nameof(course));
            CourseId = course.StableId;
            ContentRevision = course.ContentRevision;
            ScoringRevision = course.ScoringRevision;
            CharacterId = RequireStable(characterId, nameof(characterId));
            ControlModeId = RequireStable(controlModeId, nameof(controlModeId));
            WindModeId = RequireStable(windModeId, nameof(windModeId));
            AssistanceProfileId = RequireStable(assistanceProfileId, nameof(assistanceProfileId));
        }

        public bool IsValid => StableIdContract.IsValid(CourseId) && ContentRevision > 0
            && ScoringRevision > 0 && StableIdContract.IsValid(CharacterId)
            && StableIdContract.IsValid(ControlModeId) && StableIdContract.IsValid(WindModeId)
            && StableIdContract.IsValid(AssistanceProfileId);

        public string StorageKey
        {
            get
            {
                if (!IsValid) throw new InvalidOperationException("Course result key is not initialized.");
                return Prefix + "/" + CourseId + "/content-" + ContentRevision
                    + "/score-" + ScoringRevision + "/" + CharacterId + "/" + ControlModeId
                    + "/" + WindModeId + "/" + AssistanceProfileId;
            }
        }

        public bool Matches(CourseDefinition course) => course != null
            && string.Equals(CourseId, course.StableId, StringComparison.Ordinal)
            && ContentRevision == course.ContentRevision && ScoringRevision == course.ScoringRevision;

        public bool Equals(CourseResultKey other) => ContentRevision == other.ContentRevision
            && ScoringRevision == other.ScoringRevision
            && string.Equals(CourseId, other.CourseId, StringComparison.Ordinal)
            && string.Equals(CharacterId, other.CharacterId, StringComparison.Ordinal)
            && string.Equals(ControlModeId, other.ControlModeId, StringComparison.Ordinal)
            && string.Equals(WindModeId, other.WindModeId, StringComparison.Ordinal)
            && string.Equals(AssistanceProfileId, other.AssistanceProfileId, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is CourseResultKey other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (CourseId?.GetHashCode() ?? 0);
                hash = hash * 31 + ContentRevision;
                hash = hash * 31 + ScoringRevision;
                hash = hash * 31 + (CharacterId?.GetHashCode() ?? 0);
                hash = hash * 31 + (ControlModeId?.GetHashCode() ?? 0);
                hash = hash * 31 + (WindModeId?.GetHashCode() ?? 0);
                hash = hash * 31 + (AssistanceProfileId?.GetHashCode() ?? 0);
                return hash;
            }
        }

        private static string RequireStable(string value, string parameter)
        {
            if (!StableIdContract.IsValid(value)) throw new ArgumentException("Invalid stable result-key component.", parameter);
            return value;
        }
    }

    public static class CourseCatalog
    {
        private static readonly IReadOnlyList<CourseDefinition> all = Array.AsReadOnly(new[]
        {
            new CourseDefinition("moth-line", "Moth Line", 4, 2, 24, 30,
                CourseTaskDefinition.Gate("launch-gate", "Launch gate", P(0, 10, 18), 4),
                CourseTaskDefinition.CollectibleQuota("sun-moths", "Catch five Sun Moths", CollectibleCatalog.SunMoth, 5,
                    P(0, 12, 32), P(0, 10, 78)),
                CourseTaskDefinition.Gate("finish-gate", "Finish gate", P(0, 9, 95), 5),
                CourseTaskDefinition.SupportedLanding("finish-perch", "Land on the finish perch", P(0, 2, 112), 6,
                    CourseSurfaceIds.MothLineFinish)),

            new CourseDefinition("canopy-weave", "Canopy Weave", 2, 1, 20, 30,
                CourseTaskDefinition.Gate("left-gap", "Left canopy gap", P(-12, 18, 22), 4),
                CourseTaskDefinition.Gate("right-gap", "Right canopy gap", P(12, 20, 43), 4),
                CourseTaskDefinition.Gate("high-gap", "High canopy gap", P(-10, 27, 65), 4),
                CourseTaskDefinition.Gate("canopy-exit", "Canopy exit", P(8, 18, 88), 5)),

            new CourseDefinition("ruin-windows", "Ruin Windows", 2, 1, 23, 30,
                CourseTaskDefinition.Gate("first-window", "First ruin window", P(0, 16, 24), 3.5),
                CourseTaskDefinition.Corridor("gallery", "Stay inside the ruin gallery", P(0, 16, 24), P(0, 16, 78), 6),
                CourseTaskDefinition.Gate("last-window", "Last ruin window", P(0, 16, 92), 3.5),
                CourseTaskDefinition.SupportedLanding("ruin-perch", "Land beyond the ruins", P(0, 2, 108), 6,
                    CourseSurfaceIds.RuinFinish)),

            new CourseDefinition("thermal-ladder", "Thermal Ladder", 2, 1, 25, 30,
                CourseTaskDefinition.AltitudeBand("low-band", "Enter the low lift band", 14, 25, P(0, 0, 30), 18),
                CourseTaskDefinition.AltitudeBand("middle-band", "Climb through the middle band", 18, 32, P(8, 0, 58), 18),
                CourseTaskDefinition.AltitudeBand("high-band", "Reach the high band", 22, 38, P(-6, 0, 84), 18),
                CourseTaskDefinition.Gate("ladder-exit", "Dive through the exit", P(0, 22, 108), 6)),

            new CourseDefinition("trick-and-perch", "Trick and Perch", 2, 2, 26, 30,
                CourseTaskDefinition.Gate("trick-entry", "Enter the trick zone", P(0, 18, 20), 7),
                CourseTaskDefinition.Trick("inverted-hold", "Hold inverted", FlightTrick.InvertedHold),
                CourseTaskDefinition.CollectibleQuota("crown-moth", "Catch the Crown Moth", CollectibleCatalog.CrownMoth, 1,
                    P(-4, 14, 38), P(4, 10, 54)),
                CourseTaskDefinition.SupportedLanding("final-perch", "Land on the final perch", P(0, 3, 72), 6,
                    CourseSurfaceIds.TrickFinish))
        });

        public static IReadOnlyList<CourseDefinition> All => all;

        public static CourseDefinition Find(string stableId)
        {
            if (stableId == null) return null;
            foreach (var course in all)
                if (string.Equals(course.StableId, stableId, StringComparison.Ordinal)) return course;
            return null;
        }

        private static CoursePoint P(double x, double y, double z) => new CoursePoint(x, y, z);
    }

    // Stable physics IDs bind landing objectives to their authored finish surfaces.
    public static class CourseSurfaceIds
    {
        public const int MothLineFinish = 9610;
        public const int RuinFinish = 9630;
        public const int TrickFinish = 9650;
    }

    public static class CourseObjectiveIdentity
    {
        public static string For(CourseDefinition course, CourseTaskDefinition task)
        {
            if (course == null) throw new ArgumentNullException(nameof(course));
            if (task == null) throw new ArgumentNullException(nameof(task));
            return course.StableId + ".v" + course.ContentRevision + "." + task.StableId;
        }
    }
}
