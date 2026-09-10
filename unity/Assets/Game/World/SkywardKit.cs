using System;
using UnityEngine;
namespace VoarVR.World
{
    public sealed class SkywardKit:ScriptableObject
    {
        [Serializable] public struct Piece { public string Name;public Mesh Mesh; }
        public Piece[] Pieces;
        public Material Material;
        public sealed class CachedMesh {public Vector3[] Vertices;public int[] Triangles;public Color[] Colors;}
        private readonly System.Collections.Generic.Dictionary<string,CachedMesh> cache=new System.Collections.Generic.Dictionary<string,CachedMesh>();
        public CachedMesh Data(string name){if(cache.TryGetValue(name,out var value))return value;var m=Find(name);value=new CachedMesh{Vertices=m.vertices,Triangles=m.triangles,Colors=m.colors};cache[name]=value;return value;}
        public Mesh Find(string name){foreach(var p in Pieces)if(p.Name==name)return p.Mesh;return null;}
    }
}
