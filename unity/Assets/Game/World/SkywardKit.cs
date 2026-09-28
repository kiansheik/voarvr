using System;
using System.Collections.Generic;
using UnityEngine;
using VoarVR.Flight;
namespace VoarVR.World
{
    public sealed class SkywardKit:ScriptableObject
    {
        [Serializable] public struct Piece { public string Name;public Mesh Mesh; }
        public readonly struct CollisionBox
        {
            public readonly Vector3 Center,Size;
            public readonly Quaternion Rotation;
            public readonly Vector2 TopSize;
            public readonly int SharedMeshKey;
            public readonly int RadialSides;
            public readonly bool UsesAuthoredMesh;
            public bool UsesMesh=>SharedMeshKey>0||UsesAuthoredMesh;
            public CollisionBox(Vector3 center,Vector3 size){Center=center;Size=size;Rotation=Quaternion.identity;TopSize=default;SharedMeshKey=RadialSides=0;UsesAuthoredMesh=false;}
            public CollisionBox(Vector3 center,Vector3 size,Quaternion rotation){Center=center;Size=size;Rotation=rotation;TopSize=default;SharedMeshKey=RadialSides=0;UsesAuthoredMesh=false;}
            private CollisionBox(Vector3 center,Vector3 size,Vector2 topSize,int sharedMeshKey,int radialSides)
            {Center=center;Size=size;Rotation=Quaternion.identity;TopSize=topSize;SharedMeshKey=sharedMeshKey;RadialSides=radialSides;UsesAuthoredMesh=false;}
            private CollisionBox(bool authoredMesh)
            {Center=Size=default;Rotation=Quaternion.identity;TopSize=default;SharedMeshKey=RadialSides=0;UsesAuthoredMesh=authoredMesh;}
            public static CollisionBox TaperedRoof(Vector3 center,Vector3 bottomSize,Vector2 topSize,int sharedMeshKey)
                =>new CollisionBox(center,bottomSize,topSize,sharedMeshKey,0);
            public static CollisionBox TaperedPillar(Vector3 center,float height,int sharedMeshKey)
                =>new CollisionBox(center,new Vector3(3f,height,3f),new Vector2(2.4f,2.4f),sharedMeshKey,6);
            public static CollisionBox AuthoredMesh()=>new CollisionBox(true);
        }
        // Authored in the kit's local metre space. Bounded compound primitives replace a
        // landmark-wide AABB, preserving ruin gaps and tapered upper silhouettes
        // and keeping leaves permeable while trunks and representative branches stay solid,
        // without building a unique collision mesh for every streamed prop.
        private static readonly Dictionary<string,CollisionBox[]> CollisionProfiles=new Dictionary<string,CollisionBox[]>
        {
            {"Tower",BuildingProfile(4)},
            {"House",BuildingProfile(2)},
            {"Ruin",BuildingProfile(1,true)},
            {"Rock",new[]{CollisionBox.AuthoredMesh()}},
            {"Spire",new[]{CollisionBox.AuthoredMesh()}},
            {"Log",new[]{CollisionBox.AuthoredMesh()}},
            {"Tree0",TreeProfile(9f)},
            {"Tree1",TreeProfile(12f)},
            {"Tree2",TreeProfile(15f)},
            {"DeadTree",DeadTreeProfile()}
        };
        private static readonly Dictionary<int,Mesh> SharedCollisionMeshes=new Dictionary<int,Mesh>();
        public Piece[] Pieces;
        public Material Material;
        public sealed class CachedMesh {public Vector3[] Vertices;public int[] Triangles;public Color[] Colors;}
        private readonly System.Collections.Generic.Dictionary<string,CachedMesh> cache=new System.Collections.Generic.Dictionary<string,CachedMesh>();
        public CachedMesh Data(string name){if(cache.TryGetValue(name,out var value))return value;var m=Find(name);value=new CachedMesh{Vertices=m.vertices,Triangles=m.triangles,Colors=m.colors};cache[name]=value;return value;}
        public Mesh Find(string name){foreach(var p in Pieces)if(p.Name==name)return p.Mesh;return null;}
        public static IReadOnlyList<CollisionBox> CollisionProfile(string name)
            => CollisionProfiles.TryGetValue(name,out var boxes)?boxes:Array.Empty<CollisionBox>();
        public static FlightSurfaceKind SurfaceKind(string name)
            => name.StartsWith("Tree",StringComparison.Ordinal)||name=="DeadTree"||name=="Log"
                ?FlightSurfaceKind.Wood:name=="Rock"||name=="Spire"?FlightSurfaceKind.Stone:FlightSurfaceKind.Structure;

        public static Mesh CollisionMesh(CollisionBox shape,Mesh authoredMesh)
        {
            if(shape.UsesAuthoredMesh)return authoredMesh;
            if(shape.SharedMeshKey<=0)return null;
            if(SharedCollisionMeshes.TryGetValue(shape.SharedMeshKey,out var cached)&&cached!=null)return cached;
            if(shape.RadialSides>0)return RadialCollisionMesh(shape);
            float bx=shape.Size.x*.5f,by=shape.Size.y*.5f,bz=shape.Size.z*.5f;
            float tx=shape.TopSize.x*.5f,tz=shape.TopSize.y*.5f;
            var mesh=new Mesh{name="Shared tapered roof collision "+shape.SharedMeshKey,
                hideFlags=HideFlags.HideAndDontSave};
            mesh.vertices=new[]{
                new Vector3(-bx,-by,-bz),new Vector3(bx,-by,-bz),new Vector3(bx,-by,bz),new Vector3(-bx,-by,bz),
                new Vector3(-tx,by,-tz),new Vector3(tx,by,-tz),new Vector3(tx,by,tz),new Vector3(-tx,by,tz)};
            mesh.triangles=new[]{
                0,1,2,0,2,3,4,6,5,4,7,6,
                0,4,5,0,5,1,1,5,6,1,6,2,2,6,7,2,7,3,3,7,4,3,4,0};
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            SharedCollisionMeshes[shape.SharedMeshKey]=mesh;
            return mesh;
        }

