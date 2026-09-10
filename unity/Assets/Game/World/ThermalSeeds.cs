using UnityEngine;
using VoarVR.Flight;
namespace VoarVR.World
{
    // One opaque mesh.192 motes,16 real-field probes/frame; cached flow advects smoothly.
    public sealed class ThermalSeeds:MonoBehaviour
    {
        public const int Capacity=192;
        private readonly Vector3[] points=new Vector3[Capacity],flow=new Vector3[Capacity],vertices=new Vector3[Capacity*4];
        private readonly Color[] colors=new Color[Capacity*4];
        private readonly float[] life=new float[Capacity];
        private Mesh mesh;private WindField wind;private WorldSpace space;private BirdFlightDriver bird;
        private int cursor;private float last=-1;
        public void Configure(WindField field,WorldSpace coordinates,BirdFlightDriver driver)
        {
            wind=field;space=coordinates;bird=driver;var kit=Resources.Load<SkywardKit>("SkywardKit");if(kit==null)return;
            mesh=new Mesh{name="Persistent rising-air seeds"};mesh.MarkDynamic();int[] triangles=new int[Capacity*6];
            for(int i=0;i<Capacity;i++){int a=i*4,t=i*6;triangles[t]=a;triangles[t+1]=a+1;triangles[t+2]=a+2;triangles[t+3]=a;triangles[t+4]=a+2;triangles[t+5]=a+3;}
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.colors=colors;
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;gameObject.AddComponent<MeshRenderer>().sharedMaterial=kit.Material;
            space.Rebased+=Rebase;
        }
        private void Rebase(Vector3 delta){for(int i=0;i<Capacity;i++)points[i]-=delta;}
        private void LateUpdate()
        {
            if(mesh==null || bird==null || bird.Controller==null)return;
            float time=bird.Controller.SimulationTime;bool reset=last<0 || time<last;float dt=reset?0:Mathf.Min(.05f,time-last);last=time;
            if(wind.Mode==WindMode.StillAir){GetComponent<MeshRenderer>().enabled=false;return;}GetComponent<MeshRenderer>().enabled=true;
            var logical=space.ToLogical(bird.transform.position);var departure=FlightRegions.DepartureThermal(time);
            var island=FlightRegions.Island(space.Seed,(long)System.Math.Floor(logical.X/768),(long)System.Math.Floor(logical.Z/768));
            var source=FlightChallengeDistance(logical,departure)<FlightChallengeDistance(logical,island)?departure:new LogicalPosition(island.X-90+System.Math.Sin(time*.004)*20,island.Y-40,island.Z-90);
            for(int j=0;j<16;j++)
            {
                int i=cursor++%Capacity;
                if(reset || life[i]<=0 || Vector3.Distance(points[i],bird.transform.position)>240)
                {
                    float a=i*2.39996f+time*.009f;float r=(i%4==0?55+(i%7)*4:8+(i%8)*5)*wind.UsabilityScale;
                    float y=Mathf.Clamp((float)logical.Y+(i%7-3)*12,12,(float)source.Y+130);
                    points[i]=space.ToLocal(source.X+Mathf.Cos(a)*r+(y-source.Y)*.09,y,source.Z+Mathf.Sin(a)*r+(y-source.Y)*.04);life[i]=18+i%13;
                }
                flow[i]=wind.Sample(points[i],time);
            }
            var eye=Camera.main;var right=eye!=null?eye.transform.right:Vector3.right;var up=eye!=null?eye.transform.up:Vector3.up;
            for(int i=0;i<Capacity;i++)
            {
                life[i]-=dt;points[i]+=flow[i]*dt;
                float strength=Mathf.Clamp01((flow[i].y-.5f)/6);float size=life[i]>0?.20f+strength*.32f:0;
                if(flow[i].y<.5f)size=0;
                var p=transform.InverseTransformPoint(points[i]);int a=i*4;
                vertices[a]=p-right*size;vertices[a+1]=p+up*size*2;vertices[a+2]=p+right*size;vertices[a+3]=p-up*size;
                Color c=Color.Lerp(new Color(.35f,.62f,.51f),new Color(1,.78f,.30f),strength);for(int k=0;k<4;k++)colors[a+k]=c;
            }
            mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
        }
        private static double FlightChallengeDistance(LogicalPosition a,LogicalPosition b)=>(a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z);
        private void OnDestroy(){if(space!=null)space.Rebased-=Rebase;if(mesh!=null)Destroy(mesh);}
    }
}
