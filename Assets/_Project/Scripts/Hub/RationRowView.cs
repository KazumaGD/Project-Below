using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    public sealed class RationRowView : MonoBehaviour
    {
        private static readonly Color SelectedColor = new(0.48f, 0.34f, 0.19f);
        private static readonly Color LossColor = new(0.98f, 0.60f, 0.51f);
        private static readonly Color GainColor = new(0.71f, 0.85f, 0.65f);
        private static readonly Color NeutralColor = new(0.90f, 0.85f, 0.76f);

        [SerializeField] private Image _portrait;
        [SerializeField] private Text _displayName;
        [SerializeField] private Text _specialization;
        [SerializeField] private Text _workCapacity;
        [SerializeField] private Text _health;
        [SerializeField] private Text _satiety;
        [SerializeField] private Text _morale;
        [SerializeField] private Button _none;
        [SerializeField] private Button _half;
        [SerializeField] private Button _full;

        private Color _normalColor;

        internal void Bind(CharacterDefinition definition, RationRules rules, Action<FoodRation> select)
        {
            _displayName.text = definition.DisplayName;
            _specialization.text = ResidentsHud.SpecializationLabel(definition.Specialization);
            _portrait.sprite = definition.Portrait;
            _portrait.enabled = _portrait.sprite != null;
            _normalColor = _none.targetGraphic.color;
            _half.GetComponentInChildren<Text>().text = $"½ порции · {rules.GetCost(FoodRation.Half)}";
            _full.GetComponentInChildren<Text>().text = $"Полная порция · {rules.GetCost(FoodRation.Full)}";
            _none.onClick.AddListener(() => select(FoodRation.None));
            _half.onClick.AddListener(() => select(FoodRation.Half));
            _full.onClick.AddListener(() => select(FoodRation.Full));
        }

        internal void Render(CharacterStats current, CharacterStats next, FoodRation ration)
        {
            _workCapacity.text = $"Трудоспособность {current.WorkCapacity}";
            ShowStat(_satiety, "Сытость", current.Satiety, next.Satiety);
            ShowStat(_health, "Здоровье", current.Health, next.Health);
            ShowStat(_morale, "Мораль", current.Morale, next.Morale);
            _none.targetGraphic.color = ration == FoodRation.None ? SelectedColor : _normalColor;
            _half.targetGraphic.color = ration == FoodRation.Half ? SelectedColor : _normalColor;
            _full.targetGraphic.color = ration == FoodRation.Full ? SelectedColor : _normalColor;
        }

        private static void ShowStat(Text label, string name, int current, int next)
        {
            label.text = $"{name}  {current} → {next}";
            label.color = next < current ? LossColor : next > current ? GainColor : NeutralColor;
        }
    }
}
