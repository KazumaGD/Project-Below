using System;
using System.Collections;
using UnityEngine;

namespace ProjectBelow.Combat
{
    public enum CombatPhase { Inactive, PlayerTurn, EnemyTurn, Victory, Escaped, Defeat }
    public enum CombatAction { Attack, Defend, Flee }
    public enum CombatEvent { Opened, PlayerAttack, Defended, FleeFailed, EnemyAttack, Victory, Escaped, Defeat, Closed, PlayerTurn }

    [DisallowMultipleComponent, RequireComponent(typeof(GameModeController))]
    public sealed class CombatController : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _playerMaxHealth = 20;
        [SerializeField, Min(1)] private int _playerDamage = 4;
        [SerializeField, Range(0, 100)] private int _defenseReductionPercent = 50;
        [SerializeField, Range(0, 100)] private int _fleeChancePercent = 50;
        [SerializeField, Min(0f)] private float _turnDelay = 0.6f;
        [SerializeField, Min(0f)] private float _outcomeDelay = 1.2f;

        private GameModeController _gameMode;
        private Coroutine _pendingTurn;

        public event Action<CombatEvent, int> Changed;
        public CombatPhase Phase { get; private set; }
        public CombatantState Player { get; private set; }
        public EnemyCombatant Enemy { get; private set; }

        private void Awake()
        {
            _gameMode = GetComponent<GameModeController>();
            Player = new CombatantState(_playerMaxHealth, _playerDamage);
        }

        private void OnEnable()
        {
            _gameMode.ModeChanged += OnModeChanged;
        }

        private void OnDisable()
        {
            _gameMode.ModeChanged -= OnModeChanged;
            if (Phase == CombatPhase.Inactive)
                return;

            CloseEncounter();
            _gameMode.EndCombat();
        }

        public bool TryStartEncounter(EnemyCombatant enemy, Transform focus)
        {
            if (!isActiveAndEnabled || Phase != CombatPhase.Inactive || Player.IsDefeated
                || enemy == null || !enemy.isActiveAndEnabled || enemy.State == null || enemy.State.IsDefeated
                || !_gameMode.TryStartCombat(focus))
                return false;

            Enemy = enemy;
            Phase = CombatPhase.PlayerTurn;
            Publish(CombatEvent.Opened);
            return true;
        }

        public bool TryAct(CombatAction action)
        {
            if (Phase != CombatPhase.PlayerTurn || Enemy == null)
                return false;

            switch (action)
            {
                case CombatAction.Attack:
                    int damage = Enemy.State.TakeDamage(Player.Damage);
                    if (Enemy.State.IsDefeated)
                        Finish(CombatPhase.Victory, CombatEvent.Victory, CombatEvent.PlayerAttack, damage);
                    else
                        BeginEnemyTurn(CombatEvent.PlayerAttack, damage);
                    return true;

                case CombatAction.Defend:
                    BeginEnemyTurn(CombatEvent.Defended, defending: true);
                    return true;

                case CombatAction.Flee:
                    if (UnityEngine.Random.Range(0, 100) < _fleeChancePercent)
                        Finish(CombatPhase.Escaped, CombatEvent.Escaped);
                    else
                        BeginEnemyTurn(CombatEvent.FleeFailed);
                    return true;

                default:
                    return false;
            }
        }

        private void BeginEnemyTurn(CombatEvent action, int damage = 0, bool defending = false)
        {
            Phase = CombatPhase.EnemyTurn;
            _pendingTurn = StartCoroutine(ResolveEnemyTurn(defending));
            Publish(action, damage);
        }

        private IEnumerator ResolveEnemyTurn(bool defending)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, _turnDelay));
            if (Enemy == null || !Enemy.isActiveAndEnabled)
            {
                _pendingTurn = null;
                _gameMode.EndCombat();
                yield break;
            }

            int damage = Enemy.State.Damage;
            if (defending)
                damage = Mathf.RoundToInt(damage * (1f - _defenseReductionPercent / 100f));

            int applied = Player.TakeDamage(damage);
            if (Player.IsDefeated)
            {
                _pendingTurn = null;
                Finish(CombatPhase.Defeat, CombatEvent.Defeat, CombatEvent.EnemyAttack, applied);
                yield break;
            }

            Publish(CombatEvent.EnemyAttack, applied);
            yield return new WaitForSeconds(Mathf.Max(0f, _turnDelay));
            _pendingTurn = null;
            Phase = CombatPhase.PlayerTurn;
            Publish(CombatEvent.PlayerTurn);
        }

        private void Finish(CombatPhase phase, CombatEvent outcome, CombatEvent? action = null, int damage = 0)
        {
            Phase = phase;
            if (phase != CombatPhase.Defeat)
                _pendingTurn = StartCoroutine(ReturnToExploration(phase == CombatPhase.Victory));

            if (action.HasValue)
                Publish(action.Value, damage);
            Publish(outcome);
        }

        private IEnumerator ReturnToExploration(bool disableEnemy)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, _outcomeDelay));
            _pendingTurn = null;
            if (disableEnemy && Enemy != null)
                Enemy.gameObject.SetActive(false);
            _gameMode.EndCombat();
        }

        private void OnModeChanged(GameMode mode)
        {
            if (mode == GameMode.Exploration && Phase != CombatPhase.Inactive)
                CloseEncounter();
        }

        private void CloseEncounter()
        {
            if (_pendingTurn != null)
                StopCoroutine(_pendingTurn);
            _pendingTurn = null;
            Phase = CombatPhase.Inactive;
            Enemy = null;
            Publish(CombatEvent.Closed);
        }

        private void Publish(CombatEvent combatEvent, int damage = 0)
        {
            Changed?.Invoke(combatEvent, damage);
        }
    }
}
