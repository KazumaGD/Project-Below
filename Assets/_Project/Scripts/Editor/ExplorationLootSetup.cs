using System;
using System.Linq;
using ProjectBelow.Combat;
using ProjectBelow.Inventory;
using ProjectBelow.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ProjectBelow.Editor.ExplorationCombatSetup;
using static ProjectBelow.Editor.ExplorationInventorySetup;

namespace ProjectBelow.Editor
{
    internal static class ExplorationLootSetup
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Loot Crate.prefab";
        private const string Items = "Assets/_Project/Data/Items";
        private const string Art = "Assets/_Project/Art/Items";
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private static readonly Color Panel = new(0.082f, 0.075f, 0.069f, 0.995f);
        private static readonly Color Surface = new(0.129f, 0.116f, 0.102f);
        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);

        internal static void AddToScene(GameModeController mode, PlayerInventory inventory)
        {
            var scene = mode.gameObject.scene;
            var enemy = scene.GetRootGameObjects().Select(root => root.GetComponent<EnemyCombatant>())
                .Single(combatant => combatant != null);
            var food = Load<ItemDefinition>($"{Items}/Food.asset");
            var metal = Load<ItemDefinition>($"{Items}/Metal.asset");
            var materials = Load<ItemDefinition>($"{Items}/Building Materials.asset");
            var prefab = GetCrate();

            var root = new GameObject("Loot Containers").transform;
            AddCrate(prefab, root, mode, "Припасы дозора", new Vector3(-2.1f, 0f, -6.65f), (food, 7), (metal, 5));
            AddCrate(prefab, root, mode, "Ящик мастерской", new Vector3(-4.15f, 0f, 5.24f), (materials, 25), (metal, 18));
            ConfigureEnemy(enemy, prefab, (food, 5), (metal, 9), (materials, 6));
            AddHud(mode, inventory);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"Missing asset at {path}.");

        private static LootContainer GetCrate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
                return existing.GetComponent<LootContainer>();

            var wood = Load<Material>($"{Art}/Food.mat");
            var metal = Load<Material>($"{Art}/Metal.mat");
            var root = new GameObject("Loot Crate", typeof(BoxCollider), typeof(LootContainer));
            try
            {
                var collider = root.GetComponent<BoxCollider>();
                collider.center = Vector3.up * 0.29f;
                collider.size = new Vector3(0.91f, 0.58f, 0.64f);
                AddPiece(root.transform, "Body", new Vector3(0f, 0.235f, 0f), new Vector3(0.84f, 0.43f, 0.58f), wood);
                AddPiece(root.transform, "Lid", new Vector3(0f, 0.49f, 0f), new Vector3(0.91f, 0.15f, 0.64f), wood);
                for (int side = -1; side <= 1; side += 2)
                    AddPiece(root.transform, "Band", new Vector3(side * 0.30f, 0.303f, 0f), new Vector3(0.07f, 0.53f, 0.65f), metal);
                AddPiece(root.transform, "Clasp", new Vector3(0f, 0.43f, -0.33f), new Vector3(0.13f, 0.12f, 0.04f), metal);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return prefab.GetComponent<LootContainer>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddPiece(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static void AddCrate(LootContainer prefab, Transform root, GameModeController mode, string title,
            Vector3 position, params (ItemDefinition Item, int Quantity)[] contents)
        {
            var crate = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, root);
            crate.name = title;
            crate.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, -12f, 0f));
            var settings = new SerializedObject(crate.GetComponent<LootContainer>());
            settings.FindProperty("_gameMode").objectReferenceValue = mode;
            settings.FindProperty("_title").stringValue = title;
            AssignContents(settings.FindProperty("_initialContents"), contents);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureEnemy(EnemyCombatant enemy, LootContainer prefab, params (ItemDefinition Item, int Quantity)[] contents)
        {
            var settings = new SerializedObject(enemy);
            var prefabProperty = settings.FindProperty("_defeatLootPrefab");
            if (prefabProperty.objectReferenceValue == null)
                prefabProperty.objectReferenceValue = prefab;

            var definition = (EnemyDefinition)settings.FindProperty("_definition").objectReferenceValue;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var definitionSettings = new SerializedObject(definition);
            var loot = definitionSettings.FindProperty("_loot");
            if (loot.arraySize == 0)
            {
                AssignContents(loot, contents);
                definitionSettings.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(definition);
            }
        }

        private static void AssignContents(SerializedProperty list, (ItemDefinition Item, int Quantity)[] contents)
        {
            list.arraySize = contents.Length;
            for (int i = 0; i < contents.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_item").objectReferenceValue = contents[i].Item;
                entry.FindPropertyRelative("_quantity").intValue = contents[i].Quantity;
            }
        }

        internal static void AddHud(GameModeController mode, PlayerInventory inventory)
        {
            var host = new GameObject("Loot UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 26;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var window = CreateRect(host.transform, "Window", Vector2.zero, Vector2.zero, Vector2.zero);
            window.anchorMax = Vector2.one;
            SetImage(window, new Color(0.005f, 0.004f, 0.003f, 0.64f));
            var panel = CreateRect(window, "Loot Panel", Center, Vector2.zero, new Vector2(972f, 468f));
            SetImage(panel, Panel);
            var title = CreateText(panel, "Title", string.Empty, new Vector2(-110f, 180f),
                new Vector2(690f, 46f), 30, TextAnchor.MiddleLeft);
            var close = AddButton(panel, "Close", "Закрыть", new Vector2(389f, 180f), new Vector2(130f, 40f));
            SetImage(CreateRect(panel, "Divider", Center, new Vector2(0f, 144f), new Vector2(910f, 1f)), Surface);
            CreateText(panel, "Container Title", "Контейнер", new Vector2(-236f, 119f), new Vector2(438f, 30f),
                19, TextAnchor.MiddleLeft).color = Accent;
            CreateText(panel, "Player Title", "Инвентарь", new Vector2(236f, 119f), new Vector2(438f, 30f),
                19, TextAnchor.MiddleLeft).color = Accent;
            var cell = new Vector2(101f, 74f);
            var viewport = new Vector2(438f, 244f);
            var lootGrid = CreateGrid(panel, "Container Slots", new Vector2(-236f, -22f), viewport, cell);
            var playerGrid = CreateGrid(panel, "Player Slots", new Vector2(236f, -22f), viewport, cell);
            SetImage(CreateRect(panel, "Column Divider", Center, new Vector2(0f, -20f), new Vector2(1f, 244f)), Surface);
            var empty = CreateText(panel, "Empty", "Контейнер пуст", new Vector2(-236f, -146f), new Vector2(438f, 30f),
                16, TextAnchor.MiddleLeft);
            empty.color = Accent;
            var status = CreateText(panel, "Status", string.Empty, new Vector2(175f, -185f),
                new Vector2(560f, 42f), 17, TextAnchor.MiddleLeft);
            status.color = Accent;
            var takeAll = AddButton(panel, "Take All", "Взять всё", new Vector2(-378f, -187f), new Vector2(154f, 42f));
            takeAll.targetGraphic.color = new Color(0.32f, 0.24f, 0.15f);
            var takeStack = AddButton(panel, "Take Stack", "Взять стак", new Vector2(-215f, -187f), new Vector2(148f, 42f));
            var slot = CreateSlotTemplate(host.transform, cell);
            var drag = AddDragOverlay(window);
            window.gameObject.SetActive(false);

            var settings = new SerializedObject(host.AddComponent<LootHud>());
            settings.FindProperty("_gameMode").objectReferenceValue = mode;
            settings.FindProperty("_inventory").objectReferenceValue = inventory;
            settings.FindProperty("_window").objectReferenceValue = window.gameObject;
            settings.FindProperty("_containerGrid").objectReferenceValue = lootGrid;
            settings.FindProperty("_playerGrid").objectReferenceValue = playerGrid;
            settings.FindProperty("_slotTemplate").objectReferenceValue = slot;
            settings.FindProperty("_drag").objectReferenceValue = drag;
            settings.FindProperty("_title").objectReferenceValue = title;
            settings.FindProperty("_empty").objectReferenceValue = empty;
            settings.FindProperty("_status").objectReferenceValue = status;
            settings.FindProperty("_takeAll").objectReferenceValue = takeAll;
            settings.FindProperty("_takeStack").objectReferenceValue = takeStack;
            settings.FindProperty("_close").objectReferenceValue = close;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button AddButton(Transform parent, string name, string caption, Vector2 position, Vector2 size)
        {
            var button = MakeButton(CreateRect(parent, name, Center, position, size));
            CreateText(button.transform, "Label", caption, Vector2.zero, size, 17);
            return button;
        }
    }
}
