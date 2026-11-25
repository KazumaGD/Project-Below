using UnityEngine;

namespace ProjectBelow.Inventory
{
    public enum ItemCategory { Food, Material }

    [CreateAssetMenu(menuName = "Project Below/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private ItemCategory _category;
        [SerializeField, Min(1)] private int _maxStackSize = 10;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public ItemCategory Category => _category;
        public int MaxStackSize => Mathf.Max(1, _maxStackSize);
    }
}
