using System;
using ProjectBelow.Core;
using ProjectBelow.Hub;
using ProjectBelow.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ProjectBelow.Editor.ExpeditionSetup;
using static ProjectBelow.Editor.ExplorationCombatSetup;
using static ProjectBelow.Editor.HubSceneBuilder;

namespace ProjectBelow.Editor
{
    internal static class HubWorkSetup
    {
        private const string TasksFolder = "Assets/_Project/Data/Tasks";
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private static readonly Color Backdrop = new(0.079f, 0.071f, 0.062f);
        private static readonly Color Surface = new(0.124f, 0.107f, 0.089f);
        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);
        private static readonly Color Divider = new(0.24f, 0.20f, 0.15f);

        internal static void AddToHub(HubHud hub)
        {
            var settings = new SerializedObject(hub);
            SeedTasks((ExpeditionSession)settings.FindProperty("_sessionPrefab").objectReferenceValue);
            var residents = new SerializedObject(hub.GetComponent<ResidentsHud>());
            if (residents.FindProperty("_tasks").objectReferenceValue == null)
                AddTaskPicker(residents);
            if (settings.FindProperty("_workReport").objectReferenceValue == null)
                AddWorkReport(hub, settings);
        }

        private static void AddTaskPicker(SerializedObject residents)
        {
            var details = ((GameObject)residents.FindProperty("_details").objectReferenceValue).transform;
            var frame = Move(details, "Portrait Frame", new Vector2(-153f, 153f), Vector2.one * 76f);
            ((RectTransform)frame.Find("Portrait")).sizeDelta = Vector2.one * 74f;
            Move(details, "Name", new Vector2(40f, 167f), new Vector2(264f, 37f));
            Move(details, "Specialization", new Vector2(40f, 136f), new Vector2(264f, 27f));
            Move(details, "Description", new Vector2(0f, 80f), new Vector2(386f, 54f));
            ((RectTransform)details.Find("Divider")).anchoredPosition = new Vector2(0f, 47f);

            ((RectTransform)details.Find("Health")).anchoredPosition = new Vector2(0f, 25f);
            ((RectTransform)details.Find("Satiety")).anchoredPosition = new Vector2(0f, -8f);
            ((RectTransform)details.Find("Morale")).anchoredPosition = new Vector2(0f, -41f);
            ((RectTransform)details.Find("Work Capacity")).anchoredPosition = new Vector2(0f, -74f);
            SetImage(CreateRect(details, "Work Divider", Center, new Vector2(0f, -96f), new Vector2(386f, 1f)), Divider);

            var picker = CreateRect(details, "Task Picker", Center, new Vector2(0f, -151f), new Vector2(386f, 100f));
            CreateText(picker, "Task Heading", "Поручение", new Vector2(0f, 40f), new Vector2(386f, 19f), 15).color = Accent;
            var previous = AddButton(picker, "Previous Task", "‹", new Vector2(-174f, 14f), new Vector2(38f, 34f));
            var next = AddButton(picker, "Next Task", "›", new Vector2(174f, 14f), new Vector2(38f, 34f));
            previous.GetComponentInChildren<Text>().fontSize = 28;
            next.GetComponentInChildren<Text>().fontSize = 28;
            var task = CreateText(picker, "Task", string.Empty, new Vector2(0f, 14f), new Vector2(298f, 31f), 21);
            var forecast = CreateText(picker, "Forecast", string.Empty, new Vector2(0f, -14f), new Vector2(386f, 25f), 17);
            forecast.color = Accent;
            var remark = CreateText(picker, "Task Details", string.Empty, new Vector2(0f, -39f), new Vector2(386f, 24f), 15);

            var view = picker.gameObject.AddComponent<ResidentTaskView>();
            var settings = new SerializedObject(view);
            settings.FindProperty("_previous").objectReferenceValue = previous;
            settings.FindProperty("_next").objectReferenceValue = next;
            settings.FindProperty("_task").objectReferenceValue = task;
            settings.FindProperty("_forecast").objectReferenceValue = forecast;
            settings.FindProperty("_details").objectReferenceValue = remark;
            settings.ApplyModifiedPropertiesWithoutUndo();
            residents.FindProperty("_tasks").objectReferenceValue = view;
            residents.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform Move(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)parent.Find(name);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void AddWorkReport(HubHud hub, SerializedObject hubSettings)
        {
            var home = ((Button)hubSettings.FindProperty("_start").objectReferenceValue).transform.parent;
            Move(home, "Status", new Vector2(-275f, -221f), new Vector2(258f, 24f));
            var open = AddButton(home, "Open Work Report", "Работа", new Vector2(-296f, -182f), new Vector2(219f, 51f));

            var panel = CreateRect(hub.transform, "Work Report", Center, Vector2.zero, new Vector2(1000f, 564f));
            SetImage(panel, Backdrop);
            var heading = CreateText(panel, "Heading", string.Empty, new Vector2(-126f, 230f), new Vector2(676f, 54f),
                37, TextAnchor.MiddleLeft);
            var back = AddButton(panel, "Back", "Назад", new Vector2(388f, 230f), new Vector2(150f, 42f));
            SetImage(CreateRect(panel, "Divider", Center, new Vector2(0f, 193f), new Vector2(928f, 1f)), Divider);
            var list = AddScrollList(panel, "Results", new Vector2(0f, -27f), new Vector2(928f, 416f), Surface);
            var empty = CreateText(panel, "Empty", "В убежище пока нет обитателей", Vector2.zero, new Vector2(620f, 40f), 24);
            var template = CreateText(hub.transform, "Work Result Template", string.Empty, Vector2.zero, new Vector2(862f, 91f),
                24, TextAnchor.MiddleLeft);
            template.lineSpacing = 1.26f;
            template.supportRichText = false;
            template.gameObject.SetActive(false);

            var view = hub.gameObject.AddComponent<WorkReportHud>();
            var settings = new SerializedObject(view);
            settings.FindProperty("_home").objectReferenceValue = home.gameObject;
            settings.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            settings.FindProperty("_open").objectReferenceValue = open;
            settings.FindProperty("_back").objectReferenceValue = back;
            settings.FindProperty("_openLabel").objectReferenceValue = open.GetComponentInChildren<Text>();
            settings.FindProperty("_heading").objectReferenceValue = heading;
            settings.FindProperty("_empty").objectReferenceValue = empty;
            settings.FindProperty("_list").objectReferenceValue = list;
            settings.FindProperty("_template").objectReferenceValue = template;
            settings.ApplyModifiedPropertiesWithoutUndo();
            hubSettings.FindProperty("_workReport").objectReferenceValue = view;
            hubSettings.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
            open.gameObject.SetActive(false);
        }

        private static void SeedTasks(ExpeditionSession session)
        {
            if (new SerializedObject(session).FindProperty("_tasks").arraySize != 0)
                return;

            var food = AssetDatabase.LoadAssetAtPath<ItemDefinition>(FoodPath);
            var metal = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
            var materials = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Building Materials.asset");
            if (food == null || metal == null || materials == null)
                throw new InvalidOperationException("Create the shelter resource assets before installing work assignments.");
            if (!AssetDatabase.IsValidFolder(TasksFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Tasks");

            var definitions = new[]
            {
                CreateTask("Rest", "Отдых", "Восстанавливает трудоспособность.", Array.Empty<CharacterSpecialization>(),
                    null, 0, null, 0, 20),
                CreateTask("Grow Food", "Выращивание пищи", "Количество зависит от трудоспособности.", new[] { CharacterSpecialization.Agronomist },
                    null, 0, food, 8, 0),
                CreateTask("Process Metal", "Переработка металла", "Использует металл со склада.",
                    new[] { CharacterSpecialization.Craftsperson, CharacterSpecialization.Builder }, metal, 2, materials, 4, 0)
            };

            string path = AssetDatabase.GetAssetPath(session);
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var settings = new SerializedObject(prefab.GetComponent<ExpeditionSession>());
                var tasks = settings.FindProperty("_tasks");
                tasks.arraySize = definitions.Length;
                for (int i = 0; i < definitions.Length; i++)
                    tasks.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        internal static TaskDefinition CreateTask(string assetName, string displayName, string description,
            CharacterSpecialization[] specializations, ItemDefinition input, int inputQuantity, ItemDefinition output,
            int outputQuantity, int recovery)
        {
            string path = $"{TasksFolder}/{assetName}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<TaskDefinition>(path);
            if (definition != null)
                return definition;

            definition = ScriptableObject.CreateInstance<TaskDefinition>();
            var settings = new SerializedObject(definition);
            settings.FindProperty("_displayName").stringValue = displayName;
            settings.FindProperty("_description").stringValue = description;
            var roles = settings.FindProperty("_specializations");
            roles.arraySize = specializations.Length;
            for (int i = 0; i < specializations.Length; i++)
                roles.GetArrayElementAtIndex(i).enumValueIndex = (int)specializations[i];
            if (input != null)
            {
                var costs = settings.FindProperty("_costs");
                costs.arraySize = 1;
                SetResource(costs.GetArrayElementAtIndex(0), input, inputQuantity);
            }
            SetResource(settings.FindProperty("_output"), output, outputQuantity);
            settings.FindProperty("_workCapacityRecovery").intValue = recovery;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, path);
            DefinitionIds.Assign(definition, path);
            return definition;
        }

        internal static void SetResource(SerializedProperty property, ItemDefinition item, int quantity)
        {
            property.FindPropertyRelative("_item").objectReferenceValue = item;
            property.FindPropertyRelative("_quantity").intValue = quantity;
        }
    }
}