        private static Mesh RadialCollisionMesh(CollisionBox shape)
        {
            int sides=shape.RadialSides;float bottom=shape.Size.x*.5f,top=shape.TopSize.x*.5f,y=shape.Size.y*.5f;
            var vertices=new Vector3[sides*2];
            for(int i=0;i<sides;i++)
            {
                float angle=i*Mathf.PI*2f/sides,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
                vertices[i]=new Vector3(c*bottom,-y,s*bottom);
                vertices[i+sides]=new Vector3(c*top,y,s*top);
            }
            var triangles=new List<int>(sides*12);
            for(int i=1;i<sides-1;i++){triangles.Add(0);triangles.Add(i);triangles.Add(i+1);triangles.Add(sides);triangles.Add(sides+i+1);triangles.Add(sides+i);}
            for(int i=0;i<sides;i++)
            {
                int next=(i+1)%sides;
                triangles.Add(i);triangles.Add(i+sides);triangles.Add(next+sides);
                triangles.Add(i);triangles.Add(next+sides);triangles.Add(next);
            }
            var mesh=new Mesh{name="Shared tapered pillar collision "+shape.SharedMeshKey,
                hideFlags=HideFlags.HideAndDontSave};
            mesh.vertices=vertices;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
            SharedCollisionMeshes[shape.SharedMeshKey]=mesh;
            return mesh;
        }

        private static CollisionBox[] BuildingProfile(int levels,bool ruin=false)
        {
            // create_skyward_kit.py authors each level as an exact four-metre body
            // followed by a 1.7m truncated-pyramid roof. One shared eight-vertex
            // convex hull preserves every pitched face and its real horizontal cap.
            var result=new List<CollisionBox>(levels*2+(ruin?3:0));
            for(int level=0;level<levels;level++)
            {
                float y=level*5f,w=4f-level*.45f;
                result.Add(new CollisionBox(new Vector3(0,y+2f,0),new Vector3(w*2f,4f,w*1.5f)));
                RoofHalfExtents(w,0f,out float lowerX,out float lowerZ);
                RoofHalfExtents(w,1f,out float upperX,out float upperZ);
                result.Add(CollisionBox.TaperedRoof(new Vector3(0,y+4.85f,0),
                    new Vector3(lowerX*2f,1.7f,lowerZ*2f),new Vector2(upperX*2f,upperZ*2f),level+1));
            }
            if(ruin)
            {
                // FBX conversion mirrors authored X: the 7m pillar is at +8;
                // both the -8 and rear -Z pillars are the authored 11m variants.
                result.Add(CollisionBox.TaperedPillar(new Vector3(-8,5.5f,0),11f,102));
                result.Add(CollisionBox.TaperedPillar(new Vector3(8,3.5f,0),7f,101));
                result.Add(CollisionBox.TaperedPillar(new Vector3(0,5.5f,-8),11f,102));
            }
            return result.ToArray();
        }

        private static void RoofHalfExtents(float width,float heightFraction,out float x,out float z)
        {
            const float diagonal=.70710678f;
            x=Mathf.Lerp((width+1.6f)*diagonal,width*.7f*diagonal,heightFraction);
            z=Mathf.Lerp((width*.75f+1.6f)*diagonal,width*.55f*diagonal,heightFraction);
        }

        private static CollisionBox[] TreeProfile(float height)
        {
            var result=new CollisionBox[6];
            result[0]=new CollisionBox(new Vector3(.4f,height*.5f,-.15f),new Vector3(1.05f,height,1.05f));
            for(int i=0;i<5;i++)
            {
                float angle=i*2.4f;
                var start=new Vector3(.4f,height*.5f+i*.8f,0);
                var end=new Vector3(Mathf.Cos(angle)*3.8f,start.y+2f,Mathf.Sin(angle)*3f);
                result[i+1]=BranchBox(start,end,.42f);
            }
            return result;
        }

        private static CollisionBox[] DeadTreeProfile()
        {
            var result=new CollisionBox[5];
            result[0]=BranchBox(Vector3.zero,new Vector3(1,10,0),1.05f);
            for(int i=0;i<4;i++)
            {
                var start=new Vector3(.5f,4f+i,0);
                var end=new Vector3(Mathf.Sin(i)*4f,7f+i,Mathf.Cos(i)*3f);
                result[i+1]=BranchBox(start,end,.42f);
            }
            return result;
        }

        private static CollisionBox BranchBox(Vector3 start,Vector3 end,float thickness)
        {
            var direction=end-start;
            return new CollisionBox((start+end)*.5f,new Vector3(thickness,thickness,direction.magnitude),
                Quaternion.FromToRotation(Vector3.forward,direction.normalized));
        }
    }
}
