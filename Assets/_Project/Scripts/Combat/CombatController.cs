using System;
using System.Collections;
using ProjectBelow.Exploration;
using UnityEngine;

namespace ProjectBelow.Combat
{
    public enum CombatPhase { Inactive, PlayerTurn, EnemyTurn, Victory, Escaped, Defeat }
    public enum CombatAction { Attack, Defend, Flee }
    public enum CombatEvent
    {
        Opened, PlayerAttack, Defended, FleeFailed,
        EnemyAttackWindup, EnemyAttack, EnemyPrepared, EnemyHeavyAttack, EnemyRecovered,
        Victory, Escaped, Defeat, Closed, PlayerTurn
    }

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
        private CombatFeedback _feedback;
        private Coroutine _pendingTurn;
        private int _enemyTurnIndex;

        public event Action<CombatEvent, int, bool> Changed;
        public CombatPhase Phase { get; private set; }
        public CombatantState Player { get; private set; }
        public EnemyCombatant Enemy { get; private set; }
        public EnemyAction EnemyIntent { get; private set; }
        public bool CanReturnToHub { get; private set; }

        private void Awake()
        {
            _gameMode = GetComponent<GameModeController>();
            _feedback = GetComponent<CombatFeedback>();
            var expedition = GetComponent<ExpeditionFlow>();
            int maxHealth = expedition != null && expedition.Session != null ? expedition.Session.MaxHealth : _playerMaxHealth;
            Player = new CombatantState(maxHealth, _playerDamage);
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
            _enemyTurnIndex = 0;
            BeginPlayerTurn(CombatEvent.Opened);
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
            _pendingTurn = StartCoroutine(ResolveEnemyTurn(action, damage, defending));
        }

        private IEnumerator ResolveEnemyTurn(CombatEvent playerAction, int playerDamage, bool defending)
        {
            Publish(playerAction, playerDamage);
            yield return WaitForFeedback(_turnDelay);
            if (!CheckEnemy())
                yield break;

            (CombatEvent action, int damage) = EnemyIntent switch
            {
                EnemyAction.Prepare => (CombatEvent.EnemyPrepared, 0),
                EnemyAction.HeavyAttack => (CombatEvent.EnemyHeavyAttack, Enemy.HeavyAttackDamage),
                EnemyAction.Recover => (CombatEvent.EnemyRecovered, 0),
                _ => (CombatEvent.EnemyAttack, Enemy.State.Damage)
            };
            if (damage > 0)
            {
                Publish(CombatEvent.EnemyAttackWindup);
                yield return WaitForFeedback(0f);
                if (!CheckEnemy())
                    yield break;
            }

            int attackDamage = damage;
            if (defending)
                damage = (int)Math.Round(damage * ((100 - _defenseReductionPercent) / 100d));

            bool blocked = damage < attackDamage;
            int applied = Player.TakeDamage(damage);
            if (Player.IsDefeated)
            {
                Phase = CombatPhase.Defeat;
                Publish(action, applied, blocked);
                yield return WaitForFeedback(0f);
                _pendingTurn = null;
                CanReturnToHub = true;
                Publish(CombatEvent.Defeat);
                yield break;
            }

            Publish(action, applied, blocked);
            yield return WaitForFeedback(_turnDelay);
            if (!CheckEnemy())
                yield break;
            _pendingTurn = null;
            BeginPlayerTurn(CombatEvent.PlayerTurn);
        }

        private bool CheckEnemy()
        {
            if (Enemy != null && Enemy.isActiveAndEnabled)
                return true;

            _pendingTurn = null;
            _gameMode.EndCombat();
            return false;
        }

        private void BeginPlayerTurn(CombatEvent combatEvent)
        {
            EnemyIntent = Enemy.GetAction(_enemyTurnIndex++);
            Phase = CombatPhase.PlayerTurn;
            Publish(combatEvent);
        }

        private void Finish(CombatPhase phase, CombatEvent outcome, CombatEvent? action = null, int damage = 0)
        {
            Phase = phase;
            _pendingTurn = StartCoroutine(ResolveOutcome(phase, outcome, action, damage));
        }

        private IEnumerator ResolveOutcome(CombatPhase phase, CombatEvent outcome, CombatEvent? action, int damage)
        {
            if (action.HasValue)
            {
                Publish(action.Value, damage);
                yield return WaitForFeedback(0f);
            }
            Publish(outcome);
            yield return WaitForFeedback(_outcomeDelay);
            _pendingTurn = null;
            if (phase == CombatPhase.Victory && Enemy != null)
                Enemy.CompleteDefeat(_gameMode);
            _gameMode.EndCombat();
        }

        private IEnumerator WaitForFeedback(float minimumDelay)
        {
            double readyAt = Time.timeAsDouble + Mathf.Max(0f, minimumDelay);
            while (Time.timeAsDouble < readyAt || (_feedback != null && _feedback.IsPlaying))
                yield return null;
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
            CanReturnToHub = false;
            Enemy = null;
            Publish(CombatEvent.Closed);
        }

        private void Publish(CombatEvent combatEvent, int damage = 0, bool blocked = false)
        {
            Changed?.Invoke(combatEvent, damage, blocked);
        }
    }
}
