using System.Collections.Generic;
using ProjectBelow.Combat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    [DisallowMultipleComponent]
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private CombatController _combat;
        [SerializeField] private GameObject _content;
        [SerializeField] private Text _enemyName;
        [SerializeField] private RectTransform _enemyHealth;
        [SerializeField] private Text _playerHealth;
        [SerializeField] private Text _phase;
        [SerializeField] private Text _log;
        [SerializeField, Range(1, 4)] private int _visibleMessages = 4;
        [SerializeField] private Button _attack;
        [SerializeField] private Button _defend;
        [SerializeField] private Button _flee;
        [SerializeField] private Button _restart;

        private readonly Queue<string> _messages = new();
        private bool _bound;
        private bool _restarting;

        private void OnEnable()
        {
            if (_combat == null || _content == null || _enemyName == null || _enemyHealth == null
                || _playerHealth == null || _phase == null || _log == null
                || _attack == null || _defend == null || _flee == null || _restart == null)
            {
                Debug.LogError("Assign the combat controller and the HUD widgets.", this);
                enabled = false;
                return;
            }

            _bound = true;
            _combat.Changed += OnCombatChanged;
            _attack.onClick.AddListener(Attack);
            _defend.onClick.AddListener(Defend);
            _flee.onClick.AddListener(Flee);
            _restart.onClick.AddListener(RestartScene);
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
            _restart.onClick.RemoveListener(RestartScene);
            SelectControl(null);
            _content.SetActive(false);
        }

        private void Attack() => _combat.TryAct(CombatAction.Attack);
        private void Defend() => _combat.TryAct(CombatAction.Defend);
        private void Flee() => _combat.TryAct(CombatAction.Flee);

        private void RestartScene()
        {
            if (_restarting || _combat.Phase != CombatPhase.Defeat)
                return;

            string scenePath = gameObject.scene.path;
            if (!Application.CanStreamedLevelBeLoaded(scenePath))
            {
                Debug.LogError($"The test scene is not enabled in the build scene list: {scenePath}", this);
                return;
            }

            _restarting = true;
            _restart.interactable = false;
            SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
        }

        private void OnCombatChanged(CombatEvent combatEvent, int damage)
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
                CombatEvent.EnemyAttack => $"Противник наносит урон: {damage}.",
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
            bool selectAttack = playerTurn && (!_content.activeSelf || !_attack.interactable);
            bool selectRestart = defeat && !_restart.gameObject.activeSelf;

            _content.SetActive(true);
            _enemyName.text = enemy.DisplayName;
            _enemyHealth.anchorMax = new Vector2((float)enemy.State.Health / enemy.State.MaxHealth, 1f);
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
            _restart.gameObject.SetActive(defeat);
            if (selectAttack)
                SelectControl(_attack.gameObject);
            else if (selectRestart)
                SelectControl(_restart.gameObject);
            else if (!playerTurn && !defeat)
                SelectControl(null);
        }

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
