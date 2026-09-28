using System;
using System.Collections.Generic;
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
        public const string TypedForagingKey = "VoarVR.ForagingBest.v2";
        public int Load() => PlayerPrefs.GetInt(TypedForagingKey, 0);
        public bool Save(int best)
        {
            try
            {
                if (best > Load()) { PlayerPrefs.SetInt(TypedForagingKey, best); PlayerPrefs.Save(); }
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
        private readonly CollectibleDefinition[] definitions=new CollectibleDefinition[Capacity];
        private readonly string[] objectiveIds=new string[Capacity];
        private BirdFlightDriver bird;private WorldSpace space;private WindField wind;
        private Mesh mesh;private Vector3[] source,vertices,sourceNormals,normals;private int[] triangles;private Color[] colors,baseColors;
        private TextMesh counter;private Vector3 previous;private bool previousValid;private float noticeUntil,nextText;private int reward,best;
        private IForagingBestStorage bestStorage;
        private int persistedBest;
        private float saveElapsed;
        private bool catchWasAllowed;
        private int objectiveCollectibleCount,objectiveAttemptSequence=int.MinValue;
        private string activeObjectiveId;
        private float objectiveStartedSimulationTime;
        public const float SaveIntervalSeconds=5f;
        public bool HasUnsavedBest=>Mathf.Max(best,Score.Points)>persistedBest;
        public int Best => Mathf.Max(best, Score.Points);
        public CatchResult LastCatch { get; private set; }
        public float LastCatchNoticeUntil => noticeUntil;
        public int CatchSequence { get; private set; }
        public event Action<CatchResult> Caught;
        public ForagingScore Score {get;}=new ForagingScore();
        public int CourseObjectiveSlotCount=>objectiveCollectibleCount;
        public string CourseObjectiveId=>activeObjectiveId;
        public string ObjectiveIdAt(int index)=>index>=0&&index<Capacity?objectiveIds[index]:null;
        public LogicalPosition HomeAt(int index)=>index>=0&&index<Capacity?homes[index]:default;
        public float AvailabilityAt(int index)=>index>=0&&index<Capacity?available[index]:float.NaN;
        public void Configure(BirdFlightDriver driver,WorldSpace coordinates,WindField field)
        {
            bird=driver;space=coordinates;wind=field;ConfigurePersistence(driver!=null?driver.Expedition:null);
            var kit=Resources.Load<SkywardKit>("SkywardKit");var model=kit!=null?kit.Find("SunMoth"):null;if(model==null){enabled=false;return;}
            source=model.vertices;sourceNormals=model.normals;baseColors=model.colors;normals=new Vector3[source.Length*Capacity];vertices=new Vector3[source.Length*Capacity];colors=new Color[vertices.Length];triangles=new int[model.triangles.Length*Capacity];var baseTriangles=model.triangles;
            for(int i=0;i<Capacity;i++)
            {
                definitions[i]=DefaultDefinition(i);
                TintInstance(i,baseColors,definitions[i]);
                for(int t=0;t<baseTriangles.Length;t++)triangles[i*baseTriangles.Length+t]=baseTriangles[t]+i*source.Length;
            }
            mesh=new Mesh{name="Sun moth flock"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.triangles=triangles;mesh.colors=colors;
            var go=new GameObject("Sun moths");go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=kit.Material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        public bool ActivateCourseObjective(int attemptSequence,string objectiveId,
            CollectibleDefinition definition,IReadOnlyList<LogicalPosition> logicalHomes)
        {
            if(attemptSequence<0||!StableIdContract.IsValid(objectiveId)||definition==null
                ||logicalHomes==null||logicalHomes.Count<1||logicalHomes.Count>Capacity)return false;
            for(int i=0;i<logicalHomes.Count;i++)if(!CourseGeometry.IsFinite(logicalHomes[i]))return false;
            if(objectiveAttemptSequence==attemptSequence
                &&string.Equals(activeObjectiveId,objectiveId,StringComparison.Ordinal))return true;
            ClearCourseObjective();
            objectiveAttemptSequence=attemptSequence;activeObjectiveId=objectiveId;
            objectiveCollectibleCount=logicalHomes.Count;
            objectiveStartedSimulationTime=bird?.Controller?.SimulationTime??0f;
            for(int i=0;i<objectiveCollectibleCount;i++)
            {
                definitions[i]=definition;objectiveIds[i]=objectiveId;homes[i]=logicalHomes[i];
                placed[i]=true;available[i]=float.NegativeInfinity;
                TintInstance(i,baseColors,definition);
            }
            if(mesh!=null)mesh.colors=colors;
            InvalidateSweep();
            return true;
        }

        public void ClearCourseObjective()
        {
            bool changed=false;
            for(int i=0;i<Capacity;i++)
            {
                if(string.IsNullOrEmpty(objectiveIds[i]))continue;
                objectiveIds[i]=null;definitions[i]=DefaultDefinition(i);placed[i]=false;available[i]=0;
                TintInstance(i,baseColors,definitions[i]);changed=true;
            }
            objectiveCollectibleCount=0;objectiveAttemptSequence=int.MinValue;
            activeObjectiveId=null;objectiveStartedSimulationTime=0;
            if(changed&&mesh!=null)mesh.colors=colors;
            if(changed)InvalidateSweep();
        }
        public void ConfigurePersistence(ExpeditionDirector director,IForagingBestStorage storage=null)
        {
            // Typed rarity/combo values are not comparable with the shipped Sun-Moth-only
            // Journey/Foraging v1 record. Keep that legacy data untouched and start v2 here.
            bestStorage=storage??CreateDefaultStorage();
            best=Mathf.Max(best,bestStorage.Load());
            persistedBest=best;saveElapsed=0;
        }
        public bool SaveBest()
        {
            best=Mathf.Max(best,Score.Points);
            if(bestStorage==null || best<=persistedBest)return true;
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
                bool objectiveSlot=!string.IsNullOrEmpty(objectiveIds[i]);
                // A ranked quota must never present identical ambient moths that award
                // points but cannot advance the task. Collapse every non-objective slot
                // for the lifetime of the active quota; its ambient state is left intact
                // and resumes only after ClearCourseObjective.
                if(!objectiveSlot&&!string.IsNullOrEmpty(activeObjectiveId))
                {
                    for(int v=0;v<source.Length;v++)
                    {vertices[i*source.Length+v]=Vector3.zero;normals[i*source.Length+v]=sourceNormals[v];}
                    continue;
                }
                if(!objectiveSlot && (!placed[i] || Vector3.Distance(p,center)>140 || reset))
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
                // Each rarity keeps the same readable moth silhouette while changing movement.
                var definition=definitions[i]??CollectibleCatalog.SunMoth;
                float animationTime=objectiveSlot?Mathf.Max(0,time-objectiveStartedSimulationTime):time;
                var food=AnimatedPosition(definition,center,p,animationTime,i);
                bool visible=time>=available[i];
                if(visible && canCatch && catchWasAllowed && !reset && ForagingScore.Touches(previous,p,food,2.2f))
                {
                    LastCatch=Score.Catch(definition,time,objectiveIds[i]);reward=LastCatch.AwardedValue;
                    best=Mathf.Max(best,Score.Points);available[i]=objectiveSlot?float.PositiveInfinity:time+RespawnSeconds(definition.Rarity);
                    noticeUntil=Time.unscaledTime+2.5f;CatchSequence++;
                    bird.GetComponent<FlightFeedback>()?.SnackCue(Score.Combo);Caught?.Invoke(LastCatch);visible=false;
                }
                var local=transform.InverseTransformPoint(food);var rotation=Quaternion.Euler(0,time*14+i*53,0);float flutter=.45f+.55f*Mathf.Abs(Mathf.Sin(time*8+i));
                float scale=RarityScale(definition.Rarity);
                for(int v=0;v<source.Length;v++){vertices[i*source.Length+v]=visible?local+rotation*Vector3.Scale(source[v]*scale,new Vector3(flutter,1,1)):local;normals[i*source.Length+v]=rotation*sourceNormals[v];}
            }
            previous=p;catchWasAllowed=canCatch;mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();
            if(Time.unscaledTime>=nextText){nextText=Time.unscaledTime+.2f;UpdateCounter();}
        }
        private static CollectibleDefinition DefaultDefinition(int index)
        {
            if(index%12==11)return CollectibleCatalog.CrownMoth;
            if(index%6==5)return CollectibleCatalog.EmberMoth;
            if(index%3==2)return CollectibleCatalog.MoonMoth;
            return CollectibleCatalog.SunMoth;
        }
        private static Vector3 AnimatedPosition(CollectibleDefinition definition,Vector3 center,Vector3 birdPosition,float time,int index)
        {
            float phase=time+index*1.73f;
            switch(definition.Behavior)
            {
                case CollectibleBehavior.Circle:
                    return center+new Vector3(Mathf.Sin(phase*1.25f)*3.2f,Mathf.Sin(phase*1.7f)*.8f,Mathf.Cos(phase*1.25f)*3.2f);
                case CollectibleBehavior.Dart:
                    return center+new Vector3(Mathf.Sin(phase*2.7f)*4f,Mathf.Sin(phase*3.9f)*1.5f,Mathf.Cos(phase*1.9f)*2.5f);
                case CollectibleBehavior.Flee:
                    var away=center-birdPosition;away.y=0;
                    // The rare moth dodges while approached, then tires inside the
                    // catch envelope. Keeping both retreat and wobble continuous at
                    // close range gives normal-speed flight a fair intercept instead
                    // of teleporting the target permanently beyond the catch radius.
                    float distance=away.magnitude;
                    float closeScale=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(1.5f,7f,distance));
                    float retreat=Mathf.Min(3f,Mathf.Max(0f,distance-1.5f)*.35f);
                    return center+(away.sqrMagnitude>.01f?away.normalized:Vector3.right)*retreat
                        +new Vector3(Mathf.Sin(phase*.9f)*2f,Mathf.Sin(phase*1.4f)*1.8f,
                            Mathf.Cos(phase*.9f)*2f)*closeScale;
                default:
                    return center+new Vector3(Mathf.Sin(time*.55f+index)*2,Mathf.Sin(time*.8f+index*2)*1.2f,Mathf.Cos(time*.45f+index)*2);
            }
        }
        private void TintInstance(int index,Color[] baseColors,CollectibleDefinition definition)
        {
            if(source==null)return;
            Color tint=definition.Type==CollectibleType.MoonMoth?new Color(.45f,.86f,1f):definition.Type==CollectibleType.EmberMoth
                ?new Color(1f,.34f,.12f):definition.Type==CollectibleType.CrownMoth?new Color(1f,.35f,.88f):new Color(1f,.82f,.28f);
            for(int v=0;v<source.Length;v++)
            {
                Color original=baseColors!=null && v<baseColors.Length?baseColors[v]:Color.white;
                colors[index*source.Length+v]=new Color(original.r*tint.r,original.g*tint.g,original.b*tint.b,original.a);
            }
        }
        private static float RarityScale(CollectibleRarity rarity)=>rarity==CollectibleRarity.Epic?1.45f:rarity==CollectibleRarity.Rare?1.25f:rarity==CollectibleRarity.Uncommon?1.1f:1f;
        private static float RespawnSeconds(CollectibleRarity rarity)=>rarity==CollectibleRarity.Epic?48f:rarity==CollectibleRarity.Rare?36f:rarity==CollectibleRarity.Uncommon?30f:22f;
        private void UpdateCounter()
        {
            // GameplayRibbon owns the persistent catch tally and keeps it out of the
            // objective/instrument rectangles. Retain legacy cleanup for old instances.
            if(counter!=null)counter.gameObject.SetActive(false);
            return;
#pragma warning disable CS0162
            bool visible=bird!=null && bird.ShowFlightText;
            if(counter!=null)counter.gameObject.SetActive(visible);
            if(!visible)return;
            if(counter==null && Camera.main!=null)
            {var go=new GameObject("Foraging score");go.transform.SetParent(Camera.main.transform,false);go.transform.localPosition=new Vector3(.72f,-.4f,2);counter=go.AddComponent<TextMesh>();counter.anchor=TextAnchor.MiddleRight;counter.fontSize=40;counter.characterSize=.009f;counter.color=new Color(1,.82f,.36f);go.AddComponent<VoarVR.UI.FlightCard>();}
            if(counter==null)return;
            counter.text=Time.unscaledTime<noticeUntil?"+"+reward+"  TASTY!  x"+Score.Combo+"\n"+Score.Points+" POINTS":Score.Points>0?Score.Points+" POINTS  ·  BEST "+best:"SUN MOTHS\nFly through to eat +10";
#pragma warning restore CS0162
        }
        private void OnApplicationPause(bool paused){if(paused){InvalidateSweep();SaveBest();}}
        private void OnApplicationFocus(bool focused){if(!focused){InvalidateSweep();SaveBest();}}
        private void OnDisable(){InvalidateSweep();SaveBest();}
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(counter!=null)Destroy(counter.gameObject);}
    }
}
