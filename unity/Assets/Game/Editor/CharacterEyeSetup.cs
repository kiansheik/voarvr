using UnityEngine;
using UnityEditor;
using VoarVR.Flight;
namespace VoarVR.Editor
{
    public static class CharacterEyeSetup
    {
        [MenuItem("VoarVR/Configure Character Eye Anchors")]
        public static void Configure()
        {
            // Midpoints of the two authored eyes, converted from Blender Z-up/-Y-forward.
            string[] names={"Duck","Dragon","Magpie"};
            Vector3[] eyes={new Vector3(0,.288f,.32f),new Vector3(0,.74f,1.32f),new Vector3(0,.058f,.099f)};
            for(int i=0;i<names.Length;i++)
            {var bird=AssetDatabase.LoadAssetAtPath<BirdCharacterDefinition>("Assets/Resources/Characters/"+names[i]+".asset");bird.FirstPersonEyeAnchor=eyes[i];EditorUtility.SetDirty(bird);}
            AssetDatabase.SaveAssets();
        }
    }
}
