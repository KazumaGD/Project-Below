using ProjectBelow.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuHud : MonoBehaviour
    {
        private const string VolumePreference = "masterVolume";

        [SerializeField] private ExpeditionSession _sessionPrefab;
        [SerializeField] private GameObject _menu;
        [SerializeField] private Button _newGame;
        [SerializeField] private Button _continue;
        [SerializeField] private Button _settings;
        [SerializeField] private Button _quit;
        [SerializeField] private Text _status;
        [SerializeField] private GameObject _confirmation;
        [SerializeField] private Button _replace;
        [SerializeField] private Button _cancel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private Button _volumeDown;
        [SerializeField] private Button _volumeUp;
        [SerializeField] private Button _settingsBack;
        [SerializeField] private Text _volumeLabel;

        private SaveSystem _saves;
        private SaveReadResult _slot;
        private SaveData _saved;
        private bool _opening;

        private void Start()
        {
            _saves = new SaveSystem();
            _newGame.onClick.AddListener(NewGame);
            _continue.onClick.AddListener(Continue);
            _settings.onClick.AddListener(OpenSettings);
            _quit.onClick.AddListener(Application.Quit);
            _replace.onClick.AddListener(Replace);
            _cancel.onClick.AddListener(Cancel);
            _volumeDown.onClick.AddListener(LowerVolume);
            _volumeUp.onClick.AddListener(RaiseVolume);
            _settingsBack.onClick.AddListener(CloseSettings);
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePreference, 1f));
            RefreshSlot();
            Select(_continue.interactable ? _continue : _newGame);
        }

        private void RefreshSlot()
        {
            _slot = _saves.Read(_sessionPrefab.IsValidSave, out _saved);
            _continue.interactable = _slot is SaveReadResult.Primary or SaveReadResult.Backup;
            _status.text = _slot switch
            {
                SaveReadResult.Backup => "Доступна резервная копия сохранения.",
                SaveReadResult.Invalid => "Сохранение повреждено или несовместимо.",
                _ => string.Empty
            };
        }

        private void NewGame()
        {
            if (_opening)
                return;
            RefreshSlot();
            if (_slot == SaveReadResult.Missing)
                OpenSession(null);
            else
            {
                _menu.SetActive(false);
                _confirmation.SetActive(true);
                Select(_cancel);
            }
        }

        private void Cancel()
        {
            _confirmation.SetActive(false);
            _menu.SetActive(true);
            Select(_newGame);
        }

        private void Replace()
        {
            Cancel();
            OpenSession(null);
        }

        private void Continue()
        {
            if (_opening)
                return;
            RefreshSlot();
            if (_saved != null)
                OpenSession(_saved);
        }

        private void OpenSession(SaveData data)
        {
            if (_opening)
                return;
            if (!Application.CanStreamedLevelBeLoaded(ExpeditionSession.HubScene))
            {
                _status.text = "Сцена убежища недоступна.";
                return;
            }

            _opening = true;
            var session = Instantiate(_sessionPrefab);
            bool ready = data == null ? session.TrySaveNewGame() : session.TryRestoreSave(data);
            if (ready && session.TryOpenHub())
                return;

            Destroy(session.gameObject);
            _opening = false;
            _status.text = data == null ? "Не удалось начать новую игру." : "Не удалось загрузить прогресс.";
        }

        private void OpenSettings()
        {
            if (_opening)
                return;
            _menu.SetActive(false);
            _settingsPanel.SetActive(true);
            ShowVolume();
            Select(_settingsBack);
        }

        private void CloseSettings()
        {
            _settingsPanel.SetActive(false);
            _menu.SetActive(true);
            Select(_settings);
        }

        private void LowerVolume() => ChangeVolume(-0.1f);
        private void RaiseVolume() => ChangeVolume(0.1f);

        private void ChangeVolume(float change)
        {
            AudioListener.volume = Mathf.Clamp01(AudioListener.volume + change);
            PlayerPrefs.SetFloat(VolumePreference, AudioListener.volume);
            PlayerPrefs.Save();
            ShowVolume();
        }

        private void ShowVolume()
        {
            int percent = Mathf.RoundToInt(AudioListener.volume * 100f);
            _volumeLabel.text = $"{percent}%";
            _volumeDown.interactable = percent > 0;
            _volumeUp.interactable = percent < 100;
        }

        private static void Select(Button button) => EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }
}
