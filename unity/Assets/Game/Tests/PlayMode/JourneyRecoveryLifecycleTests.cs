using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class JourneyRecoveryLifecycleTests
    {
        private sealed class FailingFlushStorage : IFlightSaveStorage
        {
            private readonly Dictionary<string,string> values=new Dictionary<string,string>();
            public bool HasKey(string key)=>values.ContainsKey(key);
            public string GetString(string key,string fallback="")=>values.TryGetValue(key,out var value)?value:fallback;
            public int GetInt(string key,int fallback=0)=>fallback;
            public void SetString(string key,string value)=>values[key]=value;
            public void Save()=>throw new System.InvalidOperationException("Test disk flush failed");
        }
        private BirdFlightDriver driver;
        private WorldStreamer world;
        private GameObject platform;
        private FlightActivity priorActivity;
        private bool priorResume;
        [UnitySetUp]
        public IEnumerator LoadFreshJourney()
        {
            priorActivity=ActivitySelection.Chosen;priorResume=ActivitySelection.ResumeRequested;
            ActivitySelection.Chosen=FlightActivity.RouteHome;ActivitySelection.ResumeRequested=false;
            CharacterSelection.Chosen=Resources.Load<BirdCharacterDefinition>("Characters/Duck");
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            driver=Object.FindAnyObjectByType<BirdFlightDriver>();Assert.That(driver,Is.Not.Null);
            driver.enabled=false;driver.SetSyntheticGesture(SyntheticGesture.Glide,true);
            world=Object.FindAnyObjectByType<WorldStreamer>();Assert.That(world,Is.Not.Null);
            world.enabled=false;
        }
        [UnityTearDown]
        public IEnumerator UnloadJourney()
        {
            if(platform!=null)Object.Destroy(platform);
            yield return SceneManager.LoadSceneAsync("CharacterSelect");yield return null;
            ActivitySelection.Chosen=priorActivity;ActivitySelection.ResumeRequested=priorResume;
        }
        private static ChallengeCheckpoint EarnedSeedCheckpoint()
        {
            var challenge=new FlightChallenge(FlightActivity.RouteHome);
            var checkpoint=challenge.Capture();
            checkpoint.Stage=3;checkpoint.SeedCollected=true;
            checkpoint.ActiveSeconds=120;checkpoint.RestSeconds=35;checkpoint.StrokeSeconds=44;
            checkpoint.HighestAltitude=245;checkpoint.SoaringGain=115;checkpoint.TechniqueCount=2;
            return checkpoint;
        }
        [UnityTest]
        public IEnumerator RecalibrateInPlacePreservesPhysicalStateAndEarnedJourney()
        {
            Assert.That(driver.Expedition.Challenge.Restore(EarnedSeedCheckpoint()),Is.True);
            driver.Controller.SetPaused(true);
            var before=driver.Controller.State;var checkpoint=driver.Expedition.Challenge.Capture();
            float simulationTime=driver.Controller.SimulationTime;
            Assert.That(driver.RecalibrateInPlace(),Is.True);
            Assert.That(driver.Controller.State.Position,Is.EqualTo(before.Position));
            Assert.That(driver.Controller.State.Rotation,Is.EqualTo(before.Rotation));
            Assert.That(driver.Controller.IsPaused,Is.True);
            Assert.That(driver.Controller.SimulationTime,Is.EqualTo(simulationTime));
            Assert.That(driver.Expedition.Challenge.Stage,Is.EqualTo(checkpoint.Stage));
            Assert.That(driver.Expedition.Challenge.SeedCollected,Is.True);
            Assert.That(driver.Expedition.Challenge.SoaringGain,Is.EqualTo(checkpoint.SoaringGain));
            Assert.That(driver.Expedition.Challenge.ActiveSeconds,Is.EqualTo(checkpoint.ActiveSeconds));
            Assert.That(driver.Expedition.Challenge.RestSeconds,Is.EqualTo(checkpoint.RestSeconds));
            driver.Tick(.02f);
            Assert.That(driver.Controller.State.Position,Is.EqualTo(before.Position));
            Assert.That(driver.Expedition.Challenge.Stage,Is.EqualTo(checkpoint.Stage));
            yield return null;
        }
        [UnityTest]
        public IEnumerator SaveAndLeaveFailureKeepsTheSessionAndEarnedCheckpoint()
        {
            ActivitySelection.Chosen=FlightActivity.RouteHome;ActivitySelection.ResumeRequested=false;
            driver.Expedition.Configure(driver,world.Space,new FailingFlushStorage());
            var checkpoint=EarnedSeedCheckpoint();
            Assert.That(driver.Expedition.Challenge.Restore(checkpoint),Is.True);
            driver.ReturnToCharacterSelect();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("BirdFlight"));
            Assert.That(driver,Is.Not.Null);Assert.That(driver.Controller.IsPaused,Is.True);
            Assert.That(driver.Expedition.Challenge.Stage,Is.EqualTo(checkpoint.Stage));
            Assert.That(driver.Expedition.Challenge.SeedCollected,Is.True);
            Assert.That(driver.Expedition.Journey.GetCheckpoint(FlightActivity.RouteHome).Stage,Is.EqualTo(checkpoint.Stage));
            Assert.That(driver.Expedition.Journey.SaveNotice,Does.Contain("could not be saved"));
        }
        [UnityTest]
        public IEnumerator CheckpointRecoveryUsesLoadedSupportAndRequiresRealTakeoffBeforeLandingCredit()
        {
            var checkpoint=EarnedSeedCheckpoint();checkpoint.Stage=4;
            Assert.That(driver.Expedition.Challenge.Restore(checkpoint),Is.True);
            var destination=driver.Expedition.Challenge.Destination;
            float radius=Resources.Load<BirdCharacterDefinition>("Characters/Duck").BuildProfile().CollisionRadius;
            // A real, broad test perch sits 2m above the authored garden, within its landing
            // objective. This isolates recovery from decorative collider placement.
            var target=new Vector3((float)destination.X,(float)destination.Y+2+radius+FlightContactSolver.Skin,(float)destination.Z);
            for(int i=0;i<250 && !world.IsReadyAt(target);i++)world.TickStreaming(target);
            Assert.That(world.IsReadyAt(target),Is.True,"Recovery must have loaded native ground collision");
            platform=new GameObject("Verified recovery perch"){layer=UnityFlightEnvironment.CollisionLayer};
            platform.transform.position=new Vector3(target.x,(float)destination.Y+1.5f,target.z);
            platform.AddComponent<BoxCollider>().size=new Vector3(20,1,20);
            const int surfaceId=987123;platform.AddComponent<LandingSurface>().SurfaceId=surfaceId;
            Physics.SyncTransforms();
            var environment=driver.GetComponent<UnityFlightEnvironment>();
            Assert.That(environment.IsSupported(target,radius,surfaceId),Is.True,"The saved candidate is backed by an actual collider");
            driver.Expedition.RecordSupportedPerch(world.Space.ToLogical(target));
            int landings=driver.Controller.LandingCount,collisions=driver.Controller.CollisionCount;
            int catches=driver.GetComponent<SkyForaging>().Score.Caught;
            driver.ReturnToCheckpoint();
            Assert.That(driver.RecoveryPending,Is.True);Assert.That(driver.Controller.IsPaused,Is.True);
            for(int i=0;i<250 && driver.RecoveryPending;i++)driver.Tick(.02f);
            Assert.That(driver.RecoveryPending,Is.False);
            Assert.That(driver.Controller.HasSupportedPerch,Is.True);
            Assert.That(driver.Controller.IsPaused,Is.True);
            Assert.That(Vector3.Distance(driver.Controller.State.Position,target),Is.LessThan(.1f));
            Assert.That(driver.Controller.LandingCount,Is.EqualTo(landings));
            Assert.That(driver.Controller.CollisionCount,Is.EqualTo(collisions));
            Assert.That(driver.GetComponent<SkyForaging>().Score.Caught,Is.EqualTo(catches));
            Assert.That(driver.Expedition.Challenge.Stage,Is.EqualTo(4));
            Assert.That(driver.Expedition.Challenge.Status,Is.EqualTo(ChallengeStatus.Active));
            Assert.That(driver.Expedition.Challenge.SoaringGain,Is.EqualTo(checkpoint.SoaringGain));
            Assert.That(driver.Expedition.Challenge.TechniqueCount,Is.EqualTo(checkpoint.TechniqueCount));
            Assert.That(driver.Expedition.Challenge.HighestAltitude,Is.EqualTo(checkpoint.HighestAltitude));
            driver.Controller.SetPaused(false);
            for(int i=0;i<20;i++)driver.Tick(.02f);
            Assert.That(driver.Controller.State.Phase,Is.EqualTo(FlightPhase.Perched));
            Assert.That(driver.Expedition.Challenge.Status,Is.EqualTo(ChallengeStatus.Active),"Remaining on the recovered garden perch is not a new landing");
            Assert.That(driver.Expedition.Challenge.Score,Is.Zero);
            driver.SetSyntheticGesture(SyntheticGesture.Flap,true);
            for(int i=0;i<180 && driver.Controller.State.Phase==FlightPhase.Perched;i++)driver.Tick(1f/90f);
            Assert.That(driver.Controller.State.Phase,Is.Not.EqualTo(FlightPhase.Perched),"The recovered perch must allow a normal physical flap takeoff");
            Assert.That(driver.Controller.IsPaused,Is.False);
            Assert.That(driver.Expedition.Challenge.Status,Is.EqualTo(ChallengeStatus.Active));
            yield return null;
        }
    }
}
