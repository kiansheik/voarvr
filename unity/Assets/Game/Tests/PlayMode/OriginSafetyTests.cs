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
    public sealed class OriginSafetyTests
    {
        [UnityTest] public IEnumerator RebasePreservesFlightTrackingWindAndRelativeCamera()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
            var d=Object.FindAnyObjectByType<BirdFlightDriver>();d.enabled=false;
            var s=Object.FindAnyObjectByType<WorldStreamer>();var w=Object.FindAnyObjectByType<WindField>();
            var f=FlightInputFrame.Neutral;f.HeadTracked=true;f.HeadPosition=new Vector3(0,1.6f,0);
            f.LeftWing.Position=new Vector3(-.7f,1.25f,0);f.RightWing.Position=new Vector3(.7f,1.25f,0);
            d.RestartAndCalibrate(f);yield return null;
            var before=d.Controller.State;var logical=s.Space.ToLogical(before.Position);
            var head=d.Calibration.HeadOrigin;var left=d.Calibration.LeftNeutral;var input=d.Controller.LastInput;
            var cameraOffset=Camera.main.transform.position-d.transform.position;
            var wind=w.Sample(before.Position,31f);var delta=new Vector3(1024,0,-1024);
            d.RebaseWorld(delta);
            Assert.That(d.Controller.State.Velocity,Is.EqualTo(before.Velocity));
            Assert.That(d.Controller.State.Rotation,Is.EqualTo(before.Rotation));
            Assert.That(s.Space.ToLogical(d.Controller.State.Position).X,Is.EqualTo(logical.X));
            Assert.That(s.Space.ToLogical(d.Controller.State.Position).Z,Is.EqualTo(logical.Z));
            Assert.That(d.Calibration.HeadOrigin,Is.EqualTo(head));Assert.That(d.Calibration.LeftNeutral,Is.EqualTo(left));
            Assert.That(d.Controller.LastInput.LeftWing.Velocity,Is.EqualTo(input.LeftWing.Velocity));
            Assert.That(w.Sample(d.Controller.State.Position,31f),Is.EqualTo(wind));
            Assert.That(Vector3.Distance(Camera.main.transform.position-d.transform.position,cameraOffset),Is.LessThan(.0001f));
            d.RestartAndCalibrate(f);
            Assert.That(s.Space.OffsetX,Is.Zero);Assert.That(s.Space.OffsetZ,Is.Zero);
            Assert.That(d.Controller.State.Position,Is.EqualTo(before.Position));
        }
    }
}
