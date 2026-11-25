using System;
using System.IO;
using System.Linq;
using ProjectBelow.Combat;
using ProjectBelow.Core;
using ProjectBelow.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ProjectBelow.Editor.ExplorationCombatSetup;
using static ProjectBelow.Editor.ExpeditionSetup;

namespace ProjectBelow.Editor
{
    internal static class HubSceneBuilder
    {
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private static readonly Vector2 Top = new(0.5f, 1f);
        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);

        [MenuItem("Project Below/Hub/Create Scene")]
        internal static void CreateHub()
        {
            if (!File.Exists(ExpeditionSession.HubScene))
                BuildNewScene();

            var previous = EditorBuildSettings.globalScenes;
            var menu = File.Exists(ExpeditionSession.MainMenuScene)
                ? new[] { new EditorBuildSettingsScene(ExpeditionSession.MainMenuScene, true) }
                : Array.Empty<EditorBuildSettingsScene>();
            var scenes = menu.Concat(new[]
                {
                    new EditorBuildSettingsScene(ExpeditionSession.HubScene, true),
                    new EditorBuildSettingsScene(ExpeditionSession.DungeonScene, true)
                })
                .Concat(previous.Where(scene => scene.path != ExpeditionSession.MainMenuScene
                    && scene.path != ExpeditionSession.HubScene && scene.path != ExpeditionSession.DungeonScene));
            EditorBuildSettings.globalScenes = scenes.ToArray();
        }

