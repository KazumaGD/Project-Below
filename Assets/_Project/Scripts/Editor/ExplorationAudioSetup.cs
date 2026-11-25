using System;
using System.Linq;
using ProjectBelow.Audio;
using ProjectBelow.Exploration;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectBelow.Editor
{
    internal sealed class ExplorationAudioSetup
    {
        private const string ClipFolder = "Assets/_Project/Audio/Placeholders";
        public const string MixerPath = "Assets/_Project/Audio/Dungeon Audio.mixer";

        private readonly AudioMixerGroup _ambience;
        private readonly AudioMixerGroup _environment;
        private readonly AudioMixerGroup _sfx;

        public ExplorationAudioSetup()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
                throw new InvalidOperationException($"Missing audio mixer at {MixerPath}.");

            _ambience = FindGroup(mixer, "Ambience");
            _environment = FindGroup(mixer, "Environment");
            _sfx = FindGroup(mixer, "SFX");
        }

        public void AddFootsteps(GameObject player)
        {
            AddSource(player, _sfx, 0.42f);
            var settings = new SerializedObject(player.AddComponent<PlayerFootsteps>());
            AssignClips(settings.FindProperty("_clips"), "Stone Step 01", "Stone Step 02", "Stone Step 03");
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        internal AudioSource AddCombatSource(GameObject target) => AddSource(target, _sfx, 0.58f);

        public void AddSceneAudio(TestLever lever)
        {
            var root = new GameObject("Dungeon Audio").transform;
            AddAmbience(root);

            var emitters = new GameObject("Environment Emitters").transform;
            emitters.SetParent(root, false);
            AddEmitter(emitters, "Room Drips", new Vector3(-4.2f, 2.35f, 6.75f), 0.36f, 9.5f,
                new Vector2(3.6f, 8.2f), "Water Drip 01", "Water Drip 02");
            AddEmitter(emitters, "Junction Stone Shifts", new Vector3(0.95f, 0.75f, 5.50f), 0.30f, 10.5f,
                new Vector2(6.5f, 13.5f), "Stone Shift 01", "Stone Shift 02");
            AddEmitter(emitters, "Exit Drips", new Vector3(5.25f, 2.1f, 5.52f), 0.27f, 7.5f,
                new Vector2(4.8f, 11f), "Water Drip 02", "Water Drip 01");

            var switchSound = AddSource(lever.gameObject, _sfx, 0.62f, 6f);
            switchSound.clip = LoadClip("Lever Switch");
            var leverSettings = new SerializedObject(lever);
            leverSettings.FindProperty("_switchSound").objectReferenceValue = switchSound;
            leverSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private void AddAmbience(Transform root)
        {
            var ambience = new GameObject("Dungeon Ambience").AddComponent<DungeonAmbience>();
            ambience.transform.SetParent(root, false);
            var settings = new SerializedObject(ambience);
            settings.FindProperty("_output").objectReferenceValue = _ambience;
            SerializedProperty layers = settings.FindProperty("_layers");
            layers.arraySize = 2;
            SetLayer(layers.GetArrayElementAtIndex(0), "Dungeon Air Loop", 0.21f);
            SetLayer(layers.GetArrayElementAtIndex(1), "Distant Air Texture Loop", 0.16f);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private void AddEmitter(Transform parent, string name, Vector3 position, float volume, float range,
            Vector2 interval, params string[] clips)
        {
            var emitter = new GameObject(name);
            emitter.transform.SetParent(parent, false);
            emitter.transform.localPosition = position;
            AddSource(emitter, _environment, volume, range);
            var settings = new SerializedObject(emitter.AddComponent<DungeonSoundEmitter>());
            AssignClips(settings.FindProperty("_clips"), clips);
            settings.FindProperty("_intervalSeconds").vector2Value = interval;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioSource AddSource(GameObject target, AudioMixerGroup group, float volume, float range = 0f)
        {
            var source = target.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = volume;
            source.outputAudioMixerGroup = group;
            if (range > 0f)
            {
                source.spatialBlend = 1f;
                source.dopplerLevel = 0f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 1f;
                source.maxDistance = range;
            }
            return source;
        }

        private static void AssignClips(SerializedProperty clips, params string[] names)
        {
            clips.arraySize = names.Length;
            for (int index = 0; index < names.Length; index++)
                clips.GetArrayElementAtIndex(index).objectReferenceValue = LoadClip(names[index]);
        }

        private static void SetLayer(SerializedProperty layer, string clip, float volume)
        {
            layer.FindPropertyRelative("_clip").objectReferenceValue = LoadClip(clip);
            layer.FindPropertyRelative("_volume").floatValue = volume;
        }

        internal static AudioClip LoadClip(string name)
        {
            string path = $"{ClipFolder}/{name}.wav";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            return clip != null ? clip : throw new InvalidOperationException($"Missing audio clip at {path}.");
        }

        private static AudioMixerGroup FindGroup(AudioMixer mixer, string name)
        {
            return mixer.FindMatchingGroups(name).Single(group => group.name == name);
        }
    }
}
