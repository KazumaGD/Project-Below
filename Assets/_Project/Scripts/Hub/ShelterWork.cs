using System;
using System.Collections.Generic;
using ProjectBelow.Core;

namespace ProjectBelow.Hub
{
    public enum TaskOutcome { Unassigned, Completed, MissingMaterials, NoProduction, InvalidDefinition, StorageFull }

    public sealed class ResidentTaskResult
    {
        public CharacterDefinition Resident { get; }
        public TaskDefinition Task { get; }
        public TaskOutcome Outcome { get; }
        public IReadOnlyCollection<ResourceAmount> Costs { get; }
        public ResourceAmount Output { get; }
        public int WorkCapacityGain { get; }
        public ResourceAmount MissingMaterial { get; }
        public ConstructionOrder Construction { get; }

        internal ResidentTaskResult(CharacterDefinition resident, TaskDefinition task, TaskOutcome outcome,
            IReadOnlyCollection<ResourceAmount> costs, ResourceAmount output, int workCapacityGain,
            ResourceAmount missingMaterial = default, ConstructionOrder construction = null)
        {
            Resident = resident;
            Task = task;
            Outcome = outcome;
            Costs = costs;
            Output = output;
            WorkCapacityGain = workCapacityGain;
            MissingMaterial = missingMaterial;
            Construction = construction;
        }
    }

    public sealed class WorkReport
    {
        public long DayNumber { get; }
        public IReadOnlyList<ResidentTaskResult> Results { get; }

        internal WorkReport(long day, ResidentTaskResult[] results)
        {
            DayNumber = day;
            Results = Array.AsReadOnly(results);
        }
    }

    internal static class ShelterWork
    {
        internal static WorkReport Run(long day, IReadOnlyList<CharacterState> residents,
            IReadOnlyList<FacilityState> facilities, ConstructionOrder construction,
            Dictionary<string, ResourceAmount> stock, bool applyWorkCapacity)
        {
            var results = new ResidentTaskResult[residents.Count];
            for (int i = 0; i < residents.Count; i++)
            {
                var resident = residents[i];
                var result = construction != null && construction.Day == day && ReferenceEquals(resident, construction.Builder)
                    ? new ResidentTaskResult(resident.Definition, resident.AssignedTask, TaskOutcome.Completed,
                        Array.Empty<ResourceAmount>(), default, 0, construction: construction)
                    : Evaluate(resident, stock, facilities, day);
                results[i] = result;
                if (result.Outcome != TaskOutcome.Completed)
                    continue;

                foreach (var cost in result.Costs)
                    ShelterStock.Adjust(stock, cost.Item, -cost.Quantity);
                if (result.Output.Quantity > 0)
                    ShelterStock.Adjust(stock, result.Output.Item, result.Output.Quantity);

                if (applyWorkCapacity && result.WorkCapacityGain > 0)
                {
                    var stats = resident.Stats;
                    resident.AssignStats(new CharacterStats(stats.Health, stats.Satiety, stats.Morale,
                        stats.WorkCapacity + result.WorkCapacityGain));
                }
            }
            return new WorkReport(day, results);
        }

        private static ResidentTaskResult Evaluate(CharacterState resident, Dictionary<string, ResourceAmount> stock,
            IReadOnlyList<FacilityState> facilities, long day)
        {
            var task = resident.AssignedTask;
            if (task == null)
                return EmptyResult(resident, TaskOutcome.Unassigned);

            var product = task.Output;
            bool hasProduct = product.Item != null || product.Quantity != 0;
            if (!task.Allows(resident.Definition.Specialization)
                || hasProduct && (!ShelterStock.IsSaved(product.Item) || product.Quantity <= 0)
                || !hasProduct && task.WorkCapacityRecovery == 0
                || !ShelterStock.TryTotalCosts(task.Costs, out var costs))
                return EmptyResult(resident, TaskOutcome.InvalidDefinition);

            long baseQuantity = product.Quantity;
            long baseRecovery = task.WorkCapacityRecovery;
            foreach (var facility in facilities)
            {
                var bonus = facility.Definition.GetUpgrade(facility.ActiveLevel(day));
                if (bonus == null)
                    continue;
                if (hasProduct && bonus.ProductionBonus.Item == product.Item)
                    baseQuantity += Math.Max(0, bonus.ProductionBonus.Quantity);
                if (task.WorkCapacityRecovery > 0)
                    baseRecovery += bonus.RestBonus;
            }
            long quantity = Math.Min(int.MaxValue, baseQuantity) * resident.Stats.WorkCapacity / 100;
            var output = new ResourceAmount(product.Item, quantity);
            int recovery = (int)Math.Min(baseRecovery, 100 - resident.Stats.WorkCapacity);
            if (product.Item != null && quantity == 0)
                return new ResidentTaskResult(resident.Definition, task, TaskOutcome.NoProduction, costs.Values, output, recovery);

            var shortage = ShelterStock.FindShortage(stock, costs.Values);
            if (shortage.Item != null)
                return new ResidentTaskResult(resident.Definition, task, TaskOutcome.MissingMaterials,
                    costs.Values, output, recovery, shortage);

            if (quantity > 0)
            {
                costs.TryGetValue(product.Item.Id, out var outputCost);
                long remaining = ShelterStock.Quantity(stock, product.Item) - outputCost.Quantity;
                if (quantity > long.MaxValue - remaining)
                    return new ResidentTaskResult(resident.Definition, task, TaskOutcome.StorageFull, costs.Values, output, recovery);
            }
            return new ResidentTaskResult(resident.Definition, task, TaskOutcome.Completed, costs.Values, output, recovery);
        }

        private static ResidentTaskResult EmptyResult(CharacterState resident, TaskOutcome outcome) =>
            new(resident.Definition, resident.AssignedTask, outcome, Array.Empty<ResourceAmount>(), default, 0);

    }
}