        private static void BuildNewScene()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions == null || !File.Exists(ExpeditionSession.DungeonScene))
                throw new InvalidOperationException("Create ExplorationGraybox and assign the project input actions first.");

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var camera = new GameObject("Hub Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.030f, 0.027f, 0.024f);
                camera.cullingMask = 0;
                var mode = new GameObject("Game Modes").AddComponent<GameModeController>();
                var modeSettings = new SerializedObject(mode);
                modeSettings.FindProperty("_initialMode").enumValueIndex = (int)GameMode.Hub;
                modeSettings.ApplyModifiedPropertiesWithoutUndo();
                CreateEventSystem(actions);
                CreateHud(mode);
                if (!EditorSceneManager.SaveScene(scene, ExpeditionSession.HubScene))
                    throw new InvalidOperationException("Could not save the shelter scene.");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void CreateHud(GameModeController mode)
        {
            var canvas = CreateCanvas("Hub UI", 0);
            var panel = CreateRect(canvas.transform, "Panel", Center, Vector2.zero, new Vector2(890f, 486f));
            SetImage(panel, new Color(0.079f, 0.071f, 0.062f));
            CreateText(panel, "Title", "Убежище", new Vector2(-246f, 169f), new Vector2(314f, 57f), 39, TextAnchor.MiddleLeft);
            CreateText(panel, "Health Label", "Здоровье", new Vector2(321f, 185f), new Vector2(170f, 27f),
                18, TextAnchor.MiddleRight).color = Accent;
            var health = CreateText(panel, "Health", string.Empty, new Vector2(321f, 152f),
                new Vector2(170f, 40f), 27, TextAnchor.MiddleRight);
            SetImage(CreateRect(panel, "Divider", Center, new Vector2(0f, 120f), new Vector2(814f, 1f)),
                new Color(0.24f, 0.20f, 0.15f));

            var stockPanel = CreateRect(panel, "Stock Panel", Center, new Vector2(-208f, -18f), new Vector2(398f, 236f));
            var resultPanel = CreateRect(panel, "Result Panel", Center, new Vector2(208f, -18f), new Vector2(398f, 236f));
            SetImage(stockPanel, new Color(0.124f, 0.107f, 0.089f));
            SetImage(resultPanel, new Color(0.124f, 0.107f, 0.089f));
            CreateText(stockPanel, "Heading", "Склад", new Vector2(0f, 81f), new Vector2(350f, 33f),
                24, TextAnchor.MiddleLeft).color = Accent;
            CreateText(resultPanel, "Heading", "Последняя вылазка", new Vector2(0f, 81f), new Vector2(350f, 33f),
                24, TextAnchor.MiddleLeft).color = Accent;
            var resultTitle = CreateText(resultPanel, "Result", string.Empty, new Vector2(0f, 40f),
                new Vector2(350f, 24f), 18, TextAnchor.MiddleLeft);
            var stockNames = AddRows(stockPanel, "Names", -41f, 54f, 263f, 150f, TextAnchor.UpperLeft);
            var stockQuantities = AddRows(stockPanel, "Quantities", 128f, 54f, 94f, 150f, TextAnchor.UpperRight);
            var resultNames = AddRows(resultPanel, "Names", -41f, 17f, 263f, 113f, TextAnchor.UpperLeft);
            var resultQuantities = AddRows(resultPanel, "Quantities", 128f, 17f, 94f, 113f, TextAnchor.UpperRight);

            var start = AddButton(panel, "Start Expedition", "Начать вылазку", new Vector2(278f, -182f), new Vector2(257f, 51f));
            start.targetGraphic.color = new Color(0.36f, 0.26f, 0.16f);
            var status = CreateText(panel, "Status", string.Empty, new Vector2(-132f, -182f), new Vector2(529f, 39f),
                17, TextAnchor.MiddleLeft);
            var hud = canvas.gameObject.AddComponent<HubHud>();
            var settings = new SerializedObject(hud);
            settings.FindProperty("_sessionPrefab").objectReferenceValue = GetSessionPrefab();
            settings.FindProperty("_mode").objectReferenceValue = mode;
            settings.FindProperty("_stockNames").objectReferenceValue = stockNames;
            settings.FindProperty("_stockQuantities").objectReferenceValue = stockQuantities;
            settings.FindProperty("_health").objectReferenceValue = health;
            settings.FindProperty("_resultTitle").objectReferenceValue = resultTitle;
            settings.FindProperty("_resultNames").objectReferenceValue = resultNames;
            settings.FindProperty("_resultQuantities").objectReferenceValue = resultQuantities;
            settings.FindProperty("_status").objectReferenceValue = status;
            settings.FindProperty("_start").objectReferenceValue = start;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AddDayControls(hud);
            HubResidentsSetup.AddToHub(hud);
            HubRationsSetup.AddToHub(hud);
            HubWorkSetup.AddToHub(hud);
            HubFacilitiesSetup.AddToHub(hud);
            MainMenuSceneBuilder.AddHubSaveControls(hud);
            HubDialogueSetup.AddToHub(hud, mode);
        }

        internal static void AddDayControls(HubHud hud)
        {
            var settings = new SerializedObject(hud);
            var start = (Button)settings.FindProperty("_start").objectReferenceValue;
            var panel = (RectTransform)start.transform.parent;
            var title = (RectTransform)panel.Find("Title");
            title.anchoredPosition = new Vector2(-246f, 186f);
            var day = CreateText(panel, "Day and Phase", string.Empty, new Vector2(-213f, 141f),
                new Vector2(380f, 26f), 19, TextAnchor.MiddleLeft);
            day.color = Accent;
            var skip = AddButton(panel, "Skip Expedition", "Пропустить вылазку", new Vector2(23f, -182f), new Vector2(227f, 51f));
            var rations = AddButton(panel, "Open Rations", "Распределить пищу", new Vector2(278f, -182f), new Vector2(257f, 51f));
            rations.targetGraphic.color = new Color(0.36f, 0.26f, 0.16f);
            rations.gameObject.SetActive(false);
            settings.FindProperty("_dayLabel").objectReferenceValue = day;
            settings.FindProperty("_skip").objectReferenceValue = skip;
            settings.FindProperty("_openRations").objectReferenceValue = rations;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static ScrollRect AddScrollList(Transform parent, string name, Vector2 position, Vector2 size, Color background)
        {
            var viewport = CreateRect(parent, name, Center, position, size);
            viewport.gameObject.AddComponent<RectMask2D>();
            SetImage(viewport, background).raycastTarget = true;

            var content = CreateRect(viewport, "Cards", Top, Vector2.zero, new Vector2(size.x, 0f));
            content.pivot = Top;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 9f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var list = viewport.gameObject.AddComponent<ScrollRect>();
            list.viewport = viewport;
            list.content = content;
            list.horizontal = false;
            list.movementType = ScrollRect.MovementType.Clamped;
            list.scrollSensitivity = 26f;
            return list;
        }

        private static Text AddRows(Transform parent, string name, float x, float top, float width, float height, TextAnchor alignment)
        {
            var text = CreateText(parent, name, string.Empty, new Vector2(x, top - height * 0.5f), new Vector2(width, height), 20, alignment);
            text.lineSpacing = 1.25f;
            return text;
        }
    }
}
