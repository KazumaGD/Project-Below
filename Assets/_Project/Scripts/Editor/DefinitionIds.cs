using System;
using ProjectBelow.Hub;
using ProjectBelow.Inventory;
using UnityEditor;
using UnityEngine;

namespace ProjectBelow.Editor
{
    internal sealed class DefinitionIds : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
                UpdateId(path);
            foreach (string path in movedAssets)
                UpdateId(path);
        }

        private static void UpdateId(string path)
        {
            if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                return;

            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == typeof(ItemDefinition) || type == typeof(CharacterDefinition) || type == typeof(TaskDefinition)
                || type == typeof(FacilityDefinition))
                Assign((ScriptableObject)AssetDatabase.LoadMainAssetAtPath(path), path);
        }

        internal static void Assign(ScriptableObject definition, string path)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (definition == null || string.IsNullOrEmpty(guid))
                return;

            var settings = new SerializedObject(definition);
            var id = settings.FindProperty("_id");
            if (id.stringValue == guid)
                return;
            id.stringValue = guid;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(definition);
        }
    }
}
