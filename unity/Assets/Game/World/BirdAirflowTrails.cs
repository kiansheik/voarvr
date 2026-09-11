using UnityEngine;
using UnityEngine.Rendering;
using VoarVR.Flight;

namespace VoarVR.World
{
    // Two bounded wingtip histories: actual articulated tips leave an air-relative wake.
    public sealed class BirdAirflowTrails : MonoBehaviour
    {
        public const int Points=20;
        private readonly Vector3[][] positions={new Vector3[Points],new Vector3[Points]};
        private readonly Vector3[][] drawn={new Vector3[Points],new Vector3[Points]};
        private readonly LineRenderer[] lines=new LineRenderer[2];
        private BirdFlightDriver bird;
        private BirdRigDriver rig;
        private WorldSpace space;
        private Material material;
        private float lastTime=-1,nextPoint;
        private Vector3 previousPosition;
        public LineRenderer GetTrail(int index) => lines[index];
        public int VisibleTrails => (lines[0]!=null&&lines[0].enabled?1:0)+(lines[1]!=null&&lines[1].enabled?1:0);
        public void Configure(BirdFlightDriver driver,Material source,WorldSpace worldSpace)
        {
            if(source==null)return;
            bird=driver;rig=driver.GetComponent<BirdRigDriver>();space=worldSpace;
            if(space!=null)space.Rebased+=Rebase;
            material=new Material(source);material.SetColor("_BaseColor",Color.white);material.SetFloat("_PulseFloor",.7f);
            for(int i=0;i<2;i++)
            {
                var go=new GameObject(i==0?"Left wing airflow":"Right wing airflow");go.transform.SetParent(transform,false);
                var line=go.AddComponent<LineRenderer>();line.positionCount=Points;line.useWorldSpace=true;
                line.sharedMaterial=material;line.widthMultiplier=.025f;line.shadowCastingMode=ShadowCastingMode.Off;
                line.receiveShadows=false;line.lightProbeUsage=LightProbeUsage.Off;line.reflectionProbeUsage=ReflectionProbeUsage.Off;
                line.enabled=false;lines[i]=line;
            }
        }
        private void Rebase(Vector3 delta)
        {
            previousPosition-=delta;
            for(int i=0;i<2;i++)for(int p=0;p<Points;p++)positions[i][p]-=delta;
        }
        public void ClearHistory()
        {
            lastTime = -1;
            for (int i = 0; i < lines.Length; i++) if (lines[i] != null) lines[i].enabled = false;
        }
        public void TickTrails()
        {
            if(bird==null||bird.Controller==null||rig==null||material==null)return;
            var c=bird.Controller;float time=c.SimulationTime;
            bool reset=lastTime<0||time<lastTime||Vector3.Distance(previousPosition,bird.transform.position)>20;
            float dt=reset?0:Mathf.Clamp(time-lastTime,0,.05f);
            float speed=(c.State.Velocity-c.WindVelocity).magnitude;
            // Cyan to violet distinguishes speed from gold rising-air navigation markers.
            var color=VoarVR.UI.FlightHud.SpeedColor(speed);
            for(int i=0;i<2;i++)
            {
                var tip=i==0?rig.leftTip:rig.rightTip;if(tip==null){lines[i].enabled=false;continue;}
                if(reset)for(int p=0;p<Points;p++)positions[i][p]=tip.position;
                else for(int p=0;p<Points;p++)positions[i][p]+=c.WindVelocity*dt;
                if(time>=nextPoint)for(int p=Points-1;p>0;p--)positions[i][p]=positions[i][p-1];
                positions[i][0]=tip.position;
                float alpha=Mathf.InverseLerp(2,8,speed)*.55f;
                lines[i].startColor=new Color(color.r,color.g,color.b,alpha);
                lines[i].endColor=new Color(color.r,color.g,color.b,0);
                lines[i].widthMultiplier=Mathf.Lerp(.04f,.09f,Mathf.InverseLerp(3,22,speed));
                DrawShortWake(i,Mathf.Clamp(bird.CharacterRestHalfSpan*2.5f,1.6f,4f));
                lines[i].SetPositions(drawn[i]);lines[i].enabled=alpha>.01f&&c.State.Phase!=FlightPhase.Perched&&c.State.Phase!=FlightPhase.Paused;
            }
            if(reset||time>=nextPoint)nextPoint=time+.035f;
            lastTime=time;previousPosition=bird.transform.position;
        }
        private void DrawShortWake(int side,float maximumLength)
        {
            // Resample the recent history by distance so fast flight never sends the
            // entire visible wake behind the chase camera. Preserve the actual path.
            var history=positions[side];float length=0;
            for(int i=1;i<Points;i++)length+=Vector3.Distance(history[i-1],history[i]);
            length=Mathf.Min(length,maximumLength);
            int segment=1;float consumed=0;
            drawn[side][0]=history[0];
            for(int p=1;p<Points;p++)
            {
                float target=length*p/(Points-1);
                float span=Vector3.Distance(history[segment-1],history[segment]);
                while(segment<Points-1&&consumed+span<target)
                { consumed+=span;segment++;span=Vector3.Distance(history[segment-1],history[segment]); }
                drawn[side][p]=Vector3.Lerp(history[segment-1],history[segment],span>.0001f?(target-consumed)/span:0);
            }
        }
        private void LateUpdate()=>TickTrails();
        private void OnDestroy(){if(space!=null)space.Rebased-=Rebase;if(material!=null)Destroy(material);}
    }
}
