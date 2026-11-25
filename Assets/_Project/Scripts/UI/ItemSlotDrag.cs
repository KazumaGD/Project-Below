using ProjectBelow.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    public sealed class ItemSlotDrag : MonoBehaviour
    {
        [SerializeField] private Image _preview;
        [SerializeField] private Vector2 _offset = new(24f, -24f);

        private InventorySlotView _origin;
        private ItemSlots _source;
        private int _slot;
        private int _revision;
        private int _pointerId;

        internal void Begin(InventorySlotView origin, ItemSlots source, int slot, PointerEventData pointer)
        {
            if (_origin != null || !isActiveAndEnabled || !source.Contains(slot) || source[slot].IsEmpty
                || pointer.button != PointerEventData.InputButton.Left)
                return;

            _origin = origin;
            _source = source;
            _slot = slot;
            _revision = source.Revision;
            _pointerId = pointer.pointerId;
            _preview.sprite = source[slot].Item.Icon;
            _preview.gameObject.SetActive(_preview.sprite != null);
            UpdatePosition(pointer);
        }

        internal void UpdatePosition(PointerEventData pointer)
        {
            if (_origin == null || pointer.pointerId != _pointerId)
                return;

            var rect = (RectTransform)_preview.transform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, pointer.position, pointer.pressEventCamera, out Vector2 local))
                _preview.rectTransform.anchoredPosition = local + _offset;
        }

        internal void Drop(ItemSlots destination, int slot, PointerEventData pointer)
        {
            if (_origin == null || pointer.pointerId != _pointerId || pointer.button != PointerEventData.InputButton.Left)
                return;

            ItemSlots source = _source;
            int originSlot = _slot;
            int revision = _revision;
            Cancel();
            source.MoveTo(originSlot, destination, slot, revision);
        }

        internal void End(InventorySlotView origin)
        {
            if (_origin == origin)
                Cancel();
        }

        internal void Cancel()
        {
            _origin = null;
            _source = null;
            if (_preview != null)
                _preview.gameObject.SetActive(false);
        }

        private void OnDisable() => Cancel();
    }
}
