using System;
using UnityEngine;
using VoarVR.World;
using VoarVR.Flight;

namespace VoarVR.Gameplay
{
    public enum FlightActivity { FreeFlight, Training, SkywardExpedition, RidgeJourney }
    public enum ObjectiveKind { DiscoverLift, Soar, Precision, Land, Acrobatic, Migration }
    public enum ChallengeStatus { Available, Active, Completed, Failed }
    public static class ActivitySelection { public static FlightActivity Chosen=FlightActivity.FreeFlight; }
    [Serializable] public sealed class FlightProgress
    {
        public int Version=1;
        public int SkywardBest, RidgeBest, TrainingBest, TrickBest;
        public bool RidgeUnlocked=>SkywardBest>0;
        public void Apply(FlightActivity activity,int score,int tricks)
        {if(activity==FlightActivity.SkywardExpedition)SkywardBest=Math.Max(SkywardBest,score);if(activity==FlightActivity.RidgeJourney)RidgeBest=Math.Max(RidgeBest,score);if(activity==FlightActivity.Training)TrainingBest=Math.Max(TrainingBest,score);TrickBest=Math.Max(TrickBest,tricks);}
    }
    public readonly struct ChallengeObservation
    {
        public readonly LogicalPosition Position;
        public readonly Vector3 Velocity;
        public readonly float VerticalAir,Stroke,Dt;
        public readonly bool Landed;
        public readonly int Collisions,Tricks;
        public ChallengeObservation(LogicalPosition p,Vector3 v,float air,float stroke,float dt,bool landed,int collisions,int tricks)
        {Position=p;Velocity=v;VerticalAir=air;Stroke=stroke;Dt=dt;Landed=landed;Collisions=collisions;Tricks=tricks;}
    }
    public sealed class FlightChallenge
    {
        public FlightActivity Activity {get;}
        public ChallengeStatus Status {get;private set;}=ChallengeStatus.Active;
        public int Stage {get;private set;}
        public float Progress {get;private set;}
        public float Elapsed {get;private set;}
        public int Score {get;private set;}
        public int Medal=>Score>=850?3:Score>=600?2:Score>0?1:0;
        public float SoaringGain {get;private set;}
        public double HighestAltitude {get;private set;}
        public float RequiredGain=>Activity==FlightActivity.Training?30:100;
        public float RequiredAltitude=>Activity==FlightActivity.Training?50:230;
        public float StrokeSeconds {get;private set;}
        private LogicalPosition previous;
        private bool hasPrevious;
        private int initialCollisions,startingCollisions;
        private bool countsInitialized;
        public readonly LogicalPosition Destination;
        public FlightChallenge(FlightActivity activity,int seed=7319)
        {Activity=activity;Destination=activity==FlightActivity.RidgeJourney?FlightRegions.Island(seed,1,0):FlightRegions.Island(seed,0,0);if(activity==FlightActivity.FreeFlight)Status=ChallengeStatus.Available;}
        public ObjectiveKind Kind=>Activity==FlightActivity.Training?(Stage==0?ObjectiveKind.DiscoverLift:ObjectiveKind.Soar):Stage==0?ObjectiveKind.DiscoverLift:Stage==1?ObjectiveKind.Soar:Stage==2?(Activity==FlightActivity.RidgeJourney?ObjectiveKind.Migration:ObjectiveKind.Precision):Stage==3 && Activity==FlightActivity.RidgeJourney?ObjectiveKind.Precision:ObjectiveKind.Land;
        public string Title=>Activity==FlightActivity.Training?"LEARNING THE AIR":Activity==FlightActivity.RidgeJourney?"RIDGE JOURNEY":"SKYWARD EXPEDITION";
        public string Instruction=>Status==ChallengeStatus.Completed?"RESULT SAVED · CONTINUE EXPLORING":Kind==ObjectiveKind.DiscoverLift?"Find the golden seeds above the valley":Kind==ObjectiveKind.Soar?"Circle in rising air · gain height with quiet wings":Kind==ObjectiveKind.Migration?"Follow the bright ridge around the rain front":Kind==ObjectiveKind.Precision?"Fly through the split stone arch": "Land on the garden terrace";
        public LogicalPosition Target(double time)=>(Kind==ObjectiveKind.DiscoverLift || Kind==ObjectiveKind.Soar)?FlightRegions.DepartureThermal(time):Kind==ObjectiveKind.Migration?new LogicalPosition(770,Destination.Y+20,240):Kind==ObjectiveKind.Precision?new LogicalPosition(Destination.X,Destination.Y+17,Destination.Z-65):Destination;
        public void Step(ChallengeObservation o)
        {
            if(Status!=ChallengeStatus.Active || o.Dt<=0)return;
            if(!countsInitialized){initialCollisions=startingCollisions=o.Collisions;countsInitialized=true;}
            HighestAltitude=Math.Max(HighestAltitude,o.Position.Y);
            Elapsed+=o.Dt;if(o.Stroke>.5f)StrokeSeconds+=o.Dt;
            if(Kind==ObjectiveKind.DiscoverLift)
            {
                var t=FlightRegions.DepartureThermal(Elapsed);
                if(HorizontalDistance(o.Position,t)<95 && o.VerticalAir>2 && !o.Landed){Stage++;Progress=0;}
            }
            else if(Kind==ObjectiveKind.Soar)
            {
                if(hasPrevious && o.VerticalAir>1.5f && o.Stroke<.5f && !o.Landed)
                    SoaringGain+=Mathf.Max(0,(float)(o.Position.Y-previous.Y));
                float required=RequiredGain;
                Progress=Mathf.Min(Mathf.Clamp01(SoaringGain/required),Mathf.Clamp01((float)HighestAltitude/RequiredAltitude));
                if(SoaringGain>=required && HighestAltitude>=RequiredAltitude)
                {Stage++;Progress=0;if(Activity==FlightActivity.Training)Complete(o);}
            }
            else if(Kind==ObjectiveKind.Migration)
            {Progress=Mathf.Clamp01(1-(float)HorizontalDistance(o.Position,Target(Elapsed))/700);if(HorizontalDistance(o.Position,Target(Elapsed))<45 && o.Position.Y>230){Stage++;Progress=0;}}
            else if(Kind==ObjectiveKind.Precision && hasPrevious)
            {
                var target=Target(Elapsed);
                // Swept crossing of the real arch plane, not proximity to a UI ring.
                double dz=o.Position.Z-previous.Z;
                if(Math.Abs(dz)>1e-6)
                {
                    double t=(target.Z-previous.Z)/dz;
                    if(t>=0 && t<=1)
                    {
                        double x=previous.X+(o.Position.X-previous.X)*t-target.X,y=previous.Y+(o.Position.Y-previous.Y)*t-target.Y;
                        if(Math.Abs(x)<13 && Math.Abs(y)<13 && o.Collisions==initialCollisions){Stage++;Progress=0;}
                    }
                }
                // A collision costs score; it never permanently prevents another approach.
                initialCollisions=o.Collisions;
            }
            else if(Kind==ObjectiveKind.Land && o.Landed && HorizontalDistance(o.Position,Destination)<25 && Math.Abs(o.Position.Y-Destination.Y)<4)
                Complete(o);
            previous=o.Position;hasPrevious=true;
        }
        public void Fail(){if(Status==ChallengeStatus.Active)Status=ChallengeStatus.Failed;}
        private void Complete(ChallengeObservation o)
        {Status=ChallengeStatus.Completed;Progress=1;Score=Mathf.Max(100,1000-Mathf.RoundToInt(StrokeSeconds*2)-(o.Collisions-startingCollisions)*40-Mathf.RoundToInt(Mathf.Max(0,Elapsed-600)*.2f)+Mathf.Min(o.Tricks,3)*75);}
        public static double HorizontalDistance(LogicalPosition a,LogicalPosition b)=>Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
    }
}
