using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Editor
{
    public static class MagpieReview
    {
        public static string Folder="../artifacts/reviews/magpie-telemetry/round-01";
        public static string Capture()
        {
            var driver=Object.FindAnyObjectByType<BirdFlightDriver>();driver.enabled=false;
            driver.StartCoroutine(Poses(driver));return "Magpie pose capture started";
        }
        private sealed class PoseInput:IFlightInput
        {
            public FlightInputFrame Frame=FlightInputFrame.Neutral;
            public string Mode=>"Magpie pose fixture";
            public FlightInputFrame Sample(float dt)=>Frame;
        }
        private static IEnumerator Poses(BirdFlightDriver d)
        {
            var input=new PoseInput();var definition=Resources.Load<BirdCharacterDefinition>("Characters/Magpie");
            var p=definition.BuildProfile();
            var c=new BirdFlightController(input,Vector3.up*80,profile:p);
            typeof(BirdFlightDriver).GetField("input",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(d,input);
            typeof(BirdFlightDriver).GetProperty("Controller").SetValue(d,c);
            var log=new StringBuilder();
            foreach(string pose in new[]{"spread","downstroke","upstroke","fold","half-fold","brake","bank","sweep-forward","sweep-back","wrist","tracking-loss"})
            {
                c.Reset();input.Frame=FlightInputFrame.Neutral;
                if(pose=="downstroke") { input.Frame.LeftWing.Position.y=input.Frame.RightWing.Position.y=-.35f;input.Frame.LeftWing.Velocity=input.Frame.RightWing.Velocity=Vector3.down*1.8f; }
                if(pose=="upstroke")input.Frame.LeftWing.Position.y=input.Frame.RightWing.Position.y=.35f;
                if(pose=="fold") { input.Frame.Tuck=1;input.Frame.LeftWing.Position.x=-.2f;input.Frame.RightWing.Position.x=.2f; }
                if(pose=="half-fold") {input.Frame.Tuck=.5f;input.Frame.LeftWing.Position.x=-.4f;input.Frame.RightWing.Position.x=.4f;}
                if(pose=="sweep-forward")input.Frame.LeftWing.Position.z=input.Frame.RightWing.Position.z=.25f;
                if(pose=="sweep-back")input.Frame.LeftWing.Position.z=input.Frame.RightWing.Position.z=-.25f;
                if(pose=="wrist")input.Frame.LeftWing.Orientation=input.Frame.RightWing.Orientation=Quaternion.Euler(35,0,0);
                if(pose=="brake")input.Frame.Flare=1;
                if(pose=="bank") { input.Frame.Bank=.6f;input.Frame.LeftWing.Position.y=-.25f;input.Frame.RightWing.Position.y=.25f; }
                if(pose=="tracking-loss")input.Frame.LeftWing.Tracked=input.Frame.RightWing.Tracked=false;
                for(int i=0;i<90;i++)d.Tick(1f/120);
                yield return null;yield return null;
                DuckReview.Capture(Folder,"Magpie-"+pose,new Vector3(1.3f,1,-1.7f),new Vector3(0,0,-.1f));
                DuckReview.Capture(Folder,"Magpie-"+pose+"-top",new Vector3(.01f,2.3f,-.1f),new Vector3(0,0,-.1f));
                if(pose=="fold" || pose=="half-fold" || pose=="brake")
                    DuckReview.Capture(Folder,"Magpie-"+pose+"-side",new Vector3(1.8f,.12f,-.1f),new Vector3(0,-.04f,-.1f));
                var avian=d.GetComponent<AvianWingPresentation>();var rig=d.GetComponent<BirdRigDriver>();
                log.AppendLine(pose+": "+JsonUtility.ToJson(avian.State)+" reach="+rig.LeftReachError+","+rig.RightReachError+" phase="+c.State.Phase);
            }
            Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"poses.txt"),log.ToString());
        }
    }
}
