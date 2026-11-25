using System.Collections.Generic;
using System.Linq;
using ProjectBelow.Combat;
using ProjectBelow.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    [DisallowMultipleComponent]
    public sealed class HubHud : MonoBehaviour
    {
        [SerializeField] private ExpeditionSession _sessionPrefab;
        [SerializeField] private GameModeController _mode;
        [SerializeField] private Text _stockNames;
        [SerializeField] private Text _stockQuantities;
        [SerializeField] private Text _health;
        [SerializeField] private Text _resultTitle;
        [SerializeField] private Text _resultNames;
        [SerializeField] private Text _resultQuantities;
        [SerializeField] private Text _status;
        [SerializeField] private Text _dayLabel;
        [SerializeField] private Button _start;
        [SerializeField] private Button _skip;
        [SerializeField, FormerlySerializedAs("_endDay")] private Button _openRations;
        [SerializeField] private WorkReportHud _workReport;

        private ExpeditionSession _session;
        internal ExpeditionSession Session => _session;

        private void Start()
        {
            if (_sessionPrefab == null || _mode == null || _stockNames == null || _stockQuantities == null || _health == null
                || _resultTitle == null || _resultNames == null || _resultQuantities == null || _status == null
                || _dayLabel == null || _start == null || _skip == null || _openRations == null)
            {
                Debug.LogError("Assign the shelter session and HUD widgets.", this);
                enabled = false;
                return;
            }

            _session = ExpeditionSession.Resolve(_sessionPrefab);
            _session.Day.Changed += RefreshDay;
            _start.onClick.AddListener(StartExpedition);
            _skip.onClick.AddListener(SkipExpedition);
            _health.text = $"{_session.Health} / {_session.MaxHealth}";
            var report = _session.LastReport;
            _resultTitle.text = report == null ? "Вылазок ещё не было"
                : $"День {report.DayNumber} · {(report.ReturnedSafely ? "Доставлено на склад" : "Потеряно при поражении")}";
            if (report != null && report.Resources.Count > 0)
                FillRows(report.Resources, _resultNames, _resultQuantities);
            else if (report != null)
                _resultNames.text = report.ReturnedSafely ? "Рюкзак был пуст." : "Ресурсов в рюкзаке не было.";

            RefreshDay();
        }

        private void OnDestroy()
        {
            if (_session != null)
                _session.Day.Changed -= RefreshDay;
            if (_start != null)
                _start.onClick.RemoveListener(StartExpedition);
            if (_skip != null)
                _skip.onClick.RemoveListener(SkipExpedition);
        }

        private void StartExpedition()
        {
            if (_mode.CurrentMode == GameMode.Hub && !_session.IsLoading && _session.Day.Phase == DayPhase.Morning
                && !_session.TryStartExpedition())
                _status.text = "Не удалось начать вылазку.";
        }

        private void SkipExpedition()
        {
            if (_mode.CurrentMode == GameMode.Hub && _session.TrySkipExpedition())
                _status.text = "Вылазка пропущена.";
        }

        private void RefreshDay()
        {
            var day = _session.Day;
            string phase = day.Phase switch
            {
                DayPhase.Morning => "Утро",
                DayPhase.Expedition => "Вылазка",
                DayPhase.Evening => "Вечер",
                _ => string.Empty
            };
            _dayLabel.text = $"День {day.Number} · {phase}";
            _status.text = string.Empty;
            RefreshStock();
            bool morning = day.Phase == DayPhase.Morning;
            bool evening = day.Phase == DayPhase.Evening;
            _start.gameObject.SetActive(morning);
            _skip.gameObject.SetActive(morning);
            _openRations.gameObject.SetActive(evening);
            _workReport?.Refresh(_session.LastWorkReport, !_session.SuppressInitialWorkPopup);
            _session.SuppressInitialWorkPopup = false;
            FocusDayAction();
        }

        internal void FocusDayAction()
        {
            if (_mode.CurrentMode == GameMode.Hub && EventSystem.current != null && _dayLabel.gameObject.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(_session.Day.Phase switch
                {
                    DayPhase.Morning => _start.gameObject,
                    DayPhase.Evening => _openRations.gameObject,
                    _ => null
                });
        }

        internal void RefreshStock() => FillRows(_session.Stock, _stockNames, _stockQuantities);

        private static void FillRows(IEnumerable<ResourceAmount> resources, Text names, Text quantities)
        {
            var ordered = resources.OrderBy(entry => entry.Item.DisplayName).ToArray();
            names.text = string.Join("\n", ordered.Select(entry => entry.Item.DisplayName));
            quantities.text = string.Join("\n", ordered.Select(entry => entry.Quantity.ToString()));
        }
    }
}
