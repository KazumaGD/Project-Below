using ProjectBelow.Combat;
using ProjectBelow.Exploration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    [DisallowMultipleComponent]
    public sealed class ReturnPromptHud : MonoBehaviour
    {
        [SerializeField] private GameModeController _mode;
        [SerializeField] private ExpeditionFlow _expedition;
        [SerializeField] private GameObject _window;
        [SerializeField] private Button _confirm;
        [SerializeField] private Button _cancel;
        [SerializeField] private Text _status;

        private InputAction _cancelAction;
        private bool _bound;

        private void Start()
        {
            if (EventSystem.current != null && EventSystem.current.TryGetComponent(out InputSystemUIInputModule uiInput))
                _cancelAction = uiInput.cancel?.action;
        }

        private void Update()
        {
            if (_window.activeSelf && Application.isFocused && _cancelAction is { enabled: true }
                && _cancelAction.WasPressedThisFrame())
                Cancel();
        }

        private void OnEnable()
        {
            if (_mode == null || _expedition == null || _window == null || _confirm == null || _cancel == null || _status == null)
            {
                Debug.LogError("Assign the expedition and the return confirmation widgets.", this);
                enabled = false;
                return;
            }
            _bound = true;
            _mode.ModeChanged += Refresh;
            _confirm.onClick.AddListener(Confirm);
            _cancel.onClick.AddListener(Cancel);
            Refresh(_mode.CurrentMode);
        }

        private void OnDisable()
        {
            if (!_bound)
                return;

            _bound = false;
            _mode.ModeChanged -= Refresh;
            _confirm.onClick.RemoveListener(Confirm);
            _cancel.onClick.RemoveListener(Cancel);
            Cancel();
            Refresh(GameMode.Exploration);
        }

        private void Confirm()
        {
            if (_expedition.TryReturn())
            {
                _confirm.interactable = false;
                _cancel.interactable = false;
            }
            else if (_expedition.Session != null && !_expedition.Session.IsLoading && _window.activeSelf)
                _status.text = "Не удалось открыть убежище.";
        }

        private void Cancel()
        {
            if (_expedition.Session == null || !_expedition.Session.IsLoading)
                _mode.CloseReturnConfirmation();
        }

        private void Refresh(GameMode mode)
        {
            bool open = mode == GameMode.ReturnConfirmation;
            EventSystem system = EventSystem.current;
            if (!open && system != null && system.currentSelectedGameObject is GameObject selected
                && selected != null && selected.transform.IsChildOf(_window.transform))
                system.SetSelectedGameObject(null);

            _window.SetActive(open);
            if (!open)
                return;

            _status.text = string.Empty;
            _confirm.interactable = true;
            _cancel.interactable = true;
            if (system != null)
                system.SetSelectedGameObject(_cancel.gameObject);
        }
    }
}
