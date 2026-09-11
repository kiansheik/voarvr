using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Core;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.Input;
using VoarVR.UI;
using VoarVR.World;

namespace VoarVR.Tests
{
    // Uses the suite's existing in-memory save adapters, including scene teardown.
    public sealed class RouteBearingLifecycleTests
    {
        private BirdFlightDriver driver;
        private JourneyPresentation presentation;
        private RouteBearing bearing;
        private Camera eye;
        private Vector3 player,target;
        private FlightActivity previousActivity;
        private bool previousResume,previousGuidance,previousAudio;
        private BirdCharacterDefinition previousCharacter;

        [UnitySetUp]
        public IEnumerator LoadFarSeedRoute()
        {
            previousActivity=ActivitySelection.Chosen;previousResume=ActivitySelection.ResumeRequested;
            previousCharacter=CharacterSelection.Chosen;
            previousGuidance=FlightPreferences.GuidanceEnabled;previousAudio=FlightPreferences.AudioEnabled;
            ActivitySelection.Chosen=FlightActivity.RouteHome;ActivitySelection.ResumeRequested=false;
            CharacterSelection.Chosen=Resources.Load<BirdCharacterDefinition>("Characters/Duck");
            FlightPreferences.GuidanceEnabled=true;FlightPreferences.AudioEnabled=true;
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            driver=Object.FindAnyObjectByType<BirdFlightDriver>();Assert.That(driver,Is.Not.Null);driver.enabled=false;
            var world=Object.FindAnyObjectByType<WorldStreamer>();Assert.That(world,Is.Not.Null);world.enabled=false;
            eye=Camera.main;Assert.That(eye,Is.Not.Null);
            var cameraDriver=eye.GetComponent<FlightCamera>();if(cameraDriver!=null)cameraDriver.enabled=false;
            driver.Controller.SetPaused(false);driver.Controller.StreamingBlocked=false;
            driver.GetComponent<FlightHud>().SetVisible(false);
            var challenge=driver.Expedition.Challenge;
            challenge.Step(Observation(100,130,154));challenge.Step(Observation(100,231,154));
            Assert.That(challenge.Kind,Is.EqualTo(ObjectiveKind.CollectSeed));
            player=world.Space.ToLocal(1047,318,-483);
            presentation=driver.GetComponent<JourneyPresentation>();presentation.Tick(player);
            Assert.That(presentation.NavigationAvailable,Is.True);
            target=presentation.TargetMarkerWorldPosition;
            bearing=driver.GetComponent<RouteBearing>();Assert.That(bearing,Is.Not.Null);
            eye.transform.SetPositionAndRotation(player,Quaternion.LookRotation(player-target));
            bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
        }

        [UnityTearDown]
        public IEnumerator RestoreSceneAndPreferences()
        {
            yield return SceneManager.LoadSceneAsync("CharacterSelect");yield return null;
            FlightPreferences.GuidanceEnabled=previousGuidance;FlightPreferences.AudioEnabled=previousAudio;
            ActivitySelection.Chosen=previousActivity;ActivitySelection.ResumeRequested=previousResume;
            CharacterSelection.Chosen=previousCharacter;
        }

