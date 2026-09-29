using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.World
{
    // A small bounded flock of aerial rivals. They are physical moving hazards rather than
    // combatants: striking one uses the normal directional collision bump, sound and haptics.
    public sealed class SkyRivals:MonoBehaviour
    {
        private sealed class Rival
        {
            public GameObject Root;
            public Transform LeftUpper,RightUpper;
            public Quaternion LeftRest,RightRest;
            public LogicalPosition Home;
            public Quaternion OrbitHeading;
            public bool Placed;
        }

        public const int Capacity=3;
        private readonly List<Rival> rivals=new List<Rival>(Capacity);
        private BirdFlightDriver bird;
        private WorldSpace space;
        private float seedPhase;

        public int Count=>rivals.Count;

        public void Configure(BirdFlightDriver driver,WorldSpace coordinates)
        {
            bird=driver;space=coordinates;seedPhase=(coordinates?.Seed??7319)*.0137f;
            var characters=Resources.LoadAll<BirdCharacterDefinition>(CharacterSelection.ResourcesFolder);
            var source=characters.FirstOrDefault(c=>c.name.IndexOf("Magpie",StringComparison.OrdinalIgnoreCase)>=0)
                ??characters.FirstOrDefault();
            if(source==null||source.RigModel==null)return;
            for(int i=0;i<Capacity;i++)CreateRival(source,i);
        }

        private void CreateRival(BirdCharacterDefinition source,int index)
        {
            var root=new GameObject("Wind rival "+(index+1)){layer=WorldStreamer.CollisionLayer};
            var model=Instantiate(source.RigModel,root.transform);model.name="Rival bird";
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
            model.transform.localScale=Vector3.one*source.RigPresentationScale*.82f;
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=Enumerable.Repeat(source.BodyMaterial,renderer.sharedMaterials.Length).ToArray();
                var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",index==0?new Color(.3f,.16f,.12f)
                    :index==1?new Color(.16f,.2f,.28f):new Color(.28f,.13f,.25f));renderer.SetPropertyBlock(block);
            }
            var collider=root.AddComponent<SphereCollider>();collider.radius=.65f;
            var surface=root.AddComponent<LandingSurface>();surface.SurfaceId=9800+index;
            surface.SurfaceKind=FlightSurfaceKind.Structure;surface.CanLand=false;
            var bones=model.GetComponentsInChildren<Transform>();
            var left=bones.FirstOrDefault(t=>t.name=="LeftUpper");var right=bones.FirstOrDefault(t=>t.name=="RightUpper");
            rivals.Add(new Rival{Root=root,LeftUpper=left,RightUpper=right,
                LeftRest=left!=null?left.localRotation:Quaternion.identity,RightRest=right!=null?right.localRotation:Quaternion.identity});
        }

        public void Tick(float deltaTime)
        {
            if(bird==null||bird.Controller==null||space==null)return;
            Vector3 player=bird.Controller.State.Position;float time=bird.Controller.SimulationTime;
            Quaternion heading=bird.Heading;
            for(int i=0;i<rivals.Count;i++)
            {
                var rival=rivals[i];Vector3 home=space.ToLocal(rival.Home.X,rival.Home.Y,rival.Home.Z);
                if(!rival.Placed||Vector3.Distance(player,home)>125f)
                {
                    float side=i==0?-1:i==1?1:0;
                    home=player+heading*new Vector3(side*(18+i*6),10+i*5,48+i*22);
                    double terrain=WorldTerrain.Elevation(space.Seed,space.ToLogical(home).X,space.ToLogical(home).Z);
                    home.y=Mathf.Max(home.y,(float)terrain+14f);rival.Home=space.ToLogical(home);
                    rival.OrbitHeading=heading;rival.Placed=true;
                }
                float phase=time*(.55f+i*.09f)+seedPhase+i*2.1f;
                // Cross the flight line, circle back, and occasionally dip toward the player.
                Vector3 offset=rival.OrbitHeading*new Vector3(Mathf.Sin(phase)*14f,Mathf.Sin(phase*1.7f)*4f,
                    Mathf.Cos(phase*.7f)*9f);
                Vector3 position=home+offset;rival.Root.transform.position=position;
                Vector3 tangent=rival.OrbitHeading*new Vector3(Mathf.Cos(phase)*14f*.55f,
                    Mathf.Cos(phase*1.7f)*4f*1.7f,-Mathf.Sin(phase*.7f)*9f*.7f);
                if(tangent.sqrMagnitude>.01f)rival.Root.transform.rotation=Quaternion.LookRotation(tangent.normalized,Vector3.up);
                float flap=Mathf.Sin(time*7.5f+i)*24f;
                if(rival.LeftUpper!=null)rival.LeftUpper.localRotation=rival.LeftRest*Quaternion.Euler(0,0,flap);
                if(rival.RightUpper!=null)rival.RightUpper.localRotation=rival.RightRest*Quaternion.Euler(0,0,-flap);
            }
            // Auto Sync Transforms is intentionally disabled; publish the whole three-rival
            // batch once so the next manual flight sweep matches what the player sees.
            Physics.SyncTransforms();
        }

        public void InvalidatePositions(){foreach(var rival in rivals)rival.Placed=false;}
        private void OnDestroy(){foreach(var rival in rivals)if(rival.Root!=null)Destroy(rival.Root);}
    }
}
