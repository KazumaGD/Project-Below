using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    public sealed class ResidentCardView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _displayName;
        [SerializeField] private Text _specialization;
        [SerializeField] private Text _healthAndMorale;
        [SerializeField] private Text _satietyAndWork;
        [SerializeField] private Graphic _selection;

        internal GameObject SelectionTarget => _button.gameObject;

        internal void Bind(CharacterState state, Action<CharacterState> select) =>
            _button.onClick.AddListener(() => select(state));

        internal void Render(CharacterState state, bool selected)
        {
            var definition = state.Definition;
            _portrait.sprite = definition.Portrait;
            _portrait.enabled = _portrait.sprite != null;
            _displayName.text = definition.DisplayName;
            _specialization.text = ResidentsHud.SpecializationLabel(definition.Specialization);
            _healthAndMorale.text = $"Здоровье  {state.Stats.Health}\nМораль  {state.Stats.Morale}";
            _satietyAndWork.text = $"Сытость  {state.Stats.Satiety}\nТрудоспособность  {state.Stats.WorkCapacity}";
            _selection.enabled = selected;
        }
    }
}
