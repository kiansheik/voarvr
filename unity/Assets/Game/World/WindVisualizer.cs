using System;
using UnityEngine;
using UnityEngine.Rendering;
using VoarVR.Flight;

namespace VoarVR.World
{
    // Fixed pool of actual advected particle histories. No per-frame path rebuilding.
    public sealed class WindVisualizer : MonoBehaviour
    {
        public const int RibbonCapacity=24, PointsPerRibbon=12;
        private sealed class Ribbon
        {
            public LineRenderer Line;
            public readonly Vector3[] Points=new Vector3[PointsPerRibbon];
            public Vector3 Head;
            public float Born,Duration,NextPoint;
            public int Generation;
        }
        private readonly Ribbon[] ribbons=new Ribbon[RibbonCapacity];
        private readonly AtmosphereModel.Plume[] nearestPlumes=new AtmosphereModel.Plume[3];
        private readonly double[] nearestDistances=new double[3];
        private WindField wind;
        private WorldSpace space;
        private BirdFlightDriver driver;
        private Material lift,sink, neutralTint;
        private float lastTime=-1;
        private bool initialized;
        private WindMode lastMode;
        private static readonly int ClockId=Shader.PropertyToID("_FlightWindTime");
        public int ActiveRibbonCount { get; private set; }
        public int SamplesLastFrame { get; private set; }
        public void Configure(WindField field,WorldSpace worldSpace,BirdFlightDriver flight,Material rising,Material falling)
        {
            if(space!=null) space.Rebased-=Rebase;
            wind=field;space=worldSpace;driver=flight;lift=rising;sink=falling;
            if(space!=null) space.Rebased+=Rebase;
            if(initialized) return;
            neutralTint=new Material(rising);neutralTint.SetColor("_BaseColor",Color.white);
            for(int i=0;i<RibbonCapacity;i++)
            {
                var go=new GameObject("Air ribbon "+i);
                go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();
                line.useWorldSpace=true;line.positionCount=PointsPerRibbon;
                line.widthMultiplier=.75f;line.numCornerVertices=1;line.numCapVertices=0;
                line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
                line.lightProbeUsage=LightProbeUsage.Off;line.reflectionProbeUsage=ReflectionProbeUsage.Off;
                line.motionVectorGenerationMode=MotionVectorGenerationMode.ForceNoMotion;
                line.sharedMaterial=lift;line.enabled=false;
                ribbons[i]=new Ribbon { Line=line, Born=-1000,Duration=12 };
            }
            initialized=true;
        }
        private void OnDestroy() { if(space!=null) space.Rebased-=Rebase; if(neutralTint!=null)Destroy(neutralTint); }
        private void Rebase(Vector3 delta)
        {
            if(!initialized) return;
            foreach(var ribbon in ribbons)
            {
                ribbon.Head-=delta;
                for(int p=0;p<PointsPerRibbon;p++) ribbon.Points[p]-=delta;
                ribbon.Line.SetPositions(ribbon.Points);
            }
        }
        private void Spawn(Ribbon ribbon,int index,float time)
        {
            var player=space.ToLogical(driver.transform.position);
            long cx=(long)Math.Floor(player.X/AtmosphereModel.CellSize),cz=(long)Math.Floor(player.Z/AtmosphereModel.CellSize);
            uint h=AtmosphereModel.Hash(space.Seed,cx+index,cz,ribbon.Generation++);
            double x,y,z;
            if(index%3!=0)
            {
                // Reveal the nearest active features, rather than distributing the small
                // pool across distant cells whose ribbons cannot be seen from the bird.
                FindNearestPlumes(player,cx,cz,time);
                var plume=nearestPlumes[(index/3)%3];
                y=Math.Max(8,Math.Max(plume.Altitude-50,Math.Min(plume.Altitude+50,player.Y))
                    +((index%4)-1.5)*8);
                var center=plume.Center(time,y);
                x=center.X+((h&255)/255f-.5f)*plume.Radius;
                z=center.Z+(((h>>8)&255)/255f-.5f)*plume.Radius;
            }
            else
            {
                // Eight ambient traces fade into a broad forward fan. Their placement
                // aids discovery; their direction and motion still come only from wind.
                var forward=driver.Heading*Vector3.forward;
                var right=driver.Heading*Vector3.right;
                float distance=22+(h&1023)/1023f*65;
                float lateral=(((h>>10)&1023)/1023f-.5f)*Mathf.Min(80,distance*1.1f);
                x=player.X+forward.x*distance+right.x*lateral;
                z=player.Z+forward.z*distance+right.z*lateral;
                y=Math.Max(8,player.Y+((index/3)%3-1)*Mathf.Min(20,distance*.35f));
            }
            y=Math.Max(y,WorldTerrain.Elevation(space.Seed,x,z)+5);
            ribbon.Head=space.ToLocal(x,y,z);
            ribbon.Born=time; ribbon.Duration=10+index%5; ribbon.NextPoint=time+.35f;
            var point=ribbon.Head;
            for(int p=0;p<PointsPerRibbon;p++)
            {
                ribbon.Points[p]=point;
                point-=wind.Sample(point,time)*.35f;
                SamplesLastFrame++;
            }
        }
        private void FindNearestPlumes(LogicalPosition player,long cx,long cz,float time)
        {
            for(int i=0;i<3;i++) nearestDistances[i]=double.PositiveInfinity;
            long epoch=(long)Math.Floor(time/AtmosphereModel.EpochSeconds);
            for(long x=cx-1;x<=cx+1;x++) for(long z=cz-1;z<=cz+1;z++) for(long e=epoch-1;e<=epoch;e++)
            {
                var plume=AtmosphereModel.GetPlume(space.Seed,x,z,e,wind.Mode);
                if(plume.Envelope(time)<.08f) continue;
                double altitude=Math.Max(plume.Altitude-50,Math.Min(plume.Altitude+50,player.Y));
                var center=plume.Center(time,altitude);
                double dx=center.X-player.X,dy=altitude-player.Y,dz=center.Z-player.Z;
                double distance=dx*dx+dy*dy+dz*dz;
                for(int i=0;i<3;i++)
                {
                    if(distance>=nearestDistances[i]) continue;
                    for(int j=2;j>i;j--) { nearestDistances[j]=nearestDistances[j-1];nearestPlumes[j]=nearestPlumes[j-1]; }
                    nearestDistances[i]=distance;nearestPlumes[i]=plume;break;
                }
            }
        }
        private void LateUpdate() => TickVisuals();

