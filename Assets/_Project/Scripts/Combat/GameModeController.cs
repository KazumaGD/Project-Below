using System;
using ProjectBelow.Exploration;
using ProjectBelow.Inventory;
using UnityEngine;

namespace ProjectBelow.Combat
{
    public enum GameMode
    {
        Exploration,
        Combat,
        Inventory,
        Loot,
        Hub,
        ReturnConfirmation,
        Dialogue
    }

    [DisallowMultipleComponent]
    public sealed class GameModeController : MonoBehaviour
    {
        [SerializeField] private FirstPersonController _player;
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private GameMode _initialMode = GameMode.Exploration;
        [SerializeField, Min(1f)] private float _cameraTurnSpeed = 230f;

        private Transform _combatFocus;
        private bool _previousMovement;
        private bool _previousLook;
        private bool _previousInteraction;
        private bool _previousCursorCapture;

        public event Action<GameMode> ModeChanged;
        public GameMode CurrentMode { get; private set; } = GameMode.Exploration;
        public LootContainer OpenLoot { get; private set; }
        public Transform PlayerView => _player != null ? _player.ViewPivot : null;
        public Transform PlayerRoot => _player != null ? _player.transform : null;

        private void Awake()
        {
            if (_initialMode == GameMode.Hub)
            {
                CurrentMode = GameMode.Hub;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

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

            _combatFocus = focus;
            EnterMode(GameMode.Combat);
            return true;
        }

        public bool TryOpenInventory()
        {
            if (CurrentMode != GameMode.Exploration || !isActiveAndEnabled || PlayerView == null)
                return false;

            EnterMode(GameMode.Inventory);
            return true;
        }

        public void CloseInventory()
        {
            if (CurrentMode == GameMode.Inventory)
                ReturnToExploration();
        }

        internal bool TryOpenLoot(LootContainer container)
        {
            if (CurrentMode != GameMode.Exploration || !isActiveAndEnabled || PlayerView == null || container == null)
                return false;

            OpenLoot = container;
            EnterMode(GameMode.Loot);
            return true;
        }

        public void CloseLoot()
        {
            if (CurrentMode != GameMode.Loot)
                return;

            OpenLoot = null;
            ReturnToExploration();
        }

        internal bool TryOpenReturnConfirmation()
        {
            if (CurrentMode != GameMode.Exploration || !isActiveAndEnabled || PlayerView == null)
                return false;

            EnterMode(GameMode.ReturnConfirmation);
            return true;
        }

        public void CloseReturnConfirmation()
        {
            if (CurrentMode == GameMode.ReturnConfirmation)
                ReturnToExploration();
        }

        public bool TryOpenDialogue()
        {
            if (CurrentMode != GameMode.Hub || !isActiveAndEnabled)
                return false;

            CurrentMode = GameMode.Dialogue;
            ModeChanged?.Invoke(CurrentMode);
            return true;
        }

        public void CloseDialogue()
        {
            if (CurrentMode != GameMode.Dialogue)
                return;

            CurrentMode = GameMode.Hub;
            ModeChanged?.Invoke(CurrentMode);
        }

        public void EndCombat()
        {
            if (CurrentMode != GameMode.Combat)
                return;

            _combatFocus = null;
            ReturnToExploration();
        }

        private void EnterMode(GameMode mode)
        {
            _previousMovement = _player.MovementEnabled;
            _previousLook = _player.LookEnabled;
            _previousInteraction = _interaction.InteractionEnabled;
            _previousCursorCapture = _player.CursorCaptureEnabled;

            CurrentMode = mode;
            _player.MovementEnabled = false;
            _player.LookEnabled = false;
            _player.CursorCaptureEnabled = false;
            _interaction.InteractionEnabled = false;
            ModeChanged?.Invoke(CurrentMode);
        }

        private void ReturnToExploration()
        {
            CurrentMode = GameMode.Exploration;
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
            CloseInventory();
            CloseLoot();
            CloseReturnConfirmation();
            CloseDialogue();
        }
    }
}
