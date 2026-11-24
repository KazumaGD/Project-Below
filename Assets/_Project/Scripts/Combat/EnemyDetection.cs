using UnityEngine;

namespace ProjectBelow.Combat
{
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyCombatant))]
    public sealed class EnemyDetection : MonoBehaviour
    {
        [SerializeField] private GameModeController _gameMode;
        [SerializeField] private Transform _eyes;
        [SerializeField] private Transform _combatFocus;
        [SerializeField, Min(0f)] private float _detectionDistance = 3f;
        [SerializeField, Range(0f, 180f)] private float _viewAngle = 96f;
        [SerializeField] private LayerMask _obstructionLayers = Physics.DefaultRaycastLayers;

        private CombatController _combat;
        private EnemyCombatant _enemy;
        private bool _waitUntilPlayerLeavesSight;

        private void Awake()
        {
            _enemy = GetComponent<EnemyCombatant>();
            if (_gameMode == null || !_gameMode.TryGetComponent(out _combat) || _eyes == null || _combatFocus == null)
            {
                Debug.LogError("Assign a mode controller with combat, an eye origin and a combat focus.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (_gameMode == null || _combat == null || _gameMode.CurrentMode != GameMode.Exploration
                || _gameMode.PlayerView == null)
                return;

            if (!CanSee(_gameMode.PlayerView))
            {
                _waitUntilPlayerLeavesSight = false;
                return;
            }

            // A successful escape leaves the player in place, so require a new sighting.
            if (!_waitUntilPlayerLeavesSight && _combat.TryStartEncounter(_enemy, _combatFocus))
                _waitUntilPlayerLeavesSight = true;
        }

        private bool CanSee(Transform playerView)
        {
            Vector3 toPlayer = playerView.position - _eyes.position;
            float distanceSquared = toPlayer.sqrMagnitude;
            if (distanceSquared > _detectionDistance * _detectionDistance || distanceSquared < 0.0001f)
                return false;

            float distance = Mathf.Sqrt(distanceSquared);
            Vector3 direction = toPlayer / distance;
            float minimumDot = Mathf.Cos(_viewAngle * 0.5f * Mathf.Deg2Rad);
            if (Vector3.Dot(_eyes.forward, direction) < minimumDot)
                return false;

            return !Physics.Raycast(_eyes.position, direction, out RaycastHit hit, distance,
                       _obstructionLayers, QueryTriggerInteraction.Ignore)
                   || hit.collider.transform.IsChildOf(_gameMode.PlayerRoot);
        }
    }
}
