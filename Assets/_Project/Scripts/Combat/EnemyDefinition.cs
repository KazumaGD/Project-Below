using System;
using System.Collections.Generic;
using ProjectBelow.Inventory;
using UnityEngine;

namespace ProjectBelow.Combat
{
    public enum EnemyAction { Attack, Prepare, HeavyAttack, Recover }

    [CreateAssetMenu(menuName = "Project Below/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Страж подземелья";
        [SerializeField, Min(1)] private int _maxHealth = 12;
        [SerializeField, Min(1)] private int _damage = 4;
        [SerializeField, Min(1)] private int _heavyAttackDamage = 12;
        [SerializeField] private EnemyAction[] _actionSequence = { EnemyAction.Attack };
        [SerializeField] private LootEntry[] _loot = Array.Empty<LootEntry>();

        public string DisplayName => _displayName;
        public int MaxHealth => _maxHealth;
        public int Damage => _damage;
        public int HeavyAttackDamage => Mathf.Max(1, _heavyAttackDamage);
        internal IReadOnlyList<LootEntry> Loot => _loot ?? Array.Empty<LootEntry>();

        internal EnemyAction GetAction(int turnIndex)
        {
            if (_actionSequence == null || _actionSequence.Length == 0)
                return EnemyAction.Attack;

            return _actionSequence[turnIndex % _actionSequence.Length];
        }
    }
}
