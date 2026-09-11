using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VoarVR.Gameplay;

namespace VoarVR.Tests
{
    public class ForagingPersistenceTests
    {
        private sealed class BestStorage : IForagingBestStorage
        {
            public int Best, Writes;
            public bool Fail;
            public int Load()=>Best;
            public bool Save(int best){Writes++;if(Fail)return false;Best=Mathf.Max(Best,best);return true;}
        }
        private sealed class JourneyStorage : IFlightSaveStorage
        {
            public readonly Dictionary<string,string> Values=new Dictionary<string,string>();
            public bool Fail;
            public bool HasKey(string key)=>Values.ContainsKey(key);
            public string GetString(string key,string fallback="")=>Values.TryGetValue(key,out var value)?value:fallback;
            public int GetInt(string key,int fallback=0)=>fallback;
            public void SetString(string key,string value){if(Fail)throw new System.InvalidOperationException("Unavailable test storage");Values[key]=value;}
            public void Save(){}
        }
        [Test] public void NewBestFlushesAtBoundedCadenceAndOnlyWhenChanged()
        {
            var root=new GameObject("Isolated foraging save");
            try
            {
                var storage=new BestStorage();var food=root.AddComponent<SkyForaging>();food.ConfigurePersistence(null,storage);
                food.Score.Catch(1);food.Tick(4.9f);Assert.That(storage.Writes,Is.Zero);
                food.Tick(.2f);Assert.That(storage.Best,Is.EqualTo(10));Assert.That(storage.Writes,Is.EqualTo(1));
                food.Tick(20);food.SaveBest();Assert.That(storage.Writes,Is.EqualTo(1));
                food.Score.Catch(2);Assert.That(food.SaveBest(),Is.True);Assert.That(storage.Best,Is.EqualTo(30));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void FocusLossFlushesBestWithoutDestroyingComponent()
        {
            var root=new GameObject("Isolated focus save");
            try
            {
                var storage=new BestStorage();var food=root.AddComponent<SkyForaging>();food.ConfigurePersistence(null,storage);
                food.Score.Catch(1);
                typeof(SkyForaging).GetMethod("OnApplicationFocus",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(food,new object[]{false});
                Assert.That(storage.Best,Is.EqualTo(10));Assert.That(food.HasUnsavedBest,Is.False);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void FailedWriteRemainsDirtyAndExplicitRetryPersistsIt()
        {
            var root=new GameObject("Isolated retry save");
            try
            {
                var storage=new BestStorage{Fail=true};var food=root.AddComponent<SkyForaging>();food.ConfigurePersistence(null,storage);
                food.Score.Catch(1);Assert.That(food.SaveBest(),Is.False);Assert.That(food.HasUnsavedBest,Is.True);
                storage.Fail=false;Assert.That(food.SaveBest(),Is.True);Assert.That(storage.Best,Is.EqualTo(10));
                Assert.That(food.HasUnsavedBest,Is.False);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void ExistingLegacyRecordCannotDecrease()
        {
            var root=new GameObject("Isolated legacy save");
            try
            {
                var storage=new BestStorage{Best=750};var food=root.AddComponent<SkyForaging>();food.ConfigurePersistence(null,storage);
                food.Score.Catch(1);Assert.That(food.SaveBest(),Is.True);
                Assert.That(storage.Best,Is.EqualTo(750));Assert.That(storage.Writes,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void UnsupportedJourneyPreservesItsBytesAndDoesNotClaimBestWasSaved()
        {
            var root=new GameObject("Isolated unsupported journey");
            var activity=ActivitySelection.Chosen;var resume=ActivitySelection.ResumeRequested;
            try
            {
                var journeyStorage=new JourneyStorage();const string raw="{\"Version\":99,\"FutureReward\":42}";
                journeyStorage.Values[FlightJourneyStore.SaveKey]=raw;
                ActivitySelection.Chosen=FlightActivity.RouteHome;ActivitySelection.ResumeRequested=false;
                var director=root.AddComponent<ExpeditionDirector>();director.Configure(null,null,journeyStorage);
                var storage=new BestStorage();var food=root.AddComponent<SkyForaging>();food.ConfigurePersistence(director,storage);
                food.Score.Catch(1);Assert.That(food.SaveBest(),Is.False);Assert.That(food.HasUnsavedBest,Is.True);
                Assert.That(storage.Writes,Is.Zero);
                Assert.That(journeyStorage.Values[FlightJourneyStore.SaveKey],Is.EqualTo(raw));
            }
            finally{Object.DestroyImmediate(root);ActivitySelection.Chosen=activity;ActivitySelection.ResumeRequested=resume;}
        }
        [Test] public void JourneyWriteFailureRetriesSameBestAndKeepsLaterHigherBest()
        {
            var root=new GameObject("Isolated journey retry");
            var activity=ActivitySelection.Chosen;var resume=ActivitySelection.ResumeRequested;
            try
            {
                var journeyStorage=new JourneyStorage{Fail=true};ActivitySelection.Chosen=FlightActivity.FreeFlight;ActivitySelection.ResumeRequested=false;
                var director=root.AddComponent<ExpeditionDirector>();director.Configure(null,null,journeyStorage);
                var storage=new BestStorage();var food=root.AddComponent<SkyForaging>();food.ConfigurePersistence(director,storage);
                food.Score.Catch(1);Assert.That(food.SaveBest(),Is.False);
                Assert.That(storage.Writes,Is.Zero);Assert.That(food.SaveBest(),Is.False);
                food.Score.Catch(2);journeyStorage.Fail=false;
                Assert.That(food.SaveBest(),Is.True);
                Assert.That(new FlightJourneyStore(journeyStorage).Load().ForagingBest,Is.EqualTo(30));
                Assert.That(storage.Best,Is.EqualTo(30));
            }
            finally{Object.DestroyImmediate(root);ActivitySelection.Chosen=activity;ActivitySelection.ResumeRequested=resume;}
        }
        [Test] public void NewForagingBestIsDurableInBothJourneyAndLegacyRecord()
        {
            var root=new GameObject("Isolated integrated best");
            var activity=ActivitySelection.Chosen;var resume=ActivitySelection.ResumeRequested;
            try
            {
                var journeyStorage=new JourneyStorage();ActivitySelection.Chosen=FlightActivity.FreeFlight;ActivitySelection.ResumeRequested=false;
                var director=root.AddComponent<ExpeditionDirector>();director.Configure(null,null,journeyStorage);
                var storage=new BestStorage();var food=root.AddComponent<SkyForaging>();food.ConfigurePersistence(director,storage);
                food.Score.Catch(1);food.Score.Catch(2);Assert.That(food.SaveBest(),Is.True);
                Assert.That(new FlightJourneyStore(journeyStorage).Load().ForagingBest,Is.EqualTo(30));
                Assert.That(storage.Best,Is.EqualTo(30));
            }
            finally{Object.DestroyImmediate(root);ActivitySelection.Chosen=activity;ActivitySelection.ResumeRequested=resume;}
        }
    }
}
