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
        }
        private readonly IslandSlot[] slots=new IslandSlot[9];
        private readonly Mesh[] combined=new Mesh[3];
        private WorldSpace space;private SkywardKit kit;
        private long lastX=long.MinValue,lastZ;
        public int ActiveIslands=>kit!=null?9:0;
        public void Configure(WorldSpace coordinates)
        {
            space=coordinates;kit=Resources.Load<SkywardKit>("SkywardKit");if(kit==null)return;
            for(int v=0;v<3;v++)combined[v]=BuildIsland(v);
            for(int i=0;i<slots.Length;i++)
            {
                var root=new GameObject("Sky island "+i);root.transform.SetParent(transform,false);root.layer=WorldStreamer.CollisionLayer;
                var near=Visual(root.transform,"Garden and arch",combined[i%3]);var far=Visual(root.transform,"Distant silhouette",kit.Find("Island"+(i%3)));
                var surface=root.AddComponent<LandingSurface>();surface.SurfaceId=100000+i;
                var body=root.AddComponent<MeshCollider>();body.sharedMesh=combined[i%3];
                var garden=root.AddComponent<BoxCollider>();garden.center=new Vector3(0,-.6f,0);garden.size=new Vector3(38,1.2f,30);
                slots[i]=new IslandSlot {Root=root,Near=near,Far=far,NearRenderer=near.GetComponent<MeshRenderer>(),FarRenderer=far.GetComponent<MeshRenderer>(),Colliders=new Collider[]{body,garden}};
            }
            space.Rebased+=Rebase;Tick(Vector3.zero);
        }
        private MeshFilter Visual(Transform parent,string name,Mesh mesh)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=kit.Material;renderer.shadowCastingMode=ShadowCastingMode.Off;return filter;}
        private Mesh BuildIsland(int variant)
        {
            var parts=new List<CombineInstance>();
            void Add(string name,Vector3 p,Vector3 scale,float yaw=0){parts.Add(new CombineInstance{mesh=kit.Find(name),transform=Matrix4x4.TRS(p,Quaternion.Euler(0,yaw,0),scale)});}
            Add("Island"+variant,Vector3.zero,Vector3.one);Add("Arch",new Vector3(0,0,-65),Vector3.one);Add("Ruin",new Vector3(-9,0,22),Vector3.one*1.5f);
            Add("Tower",new Vector3(23,0,16),new Vector3(1.2f,1.8f,1.2f),variant*45);
            for(int i=0;i<9+variant;i++){float a=i*2.399f+variant;float r=30+i%3*7;Add("Tree"+((i+variant)%3),new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r*.7f),Vector3.one*(1.1f+i%4*.16f),i*73); }
            Add("Bridge",new Vector3(0,0,-28),new Vector3(1,1,1),90);
            var mesh=new Mesh{name="Shared sky garden "+variant};mesh.CombineMeshes(parts.ToArray(),true,true);return mesh;
        }
        public void Tick(Vector3 player)
        {
            if(kit==null)return;var logical=space.ToLogical(player);long x=(long)Math.Floor(logical.X/FlightRegions.IslandCell),z=(long)Math.Floor(logical.Z/FlightRegions.IslandCell);
            if(x!=lastX || z!=lastZ)
            {
                for(long ix=x-1;ix<=x+1;ix++)for(long iz=z-1;iz<=z+1;iz++)
                {
                    int index=(int)((ix%3+3)%3)*3+(int)((iz%3+3)%3);var slot=slots[index];
                    uint key=AtmosphereModel.Hash(space.Seed,ix,iz,905);int variant=ix==0 && iz==0?0:(int)(key%3);
                    slot.Logical=FlightRegions.Island(space.Seed,ix,iz);slot.Root.transform.position=space.ToLocal(slot.Logical.X,slot.Logical.Y,slot.Logical.Z);
                    slot.Near.sharedMesh=combined[variant];slot.Far.sharedMesh=kit.Find("Island"+variant);((MeshCollider)slot.Colliders[0]).sharedMesh=combined[variant];
                    slot.Root.GetComponent<LandingSurface>().SurfaceId=unchecked((int)key)|1;
                }
                lastX=x;lastZ=z;
            }
            foreach(var slot in slots)
            {
                float distance=Vector3.Distance(player,slot.Root.transform.position);bool near=distance<520;
                slot.NearRenderer.enabled=near;slot.FarRenderer.enabled=!near;
                foreach(var collider in slot.Colliders)collider.enabled=distance<260;
            }
        }
        private void LateUpdate(){var eye=Camera.main;if(eye!=null)Tick(eye.transform.position);}
        private void Rebase(Vector3 delta){foreach(var slot in slots)if(slot!=null)slot.Root.transform.position-=delta;}
        private void OnDestroy(){if(space!=null)space.Rebased-=Rebase;foreach(var m in combined)if(m!=null)Destroy(m);}
    }
}
