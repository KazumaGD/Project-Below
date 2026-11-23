using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProjectBelow.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class DungeonSoundEmitter : MonoBehaviour
    {
        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
        [SerializeField, Tooltip("Minimum and maximum silence between sounds, in seconds.")]
        private Vector2 _intervalSeconds = new(3f, 8f);
        [SerializeField] private Vector2 _pitchRange = new(0.9f, 1.1f);

        private AudioSource _source;
        private AudioClip[] _playableClips;
        private float _waitSeconds;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _playableClips = Array.FindAll(_clips, clip => clip != null);
        }

        private void OnEnable()
        {
            ScheduleNextSound();
        }

        private void OnDisable()
        {
            _source?.Stop();
        }

        private void Update()
        {
            if (_playableClips.Length == 0 || _source.isPlaying)
                return;

            _waitSeconds -= Time.deltaTime;
            if (_waitSeconds > 0f)
                return;

            _source.clip = _playableClips[Random.Range(0, _playableClips.Length)];
            _source.pitch = Random.Range(Mathf.Clamp(_pitchRange.x, 0.01f, 3f), Mathf.Clamp(_pitchRange.y, 0.01f, 3f));
            _source.Play();
            ScheduleNextSound();
        }

        private void ScheduleNextSound()
        {
            _waitSeconds = Random.Range(Mathf.Max(0.05f, _intervalSeconds.x), Mathf.Max(0.05f, _intervalSeconds.y));
        }
    }
}
