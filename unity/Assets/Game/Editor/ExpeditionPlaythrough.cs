using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
using VoarVR.World;
using VoarVR.Gameplay;
namespace VoarVR.Editor
{
    // Ordinary semantic-input pilot for reproducible desktop route validation, not wearer proof.
    public static class ExpeditionPlaythrough
    {
        public static string Run()
        {var d=Object.FindAnyObjectByType<BirdFlightDriver>();d.enabled=false;d.StartCoroutine(Fly(d));return "Continuous input-only expedition fixture started";}
        private static IEnumerator Fly(BirdFlightDriver d)
        {
            var input=new FlightGameMeasurements.Controls();
            typeof(BirdFlightDriver).GetField("input",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d,input);
            var profile=Resources.Load<BirdCharacterDefinition>("Characters/Duck").BuildProfile();
            var wind=Object.FindAnyObjectByType<WindField>();wind.SetMode(WindMode.Assisted);
            var world=Object.FindAnyObjectByType<WorldStreamer>();world.ResetOrigin();
            var env=d.GetComponent<UnityFlightEnvironment>();var c=new BirdFlightController(input,new Vector3(0,25,0),profile:profile,wind:wind,environment:env);
            typeof(BirdFlightDriver).GetProperty("Controller").SetValue(d,c);
            ActivitySelection.Chosen=FlightActivity.SkywardExpedition;d.Expedition.Configure(d,world.Space);
            var log=new StringBuilder();int lastStage=-1;bool arrivalClimbed=false,approachAligned=false;
            for(int step=0;step<144000 && d.Expedition.Challenge.Status==ChallengeStatus.Active;step++)
            {
                var challenge=d.Expedition.Challenge;var pos=c.State.Position;float time=c.SimulationTime;
                var departure=FlightRegions.DepartureThermal(time);var center=world.Space.ToLocal(departure.X,150,departure.Z);
                var arrival=world.Space.ToLocal(330+Mathf.Sin(time*.004f)*20,300,530);
                var target=world.Space.ToLocal(challenge.Destination.X,challenge.Destination.Y,challenge.Destination.Z);
                Vector3 aim=target;bool quiet=false;
                if(challenge.Stage==0)aim=new Vector3(center.x,70,center.z);
                else if(challenge.Stage==1 || (challenge.Stage==2 && Vector3.Distance(new Vector3(pos.x,0,pos.z),new Vector3(center.x,0,center.z))<130 && pos.y<285))
                {aim=Circle(pos,center,26);quiet=pos.y>45 && c.State.Phase!=FlightPhase.Perched;}
                else if(challenge.Stage==2)
                {
                    float arrivalDistance=Vector2.Distance(new Vector2(pos.x,pos.z),new Vector2(arrival.x,arrival.z));
                    if(arrivalDistance<110 && pos.y>=300)arrivalClimbed=true;
                    if(!arrivalClimbed && pos.y<300 && arrivalDistance<110){aim=Circle(pos,arrival,24);quiet=pos.y>45 && c.State.Phase!=FlightPhase.Perched;}
                    else
                    {var setup=new Vector3(target.x,target.y+17,target.z-125);
                     if(Vector2.Distance(new Vector2(pos.x,pos.z),new Vector2(setup.x,setup.z))<12 && Mathf.Abs(pos.y-setup.y)<5)approachAligned=true;
                     if(pos.z>target.z-25)approachAligned=false;
                     aim=approachAligned?new Vector3(target.x,target.y+17,target.z-30):setup;}
                }
                else aim=target;
                var desired=aim-pos;float heading=Mathf.Atan2(c.State.Velocity.x,c.State.Velocity.z)*Mathf.Rad2Deg;
                float desiredYaw=Mathf.Atan2(desired.x,desired.z)*Mathf.Rad2Deg;
                var f=FlightInputFrame.Neutral;f.Bank=Mathf.Clamp(Mathf.DeltaAngle(heading,desiredYaw)/45,-.7f,.7f);
                float pitch=quiet?0:Mathf.Abs(aim.y-pos.y)<3?0:Mathf.Clamp(Mathf.Sign(aim.y-pos.y)*18+(aim.y-pos.y)*.5f,-35,14);f.LookDirection=Quaternion.Euler(-pitch,0,0)*Vector3.forward;
                if(c.State.Phase==FlightPhase.Perched || (!quiet && challenge.Stage<3 && pos.y<aim.y-3)){float stroke=Mathf.Max(0,Mathf.Sin(time*Mathf.PI*2))*2.8f;f.LeftWing.Velocity=f.RightWing.Velocity=Vector3.down*stroke;}
                if(challenge.Stage==3 && new Vector2(desired.x,desired.z).magnitude<24)f.Flare=1;
                input.Frame=f;world.TickStreaming(pos);Object.FindAnyObjectByType<SkyArchipelago>().Tick(pos);d.Tick(1f/120);
                if(lastStage!=challenge.Stage || step%1200==0)
                {log.AppendLine("t="+time.ToString("F1")+" stage="+challenge.Stage+" pos="+pos+" wind="+c.WindVelocity+" gain="+challenge.SoaringGain+" phase="+c.State.Phase);lastStage=challenge.Stage;}
                if(step%60==0)yield return null;
            }
            var result=d.Expedition.Challenge;log.AppendLine("RESULT "+result.Status+" stage="+result.Stage+" score="+result.Score+" pos="+c.State.Position);
            Directory.CreateDirectory(FlightGameReview.Folder);File.WriteAllText(Path.Combine(FlightGameReview.Folder,"continuous-expedition.txt"),log.ToString());
            d.GetComponent<VoarVR.Core.FlightCamera>()?.SendMessage("LateUpdate");
            DuckReview.CaptureCurrent(FlightGameReview.Folder,"expedition-player-result");
        }
        private static Vector3 Circle(Vector3 p,Vector3 center,float radius)
        {var radial=p-center;radial.y=0;float r=radial.magnitude;radial=r>.1f?radial/r:Vector3.right;var tangent=Vector3.Cross(Vector3.up,radial);return p+tangent*18+radial*(radius-r)*1.2f;}
    }
}
