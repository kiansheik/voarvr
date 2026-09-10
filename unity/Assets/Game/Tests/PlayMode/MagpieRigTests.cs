using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class MagpieRigTests
    {
        [UnityTest] public IEnumerator FeatherRigFoldsSymmetricallyWithoutStretchingBones()
        {
            var definition=Resources.Load<BirdCharacterDefinition>("Characters/Magpie");
            var owner=new GameObject("MagpieRigTest");
            try
            {
                var model=Object.Instantiate(definition.RigModel,owner.transform);model.transform.localScale=Vector3.one*definition.RigPresentationScale;
                var bones=model.GetComponentsInChildren<Transform>();Transform B(string name)=>bones.Single(t=>t.name==name);
                var rig=owner.AddComponent<BirdRigDriver>();
                rig.leftUpper=B("LeftUpper");rig.leftForearm=B("LeftForearm");rig.leftHand=B("LeftHand");rig.leftTip=B("LeftTip");
                rig.rightUpper=B("RightUpper");rig.rightForearm=B("RightForearm");rig.rightHand=B("RightHand");rig.rightTip=B("RightTip");
                rig.restArmSpan=definition.RestArmSpan;rig.Configure();
                var avian=owner.AddComponent<AvianWingPresentation>();avian.Configure(rig,definition.Articulation);
                var calibration=new BirdTrackingCalibration();calibration.ConfigureBirdHalfSpan(definition.RestArmSpan);
                var flight=new BirdFlightController(new SyntheticFlightInput(),Vector3.zero,profile:definition.BuildProfile());
                var original=bones.Select(b=>b.localPosition).ToArray();
                var input=FlightInputFrame.Neutral;
                for(int i=0;i<60;i++) {rig.Present(input,calibration,Quaternion.identity,1f/120);avian.Present(input,calibration,flight,0,1f/120);}
                yield return null;
                float spread=Width(model);
                input.Tuck=1;input.LeftWing.Position.x=-.2f;input.RightWing.Position.x=.2f;
                for(int i=0;i<90;i++) {rig.Present(input,calibration,Quaternion.identity,1f/120);avian.Present(input,calibration,flight,0,1f/120);}
                yield return null;
                float folded=Width(model);
                Assert.That(folded,Is.LessThan(spread*.8f),$"Visible folded span {folded} versus spread {spread}");
                Assert.That(avian.State.LeftFold,Is.EqualTo(1));Assert.That(avian.State.LeftFan,Is.Zero);
                Assert.That(rig.leftTip.position.x,Is.EqualTo(-rig.rightTip.position.x).Within(.002f));
                Assert.That(rig.leftTip.position.y,Is.EqualTo(rig.rightTip.position.y).Within(.002f));
                for(int i=0;i<bones.Length;i++)Assert.That(Vector3.Distance(bones[i].localPosition,original[i]),Is.LessThan(.00001f),bones[i].name+" stretched");
                Assert.That(bones.Count(b=>b.name.StartsWith("Rectrix")),Is.EqualTo(12));
                input.LeftWing.Tracked=input.RightWing.Tracked=false;input.Tuck=0;
                for(int i=0;i<180;i++){rig.Present(input,calibration,Quaternion.identity,1f/120);avian.Present(input,calibration,flight,0,1f/120);}
                Assert.That(rig.LeftReachError,Is.LessThan(.01f));Assert.That(avian.State.LeftFan,Is.EqualTo(1));
            }
            finally {Object.Destroy(owner);}
        }
        private static float Width(GameObject model)
        {
            var renderer=model.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="MagpieWings");
            var mesh=new Mesh();renderer.BakeMesh(mesh);
            try {var v=mesh.vertices.Select(p=>renderer.transform.TransformPoint(p).x).ToArray();return v.Max()-v.Min();}
            finally {Object.Destroy(mesh);}
        }
    }
}
