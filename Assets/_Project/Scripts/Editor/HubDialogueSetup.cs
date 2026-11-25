using System;
using ProjectBelow.Combat;
using ProjectBelow.Hub;
using ProjectBelow.Narrative;
using ProjectBelow.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ProjectBelow.Editor.ExpeditionSetup;
using static ProjectBelow.Editor.ExplorationCombatSetup;
using static ProjectBelow.Editor.HubSceneBuilder;

namespace ProjectBelow.Editor
{
    internal static class HubDialogueSetup
    {
        private const string DialogueFolder = "Assets/_Project/Data/Narrative";
        private const string DemoPath = DialogueFolder + "/Hub Conversation.asset";
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private static readonly Color Background = new(0.079f, 0.071f, 0.062f);
        private static readonly Color Surface = new(0.124f, 0.107f, 0.089f);
        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);

        [MenuItem("Project Below/Dialogue/Validate Selected")]
        private static void ValidateSelected()
        {
            var dialogue = (DialogueDefinition)Selection.activeObject;
            if (dialogue.Validate(out string error))
                Debug.Log($"Dialogue '{dialogue.name}' is valid.", dialogue);
            else
                Debug.LogError(error, dialogue);
        }

        [MenuItem("Project Below/Dialogue/Validate Selected", true)]
        private static bool CanValidateSelected() => Selection.activeObject is DialogueDefinition;

