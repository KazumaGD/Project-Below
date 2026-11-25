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
using static ProjectBelow.Editor.HubWorkSetup;

namespace ProjectBelow.Editor
{
    internal static class HubFacilitiesSetup
    {
        private const string FacilitiesFolder = "Assets/_Project/Data/Facilities";
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private static readonly Color Background = new(0.079f, 0.071f, 0.062f);
        private static readonly Color Surface = new(0.124f, 0.107f, 0.089f);
        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);
        private static readonly Color Divider = new(0.24f, 0.20f, 0.15f);
        private static readonly Vector2 CardSize = new(928f, 119f);

        internal static void AddToHub(HubHud hub)
        {
            var hubSettings = new SerializedObject(hub);
            SeedFacilities((ExpeditionSession)hubSettings.FindProperty("_sessionPrefab").objectReferenceValue);
            if (hub.GetComponent<FacilitiesHud>() != null)
                return;

            var home = ((Button)hubSettings.FindProperty("_start").objectReferenceValue).transform.parent;
            var open = AddButton(home, "Open Facilities", "Помещения", new Vector2(88f, 145f), new Vector2(192f, 34f));
            var panel = CreateRect(hub.transform, "Facilities", Center, Vector2.zero, new Vector2(1000f, 564f));
            SetImage(panel, Background);
            CreateText(panel, "Title", "Помещения", new Vector2(-245f, 232f), new Vector2(438f, 54f),
                39, TextAnchor.MiddleLeft);
            var back = AddButton(panel, "Back", "Назад", new Vector2(388f, 232f), new Vector2(150f, 42f));
            SetImage(CreateRect(panel, "Divider", Center, new Vector2(0f, 196f), new Vector2(CardSize.x, 1f)), Divider);
            var status = CreateText(panel, "Construction Status", string.Empty, new Vector2(0f, 166f), new Vector2(CardSize.x, 44f),
                21, TextAnchor.MiddleLeft);
            status.color = Accent;
            var list = AddScrollList(panel, "Facility List", new Vector2(0f, -59f), new Vector2(CardSize.x, 396f), Background);

            var settings = new SerializedObject(hub.gameObject.AddComponent<FacilitiesHud>());
            settings.FindProperty("_hub").objectReferenceValue = hub;
            settings.FindProperty("_home").objectReferenceValue = home.gameObject;
            settings.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            settings.FindProperty("_open").objectReferenceValue = open;
            settings.FindProperty("_back").objectReferenceValue = back;
            settings.FindProperty("_status").objectReferenceValue = status;
            settings.FindProperty("_list").objectReferenceValue = list;
            settings.FindProperty("_template").objectReferenceValue = CreateCard(hub.transform);
            settings.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
        }

        private static FacilityCardView CreateCard(Transform canvas)
        {
            var card = CreateRect(canvas, "Facility Card Template", Center, Vector2.zero, CardSize);
            SetImage(card, Surface);
            var name = CreateText(card, "Name", string.Empty, new Vector2(-296f, 32f), new Vector2(286f, 35f),
                26, TextAnchor.MiddleLeft);
            var description = CreateText(card, "Description", string.Empty, new Vector2(-296f, -20f), new Vector2(286f, 53f),
                17, TextAnchor.UpperLeft);
            SetImage(CreateRect(card, "Info Divider", Center, new Vector2(-144f, 0f), new Vector2(1f, 81f)), Divider);
            var level = CreateText(card, "Level", string.Empty, new Vector2(17f, 32f), new Vector2(282f, 28f),
                19, TextAnchor.MiddleLeft);
            level.color = Accent;
            var active = CreateText(card, "Active Bonus", string.Empty, new Vector2(17f, 0f), new Vector2(282f, 38f),
                16, TextAnchor.MiddleLeft);
            var next = CreateText(card, "Next Bonus", string.Empty, new Vector2(17f, -31f), new Vector2(282f, 30f),
                16, TextAnchor.MiddleLeft);
            next.color = Accent;
            SetImage(CreateRect(card, "Cost Divider", Center, new Vector2(172f, 0f), new Vector2(1f, 81f)), Divider);
            var cost = CreateText(card, "Cost", string.Empty, new Vector2(318f, 34f), new Vector2(256f, 40f),
                16);
            var availability = CreateText(card, "Availability", string.Empty, new Vector2(318f, 2f), new Vector2(256f, 25f),
                17);
            var build = AddButton(card, "Build", "Начать улучшение", new Vector2(318f, -34f), new Vector2(240f, 34f));

            var view = card.gameObject.AddComponent<FacilityCardView>();
            var settings = new SerializedObject(view);
            settings.FindProperty("_name").objectReferenceValue = name;
            settings.FindProperty("_description").objectReferenceValue = description;
            settings.FindProperty("_level").objectReferenceValue = level;
            settings.FindProperty("_activeBonus").objectReferenceValue = active;
            settings.FindProperty("_nextBonus").objectReferenceValue = next;
            settings.FindProperty("_cost").objectReferenceValue = cost;
            settings.FindProperty("_availability").objectReferenceValue = availability;
            settings.FindProperty("_build").objectReferenceValue = build;
            settings.ApplyModifiedPropertiesWithoutUndo();
            card.gameObject.SetActive(false);
            return view;
        }

