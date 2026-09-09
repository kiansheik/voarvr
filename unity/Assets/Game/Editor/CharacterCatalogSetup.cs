using System;
using UnityEditor;
using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.Editor
{
    // Wires every BirdCharacterDefinition's RigModel from its ModelAssetPath and normalizes the
    // FBX import settings, mirroring the importer overrides DuckSetup applies inline. Run once
    // after adding or re-exporting a species model; safe to re-run any time.
    public static class CharacterCatalogSetup
    {
        [MenuItem("VoarVR/Configure Characters")]
        public static void Configure()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(BirdCharacterDefinition));
            if (guids.Length == 0) throw new InvalidOperationException("No BirdCharacterDefinition assets found.");
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var character = AssetDatabase.LoadAssetAtPath<BirdCharacterDefinition>(assetPath);
                if (string.IsNullOrEmpty(character.ModelAssetPath))
                    throw new InvalidOperationException(character.DisplayName + " has no ModelAssetPath.");
                var importer = (ModelImporter)AssetImporter.GetAtPath(character.ModelAssetPath);
                if (importer == null)
                    throw new InvalidOperationException("Missing model at " + character.ModelAssetPath + " for " + character.DisplayName);
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = false;
                importer.optimizeGameObjects = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
                character.RigModel = AssetDatabase.LoadAssetAtPath<GameObject>(character.ModelAssetPath);
                if (character.RigModel == null)
                    throw new InvalidOperationException("Could not load a GameObject from " + character.ModelAssetPath);
                EditorUtility.SetDirty(character);
                Debug.Log("Configured character: " + character.DisplayName);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
