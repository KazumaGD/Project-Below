using System.Collections;
using ProjectBelow.Combat;
using ProjectBelow.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    [DisallowMultipleComponent]
    public sealed class InventoryHud : MonoBehaviour
    {
        [SerializeField] private GameModeController _gameMode;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private GameObject _window;
        [SerializeField] private ScrollRect _grid;
        [SerializeField] private InventorySlotView _slotTemplate;
        [SerializeField] private Button _close;
        [SerializeField] private Text _usage;
        [SerializeField] private Text _itemName;
        [SerializeField] private Text _itemDescription;
        [SerializeField] private Text _itemCategory;
        [SerializeField] private GameObject _notice;
        [SerializeField] private Text _noticeText;
        [SerializeField] private ItemSlotDrag _drag;
        [SerializeField] private WorldItemDropper _dropper;
        [SerializeField] private Button _drop;
        [SerializeField] private Text _dropStatus;
        [SerializeField, Min(0.25f)] private float _noticeDuration = 2.75f;

        private InventorySlotView[] _slots;
        private int _selected = -1;
        private Coroutine _noticeTimer;
        private bool _bound;

        private void OnEnable()
        {
            if (_gameMode == null || _inventory == null || _window == null || _grid == null || _grid.content == null
                || _slotTemplate == null || _close == null || _usage == null || _itemName == null
                || _itemDescription == null || _itemCategory == null || _notice == null || _noticeText == null
                || _drag == null || _dropper == null || _drop == null || _dropStatus == null)
            {
                Debug.LogError("Assign the inventory model, game modes and inventory HUD widgets.", this);
                enabled = false;
                return;
            }

            _bound = true;
            _gameMode.ModeChanged += OnModeChanged;
            _inventory.Changed += Refresh;
            _inventory.PickupReported += ShowPickup;
            _close.onClick.AddListener(_gameMode.CloseInventory);
            _drop.onClick.AddListener(DropSelected);
            OnModeChanged(_gameMode.CurrentMode);
        }

        private void OnDisable()
        {
            if (!_bound)
                return;
            _bound = false;
            _gameMode.ModeChanged -= OnModeChanged;
            _inventory.Changed -= Refresh;
            _inventory.PickupReported -= ShowPickup;
            _close.onClick.RemoveListener(_gameMode.CloseInventory);
            _drop.onClick.RemoveListener(DropSelected);
            _gameMode.CloseInventory();
            HideNotice();
            _window.SetActive(false);
        }

        private void OnModeChanged(GameMode mode)
        {
            bool open = mode == GameMode.Inventory;
            var eventSystem = EventSystem.current;
            if (!open && eventSystem != null && eventSystem.currentSelectedGameObject is GameObject focused
                && focused != null && focused.transform.IsChildOf(_window.transform))
                eventSystem.SetSelectedGameObject(null);
            _window.SetActive(open);
            if (mode != GameMode.Exploration)
                HideNotice();
            if (!open)
                return;

            _dropStatus.text = string.Empty;
            if (_slots == null)
            {
                _slots = new InventorySlotView[_inventory.Capacity];
                for (int i = 0; i < _slots.Length; i++)
                {
                    var slot = Instantiate(_slotTemplate, _grid.content, false);
                    slot.name = $"Slot {i + 1}";
                    slot.Initialize(_inventory.Slots, i, _drag, SelectSlot);
                    slot.gameObject.SetActive(true);
                    _slots[i] = slot;
                }
                Canvas.ForceUpdateCanvases();
                _grid.verticalNormalizedPosition = 1f;
            }

            Refresh();
        }

        private void SelectSlot(int index)
        {
            _selected = index;
            _dropStatus.text = string.Empty;
            Refresh();
        }

        private void DropSelected()
        {
            if (_selected < 0)
                return;

            _drag.Cancel();
            _dropStatus.text = _dropper.TryDrop(_selected) ? string.Empty : "Впереди нет свободного места.";
        }

        private void Refresh()
        {
            if (!_window.activeSelf || _slots == null)
                return;

            int used = 0;
            int firstOccupied = -1;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_inventory.GetSlot(i).IsEmpty)
                    continue;
                used++;
                if (firstOccupied < 0)
                    firstOccupied = i;
            }

            if (_selected < 0 || _inventory.GetSlot(_selected).IsEmpty)
                _selected = firstOccupied;

            for (int i = 0; i < _slots.Length; i++)
                _slots[i].Render(_inventory.GetSlot(i), i == _selected);

            _usage.text = $"{used} / {_slots.Length} ячеек";
            var item = _selected >= 0 ? _inventory.GetSlot(_selected).Item : null;
            _drop.interactable = item != null;
            _itemName.text = item != null ? item.DisplayName : "Инвентарь пуст";
            _itemDescription.text = item != null ? item.Description : string.Empty;
            _itemCategory.text = item == null ? string.Empty : item.Category switch
            {
                ItemCategory.Food => "Провизия",
                ItemCategory.Material => "Материал",
                _ => string.Empty
            };
        }

        private void ShowPickup(ItemDefinition item, int quantity)
        {
            if (_gameMode.CurrentMode != GameMode.Exploration)
                return;

            HideNotice();
            _noticeText.text = quantity > 0 ? $"Получено  +{quantity}  {item.DisplayName}" : "Инвентарь заполнен";
            _notice.SetActive(true);
            _noticeTimer = StartCoroutine(ExpireNotice());
        }

        private IEnumerator ExpireNotice()
        {
            yield return new WaitForSecondsRealtime(_noticeDuration);
            _noticeTimer = null;
            _notice.SetActive(false);
        }

        private void HideNotice()
        {
            if (_noticeTimer != null)
                StopCoroutine(_noticeTimer);
            _noticeTimer = null;
            _notice.SetActive(false);
        }
    }
}
