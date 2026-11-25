using System;
using System.IO;
using ProjectBelow.Core;
using ProjectBelow.Hub;
using ProjectBelow.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ProjectBelow.Editor.ExpeditionSetup;
using static ProjectBelow.Editor.ExplorationCombatSetup;

namespace ProjectBelow.Editor
{
    internal static class MainMenuSceneBuilder
    {
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private static readonly Color PanelColor = new(0.079f, 0.071f, 0.062f);
        private static readonly Color ButtonColor = new(0.36f, 0.26f, 0.16f);
        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);

        [MenuItem("Project Below/Main Menu/Create Scene")]
        internal static void CreateMainMenu()
        {
            HubSceneBuilder.CreateHub();
            if (File.Exists(ExpeditionSession.MainMenuScene))
                return;

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actions == null)
                throw new InvalidOperationException("Assign the project's input actions before creating the menu.");

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var camera = new GameObject("Main Menu Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.030f, 0.027f, 0.024f);
                camera.cullingMask = 0;
                CreateEventSystem(actions);
                CreateHud();
                if (!EditorSceneManager.SaveScene(scene, ExpeditionSession.MainMenuScene))
                    throw new InvalidOperationException("Could not save the main menu scene.");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }

            HubSceneBuilder.CreateHub();
        }

        internal static void AddHubSaveControls(HubHud hub)
        {
            if (hub.GetComponent<HubSaveHud>() != null)
                return;

            var hubSettings = new SerializedObject(hub);
            var home = ((Button)hubSettings.FindProperty("_start").objectReferenceValue).transform.parent;
            var save = AddButton(home, "Save Progress", "Сохранить", new Vector2(9f, -224f), new Vector2(154f, 32f));
            var menu = AddButton(home, "Return to Menu", "В главное меню", new Vector2(180f, -224f), new Vector2(172f, 32f));
            var confirmation = MakePanel(hub.transform, "Leave Confirmation", new Vector2(582f, 224f));
            CreateText(confirmation, "Title", "Вернуться в главное меню?", new Vector2(0f, 63f), new Vector2(526f, 51f), 28);
            CreateText(confirmation, "Description", "Несохранённые изменения будут потеряны.",
                new Vector2(0f, 14f), new Vector2(510f, 37f), 18);
            var cancel = AddButton(confirmation, "Cancel", "Отмена", new Vector2(-122f, -62f), new Vector2(206f, 45f));
            var leave = AddButton(confirmation, "Leave", "В главное меню", new Vector2(122f, -62f), new Vector2(206f, 45f));
            leave.targetGraphic.color = ButtonColor;
            confirmation.gameObject.SetActive(false);

            var fields = new SerializedObject(hub.gameObject.AddComponent<HubSaveHud>());
            fields.FindProperty("_hub").objectReferenceValue = hub;
            fields.FindProperty("_home").objectReferenceValue = home.gameObject;
            fields.FindProperty("_status").objectReferenceValue = hubSettings.FindProperty("_status").objectReferenceValue;
            fields.FindProperty("_save").objectReferenceValue = save;
            fields.FindProperty("_menu").objectReferenceValue = menu;
            fields.FindProperty("_confirmation").objectReferenceValue = confirmation.gameObject;
            fields.FindProperty("_leave").objectReferenceValue = leave;
            fields.FindProperty("_cancel").objectReferenceValue = cancel;
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHud()
        {
            var canvas = CreateCanvas("Main Menu UI", 0);
            var menu = MakePanel(canvas.transform, "Main Menu", new Vector2(486f, 496f));
            CreateText(menu, "Title", "Project Below", new Vector2(0f, 170f), new Vector2(422f, 63f), 47);
            SetImage(CreateRect(menu, "Divider", Center, new Vector2(0f, 119f), new Vector2(355f, 1f)), Accent);
            var continueGame = MenuButton(menu, "Continue", "Продолжить", 72f);
            continueGame.targetGraphic.color = ButtonColor;
            var newGame = MenuButton(menu, "New Game", "Новая игра", 11f);
            var settings = MenuButton(menu, "Settings", "Настройки", -50f);
            var quit = MenuButton(menu, "Quit", "Выход", -111f);
            var status = CreateText(menu, "Status", string.Empty, new Vector2(0f, -185f), new Vector2(416f, 53f), 17);
            status.color = Accent;
            continueGame.interactable = false;

            var confirmation = MakePanel(canvas.transform, "Overwrite Confirmation", new Vector2(582f, 224f));
            CreateText(confirmation, "Title", "Начать новую игру?", new Vector2(0f, 63f), new Vector2(526f, 51f), 28);
            CreateText(confirmation, "Description", "Текущий слот будет перезаписан.",
                new Vector2(0f, 14f), new Vector2(510f, 37f), 18);
            var cancel = AddButton(confirmation, "Cancel", "Отмена", new Vector2(-122f, -62f), new Vector2(206f, 45f));
            var replace = AddButton(confirmation, "Replace", "Перезаписать", new Vector2(122f, -62f), new Vector2(206f, 45f));
            replace.targetGraphic.color = ButtonColor;
            confirmation.gameObject.SetActive(false);

            var options = MakePanel(canvas.transform, "Settings Panel", new Vector2(486f, 302f));
            CreateText(options, "Title", "Настройки", new Vector2(0f, 92f), new Vector2(420f, 56f), 34);
            CreateText(options, "Volume Caption", "Общая громкость", new Vector2(0f, 37f), new Vector2(408f, 32f), 21).color = Accent;
            var down = AddButton(options, "Volume Down", "−", new Vector2(-110f, -12f), new Vector2(58f, 44f));
            var up = AddButton(options, "Volume Up", "+", new Vector2(110f, -12f), new Vector2(58f, 44f));
            var volume = CreateText(options, "Volume Value", string.Empty, new Vector2(0f, -12f), new Vector2(140f, 46f), 25);
            var back = AddButton(options, "Settings Back", "Назад", new Vector2(0f, -94f), new Vector2(206f, 45f));
            options.gameObject.SetActive(false);

            var fields = new SerializedObject(canvas.gameObject.AddComponent<MainMenuHud>());
            fields.FindProperty("_sessionPrefab").objectReferenceValue = GetSessionPrefab();
            fields.FindProperty("_menu").objectReferenceValue = menu.gameObject;
            fields.FindProperty("_newGame").objectReferenceValue = newGame;
            fields.FindProperty("_continue").objectReferenceValue = continueGame;
            fields.FindProperty("_settings").objectReferenceValue = settings;
            fields.FindProperty("_quit").objectReferenceValue = quit;
            fields.FindProperty("_status").objectReferenceValue = status;
            fields.FindProperty("_confirmation").objectReferenceValue = confirmation.gameObject;
            fields.FindProperty("_replace").objectReferenceValue = replace;
            fields.FindProperty("_cancel").objectReferenceValue = cancel;
            fields.FindProperty("_settingsPanel").objectReferenceValue = options.gameObject;
            fields.FindProperty("_volumeDown").objectReferenceValue = down;
            fields.FindProperty("_volumeUp").objectReferenceValue = up;
            fields.FindProperty("_volumeLabel").objectReferenceValue = volume;
            fields.FindProperty("_settingsBack").objectReferenceValue = back;
            fields.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button MenuButton(Transform panel, string name, string text, float y)
        {
            var button = AddButton(panel, name, text, new Vector2(0f, y), new Vector2(355f, 49f));
            button.GetComponentInChildren<Text>().fontSize = 22;
            return button;
        }

        private static RectTransform MakePanel(Transform root, string name, Vector2 size)
        {
            var panel = CreateRect(root, name, Center, Vector2.zero, size);
            SetImage(panel, PanelColor);
            return panel;
        }
    }
}
