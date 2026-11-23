using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectBelow.Editor
{
    internal static class ExplorationLightingSetup
    {
        private const string ArtFolder = "Assets/_Project/Art";
        private const string MaterialsFolder = ArtFolder + "/Materials";
        private const string LitShader = "Universal Render Pipeline/Lit";

        public static void AddPlayerLight(Camera camera)
        {
            var light = new GameObject("Player Spotlight").AddComponent<Light>();
            light.transform.SetParent(camera.transform, false);
            light.transform.localPosition = new Vector3(0.08f, -0.12f, 0.04f);
            light.type = LightType.Spot;
            light.color = new Color32(221, 233, 255, 255);
            light.intensity = 6.5f;
            ConfigurePlayerBeam(camera, light);
            ConfigureShadows(light);
        }

        private static void ConfigurePlayerBeam(Camera camera, Light light)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            light.range = 5.5f;
            light.spotAngle = 70f;
            light.innerSpotAngle = 32f;
        }

        private static void ConfigureEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.ambientEquatorColor = Color.black;
            RenderSettings.ambientGroundColor = Color.black;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.reflectionIntensity = 0f;

            RenderSettings.fog = false;
        }

        public static void ConfigureDungeon(Transform geometry)
        {
            ConfigureEnvironment();
            EnsureMaterialsFolder();
            var wall = GetOrCreateMaterial("Dungeon Wall", new Color32(62, 72, 83, 255), 0.09f);
            var floor = GetOrCreateMaterial("Dungeon Floor", new Color32(70, 63, 57, 255), 0.12f);
            var ceiling = GetOrCreateMaterial("Dungeon Ceiling", new Color32(41, 46, 53, 255), 0.04f);
            var warmLens = GetOrCreateEmissiveLens("Dungeon Lamp Lens", new Color(1.1f, 0.71f, 0.39f, 1f));
            var coolLens = GetOrCreateEmissiveLens("Dungeon Exit Lamp Lens", new Color(0.42f, 0.70f, 1.12f, 1f));

            foreach (Transform wallSegment in geometry.Find("Perimeter Walls"))
                wallSegment.GetComponent<Renderer>().sharedMaterial = wall;
            geometry.Find("Room Pillar").GetComponent<Renderer>().sharedMaterial = wall;
            geometry.Find("Floor").GetComponent<Renderer>().sharedMaterial = floor;
            geometry.Find("Room Step").GetComponent<Renderer>().sharedMaterial = floor;

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Ceiling";
            roof.transform.SetParent(geometry, false);
            roof.transform.localPosition = new Vector3(0.5f, 3.14f, -0.5f);
            roof.transform.localScale = new Vector3(12f, 0.4f, 18f);
            roof.GetComponent<Renderer>().sharedMaterial = ceiling;

            Transform lights = new GameObject("Dungeon Lighting").transform;
            var junction = CreateSconce("Junction Lamp", lights, new Vector3(0f, 2.60f, 5.77f),
                Vector3.back, warmLens, LightType.Spot, new Color32(255, 194, 135, 255), 6.8f, 4.6f);
            AimSpotlight(junction, new Vector3(0f, 0.40f, 4.1f), 92f, 52f);

            CreateSconce("Lever Lamp", lights, new Vector3(-2.65f, 2.44f, 7.77f),
                Vector3.back, warmLens, LightType.Point, new Color32(255, 205, 154, 255), 2.1f, 3.4f);

            var exit = CreateSconce("Exit Lamp", lights, new Vector3(5.77f, 2.53f, 4.5f),
                Vector3.left, coolLens, LightType.Spot, new Color32(169, 201, 255, 255), 4.2f, 5.4f);
            AimSpotlight(exit, new Vector3(3.55f, 0.65f, 4.5f), 84f, 45f);
        }

        private static Material GetOrCreateEmissiveLens(string name, Color emission)
        {
            var material = GetOrCreateMaterial(name, Color.black, 0f);
            if (material.shader.name == LitShader && material.IsKeywordEnabled("_EMISSION")
                && (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.AnyEmissive) != 0)
                return material;

            material.shader = FindLitShader();
            material.color = Color.black;
            material.SetFloat("_Smoothness", 0f);
            material.SetColor("_EmissionColor", emission);
            // URP Lit validates emission against this flag. The lens meshes do not contribute GI.
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            material.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static Light CreateSconce(string name, Transform parent, Vector3 position, Vector3 inward,
            Material lens, LightType type, Color color, float intensity, float range)
        {
            var fixture = new GameObject(name).transform;
            fixture.SetParent(parent, false);
            fixture.localPosition = position;
            fixture.localRotation = Quaternion.LookRotation(inward);

            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = "Lamp Lens";
            cover.transform.SetParent(fixture, false);
            cover.transform.localScale = new Vector3(0.32f, 0.15f, 0.14f);
            UnityEngine.Object.DestroyImmediate(cover.GetComponent<Collider>());
            var renderer = cover.GetComponent<Renderer>();
            renderer.sharedMaterial = lens;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            var light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(fixture, false);
            light.transform.localPosition = new Vector3(0f, -0.11f, 0.30f);
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            ConfigureShadows(light);
            return light;
        }

        private static void AimSpotlight(Light light, Vector3 target, float outerAngle, float innerAngle)
        {
            light.transform.rotation = Quaternion.LookRotation(target - light.transform.position);
            light.spotAngle = outerAngle;
            light.innerSpotAngle = innerAngle;
        }

        private static void ConfigureShadows(Light light)
        {
            light.shadows = LightShadows.Soft;
            light.shadowBias = 0.045f;
            light.shadowNormalBias = 0.15f;
            light.shadowNearPlane = 0.08f;

            int tier = light.type == LightType.Point
                ? UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow
                : UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
            var data = light.GetUniversalAdditionalLightData();

            // URP exposes the tier as read-only. Use the serialized field shared with its Inspector.
            var settings = new SerializedObject(data);
            settings.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = tier;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder(ArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "Art");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                AssetDatabase.CreateFolder(ArtFolder, "Materials");
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var material = new Material(FindLitShader()) { color = color, name = name };
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Shader FindLitShader()
        {
            return Shader.Find(LitShader) ?? throw new InvalidOperationException($"The shader {LitShader} is required.");
        }
    }
}