        private static void SeedFacilities(ExpeditionSession session)
        {
            var settings = new SerializedObject(session);
            bool needsFacilities = settings.FindProperty("_facilities").arraySize == 0;
            bool needsTask = settings.FindProperty("_constructionTask").objectReferenceValue == null;
            if (!needsFacilities && !needsTask)
                return;

            var definitions = needsFacilities ? CreateFacilities() : null;
            var task = needsTask ? CreateTask("Construction", "Строительство", "Завершает оплаченное улучшение вечером.",
                new[] { CharacterSpecialization.Builder }, null, 0, null, 0, 0) : null;
            string path = AssetDatabase.GetAssetPath(session);
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                settings = new SerializedObject(prefab.GetComponent<ExpeditionSession>());
                if (needsFacilities)
                {
                    var facilities = settings.FindProperty("_facilities");
                    facilities.arraySize = definitions.Length;
                    for (int i = 0; i < definitions.Length; i++)
                        facilities.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
                }
                if (needsTask)
                    settings.FindProperty("_constructionTask").objectReferenceValue = task;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static FacilityDefinition[] CreateFacilities()
        {
            var food = AssetDatabase.LoadAssetAtPath<ItemDefinition>(FoodPath);
            var metal = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Metal.asset");
            var materials = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Project/Data/Items/Building Materials.asset");
            if (food == null || metal == null || materials == null)
                throw new InvalidOperationException("Create the existing resource assets before installing facilities.");
            if (!AssetDatabase.IsValidFolder(FacilitiesFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Facilities");

            return new[]
            {
                CreateFacility("Farm", "Ферма", "Увеличивает выпуск пищи при выращивании.",
                    food, materials, metal, (4, 0, 4, 0), (8, 2, 7, 0)),
                CreateFacility("Workshop", "Мастерская", "Увеличивает выпуск стройматериалов.",
                    materials, materials, metal, (5, 0, 2, 0), (9, 3, 4, 0)),
                CreateFacility("Living Quarters", "Жилая зона", "Усиливает восстановление при отдыхе.",
                    null, materials, metal, (3, 0, 0, 10), (6, 2, 0, 20))
            };
        }

        private static FacilityDefinition CreateFacility(string assetName, string name, string description,
            ItemDefinition product, ItemDefinition materials, ItemDefinition metal,
            (int Materials, int Metal, int Output, int Recovery) first, (int Materials, int Metal, int Output, int Recovery) second)
        {
            string path = $"{FacilitiesFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<FacilityDefinition>(path);
            if (existing != null)
                return existing;

            var definition = ScriptableObject.CreateInstance<FacilityDefinition>();
            var settings = new SerializedObject(definition);
            settings.FindProperty("_displayName").stringValue = name;
            settings.FindProperty("_description").stringValue = description;
            var upgrades = settings.FindProperty("_upgrades");
            upgrades.arraySize = 2;
            ConfigureUpgrade(upgrades.GetArrayElementAtIndex(0), product, materials, metal, first);
            ConfigureUpgrade(upgrades.GetArrayElementAtIndex(1), product, materials, metal, second);
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, path);
            DefinitionIds.Assign(definition, path);
            return definition;
        }

        private static void ConfigureUpgrade(SerializedProperty upgrade, ItemDefinition product, ItemDefinition materials,
            ItemDefinition metal, (int Materials, int Metal, int Output, int Recovery) values)
        {
            var costs = upgrade.FindPropertyRelative("_costs");
            costs.arraySize = values.Metal == 0 ? 1 : 2;
            SetResource(costs.GetArrayElementAtIndex(0), materials, values.Materials);
            if (values.Metal > 0)
                SetResource(costs.GetArrayElementAtIndex(1), metal, values.Metal);
            SetResource(upgrade.FindPropertyRelative("_productionBonus"), product, values.Output);
            upgrade.FindPropertyRelative("_restBonus").intValue = values.Recovery;
        }
    }
}
