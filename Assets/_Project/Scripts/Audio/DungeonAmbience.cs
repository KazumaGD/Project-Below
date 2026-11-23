using System;
using UnityEngine;
using UnityEngine.Audio;

namespace ProjectBelow.Audio
{
    [DisallowMultipleComponent]
    public sealed class DungeonAmbience : MonoBehaviour
    {
        [Serializable]
        private sealed class Layer
        {
            [SerializeField] private AudioClip _clip;
            [SerializeField, Range(0f, 1f)] private float _volume = 0.25f;

            public AudioClip Clip => _clip;
            public float Volume => _volume;
        }

        [SerializeField] private AudioMixerGroup _output;
        [SerializeField] private Layer[] _layers = Array.Empty<Layer>();
        [SerializeField] private bool _playOnEnable = true;
        [SerializeField, Min(0f)] private float _fadeInSeconds = 2.5f;
        [SerializeField, Min(0f)] private float _fadeOutSeconds = 2f;

        private AudioSource[] _sources = Array.Empty<AudioSource>();
        private float _gain;
        private float _targetGain;

        private void Awake()
        {
            _sources = new AudioSource[_layers.Length];
            for (int index = 0; index < _layers.Length; index++)
            {
                if (_layers[index]?.Clip == null)
                    continue;

                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.volume = 0f;
                source.clip = _layers[index].Clip;
                source.outputAudioMixerGroup = _output;
                _sources[index] = source;
            }
        }

        private void OnEnable()
        {
            if (_playOnEnable)
                FadeIn();
        }

        private void OnDisable()
        {
            _targetGain = 0f;
            ApplyGain(0f);
            StopLayers();
        }

        public void FadeIn()
        {
            if (isActiveAndEnabled)
                _targetGain = 1f;
        }

        public void FadeOut()
        {
            _targetGain = 0f;
            if (_gain == 0f || _fadeOutSeconds <= 0f)
            {
                ApplyGain(0f);
                StopLayers();
            }
        }

        private void Update()
        {
            if (_gain == _targetGain)
                return;

            bool starting = _gain == 0f;
            float duration = _targetGain > _gain ? _fadeInSeconds : _fadeOutSeconds;
            // A stalled frame should not skip the audible fade.
            float delta = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float gain = duration > 0f
                ? Mathf.MoveTowards(_gain, _targetGain, delta / duration)
                : _targetGain;
            ApplyGain(gain);
            if (_gain > 0f && starting)
                PlayLayers();
            else if (_gain == 0f)
                StopLayers();
        }

        private void ApplyGain(float gain)
        {
            _gain = gain;
            for (int index = 0; index < _sources.Length; index++)
            {
                if (_sources[index] != null)
                    _sources[index].volume = gain * _layers[index].Volume;
            }
        }

        private void StopLayers()
        {
            foreach (AudioSource source in _sources)
            {
                if (source != null)
                    source.Stop();
            }
        }

        private void PlayLayers()
        {
            foreach (AudioSource source in _sources)
            {
                if (source != null)
                    source.Play();
            }
        }
    }
}
