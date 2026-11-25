using System;
using UnityEngine;

namespace ProjectBelow.Hub
{
    public enum FoodRation { None, Half, Full }

    [Serializable]
    public sealed class RationRules
    {
        [Serializable]
        private struct Effects
        {
            [SerializeField, Range(-100, 100)] private int _satiety;
            [SerializeField, Range(-100, 100)] private int _health;
            [SerializeField, Range(-100, 100)] private int _morale;

            public int Satiety => Mathf.Clamp(_satiety, -100, 100);
            public int Health => Mathf.Clamp(_health, -100, 100);
            public int Morale => Mathf.Clamp(_morale, -100, 100);

            public Effects(int satiety, int health, int morale)
            {
                _satiety = satiety;
                _health = health;
                _morale = morale;
            }
        }

        [SerializeField, Min(1)] private int _halfPortionCost = 1;
        [SerializeField] private Effects _noFood = new(-28, 0, -8);
        [SerializeField] private Effects _halfPortion = new(-8, 0, -2);
        [SerializeField] private Effects _fullPortion = new(18, 4, 5);
        [SerializeField, Range(0, 100)] private int _starvationThreshold = 25;
        [SerializeField, Range(0, 100)] private int _starvationHealthLoss = 12;
        [SerializeField, Range(0, 100)] private int _starvationMoraleLoss = 5;

        private int HalfPortionCost => Mathf.Max(1, _halfPortionCost);

        internal static bool IsValid(FoodRation ration) => ration is FoodRation.None or FoodRation.Half or FoodRation.Full;

        public long GetCost(FoodRation ration) => ration switch
        {
            FoodRation.None => 0L,
            FoodRation.Half => HalfPortionCost,
            FoodRation.Full => 2L * HalfPortionCost,
            _ => throw new ArgumentOutOfRangeException(nameof(ration))
        };

        public CharacterStats Predict(CharacterStats current, FoodRation ration)
        {
            Effects effects = ration switch
            {
                FoodRation.None => _noFood,
                FoodRation.Half => _halfPortion,
                FoodRation.Full => _fullPortion,
                _ => throw new ArgumentOutOfRangeException(nameof(ration))
            };
            int satiety = Mathf.Clamp(current.Satiety + effects.Satiety, 0, 100);
            int health = current.Health + effects.Health;
            int morale = current.Morale + effects.Morale;
            if (satiety <= Mathf.Clamp(_starvationThreshold, 0, 100))
            {
                health -= Mathf.Clamp(_starvationHealthLoss, 0, 100);
                morale -= Mathf.Clamp(_starvationMoraleLoss, 0, 100);
            }
            return new CharacterStats(health, satiety, morale, current.WorkCapacity);
        }

        internal FoodRation[] AllocateAutomatically(int count, long availableFood, int firstResident,
            out long cost, out int nextResident)
        {
            cost = 0;
            nextResident = firstResident;
            if (count == 0)
                return Array.Empty<FoodRation>();

            long portions = Math.Min(availableFood / HalfPortionCost, 2L * count);
            int portionsPerResident = (int)(portions / count);
            int extraPortions = (int)(portions % count);
            cost = portions * HalfPortionCost;
            var rations = new FoodRation[count];
            int index = firstResident;
            for (int i = 0; i < count; i++)
            {
                rations[index] = (FoodRation)(portionsPerResident + (i < extraPortions ? 1 : 0));
                index = (index + 1) % count;
            }

            // Start the next partial round with the first resident who missed the extra portion.
            nextResident = (int)(((long)firstResident + extraPortions) % count);
            return rations;
        }
    }
}
