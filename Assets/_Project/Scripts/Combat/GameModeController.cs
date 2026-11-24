using System;
using ProjectBelow.Exploration;
using UnityEngine;

namespace ProjectBelow.Combat
{
    public enum GameMode
    {
        Exploration,
        Combat
    }

    [DisallowMultipleComponent]
    public sealed class GameModeController : MonoBehaviour
    {
        [SerializeField] private FirstPersonController _player;
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField, Min(1f)] private float _cameraTurnSpeed = 230f;

        private Transform _combatFocus;
        private bool _previousMovement;
        private bool _previousLook;
        private bool _previousInteraction;
        private bool _previousCursorCapture;

        public event Action<GameMode> ModeChanged;
        public GameMode CurrentMode { get; private set; } = GameMode.Exploration;
        public Transform PlayerView => _player != null ? _player.ViewPivot : null;
        public Transform PlayerRoot => _player != null ? _player.transform : null;

        private void Awake()
        {
            if (PlayerView == null || _interaction == null)
            {
                Debug.LogError("Assign the first-person player and their interaction component.", this);
                enabled = false;
            }
        }

        public bool TryStartCombat(Transform focus)
        {
            if (CurrentMode != GameMode.Exploration || !isActiveAndEnabled || focus == null || PlayerView == null)
                return false;

            _previousMovement = _player.MovementEnabled;
            _previousLook = _player.LookEnabled;
            _previousInteraction = _interaction.InteractionEnabled;
            _previousCursorCapture = _player.CursorCaptureEnabled;

            CurrentMode = GameMode.Combat;
            _combatFocus = focus;
            _player.MovementEnabled = false;
            _player.LookEnabled = false;
            _player.CursorCaptureEnabled = false;
            _interaction.InteractionEnabled = false;
            ModeChanged?.Invoke(CurrentMode);
            return true;
        }

        public void EndCombat()
        {
            if (CurrentMode != GameMode.Combat)
                return;

            CurrentMode = GameMode.Exploration;
            _combatFocus = null;
            if (_player != null)
            {
                _player.MovementEnabled = _previousMovement;
                _player.LookEnabled = _previousLook;
                _player.CursorCaptureEnabled = _previousCursorCapture;
            }
            if (_interaction != null)
                _interaction.InteractionEnabled = _previousInteraction;

            ModeChanged?.Invoke(CurrentMode);
        }

        private void LateUpdate()
        {
            if (CurrentMode != GameMode.Combat)
                return;

            if (_combatFocus == null || PlayerView == null)
            {
                EndCombat();
                return;
            }

            _player.TurnViewTowards(_combatFocus.position, _cameraTurnSpeed * Time.deltaTime);
        }

        private void OnDisable()
        {
            EndCombat();
        }
    }
}
