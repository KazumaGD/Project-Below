using ProjectBelow.Exploration;
using UnityEngine;

namespace ProjectBelow.Inventory
{
    [DisallowMultipleComponent]
    public sealed class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField, Min(1)] private int _quantity = 1;
        [SerializeField] private PlayerInventory _inventory;

        private bool _dropped;
        public int Quantity => _quantity;
        public bool CanInteract => _quantity > 0 && _inventory != null && isActiveAndEnabled;
        public string InteractionPrompt { get; private set; }

        internal void ConfigureDropped(ItemStack stack, PlayerInventory inventory)
        {
            _item = stack.Item;
            _quantity = stack.Quantity;
            _inventory = inventory;
            _dropped = true;
        }

        private void Awake()
        {
            if (_item == null || string.IsNullOrEmpty(_item.Id) || _inventory == null)
            {
                Debug.LogError("Assign a saved item definition and the player's inventory.", this);
                enabled = false;
                return;
            }

            UpdatePrompt();
        }

        public void Interact()
        {
            if (!CanInteract)
                return;

            // Commit both counts before notifying listeners.
            int added = _inventory.AddToSlots(_item, _quantity);
            _quantity -= added;
            if (_quantity == 0)
            {
                gameObject.SetActive(false);
                if (_dropped)
                    Destroy(gameObject);
            }
            else if (added > 0)
                UpdatePrompt();
            _inventory.ReportPickup(_item, added);
        }

        private void UpdatePrompt() => InteractionPrompt = $"Подобрать  {_item.DisplayName}  ×{_quantity}";
    }
}
