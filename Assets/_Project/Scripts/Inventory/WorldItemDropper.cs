using ProjectBelow.Combat;
using ProjectBelow.Exploration;
using UnityEngine;

namespace ProjectBelow.Inventory
{
    [DisallowMultipleComponent, RequireComponent(typeof(GameModeController))]
    public sealed class WorldItemDropper : MonoBehaviour
    {
        private const float FloorClearance = 0.025f;
        private const float FloorTolerance = 0.08f;
        private const int PlacementAttempts = 6;
        private static readonly Vector2[] Corners =
        {
            new(-1f, -1f), new(-1f, 1f), new(1f, -1f), new(1f, 1f)
        };

        [SerializeField] private ItemPickup _pickupPrefab;
        [SerializeField, Min(0.5f)] private float _dropDistance = 1.45f;
        [SerializeField, Min(0f)] private float _maxStepUp = 0.25f;
        [SerializeField, Min(0f)] private float _maxStepDown = 0.35f;

        private GameModeController _mode;
        private CharacterController _character;
        private PlayerInventory _inventory;
        private PlayerInteraction _interaction;
        private BoxCollider _shape;

        private void Awake()
        {
            _mode = GetComponent<GameModeController>();
            var player = _mode.PlayerRoot;
            if (player != null)
            {
                _character = player.GetComponent<CharacterController>();
                _inventory = player.GetComponent<PlayerInventory>();
                _interaction = player.GetComponent<PlayerInteraction>();
            }
            if (_pickupPrefab != null)
                _shape = _pickupPrefab.GetComponent<BoxCollider>();
            if (_shape == null || !_shape.enabled || _shape.isTrigger || _pickupPrefab.gameObject.activeSelf
                || _character == null || _inventory == null || _interaction == null)
            {
                Debug.LogError("Assign an inactive pickup template with a solid BoxCollider and a valid exploration player.", this);
                enabled = false;
            }
        }

        public bool TryDrop(int index)
        {
            if (!isActiveAndEnabled || _mode.CurrentMode != GameMode.Inventory || !_inventory.Slots.Contains(index))
                return false;

            ItemStack stack = _inventory.GetSlot(index);
            if (stack.IsEmpty || !TryFindPlacement(out Vector3 position))
                return false;

            var pickup = Instantiate(_pickupPrefab, position, Quaternion.identity);
            pickup.name = $"Dropped {stack.Item.DisplayName}";
            pickup.ConfigureDropped(stack, _inventory);
            pickup.gameObject.SetActive(true);
            if (!pickup.CanInteract || !_inventory.GetSlot(index).Equals(stack))
            {
                pickup.gameObject.SetActive(false);
                Destroy(pickup.gameObject);
                return false;
            }

            _inventory.Slots[index] = default;
            _inventory.NotifyChanged();
            return true;
        }

        private bool TryFindPlacement(out Vector3 position)
        {
            position = default;
            if (!_character.enabled || _mode.PlayerView == null)
                return false;

            Physics.SyncTransforms();
            Bounds playerBounds = _character.bounds;
            Vector3 feet = new(playerBounds.center.x, playerBounds.min.y, playerBounds.center.z);
            Vector3 direction = Vector3.ProjectOnPlane(_mode.PlayerRoot.forward, Vector3.up).normalized;
            Vector3 scale = _pickupPrefab.transform.localScale;
            Vector3 half = Vector3.Scale(_shape.size * 0.5f, scale);
            Vector3 shapeOffset = Vector3.Scale(_shape.center, scale);
            float footprintRadius = new Vector2(half.x, half.z).magnitude;
            float minDistance = Mathf.Max(playerBounds.extents.x, playerBounds.extents.z) + footprintRadius + 2f * FloorClearance;
            if (_dropDistance < minDistance || direction == Vector3.zero)
                return false;

            for (int i = 0; i < PlacementAttempts; i++)
            {
                float distance = Mathf.Lerp(_dropDistance, minDistance, i / (float)(PlacementAttempts - 1));
                Vector3 sample = feet + direction * distance;
                if (!FindGround(sample, out float ground) || !HasSupport(sample, ground, half))
                    continue;

                Vector3 center = new(sample.x, ground + half.y + FloorClearance, sample.z);
                if (IsClearAndReachable(center, feet, half))
                {
                    position = center - shapeOffset;
                    return true;
                }
            }
            return false;
        }

        private bool HasSupport(Vector3 sample, float ground, Vector3 half)
        {
            foreach (Vector2 corner in Corners)
            {
                Vector3 offset = new(corner.x * half.x, 0f, corner.y * half.z);
                if (!FindGround(sample + offset, out float cornerGround) || Mathf.Abs(ground - cornerGround) > FloorTolerance)
                    return false;
            }
            return true;
        }

        private bool FindGround(Vector3 sample, out float ground)
        {
            Vector3 origin = sample + Vector3.up * (_maxStepUp + FloorClearance);
            float distance = _maxStepUp + _maxStepDown + FloorClearance;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) && hit.normal.y >= 0.7f)
            {
                ground = hit.point.y;
                return true;
            }

            ground = 0f;
            return false;
        }

        private bool IsClearAndReachable(Vector3 center, Vector3 feet, Vector3 half)
        {
            if (Physics.CheckBox(center, half, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;

            Vector3 view = _mode.PlayerView.position;
            Vector3 sightLine = center - view;
            float reach = sightLine.magnitude;
            if (reach > _interaction.InteractionDistance
                || Physics.Raycast(view, sightLine, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;

            Vector3 origin = new(feet.x, center.y, feet.z);
            Vector3 path = center - origin;
            foreach (RaycastHit hit in Physics.BoxCastAll(origin, half, path, Quaternion.identity, path.magnitude,
                         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(_mode.PlayerRoot))
                    return false;
            return true;
        }
    }
}
