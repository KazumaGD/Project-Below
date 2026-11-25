using ProjectBelow.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    [DefaultExecutionOrder(10), DisallowMultipleComponent]
    public sealed class HubSaveHud : MonoBehaviour
    {
        [SerializeField] private HubHud _hub;
        [SerializeField] private GameObject _home;
        [SerializeField] private Text _status;
        [SerializeField] private Button _save;
        [SerializeField] private Button _menu;
        [SerializeField] private GameObject _confirmation;
        [SerializeField] private Button _leave;
        [SerializeField] private Button _cancel;

        private ExpeditionSession _session;

        private void Start()
        {
            _session = _hub.Session;
            _save.onClick.AddListener(Save);
            _menu.onClick.AddListener(OpenConfirmation);
            _leave.onClick.AddListener(Leave);
            _cancel.onClick.AddListener(Cancel);
            if (_session != null)
            {
                _session.SaveMessageChanged += ShowStatus;
                if (!string.IsNullOrEmpty(_session.SaveMessage))
                    ShowStatus(_session.SaveMessage);
            }
        }

        private void OnDestroy()
        {
            if (_session != null)
                _session.SaveMessageChanged -= ShowStatus;
        }

        private void Save() => _session?.TrySaveProgress();
        private void ShowStatus(string message) => _status.text = message;

        private void OpenConfirmation()
        {
            if (_session == null || !_session.InShelter)
                return;
            _home.SetActive(false);
            _confirmation.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(_cancel.gameObject);
        }

        private void Cancel()
        {
            _confirmation.SetActive(false);
            _home.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(_menu.gameObject);
        }

        private void Leave()
        {
            if (_session.TryReturnToMainMenu())
            {
                _leave.interactable = false;
                _cancel.interactable = false;
            }
            else
            {
                Cancel();
                ShowStatus("Не удалось открыть главное меню.");
            }
        }
    }
}
