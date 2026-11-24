using UnityEngine;

namespace ProjectBelow.Combat
{
    public sealed class CombatantState
    {
        public int MaxHealth { get; }
        public int Health { get; private set; }
        public int Damage { get; }
        public bool IsDefeated => Health == 0;

        internal CombatantState(int maxHealth, int damage)
        {
            MaxHealth = Mathf.Max(1, maxHealth);
            Health = MaxHealth;
            Damage = Mathf.Max(1, damage);
        }

        internal int TakeDamage(int damage)
        {
            int applied = Mathf.Clamp(damage, 0, Health);
            Health -= applied;
            return applied;
        }
    }
}
