using System.Linq;
using ProjectBelow.Inventory;
using UnityEngine;

namespace ProjectBelow.Combat
{
    [DisallowMultipleComponent]
    public sealed class EnemyCombatant : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition _definition;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private LootContainer _defeatLootPrefab;

        private bool _defeatHandled;

        public string DisplayName => _definition.DisplayName;
        public CombatantState State { get; private set; }
        internal Transform VisualRoot => _visualRoot;
        internal int HeavyAttackDamage => _definition.HeavyAttackDamage;

        internal EnemyAction GetAction(int turnIndex) => _definition.GetAction(turnIndex);

        internal void CompleteDefeat(GameModeController gameMode)
        {
            if (_defeatHandled || State?.IsDefeated != true)
                return;

            _defeatHandled = true;
            if (_defeatLootPrefab != null && _definition.Loot.Any(entry => entry.IsValid))
            {
                var drop = Instantiate(_defeatLootPrefab, transform.position, transform.rotation);
                drop.name = $"Loot — {DisplayName}";
                drop.PrepareDrop(gameMode, $"Добыча · {DisplayName}", _definition.Loot);
            }
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (_definition == null)
            {
                Debug.LogError("Assign an enemy definition.", this);
                enabled = false;
                return;
            }

            State = new CombatantState(_definition.MaxHealth, _definition.Damage);
        }
    }
}
