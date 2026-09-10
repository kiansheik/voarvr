using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VoarVR.World;
namespace VoarVR.Editor
{
    public static class SkywardSetup
    {
        [MenuItem("VoarVR/Configure Skyward Kit")]
        public static void Configure()
        {
            const string path="Assets/Resources/SkywardKit.asset";
            var kit=AssetDatabase.LoadAssetAtPath<SkywardKit>(path);
            if(kit==null){kit=ScriptableObject.CreateInstance<SkywardKit>();AssetDatabase.CreateAsset(kit,path);}
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Skyward.mat");
            if(material==null){material=new Material(Shader.Find("VoarVR/SkywardVertex"));AssetDatabase.CreateAsset(material,"Assets/Art/Materials/Skyward.mat");}
            kit.Material=material;
            const string skyPath="Assets/Resources/SkyGardenSky.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(sky==null){sky=new Material(Shader.Find("VoarVR/SkyGardenSky"));AssetDatabase.CreateAsset(sky,skyPath);}
            sky.shader=Shader.Find("VoarVR/SkyGardenSky");sky.SetColor("_SkyTint",Color.white);sky.SetFloat("_Exposure",1);EditorUtility.SetDirty(sky);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/SkywardKit.fbx");
            var pieces=new List<SkywardKit.Piece>();
            foreach(var filter in source.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh=kit.Pieces!=null?kit.Find(filter.name):null;
                if(mesh==null){mesh=new Mesh {name=filter.name};AssetDatabase.AddObjectToAsset(mesh,kit);}
                mesh.Clear();var vertices=filter.sharedMesh.vertices;var normals=filter.sharedMesh.normals;
                var matrix=filter.transform.localToWorldMatrix;
                for(int i=0;i<vertices.Length;i++){vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);normals[i]=matrix.MultiplyVector(normals[i]).normalized;}
                mesh.vertices=vertices;mesh.triangles=filter.sharedMesh.triangles;mesh.normals=normals;mesh.colors=filter.sharedMesh.colors;mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                pieces.Add(new SkywardKit.Piece{Name=filter.name,Mesh=mesh});
            }
            kit.Pieces=pieces.ToArray();EditorUtility.SetDirty(kit);AssetDatabase.SaveAssets();
        }
    }
}
