using System.Collections.Generic;
using ProjectBelow.Combat;
using ProjectBelow.Exploration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    [DisallowMultipleComponent]
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private CombatController _combat;
        [SerializeField] private GameObject _content;
        [SerializeField] private Text _enemyName;
        [SerializeField] private Text _enemyIntent;
        [SerializeField] private Text _playerHealth;
        [SerializeField] private Text _phase;
        [SerializeField] private Text _log;
        [SerializeField, Range(1, 4)] private int _visibleMessages = 4;
        [SerializeField] private Button _attack;
        [SerializeField] private Button _defend;
        [SerializeField] private Button _flee;
        [SerializeField, FormerlySerializedAs("_restart")] private Button _returnToHub;

        private readonly Queue<string> _messages = new();
        private bool _bound;
        private ExpeditionFlow _expedition;

        private void OnEnable()
        {
            if (_combat == null || _content == null || _enemyName == null || _enemyIntent == null
                || _playerHealth == null || _phase == null || _log == null
                || _attack == null || _defend == null || _flee == null || _returnToHub == null)
            {
                Debug.LogError("Assign the combat controller and the HUD widgets.", this);
                enabled = false;
                return;
            }

            _bound = true;
            _expedition = _combat.GetComponent<ExpeditionFlow>();
            _combat.Changed += OnCombatChanged;
            _attack.onClick.AddListener(Attack);
            _defend.onClick.AddListener(Defend);
            _flee.onClick.AddListener(Flee);
            _returnToHub.onClick.AddListener(ReturnDefeated);
            Refresh();
        }

        private void OnDisable()
        {
            if (!_bound)
                return;

            _bound = false;
            if (_combat != null)
                _combat.Changed -= OnCombatChanged;
            _attack.onClick.RemoveListener(Attack);
            _defend.onClick.RemoveListener(Defend);
            _flee.onClick.RemoveListener(Flee);
            _returnToHub.onClick.RemoveListener(ReturnDefeated);
            SelectControl(null);
            _content.SetActive(false);
        }

        private void Attack() => _combat.TryAct(CombatAction.Attack);
        private void Defend() => _combat.TryAct(CombatAction.Defend);
        private void Flee() => _combat.TryAct(CombatAction.Flee);

        private void ReturnDefeated()
        {
            if (_expedition == null)
                Debug.LogError("The combat scene needs an ExpeditionFlow to return to the shelter.", this);
            else if (_expedition.TryReturnDefeated())
                _returnToHub.interactable = false;
        }

        private void OnCombatChanged(CombatEvent combatEvent, int damage, bool blocked)
        {
            if (combatEvent == CombatEvent.Opened)
                _messages.Clear();

            string message = combatEvent switch
            {
                CombatEvent.Opened => "Противник заметил вас.",
                CombatEvent.PlayerAttack => $"Вы наносите урон: {damage}.",
                CombatEvent.Defended => "Вы готовитесь отразить удар.",
                CombatEvent.FleeFailed => "Сбежать не удалось.",
                CombatEvent.EnemyAttack when damage == 0 => "Вы полностью блокируете удар.",
                CombatEvent.EnemyAttack when blocked => $"Вы смягчаете удар. Урон: {damage}.",
                CombatEvent.EnemyAttack => $"Противник наносит урон: {damage}.",
                CombatEvent.EnemyPrepared => "Противник готовится к удару.",
                CombatEvent.EnemyHeavyAttack when damage == 0 => "Вы блокируете мощный удар.",
                CombatEvent.EnemyHeavyAttack when blocked => $"Вы смягчаете мощный удар. Урон: {damage}.",
                CombatEvent.EnemyHeavyAttack => $"Мощный удар противника. Урон: {damage}.",
                CombatEvent.EnemyRecovered => "Противник восстанавливается.",
                CombatEvent.Victory => "Противник побеждён.",
                CombatEvent.Escaped => "Вы успешно сбежали.",
                CombatEvent.Defeat => "Вы потерпели поражение.",
                _ => null
            };
            if (message != null)
            {
                _messages.Enqueue(message);
                while (_messages.Count > Mathf.Clamp(_visibleMessages, 1, 4))
                    _messages.Dequeue();
                _log.text = string.Join("\n", _messages);
            }

            Refresh();
        }

        private void Refresh()
        {
            CombatPhase phase = _combat.Phase;
            bool active = phase != CombatPhase.Inactive && _combat.Enemy != null;
            if (!active)
            {
                SelectControl(null);
                _content.SetActive(false);
                return;
            }

            var enemy = _combat.Enemy;
            var player = _combat.Player;
            bool playerTurn = phase == CombatPhase.PlayerTurn;
            bool defeat = phase == CombatPhase.Defeat;
            bool canReturn = _combat.CanReturnToHub && _expedition?.Session?.IsLoading != true;
            bool selectAttack = playerTurn && (!_content.activeSelf || !_attack.interactable);
            bool selectReturn = canReturn && !_returnToHub.gameObject.activeSelf;

            _content.SetActive(true);
            _enemyName.text = enemy.DisplayName;
            _enemyIntent.text = playerTurn ? FormatIntent(_combat.EnemyIntent) : string.Empty;
            _playerHealth.text = $"Ваше здоровье  {player.Health} / {player.MaxHealth}";
            _phase.text = phase switch
            {
                CombatPhase.PlayerTurn => "Ваш ход",
                CombatPhase.EnemyTurn => "Ход противника",
                CombatPhase.Victory => "Победа",
                CombatPhase.Escaped => "Побег",
                CombatPhase.Defeat => "Поражение",
                _ => string.Empty
            };

            _attack.interactable = playerTurn;
            _defend.interactable = playerTurn;
            _flee.interactable = playerTurn;
            _attack.gameObject.SetActive(!defeat);
            _defend.gameObject.SetActive(!defeat);
            _flee.gameObject.SetActive(!defeat);
            _returnToHub.gameObject.SetActive(canReturn);
            if (selectAttack)
                SelectControl(_attack.gameObject);
            else if (selectReturn)
                SelectControl(_returnToHub.gameObject);
            else if (!playerTurn && !canReturn)
                SelectControl(null);
        }

        private static string FormatIntent(EnemyAction action) => action switch
        {
            EnemyAction.Prepare => "Намерение · подготовка",
            EnemyAction.HeavyAttack => "Намерение · мощная атака",
            EnemyAction.Recover => "Намерение · восстановление",
            _ => "Намерение · обычная атака"
        };

        private void SelectControl(GameObject control)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            if (control != null)
                eventSystem.SetSelectedGameObject(control);
            else if (eventSystem.currentSelectedGameObject is GameObject selected
                     && selected != null && selected.transform.IsChildOf(_content.transform))
                eventSystem.SetSelectedGameObject(null);
        }
    }
}
