using System.IO;
using ProjectBelow.Exploration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ProjectBelow.Editor
{
    internal static class ExplorationGrayboxBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/ExplorationGraybox.unity";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/ExplorationPlayer.prefab";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Project Below/Exploration/Create Graybox")]
        private static void CreateGraybox()
        {
            if (File.Exists(ScenePath))
            {
                Debug.Log($"The exploration scene already exists at {ScenePath}.");
                return;
            }

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                Debug.LogError($"Could not load input actions from {InputActionsPath}.");
                return;
            }

            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            try
            {
                var audioSetup = new ExplorationAudioSetup();
                GameObject playerPrefab = GetOrCreatePlayerPrefab(actions, audioSetup);
                if (playerPrefab == null)
                {
                    Debug.LogError($"Could not load or create the player prefab at {PlayerPrefabPath}.");
                    return;
                }

                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
                player.transform.position = new Vector3(0f, 0.45f, -7f);

                Transform geometry = CreateLayout();
                TestLever lever = ExplorationInteractionSetup.AddLever();
                ExplorationLightingSetup.ConfigureDungeon(geometry);
                audioSetup.AddSceneAudio(lever);
                ExplorationCombatSetup.AddEncounter(player, actions);

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    Debug.LogError($"Could not save the exploration scene at {ScenePath}.");
                    return;
                }

                Debug.Log($"Created {ScenePath}.", AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            }
            finally
            {
                SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject GetOrCreatePlayerPrefab(InputActionAsset actions, ExplorationAudioSetup audioSetup)
        {
            if (File.Exists(PlayerPrefabPath))
                return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            var player = new GameObject("ExplorationPlayer");
            try
            {
                var characterController = player.AddComponent<CharacterController>();
                characterController.height = 1.8f;
                characterController.radius = 0.32f;
                characterController.center = Vector3.up * 0.9f;
                characterController.stepOffset = 0.25f;
                characterController.skinWidth = 0.035f;
                characterController.minMoveDistance = 0f;

                var input = player.AddComponent<PlayerInput>();
                input.actions = actions;
                input.defaultActionMap = "Player";
                input.defaultControlScheme = "Keyboard&Mouse";
                input.neverAutoSwitchControlSchemes = true;
                input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

                var view = new GameObject("View Camera");
                view.transform.SetParent(player.transform, false);
                view.transform.localPosition = Vector3.up * 1.6f;
                view.tag = "MainCamera";
                var camera = view.AddComponent<Camera>();
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 60f;
                camera.fieldOfView = 70f;
                view.AddComponent<AudioListener>();

                var controller = new SerializedObject(player.AddComponent<FirstPersonController>());
                controller.FindProperty("_viewPivot").objectReferenceValue = view.transform;
                controller.ApplyModifiedPropertiesWithoutUndo();

                ExplorationInteractionSetup.AddToPlayer(player, camera);
                ExplorationLightingSetup.AddPlayerLight(camera);
                audioSetup.AddFootsteps(player);
                return PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        private static Transform CreateLayout()
        {
            Transform geometry = new GameObject("Graybox Geometry").transform;
            CreateCube("Floor", geometry, new Vector3(0.5f, -0.25f, -0.5f), new Vector3(12f, 0.5f, 18f));

            Transform walls = new GameObject("Perimeter Walls").transform;
            walls.SetParent(geometry, false);
            Vector2[] perimeter =
            {
                new(-3f, -9f), new(3f, -9f), new(3f, -3f), new(1.5f, -3f),
                new(1.5f, 3f), new(6f, 3f), new(6f, 6f), new(-1.5f, 6f),
                new(-1.5f, 8f), new(-5f, 8f), new(-5f, 3f), new(-1.5f, 3f),
                new(-1.5f, -3f), new(-3f, -3f)
            };

            for (int index = 0; index < perimeter.Length; index++)
            {
                Vector2 start = perimeter[index];
                Vector2 end = perimeter[(index + 1) % perimeter.Length];
                Vector2 center = (start + end) * 0.5f;
                Vector2 span = end - start;
                CreateCube($"Wall {index + 1}", walls, new Vector3(center.x, 1.5f, center.y),
                    new Vector3(Mathf.Abs(span.x) + 0.25f, 3f, Mathf.Abs(span.y) + 0.25f));
            }

            CreateCube("Room Pillar", geometry, new Vector3(-3.65f, 1.5f, 6.2f), new Vector3(0.8f, 3f, 0.8f));
            CreateCube("Room Step", geometry, new Vector3(-3.5f, 0.1f, 4.2f), new Vector3(1.1f, 0.2f, 1.2f));

            Transform exit = new GameObject("Exit Marker").transform;
            exit.SetParent(geometry, false);
            exit.localPosition = new Vector3(5.6f, 0f, 4.5f);
            CreateCube("Left Jamb", exit, new Vector3(0f, 1.05f, -0.65f), new Vector3(0.25f, 2.1f, 0.2f));
            CreateCube("Right Jamb", exit, new Vector3(0f, 1.05f, 0.65f), new Vector3(0.25f, 2.1f, 0.2f));
            CreateCube("Lintel", exit, new Vector3(0f, 2.2f, 0f), new Vector3(0.25f, 0.2f, 1.5f));

            return geometry;
        }

        private static void CreateCube(string name, Transform parent, Vector3 position, Vector3 size)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = size;
        }
    }
}
