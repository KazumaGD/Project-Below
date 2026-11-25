using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectBelow.Inventory
{
    internal sealed class ItemSlots
    {
        private readonly List<ItemStack> _items = new();
        private readonly bool _expandable;

        internal event Action Changed;
        internal int Count => _items.Count;
        internal int Revision { get; private set; }
        internal ItemStack this[int index] { get => _items[index]; set => _items[index] = value; }

        internal ItemSlots(int capacity = 0, bool expandable = false)
        {
            _expandable = expandable;
            for (int i = 0; i < capacity; i++)
                _items.Add(default);
        }

        internal void Reset() => _items.Clear();
        internal void Append(ItemStack stack) => _items.Add(stack);

        internal bool Contains(int index) => index >= 0 && index < Count;
        internal bool HasItems => _items.Exists(stack => !stack.IsEmpty);

        internal void NotifyChanged(ItemSlots other = null)
        {
            CompleteWrite();
            if (other != null && other != this)
                other.CompleteWrite();
            Changed?.Invoke();
            if (other != null && other != this)
                other.Changed?.Invoke();
        }

        internal int MoveTo(int index, ItemSlots target, int targetIndex, int revision)
        {
            if (target == null || Revision != revision || !Contains(index) || !target.Contains(targetIndex)
                || (target == this && index == targetIndex))
                return 0;

            ItemStack source = this[index];
            ItemStack destination = target[targetIndex];
            if (source.IsEmpty)
                return 0;

            int moved = source.Quantity;
            ItemStack sourceAfter = destination;
            ItemStack targetAfter = source;
            if (destination.IsEmpty || !SameItem(source, destination.Item))
            {
                if (source.Quantity > source.Item.MaxStackSize
                    || (!destination.IsEmpty && destination.Quantity > destination.Item.MaxStackSize))
                    return 0;
            }
            else
            {
                moved = Mathf.Min(source.Quantity, Mathf.Max(0, destination.Item.MaxStackSize - destination.Quantity));
                if (moved <= 0)
                    return 0;
                targetAfter = new ItemStack(destination.Item, destination.Quantity + moved);
                sourceAfter = moved == source.Quantity ? default : new ItemStack(source.Item, source.Quantity - moved);
            }

            this[index] = sourceAfter;
            target[targetIndex] = targetAfter;
            // Commit both stores before publishing either change.
            NotifyChanged(target);
            return moved;
        }

        private void CompleteWrite()
        {
            EnsureEmptySlot();
            Revision++;
        }

        internal void EnsureEmptySlot()
        {
            if (_expandable && !_items.Exists(stack => stack.IsEmpty))
                _items.Add(default);
        }

        internal static bool SameItem(ItemStack stack, ItemDefinition item) =>
            !stack.IsEmpty && item != null && string.Equals(stack.Item.Id, item.Id, StringComparison.Ordinal);
    }
}