        public void TickVisuals()
        {
            if(!initialized || driver==null || driver.Controller==null || space==null || wind==null) return;
            float time=driver.Controller.SimulationTime;
            Shader.SetGlobalFloat(ClockId,time);
            SamplesLastFrame=0;ActiveRibbonCount=0;
            bool reset=lastTime<0 || time<lastTime || wind.Mode!=lastMode;
            float dt=reset ? 0 : Mathf.Min(.05f,time-lastTime);
            int count=wind.Mode==WindMode.StillAir ? 0 : wind.Mode==WindMode.Touring ? 16 : RibbonCapacity;
            int spawned=0;
            for(int i=0;i<RibbonCapacity;i++)
            {
                var r=ribbons[i];
                if(reset) r.Born=-1000;
                if(i>=count) { r.Line.enabled=false;continue; }
                if(time-r.Born>=r.Duration)
                {
                    r.Line.enabled=false;
                    if(spawned>=2) continue; // Bound initial/restart work as well as recycling.
                    Spawn(r,i,time);spawned++;
                }
                var velocity=wind.Sample(r.Head,time);SamplesLastFrame++;
                r.Head+=velocity*dt;
                if(time>=r.NextPoint)
                {
                    for(int p=PointsPerRibbon-1;p>0;p--) r.Points[p]=r.Points[p-1];
                    r.NextPoint=time+.35f;
                }
                r.Points[0]=r.Head;
                float age=time-r.Born;
                float alpha=Mathf.SmoothStep(0,1,Mathf.Min(age/2,(r.Duration-age)/2));
                float proximity=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(110,210,Vector3.Distance(r.Head,driver.transform.position)));
                alpha*=proximity*(wind.Mode==WindMode.Touring ? .47f : .86f);
                var tint=VoarVR.UI.FlightHud.AirColor(velocity.y);
                r.Line.startColor=new Color(tint.r,tint.g,tint.b,alpha*.72f);
                r.Line.endColor=new Color(tint.r,tint.g,tint.b,alpha*.06f);
                r.Line.sharedMaterial=neutralTint;
                r.Line.SetPositions(r.Points);r.Line.enabled=alpha>.005f;
                if(r.Line.enabled) ActiveRibbonCount++;
            }
            lastTime=time;lastMode=wind.Mode;
        }
    }
}
