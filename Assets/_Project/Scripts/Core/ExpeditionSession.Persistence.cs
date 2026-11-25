using System;
using System.Linq;
using ProjectBelow.Hub;
using ProjectBelow.Narrative;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectBelow.Core
{
    public sealed partial class ExpeditionSession
    {
        private SaveSystem _saveSystem;
        private SaveCatalog _saveCatalog;
        private readonly StoryFlags _storyFlags = new();
        internal bool SuppressInitialWorkPopup { get; set; }
        private SaveCatalog Catalog => _saveCatalog ??= new SaveCatalog(_resources, _food, _residents, _tasks, _facilities, _constructionTask);
        private SaveSystem Saves => _saveSystem ??= new SaveSystem();

        public string SaveMessage { get; private set; }
        public event Action<string> SaveMessageChanged;

        internal bool InShelter => !IsLoading && SceneManager.GetActiveScene().path == HubScene
            && Day.Phase is DayPhase.Morning or DayPhase.Evening;

        internal bool IsValidSave(SaveData data) => Catalog.IsValid(data);

        public bool HasStoryFlag(string id) => _storyFlags.Has(id);

        internal void SetStoryFlag(string id) => _storyFlags.Set(id);

        public bool TrySaveProgress()
        {
            if (!InShelter)
                return false;
            return WriteProgress(false);
        }

        internal bool TrySaveNewGame() => SceneManager.GetActiveScene().path == MainMenuScene
            && Day.Number == 1 && Day.Phase == DayPhase.Morning && WriteProgress(false);

        internal bool TryPlanRation(CharacterState resident, FoodRation ration)
        {
            if (!InShelter || Day.Phase != DayPhase.Evening || !RationRules.IsValid(ration) || !Residents.Contains(resident))
                return false;
            resident.PlannedRation = ration;
            return true;
        }

        private bool WriteProgress(bool automatic)
        {
            bool success = Saves.TryWrite(CaptureSave(), IsValidSave);
            SaveMessage = success ? automatic ? "Автосохранение выполнено." : "Прогресс сохранён."
                : "Не удалось сохранить прогресс.";
            SaveMessageChanged?.Invoke(SaveMessage);
            return success;
        }

        private void Autosave()
        {
            if (InShelter)
                WriteProgress(true);
        }

        internal SaveData CaptureSave() => new()
        {
            version = SaveData.CurrentVersion,
            day = Day.Number,
            phase = Day.Phase,
            playerHealth = Health,
            storyFlags = _storyFlags.Capture(),
            nextRationResidentId = Residents.Count == 0 ? string.Empty : Residents[_nextAutomaticResident].Definition.Id,
            stock = _stock.Values.Select(CaptureResource).ToArray(),
            residents = Residents.Select(resident => new SaveData.Resident
            {
                characterId = resident.Definition.Id,
                taskId = resident.AssignedTask?.Id,
                health = resident.Stats.Health,
                satiety = resident.Stats.Satiety,
                morale = resident.Stats.Morale,
                workCapacity = resident.Stats.WorkCapacity,
                ration = resident.PlannedRation
            }).ToArray(),
            facilities = Facilities.Select(facility => new SaveData.Facility
            {
                facilityId = facility.Definition.Id,
                benefitsFromDays = facility.CaptureBenefitDays()
            }).ToArray(),
            construction = CaptureOrder(Construction),
            expedition = LastReport == null ? null : new SaveData.Expedition
            {
                day = LastReport.DayNumber,
                returnedSafely = LastReport.ReturnedSafely,
                resources = LastReport.Resources.Select(CaptureResource).ToArray()
            },
            work = LastWorkReport == null ? null : new SaveData.Work
            {
                day = LastWorkReport.DayNumber,
                results = LastWorkReport.Results.Select(result => new SaveData.WorkResult
                {
                    characterId = result.Resident.Id,
                    taskId = result.Task?.Id,
                    outcome = result.Outcome,
                    costs = result.Costs.Select(CaptureResource).ToArray(),
                    output = CaptureResource(result.Output),
                    workCapacityGain = result.WorkCapacityGain,
                    missingMaterial = CaptureResource(result.MissingMaterial),
                    construction = CaptureOrder(result.Construction)
                }).ToArray()
            }
        };

        private static SaveData.Resource CaptureResource(ResourceAmount amount) => new()
        {
            itemId = amount.Item?.Id,
            quantity = amount.Quantity
        };

        private static SaveData.Construction CaptureOrder(ConstructionOrder order) => order == null ? null : new SaveData.Construction
        {
            facilityId = order.Facility.Definition.Id,
            builderId = order.Builder.Definition.Id,
            targetLevel = order.TargetLevel,
            day = order.Day,
            completed = order.IsCompleted
        };

        internal bool TryRestoreSave(SaveData data)
        {
            if (!IsValidSave(data))
                return false;

            foreach (var resource in data.stock)
                _stock[resource.itemId] = RestoreResource(resource);
            var residents = Residents.ToDictionary(resident => resident.Definition.Id, StringComparer.Ordinal);
            var facilities = Facilities.ToDictionary(facility => facility.Definition.Id, StringComparer.Ordinal);
            foreach (var saved in data.residents)
            {
                var resident = residents[saved.characterId];
                resident.AssignStats(new CharacterStats(saved.health, saved.satiety, saved.morale, saved.workCapacity));
                resident.AssignedTask = RestoreTask(saved.taskId);
                resident.PlannedRation = saved.ration;
            }
            _nextAutomaticResident = 0;
            for (int i = 0; i < Residents.Count; i++)
                if (Residents[i].Definition.Id == data.nextRationResidentId)
                    _nextAutomaticResident = i;
            foreach (var saved in data.facilities)
                facilities[saved.facilityId].RestoreBenefitDays(saved.benefitsFromDays);

            Construction = RestoreOrder(data.construction);
            var expedition = data.expedition;
            LastReport = expedition == null || expedition.day == 0 ? null : new ExpeditionReport(expedition.returnedSafely,
                expedition.day, expedition.resources.Select(RestoreResource).ToList());
            var work = data.work;
            LastWorkReport = work == null || work.day == 0 ? null : new WorkReport(work.day, work.results.Select(result =>
                new ResidentTaskResult(residents[result.characterId].Definition, RestoreTask(result.taskId), result.outcome,
                    result.costs.Select(RestoreResource).ToArray(), RestoreResource(result.output), result.workCapacityGain,
                    RestoreResource(result.missingMaterial), RestoreOrder(result.construction))).ToArray());
            Health = Mathf.Clamp(data.playerHealth, 0, MaxHealth);
            _storyFlags.Restore(data.storyFlags);
            Day.Restore(data.day, data.phase);
            SuppressInitialWorkPopup = true;
            return true;

            ConstructionOrder RestoreOrder(SaveData.Construction saved)
            {
                if (saved == null || saved.day == 0)
                    return null;
                var facility = facilities[saved.facilityId];
                if (Construction != null && Construction.Day == saved.day && Construction.Facility == facility)
                    return Construction;
                return new ConstructionOrder(facility, residents[saved.builderId], saved.day, saved.targetLevel, saved.completed);
            }
        }

        private ResourceAmount RestoreResource(SaveData.Resource saved) => saved == null || string.IsNullOrEmpty(saved.itemId)
            ? default : new ResourceAmount(Catalog.Items[saved.itemId], saved.quantity);

        private TaskDefinition RestoreTask(string id) => string.IsNullOrEmpty(id) ? null : Catalog.Tasks[id];
    }
}
