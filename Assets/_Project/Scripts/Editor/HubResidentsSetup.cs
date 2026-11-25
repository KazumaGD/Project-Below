using ProjectBelow.Core;
using ProjectBelow.Hub;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ProjectBelow.Editor.ExpeditionSetup;
using static ProjectBelow.Editor.ExplorationCombatSetup;
using static ProjectBelow.Editor.ExplorationInventorySetup;
using static ProjectBelow.Editor.HubSceneBuilder;

namespace ProjectBelow.Editor
{
    internal static class HubResidentsSetup
    {
        private const string DefinitionsFolder = "Assets/_Project/Data/Characters";
        private const string PortraitsFolder = "Assets/_Project/Art/Characters";
        private static readonly Vector2 Center = Vector2.one * 0.5f;
        private static readonly Vector2 Top = new(0.5f, 1f);
        private static readonly Color Backdrop = new(0.079f, 0.071f, 0.062f);
        private static readonly Color Surface = new(0.124f, 0.107f, 0.089f);
        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);
        private static readonly Vector2 CardSize = new(448f, 122f);

        internal static void AddToHub(HubHud hub)
        {
            if (hub.GetComponent<ResidentsHud>() != null)
                return;

            var hubSettings = new SerializedObject(hub);
            var session = (ExpeditionSession)hubSettings.FindProperty("_sessionPrefab").objectReferenceValue;
            SeedRoster(session);

            var home = ((Button)hubSettings.FindProperty("_start").objectReferenceValue).transform.parent;
            var open = AddButton(home, "Open Residents", "Обитатели", new Vector2(88f, 187f), new Vector2(192f, 42f));
            var panel = CreateRect(hub.transform, "Residents", Center, Vector2.zero, new Vector2(1000f, 564f));
            SetImage(panel, Backdrop);
            CreateText(panel, "Title", "Обитатели", new Vector2(-277f, 232f), new Vector2(376f, 54f),
                39, TextAnchor.MiddleLeft);
            var back = AddButton(panel, "Back", "Назад", new Vector2(388f, 232f), new Vector2(150f, 42f));
            SetImage(CreateRect(panel, "Divider", Center, new Vector2(0f, 193f), new Vector2(928f, 1f)), Surface);
            var list = AddScrollList(panel, "Roster", new Vector2(-240f, -28f), new Vector2(448f, 416f), Backdrop);
            var template = CreateCard(hub.transform);
            var empty = CreateText(panel, "Empty", "В убежище пока нет обитателей", new Vector2(0f, -26f),
                new Vector2(620f, 56f), 24);
            empty.gameObject.SetActive(false);

            var details = CreateRect(panel, "Details", Center, new Vector2(248f, -28f), new Vector2(432f, 416f));
            SetImage(details, Surface);
            var portrait = AddPortrait(details, new Vector2(-141f, 136f), 102f);
            var displayName = CreateText(details, "Name", string.Empty, new Vector2(54f, 157f), new Vector2(242f, 40f),
                28, TextAnchor.MiddleLeft);
            var specialization = CreateText(details, "Specialization", string.Empty, new Vector2(54f, 121f),
                new Vector2(242f, 30f), 19, TextAnchor.MiddleLeft);
            specialization.color = Accent;
            var description = CreateText(details, "Description", string.Empty, new Vector2(0f, 37f), new Vector2(386f, 68f),
                18, TextAnchor.UpperLeft);
            SetImage(CreateRect(details, "Divider", Center, new Vector2(0f, -3f), new Vector2(386f, 1f)), Backdrop);

            var settings = new SerializedObject(hub.gameObject.AddComponent<ResidentsHud>());
            settings.FindProperty("_hub").objectReferenceValue = hub;
            settings.FindProperty("_home").objectReferenceValue = home.gameObject;
            settings.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            settings.FindProperty("_open").objectReferenceValue = open;
            settings.FindProperty("_back").objectReferenceValue = back;
            settings.FindProperty("_list").objectReferenceValue = list;
            settings.FindProperty("_template").objectReferenceValue = template;
            settings.FindProperty("_empty").objectReferenceValue = empty.gameObject;
            settings.FindProperty("_details").objectReferenceValue = details.gameObject;
            settings.FindProperty("_portrait").objectReferenceValue = portrait;
            settings.FindProperty("_displayName").objectReferenceValue = displayName;
            settings.FindProperty("_specialization").objectReferenceValue = specialization;
            settings.FindProperty("_description").objectReferenceValue = description;
            AddStat(details, "Health", "Здоровье", -32f, settings.FindProperty("_health"));
            AddStat(details, "Satiety", "Сытость", -76f, settings.FindProperty("_satiety"));
            AddStat(details, "Morale", "Мораль", -120f, settings.FindProperty("_morale"));
            AddStat(details, "Work Capacity", "Трудоспособность", -164f, settings.FindProperty("_workCapacity"));
            settings.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
        }

