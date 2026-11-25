using ProjectBelow.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectBelow.Inventory
{
    [DefaultExecutionOrder(-10), DisallowMultipleComponent, RequireComponent(typeof(GameModeController))]
    public sealed class InventoryToggle : MonoBehaviour
    {
        private GameModeController _gameMode;
        private InputAction _toggle;

        private void Start()
        {
            _gameMode = GetComponent<GameModeController>();
            var input = _gameMode.PlayerRoot != null ? _gameMode.PlayerRoot.GetComponent<PlayerInput>() : null;
            _toggle = input?.actions?.FindAction("Player/ToggleInventory");
            if (_toggle == null)
            {
                Debug.LogError("Assign the Player/ToggleInventory input action.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_toggle.enabled || !_toggle.WasPressedThisFrame() || !Application.isFocused)
                return;

            if (_gameMode.CurrentMode == GameMode.Inventory)
                _gameMode.CloseInventory();
            else
                _gameMode.TryOpenInventory();
        }

        private void OnDisable()
        {
            if (_gameMode != null)
                _gameMode.CloseInventory();
        }
    }
}
