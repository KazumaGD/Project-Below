using System;
using System.Collections.Generic;
using ProjectBelow.Hub;
using ProjectBelow.Inventory;
using ProjectBelow.Narrative;

namespace ProjectBelow.Core
{
    internal sealed class SaveCatalog
    {
        internal readonly Dictionary<string, ItemDefinition> Items = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, CharacterDefinition> Residents = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, TaskDefinition> Tasks = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, FacilityDefinition> Facilities = new(StringComparer.Ordinal);
        private readonly TaskDefinition _construction;

        internal SaveCatalog(ItemDefinition[] items, ItemDefinition food, CharacterDefinition[] residents,
            TaskDefinition[] tasks, FacilityDefinition[] facilities, TaskDefinition construction)
        {
            _construction = construction;
            foreach (var item in items ?? Array.Empty<ItemDefinition>())
                AddItem(item);
            AddItem(food);
            foreach (var resident in residents ?? Array.Empty<CharacterDefinition>())
                if (resident != null && !string.IsNullOrEmpty(resident.Id))
                    Residents.TryAdd(resident.Id, resident);
            foreach (var task in tasks ?? Array.Empty<TaskDefinition>())
            {
                if (task == null || string.IsNullOrEmpty(task.Id))
                    continue;
                Tasks.TryAdd(task.Id, task);
                AddItem(task.Output.Item);
                foreach (var cost in task.Costs)
                    AddItem(cost.Item);
            }
            if (construction != null && !string.IsNullOrEmpty(construction.Id))
                Tasks.TryAdd(construction.Id, construction);
            foreach (var facility in facilities ?? Array.Empty<FacilityDefinition>())
            {
                if (facility == null || string.IsNullOrEmpty(facility.Id))
                    continue;
                Facilities.TryAdd(facility.Id, facility);
                for (int level = 1; level <= facility.MaxLevel; level++)
                {
                    var upgrade = facility.GetUpgrade(level);
                    if (upgrade == null)
                        continue;
                    AddItem(upgrade.ProductionBonus.Item);
                    foreach (var cost in upgrade.Costs)
                        AddItem(cost.Item);
                }
            }
        }

        internal bool IsValid(SaveData data)
        {
            if (data == null || data.version != SaveData.CurrentVersion || data.day < 1 || data.playerHealth < 0
                || data.phase is not (DayPhase.Morning or DayPhase.Evening)
                || !ValidResources(data.stock, true) || data.residents == null || data.facilities == null
                || !StoryFlags.AreValid(data.storyFlags))
                return false;

            var residents = new HashSet<string>(StringComparer.Ordinal);
            foreach (var resident in data.residents)
            {
                if (resident == null || string.IsNullOrEmpty(resident.characterId)
                    || !Residents.TryGetValue(resident.characterId, out var definition) || !residents.Add(resident.characterId)
                    || !InStatRange(resident.health) || !InStatRange(resident.satiety)
                    || !InStatRange(resident.morale) || !InStatRange(resident.workCapacity)
                    || !RationRules.IsValid(resident.ration)
                    || (data.phase == DayPhase.Morning && resident.ration != FoodRation.None))
                    return false;
                if (!string.IsNullOrEmpty(resident.taskId) && (!Tasks.TryGetValue(resident.taskId, out var task)
                    || !task.Allows(definition.Specialization)
                    || (task == _construction && (data.construction?.day != data.day
                        || data.construction.builderId != resident.characterId))))
                    return false;
            }
            if (residents.Count > 0 && !residents.Contains(data.nextRationResidentId))
                return false;

            var facilities = new Dictionary<string, long[]>(StringComparer.Ordinal);
            foreach (var facility in data.facilities)
            {
                if (facility == null || string.IsNullOrEmpty(facility.facilityId)
                    || !Facilities.TryGetValue(facility.facilityId, out var definition) || facility.benefitsFromDays == null
                    || facility.benefitsFromDays.Length > definition.MaxLevel
                    || !facilities.TryAdd(facility.facilityId, facility.benefitsFromDays))
                    return false;

                long previous = 1;
                foreach (long day in facility.benefitsFromDays)
                {
                    if (day <= previous || (day > data.day && !(data.phase == DayPhase.Evening && day - 1 == data.day)))
                        return false;
                    previous = day;
                }
            }

            var construction = data.construction;
            if (construction != null && construction.day != 0)
            {
                if (!ValidConstruction(construction, data.day, residents, facilities)
                    || (!construction.completed && (construction.day != data.day || data.phase != DayPhase.Morning))
                    || (construction.completed && construction.day == data.day && data.phase != DayPhase.Evening))
                    return false;
                if (construction.day == data.day && !Array.Exists(data.residents,
                        resident => resident.characterId == construction.builderId && resident.taskId == _construction.Id))
                    return false;
            }

            var expedition = data.expedition;
            if (expedition != null && expedition.day != 0
                && (expedition.day < 1 || expedition.day > data.day
                    || (expedition.day == data.day && (!expedition.returnedSafely || data.phase != DayPhase.Evening))
                    || !ValidResources(expedition.resources, false)))
                return false;

            long workDay = data.phase == DayPhase.Evening ? data.day : data.day - 1;
            if ((data.work?.day ?? 0) != workDay)
                return false;
            return workDay == 0 || ValidWork(data.work, residents, facilities);
        }

