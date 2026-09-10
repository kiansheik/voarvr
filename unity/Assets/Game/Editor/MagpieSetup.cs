using UnityEditor;
using System.Linq;
using UnityEngine;
using VoarVR.Flight;
namespace VoarVR.Editor
{
    public static class MagpieSetup
    {
        [MenuItem("VoarVR/Configure Magpie")]
        public static void Configure()
        {
            const string path="Assets/Resources/Characters/Magpie.asset";
            var bird=AssetDatabase.LoadAssetAtPath<BirdCharacterDefinition>(path);
            if(bird==null) { bird=ScriptableObject.CreateInstance<BirdCharacterDefinition>(); AssetDatabase.CreateAsset(bird,path); }
            bird.DisplayName="Magpie";bird.FlavorText="Light, agile, expressive feathers and a long steering tail.";bird.SortOrder=2;
            bird.ModelAssetPath="Assets/Art/Models/Magpie.fbx";
            bird.Architecture=WingArchitecture.ArticulatedAvian;
            bird.Morphology=new BirdMorphology { Species="Pica pica",Provenance="docs/research/species/magpie.md: mass literature aggregate; span inferred; area measured authored projection; joints inferred; flight context comparative P.hudsonia" };
            bird.Articulation=new AvianArticulationSettings();
            bird.RigPresentationScale=2.5f;bird.RestArmSpan=.4375f;
            bird.Size=.58f;bird.Speed=.55f;bird.Power=.58f;bird.Agility=.85f;bird.Weight=.45f;
            bird.StrokeForwardRatio=.28f;bird.StrokeSpeedLimit=2.6f;bird.GlideDragMultiplier=1.05f;bird.WingPitchSensitivity=.25f;
            bird.BodyMaterial=AssetDatabase.LoadAssetAtPath<BirdCharacterDefinition>("Assets/Resources/Characters/Duck.asset").BodyMaterial;
            EditorUtility.SetDirty(bird);
            CharacterCatalogSetup.Configure();
            bird.RestArmSpan=Mathf.Abs(bird.RigModel.GetComponentsInChildren<Transform>().Single(t=>t.name=="LeftHand").position.x)*bird.RigPresentationScale;
            foreach(string legacy in new[]{"Duck","Dragon"})
            {
                var old=AssetDatabase.LoadAssetAtPath<BirdCharacterDefinition>("Assets/Resources/Characters/"+legacy+".asset");
                old.Morphology=null;old.Architecture=legacy=="Dragon"?WingArchitecture.Membrane:WingArchitecture.LegacyAvian;
                EditorUtility.SetDirty(old);
            }
            EditorUtility.SetDirty(bird);AssetDatabase.SaveAssets();
            Debug.Log("Magpie configured:63 bones,10/9 remiges per wing,12 rectrices; biological and presentation scales separate.");
        }
    }
}
