using System;
using ProjectBelow.Hub;

namespace ProjectBelow.Core
{
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version;
        public long day;
        public DayPhase phase;
        public int playerHealth;
        public string nextRationResidentId;
        public Resource[] stock;
        public Resident[] residents;
        public Facility[] facilities;
        public Construction construction;
        public Expedition expedition;
        public Work work;
        public string[] storyFlags;

        [Serializable]
        public sealed class Resource
        {
            public string itemId;
            public long quantity;
        }

        [Serializable]
        public sealed class Resident
        {
            public string characterId;
            public string taskId;
            public int health;
            public int satiety;
            public int morale;
            public int workCapacity;
            public FoodRation ration;
        }

        [Serializable]
        public sealed class Facility
        {
            public string facilityId;
            public long[] benefitsFromDays;
        }

        [Serializable]
        public sealed class Construction
        {
            public string facilityId;
            public string builderId;
            public int targetLevel;
            public long day;
            public bool completed;
        }

        [Serializable]
        public sealed class Expedition
        {
            public long day;
            public bool returnedSafely;
            public Resource[] resources;
        }

        [Serializable]
        public sealed class Work
        {
            public long day;
            public WorkResult[] results;
        }

        [Serializable]
        public sealed class WorkResult
        {
            public string characterId;
            public string taskId;
            public TaskOutcome outcome;
            public Resource[] costs;
            public Resource output;
            public int workCapacityGain;
            public Resource missingMaterial;
            public Construction construction;
        }
    }
}
