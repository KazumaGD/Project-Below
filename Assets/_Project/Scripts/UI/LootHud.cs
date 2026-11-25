using System.Collections.Generic;
using ProjectBelow.Combat;
using ProjectBelow.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    [DisallowMultipleComponent]
    public sealed class LootHud : MonoBehaviour
    {
        [SerializeField] private GameModeController _gameMode;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private GameObject _window;
        [SerializeField] private ScrollRect _containerGrid;
        [SerializeField] private ScrollRect _playerGrid;
        [SerializeField] private InventorySlotView _slotTemplate;
        [SerializeField] private ItemSlotDrag _drag;
        [SerializeField] private Text _title;
        [SerializeField] private Text _empty;
        [SerializeField] private Text _status;
        [SerializeField] private Button _takeAll;
        [SerializeField] private Button _takeStack;
        [SerializeField] private Button _close;

        private readonly List<InventorySlotView> _containerSlots = new();
        private readonly List<InventorySlotView> _playerSlots = new();
        private LootContainer _container;
        private InputAction _cancel;
        private bool _bound;
        private int _selected = -1;

        private void Start()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.TryGetComponent(out InputSystemUIInputModule uiInput))
                _cancel = uiInput.cancel?.action;
        }

        private void Update()
        {
            if (_container != null && Application.isFocused && _cancel is { enabled: true } && _cancel.WasPressedThisFrame())
                _gameMode.CloseLoot();
        }

        private void OnEnable()
        {
            if (_gameMode == null || _inventory == null || _window == null || _containerGrid == null || _playerGrid == null
                || _containerGrid.content == null || _playerGrid.content == null || _slotTemplate == null || _drag == null
                || _title == null || _empty == null || _status == null || _takeAll == null || _takeStack == null || _close == null)
            {
                Debug.LogError("Assign the loot HUD widgets and the player inventory.", this);
                enabled = false;
                return;
            }

            _bound = true;
            _gameMode.ModeChanged += OnModeChanged;
            _inventory.Changed += OnContentsChanged;
            _takeAll.onClick.AddListener(TakeAll);
            _takeStack.onClick.AddListener(TakeSelected);
            _close.onClick.AddListener(_gameMode.CloseLoot);
            OnModeChanged(_gameMode.CurrentMode);
        }

        private void OnDisable()
        {
            if (!_bound)
                return;

            _bound = false;
            _gameMode.ModeChanged -= OnModeChanged;
            _inventory.Changed -= OnContentsChanged;
            _takeAll.onClick.RemoveListener(TakeAll);
            _takeStack.onClick.RemoveListener(TakeSelected);
            _close.onClick.RemoveListener(_gameMode.CloseLoot);
            _gameMode.CloseLoot();
            OnModeChanged(GameMode.Exploration);
        }

        private void OnModeChanged(GameMode mode)
        {
            if (_container != null)
                _container.Slots.Changed -= OnContentsChanged;
            _drag.Cancel();
            _container = mode == GameMode.Loot ? _gameMode.OpenLoot : null;
            var eventSystem = EventSystem.current;
            if (_container == null)
            {
                if (eventSystem != null && eventSystem.currentSelectedGameObject is GameObject selection
                    && selection != null && selection.transform.IsChildOf(_window.transform))
                    eventSystem.SetSelectedGameObject(null);
                _window.SetActive(false);
                return;
            }

            _window.SetActive(true);
            _selected = -1;
            _container.Slots.Changed += OnContentsChanged;
            _title.text = _container.Title;
            _status.text = string.Empty;
            RefreshSlots();
            Canvas.ForceUpdateCanvases();
            _containerGrid.verticalNormalizedPosition = 1f;
            _playerGrid.verticalNormalizedPosition = 1f;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(_close.gameObject);
        }

        private void SelectContainerSlot(int index)
        {
            _selected = index;
            RefreshSlots();
        }

        private void TakeSelected()
        {
            if (_container == null || !_container.Slots.Contains(_selected) || _container.Slots[_selected].IsEmpty)
                return;

            _drag.Cancel();
            int amount = _container.Slots[_selected].Quantity;
            int taken = _container.TakeStack(_inventory, _selected);
            ShowResult(taken, taken < amount);
        }

        private void TakeAll()
        {
            if (_container == null || !_container.Slots.HasItems)
                return;

            _drag.Cancel();
            long taken = _container.TakeAll(_inventory);
            if (_container != null)
                ShowResult(taken, _container.Slots.HasItems);
        }

        private void ShowResult(long taken, bool partial)
        {
            if (_container == null)
                return;

            _status.text = taken <= 0 ? "В инвентаре нет места."
                : partial ? $"Получено {taken}. Остальное в контейнере."
                : $"Получено {taken}.";
            RefreshSlots();
        }

        private void OnContentsChanged()
        {
            if (_container == null)
                return;
            _status.text = string.Empty;
            RefreshSlots();
        }

        private void RefreshSlots()
        {
            var storage = _container.Slots;
            if (!storage.Contains(_selected) || storage[_selected].IsEmpty)
                _selected = -1;
            RenderGrid(_containerSlots, _containerGrid.content, storage, _selected, SelectContainerSlot);
            RenderGrid(_playerSlots, _playerGrid.content, _inventory.Slots, -1, null);
            _empty.gameObject.SetActive(!storage.HasItems);
            _takeAll.interactable = storage.HasItems;
            _takeStack.interactable = _selected >= 0;
        }

        private void RenderGrid(List<InventorySlotView> views, RectTransform parent, ItemSlots storage,
            int selected, System.Action<int> select)
        {
            for (int i = views.Count; i < storage.Count; i++)
            {
                var view = Instantiate(_slotTemplate, parent, false);
                view.name = $"Slot {i + 1}";
                view.Initialize(storage, i, _drag, select);
                views.Add(view);
            }
            for (int i = 0; i < views.Count; i++)
            {
                var view = views[i];
                if (i < storage.Count)
                {
                    view.Bind(storage);
                    view.Render(storage[i], i == selected);
                }
                view.gameObject.SetActive(i < storage.Count);
            }
        }
    }
}
