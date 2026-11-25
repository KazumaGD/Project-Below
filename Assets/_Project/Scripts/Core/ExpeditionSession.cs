using System;
using System.Collections.Generic;
using ProjectBelow.Hub;
using ProjectBelow.Inventory;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectBelow.Core
{
    public readonly struct ResourceAmount
    {
        public ItemDefinition Item { get; }
        public long Quantity { get; }

        internal ResourceAmount(ItemDefinition item, long quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    public sealed class ExpeditionReport
    {
        public bool ReturnedSafely { get; }
        public long DayNumber { get; }
        public IReadOnlyList<ResourceAmount> Resources { get; }

        internal ExpeditionReport(bool returnedSafely, long dayNumber, List<ResourceAmount> resources)
        {
            ReturnedSafely = returnedSafely;
            DayNumber = dayNumber;
            Resources = resources.AsReadOnly();
        }
    }

    [DisallowMultipleComponent]
    public sealed partial class ExpeditionSession : MonoBehaviour
    {
        public const string HubScene = "Assets/_Project/Scenes/Hub.unity";
        public const string DungeonScene = "Assets/_Project/Scenes/ExplorationGraybox.unity";
        public const string MainMenuScene = "Assets/_Project/Scenes/MainMenu.unity";

        [SerializeField, Min(1)] private int _maxHealth = 20;
        [SerializeField] private ItemDefinition[] _resources;
        [SerializeField] private CharacterDefinition[] _residents;
        [SerializeField] private ItemDefinition _food;
        [SerializeField] private RationRules _rationRules = new();
        [SerializeField] private TaskDefinition[] _tasks;
        [SerializeField] private FacilityDefinition[] _facilities;
        [SerializeField] private TaskDefinition _constructionTask;

        private readonly Dictionary<string, ResourceAmount> _stock = new(StringComparer.Ordinal);
        private int _nextAutomaticResident;
        public DayController Day { get; } = new();
        public int MaxHealth => Mathf.Max(1, _maxHealth);
        public int Health { get; private set; }
        public bool IsLoading { get; private set; }
        public ExpeditionReport LastReport { get; private set; }
        public WorkReport LastWorkReport { get; private set; }
        public IReadOnlyCollection<ResourceAmount> Stock => _stock.Values;
        public IReadOnlyList<CharacterState> Residents { get; private set; }
        public long FoodStock => _food != null && !string.IsNullOrEmpty(_food.Id)
            && _stock.TryGetValue(_food.Id, out var food) ? food.Quantity : 0L;
        public RationRules Rations => _rationRules;
        public IReadOnlyList<TaskDefinition> Tasks { get; private set; }
        public IReadOnlyList<FacilityState> Facilities { get; private set; }
        public ConstructionOrder Construction { get; private set; }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Health = MaxHealth;
            foreach (var item in _resources ?? Array.Empty<ItemDefinition>())
                if (item != null && !string.IsNullOrEmpty(item.Id))
                    _stock.TryAdd(item.Id, new ResourceAmount(item, 0));
            if (_food != null && !string.IsNullOrEmpty(_food.Id))
                _stock.TryAdd(_food.Id, new ResourceAmount(_food, 0));
            else
                Debug.LogError("Assign the Food item definition to the expedition session.", this);

            var residents = new List<CharacterState>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definition in _residents ?? Array.Empty<CharacterDefinition>())
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                    continue;
                if (ids.Add(definition.Id))
                    residents.Add(new CharacterState(definition));
            }
            Residents = residents.AsReadOnly();

            var tasks = new List<TaskDefinition>();
            ids.Clear();
            foreach (var task in _tasks ?? Array.Empty<TaskDefinition>())
                if (task != null && task != _constructionTask && !string.IsNullOrEmpty(task.Id) && ids.Add(task.Id))
                    tasks.Add(task);
            Tasks = tasks.AsReadOnly();

            var facilities = new List<FacilityState>();
            ids.Clear();
            foreach (var facility in _facilities ?? Array.Empty<FacilityDefinition>())
                if (facility != null && !string.IsNullOrEmpty(facility.Id) && ids.Add(facility.Id))
                    facilities.Add(new FacilityState(facility));
            Facilities = facilities.AsReadOnly();
        }

        internal static ExpeditionSession Resolve(ExpeditionSession prefab)
        {
            var existing = FindFirstObjectByType<ExpeditionSession>();
            return existing != null ? existing : Instantiate(prefab);
        }

        public bool TryStartExpedition()
        {
            if (!Day.CanStartExpedition || !TryLoad(DungeonScene))
                return false;

            OpenExpedition();
            return true;
        }

        internal void OpenExpedition()
        {
            if (!Day.CanStartExpedition)
                return;

            Health = MaxHealth;
            Day.TryStartExpedition();
        }

        public bool TrySkipExpedition()
        {
            if (IsLoading || !Day.TrySkipExpedition(() => CompleteWork(Day.Number)))
                return false;

            NotifyResidents();
            return true;
        }

        public bool TryAssignTask(CharacterState resident, TaskDefinition task)
        {
            if (IsLoading || !Day.CanStartExpedition || resident == null)
                return false;

            bool isResident = false;
            foreach (var candidate in Residents)
                if (ReferenceEquals(resident, candidate))
                {
                    isResident = true;
                    break;
                }
            if (!isResident || IsBuilding(resident)
                || task != null && (!task.Allows(resident.Definition.Specialization) || !HasTask(task)))
                return false;

            resident.AssignedTask = task;
            return true;
        }

        public WorkReport PreviewWork() =>
            ShelterWork.Run(Day.Number, Residents, Facilities, Construction,
                new Dictionary<string, ResourceAmount>(_stock, StringComparer.Ordinal), false);

        public bool IsBuilding(CharacterState resident) => Construction is { IsCompleted: false }
            && ReferenceEquals(Construction.Builder, resident);

        public UpgradeAvailability GetUpgradeAvailability(FacilityState facility, out ResourceAmount shortage) =>
            CheckUpgrade(facility, out _, out _, out shortage);

        public bool TryStartConstruction(FacilityState facility)
        {
            if (CheckUpgrade(facility, out var builder, out var costs, out _) != UpgradeAvailability.Available)
                return false;

            var order = new ConstructionOrder(facility, builder, Day.Number);
            foreach (var cost in costs.Values)
                ShelterStock.Adjust(_stock, cost.Item, -cost.Quantity);
            builder.AssignedTask = _constructionTask;
            Construction = order;
            return true;
        }

        private UpgradeAvailability CheckUpgrade(FacilityState facility, out CharacterState builder,
            out Dictionary<string, ResourceAmount> costs, out ResourceAmount shortage)
        {
            builder = null;
            costs = null;
            shortage = default;
            if (facility == null || !ContainsFacility(facility))
                return UpgradeAvailability.InvalidDefinition;
            if (facility.Level >= facility.Definition.MaxLevel)
                return UpgradeAvailability.MaxLevel;
            if (Construction is { IsCompleted: false })
                return UpgradeAvailability.InProgress;
            if (IsLoading || !Day.CanStartExpedition || Day.Number == long.MaxValue)
                return UpgradeAvailability.MorningOnly;
            var upgrade = facility.Definition.GetUpgrade(facility.Level + 1);
            if (upgrade == null || !upgrade.HasValidBonus || !ShelterStock.TryTotalCosts(upgrade.Costs, out costs)
                || _constructionTask == null || string.IsNullOrEmpty(_constructionTask.Id))
                return UpgradeAvailability.InvalidDefinition;

            foreach (var resident in Residents)
                if (resident.Definition.Specialization == CharacterSpecialization.Builder
                    && _constructionTask.Allows(resident.Definition.Specialization))
                {
                    builder = resident;
                    break;
                }
            if (builder == null)
                return UpgradeAvailability.NoBuilder;

            shortage = ShelterStock.FindShortage(_stock, costs.Values);
            return shortage.Item != null ? UpgradeAvailability.MissingMaterials : UpgradeAvailability.Available;
        }

        private bool ContainsFacility(FacilityState facility)
        {
            foreach (var candidate in Facilities)
                if (ReferenceEquals(candidate, facility))
                    return true;
            return false;
        }

        private bool HasTask(TaskDefinition task)
        {
            foreach (var option in Tasks)
                if (option == task)
                    return true;
            return false;
        }

        private void CompleteWork(long day)
        {
            if (LastWorkReport?.DayNumber == day)
                return;

            LastWorkReport = ShelterWork.Run(day, Residents, Facilities, Construction, _stock, true);
            if (Construction is { IsCompleted: false } && Construction.Day == day)
                Construction.Complete();
        }

        public bool TryEndDay(long day, IReadOnlyList<FoodRation> rations)
        {
            if (IsLoading || !Day.CanEndDay(day) || rations == null || rations.Count != Residents.Count)
                return false;

            long budget = FoodStock;
            long cost = 0;
            foreach (var ration in rations)
            {
                if (!RationRules.IsValid(ration))
                    return false;
                cost += _rationRules.GetCost(ration);
            }
            if (cost > budget || !Day.TryEndDay(day, () =>
            {
                ApplyRations(rations, cost);
                ClearAssignments();
            }))
                return false;

            NotifyResidents();
            Autosave();
            return true;
        }

        internal bool TryFinish(PlayerInventory inventory, int health, bool returnedSafely)
        {
            if (Day.Phase != DayPhase.Expedition || IsLoading || inventory == null
                || (!returnedSafely && !Day.CanFinishAfterDefeat))
                return false;

            var totals = CountResources(inventory);
            if (returnedSafely)
            {
                foreach (var entry in totals)
                {
                    if (_stock.TryGetValue(entry.Key, out ResourceAmount current)
                        && current.Quantity > long.MaxValue - entry.Value.Quantity)
                    {
                        Debug.LogError("The shelter stock cannot hold the returned quantity.", this);
                        return false;
                    }
                }
            }
            if (!TryLoad(HubScene, Autosave))
                return false;

            Health = Mathf.Clamp(health, 0, MaxHealth);
            LastReport = new ExpeditionReport(returnedSafely, Day.Number, new List<ResourceAmount>(totals.Values));
            if (returnedSafely)
            {
                foreach (var entry in totals)
                {
                    _stock.TryGetValue(entry.Key, out ResourceAmount current);
                    _stock[entry.Key] = new ResourceAmount(entry.Value.Item, current.Quantity + entry.Value.Quantity);
                }
            }
            bool inventoryChanged = inventory.ClearSlots();
            if (returnedSafely)
            {
                CompleteWork(Day.Number);
                Day.ReturnFromExpedition();
                NotifyResidents();
            }
            else
                FinishAfterDefeat();
            if (inventoryChanged)
                inventory.NotifyChanged();
            return true;
        }

        private void FinishAfterDefeat()
        {
            long day = Day.Number;
            if (Day.TryFinishAfterDefeat(() =>
            {
                CompleteWork(day);
                var rations = _rationRules.AllocateAutomatically(Residents.Count, FoodStock, _nextAutomaticResident,
                    out long cost, out int nextResident);
                _nextAutomaticResident = nextResident;
                ApplyRations(rations, cost);
                ClearAssignments();
            }))
                NotifyResidents();
        }

        private void ApplyRations(IReadOnlyList<FoodRation> rations, long cost)
        {
            if (cost > 0)
            {
                var stock = _stock[_food.Id];
                _stock[_food.Id] = new ResourceAmount(stock.Item, stock.Quantity - cost);
            }

            for (int i = 0; i < Residents.Count; i++)
            {
                var resident = Residents[i];
                resident.AssignStats(_rationRules.Predict(resident.Stats, rations[i]));
            }
        }

        private void NotifyResidents()
        {
            foreach (var resident in Residents)
                resident.NotifyStatsChanged();
        }

        private void ClearAssignments()
        {
            foreach (var resident in Residents)
            {
                resident.AssignedTask = null;
                resident.PlannedRation = FoodRation.None;
            }
        }

        private static Dictionary<string, ResourceAmount> CountResources(PlayerInventory inventory)
        {
            var totals = new Dictionary<string, ResourceAmount>(StringComparer.Ordinal);
            for (int i = 0; i < inventory.Capacity; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack.IsEmpty)
                    continue;

                string id = stack.Item.Id;
                totals.TryGetValue(id, out ResourceAmount current);
                totals[id] = new ResourceAmount(stack.Item, current.Quantity + stack.Quantity);
            }
            return totals;
        }

        internal bool TryOpenHub() => TryLoad(HubScene);

        internal bool TryReturnToMainMenu() => InShelter && TryLoad(MainMenuScene, () => Destroy(gameObject));

        private bool TryLoad(string scenePath, Action onLoaded = null)
        {
            if (IsLoading)
                return false;
            if (!Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogError($"Scene is not enabled in the build scene list: {scenePath}", this);
                return false;
            }

            IsLoading = true;
            AsyncOperation operation;
            try
            {
                operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            }
            catch (ArgumentException exception)
            {
                IsLoading = false;
                Debug.LogException(exception, this);
                return false;
            }
            if (operation == null)
            {
                IsLoading = false;
                return false;
            }

            operation.completed += _ =>
            {
                IsLoading = false;
                onLoaded?.Invoke();
            };
            return true;
        }
    }
}
