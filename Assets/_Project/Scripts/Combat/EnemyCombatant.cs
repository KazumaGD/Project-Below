using UnityEngine;

namespace ProjectBelow.Combat
{
    [DisallowMultipleComponent]
    public sealed class EnemyCombatant : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition _definition;

        public string DisplayName => _definition.DisplayName;
        public CombatantState State { get; private set; }

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
