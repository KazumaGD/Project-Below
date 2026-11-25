using System;

namespace ProjectBelow.Hub
{
    public sealed class CharacterState
    {
        public CharacterDefinition Definition { get; }
        public CharacterStats Stats { get; private set; }
        public TaskDefinition AssignedTask { get; internal set; }
        internal FoodRation PlannedRation { get; set; }
        public event Action<CharacterState> Changed;

        internal CharacterState(CharacterDefinition definition)
        {
            Definition = definition;
            Stats = definition.InitialStats;
        }

        public void SetStats(CharacterStats stats)
        {
            AssignStats(stats);
            NotifyStatsChanged();
        }

        internal void AssignStats(CharacterStats stats) => Stats = stats;
        internal void NotifyStatsChanged() => Changed?.Invoke(this);
    }
}