        [UnityTest]
        public IEnumerator HiddenHudStillGivesABearingAndHeadTurnReacquiresTheWorldTarget()
        {
            Assert.That(driver.ShowFlightText,Is.False);
            Assert.That(driver.GetComponent<FlightHud>().Visible,Is.False);
            Assert.That(presentation.SeedVisible,Is.False,"This is the actual far Quest position, beyond the physical seed's draw range.");
            Assert.That(presentation.TargetMarkerVisible,Is.True);
            Assert.That(bearing.Visible,Is.True,"Reacquisition cannot depend on enabling text instruments.");
            eye.transform.rotation=Quaternion.LookRotation(target-eye.transform.position);
            bearing.TickBearing();Assert.That(bearing.Visible,Is.False,"Looking toward the actual goal removes the turn cue.");
            eye.transform.rotation=Quaternion.LookRotation(eye.transform.position-target);
            bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
            var cue=GameObject.Find("Offscreen route bearing");Assert.That(cue,Is.Not.Null);
            var voice=cue.GetComponent<AudioSource>();Assert.That(voice.spatialBlend,Is.EqualTo(1));

            FlightPreferences.GuidanceEnabled=false;bearing.TickBearing();
            AssertSuppressed(bearing,voice,"World guidance preference");
            FlightPreferences.GuidanceEnabled=true;bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
            driver.Controller.SetPaused(true);bearing.TickBearing();AssertSuppressed(bearing,voice,"Controller pause");
            driver.Controller.SetPaused(false);bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
            driver.Controller.StreamingBlocked=true;bearing.TickBearing();AssertSuppressed(bearing,voice,"Streaming pause");
            driver.Controller.StreamingBlocked=false;bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
            presentation.enabled=false;
            Assert.That(presentation.NavigationAvailable,Is.False,"Disabled presentation cannot publish a stale target.");
            bearing.TickBearing();AssertSuppressed(bearing,voice,"Disabling the target owner suppresses its bearing");
            presentation.enabled=true;presentation.Tick(player);bearing.TickBearing();Assert.That(bearing.Visible,Is.True);

            Callback("OnApplicationFocus",false);
            AssertSuppressed(bearing,voice,"Own focus callback hides without waiting for another frame");
            Callback("OnApplicationFocus",true);bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
            Callback("OnApplicationPause",true);
            AssertSuppressed(bearing,voice,"Own pause callback hides without waiting for another frame");
            Callback("OnApplicationPause",false);bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
            FlightPreferences.AudioEnabled=false;bearing.TickBearing();
            Assert.That(bearing.Visible,Is.True,"Audio mute keeps the non-text visual direction available.");
            Assert.That(voice.isPlaying,Is.False);
            bearing.enabled=false;AssertSuppressed(bearing,voice,"Disabling the component releases its live cue immediately");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrackingMenuAndFinalCameraPoseControlTheLiveBearing()
        {
            var frame=FlightInputFrame.Neutral;frame.HeadTracked=true;
            frame.HeadPosition=new Vector3(0,1.65f,0);
            frame.LeftWing.Position=new Vector3(-.7f,1.25f,.1f);frame.RightWing.Position=new Vector3(.7f,1.25f,.1f);
            Assert.That(driver.Calibration.CaptureComfortableGlide(frame),Is.True);
            typeof(BirdFlightDriver).GetProperty("UsesXR").SetValue(driver,true);
            typeof(BirdFlightController).GetProperty("LastInput").SetValue(driver.Controller,frame);
            bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
            frame.HeadTracked=false;typeof(BirdFlightController).GetProperty("LastInput").SetValue(driver.Controller,frame);
            bearing.TickBearing();Assert.That(bearing.Visible,Is.False);
            frame.HeadTracked=true;frame.LeftWing.Tracked=false;typeof(BirdFlightController).GetProperty("LastInput").SetValue(driver.Controller,frame);
            bearing.TickBearing();Assert.That(bearing.Visible,Is.False);
            frame.LeftWing.Tracked=true;typeof(BirdFlightController).GetProperty("LastInput").SetValue(driver.Controller,frame);
            bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
            var menu=driver.GetComponent<FlightMenu>();menu.Open();
            bearing.TickBearing();Assert.That(bearing.Visible,Is.False);menu.Close();
            bearing.TickBearing();Assert.That(bearing.Visible,Is.True);
            var previous=bearing.CuePosition;
            eye.transform.position+=new Vector3(.24f,.1f,.12f);
            eye.transform.rotation*=Quaternion.Euler(-15,25,0);
            typeof(RouteBearing).GetMethod("ApplyFinalPose",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(bearing,null);
            Assert.That(bearing.Visible,Is.True);Assert.That(Vector3.Distance(previous,bearing.CuePosition),Is.GreaterThan(.1f));
            Assert.That(RouteBearing.TryProject(eye.transform.InverseTransformPoint(target),eye.fieldOfView,eye.aspect,out var projected,out _),Is.True);
            Assert.That(Vector3.Distance(bearing.CuePosition,eye.transform.TransformPoint(projected)),Is.LessThan(.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReconfigurationIsBoundedAndRemovalDestroysOwnedCueMeshAndVoice()
        {
            var cue=GameObject.Find("Offscreen route bearing");Assert.That(cue,Is.Not.Null);
            var mesh=cue.GetComponent<MeshFilter>().sharedMesh;
            var voice=cue.GetComponent<AudioSource>();
            var clip=(AudioClip)typeof(RouteBearing).GetField("clip",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(bearing);
            Assert.That(clip,Is.Not.Null);
            for(int i=0;i<8;i++)bearing.Configure(driver,presentation,eye);
            bearing.TickBearing();
            Assert.That(GameObject.Find("Offscreen route bearing"),Is.SameAs(cue));
            Assert.That(cue.GetComponents<MeshRenderer>().Length,Is.EqualTo(1));
            Assert.That(cue.GetComponents<AudioSource>().Length,Is.EqualTo(1));
            Assert.That(cue.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(mesh));
            int count=0;
            foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(filter.gameObject.name=="Offscreen route bearing")count++;
            Assert.That(count,Is.EqualTo(1),"Repeated configuration cannot leak another world cue.");
            Object.Destroy(bearing);yield return null;yield return null;
            Assert.That(cue==null,Is.True,"The unparented cue is owned by the component.");
            Assert.That(voice==null,Is.True);Assert.That(mesh==null,Is.True);Assert.That(clip==null,Is.True);
        }

        private static void AssertSuppressed(RouteBearing cue,AudioSource voice,string reason)
        {Assert.That(cue.Visible,Is.False,reason);Assert.That(voice.isPlaying,Is.False,reason);}
        private void Callback(string method,bool value)
            =>typeof(RouteBearing).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(bearing,new object[]{value});
        private static ChallengeObservation Observation(double x,double y,double z)
            =>new ChallengeObservation(new LogicalPosition(x,y,z),Vector3.forward*12,4,1,1,false,0,0);
    }
}
