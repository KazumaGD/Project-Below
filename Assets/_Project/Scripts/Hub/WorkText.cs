using System.Collections.Generic;
using System.Linq;
using ProjectBelow.Core;

namespace ProjectBelow.Hub
{
    internal static class WorkText
    {
        internal static string Forecast(ResidentTaskResult result)
        {
            if (result.Construction != null)
                return $"{result.Construction.Facility.Definition.DisplayName} · уровень {result.Construction.TargetLevel}";

            string cost = result.Costs.Count == 0 ? "Без затрат" : FormatResources(result.Costs, "−");
            string output = result.Output.Item == null ? $"Трудоспособность +{result.WorkCapacityGain}"
                : $"+{result.Output.Quantity} {result.Output.Item.DisplayName}";
            if (result.Output.Item != null && result.WorkCapacityGain > 0)
                output += $", трудоспособность +{result.WorkCapacityGain}";
            return $"{cost}   ·   {output}";
        }

        internal static string Outcome(ResidentTaskResult result) => result.Outcome switch
        {
            TaskOutcome.Completed => $"Выполнено · {Forecast(result)}",
            TaskOutcome.MissingMaterials => $"Не хватает {result.MissingMaterial.Quantity} {result.MissingMaterial.Item.DisplayName}",
            TaskOutcome.NoProduction => "Трудоспособности не хватает для одной единицы",
            TaskOutcome.InvalidDefinition => "Поручение настроено некорректно",
            TaskOutcome.StorageFull => "На складе нет места для результата",
            _ => "Без поручения"
        };

        private static string FormatResources(IEnumerable<ResourceAmount> resources, string sign) =>
            string.Join(", ", resources.Select(resource => $"{sign}{resource.Quantity} {resource.Item.DisplayName}"));
    }
}
