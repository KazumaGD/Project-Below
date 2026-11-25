using System;
using UnityEngine;

namespace ProjectBelow.Hub
{
    public enum CharacterSpecialization { Craftsperson, Agronomist, Builder }

    [Serializable]
    public struct CharacterStats
    {
        [SerializeField, Range(0, 100)] private int _health;
        [SerializeField, Range(0, 100)] private int _satiety;
        [SerializeField, Range(0, 100)] private int _morale;
        [SerializeField, Range(0, 100)] private int _workCapacity;

        public int Health => Mathf.Clamp(_health, 0, 100);
        public int Satiety => Mathf.Clamp(_satiety, 0, 100);
        public int Morale => Mathf.Clamp(_morale, 0, 100);
        public int WorkCapacity => Mathf.Clamp(_workCapacity, 0, 100);

        public CharacterStats(int health, int satiety, int morale, int workCapacity)
        {
            _health = Mathf.Clamp(health, 0, 100);
            _satiety = Mathf.Clamp(satiety, 0, 100);
            _morale = Mathf.Clamp(morale, 0, 100);
            _workCapacity = Mathf.Clamp(workCapacity, 0, 100);
        }
    }

    [CreateAssetMenu(menuName = "Project Below/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _portrait;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private CharacterSpecialization _specialization;
        [SerializeField] private CharacterStats _initialStats = new(100, 100, 100, 100);

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Portrait => _portrait;
        public string Description => _description;
        public CharacterSpecialization Specialization => _specialization;
        public CharacterStats InitialStats => _initialStats;
    }
}