        internal static void AddToHub(HubHud hub, GameModeController mode)
        {
            var hubSettings = new SerializedObject(hub);
            var home = ((Button)hubSettings.FindProperty("_start").objectReferenceValue).transform.parent;
            if (home.Find("Start Test Dialogue") != null)
                return;

            var demo = GetOrCreateDemo();
            if (!demo.Validate(out string error))
                throw new InvalidOperationException(error);

            var group = hub.GetComponent<CanvasGroup>();
            if (group == null)
                group = hub.gameObject.AddComponent<CanvasGroup>();
            var launch = AddButton(home, "Start Test Dialogue", "Тестовый диалог",
                new Vector2(337f, -224f), new Vector2(138f, 32f));
            launch.GetComponentInChildren<Text>().fontSize = 16;

            var canvas = CreateCanvas("Dialogue UI", 40);
            var overlay = CreateRect(canvas.transform, "Dialogue Overlay", Center, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            SetImage(overlay, new Color(0.016f, 0.013f, 0.011f, 0.85f)).raycastTarget = true;
            var page = CreateRect(overlay, "Dialogue Panel", Center, Vector2.zero, new Vector2(1000f, 604f));
            SetImage(page, Background);
            var portraits = CreateRect(page, "Participants", Center, new Vector2(0f, 164f), new Vector2(900f, 236f));
            var layout = portraits.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var speech = CreateRect(page, "Speech", Center, new Vector2(0f, -49f), new Vector2(900f, 160f));
            SetImage(speech, Surface);
            var speaker = CreateText(speech, "Speaker", string.Empty, new Vector2(0f, 50f), new Vector2(844f, 36f),
                27, TextAnchor.MiddleLeft);
            speaker.color = Accent;
            var line = CreateText(speech, "Line", string.Empty, new Vector2(0f, -17f), new Vector2(844f, 100f),
                25, TextAnchor.UpperLeft);
            line.lineSpacing = 1.2f;
            line.supportRichText = false;
            line.resizeTextForBestFit = true;
            line.resizeTextMinSize = 17;
            line.resizeTextMaxSize = 25;

            var choices = AddScrollList(page, "Dialogue Choices", new Vector2(0f, -202f), new Vector2(900f, 132f), Background);
            var continueButton = AddButton(page, "Dialogue Continue", "Продолжить", new Vector2(326f, -209f), new Vector2(248f, 49f));
            continueButton.targetGraphic.color = new Color(0.36f, 0.26f, 0.16f);
            var choiceTemplate = AddButton(canvas.transform, "Dialogue Choice Template", string.Empty, Vector2.zero,
                new Vector2(900f, 48f));
            choiceTemplate.GetComponentInChildren<Text>().supportRichText = false;
            choiceTemplate.gameObject.SetActive(false);
            var portraitTemplate = CreatePortrait(canvas.transform);
            overlay.gameObject.SetActive(false);

            var view = new SerializedObject(canvas.gameObject.AddComponent<DialogueHud>());
            view.FindProperty("_hub").objectReferenceValue = hub;
            view.FindProperty("_mode").objectReferenceValue = mode;
            view.FindProperty("_hubCanvas").objectReferenceValue = group;
            view.FindProperty("_demo").objectReferenceValue = demo;
            view.FindProperty("_launch").objectReferenceValue = launch;
            view.FindProperty("_overlay").objectReferenceValue = overlay.gameObject;
            view.FindProperty("_portraitRow").objectReferenceValue = portraits;
            view.FindProperty("_portraitTemplate").objectReferenceValue = portraitTemplate;
            view.FindProperty("_speaker").objectReferenceValue = speaker;
            view.FindProperty("_line").objectReferenceValue = line;
            view.FindProperty("_continue").objectReferenceValue = continueButton;
            view.FindProperty("_choices").objectReferenceValue = choices;
            view.FindProperty("_choiceTemplate").objectReferenceValue = choiceTemplate;
            view.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DialoguePortraitView CreatePortrait(Transform canvas)
        {
            var card = CreateRect(canvas, "Dialogue Portrait Template", Center, Vector2.zero, new Vector2(200f, 229f));
            SetImage(card, Surface);
            card.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var highlight = SetImage(CreateRect(card, "Speaker Highlight", Center, new Vector2(0f, 15f), new Vector2(180f, 180f)), Accent);
            var portrait = SetImage(CreateRect(card, "Portrait", Center, new Vector2(0f, 15f), new Vector2(176f, 176f)), Color.white);
            portrait.preserveAspect = true;
            var name = CreateText(card, "Name", string.Empty, new Vector2(0f, -92f), new Vector2(190f, 30f), 19);
            var portraitView = card.gameObject.AddComponent<DialoguePortraitView>();
            var fields = new SerializedObject(portraitView);
            fields.FindProperty("_portrait").objectReferenceValue = portrait;
            fields.FindProperty("_highlight").objectReferenceValue = highlight;
            fields.FindProperty("_name").objectReferenceValue = name;
            fields.ApplyModifiedPropertiesWithoutUndo();
            card.gameObject.SetActive(false);
            return portraitView;
        }

        private static DialogueDefinition GetOrCreateDemo()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(DemoPath);
            if (existing != null)
                return existing;

            var mira = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/_Project/Data/Characters/Mira.asset");
            var savely = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/_Project/Data/Characters/Savely.asset");
            if (mira == null || savely == null)
                throw new InvalidOperationException("Create the shelter residents before installing the test dialogue.");
            if (!AssetDatabase.IsValidFolder(DialogueFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Narrative");

            var definition = ScriptableObject.CreateInstance<DialogueDefinition>();
            var settings = new SerializedObject(definition);
            settings.FindProperty("_entryNodeId").stringValue = "draft";
            var nodes = settings.FindProperty("_nodes");
            nodes.arraySize = 6;
            SetNode(nodes, 0, "draft", mira, "У полок опять тянет холодом. Савелий, как думаешь, с чего начнём?", "priorities");
            var branch = SetNode(nodes, 1, "priorities", savely,
                "Можно проверить, как хранятся припасы, или посмотреть, откуда идёт сквозняк.", string.Empty);
            var choices = branch.FindPropertyRelative("_choices");
            choices.arraySize = 2;
            SetChoice(choices.GetArrayElementAtIndex(0), "Проверить запасы", "pantry", "hub.plan_pantry");
            SetChoice(choices.GetArrayElementAtIndex(1), "Осмотреть вентиляцию", "vent", "hub.plan_vent");
            SetNode(nodes, 2, "pantry", mira, "Тогда начнём с полок. Если в нижних ящиках сыро, переставим их повыше.", "pantry_end");
            SetNode(nodes, 3, "pantry_end", savely, "Договорились. Я запишу, какие ящики мы уже осмотрели.", string.Empty);
            SetNode(nodes, 4, "vent", savely, "Посмотрим вентиляционную решётку. По сквозняку будет проще найти щель.", "vent_end");
            SetNode(nodes, 5, "vent_end", mira, "Возьму фонарь, чтобы заглянуть за решётку.", string.Empty);
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, DemoPath);
            return definition;
        }

        private static SerializedProperty SetNode(SerializedProperty nodes, int index, string id, CharacterDefinition speaker,
            string line, string next)
        {
            var node = nodes.GetArrayElementAtIndex(index);
            node.FindPropertyRelative("_id").stringValue = id;
            node.FindPropertyRelative("_speaker").objectReferenceValue = speaker;
            node.FindPropertyRelative("_text").stringValue = line;
            node.FindPropertyRelative("_nextNodeId").stringValue = next;
            return node;
        }

        private static void SetChoice(SerializedProperty option, string caption, string next, string flag)
        {
            option.FindPropertyRelative("_text").stringValue = caption;
            option.FindPropertyRelative("_nextNodeId").stringValue = next;
            option.FindPropertyRelative("_flagId").stringValue = flag;
        }
    }
}
