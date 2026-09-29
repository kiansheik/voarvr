using System;
using System.Collections.Generic;
using NUnit.Framework;
using VoarVR.Gameplay;

namespace VoarVR.Tests
{
    public sealed class CourseLeaderboardStoreTests
    {
        private sealed class MemoryStorage:ICourseLeaderboardStorage
        {
            private readonly Dictionary<string,string> values=new Dictionary<string,string>();
            public int SaveCount {get;private set;}
            public string Load(string key)=>values.TryGetValue(key,out var value)?value:string.Empty;
            public bool Save(string key,string json){SaveCount++;values[key]=json;return true;}
            public void Seed(string key,string json)=>values[key]=json;
        }

        [Test]
        public void EditorDefaultLeaderboardKeepsRecordsOutOfPlayerPrefs()
        {
            var prior=CourseLeaderboardStore.EditorStorageOverride;
            var isolated=new MemoryStorage();var explicitStorage=new MemoryStorage();
            var key=new CourseResultKey(CourseCatalog.Find("moth-line"),"duck","beginner","assisted","assisted");
            bool existed=UnityEngine.PlayerPrefs.HasKey(key.StorageKey);
            string original=UnityEngine.PlayerPrefs.GetString(key.StorageKey,string.Empty);
            try
            {
                CourseLeaderboardStore.EditorStorageOverride=()=>isolated;
                var defaultStore=new CourseLeaderboardStore();
                Assert.That(defaultStore.Record(Result(key,17),"review","Review","duck",DateTime.UtcNow),Is.True);
                Assert.That(new CourseLeaderboardStore().PersonalBest(key,"review"),Is.EqualTo(17));
                var explicitStore=new CourseLeaderboardStore(explicitStorage);
                Assert.That(explicitStore.Load(key),Is.Empty);
                Assert.That(explicitStore.Record(Result(key,16),"explicit","Explicit","duck",DateTime.UtcNow),Is.True);
                Assert.That(isolated.SaveCount,Is.EqualTo(1));
                Assert.That(explicitStorage.SaveCount,Is.EqualTo(1));
                Assert.That(UnityEngine.PlayerPrefs.HasKey(key.StorageKey),Is.EqualTo(existed));
                Assert.That(UnityEngine.PlayerPrefs.GetString(key.StorageKey,string.Empty),Is.EqualTo(original));
            }
            finally {CourseLeaderboardStore.EditorStorageOverride=prior;}
        }

        [Test]
        public void RankedResultsSortAndKeepIndependentPlayerBests()
        {
            var course=CourseCatalog.Find("moth-line");
            var key=new CourseResultKey(course,"duck","beginner","assisted","assisted");
            var store=new CourseLeaderboardStore(new MemoryStorage());
            Assert.That(store.Record(Result(key,19.5),"first","Ana","duck",DateTime.UtcNow),Is.True);
            Assert.That(store.Record(Result(key,17.25),"second","Beto","duck",DateTime.UtcNow),Is.True);
            Assert.That(store.Record(Result(key,18),"first","Ana","duck",DateTime.UtcNow),Is.True);
            var entries=store.Load(key);
            Assert.That(entries.Length,Is.EqualTo(3));
            Assert.That(entries[0].ProfileName,Is.EqualTo("Beto"));
            Assert.That(store.PersonalBest(key,"first"),Is.EqualTo(18).Within(.001));
            Assert.That(store.PersonalBest(key,"missing"),Is.EqualTo(double.PositiveInfinity));
        }

        [Test]
        public void PracticeAndFailedRunsDoNotEnterLeaderboard()
        {
            var course=CourseCatalog.Find("moth-line");
            var key=new CourseResultKey(course,"duck","beginner","assisted","assisted");
            var store=new CourseLeaderboardStore(new MemoryStorage());
            var practice=new CourseAttemptResult(key,true,15,4,4,false,CourseNonRankedReason.Paused,CourseFailureReason.None);
            var failed=new CourseAttemptResult(key,false,15,2,4,false,CourseNonRankedReason.None,CourseFailureReason.Explicit);
            Assert.That(store.Record(practice,"first","Ana","duck",DateTime.UtcNow),Is.False);
            Assert.That(store.Record(failed,"first","Ana","duck",DateTime.UtcNow),Is.False);
            Assert.That(store.Load(key),Is.Empty);
        }

        [Test]
        public void CorruptOrFutureLeaderboardIsNeverOverwrittenByRecord()
        {
            var course=CourseCatalog.Find("moth-line");
            var key=new CourseResultKey(course,"duck","beginner","assisted","assisted");
            foreach(var json in new[]{"{not-json","{\"Version\":2,\"Entries\":[]}"})
            {
                var storage=new MemoryStorage();storage.Seed(key.StorageKey,json);
                var store=new CourseLeaderboardStore(storage);
                Assert.That(store.Record(Result(key,17),"first","Ana","duck",DateTime.UtcNow),Is.False);
                Assert.That(storage.SaveCount,Is.Zero);
                Assert.That(store.LastError,Is.Not.Empty);
            }
        }

        [Test]
        public void FullSharedBoardStillRetainsEveryPlayersPersonalBest()
        {
            var course=CourseCatalog.Find("moth-line");
            var key=new CourseResultKey(course,"duck","beginner","assisted","assisted");
            var store=new CourseLeaderboardStore(new MemoryStorage());
            var now=new DateTime(2026,9,12,12,0,0,DateTimeKind.Utc);
            for(int i=0;i<CourseLeaderboardStore.MaximumEntries;i++)
                Assert.That(store.Record(Result(key,10+i*.1),"fast","Fast Flyer","duck",now.AddSeconds(i)),Is.True);
            Assert.That(store.Record(Result(key,25),"learning","Learning Flyer","duck",now.AddMinutes(1)),Is.True);

            Assert.That(store.Load(key).Length,Is.EqualTo(CourseLeaderboardStore.MaximumEntries+1));
            Assert.That(store.PersonalBest(key,"learning"),Is.EqualTo(25).Within(.001));
            Assert.That(store.Record(Result(key,24),"learning","Learning Flyer","duck",now.AddMinutes(2)),Is.True);
            Assert.That(store.PersonalBest(key,"learning"),Is.EqualTo(24).Within(.001));
        }

        private static CourseAttemptResult Result(CourseResultKey key,double seconds)=>new CourseAttemptResult(
            key,true,seconds,4,4,true,CourseNonRankedReason.None,CourseFailureReason.None);
    }
}
