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
    internal static class HubRationsSetup
    {
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private static readonly Vector2 RowSize = new(984f, 103f);
        private static readonly Color Backdrop = new(0.079f, 0.071f, 0.062f);
        private static readonly Color Surface = new(0.124f, 0.107f, 0.089f);
        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);
        private static readonly Color Divider = new(0.24f, 0.20f, 0.15f);

        internal static void AddToHub(HubHud hub)
        {
            var hubSettings = new SerializedObject(hub);
            AssignFood((ExpeditionSession)hubSettings.FindProperty("_sessionPrefab").objectReferenceValue);
            if (hub.GetComponent<RationsHud>() != null)
                return;

            var open = (Button)hubSettings.FindProperty("_openRations").objectReferenceValue;
            open.name = "Open Rations";
            open.GetComponentInChildren<Text>(true).text = "Распределить пищу";
            var page = CreateRect(hub.transform, "Rations", Center, Vector2.zero, new Vector2(1064f, 566f));
            SetImage(page, Backdrop);
            CreateText(page, "Heading", "Распределение пищи", new Vector2(-234f, 234f), new Vector2(516f, 53f),
                38, TextAnchor.MiddleLeft);
            var day = CreateText(page, "Day", string.Empty, new Vector2(250f, 234f), new Vector2(200f, 34f), 20,
                TextAnchor.MiddleRight);
            day.color = Accent;
            var back = AddButton(page, "Back", "Назад", new Vector2(424f, 234f), new Vector2(140f, 42f));
            SetImage(CreateRect(page, "Divider", Center, new Vector2(0f, 200f), new Vector2(RowSize.x, 1f)), Divider);
            var food = CreateText(page, "Food Stock", string.Empty, new Vector2(-245f, 175f), new Vector2(494f, 36f),
                21, TextAnchor.MiddleLeft);
            CreateText(page, "Forecast Heading", "Прогноз до следующего утра", new Vector2(300f, 175f),
                new Vector2(384f, 30f), 17, TextAnchor.MiddleRight).color = Accent;

            var list = AddScrollList(page, "Ration List", new Vector2(0f, -16f), new Vector2(RowSize.x, 334f), Backdrop);
            var empty = CreateText(page, "Empty", "В убежище пока нет обитателей", Vector2.zero, new Vector2(600f, 45f),
                23);
            var budget = CreateText(page, "Food Balance", string.Empty, new Vector2(-242f, -234f), new Vector2(500f, 44f),
                22, TextAnchor.MiddleLeft);
            var confirm = AddButton(page, "Confirm Rations", "Подтвердить и завершить день", new Vector2(317f, -234f),
                new Vector2(350f, 50f));
            confirm.targetGraphic.color = new Color(0.36f, 0.26f, 0.16f);

            var settings = new SerializedObject(hub.gameObject.AddComponent<RationsHud>());
            settings.FindProperty("_hub").objectReferenceValue = hub;
            settings.FindProperty("_home").objectReferenceValue = open.transform.parent.gameObject;
            settings.FindProperty("_page").objectReferenceValue = page.gameObject;
            settings.FindProperty("_list").objectReferenceValue = list;
            settings.FindProperty("_template").objectReferenceValue = CreateRow(hub.transform);
            settings.FindProperty("_day").objectReferenceValue = day;
            settings.FindProperty("_food").objectReferenceValue = food;
            settings.FindProperty("_empty").objectReferenceValue = empty;
            settings.FindProperty("_budget").objectReferenceValue = budget;
            settings.FindProperty("_open").objectReferenceValue = open;
            settings.FindProperty("_back").objectReferenceValue = back;
            settings.FindProperty("_confirm").objectReferenceValue = confirm;
            settings.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);
        }

        private static RationRowView CreateRow(Transform canvas)
        {
            var row = CreateRect(canvas, "Ration Row Template", Center, Vector2.zero, RowSize);
            SetImage(row, Surface);
            var frame = CreateRect(row, "Portrait Frame", Center, new Vector2(-435f, 0f), new Vector2(80f, 80f));
            SetImage(frame, Divider);
            var portrait = SetImage(CreateRect(frame, "Portrait", Center, Vector2.zero, new Vector2(78f, 78f)), Color.white);
            portrait.preserveAspect = true;
            var name = CreateText(row, "Name", string.Empty, new Vector2(-288f, 24f), new Vector2(190f, 31f),
                24, TextAnchor.MiddleLeft);
            var specialization = CreateText(row, "Specialization", string.Empty, new Vector2(-288f, 0f),
                new Vector2(190f, 23f), 17, TextAnchor.MiddleLeft);
            specialization.color = Accent;
            var capacity = CreateText(row, "Work Capacity", string.Empty, new Vector2(-288f, -24f),
                new Vector2(190f, 22f), 14, TextAnchor.MiddleLeft);
            var none = AddChoice(row, "None", "Без еды", -60f);
            var half = AddChoice(row, "Half", "½ порции", 160f);
            var full = AddChoice(row, "Full", "Полная порция", 380f);
            var satiety = AddForecast(row, "Satiety", -60f);
            var health = AddForecast(row, "Health", 160f);
            var morale = AddForecast(row, "Morale", 380f);

            var view = row.gameObject.AddComponent<RationRowView>();
            var settings = new SerializedObject(view);
            settings.FindProperty("_portrait").objectReferenceValue = portrait;
            settings.FindProperty("_displayName").objectReferenceValue = name;
            settings.FindProperty("_specialization").objectReferenceValue = specialization;
            settings.FindProperty("_workCapacity").objectReferenceValue = capacity;
            settings.FindProperty("_satiety").objectReferenceValue = satiety;
            settings.FindProperty("_health").objectReferenceValue = health;
            settings.FindProperty("_morale").objectReferenceValue = morale;
            settings.FindProperty("_none").objectReferenceValue = none;
            settings.FindProperty("_half").objectReferenceValue = half;
            settings.FindProperty("_full").objectReferenceValue = full;
            settings.ApplyModifiedPropertiesWithoutUndo();
            row.gameObject.SetActive(false);
            return view;
        }

        private static Button AddChoice(Transform row, string name, string caption, float x) =>
            AddButton(row, name, caption, new Vector2(x, 20f), new Vector2(202f, 34f));

        private static Text AddForecast(Transform row, string name, float x) =>
            CreateText(row, name, string.Empty, new Vector2(x, -23f), new Vector2(202f, 28f), 18);

        private static void AssignFood(ExpeditionSession session)
        {
            var settings = new SerializedObject(session);
            if (settings.FindProperty("_food").objectReferenceValue != null)
                return;

            var food = AssetDatabase.LoadAssetAtPath<ItemDefinition>(FoodPath);
            if (food == null)
                throw new InvalidOperationException("Create the Food item before installing the ration screen.");

            string path = AssetDatabase.GetAssetPath(session);
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                settings = new SerializedObject(prefab.GetComponent<ExpeditionSession>());
                settings.FindProperty("_food").objectReferenceValue = food;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }
    }
}
