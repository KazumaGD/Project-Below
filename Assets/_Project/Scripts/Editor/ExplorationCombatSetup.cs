using System;
using System.Linq;
using ProjectBelow.Combat;
using ProjectBelow.Exploration;
using ProjectBelow.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectBelow.Editor
{
    internal static class ExplorationCombatSetup
    {
        public static void AddEncounter(GameObject player, InputActionAsset actions)
        {
            CreateEventSystem(actions);
            var gameMode = new GameObject("Game Modes").AddComponent<GameModeController>();
            var modeSettings = new SerializedObject(gameMode);
            modeSettings.FindProperty("_player").objectReferenceValue = player.GetComponent<FirstPersonController>();
            modeSettings.FindProperty("_interaction").objectReferenceValue = player.GetComponent<PlayerInteraction>();
            modeSettings.ApplyModifiedPropertiesWithoutUndo();

            var combat = gameMode.gameObject.AddComponent<CombatController>();
            CreateEnemy(gameMode);
            var canvas = new GameObject("Combat UI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            CreateHud(canvas, combat);
        }

        private static void CreateEnemy(GameModeController gameMode)
        {
            var enemy = new GameObject("Encounter Dummy");
            enemy.transform.SetPositionAndRotation(new Vector3(4.05f, 0f, 4.65f), Quaternion.Euler(0f, -90f, 0f));
            var collider = enemy.AddComponent<CapsuleCollider>();
            collider.height = 1.90f;
            collider.radius = 0.37f;
            collider.center = Vector3.up * 0.95f;

            Transform root = enemy.transform;
            CreatePart(root, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.84f, 0f), new Vector3(0.66f, 0.83f, 0.52f));
            CreatePart(root, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.65f, 0.04f), new Vector3(0.44f, 0.40f, 0.40f));
            CreatePart(root, "Left Arm", PrimitiveType.Capsule, new Vector3(-0.36f, 1.07f, -0.05f), new Vector3(0.19f, 0.39f, 0.20f));
            CreatePart(root, "Right Arm", PrimitiveType.Capsule, new Vector3(0.36f, 1.07f, -0.05f), new Vector3(0.19f, 0.39f, 0.20f));

            var eyeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Dungeon Lamp Lens.mat");
            foreach (float x in new[] { -0.075f, 0.075f })
                CreatePart(root, "Eye", PrimitiveType.Sphere, new Vector3(x, 1.68f, 0.218f), Vector3.one * 0.07f)
                    .sharedMaterial = eyeMaterial;

            var eyes = new GameObject("Sight Origin").transform;
            eyes.SetParent(root, false);
            eyes.localPosition = new Vector3(0f, 1.60f, 0.38f);
            var focus = new GameObject("Combat Focus").transform;
            focus.SetParent(root, false);
            focus.localPosition = new Vector3(0f, 1.42f, 0.12f);

            ConfigureEnemy(enemy);
            var settings = new SerializedObject(enemy.AddComponent<EnemyDetection>());
            settings.FindProperty("_gameMode").objectReferenceValue = gameMode;
            settings.FindProperty("_eyes").objectReferenceValue = eyes;
            settings.FindProperty("_combatFocus").objectReferenceValue = focus;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void ConfigureEnemy(GameObject enemy)
        {
            const string dataPath = "Assets/_Project/Data";
            const string definitionPath = dataPath + "/Dungeon Guardian.asset";
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(definitionPath);
            if (definition == null)
            {
                if (!AssetDatabase.IsValidFolder(dataPath))
                    AssetDatabase.CreateFolder("Assets/_Project", "Data");
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(definition, definitionPath);
            }

            var settings = new SerializedObject(enemy.AddComponent<EnemyCombatant>());
            settings.FindProperty("_definition").objectReferenceValue = definition;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Renderer CreatePart(Transform root, string name, PrimitiveType type, Vector3 position, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(root, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            return part.GetComponent<Renderer>();
        }

        private static void CreateEventSystem(InputActionAsset actions)
        {
            var references = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(actions))
                .OfType<InputActionReference>()
                .Where(reference => (reference.hideFlags & HideFlags.HideInHierarchy) == 0)
                .ToArray();
            var map = actions.FindActionMap("UI", true);
            InputActionReference Reference(string actionName)
            {
                InputAction action = map.FindAction(actionName, true);
                return references.SingleOrDefault(reference => reference.action?.id == action.id)
                    ?? throw new InvalidOperationException($"Missing stored input reference for UI/{actionName}.");
            }

            var point = Reference("Point");
            var click = Reference("Click");
            var navigate = Reference("Navigate");
            var submit = Reference("Submit");
            var cancel = Reference("Cancel");

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            var module = eventSystem.AddComponent<InputSystemUIInputModule>();
            module.actionsAsset = actions;
            module.point = point;
            module.leftClick = click;
            module.move = navigate;
            module.submit = submit;
            module.cancel = cancel;
        }

        internal static void CreateHud(GameObject canvasObject, CombatController combat)
        {
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = CreateRect(canvas.transform, "Content", Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            Color panelColor = new(0.075f, 0.070f, 0.067f, 0.97f);
            Color accent = new(0.87f, 0.70f, 0.46f);
            var topPanel = CreateRect(root, "Opponent", new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(360f, 72f));
            SetImage(topPanel, panelColor);
            var enemyName = CreateText(topPanel, "Enemy Name", string.Empty, new Vector2(0f, 12f), new Vector2(320f, 32f), 22);
            var healthTrack = CreateRect(topPanel, "Health Track", Vector2.one * 0.5f, new Vector2(0f, -17f), new Vector2(312f, 8f));
            SetImage(healthTrack, new Color(0.23f, 0.20f, 0.17f));
            var enemyHealth = CreateRect(healthTrack, "Fill", Vector2.zero, Vector2.zero, Vector2.zero);
            enemyHealth.anchorMax = Vector2.one;
            SetImage(enemyHealth, accent);

            var panel = CreateRect(root, "Commands", new Vector2(0.5f, 0f), new Vector2(0f, 131f), new Vector2(600f, 222f));
            SetImage(panel, panelColor);
            var playerHealth = CreateText(panel, "Player Health", string.Empty, new Vector2(-152f, 79f),
                new Vector2(252f, 29f), 20, TextAnchor.MiddleLeft);
            var phase = CreateText(panel, "Phase", string.Empty, new Vector2(148f, 79f),
                new Vector2(260f, 29f), 20, TextAnchor.MiddleRight);
            phase.color = accent;
            var log = CreateText(panel, "Combat Log", string.Empty, new Vector2(0f, 11f), new Vector2(552f, 90f),
                18, TextAnchor.LowerLeft);
            log.verticalOverflow = VerticalWrapMode.Overflow;
            var attack = CreateButton(panel, "Attack", "Атака", -188f, -70f);
            var defend = CreateButton(panel, "Defend", "Защита", 0f, -70f);
            var flee = CreateButton(panel, "Flee", "Побег", 188f, -70f);
            var restart = CreateButton(panel, "Restart", "Начать заново", 0f, -70f);
            restart.gameObject.SetActive(false);
            root.gameObject.SetActive(false);

            var settings = new SerializedObject(canvasObject.AddComponent<CombatHud>());
            settings.FindProperty("_combat").objectReferenceValue = combat;
            settings.FindProperty("_content").objectReferenceValue = root.gameObject;
            settings.FindProperty("_enemyName").objectReferenceValue = enemyName;
            settings.FindProperty("_enemyHealth").objectReferenceValue = enemyHealth;
            settings.FindProperty("_playerHealth").objectReferenceValue = playerHealth;
            settings.FindProperty("_phase").objectReferenceValue = phase;
            settings.FindProperty("_log").objectReferenceValue = log;
            settings.FindProperty("_attack").objectReferenceValue = attack;
            settings.FindProperty("_defend").objectReferenceValue = defend;
            settings.FindProperty("_flee").objectReferenceValue = flee;
            settings.FindProperty("_restart").objectReferenceValue = restart;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image SetImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size,
            int fontSize, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var text = CreateRect(parent, name, Vector2.one * 0.5f, position, size).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.95f, 0.91f, 0.84f);
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, float x, float y)
        {
            var rect = CreateRect(parent, name, Vector2.one * 0.5f, new Vector2(x, y), new Vector2(176f, 44f));
            var image = SetImage(rect, new Color(0.32f, 0.28f, 0.23f));
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.82f, 0.79f, 0.74f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.63f, 0.60f, 0.56f);
            colors.disabledColor = new Color(0.42f, 0.40f, 0.38f);
            button.colors = colors;
            CreateText(rect, "Label", label, Vector2.zero, rect.sizeDelta, 20);
            return button;
        }
    }
}
