using System;
using UnityEngine;

namespace ProjectBelow.Inventory
{
    public readonly struct ItemStack
    {
        public ItemDefinition Item { get; }
        public int Quantity { get; }
        public bool IsEmpty => Item == null;

        internal ItemStack(ItemDefinition item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _capacity = 12;
        internal ItemSlots Slots { get; private set; }

        public event Action Changed;
        public event Action<ItemDefinition, int> PickupReported;
        public int Capacity => Slots.Count;

        private void Awake()
        {
            Slots = new ItemSlots(Mathf.Max(1, _capacity));
            Slots.Changed += () => Changed?.Invoke();
        }

        public ItemStack GetSlot(int index) => Slots[index];

        public int Add(ItemDefinition item, int quantity) => ReportChange(AddToSlots(item, quantity));

        internal int AddToSlots(ItemDefinition item, int quantity)
        {
            if (!IsDefined(item) || quantity <= 0)
                return 0;

            int remaining = quantity;
            for (int i = 0; i < Capacity && remaining > 0; i++)
            {
                ItemStack slot = Slots[i];
                if (!ItemSlots.SameItem(slot, item))
                    continue;

                int added = Mathf.Min(remaining, Mathf.Max(0, slot.Item.MaxStackSize - slot.Quantity));
                if (added > 0)
                    Slots[i] = new ItemStack(slot.Item, slot.Quantity + added);
                remaining -= added;
            }

            for (int i = 0; i < Capacity && remaining > 0; i++)
            {
                if (!Slots[i].IsEmpty)
                    continue;

                int added = Mathf.Min(remaining, item.MaxStackSize);
                Slots[i] = new ItemStack(item, added);
                remaining -= added;
            }

            return quantity - remaining;
        }

        public int Remove(ItemDefinition item, int quantity)
        {
            if (!IsDefined(item) || quantity <= 0)
                return 0;

            int remaining = quantity;
            for (int i = Capacity - 1; i >= 0 && remaining > 0; i--)
            {
                ItemStack slot = Slots[i];
                if (!ItemSlots.SameItem(slot, item))
                    continue;

                int removed = Mathf.Min(remaining, slot.Quantity);
                int left = slot.Quantity - removed;
                Slots[i] = left > 0 ? new ItemStack(slot.Item, left) : default;
                remaining -= removed;
            }

            return ReportChange(quantity - remaining);
        }

        public long Count(ItemDefinition item)
        {
            if (!IsDefined(item))
                return 0;

            long quantity = 0;
            for (int i = 0; i < Capacity; i++)
                if (ItemSlots.SameItem(Slots[i], item))
                    quantity += Slots[i].Quantity;
            return quantity;
        }

        internal bool ClearSlots()
        {
            if (!Slots.HasItems)
                return false;

            for (int i = 0; i < Capacity; i++)
                Slots[i] = default;
            return true;
        }

        internal void ReportPickup(ItemDefinition item, int quantity)
        {
            ReportChange(quantity);
            PickupReported?.Invoke(item, quantity);
        }

        private int ReportChange(int quantity)
        {
            if (quantity > 0)
                NotifyChanged();
            return quantity;
        }

        internal void NotifyChanged() => Slots.NotifyChanged();

        private static bool IsDefined(ItemDefinition item) => item != null && !string.IsNullOrEmpty(item.Id);

    }
}
