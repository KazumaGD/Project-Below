using System.Collections.Generic;

namespace ProjectBelow.Hub
{
    public sealed class FacilityState
    {
        public FacilityDefinition Definition { get; }
        private readonly List<long> _benefitDays = new();
        public int Level => _benefitDays.Count;
        public long BenefitsFromDay => Level == 0 ? 0 : _benefitDays[Level - 1];

        internal FacilityState(FacilityDefinition definition) => Definition = definition;

        public int ActiveLevel(long day)
        {
            int level = Level;
            while (level > 0 && _benefitDays[level - 1] > day)
                level--;
            return level;
        }

        internal void FinishUpgrade(long day) => _benefitDays.Add(day + 1);
        internal long[] CaptureBenefitDays() => _benefitDays.ToArray();
        internal void RestoreBenefitDays(long[] days)
        {
            _benefitDays.Clear();
            _benefitDays.AddRange(days);
        }
    }

    public sealed class ConstructionOrder
    {
        public FacilityState Facility { get; }
        public CharacterState Builder { get; }
        public int TargetLevel { get; }
        public long Day { get; }
        public bool IsCompleted { get; private set; }

        internal ConstructionOrder(FacilityState facility, CharacterState builder, long day)
            : this(facility, builder, day, facility.Level + 1, false) { }

        internal ConstructionOrder(FacilityState facility, CharacterState builder, long day, int level, bool completed)
        {
            Facility = facility;
            Builder = builder;
            TargetLevel = level;
            Day = day;
            IsCompleted = completed;
        }

        internal void Complete()
        {
            Facility.FinishUpgrade(Day);
            IsCompleted = true;
        }
    }

    public enum UpgradeAvailability { Available, MorningOnly, InProgress, MaxLevel, NoBuilder, MissingMaterials, InvalidDefinition }
}
