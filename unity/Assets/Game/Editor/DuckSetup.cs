using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.World;

namespace VoarVR.Editor
{
    public static class DuckSetup
    {
        [MenuItem("VoarVR/Configure Duck Prototype")]
        public static void Configure()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ProjectSetup.Scenes[2]) throw new InvalidOperationException("Open BirdFlight scene first.");
            const string path="Assets/Art/Models/Duck.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Generic;
            importer.importAnimation=false; importer.optimizeGameObjects=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var bird=UnityEngine.Object.FindAnyObjectByType<BirdFlightDriver>();
            if (bird == null) throw new InvalidOperationException("Missing simulation root");
            foreach(var name in new[]{"BirdBody","LeftWing","RightWing"})
            {
                var old=bird.transform.Find(name);
                if(old != null) Undo.DestroyObjectImmediate(old.gameObject);
            }
            var existing=bird.transform.Find("DuckRig");
            var duck=existing != null ? existing.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
            duck.name="DuckRig"; duck.transform.SetParent(bird.transform,false);
            duck.transform.localPosition=Vector3.zero; duck.transform.localRotation=Quaternion.identity; duck.transform.localScale=Vector3.one;
            const string matPath="Assets/Art/Materials/DuckPalette.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(material == null){material=new Material(Shader.Find("VoarVR/DuckVertex"));AssetDatabase.CreateAsset(material,matPath);}
            foreach(var renderer in duck.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                if(renderer is SkinnedMeshRenderer skin)
                {
                    skin.updateWhenOffscreen=true; // Nine bones, also solve when wings leave HMD frustum.
                    skin.localBounds=new Bounds(Vector3.zero,Vector3.one*3f);
                }
            }
            var rig=bird.GetComponent<BirdRigDriver>() ?? Undo.AddComponent<BirdRigDriver>(bird.gameObject);
            var bones=duck.GetComponentsInChildren<Transform>();
            rig.leftUpper=bones.Single(t=>t.name=="LeftUpper");rig.leftForearm=bones.Single(t=>t.name=="LeftForearm");
            rig.leftHand=bones.Single(t=>t.name=="LeftHand");rig.leftTip=bones.Single(t=>t.name=="LeftTip");
            rig.rightUpper=bones.Single(t=>t.name=="RightUpper");rig.rightForearm=bones.Single(t=>t.name=="RightForearm");
            rig.rightHand=bones.Single(t=>t.name=="RightHand");rig.rightTip=bones.Single(t=>t.name=="RightTip");
            rig.face=duck.GetComponentsInChildren<Renderer>().Single(r=>r.name=="DuckFace");
            rig.firstPersonHidden=duck.GetComponentsInChildren<Renderer>()
                .Where(r=>r.name=="DuckFace" || r.name=="DuckNeck").ToArray();
            EditorUtility.SetDirty(rig);
            bird.transform.position=new Vector3(0f,12f,0f);
            var camera=Camera.main;camera.nearClipPlane=.025f;camera.farClipPlane=500f;
            camera.backgroundColor=new Color(.42f,.65f,.76f);camera.clearFlags=CameraClearFlags.SolidColor;
            var ground=GameObject.Find("Ground");ground.transform.position=new Vector3(0f,-.5f,150f);ground.transform.localScale=new Vector3(300f,1f,600f);
            var worldObject=GameObject.Find("ProceduralWorld") ?? new GameObject("ProceduralWorld");
            var wind=worldObject.GetComponent<WindField>() ?? Undo.AddComponent<WindField>(worldObject);
            var world=worldObject.GetComponent<ProceduralFlightWorld>() ?? Undo.AddComponent<ProceduralFlightWorld>(worldObject);
            world.Configure(wind,
                GetMaterial("Assets/Art/Materials/SpiritCity.mat", "VoarVR/PrototypeUnlit", new Color(.18f,.27f,.38f,1f)),
                GetMaterial("Assets/Art/Materials/SpiritForest.mat", "VoarVR/PrototypeUnlit", new Color(.12f,.28f,.21f,1f)),
                GetMaterial("Assets/Art/Materials/SpiritCanopy.mat", "VoarVR/PrototypeUnlit", new Color(.18f,.55f,.38f,1f)),
                GetMaterial("Assets/Art/Materials/SpiritGlow.mat", "VoarVR/PrototypeUnlit", new Color(.35f,.92f,.72f,1f)),
                GetMaterial("Assets/Art/Materials/WindHelpful.mat", "VoarVR/WindRibbon", new Color(.25f,.9f,1f,.62f)),
                GetMaterial("Assets/Art/Materials/WindHazard.mat", "VoarVR/WindRibbon", new Color(1f,.28f,.18f,.68f)));
            EditorUtility.SetDirty(world);
            if(GameObject.Find("FlightReferences") == null)
            {
                var refs=new GameObject("FlightReferences");
                var perchMat=GameObject.Find("Perch_01").GetComponent<Renderer>().sharedMaterial;
                for(int z=0;z<=250;z+=25)
                    for(int side=-1;side<=1;side+=2)
                    {
                        var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.name="DistancePost_"+z+"_"+side;
                        post.transform.SetParent(refs.transform);post.transform.position=new Vector3(side*12f,6f,z);
                        post.transform.localScale=new Vector3(.4f,12f,.4f);post.GetComponent<Renderer>().sharedMaterial=perchMat;
                        Undo.DestroyObjectImmediate(post.GetComponent<Collider>());
                    }
                for(int z=25;z<=200;z+=25)
                {
                    var stripe=GameObject.CreatePrimitive(PrimitiveType.Cube);stripe.name="GroundInterval_"+z;
                    stripe.transform.SetParent(refs.transform);stripe.transform.position=new Vector3(0f,.015f,z);
                    stripe.transform.localScale=new Vector3(24f,.03f,.25f);stripe.GetComponent<Renderer>().sharedMaterial=perchMat;
                    Undo.DestroyObjectImmediate(stripe.GetComponent<Collider>());
                }
            }
            if(UnityEngine.Object.FindAnyObjectByType<Light>()==null)
            {
                var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(40,-30,0);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Duck prototype saved: articulated rig, improved views, procedural city/forest and visible wind.");
        }

        private static Material GetMaterial(string path, string shaderName, Color color)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material == null)
            {
                var shader=Shader.Find(shaderName);
                if(shader == null) throw new InvalidOperationException("Missing shader "+shaderName);
                material=new Material(shader);AssetDatabase.CreateAsset(material,path);
            }
            material.SetColor("_BaseColor",color);EditorUtility.SetDirty(material);return material;
        }
    }
}
