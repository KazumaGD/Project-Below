using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ProjectBelow.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(AudioSource))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        private const float MinimumGroundSpeed = 0.12f;
        private const float TeleportDistance = 3f;

        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
        [SerializeField, Min(0.1f)] private float _stepDistance = 1.65f;
        [SerializeField, Range(0f, 0.4f)] private float _volumeVariation = 0.14f;
        [SerializeField] private Vector2 _pitchRange = new(0.94f, 1.06f);

        private CharacterController _controller;
        private AudioSource _source;
        private AudioClip[] _playableClips;
        private Vector3 _previousPosition;
        private float _distanceSinceStep;
        private int _lastClipIndex = -1;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _source = GetComponent<AudioSource>();
            _playableClips = Array.FindAll(_clips, clip => clip != null);
        }

        private void OnEnable()
        {
            _previousPosition = transform.position;
            _distanceSinceStep = 0f;
        }

        private void OnDisable()
        {
            _source?.Stop();
        }

        private void LateUpdate()
        {
            // Sample after CharacterController.Move so a held key against a wall adds no distance.
            Vector3 displacement = transform.position - _previousPosition;
            _previousPosition = transform.position;
            displacement.y = 0f;
            float distance = displacement.magnitude;

            if (!_controller.isGrounded || distance > TeleportDistance)
            {
                _distanceSinceStep = 0f;
                return;
            }

            if (_playableClips.Length == 0 || distance <= MinimumGroundSpeed * Time.deltaTime)
                return;

            _distanceSinceStep += distance;
            float stepDistance = Mathf.Max(0.1f, _stepDistance);
            if (_distanceSinceStep < stepDistance)
                return;

            _distanceSinceStep %= stepDistance;
            int clipIndex = Random.Range(0, _playableClips.Length);
            if (clipIndex == _lastClipIndex && _playableClips.Length > 1)
                clipIndex = (_lastClipIndex + Random.Range(1, _playableClips.Length)) % _playableClips.Length;
            _lastClipIndex = clipIndex;

            _source.pitch = Random.Range(Mathf.Clamp(_pitchRange.x, 0.01f, 3f), Mathf.Clamp(_pitchRange.y, 0.01f, 3f));
            _source.PlayOneShot(_playableClips[clipIndex], Random.Range(1f - _volumeVariation, 1f));
        }
    }
}
