using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace VoarVR.World
{
    // Nine horizontal region slots, shared meshes and near-only collision. No 3D chunk grid.
    public sealed class SkyArchipelago:MonoBehaviour
    {
        private sealed class IslandSlot
        {
            public GameObject Root;public MeshFilter Near,Far;public MeshRenderer NearRenderer,FarRenderer;
            public Collider[] Colliders;public LogicalPosition Logical;
            public bool ShowingNear=true;
        }
        private readonly IslandSlot[] slots=new IslandSlot[9];
        // The first template is the authored chapter destination. Other region slots
        // share three ordinary variants; neither template count nor distance grows in play.
        private readonly Mesh[] combined=new Mesh[4],silhouettes=new Mesh[4];
        public static readonly Vector3 SanctuaryPlantOffset=new Vector3(-23,0,10);
        public static readonly Vector3 SanctuaryPlantScale=new Vector3(1.7f,1.9f,1.7f);
        public static readonly Quaternion SanctuaryPlantRotation=Quaternion.Euler(0,25,0);
        // SkywardSetup's baked FBX mesh mirrors authored X: the actual trunk cap is
        // centered at (-1,10,0). Embed the crown 7 cm into that cap, before scaling.
        public static Vector3 SanctuaryCrownOffset=>SanctuaryPlantOffset+SanctuaryPlantRotation*Vector3.Scale(new Vector3(-1,9.93f,0),SanctuaryPlantScale);
        public const float NearEnterDistance=480,FarEnterDistance=560;
        private WorldSpace space;private SkywardKit kit;
        private long lastX=long.MinValue,lastZ;
        public int ActiveIslands=>kit!=null?9:0;
        public void Configure(WorldSpace coordinates)
        {
            space=coordinates;kit=Resources.Load<SkywardKit>("SkywardKit");if(kit==null)return;
            for(int v=0;v<combined.Length;v++){combined[v]=BuildIsland(v,false);silhouettes[v]=BuildIsland(v,true);}
            for(int i=0;i<slots.Length;i++)
            {
                var root=new GameObject("Sky island "+i);root.transform.SetParent(transform,false);root.layer=WorldStreamer.CollisionLayer;
                var near=Visual(root.transform,"Garden and arch",combined[i%4]);var far=Visual(root.transform,"Distant silhouette",silhouettes[i%4]);
                var surface=root.AddComponent<LandingSurface>();surface.SurfaceId=100000+i;
                var body=root.AddComponent<MeshCollider>();body.sharedMesh=combined[i%4];
                var garden=root.AddComponent<BoxCollider>();garden.center=new Vector3(0,-.6f,0);garden.size=new Vector3(38,1.2f,30);
                slots[i]=new IslandSlot {Root=root,Near=near,Far=far,NearRenderer=near.GetComponent<MeshRenderer>(),FarRenderer=far.GetComponent<MeshRenderer>(),Colliders=new Collider[]{body,garden}};
            }
            space.Rebased+=Rebase;Tick(Vector3.zero);
        }
        private MeshFilter Visual(Transform parent,string name,Mesh mesh)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=kit.Material;renderer.shadowCastingMode=ShadowCastingMode.Off;return filter;}
        private Mesh BuildIsland(int template,bool distant)
        {
            bool sanctuary=template==0;int variant=sanctuary?0:template-1;
            var parts=new List<CombineInstance>();
            void Add(string name,Vector3 p,Vector3 scale,float yaw=0){parts.Add(new CombineInstance{mesh=kit.Find(name),transform=Matrix4x4.TRS(p,Quaternion.Euler(0,yaw,0),scale)});}
            // The arch opening and hero tower retain their original collision positions.
            // Keep both in the far mesh so a destination never vanishes at the LOD boundary.
            Add("Island"+variant,Vector3.zero,Vector3.one);Add("Arch",new Vector3(0,0,-65),Vector3.one);
            Add("Tower",new Vector3(23,0,16),new Vector3(1.2f,1.8f,1.2f),variant*45);
            if(sanctuary)Add("DeadTree",SanctuaryPlantOffset,SanctuaryPlantScale,SanctuaryPlantRotation.eulerAngles.y);
            if(!distant)
            {
                Add("Ruin",new Vector3(-9,0,22),Vector3.one*(sanctuary?.9f:1.5f));
                if(sanctuary)
                {
                    // A low, open center lets the approach arch frame one pale tower and
                    // the restoring crown, instead of a uniform wall of repeated trees.
                    Add("Tree2",new Vector3(-39,0,-10),Vector3.one*1.05f,70);
                    Add("Tree1",new Vector3(-40,0,25),Vector3.one*.85f,190);
                    Add("Tree2",new Vector3(40,0,4),Vector3.one*.85f,30);
                    Add("Tree0",new Vector3(12,0,36),Vector3.one*.85f,150);
                    Add("Tree0",new Vector3(-15,0,36),Vector3.one*.9f,250);
                }
                else for(int i=0;i<8+variant;i++){float a=i*2.399f+variant;float r=30+i%3*7;Add("Tree"+((i+variant)%3),new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r*.7f),Vector3.one*(1.1f+i%4*.16f),i*73);}
                Add("Bridge",new Vector3(0,0,-28),Vector3.one,90);
            }
            var mesh=new Mesh{name=(distant?"Landmark silhouette ":"Shared sky garden ")+template};mesh.CombineMeshes(parts.ToArray(),true,true);return mesh;
        }
        public static bool UseNearMesh(float distance,bool currentlyNear)=>distance<(currentlyNear?FarEnterDistance:NearEnterDistance);
        public void Tick(Vector3 player)
        {
            if(kit==null)return;var logical=space.ToLogical(player);long x=(long)Math.Floor(logical.X/FlightRegions.IslandCell),z=(long)Math.Floor(logical.Z/FlightRegions.IslandCell);
            if(x!=lastX || z!=lastZ)
            {
                for(long ix=x-1;ix<=x+1;ix++)for(long iz=z-1;iz<=z+1;iz++)
                {
                    int index=(int)((ix%3+3)%3)*3+(int)((iz%3+3)%3);var slot=slots[index];
                    uint key=AtmosphereModel.Hash(space.Seed,ix,iz,905);int variant=ix==0 && iz==0?0:1+(int)(key%3);
                    slot.Logical=FlightRegions.Island(space.Seed,ix,iz);slot.Root.transform.position=space.ToLocal(slot.Logical.X,slot.Logical.Y,slot.Logical.Z);
                    slot.Near.sharedMesh=combined[variant];slot.Far.sharedMesh=silhouettes[variant];((MeshCollider)slot.Colliders[0]).sharedMesh=combined[variant];
                    slot.Root.GetComponent<LandingSurface>().SurfaceId=unchecked((int)key)|1;
                }
                lastX=x;lastZ=z;
            }
            foreach(var slot in slots)
            {
                float distance=Vector3.Distance(player,slot.Root.transform.position);bool near=UseNearMesh(distance,slot.ShowingNear);slot.ShowingNear=near;
                slot.NearRenderer.enabled=near;slot.FarRenderer.enabled=!near;
                foreach(var collider in slot.Colliders)collider.enabled=distance<260;
            }
        }
        private void LateUpdate(){var eye=Camera.main;if(eye!=null)Tick(eye.transform.position);}
        private void Rebase(Vector3 delta){foreach(var slot in slots)if(slot!=null)slot.Root.transform.position-=delta;}
        private void OnDestroy(){if(space!=null)space.Rebased-=Rebase;foreach(var m in combined)if(m!=null)Destroy(m);foreach(var m in silhouettes)if(m!=null)Destroy(m);}
    }
}
