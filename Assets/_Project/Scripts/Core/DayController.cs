using System;

namespace ProjectBelow.Core
{
    public enum DayPhase { Morning, Expedition, Evening }

    public sealed class DayController
    {
        private bool _closingDay;

        public long Number { get; private set; } = 1;
        public DayPhase Phase { get; private set; } = DayPhase.Morning;

        public event Action Changed;
        public event Action<long> DayEnded;

        internal bool CanStartExpedition => !_closingDay && Phase == DayPhase.Morning;
        internal bool CanFinishAfterDefeat => CanFinish(Number, DayPhase.Expedition);
        internal bool CanEndDay(long day) => CanFinish(day, DayPhase.Evening);

        internal void Restore(long day, DayPhase phase)
        {
            Number = day;
            Phase = phase;
        }

        internal bool TryStartExpedition()
        {
            if (!CanStartExpedition)
                return false;

            SetPhase(DayPhase.Expedition);
            return true;
        }

        internal bool TrySkipExpedition(Action completeWork)
        {
            if (!CanStartExpedition)
                return false;

            Phase = DayPhase.Evening;
            completeWork();
            Changed?.Invoke();
            return true;
        }

        internal void ReturnFromExpedition() => SetPhase(DayPhase.Evening);
        internal bool TryFinishAfterDefeat(Action applyConsequences) => TryFinish(Number, DayPhase.Expedition, applyConsequences);
        internal bool TryEndDay(long day, Action applyConsequences) => TryFinish(day, DayPhase.Evening, applyConsequences);

        private bool CanFinish(long day, DayPhase requiredPhase) =>
            !_closingDay && Number == day && Phase == requiredPhase && Number < long.MaxValue;

        private bool TryFinish(long day, DayPhase requiredPhase, Action applyConsequences)
        {
            if (!CanFinish(day, requiredPhase))
                return false;

            _closingDay = true;
            try
            {
                Number++;
                Phase = DayPhase.Morning;
                applyConsequences();
                DayEnded?.Invoke(day);
            }
            finally
            {
                _closingDay = false;
                Changed?.Invoke();
            }
            return true;
        }

        private void SetPhase(DayPhase phase)
        {
            Phase = phase;
            Changed?.Invoke();
        }
    }
}
