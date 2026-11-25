using System;
using System.Linq;
using ProjectBelow.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    public sealed class FacilityCardView : MonoBehaviour
    {
        [SerializeField] private Text _name;
        [SerializeField] private Text _description;
        [SerializeField] private Text _level;
        [SerializeField] private Text _activeBonus;
        [SerializeField] private Text _nextBonus;
        [SerializeField] private Text _cost;
        [SerializeField] private Text _availability;
        [SerializeField] private Button _build;

        private static readonly Color AvailableColor = new(0.74f, 0.85f, 0.66f);
        private static readonly Color UnavailableColor = new(0.92f, 0.66f, 0.54f);

        internal void Bind(FacilityState facility, Action<FacilityState> build)
        {
            _name.text = facility.Definition.DisplayName;
            _description.text = facility.Definition.Description;
            _build.onClick.AddListener(() => build(facility));
        }

        internal void Render(ExpeditionSession session, FacilityState facility)
        {
            var definition = facility.Definition;
            int active = facility.ActiveLevel(session.Day.Number);
            var next = definition.GetUpgrade(facility.Level + 1);
            var availability = session.GetUpgradeAvailability(facility, out var shortage);
            bool canBuild = availability == UpgradeAvailability.Available;

            _level.text = $"Уровень {facility.Level} / {definition.MaxLevel}";
            _activeBonus.text = active == 0 ? "Активных бонусов пока нет" : $"Действует · {FormatBonus(definition.GetUpgrade(active))}";
            _nextBonus.text = active < facility.Level
                ? $"С дня {facility.BenefitsFromDay} · {FormatBonus(definition.GetUpgrade(facility.Level))}"
                : next == null ? string.Empty : $"Далее · {FormatBonus(next)}";
            bool inProgress = session.Construction is { IsCompleted: false } && session.Construction.Facility == facility;
            _cost.text = inProgress ? "Улучшение оплачено" : FormatCost(next);
            _availability.text = inProgress ? "В работе" : AvailabilityText(availability, shortage);
            _availability.color = canBuild ? AvailableColor : UnavailableColor;
            _build.interactable = canBuild;
        }

        private static string FormatCost(FacilityDefinition.Upgrade upgrade)
        {
            if (upgrade == null)
                return string.Empty;
            return upgrade.Costs.Count == 0 ? "Без затрат" : string.Join(" · ",
                upgrade.Costs.Select(cost => $"{cost.Quantity} {cost.Item?.DisplayName ?? "?"}"));
        }

        private static string FormatBonus(FacilityDefinition.Upgrade upgrade)
        {
            if (upgrade == null)
                return "—";
            var product = upgrade.ProductionBonus;
            string rest = upgrade.RestBonus > 0 ? $"+{upgrade.RestBonus} к отдыху" : string.Empty;
            if (product.Item == null)
                return rest;
            string production = $"+{product.Quantity} {product.Item.DisplayName} к базе";
            return rest.Length == 0 ? production : $"{production}, {rest}";
        }

        private static string AvailabilityText(UpgradeAvailability availability, ResourceAmount shortage) => availability switch
        {
            UpgradeAvailability.Available => "Доступно",
            UpgradeAvailability.MorningOnly => "Доступно утром",
            UpgradeAvailability.InProgress => "Строитель занят",
            UpgradeAvailability.MaxLevel => "Максимальный уровень",
            UpgradeAvailability.NoBuilder => "Нет строителя",
            UpgradeAvailability.MissingMaterials => $"Не хватает {shortage.Quantity} {shortage.Item.DisplayName}",
            _ => "Проверьте настройки"
        };
    }
}
