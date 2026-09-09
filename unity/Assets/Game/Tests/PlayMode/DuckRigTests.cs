using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class DuckRigTests
    {
        [UnityTest] public IEnumerator ImportedRigFollowsBothHandsAndTwistWithoutStretch()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            var rig=driver.GetComponent<BirdRigDriver>();Assert.That(rig,Is.Not.Null);
            Assert.That(driver.transform.InverseTransformPoint(rig.leftHand.position).x,Is.LessThan(0f));
            Assert.That(driver.transform.InverseTransformPoint(rig.rightHand.position).x,Is.GreaterThan(0f));
            var frame=FlightInputFrame.Neutral;var calibration=new BirdTrackingCalibration();
            float upper=Vector3.Distance(rig.leftUpper.position,rig.leftForearm.position);
            float lower=Vector3.Distance(rig.leftForearm.position,rig.leftHand.position);
            foreach(var delta in new[]{Vector3.up*.16f,Vector3.down*.16f,Vector3.forward*.16f,Vector3.back*.16f,Vector3.right*.16f})
            {
                frame=FlightInputFrame.Neutral;
                for(int i=0;i<60;i++)rig.Present(frame,calibration,driver.Heading,1f/120);
                var initial=rig.leftHand.position;
                frame.LeftWing.Position+=delta;
                for(int i=0;i<60;i++)rig.Present(frame,calibration,driver.Heading,1f/120);
                Assert.That(Vector3.Dot(rig.leftHand.position-initial,driver.Heading*delta),Is.GreaterThan(.005f));
                Assert.That(Vector3.Distance(rig.leftUpper.position,rig.leftForearm.position),Is.EqualTo(upper).Within(.001f));
                Assert.That(Vector3.Distance(rig.leftForearm.position,rig.leftHand.position),Is.EqualTo(lower).Within(.001f));
            }
            var rest=rig.leftHand.rotation;frame.LeftWing.Orientation=Quaternion.Euler(45,20,10);
            for(int i=0;i<60;i++)rig.Present(frame,calibration,driver.Heading,1f/120);
            Assert.That(Quaternion.Angle(rest,rig.leftHand.rotation),Is.GreaterThan(20));
            frame.LeftWing.Tracked=false;
            for(int i=0;i<120;i++)rig.Present(frame,calibration,driver.Heading,1f/120);
            Assert.That(float.IsNaN(rig.leftHand.position.x),Is.False);
        }
        [UnityTest] public IEnumerator FirstPersonCameraDoesNotInheritBodyBank()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            driver.transform.rotation=Quaternion.Euler(30,0,45);
            yield return null;
            Assert.That(Mathf.Abs(Vector3.Dot(Camera.main.transform.right,Vector3.up)),Is.LessThan(.01f));
            Assert.That(Vector3.Distance(Camera.main.transform.position,driver.transform.position+driver.Heading*BirdTrackingCalibration.EyeAnchor),Is.LessThan(.001f));
            Assert.That(driver.GetComponent<BirdRigDriver>().face.enabled,Is.False);
        }

        [UnityTest] public IEnumerator NeutralWingTargetsTravelWithRootWithoutLag()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight"); yield return null;
            var driver = Object.FindAnyObjectByType<BirdFlightDriver>(); driver.enabled = false;
            var rig = driver.GetComponent<BirdRigDriver>();
            var frame = FlightInputFrame.Neutral;
            var calibration = new BirdTrackingCalibration();
            for (int i = 0; i < 30; i++) rig.Present(frame, calibration, driver.Heading, 1f / 120f);
            var leftLocal = Quaternion.Inverse(driver.Heading) * (rig.LeftTarget - driver.transform.position);
            driver.transform.position += new Vector3(3f, 1f, 7f);
            driver.transform.rotation = Quaternion.Euler(0f, 60f, 0f);
            rig.Present(frame, calibration, driver.Heading, 1f / 120f);
            var movedLocal = Quaternion.Inverse(driver.Heading) * (rig.LeftTarget - driver.transform.position);
            Assert.That(Vector3.Distance(leftLocal, movedLocal), Is.LessThan(.0001f));
            Assert.That(rig.LeftReachError, Is.LessThan(.01f));
        }

        [UnityTest] public IEnumerator BakedWingMeshChangesAcrossGlideFlapAndTuck()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight"); yield return null;
            var driver = Object.FindAnyObjectByType<BirdFlightDriver>(); driver.enabled = false;
            var skin = GameObject.Find("DuckWings").GetComponent<SkinnedMeshRenderer>();
            Bounds Bake()
            {
                var mesh = new Mesh(); skin.BakeMesh(mesh, true); var bounds = mesh.bounds;
                Object.DestroyImmediate(mesh); return bounds;
            }
            var rig = driver.GetComponent<BirdRigDriver>();
            var calibration = new BirdTrackingCalibration();
            var frame = FlightInputFrame.Neutral;
            for (int i = 0; i < 60; i++) rig.Present(frame, calibration, driver.Heading, 1f / 120f);
            yield return null;
            var glide = Bake();
            frame.LeftWing.Position.y = frame.RightWing.Position.y = -.2f;
            for (int i = 0; i < 60; i++) rig.Present(frame, calibration, driver.Heading, 1f / 120f);
            yield return null;
            var flap = Bake();
            frame = FlightInputFrame.Neutral;
            frame.LeftWing.Position = new Vector3(-.2f, -.05f, -.15f);
            frame.RightWing.Position = new Vector3(.2f, -.05f, -.15f);
            for (int i = 0; i < 60; i++) rig.Present(frame, calibration, driver.Heading, 1f / 120f);
            yield return null;
            var dive = Bake();
            Assert.That(Mathf.Abs(flap.center.z - glide.center.z), Is.GreaterThan(.05f));
            Assert.That(dive.size.x, Is.LessThan(glide.size.x * .6f));
        }

        [UnityTest] public IEnumerator ThirdPersonToggleShowsWholeDuckFromAboveAndBehind()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            driver.SetViewMode(FlightViewMode.ThirdPerson);yield return null;
            var local=Quaternion.Inverse(driver.Heading)*(Camera.main.transform.position-driver.transform.position);
            Assert.That(local.y,Is.GreaterThan(.7f));Assert.That(local.z,Is.LessThan(-2f));
            Assert.That(driver.GetComponent<BirdRigDriver>().face.enabled,Is.True);
            driver.ToggleView();yield return null;
            Assert.That(driver.ViewMode,Is.EqualTo(FlightViewMode.FirstPerson));
        }

        [UnityTest] public IEnumerator ProceduralWorldCreatesBothBiomesWindAndLandingBeacons()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var world=Object.FindAnyObjectByType<ProceduralFlightWorld>();
            var wind=Object.FindAnyObjectByType<WindField>();
            Assert.That(world,Is.Not.Null);Assert.That(wind,Is.Not.Null);
            Assert.That(GameObject.Find("SpiritCity"),Is.Not.Null);
            Assert.That(GameObject.Find("SpiritForest"),Is.Not.Null);
            Assert.That(GameObject.Find("VisibleWind"),Is.Not.Null);
            Assert.That(GameObject.Find("LandingBeacons"),Is.Not.Null);
            foreach(var ring in GameObject.Find("LandingBeacons").GetComponentsInChildren<LineRenderer>())
                Assert.That(ring.transform.position.y,Is.GreaterThan(.5f),ring.name);
            wind.SetMode(WindMode.StillAir);yield return null;
            foreach(var line in GameObject.Find("VisibleWind").GetComponentsInChildren<LineRenderer>(true))
                Assert.That(line.enabled,Is.False,line.name);
            wind.SetMode(WindMode.Wild);yield return null;
            var hazard=GameObject.Find("Thermal_2_0").GetComponent<LineRenderer>();
            Assert.That(hazard.enabled,Is.True);
            Assert.That(hazard.sharedMaterial.name,Does.Contain("WindHazard"));
            Assert.That(GameObject.Find("TravelingDraft_00"), Is.Not.Null);
            foreach(var renderer in world.GetComponentsInChildren<Renderer>())
                Assert.That(renderer.sharedMaterial?.shader?.isSupported,Is.True,renderer.name);
        }
    }
}
