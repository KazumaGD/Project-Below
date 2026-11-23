using ProjectBelow.Exploration;
using ProjectBelow.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBelow.Editor
{
    internal static class ExplorationInteractionSetup
    {
        public static void AddToPlayer(GameObject player, Camera camera)
        {
            var interaction = player.AddComponent<PlayerInteraction>();
            var interactionSettings = new SerializedObject(interaction);
            interactionSettings.FindProperty("_viewCamera").objectReferenceValue = camera;
            interactionSettings.ApplyModifiedPropertiesWithoutUndo();

            var hudRoot = new GameObject("Exploration HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            hudRoot.transform.SetParent(player.transform, false);
            hudRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = hudRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var outline = CreateImage("Crosshair", hudRoot.transform, Vector2.zero, new Vector2(8f, 8f),
                new Color(0.05f, 0.06f, 0.07f, 0.96f));
            CreateImage("Center", outline.transform, Vector2.zero, new Vector2(4f, 4f), Color.white);
            var panel = CreateImage("Interaction Prompt", hudRoot.transform, new Vector2(0f, -84f),
                new Vector2(314f, 44f), new Color(0.04f, 0.05f, 0.06f, 0.9f));

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(panel.transform, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 0f);
            rect.offsetMax = new Vector2(-12f, 0f);
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            outline.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
            var hudSettings = new SerializedObject(hudRoot.AddComponent<InteractionHud>());
            hudSettings.FindProperty("_interaction").objectReferenceValue = interaction;
            hudSettings.FindProperty("_crosshair").objectReferenceValue = outline.gameObject;
            hudSettings.FindProperty("_promptPanel").objectReferenceValue = panel.gameObject;
            hudSettings.FindProperty("_prompt").objectReferenceValue = text;
            hudSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static TestLever AddLever()
        {
            var lever = new GameObject("Test Lever");
            lever.transform.position = new Vector3(-2.65f, 1.35f, 7.72f);

            CreatePrimitive("Wall Plate", PrimitiveType.Cube, lever.transform, Vector3.zero,
                new Vector3(0.58f, 0.72f, 0.28f));
            var pivot = new GameObject("Handle Pivot").transform;
            pivot.SetParent(lever.transform, false);
            pivot.localPosition = new Vector3(-0.07f, -0.2f, -0.24f);
            pivot.localRotation = Quaternion.Euler(0f, 0f, 30f);
            CreatePrimitive("Handle", PrimitiveType.Cube, pivot, new Vector3(0f, 0.18f, 0f),
                new Vector3(0.085f, 0.36f, 0.10f));
            CreatePrimitive("Grip", PrimitiveType.Sphere, pivot, new Vector3(0f, 0.36f, 0f),
                Vector3.one * 0.15f);
            var indicator = CreatePrimitive("Indicator", PrimitiveType.Sphere, lever.transform,
                new Vector3(0.20f, 0.25f, -0.18f), Vector3.one * 0.15f);

            var component = lever.AddComponent<TestLever>();
            var leverSettings = new SerializedObject(component);
            leverSettings.FindProperty("_handlePivot").objectReferenceValue = pivot;
            leverSettings.FindProperty("_indicator").objectReferenceValue = indicator.GetComponent<Renderer>();
            leverSettings.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            return part;
        }
    }
}
