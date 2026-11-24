using UnityEngine;

namespace ProjectBelow.Combat
{
    [CreateAssetMenu(menuName = "Project Below/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName = "Страж подземелья";
        [SerializeField, Min(1)] private int _maxHealth = 12;
        [SerializeField, Min(1)] private int _damage = 4;

        public string DisplayName => _displayName;
        public int MaxHealth => _maxHealth;
        public int Damage => _damage;
    }
}
