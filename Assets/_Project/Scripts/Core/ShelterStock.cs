using System;
using System.Collections.Generic;
using ProjectBelow.Hub;
using ProjectBelow.Inventory;

namespace ProjectBelow.Core
{
    internal static class ShelterStock
    {
        internal static bool IsSaved(ItemDefinition item) => item != null && !string.IsNullOrEmpty(item.Id);

        internal static long Quantity(Dictionary<string, ResourceAmount> stock, ItemDefinition item) =>
            stock.TryGetValue(item.Id, out var amount) ? amount.Quantity : 0;

        internal static void Adjust(Dictionary<string, ResourceAmount> stock, ItemDefinition item, long delta) =>
            stock[item.Id] = new ResourceAmount(item, Quantity(stock, item) + delta);

        internal static bool TryTotalCosts(IReadOnlyList<ResourceRequirement> requirements,
            out Dictionary<string, ResourceAmount> costs)
        {
            costs = new Dictionary<string, ResourceAmount>(StringComparer.Ordinal);
            foreach (var material in requirements)
            {
                if (!IsSaved(material.Item) || material.Quantity <= 0)
                    return false;

                Adjust(costs, material.Item, material.Quantity);
            }
            return true;
        }

        internal static ResourceAmount FindShortage(Dictionary<string, ResourceAmount> stock, IEnumerable<ResourceAmount> costs)
        {
            foreach (var cost in costs)
            {
                long missing = cost.Quantity - Quantity(stock, cost.Item);
                if (missing > 0)
                    return new ResourceAmount(cost.Item, missing);
            }
            return default;
        }
    }
}
