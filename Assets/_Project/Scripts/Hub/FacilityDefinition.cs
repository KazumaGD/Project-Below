using System;
using System.Collections.Generic;
using ProjectBelow.Core;
using UnityEngine;

namespace ProjectBelow.Hub
{
    [CreateAssetMenu(menuName = "Project Below/Facility Definition")]
    public sealed class FacilityDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Upgrade
        {
            [SerializeField] private ResourceRequirement[] _costs;
            [SerializeField] private ResourceRequirement _productionBonus;
            [SerializeField, Range(0, 100)] private int _restBonus;

            public IReadOnlyList<ResourceRequirement> Costs => _costs ?? Array.Empty<ResourceRequirement>();
            public ResourceRequirement ProductionBonus => _productionBonus;
            public int RestBonus => Mathf.Clamp(_restBonus, 0, 100);

            internal bool HasValidBonus => _productionBonus.Item == null
                ? _productionBonus.Quantity == 0 && RestBonus > 0
                : ShelterStock.IsSaved(_productionBonus.Item) && _productionBonus.Quantity > 0;
        }

        [SerializeField, HideInInspector] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Upgrade[] _upgrades;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int MaxLevel => _upgrades?.Length ?? 0;

        public Upgrade GetUpgrade(int level) => level > 0 && level <= MaxLevel ? _upgrades[level - 1] : null;
    }
}
