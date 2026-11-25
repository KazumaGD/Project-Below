using System.Collections.Generic;
using ProjectBelow.Combat;
using ProjectBelow.Hub;
using ProjectBelow.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    [DisallowMultipleComponent]
    public sealed class DialogueHud : MonoBehaviour
    {
        [SerializeField] private HubHud _hub;
        [SerializeField] private GameModeController _mode;
        [SerializeField] private CanvasGroup _hubCanvas;
        [SerializeField] private DialogueDefinition _demo;
        [SerializeField] private Button _launch;
        [SerializeField] private GameObject _overlay;
        [SerializeField] private RectTransform _portraitRow;
        [SerializeField] private DialoguePortraitView _portraitTemplate;
        [SerializeField] private Text _speaker;
        [SerializeField] private Text _line;
        [SerializeField] private Button _continue;
        [SerializeField] private ScrollRect _choices;
        [SerializeField] private Button _choiceTemplate;

        private readonly List<DialoguePortraitView> _portraits = new();
        private readonly List<Button> _choiceButtons = new();
        private DialogueGraph _graph;
        private DialogueConversation _conversation;
        private GameObject _previousSelection;
        private bool _wasInteractable;
        private bool _blockedRaycasts;
        private bool _bound;

        private void OnEnable()
        {
            if (_hub == null || _mode == null || _hubCanvas == null || _demo == null || _launch == null || _overlay == null
                || _portraitRow == null || _portraitTemplate == null || _speaker == null || _line == null || _continue == null
                || _choices == null || _choices.content == null || _choiceTemplate == null)
            {
                Debug.LogError("Assign the dialogue and Hub UI references.", this);
                enabled = false;
                return;
            }

            _bound = true;
            _launch.onClick.AddListener(StartDemo);
            _mode.ModeChanged += OnModeChanged;
        }

        private void OnDisable()
        {
            if (!_bound)
                return;

            _bound = false;
            if (_launch != null)
                _launch.onClick.RemoveListener(StartDemo);
            if (_mode != null)
                _mode.ModeChanged -= OnModeChanged;
            RestoreHub();
        }

        private void StartDemo() => TryStart(_demo);

        public bool TryStart(DialogueDefinition definition)
        {
            if (!isActiveAndEnabled || _conversation != null || _mode.CurrentMode != GameMode.Hub
                || _hub.Session == null || !_hub.Session.InShelter || definition == null)
                return false;
            if (!definition.TryBuild(out var graph, out string error))
            {
                Debug.LogWarning($"Cannot start dialogue '{definition.name}'. {error}", definition);
                return false;
            }
            if (!_mode.TryOpenDialogue())
                return false;

            _wasInteractable = _hubCanvas.interactable;
            _blockedRaycasts = _hubCanvas.blocksRaycasts;
            _previousSelection = EventSystem.current?.currentSelectedGameObject;
            _hubCanvas.interactable = false;
            _hubCanvas.blocksRaycasts = false;
            _graph = graph;
            _conversation = new DialogueConversation(graph);
            _overlay.SetActive(true);
            Render();
            return true;
        }

        private void Render()
        {
            var conversation = _conversation;
            var node = conversation.Current;
            if (node == null)
            {
                RestoreHub();
                return;
            }

            _speaker.text = node.Speaker.DisplayName;
            _line.text = node.Text;
            for (int i = 0; i < _graph.Participants.Count; i++)
            {
                if (i == _portraits.Count)
                    _portraits.Add(Instantiate(_portraitTemplate, _portraitRow, false));
                var character = _graph.Participants[i];
                _portraits[i].name = character.DisplayName;
                _portraits[i].Render(character, character == node.Speaker);
                _portraits[i].gameObject.SetActive(true);
            }
            for (int i = _graph.Participants.Count; i < _portraits.Count; i++)
                _portraits[i].gameObject.SetActive(false);

            int step = conversation.Step;
            bool hasChoices = node.Choices.Count > 0;
            _continue.gameObject.SetActive(!hasChoices);
            _continue.onClick.RemoveAllListeners();
            if (!hasChoices)
            {
                _continue.GetComponentInChildren<Text>(true).text = string.IsNullOrEmpty(node.NextNodeId)
                    ? "Завершить диалог" : "Продолжить";
                _continue.onClick.AddListener(() =>
                {
                    if (_conversation == conversation && conversation.TryContinue(step))
                        Render();
                });
            }

            _choices.gameObject.SetActive(hasChoices);
            for (int i = 0; i < node.Choices.Count; i++)
            {
                if (i == _choiceButtons.Count)
                    _choiceButtons.Add(Instantiate(_choiceTemplate, _choices.content, false));
                var button = _choiceButtons[i];
                int choice = i;
                button.name = $"Choice {i + 1}";
                button.GetComponentInChildren<Text>(true).text = node.Choices[i].Text;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Choose(conversation, step, choice));
                button.gameObject.SetActive(true);
            }
            for (int i = node.Choices.Count; i < _choiceButtons.Count; i++)
            {
                _choiceButtons[i].onClick.RemoveAllListeners();
                _choiceButtons[i].gameObject.SetActive(false);
            }

            if (hasChoices)
            {
                Canvas.ForceUpdateCanvases();
                _choices.verticalNormalizedPosition = 1f;
            }
            EventSystem.current?.SetSelectedGameObject(hasChoices ? _choiceButtons[0].gameObject : _continue.gameObject);
        }

        private void Choose(DialogueConversation conversation, int step, int index)
        {
            if (_conversation != conversation || !conversation.TryChoose(step, index, out string flagId))
                return;

            if (!string.IsNullOrEmpty(flagId))
                _hub.Session.SetStoryFlag(flagId);
            Render();
        }

        private void OnModeChanged(GameMode mode)
        {
            if (mode != GameMode.Dialogue)
                RestoreHub();
        }

        private void RestoreHub()
        {
            if (_conversation == null)
                return;

            _conversation = null;
            _graph = null;
            if (_overlay != null)
                _overlay.SetActive(false);
            if (_hubCanvas != null)
            {
                _hubCanvas.interactable = _wasInteractable;
                _hubCanvas.blocksRaycasts = _blockedRaycasts;
            }
            if (_mode != null)
                _mode.CloseDialogue();
            EventSystem.current?.SetSelectedGameObject(_previousSelection != null && _previousSelection.activeInHierarchy
                ? _previousSelection : null);
            _previousSelection = null;
        }
    }
}
