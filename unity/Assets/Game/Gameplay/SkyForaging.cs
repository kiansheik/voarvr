using UnityEngine;
using UnityEngine.Rendering;
using VoarVR.Flight;
using VoarVR.World;
namespace VoarVR.Gameplay
{
    public interface IForagingBestStorage
    {
        int Load();
        bool Save(int best);
    }
    public sealed class PlayerPrefsForagingBestStorage : IForagingBestStorage
    {
        public int Load() => PlayerPrefs.GetInt(FlightJourneyStore.ForagingKey, 0);
        public bool Save(int best)
        {
            try
            {
                if (best > Load()) { PlayerPrefs.SetInt(FlightJourneyStore.ForagingKey, best); PlayerPrefs.Save(); }
                return true;
            }
            catch (System.Exception) { return false; }
        }
    }
    // One bounded dynamic mesh, no triggers or per-creature GameObjects.
    public sealed class SkyForaging:MonoBehaviour
    {
#if UNITY_EDITOR
        public static System.Func<IForagingBestStorage> EditorStorageOverride;
#endif
        private static IForagingBestStorage CreateDefaultStorage()
        {
#if UNITY_EDITOR
            if(EditorStorageOverride!=null)return EditorStorageOverride();
#endif
            return new PlayerPrefsForagingBestStorage();
        }
        public const int Capacity=24;
        private readonly LogicalPosition[] homes=new LogicalPosition[Capacity];
        private readonly float[] available=new float[Capacity];
        private readonly bool[] placed=new bool[Capacity];
        private BirdFlightDriver bird;private WorldSpace space;private WindField wind;
        private Mesh mesh;private Vector3[] source,vertices,sourceNormals,normals;private int[] triangles;private Color[] colors;
        private TextMesh counter;private Vector3 previous;private bool previousValid;private float noticeUntil,nextText;private int reward,best;
        private ExpeditionDirector expedition;
        private IForagingBestStorage bestStorage;
        private int persistedBest;
        private float saveElapsed;
        private bool journeySaveNeedsRetry,catchWasAllowed;
        public const float SaveIntervalSeconds=5f;
        public bool HasUnsavedBest=>Mathf.Max(best,Score.Points)>persistedBest;
        public ForagingScore Score {get;}=new ForagingScore();
        public void Configure(BirdFlightDriver driver,WorldSpace coordinates,WindField field)
        {
            bird=driver;space=coordinates;wind=field;ConfigurePersistence(driver!=null?driver.Expedition:null);
            var kit=Resources.Load<SkywardKit>("SkywardKit");var model=kit!=null?kit.Find("SunMoth"):null;if(model==null){enabled=false;return;}
            source=model.vertices;sourceNormals=model.normals;normals=new Vector3[source.Length*Capacity];vertices=new Vector3[source.Length*Capacity];colors=new Color[vertices.Length];triangles=new int[model.triangles.Length*Capacity];var baseTriangles=model.triangles;var baseColors=model.colors;
            for(int i=0;i<Capacity;i++){for(int v=0;v<source.Length;v++)colors[i*source.Length+v]=baseColors[v];for(int t=0;t<baseTriangles.Length;t++)triangles[i*baseTriangles.Length+t]=baseTriangles[t]+i*source.Length;}
            mesh=new Mesh{name="Sun moth flock"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.triangles=triangles;mesh.colors=colors;
            var go=new GameObject("Sun moths");go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=kit.Material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        public void ConfigurePersistence(ExpeditionDirector director,IForagingBestStorage storage=null)
        {
            expedition=director;bestStorage=storage??CreateDefaultStorage();
            best=Mathf.Max(bestStorage.Load(),director?.Journey?.ForagingBest??0);
            persistedBest=best;saveElapsed=0;journeySaveNeedsRetry=false;
        }
        public bool SaveBest()
        {
            best=Mathf.Max(best,Score.Points);
            if(bestStorage==null || best<=persistedBest)return true;
            if(expedition!=null && expedition.Journey!=null)
            {
                if(!expedition.Journey.CanWrite)return false;
                bool alreadyRecorded=expedition.Journey.ForagingBest>=best;
                bool saved=journeySaveNeedsRetry && alreadyRecorded?expedition.SaveCheckpoint():expedition.RecordForagingBest(best);
                journeySaveNeedsRetry=!saved;
                if(!saved)return false;
            }
            if(!bestStorage.Save(best))return false;
            persistedBest=best;saveElapsed=0;return true;
        }
        public void InvalidateSweep(){previousValid=false;catchWasAllowed=false;}
        public void Tick(float dt)
        {
            // Changed bests are checkpointed at a bounded cadence; explicit/lifecycle saves
            // flush immediately. No disk write is needed for frames without a new record.
            if(!float.IsNaN(dt) && !float.IsInfinity(dt) && dt>0)saveElapsed+=dt;
            if(HasUnsavedBest && saveElapsed>=SaveIntervalSeconds){SaveBest();saveElapsed=0;}
            if(mesh==null || bird==null)return;var c=bird.Controller;float time=c.SimulationTime;var p=c.State.Position;
            bool canCatch=c.State.Phase!=FlightPhase.Paused && c.State.Phase!=FlightPhase.Perched && !c.StreamingBlocked && (!bird.UsesXR || bird.Calibration.Captured);
            bool reset=c.LastInput.ResetPressed || !previousValid || (p-previous).sqrMagnitude>10000;
            if(reset){previous=p;previousValid=true;}
            var heading=bird.Heading;
            for(int i=0;i<Capacity;i++)
            {
                var center=space.ToLocal(homes[i].X,homes[i].Y,homes[i].Z);
                if(!placed[i] || Vector3.Distance(p,center)>140 || reset)
                {
                    float angle=i*2.399f;var offset=heading*new Vector3(Mathf.Sin(angle)*(8+i%4*4),Mathf.Sin(i*1.7f)*4,18+i*3.6f);
                    center=p+offset;
                    if(i%3==0 && wind!=null && wind.Mode!=WindMode.StillAir)
                    {
                        var source=FlightRegions.DepartureThermal(time);var target=space.ToLocal(source.X,p.y,source.Z);var direction=target-p;direction.y=0;
                        if(direction.magnitude>15 && direction.magnitude<140)center=p+direction.normalized*(16+i*2)+heading*Vector3.right*Mathf.Sin(angle)*6+Vector3.up*2;
                    }
                    center.y=Mathf.Max(center.y,WorldTerrain.Elevation(space.Seed,space.ToLogical(center).X,space.ToLogical(center).Z)+5);
                    homes[i]=space.ToLogical(center);placed[i]=true;available[i]=time+1;
                }
                // Gentle wandering, recognizable wings, generous swept collection radius.
                var food=center+new Vector3(Mathf.Sin(time*.55f+i)*2,Mathf.Sin(time*.8f+i*2)*1.2f,Mathf.Cos(time*.45f+i)*2);
                bool visible=time>=available[i];
                if(visible && canCatch && catchWasAllowed && !reset && ForagingScore.Touches(previous,p,food,2.2f))
                {reward=Score.Catch(time);best=Mathf.Max(best,Score.Points);available[i]=time+25;noticeUntil=Time.unscaledTime+2.5f;bird.GetComponent<FlightFeedback>()?.SnackCue(Score.Combo);visible=false;}
                var local=transform.InverseTransformPoint(food);var rotation=Quaternion.Euler(0,time*14+i*53,0);float flutter=.45f+.55f*Mathf.Abs(Mathf.Sin(time*8+i));
                for(int v=0;v<source.Length;v++){vertices[i*source.Length+v]=visible?local+rotation*Vector3.Scale(source[v],new Vector3(flutter,1,1)):local;normals[i*source.Length+v]=rotation*sourceNormals[v];}
            }
            previous=p;catchWasAllowed=canCatch;mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();
            if(Time.unscaledTime>=nextText){nextText=Time.unscaledTime+.2f;UpdateCounter();}
        }
        private void UpdateCounter()
        {
            bool visible=bird!=null && bird.ShowFlightText;
            if(counter!=null)counter.gameObject.SetActive(visible);
            if(!visible)return;
            if(counter==null && Camera.main!=null)
            {var go=new GameObject("Foraging score");go.transform.SetParent(Camera.main.transform,false);go.transform.localPosition=new Vector3(.72f,-.4f,2);counter=go.AddComponent<TextMesh>();counter.anchor=TextAnchor.MiddleRight;counter.fontSize=40;counter.characterSize=.009f;counter.color=new Color(1,.82f,.36f);go.AddComponent<VoarVR.UI.FlightCard>();}
            if(counter==null)return;
            counter.text=Time.unscaledTime<noticeUntil?"+"+reward+"  TASTY!  x"+Score.Combo+"\n"+Score.Points+" POINTS":Score.Points>0?Score.Points+" POINTS  ·  BEST "+best:"SUN MOTHS\nFly through to eat +10";
        }
        private void OnApplicationPause(bool paused){if(paused){InvalidateSweep();SaveBest();}}
        private void OnApplicationFocus(bool focused){if(!focused){InvalidateSweep();SaveBest();}}
        private void OnDisable(){InvalidateSweep();SaveBest();}
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(counter!=null)Destroy(counter.gameObject);}
    }
}
