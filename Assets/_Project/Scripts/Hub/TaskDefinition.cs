using System;
using System.Collections.Generic;
using ProjectBelow.Inventory;
using UnityEngine;

namespace ProjectBelow.Hub
{
    [Serializable]
    public struct ResourceRequirement
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField, Min(0)] private int _quantity;

        public ItemDefinition Item => _item;
        public int Quantity => _quantity;
    }

    [CreateAssetMenu(menuName = "Project Below/Task Definition")]
    public sealed class TaskDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private CharacterSpecialization[] _specializations;
        [SerializeField] private ResourceRequirement[] _costs;
        [SerializeField] private ResourceRequirement _output;
        [SerializeField, Range(0, 100)] private int _workCapacityRecovery;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public IReadOnlyList<ResourceRequirement> Costs => _costs ?? Array.Empty<ResourceRequirement>();
        public ResourceRequirement Output => _output;
        public int WorkCapacityRecovery => Mathf.Clamp(_workCapacityRecovery, 0, 100);

        public bool Allows(CharacterSpecialization specialization) =>
            _specializations == null || _specializations.Length == 0 || Array.IndexOf(_specializations, specialization) >= 0;
    }
}