        private static ResidentCardView CreateCard(Transform canvas)
        {
            var rect = CreateRect(canvas, "Resident Card Template", Top, Vector2.zero, CardSize);
            var button = MakeButton(rect);
            button.targetGraphic.color = Surface;
            var selection = SetImage(CreateRect(rect, "Selected", Center, new Vector2(-222f, 0f),
                new Vector2(4f, CardSize.y)), Accent);
            var portrait = AddPortrait(rect, new Vector2(-166f, 0f), 88f);
            var displayName = CreateText(rect, "Name", string.Empty, new Vector2(51f, 32f), new Vector2(320f, 33f),
                23, TextAnchor.MiddleLeft);
            var specialization = CreateText(rect, "Specialization", string.Empty, new Vector2(51f, 7f),
                new Vector2(320f, 22f), 16, TextAnchor.MiddleLeft);
            specialization.color = Accent;
            var healthAndMorale = CreateText(rect, "Health and Morale", string.Empty, new Vector2(-47f, -28f),
                new Vector2(124f, 43f), 15, TextAnchor.MiddleLeft);
            var satietyAndWork = CreateText(rect, "Satiety and Work Capacity", string.Empty, new Vector2(113f, -28f),
                new Vector2(178f, 43f), 15, TextAnchor.MiddleLeft);
            var card = rect.gameObject.AddComponent<ResidentCardView>();
            var settings = new SerializedObject(card);
            settings.FindProperty("_button").objectReferenceValue = button;
            settings.FindProperty("_selection").objectReferenceValue = selection;
            settings.FindProperty("_portrait").objectReferenceValue = portrait;
            settings.FindProperty("_displayName").objectReferenceValue = displayName;
            settings.FindProperty("_specialization").objectReferenceValue = specialization;
            settings.FindProperty("_healthAndMorale").objectReferenceValue = healthAndMorale;
            settings.FindProperty("_satietyAndWork").objectReferenceValue = satietyAndWork;
            settings.ApplyModifiedPropertiesWithoutUndo();
            rect.gameObject.SetActive(false);
            return card;
        }

        private static Image AddPortrait(Transform parent, Vector2 position, float size)
        {
            var backing = CreateRect(parent, "Portrait Frame", Center, position, Vector2.one * (size + 2f));
            SetImage(backing, new Color(0.24f, 0.20f, 0.15f));
            var portrait = SetImage(CreateRect(backing, "Portrait", Center, Vector2.zero, Vector2.one * size), Color.white);
            portrait.preserveAspect = true;
            return portrait;
        }

        private static void AddStat(Transform details, string name, string caption, float y, SerializedProperty property)
        {
            var row = CreateRect(details, name, Center, new Vector2(0f, y), new Vector2(386f, 41f));
            CreateText(row, "Label", caption, new Vector2(-63f, 7f), new Vector2(260f, 24f), 17, TextAnchor.MiddleLeft);
            var value = CreateText(row, "Value", string.Empty, new Vector2(136f, 7f), new Vector2(114f, 24f),
                17, TextAnchor.MiddleRight);
            var bar = CreateRect(row, "Bar", Center, new Vector2(0f, -10f), new Vector2(386f, 5f));
            SetImage(bar, Backdrop);
            var fill = CreateRect(bar, "Fill", Vector2.zero, Vector2.zero, Vector2.zero);
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            SetImage(fill, Accent);
            property.FindPropertyRelative("_fill").objectReferenceValue = fill;
            property.FindPropertyRelative("_value").objectReferenceValue = value;
        }

        private static void SeedRoster(ExpeditionSession session)
        {
            var settings = new SerializedObject(session);
            var roster = settings.FindProperty("_residents");
            if (roster.arraySize != 0)
                return;

            if (!AssetDatabase.IsValidFolder(DefinitionsFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Characters");

            var definitions = new[]
            {
                GetOrCreateDefinition("Mira", "Мира", CharacterSpecialization.Craftsperson, new CharacterStats(92, 78, 72, 88),
                    "Чинит инструменты и собирает простые механизмы из найденных деталей."),
                GetOrCreateDefinition("Savely", "Савелий", CharacterSpecialization.Agronomist, new CharacterStats(86, 81, 76, 74),
                    "Знает, как выращивать пищу при скудном свете и проверять пригодность семян."),
                GetOrCreateDefinition("Lada", "Лада", CharacterSpecialization.Builder, new CharacterStats(96, 70, 84, 91),
                    "Умеет укреплять стены и приспосабливать пустые помещения под нужды убежища.")
            };
            string prefabPath = AssetDatabase.GetAssetPath(session);
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                settings = new SerializedObject(prefab.GetComponent<ExpeditionSession>());
                roster = settings.FindProperty("_residents");
                roster.arraySize = definitions.Length;
                for (int i = 0; i < definitions.Length; i++)
                    roster.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static CharacterDefinition GetOrCreateDefinition(string assetName, string displayName,
            CharacterSpecialization specialization, CharacterStats initial, string description)
        {
            string path = $"{DefinitionsFolder}/{assetName}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (definition != null)
                return definition;

            string portraitPath = $"{PortraitsFolder}/{assetName}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(portraitPath);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            definition = ScriptableObject.CreateInstance<CharacterDefinition>();
            var settings = new SerializedObject(definition);
            settings.FindProperty("_displayName").stringValue = displayName;
            settings.FindProperty("_portrait").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
            settings.FindProperty("_description").stringValue = description;
            settings.FindProperty("_specialization").enumValueIndex = (int)specialization;
            var values = settings.FindProperty("_initialStats");
            values.FindPropertyRelative("_health").intValue = initial.Health;
            values.FindPropertyRelative("_satiety").intValue = initial.Satiety;
            values.FindPropertyRelative("_morale").intValue = initial.Morale;
            values.FindPropertyRelative("_workCapacity").intValue = initial.WorkCapacity;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(definition, path);
            DefinitionIds.Assign(definition, path);
            return definition;
        }
    }
}
