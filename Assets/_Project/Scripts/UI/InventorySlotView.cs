using System;
using ProjectBelow.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    [DisallowMultipleComponent]
    public sealed class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _title;
        [SerializeField] private Text _quantity;
        [SerializeField] private Graphic _selection;

        private ItemSlots _storage;
        private ItemSlotDrag _drag;
        private int _slot;

        internal void Initialize(ItemSlots storage, int slot, ItemSlotDrag drag, Action<int> select = null)
        {
            _storage = storage;
            _slot = slot;
            _drag = drag;
            if (select != null)
                _button.onClick.AddListener(() => select(slot));
        }

        internal void Bind(ItemSlots storage) => _storage = storage;

        internal void Render(ItemStack stack, bool selected)
        {
            bool occupied = !stack.IsEmpty;
            _button.interactable = occupied;
            _icon.sprite = occupied ? stack.Item.Icon : null;
            _icon.enabled = _icon.sprite != null;
            _title.text = occupied ? stack.Item.DisplayName : string.Empty;
            _quantity.text = occupied ? $"×{stack.Quantity}" : string.Empty;
            _selection.enabled = occupied && selected;
        }

        public void OnBeginDrag(PointerEventData eventData) => _drag?.Begin(this, _storage, _slot, eventData);
        public void OnDrag(PointerEventData eventData) => _drag?.UpdatePosition(eventData);
        public void OnDrop(PointerEventData eventData) => _drag?.Drop(_storage, _slot, eventData);
        public void OnEndDrag(PointerEventData eventData) => _drag?.End(this);
        private void OnDisable() => _drag?.End(this);
    }
}
