using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoarVR.Gameplay
{
    [Serializable]
    public sealed class CourseLeaderboardEntry
    {
        public string ProfileId = "";
        public string ProfileName = "";
        public string CharacterId = "";
        public string CompletedUtc = "";
        public double Seconds;
    }

    [Serializable]
    internal sealed class CourseLeaderboardEnvelope
    {
        public int Version = 1;
        public CourseLeaderboardEntry[] Entries = Array.Empty<CourseLeaderboardEntry>();
    }

    public interface ICourseLeaderboardStorage
    {
        string Load(string key);
        bool Save(string key,string json);
    }

    public sealed class PlayerPrefsCourseLeaderboardStorage:ICourseLeaderboardStorage
    {
        public string Load(string key)=>PlayerPrefs.GetString(key,string.Empty);
        public bool Save(string key,string json)
        {
            try { PlayerPrefs.SetString(key,json);PlayerPrefs.Save();return true; }
            catch(Exception){return false;}
        }
    }

    // A compact, local leaderboard. Comparison keys include course/scoring revisions,
    // species, control mode, weather and assistance so unlike runs are never mixed.
    public sealed class CourseLeaderboardStore
    {
#if UNITY_EDITOR
        // Match journey/foraging isolation for scene-created course directors and menus.
        public static Func<ICourseLeaderboardStorage> EditorStorageOverride;
#endif
        public const int MaximumEntries=20;
        private readonly ICourseLeaderboardStorage storage;
        public string LastError {get;private set;}="";

        public CourseLeaderboardStore(ICourseLeaderboardStorage storage=null)
        {this.storage=storage??CreateDefaultStorage();}

        private static ICourseLeaderboardStorage CreateDefaultStorage()
        {
#if UNITY_EDITOR
            if(EditorStorageOverride!=null)return EditorStorageOverride();
#endif
            return new PlayerPrefsCourseLeaderboardStorage();
        }

        public CourseLeaderboardEntry[] Load(CourseResultKey key)
        {
            LastError="";
            try
            {
                string json=storage.Load(key.StorageKey);
                if(string.IsNullOrEmpty(json))return Array.Empty<CourseLeaderboardEntry>();
                var envelope=JsonUtility.FromJson<CourseLeaderboardEnvelope>(json);
                if(envelope==null || envelope.Version!=1 || envelope.Entries==null)
                {LastError="This leaderboard is unreadable or from a newer version.";return Array.Empty<CourseLeaderboardEntry>();}
                var valid=new List<CourseLeaderboardEntry>();
                foreach(var entry in envelope.Entries)
                {
                    if(!Valid(entry))
                    {
                        LastError="This leaderboard contains invalid data and was left unchanged.";
                        return Array.Empty<CourseLeaderboardEntry>();
                    }
                    valid.Add(entry);
                }
                TrimPreservingPersonalBests(valid);
                return valid.ToArray();
            }
            catch(Exception error){LastError="Could not read the local leaderboard: "+error.Message;return Array.Empty<CourseLeaderboardEntry>();}
        }

        public bool Record(CourseAttemptResult result,string profileId,string profileName,string characterId,DateTime completedUtc)
        {
            LastError="";
            if(!result.Completed || !result.Ranked)return false;
            if(!FlightSessionSummary.SafeId(profileId) || string.IsNullOrWhiteSpace(profileName)
                || !FlightSessionSummary.SafeId(characterId) || completedUtc.Kind!=DateTimeKind.Utc
                || double.IsNaN(result.DurationSeconds) || double.IsInfinity(result.DurationSeconds)
                || result.DurationSeconds<=0)return false;
            var loaded=Load(result.Key);
            // Never replace unreadable/future data with an apparently fresh board.
            if(!string.IsNullOrEmpty(LastError))return false;
            var entries=new List<CourseLeaderboardEntry>(loaded);
            entries.Add(new CourseLeaderboardEntry{ProfileId=profileId,ProfileName=profileName.Trim(),
                CharacterId=characterId,CompletedUtc=completedUtc.ToString("o"),Seconds=result.DurationSeconds});
            TrimPreservingPersonalBests(entries);
            var envelope=new CourseLeaderboardEnvelope{Entries=entries.ToArray()};
            if(storage.Save(result.Key.StorageKey,JsonUtility.ToJson(envelope)))return true;
            LastError="Could not save the local leaderboard.";return false;
        }

        public double PersonalBest(CourseResultKey key,string profileId)
        {
            double best=double.PositiveInfinity;
            foreach(var entry in Load(key))if(entry.ProfileId==profileId)best=Math.Min(best,entry.Seconds);
            return best;
        }

        private static bool Valid(CourseLeaderboardEntry entry)=>entry!=null
            && FlightSessionSummary.SafeId(entry.ProfileId) && FlightSessionSummary.SafeId(entry.CharacterId)
            && !string.IsNullOrWhiteSpace(entry.ProfileName) && FlightSessionSummary.ValidUtc(entry.CompletedUtc)
            && !double.IsNaN(entry.Seconds) && !double.IsInfinity(entry.Seconds) && entry.Seconds>0;
        private static int Compare(CourseLeaderboardEntry a,CourseLeaderboardEntry b)
        {int time=a.Seconds.CompareTo(b.Seconds);return time!=0?time:string.CompareOrdinal(a.CompletedUtc,b.CompletedUtc);}

        private static void TrimPreservingPersonalBests(List<CourseLeaderboardEntry> entries)
        {
            entries.Sort(Compare);
            if(entries.Count<=MaximumEntries)return;
            // The first twenty remain the shared leaderboard. Also retain the fastest
            // run for every local profile outside it so a busy household can always
            // see and improve each player's own record.
            var kept=new List<CourseLeaderboardEntry>(MaximumEntries+32);
            var representedProfiles=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<MaximumEntries;i++)
            {kept.Add(entries[i]);representedProfiles.Add(entries[i].ProfileId);}
            for(int i=MaximumEntries;i<entries.Count;i++)
                if(representedProfiles.Add(entries[i].ProfileId))kept.Add(entries[i]);
            entries.Clear();entries.AddRange(kept);
        }
    }
}