        private bool ValidWork(SaveData.Work work, HashSet<string> residents, Dictionary<string, long[]> facilities)
        {
            if (work.results == null)
                return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var result in work.results)
            {
                if (result == null || !residents.Contains(result.characterId) || !seen.Add(result.characterId)
                    || !Enum.IsDefined(typeof(TaskOutcome), result.outcome) || !InStatRange(result.workCapacityGain)
                    || !ValidResources(result.costs, false) || !ValidOptionalResource(result.output)
                    || !ValidOptionalResource(result.missingMaterial)
                    || (result.outcome == TaskOutcome.MissingMaterials && (result.missingMaterial?.quantity ?? 0) <= 0))
                    return false;
                if (result.outcome == TaskOutcome.Unassigned)
                {
                    if (!string.IsNullOrEmpty(result.taskId))
                        return false;
                    continue;
                }
                if (string.IsNullOrEmpty(result.taskId) || !Tasks.TryGetValue(result.taskId, out var task))
                    return false;
                var order = result.construction;
                if (task == _construction && (order == null || order.day != work.day || !order.completed
                        || order.builderId != result.characterId || !ValidConstruction(order, work.day, residents, facilities)))
                    return false;
                if (task != _construction && order != null && order.day != 0)
                    return false;
            }
            return true;
        }

        private bool ValidConstruction(SaveData.Construction order, long currentDay, HashSet<string> residents,
            Dictionary<string, long[]> facilities)
        {
            if (_construction == null || string.IsNullOrEmpty(_construction.Id) || order.day < 1 || order.day > currentDay
                || order.day == long.MaxValue || !residents.Contains(order.builderId)
                || Residents[order.builderId].Specialization != CharacterSpecialization.Builder
                || string.IsNullOrEmpty(order.facilityId) || !facilities.TryGetValue(order.facilityId, out var days)
                || order.targetLevel < 1 || order.targetLevel > Facilities[order.facilityId].MaxLevel)
                return false;
            return order.completed ? order.targetLevel <= days.Length && days[order.targetLevel - 1] == order.day + 1
                : order.targetLevel - 1 == days.Length;
        }

        private bool ValidResources(SaveData.Resource[] resources, bool allowZero)
        {
            if (resources == null)
                return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var resource in resources)
                if (resource == null || string.IsNullOrEmpty(resource.itemId) || !Items.ContainsKey(resource.itemId)
                    || resource.quantity < (allowZero ? 0 : 1) || !seen.Add(resource.itemId))
                    return false;
            return true;
        }

        private bool ValidOptionalResource(SaveData.Resource resource) => resource == null
            || (string.IsNullOrEmpty(resource.itemId) ? resource.quantity == 0
                : resource.quantity >= 0 && Items.ContainsKey(resource.itemId));

        private void AddItem(ItemDefinition item)
        {
            if (ShelterStock.IsSaved(item))
                Items.TryAdd(item.Id, item);
        }

        private static bool InStatRange(int stat) => stat >= 0 && stat <= 100;
    }
}
