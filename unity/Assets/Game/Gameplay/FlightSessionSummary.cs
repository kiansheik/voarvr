using System;

namespace VoarVR.Gameplay
{
    [Serializable]
    public sealed class FlightSessionContext
    {
        public string SessionId = "";
        public string ProfileId = "";
        public string StartedUtc = "";
        public string BuildId = "";
        public string CharacterId = "";
        public string ActivityId = "";
        public string ContentId = "";
        public int ContentRevision;
        public int ScoringRevision;

        public bool IsValid()
        {
            return FlightSessionSummary.SafeId(SessionId) && FlightSessionSummary.SafeId(ProfileId)
                && FlightSessionSummary.ValidUtc(StartedUtc) && ContentRevision >= 0 && ScoringRevision >= 0;
        }
    }

    [Serializable]
    public sealed class FlightSessionCollectibleTally
    {
        public string CollectibleId = "";
        public int Count;
        public int Value;

        public bool IsValid() => FlightSessionSummary.SafeId(CollectibleId) && Count > 0 && Value >= 0;
    }

    [Serializable]
    public sealed class FlightSessionMetrics
    {
        // Primary time categories are mutually exclusive and sum to ObservedSeconds.
        public double ObservedSeconds;
        public double ActiveHandSeconds;
        public double QuietAirborneSeconds;
        public double SupportedPerchSeconds;
        public double PausedSeconds;
        public double ExcludedSeconds;

        // Coverage categories may overlap each other and the primary pause/perch categories.
        public double TrackingGapSeconds;
        public double StreamingBlockedSeconds;
        public double InvalidSampleSeconds;
        public double WalkingActiveSeconds;

        public double FlightHorizontalMeters;
        public double FlightDistanceMeters;
        public double WalkingHorizontalMeters;
        public double WalkingDistanceMeters;
        public double GrossAscentMeters;
        public double GrossDescentMeters;
        public double MaxGroundSpeedMps;
        public double MaxAirSpeedMps;

        // Wingbeats are estimated from a tracked up-to-down controller-velocity reversal.
        public int EstimatedWingbeats;
        public int CatchCount;
        public int CollectibleValue;
        public int TrickCount;
        public int CollisionCount;
        public int LandingCount;
        public int TakeoffCount;

        public bool IsValid()
        {
            double primary = ActiveHandSeconds + QuietAirborneSeconds + SupportedPerchSeconds + PausedSeconds + ExcludedSeconds;
            double tolerance = Math.Max(.001, ObservedSeconds * .00001);
            return NonnegativeFinite(ObservedSeconds) && NonnegativeFinite(ActiveHandSeconds)
                && NonnegativeFinite(QuietAirborneSeconds) && NonnegativeFinite(SupportedPerchSeconds)
                && NonnegativeFinite(PausedSeconds) && NonnegativeFinite(ExcludedSeconds)
                && Math.Abs(primary - ObservedSeconds) <= tolerance
                && NonnegativeFinite(TrackingGapSeconds) && NonnegativeFinite(StreamingBlockedSeconds)
                && NonnegativeFinite(InvalidSampleSeconds) && NonnegativeFinite(WalkingActiveSeconds)
                && WalkingActiveSeconds <= SupportedPerchSeconds + tolerance
                && NonnegativeFinite(FlightHorizontalMeters)
                && NonnegativeFinite(FlightDistanceMeters) && NonnegativeFinite(WalkingHorizontalMeters)
                && NonnegativeFinite(WalkingDistanceMeters) && NonnegativeFinite(GrossAscentMeters)
                && NonnegativeFinite(GrossDescentMeters) && NonnegativeFinite(MaxGroundSpeedMps)
                && NonnegativeFinite(MaxAirSpeedMps) && FlightDistanceMeters + tolerance >= FlightHorizontalMeters
                && WalkingDistanceMeters + tolerance >= WalkingHorizontalMeters
                && EstimatedWingbeats >= 0 && CatchCount >= 0 && CollectibleValue >= 0 && TrickCount >= 0
                && CollisionCount >= 0 && LandingCount >= 0 && TakeoffCount >= 0;
        }

        private static bool NonnegativeFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }

    // Compact product history. Raw development telemetry remains a separate optional artifact.
    [Serializable]
    public sealed class FlightSessionSummary
    {
        public const int CurrentVersion = 1;
        public int Version = CurrentVersion;
        public FlightSessionContext Context = new FlightSessionContext();
        public string EndedUtc = "";
        public string EndReason = "";
        public bool Finalized;
        public FlightSessionMetrics Metrics = new FlightSessionMetrics();
        public FlightSessionCollectibleTally[] Collectibles = Array.Empty<FlightSessionCollectibleTally>();

        public bool IsValid()
        {
            if (Version != CurrentVersion || Context == null || !Context.IsValid() || Metrics == null || !Metrics.IsValid()
                || Collectibles == null || Finalized && (!ValidUtc(EndedUtc) || string.IsNullOrWhiteSpace(EndReason)
                    || EndReason.Length > 64) || !Finalized && (!string.IsNullOrEmpty(EndedUtc)
                    || !string.IsNullOrEmpty(EndReason))) return false;
            int catches = 0, value = 0;
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var tally in Collectibles)
            {
                if (tally == null || !tally.IsValid() || !ids.Add(tally.CollectibleId)) return false;
                try { catches = checked(catches + tally.Count); value = checked(value + tally.Value); }
                catch (OverflowException) { return false; }
            }
            return catches == Metrics.CatchCount && value == Metrics.CollectibleValue;
        }

        internal static bool ValidUtc(string value)
        {
            bool explicitUtc = !string.IsNullOrWhiteSpace(value)
                && (value.EndsWith("Z", StringComparison.OrdinalIgnoreCase)
                    || value.EndsWith("+00:00", StringComparison.Ordinal));
            return explicitUtc && DateTimeOffset.TryParse(value,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AllowWhiteSpaces, out var parsed)
                && parsed.Offset == TimeSpan.Zero;
        }

        internal static bool SafeId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 96) return false;
            foreach (char c in value)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9')
                    && c != '-' && c != '_') return false;
            return true;
        }
    }
}
