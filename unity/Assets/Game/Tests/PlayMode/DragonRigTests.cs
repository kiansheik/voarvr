using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class DragonRigTests
    {
        [UnityTest]
        public IEnumerator CalibratedDragonKeepsBroadWingsAndMovesItsMembrane()
        {
            CharacterSelection.Chosen = Resources.Load<BirdCharacterDefinition>("Characters/Dragon");
            yield return SceneManager.LoadSceneAsync("BirdFlight");
            yield return null;
            var driver = Object.FindAnyObjectByType<BirdFlightDriver>();
            driver.enabled = false;
            driver.transform.SetPositionAndRotation(new Vector3(0f, 20f, 0f), Quaternion.identity);
            var rig = driver.GetComponent<BirdRigDriver>();
            var frame = FlightInputFrame.Neutral;
            frame.HeadTracked = true;
            frame.HeadPosition = new Vector3(0f, 1.65f, 0f);
            frame.LeftWing.Position = new Vector3(-.7f, 1.25f, .1f);
            frame.RightWing.Position = new Vector3(.7f, 1.25f, .1f);
            Assert.That(driver.Calibration.CaptureComfortableGlide(frame), Is.True);
            for (int i = 0; i < 60; i++) rig.Present(frame, driver.Calibration, driver.Heading, 1f / 120f);
            var skin = GameObject.Find("DragonWings").GetComponent<SkinnedMeshRenderer>();
            var mesh = new Mesh();
            skin.BakeMesh(mesh, true);
            var restVertices = mesh.vertices;
            Assert.That(mesh.bounds.size.x, Is.GreaterThan(6f), "Calibrated dragon must not collapse to duck span.");
            frame.LeftWing.Position += Vector3.down * .3f;
            for (int i = 0; i < 60; i++) rig.Present(frame, driver.Calibration, driver.Heading, 1f / 120f);
            skin.BakeMesh(mesh, true);
            float maxTravel = 0f;
            var movedVertices = mesh.vertices;
            for (int i = 0; i < movedVertices.Length; i++)
                maxTravel = Mathf.Max(maxTravel, Vector3.Distance(restVertices[i], movedVertices[i]));
            Assert.That(maxTravel, Is.GreaterThan(.5f), "Human downstroke should visibly articulate the giant membrane.");
            Object.DestroyImmediate(mesh);
            driver.SetViewMode(FlightViewMode.ThirdPerson);
            yield return null;
            Assert.That(Vector3.Distance(Camera.main.transform.position, driver.transform.position), Is.GreaterThan(5f));
        }
    }
}
