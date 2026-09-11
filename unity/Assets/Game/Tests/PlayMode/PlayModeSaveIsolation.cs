#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Gameplay;

namespace VoarVR.Tests
{
    // Install before any test loads BirdFlight. Scene lifecycle callbacks always use memory,
    // including on assertion failures; no player save or recovered.* archive is modified.
    [SetUpFixture]
    public sealed class PlayModeSaveIsolation
    {
        private sealed class MemoryJourneyStorage : IFlightSaveStorage
        {
            private readonly Dictionary<string,string> values=new Dictionary<string,string>();
            public bool HasKey(string key)=>values.ContainsKey(key);
            public string GetString(string key,string fallback="")=>values.TryGetValue(key,out var value)?value:fallback;
            public int GetInt(string key,int fallback=0)=>fallback;
            public void SetString(string key,string value)=>values[key]=value;
            public void Save(){}
        }
        private sealed class MemoryBestStorage : IForagingBestStorage
        {
            private int best;
            public int Load()=>best;
            public bool Save(int value){best=Math.Max(best,value);return true;}
        }
        private Func<IFlightSaveStorage> priorJourneyFactory;
        private Func<IForagingBestStorage> priorBestFactory;
        private FlightActivity priorActivity;
        private bool priorResume;
        private BirdCharacterDefinition priorCharacter;
        [OneTimeSetUp]
        public void InstallMemoryStores()
        {
            priorJourneyFactory=FlightJourneyStore.EditorStorageOverride;
            priorBestFactory=SkyForaging.EditorStorageOverride;
            priorActivity=ActivitySelection.Chosen;priorResume=ActivitySelection.ResumeRequested;
            priorCharacter=CharacterSelection.Chosen;
            var journey=new MemoryJourneyStorage();var best=new MemoryBestStorage();
            FlightJourneyStore.EditorStorageOverride=()=>journey;
            SkyForaging.EditorStorageOverride=()=>best;
            ActivitySelection.ResumeRequested=false;
        }
        [OneTimeTearDown]
        public void RestoreFactoriesAfterSaveOwnersAreGone()
        {
            // Destroy while the overrides are still installed, so OnDisable flushes stay in
            // memory. Unity's runner restores the editing scene after this suite exits.
            foreach(var driver in UnityEngine.Object.FindObjectsByType<BirdFlightDriver>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(driver!=null)UnityEngine.Object.DestroyImmediate(driver.gameObject);
            foreach(var director in UnityEngine.Object.FindObjectsByType<ExpeditionDirector>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(director!=null)UnityEngine.Object.DestroyImmediate(director);
            foreach(var food in UnityEngine.Object.FindObjectsByType<SkyForaging>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(food!=null)UnityEngine.Object.DestroyImmediate(food);
            FlightJourneyStore.EditorStorageOverride=priorJourneyFactory;
            SkyForaging.EditorStorageOverride=priorBestFactory;
            ActivitySelection.Chosen=priorActivity;ActivitySelection.ResumeRequested=priorResume;
            CharacterSelection.Chosen=priorCharacter;
        }
    }
}
#endif
