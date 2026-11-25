using System;
using System.Collections.Generic;
using ProjectBelow.Combat;
using ProjectBelow.Exploration;
using UnityEngine;

namespace ProjectBelow.Inventory
{
    [Serializable]
    public struct LootEntry
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField, Min(1)] private int _quantity;

        public ItemDefinition Item => _item;
        public int Quantity => _quantity;
        internal bool IsValid => _item != null && !string.IsNullOrEmpty(_item.Id) && _quantity > 0;
    }

    [DisallowMultipleComponent]
    public sealed class LootContainer : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameModeController _gameMode;
        [SerializeField] private string _title = "Тайник";
        [SerializeField] private LootEntry[] _initialContents = Array.Empty<LootEntry>();

        internal ItemSlots Slots { get; } = new(expandable: true);
        private string _prompt;

        public string Title => _title;
        public bool CanInteract => isActiveAndEnabled && _gameMode != null;
        public string InteractionPrompt => _prompt;

        private void Awake() => SetContents(_initialContents);

        internal void PrepareDrop(GameModeController gameMode, string title, IEnumerable<LootEntry> contents)
        {
            _gameMode = gameMode;
            _title = title;
            SetContents(contents);
        }

        public void Interact()
        {
            if (CanInteract)
                _gameMode.TryOpenLoot(this);
        }

        private void OnDisable()
        {
            if (_gameMode != null && _gameMode.OpenLoot == this)
                _gameMode.CloseLoot();
        }

        internal int TakeStack(PlayerInventory inventory, int index)
        {
            if (!IsOpen() || inventory == null || !Slots.Contains(index))
                return 0;

            int taken = Transfer(inventory, index);
            if (taken > 0)
            {
                Slots.NotifyChanged(inventory.Slots);
            }
            return taken;
        }

        internal long TakeAll(PlayerInventory inventory)
        {
            if (!IsOpen() || inventory == null)
                return 0;

            long taken = 0;
            for (int i = 0; i < Slots.Count; i++)
                taken += Transfer(inventory, i);
            if (taken > 0)
            {
                Slots.NotifyChanged(inventory.Slots);
            }
            return taken;
        }

        private bool IsOpen() => CanInteract && _gameMode.CurrentMode == GameMode.Loot && _gameMode.OpenLoot == this;

        private int Transfer(PlayerInventory inventory, int index)
        {
            ItemStack stack = Slots[index];
            int taken = inventory.AddToSlots(stack.Item, stack.Quantity);
            if (taken <= 0)
                return 0;

            int remaining = stack.Quantity - taken;
            Slots[index] = remaining > 0 ? new ItemStack(stack.Item, remaining) : default;
            return taken;
        }

        private void SetContents(IEnumerable<LootEntry> contents)
        {
            _prompt = $"Обыскать  {_title}";
            Slots.Reset();

            foreach (LootEntry entry in contents ?? Array.Empty<LootEntry>())
            {
                if (!entry.IsValid)
                    continue;
                int remaining = entry.Quantity;
                while (remaining > 0)
                {
                    int size = Mathf.Min(remaining, entry.Item.MaxStackSize);
                    Slots.Append(new ItemStack(entry.Item, size));
                    remaining -= size;
                }
            }
            Slots.EnsureEmptySlot();
        }
    }
}
