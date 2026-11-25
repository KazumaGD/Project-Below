using System;
using ProjectBelow.Combat;
using ProjectBelow.Inventory;
using ProjectBelow.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ProjectBelow.Editor.ExplorationCombatSetup;

namespace ProjectBelow.Editor
{
    internal static class ExplorationInventorySetup
    {
        private const string ItemFolder = "Assets/_Project/Data/Items";
        private const string ArtFolder = "Assets/_Project/Art/Items";
        private const string DropPrefab = "Assets/_Project/Prefabs/Dropped Item.prefab";
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private static readonly Color Panel = new(0.082f, 0.075f, 0.069f, 0.995f);
        private static readonly Color Surface = new(0.129f, 0.116f, 0.102f, 1f);
        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);

        internal static void AddToScene(GameModeController mode, PlayerInventory inventory)
        {
            if (!AssetDatabase.IsValidFolder(ItemFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Items");

            var food = PrepareItem("Food", "Сухой паёк для вылазок в подземелье.", ItemCategory.Food, 10);
            var metal = PrepareItem("Metal", "Металлические заготовки, найденные в подземелье.", ItemCategory.Material, 12);
            var materials = PrepareItem("Building Materials", "Каменные блоки и обломки, пригодные для строительства.", ItemCategory.Material, 16);
            var foodMaterial = PrepareMaterial("Food", new Color(0.48f, 0.24f, 0.10f));
            var metalMaterial = PrepareMaterial("Metal", new Color(0.37f, 0.53f, 0.58f));
            var buildingMaterial = PrepareMaterial("Building Materials", new Color(0.64f, 0.49f, 0.28f));

            mode.gameObject.AddComponent<InventoryToggle>();
            AddHud(mode, inventory);
            Transform root = new GameObject("Resource Pickups").transform;
            AddPickup(root, "Food Ration", food, foodMaterial, inventory, 7, new Vector3(-1.58f, 0.24f, -5.4f), PrimitiveType.Cube);
            AddPickup(root, "Food Reserve", food, foodMaterial, inventory, 8, new Vector3(1.04f, 0.24f, -3.55f), PrimitiveType.Cube);
            AddPickup(root, "Metal Bundle", metal, metalMaterial, inventory, 22, new Vector3(-0.95f, 0.24f, -1.05f), PrimitiveType.Cylinder);
            AddPickup(root, "Building Materials Bundle", materials, buildingMaterial, inventory, 25, new Vector3(0.95f, 0.24f, 1.85f), PrimitiveType.Cube);
            AddPickup(root, "Building Materials Cache", materials, buildingMaterial, inventory, 120, new Vector3(-3.0f, 0.32f, 6.6f), PrimitiveType.Cube);
        }

        private static ItemDefinition PrepareItem(string name, string description, ItemCategory category, int maxStackSize)
        {
            string path = $"{ItemFolder}/{name}.asset";
            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null)
                return item;

            string iconPath = $"{ArtFolder}/{name}.png";
            var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon == null)
            {
                var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                if (importer == null)
                    throw new InvalidOperationException($"Missing item icon {iconPath}.");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            }

            item = ScriptableObject.CreateInstance<ItemDefinition>();
            var settings = new SerializedObject(item);
            settings.FindProperty("_displayName").stringValue = name;
            settings.FindProperty("_description").stringValue = description;
            settings.FindProperty("_icon").objectReferenceValue = icon;
            settings.FindProperty("_category").enumValueIndex = (int)category;
            settings.FindProperty("_maxStackSize").intValue = maxStackSize;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(item, path);
            DefinitionIds.Assign(item, path);
            return item;
        }

