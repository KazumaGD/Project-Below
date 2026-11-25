using System;
using System.Linq;
using ProjectBelow.Combat;
using ProjectBelow.Core;
using ProjectBelow.Exploration;
using ProjectBelow.Inventory;
using ProjectBelow.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ProjectBelow.Editor.ExplorationCombatSetup;
using static ProjectBelow.Editor.ExplorationInventorySetup;

namespace ProjectBelow.Editor
{
    internal static class ExpeditionSetup
    {
        private const string SessionPath = "Assets/_Project/Prefabs/Expedition Session.prefab";
        internal const string FoodPath = "Assets/_Project/Data/Items/Food.asset";
        private static readonly Vector2 Center = Vector2.one * 0.5f;

        internal static ExpeditionSession GetSessionPrefab(int maxHealth = 20)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SessionPath);
            if (prefab != null)
                return prefab.GetComponent<ExpeditionSession>();

            var resourcePaths = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Project/Data/Items" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            var root = new GameObject("Expedition Session");
            try
            {
                var settings = new SerializedObject(root.AddComponent<ExpeditionSession>());
                settings.FindProperty("_maxHealth").intValue = maxHealth;
                settings.FindProperty("_food").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(FoodPath);
                var resources = settings.FindProperty("_resources");
                resources.arraySize = resourcePaths.Length;
                for (int i = 0; i < resourcePaths.Length; i++)
                    resources.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemDefinition>(resourcePaths[i]);
                settings.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root, SessionPath).GetComponent<ExpeditionSession>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        internal static void AddToDungeon(GameModeController mode)
        {
            if (mode.GetComponent<ExpeditionFlow>() != null)
                return;

            var scene = mode.gameObject.scene;
            var roots = scene.GetRootGameObjects();
            var exit = roots.Single(root => root.name == "Graybox Geometry").transform.Find("Exit Marker");
            var combatHud = roots.Select(root => root.GetComponent<CombatHud>()).Single(hud => hud != null);
            var combatSettings = new SerializedObject(combatHud);
            var defeatButton = (Button)combatSettings.FindProperty("_returnToHub").objectReferenceValue;
            var label = defeatButton != null ? defeatButton.GetComponentInChildren<Text>(true) : null;
            if (exit == null || label == null)
                throw new InvalidOperationException("The graybox exit or defeat button is missing.");

            int maxHealth = new SerializedObject(mode.GetComponent<CombatController>()).FindProperty("_playerMaxHealth").intValue;
            var expedition = mode.gameObject.AddComponent<ExpeditionFlow>();
            var expeditionSettings = new SerializedObject(expedition);
            expeditionSettings.FindProperty("_sessionPrefab").objectReferenceValue = GetSessionPrefab(maxHealth);
            expeditionSettings.ApplyModifiedPropertiesWithoutUndo();

            var exitSettings = new SerializedObject(exit.gameObject.AddComponent<DungeonExit>());
            exitSettings.FindProperty("_gameMode").objectReferenceValue = mode;
            exitSettings.ApplyModifiedPropertiesWithoutUndo();
            var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plaque.name = "Return Plaque";
            plaque.transform.SetParent(exit, false);
            plaque.transform.localPosition = new Vector3(-0.17f, 1.35f, -0.65f);
            plaque.transform.localScale = new Vector3(0.08f, 0.19f, 0.235f);
            plaque.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Dungeon Exit Lamp Lens.mat");

            defeatButton.name = "Return to Hub";
            label.text = "В убежище";
            AddReturnPrompt(mode, expedition);
        }

        private static void AddReturnPrompt(GameModeController mode, ExpeditionFlow expedition)
        {
            var canvas = CreateCanvas("Exit Confirmation", 29);
            var window = CreateRect(canvas.transform, "Window", Vector2.zero, Vector2.zero, Vector2.zero);
            window.anchorMax = Vector2.one;
            SetImage(window, new Color(0.01f, 0.008f, 0.007f, 0.65f));
            var panel = CreateRect(window, "Dialog", Center, Vector2.zero, new Vector2(546f, 236f));
            SetImage(panel, new Color(0.081f, 0.071f, 0.061f, 0.99f));
            CreateText(panel, "Title", "Вернуться в убежище?", new Vector2(0f, 61f), new Vector2(464f, 45f), 28);
            CreateText(panel, "Details", "Ресурсы из рюкзака будут доставлены на склад.", new Vector2(0f, 20f),
                new Vector2(477f, 34f), 17);
            var status = CreateText(panel, "Status", string.Empty, new Vector2(0f, -17f), new Vector2(454f, 25f), 16);
            var cancel = AddButton(panel, "Cancel", "Остаться", new Vector2(-112f, -65f), new Vector2(200f, 44f));
            var confirm = AddButton(panel, "Confirm", "Вернуться", new Vector2(112f, -65f), new Vector2(200f, 44f));
            confirm.targetGraphic.color = new Color(0.33f, 0.24f, 0.14f);
            var settings = new SerializedObject(canvas.gameObject.AddComponent<ReturnPromptHud>());
            settings.FindProperty("_mode").objectReferenceValue = mode;
            settings.FindProperty("_expedition").objectReferenceValue = expedition;
            settings.FindProperty("_window").objectReferenceValue = window.gameObject;
            settings.FindProperty("_confirm").objectReferenceValue = confirm;
            settings.FindProperty("_cancel").objectReferenceValue = cancel;
            settings.FindProperty("_status").objectReferenceValue = status;
            settings.ApplyModifiedPropertiesWithoutUndo();
            window.gameObject.SetActive(false);
        }

        internal static Canvas CreateCanvas(string name, int sortOrder)
        {
            var host = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        internal static Button AddButton(Transform parent, string name, string caption, Vector2 position, Vector2 size)
        {
            var button = MakeButton(CreateRect(parent, name, Center, position, size));
            CreateText(button.transform, "Label", caption, Vector2.zero, size, 18);
            return button;
        }
    }
}
