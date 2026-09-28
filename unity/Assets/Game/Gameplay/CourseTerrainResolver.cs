using System;
using VoarVR.World;

namespace VoarVR.Gameplay
{
    // Catalog coordinates describe clearance above a flat authoring floor. Resolve them
    // once into logical world coordinates so evaluation, route markers and colliders all
    // agree with the generated terrain for the selected world seed.
    public static class CourseTerrainResolver
    {
        public const double LandingSurfaceOffset = .25d;
        public const double LandingPadHalfExtent = 5d;
        public const double LandingTerrainSampleStep = .5d;

        public static CourseDefinition Resolve(CourseDefinition source, int worldSeed)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var tasks = new CourseTaskDefinition[source.Tasks.Count];
            for (var i = 0; i < tasks.Length; i++)
                tasks[i] = ResolveTask(source.Tasks[i], worldSeed);
            return new CourseDefinition(source.StableId, source.Title, source.ContentRevision,
                source.ScoringRevision, source.ExpectedSeconds, source.TimeLimitSeconds, tasks);
        }

        private static CourseTaskDefinition ResolveTask(CourseTaskDefinition task, int seed)
        {
            switch (task.Kind)
            {
                case CourseTaskKind.Gate:
                    return CourseTaskDefinition.Gate(task.StableId, task.Label,
                        AboveTerrain(task.PointA, seed, task.PointA.Y), task.Radius);
                case CourseTaskKind.SupportedLanding:
                    return CourseTaskDefinition.SupportedLanding(task.StableId, task.Label,
                        LandingAboveTerrain(task.PointA, seed), task.Radius, task.RequiredSurfaceId);
                case CourseTaskKind.AltitudeBand:
                {
                    var terrain = WorldTerrain.Elevation(seed, task.PointA.X, task.PointA.Z);
                    return CourseTaskDefinition.AltitudeBand(task.StableId, task.Label,
                        task.MinimumAltitude + terrain, task.MaximumAltitude + terrain,
                        new CoursePoint(task.PointA.X, terrain, task.PointA.Z), task.Radius);
                }
                case CourseTaskKind.Corridor:
                    return CourseTaskDefinition.Corridor(task.StableId, task.Label,
                        AboveTerrain(task.PointA, seed, task.PointA.Y),
                        AboveTerrain(task.PointB, seed, task.PointB.Y), task.Radius);
                case CourseTaskKind.CollectibleQuota:
                    return CourseTaskDefinition.CollectibleQuota(task.StableId, task.Label,
                        CollectibleCatalog.Find(task.CollectibleId), task.RequiredCount,
                        AboveTerrain(task.PointA, seed, task.PointA.Y),
                        AboveTerrain(task.PointB, seed, task.PointB.Y));
                case CourseTaskKind.Trick:
                    return CourseTaskDefinition.Trick(task.StableId, task.Label, task.RequiredTrick);
                default:
                    throw new ArgumentOutOfRangeException(nameof(task), task.Kind, "Unsupported course task kind.");
            }
        }

        private static CoursePoint AboveTerrain(CoursePoint point, int seed, double clearance)
        {
            return new CoursePoint(point.X,
                WorldTerrain.Elevation(seed, point.X, point.Z) + clearance, point.Z);
        }

