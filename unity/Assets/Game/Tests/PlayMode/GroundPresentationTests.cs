using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;

namespace VoarVR.Tests
{
    public sealed class GroundPresentationTests
    {
        private sealed class Input : IFlightInput
        {
            public FlightInputFrame Frame=FlightInputFrame.Neutral;
            public string Mode=>"Ground presentation";
            public FlightInputFrame Sample(float dt)=>Frame;
        }
        [UnityTest] public IEnumerator BothSelectedRigsSettleWalkAndRestoreFlyingPose()
        {
            foreach(string species in new[]{"Duck","Dragon","Magpie"})
            {
                var definition=Resources.Load<BirdCharacterDefinition>("Characters/"+species);
                CharacterSelection.Chosen=definition;
                yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
                var d=Object.FindFirstObjectByType<BirdFlightDriver>();d.enabled=false;
                var presentation=d.GetComponent<BirdGroundPresentation>();
                Assert.That(presentation.LegCount,Is.EqualTo(2),"Must bind the selected rig, not the scene rig awaiting destruction");
                var s=Object.FindFirstObjectByType<WorldStreamer>();s.ResetOrigin();
                for(int i=0;i<150;i++)s.TickStreaming(Vector3.zero);
                var input=new Input();var profile=definition.BuildProfile();
                var c=new BirdFlightController(input,new Vector3(0,-1+profile.CollisionRadius+.01f,0),profile:profile,environment:d.GetComponent<UnityFlightEnvironment>());
                typeof(BirdFlightDriver).GetField("input",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d,input);
                typeof(BirdFlightDriver).GetProperty("Controller").SetValue(d,c);
                for(int i=0;i<180;i++)d.Tick(1f/120);
                Assert.That(c.State.Phase,Is.EqualTo(FlightPhase.Perched));
                Assert.That(presentation.GroundBlend,Is.EqualTo(1));
                int landings=c.LandingCount;
                input.Frame.PausePressed=true;d.Tick(1f/120);input.Frame.PausePressed=false;
                for(int i=0;i<60;i++)d.Tick(1f/120);
                Assert.That(presentation.GroundBlend,Is.EqualTo(1),"Paused landing pose stays folded");
                input.Frame.PausePressed=true;d.Tick(1f/120);input.Frame.PausePressed=false;
                Assert.That(c.State.Phase,Is.EqualTo(FlightPhase.Perched));
                Assert.That(c.LandingCount,Is.EqualTo(landings),"Resume must not invent a second touchdown");
                var feet=d.GetComponentsInChildren<Transform>().Where(t=>t.name.EndsWith("Foot")).ToArray();
                var before=feet.Select(t=>t.position).ToArray();
                int cues=d.GetComponent<FlightFeedback>().ContactCueCount;
                input.Frame.GroundMove=Vector2.up;
                for(int i=0;i<40;i++)d.Tick(1f/120);
                Assert.That(c.State.Phase,Is.EqualTo(FlightPhase.Perched));
                Assert.That(Vector3.Distance(feet[0].position,before[0]),Is.GreaterThan(.1f));
                Assert.That(Mathf.Abs(feet[0].position.y-feet[1].position.y),Is.GreaterThan(.005f),"Walking feet should alternate, not slide together");
                Assert.That(d.GetComponent<FlightFeedback>().ContactCueCount,Is.EqualTo(cues),"Support and walking are not crashes");
                input.Frame.GroundMove=Vector2.zero;
                input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=Vector3.down*2;
                for(int i=0;i<30;i++)d.Tick(1f/120);
                Assert.That(c.State.Phase,Is.Not.EqualTo(FlightPhase.Perched));
                Assert.That(presentation.GroundBlend,Is.Zero);
                Assert.That(d.GetComponent<BirdRigDriver>().LeftReachError,Is.LessThan(.05f));
            }
        }
    }
}
