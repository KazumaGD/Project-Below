using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    [DisallowMultipleComponent]
    public sealed class ResidentsHud : MonoBehaviour
    {
        [Serializable]
        private struct StatDisplay
        {
            [SerializeField] private RectTransform _fill;
            [SerializeField] private Text _value;

            internal bool IsConfigured => _fill != null && _value != null;

            internal void Render(int value)
            {
                _fill.anchorMax = new Vector2(value / 100f, 1f);
                _value.text = $"{value} / 100";
            }
        }

        [SerializeField] private HubHud _hub;
        [SerializeField] private GameObject _home;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _open;
        [SerializeField] private Button _back;
        [SerializeField] private ScrollRect _list;
        [SerializeField] private ResidentCardView _template;
        [SerializeField] private GameObject _empty;
        [SerializeField] private GameObject _details;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _displayName;
        [SerializeField] private Text _specialization;
        [SerializeField] private Text _description;
        [SerializeField] private StatDisplay _health;
        [SerializeField] private StatDisplay _satiety;
        [SerializeField] private StatDisplay _morale;
        [SerializeField] private StatDisplay _workCapacity;
        [SerializeField] private ResidentTaskView _tasks;

        private readonly Dictionary<CharacterState, ResidentCardView> _cards = new();
        private CharacterState _selected;
        private bool _built;
        private bool _bound;

        private void Start()
        {
            if (_hub == null || _home == null || _panel == null || _open == null || _back == null || _list == null
                || _list.content == null || _template == null || _empty == null || _details == null
                || _portrait == null || _displayName == null || _specialization == null || _description == null
                || !_health.IsConfigured || !_satiety.IsConfigured || !_morale.IsConfigured || !_workCapacity.IsConfigured)
            {
                Debug.LogError("Assign the shelter resident widgets.", this);
                enabled = false;
                return;
            }

            _bound = true;
            _open.onClick.AddListener(Show);
            _back.onClick.AddListener(Back);
        }

        private void OnDestroy()
        {
            if (!_bound)
                return;

            _open.onClick.RemoveListener(Show);
            _back.onClick.RemoveListener(Back);
            foreach (var state in _cards.Keys)
                state.Changed -= OnResidentChanged;
        }

        private void Show()
        {
            var session = _hub.Session;
            if (session == null || session.IsLoading)
                return;

            _home.SetActive(false);
            _panel.SetActive(true);
            if (!_built)
                BuildCards(session.Residents);
            else if (_selected != null)
                OnResidentChanged(_selected);
            EventSystem.current?.SetSelectedGameObject(_selected != null ? _cards[_selected].SelectionTarget : _back.gameObject);
        }

        private void Back()
        {
            _panel.SetActive(false);
            _home.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(_open.gameObject);
        }

        private void BuildCards(IReadOnlyList<CharacterState> residents)
        {
            _built = true;
            foreach (var resident in residents)
            {
                var card = Instantiate(_template, _list.content, false);
                card.name = resident.Definition.DisplayName;
                card.Bind(resident, Select);
                card.Render(resident, false);
                card.gameObject.SetActive(true);
                _cards.Add(resident, card);
                resident.Changed += OnResidentChanged;
            }

            bool hasResidents = residents.Count > 0;
            _empty.SetActive(!hasResidents);
            _details.SetActive(hasResidents);
            if (hasResidents)
                Select(residents[0]);
            Canvas.ForceUpdateCanvases();
            _list.verticalNormalizedPosition = 1f;
        }

        private void Select(CharacterState resident)
        {
            if (_selected != null)
                _cards[_selected].Render(_selected, false);
            _selected = resident;
            OnResidentChanged(resident);
        }

        private void OnResidentChanged(CharacterState resident)
        {
            bool selected = resident == _selected;
            _cards[resident].Render(resident, selected);
            if (!selected)
                return;

            var definition = resident.Definition;
            _portrait.sprite = definition.Portrait;
            _portrait.enabled = _portrait.sprite != null;
            _displayName.text = definition.DisplayName;
            _specialization.text = SpecializationLabel(definition.Specialization);
            _description.text = definition.Description;
            _health.Render(resident.Stats.Health);
            _satiety.Render(resident.Stats.Satiety);
            _morale.Render(resident.Stats.Morale);
            _workCapacity.Render(resident.Stats.WorkCapacity);
            _tasks?.Render(_hub.Session, resident);
        }

        internal static string SpecializationLabel(CharacterSpecialization specialization) => specialization switch
        {
            CharacterSpecialization.Craftsperson => "Мастер",
            CharacterSpecialization.Agronomist => "Агроном",
            CharacterSpecialization.Builder => "Строитель",
            _ => string.Empty
        };
    }
}
