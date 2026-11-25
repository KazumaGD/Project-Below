using ProjectBelow.Combat;
using ProjectBelow.Core;
using ProjectBelow.Inventory;
using UnityEngine;

namespace ProjectBelow.Exploration
{
    [DefaultExecutionOrder(-20), DisallowMultipleComponent]
    [RequireComponent(typeof(GameModeController), typeof(CombatController))]
    public sealed class ExpeditionFlow : MonoBehaviour
    {
        [SerializeField] private ExpeditionSession _sessionPrefab;

        private GameModeController _mode;
        private CombatController _combat;
        private PlayerInventory _inventory;
        public ExpeditionSession Session { get; private set; }

        private void Awake()
        {
            _mode = GetComponent<GameModeController>();
            _combat = GetComponent<CombatController>();
            if (_mode.PlayerRoot != null)
                _inventory = _mode.PlayerRoot.GetComponent<PlayerInventory>();
            if (_sessionPrefab == null || _inventory == null)
            {
                Debug.LogError("Assign the expedition session prefab and a player inventory.", this);
                enabled = false;
                return;
            }

            Session = ExpeditionSession.Resolve(_sessionPrefab);
            Session.OpenExpedition();
        }

        private void OnEnable() => _combat.Changed += OnCombatChanged;

        private void OnDisable() => _combat.Changed -= OnCombatChanged;

        private void OnCombatChanged(CombatEvent combatEvent, int damage, bool blocked)
        {
            if (combatEvent == CombatEvent.Defeat)
                TryReturnDefeated();
        }

        public bool TryReturn() => isActiveAndEnabled && _mode.CurrentMode == GameMode.ReturnConfirmation
            && Session.TryFinish(_inventory, _combat.Player.Health, true);

        public bool TryReturnDefeated() => isActiveAndEnabled && _mode.CurrentMode == GameMode.Combat
            && _combat.CanReturnToHub && Session.TryFinish(_inventory, _combat.Player.Health, false);
    }
}
