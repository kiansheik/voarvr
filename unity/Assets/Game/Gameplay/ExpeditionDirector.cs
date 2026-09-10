using UnityEngine;
using VoarVR.Flight;
using VoarVR.World;
using VoarVR.Telemetry;
namespace VoarVR.Gameplay
{
    public sealed class ExpeditionDirector:MonoBehaviour
    {
        private const string SaveKey="VoarVR.FlightProgress.v1";
        public FlightChallenge Challenge {get;private set;}
        public FlightProgress Progress {get;private set;}
        private BirdFlightDriver driver;
        private WorldSpace space;
        private TextMesh caption,beacon;
        private bool unlockedBefore;
        private int previousBest;
        private float nextText;
        public static FlightProgress LoadProgress()
        {try{return JsonUtility.FromJson<FlightProgress>(PlayerPrefs.GetString(SaveKey,"{}"))??new FlightProgress();}catch{return new FlightProgress();}}
        public void Configure(BirdFlightDriver bird,WorldSpace worldSpace)
        {driver=bird;space=worldSpace;Progress=LoadProgress();unlockedBefore=Progress.RidgeUnlocked;previousBest=ActivitySelection.Chosen==FlightActivity.SkywardExpedition?Progress.SkywardBest:ActivitySelection.Chosen==FlightActivity.RidgeJourney?Progress.RidgeBest:Progress.TrainingBest;Challenge=new FlightChallenge(ActivitySelection.Chosen,space!=null?space.Seed:7319);}
        public void Restart(){if(Challenge!=null)Challenge=new FlightChallenge(Challenge.Activity,space!=null?space.Seed:7319);}
        public void Tick(float dt)
        {
            if(driver==null || Challenge==null || Challenge.Status!=ChallengeStatus.Active)return;
            var c=driver.Controller;if(c.State.Phase==FlightPhase.Paused || c.StreamingBlocked)return;
            if(c.LastInput.ResetPressed){Challenge=new FlightChallenge(Challenge.Activity,space!=null?space.Seed:7319);return;}
            if(driver.UsesXR && !driver.Calibration.Captured)return;
            var p=space!=null?space.ToLogical(c.State.Position):new LogicalPosition(c.State.Position.x,c.State.Position.y,c.State.Position.z);
            var before=Challenge.Status;
            Challenge.Step(new ChallengeObservation(p,c.State.Velocity,c.WindVelocity.y,c.StrokeForce.magnitude,dt,c.State.Phase==FlightPhase.Perched,c.CollisionCount,c.Tricks.Count));
            if(before!=Challenge.Status && Challenge.Status==ChallengeStatus.Completed)
            {Progress.Apply(Challenge.Activity,Challenge.Score,c.Tricks.Score);PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(Progress));PlayerPrefs.Save();}
        }
        private void LateUpdate()
        {
            bool visible=driver!=null && driver.ShowFlightText;
            if(caption!=null)caption.gameObject.SetActive(visible);
            if(beacon!=null)beacon.gameObject.SetActive(visible);
            if(!visible)return;
            if(Challenge==null || Challenge.Activity==FlightActivity.FreeFlight || Time.unscaledTime<nextText)return;
            nextText=Time.unscaledTime+.25f;
            if(caption==null && Camera.main!=null)
            {var go=new GameObject("Expedition card");go.transform.SetParent(Camera.main.transform,false);go.transform.localPosition=new Vector3(-.7f,.42f,2);caption=go.AddComponent<TextMesh>();caption.fontSize=40;caption.characterSize=.009f;caption.anchor=TextAnchor.UpperLeft;caption.color=new Color(.88f,.96f,1);go.AddComponent<VoarVR.UI.FlightCard>();}
            if(caption==null)return;
            if(beacon==null)
            {var go=new GameObject("Expedition destination");beacon=go.AddComponent<TextMesh>();beacon.fontSize=48;beacon.characterSize=.08f;beacon.anchor=TextAnchor.MiddleCenter;beacon.color=new Color(1,.78f,.32f);}
            var target=Challenge.Target(driver.Controller.SimulationTime);var location=space!=null?space.ToLocal(target.X,target.Y,target.Z):new Vector3((float)target.X,(float)target.Y,(float)target.Z);
            var eye=Camera.main;float distance=Vector3.Distance(eye.transform.position,location);beacon.transform.position=location+Vector3.up*(Challenge.Kind==ObjectiveKind.Land?2:7);beacon.transform.rotation=Quaternion.LookRotation(beacon.transform.position-eye.transform.position);beacon.transform.localScale=Vector3.one*Mathf.Clamp(distance/80,.2f,7);
            beacon.text=Challenge.Status==ChallengeStatus.Completed?"":Challenge.Kind==ObjectiveKind.DiscoverLift?"RISING AIR":Challenge.Kind==ObjectiveKind.Soar?"CIRCLE IN GOLDEN AIR":Challenge.Kind==ObjectiveKind.Migration?"BRIGHT RIDGE":Challenge.Kind==ObjectiveKind.Precision?"SKYWARD ARCH":"GARDEN TERRACE";

            if(Challenge.Status==ChallengeStatus.Completed)caption.text=Challenge.Title+"\n"+new string('★',Challenge.Medal)+"  "+Challenge.Score+"   PREVIOUS BEST "+previousBest+"\n"+(!unlockedBefore && Progress.RidgeUnlocked?"NEW: Ridge Journey · Left Menu":"Result saved · continue flying")+"\nGold 850 / Silver 600 · Quiet wings + clean approach";
            else caption.text=Challenge.Title+"  "+(Challenge.Stage+1)+"/"+(Challenge.Activity==FlightActivity.Training?2:Challenge.Activity==FlightActivity.RidgeJourney?5:4)+"\n"+Challenge.Instruction+"\n"+Mathf.RoundToInt(distance)+" m · Optional: trick +75"+(Challenge.Kind==ObjectiveKind.Soar?"\n"+Mathf.RoundToInt(Challenge.SoaringGain)+" / "+Challenge.RequiredGain+" m quiet climb · High "+Mathf.RoundToInt((float)Challenge.HighestAltitude)+" / "+Challenge.RequiredAltitude+" m":"");
        }
        private void OnDestroy(){if(caption!=null)Destroy(caption.gameObject);if(beacon!=null)Destroy(beacon.gameObject);}
    }
}