        private static CoursePoint LandingAboveTerrain(CoursePoint point, int seed)
        {
            double maximum = double.NegativeInfinity;
            for (double z = -LandingPadHalfExtent; z <= LandingPadHalfExtent; z += LandingTerrainSampleStep)
                for (double x = -LandingPadHalfExtent; x <= LandingPadHalfExtent; x += LandingTerrainSampleStep)
                    maximum = Math.Max(maximum, WorldTerrain.Elevation(seed, point.X + x, point.Z + z));
            return new CoursePoint(point.X, maximum + LandingSurfaceOffset, point.Z);
        }
    }

    // Procedural landmarks are omitted around the gold centerline while a course is active.
    // The authored course frames remain, but seed-dependent scenery cannot make a ranked line
    // unsafe for one bird or one world seed. Eighteen metres covers the largest generated
    // landmark footprint while retaining scenery immediately outside the route.
    public static class CourseRouteReservation
    {
        public const double LandmarkCenterClearanceMeters = 18d;

        public static bool IsActiveRouteReserved(double logicalX, double logicalZ)
        {
            if (ActivitySelection.Chosen != FlightActivity.ObstacleCourse) return false;
            return IsRouteReserved(CourseCatalog.Find(ActivitySelection.ChosenCourseId), logicalX, logicalZ);
        }

        public static bool IsRouteReserved(CourseDefinition course, double logicalX, double logicalZ,
            double clearance = LandmarkCenterClearanceMeters)
        {
            if (course == null || clearance <= 0 || double.IsNaN(logicalX) || double.IsNaN(logicalZ)) return false;
            var previous = new CoursePoint(0, 0, 0);
            foreach (var task in course.Tasks)
            {
                if (task.Kind == CourseTaskKind.Corridor)
                {
                    if (InsideSegment(previous, task.PointA, logicalX, logicalZ, clearance)
                        || InsideSegment(task.PointA, task.PointB, logicalX, logicalZ, clearance)) return true;
                    previous = task.PointB;
                }
                else if (task.Kind == CourseTaskKind.CollectibleQuota)
                {
                    if (InsideSegment(previous, task.PointA, logicalX, logicalZ, clearance)
                        || InsideSegment(task.PointA, task.PointB, logicalX, logicalZ, clearance)) return true;
                    previous = task.PointB;
                }
                else if (HasRouteAnchor(task.Kind))
                {
                    if (InsideSegment(previous, task.PointA, logicalX, logicalZ, clearance)) return true;
                    previous = task.PointA;
                }
            }
            return false;
        }

        private static bool HasRouteAnchor(CourseTaskKind kind) => kind == CourseTaskKind.Gate
            || kind == CourseTaskKind.AltitudeBand || kind == CourseTaskKind.SupportedLanding;

        private static bool InsideSegment(CoursePoint start, CoursePoint end, double x, double z,
            double clearance)
        {
            double dx = end.X - start.X, dz = end.Z - start.Z;
            double lengthSquared = dx * dx + dz * dz;
            double t = lengthSquared <= .000001 ? 0 : ((x - start.X) * dx + (z - start.Z) * dz) / lengthSquared;
            t = Math.Max(0, Math.Min(1, t));
            double offsetX = x - (start.X + dx * t), offsetZ = z - (start.Z + dz * t);
            return offsetX * offsetX + offsetZ * offsetZ <= clearance * clearance;
        }
    }

    public static class CourseCollectibleLayout
    {
        public const double MinimumGroundClearance = 5d;

        public static LogicalPosition[] Build(CourseTaskDefinition resolvedQuota, int worldSeed)
        {
            if (resolvedQuota == null || resolvedQuota.Kind != CourseTaskKind.CollectibleQuota
                || resolvedQuota.RequiredCount < 1 || resolvedQuota.RequiredCount > SkyForaging.Capacity)
                throw new ArgumentException("A bounded resolved collectible quota is required.", nameof(resolvedQuota));
            var result = new LogicalPosition[resolvedQuota.RequiredCount];
            for (int i = 0; i < result.Length; i++)
            {
                double t = result.Length == 1 ? .5d : (double)i / (result.Length - 1);
                double x = Lerp(resolvedQuota.PointA.X, resolvedQuota.PointB.X, t);
                double z = Lerp(resolvedQuota.PointA.Z, resolvedQuota.PointB.Z, t);
                double y = Math.Max(Lerp(resolvedQuota.PointA.Y, resolvedQuota.PointB.Y, t),
                    WorldTerrain.Elevation(worldSeed, x, z) + MinimumGroundClearance);
                result[i] = new LogicalPosition(x, y, z);
            }
            return result;
        }

        private static double Lerp(double from, double to, double t) => from + (to - from) * t;
    }
}