        private static Material PrepareMaterial(string name, Color color)
        {
            string path = $"{ArtFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", color * 0.17f);
            material.SetFloat("_Smoothness", 0.28f);
            material.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void AddPickup(Transform parent, string name, ItemDefinition item, Material material,
            PlayerInventory inventory, int quantity, Vector3 position, PrimitiveType primitive)
        {
            var pickupObject = GameObject.CreatePrimitive(primitive);
            pickupObject.name = name;
            var transform = pickupObject.transform;
            transform.SetParent(parent, false);
            transform.localPosition = position;
            transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
            transform.localScale = new Vector3(0.52f, primitive == PrimitiveType.Cylinder ? 0.23f : 0.42f, 0.45f);
            pickupObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            var settings = new SerializedObject(pickupObject.AddComponent<ItemPickup>());
            settings.FindProperty("_item").objectReferenceValue = item;
            settings.FindProperty("_quantity").intValue = quantity;
            settings.FindProperty("_inventory").objectReferenceValue = inventory;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddHud(GameModeController mode, PlayerInventory inventory)
        {
            var host = new GameObject("Inventory UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var window = CreateRect(canvas.transform, "Window", Vector2.zero, Vector2.zero, Vector2.zero);
            window.anchorMax = Vector2.one;
            SetImage(window, new Color(0.005f, 0.004f, 0.003f, 0.64f));
            var panel = CreateRect(window, "Inventory Panel", Center, Vector2.zero, new Vector2(882f, 466f));
            SetImage(panel, Panel);
            CreateText(panel, "Title", "Инвентарь", new Vector2(-243f, 181f), new Vector2(328f, 52f),
                37, TextAnchor.MiddleLeft);
            var usage = CreateText(panel, "Usage", string.Empty, new Vector2(181f, 177f), new Vector2(188f, 32f),
                19, TextAnchor.MiddleRight);
            usage.color = Accent;
            var close = MakeButton(CreateRect(panel, "Close", Center, new Vector2(350f, 181f), new Vector2(125f, 39f)));
            CreateText(close.transform, "Label", "Закрыть", Vector2.zero, new Vector2(118f, 35f), 16);
            SetImage(CreateRect(panel, "Divider", Center, new Vector2(0f, 144f), new Vector2(818f, 1f)), Surface);

            var cell = new Vector2(134f, 98f);
            ScrollRect grid = CreateGrid(panel, "Slots", new Vector2(-126f, -47f), new Vector2(566f, 320f), cell);
            var details = CreateRect(panel, "Details", Center, new Vector2(299f, -47f), new Vector2(218f, 320f));
            SetImage(details, Surface);
            var itemCategory = CreateText(details, "Category", string.Empty, new Vector2(0f, 123f),
                new Vector2(174f, 24f), 15, TextAnchor.MiddleLeft);
            itemCategory.color = Accent;
            var itemName = CreateText(details, "Item Name", string.Empty, new Vector2(0f, 75f),
                new Vector2(174f, 62f), 24, TextAnchor.UpperLeft);
            var description = CreateText(details, "Description", string.Empty, new Vector2(0f, -30f),
                new Vector2(174f, 144f), 17, TextAnchor.UpperLeft);
            var template = CreateSlotTemplate(canvas.transform, cell);

            var notice = CreateRect(canvas.transform, "Pickup Notice", new Vector2(0.5f, 1f),
                new Vector2(0f, -46f), new Vector2(392f, 43f));
            SetImage(notice, Panel);
            var noticeText = CreateText(notice, "Notice Text", string.Empty, Vector2.zero, new Vector2(366f, 35f), 19);
            noticeText.color = Accent;
            notice.gameObject.SetActive(false);
            window.gameObject.SetActive(false);
            var hud = host.AddComponent<InventoryHud>();
            var settings = new SerializedObject(hud);
            settings.FindProperty("_gameMode").objectReferenceValue = mode;
            settings.FindProperty("_inventory").objectReferenceValue = inventory;
            settings.FindProperty("_window").objectReferenceValue = window.gameObject;
            settings.FindProperty("_grid").objectReferenceValue = grid;
            settings.FindProperty("_slotTemplate").objectReferenceValue = template;
            settings.FindProperty("_close").objectReferenceValue = close;
            settings.FindProperty("_usage").objectReferenceValue = usage;
            settings.FindProperty("_itemName").objectReferenceValue = itemName;
            settings.FindProperty("_itemDescription").objectReferenceValue = description;
            settings.FindProperty("_itemCategory").objectReferenceValue = itemCategory;
            settings.FindProperty("_notice").objectReferenceValue = notice.gameObject;
            settings.FindProperty("_noticeText").objectReferenceValue = noticeText;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AddDropControls(hud, mode);
        }

        internal static void AddDropControls(InventoryHud hud, GameModeController mode)
        {
            var settings = new SerializedObject(hud);
            var window = (GameObject)settings.FindProperty("_window").objectReferenceValue;
            var description = (Text)settings.FindProperty("_itemDescription").objectReferenceValue;
            var details = (RectTransform)description.transform.parent;
            description.rectTransform.anchoredPosition = new Vector2(0f, -7f);
            description.rectTransform.sizeDelta = new Vector2(174f, 100f);
            var dropRect = CreateRect(details, "Drop", Center, new Vector2(0f, -130f), new Vector2(174f, 40f));
            var drop = MakeButton(dropRect);
            drop.targetGraphic.color = new Color(0.30f, 0.18f, 0.14f);
            CreateText(dropRect, "Label", "Выбросить", Vector2.zero, dropRect.sizeDelta, 17);
            var status = CreateText(details, "Drop Status", string.Empty, new Vector2(0f, -82f),
                new Vector2(174f, 43f), 15, TextAnchor.MiddleLeft);
            status.color = Accent;
            var dropper = mode.gameObject.AddComponent<WorldItemDropper>();
            var dropperSettings = new SerializedObject(dropper);
            dropperSettings.FindProperty("_pickupPrefab").objectReferenceValue = GetDroppedItemPrefab();
            dropperSettings.ApplyModifiedPropertiesWithoutUndo();
            settings.FindProperty("_drag").objectReferenceValue = AddDragOverlay((RectTransform)window.transform);
            settings.FindProperty("_dropper").objectReferenceValue = dropper;
            settings.FindProperty("_drop").objectReferenceValue = drop;
            settings.FindProperty("_dropStatus").objectReferenceValue = status;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ItemPickup GetDroppedItemPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DropPrefab);
            if (prefab != null)
                return prefab.GetComponent<ItemPickup>();

            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                root.name = "Dropped Item";
                root.transform.localScale = Vector3.one * 0.34f;
                root.GetComponent<MeshRenderer>().sharedMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>($"{ArtFolder}/Building Materials.mat");
                root.AddComponent<ItemPickup>();
                root.SetActive(false);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, DropPrefab);
                return prefab.GetComponent<ItemPickup>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        internal static ItemSlotDrag AddDragOverlay(RectTransform window)
        {
            var rect = CreateRect(window, "Drag Preview", Center, Vector2.zero, new Vector2(60f, 60f));
            var preview = SetImage(rect, new Color(1f, 1f, 1f, 0.92f));
            preview.preserveAspect = true;
            preview.raycastTarget = false;
            preview.gameObject.SetActive(false);
            var drag = window.gameObject.AddComponent<ItemSlotDrag>();
            var settings = new SerializedObject(drag);
            settings.FindProperty("_preview").objectReferenceValue = preview;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return drag;
        }

        internal static ScrollRect CreateGrid(RectTransform panel, string name, Vector2 position,
            Vector2 viewportSize, Vector2 cellSize)
        {
            var viewport = CreateRect(panel, name, Center, position, viewportSize);
            viewport.gameObject.AddComponent<RectMask2D>();
            SetImage(viewport, Panel).raycastTarget = true;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            var top = new Vector2(0.5f, 1f);
            var content = CreateRect(viewport, "Content", top, Vector2.zero, new Vector2(viewportSize.x, 0f));
            content.pivot = top;
            var layout = content.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = cellSize;
            layout.spacing = new Vector2(10f, 10f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            return scroll;
        }

        internal static InventorySlotView CreateSlotTemplate(Transform parent, Vector2 cell)
        {
            var root = CreateRect(parent, "Slot Template", Center, Vector2.zero, cell);
            var button = MakeButton(root);
            var icon = SetImage(CreateRect(root, "Icon", Center, new Vector2(0f, cell.y * 0.1f),
                Vector2.one * (cell.y * 0.54f)), Color.white);
            icon.preserveAspect = true;
            var title = CreateText(root, "Item", string.Empty, new Vector2(0f, -cell.y * 0.34f),
                new Vector2(cell.x - 8f, 29f), 13);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 13;
            var quantity = CreateText(root, "Quantity", string.Empty, new Vector2(cell.x * 0.27f, cell.y * 0.33f), new Vector2(40f, 22f),
                15, TextAnchor.MiddleRight);
            quantity.color = Accent;
            var selection = SetImage(CreateRect(root, "Selection", Center, new Vector2(0f, 1f - cell.y * 0.5f),
                new Vector2(cell.x, 2f)), Accent);
            var view = root.gameObject.AddComponent<InventorySlotView>();
            var settings = new SerializedObject(view);
            settings.FindProperty("_button").objectReferenceValue = button;
            settings.FindProperty("_icon").objectReferenceValue = icon;
            settings.FindProperty("_title").objectReferenceValue = title;
            settings.FindProperty("_quantity").objectReferenceValue = quantity;
            settings.FindProperty("_selection").objectReferenceValue = selection;
            settings.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return view;
        }

        internal static Button MakeButton(RectTransform rect)
        {
            var image = SetImage(rect, Surface);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.92f, 0.78f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.62f, 0.62f, 0.62f);
            button.colors = colors;
            return button;
        }
    }
}
