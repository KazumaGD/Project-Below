using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    [DisallowMultipleComponent]
    public sealed class WorkReportHud : MonoBehaviour
    {
        [SerializeField] private GameObject _home;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _open;
        [SerializeField] private Button _back;
        [SerializeField] private Text _openLabel;
        [SerializeField] private Text _heading;
        [SerializeField] private Text _empty;
        [SerializeField] private ScrollRect _list;
        [SerializeField] private Text _template;

        private readonly List<Text> _rows = new();
        private WorkReport _report;

        private void Awake()
        {
            _open.onClick.AddListener(Show);
            _back.onClick.AddListener(Back);
        }

        private void OnDestroy()
        {
            _open.onClick.RemoveListener(Show);
            _back.onClick.RemoveListener(Back);
        }

        internal void Refresh(WorkReport report, bool showAutomatically)
        {
            if (report == null || report == _report)
                return;

            _report = report;
            _open.gameObject.SetActive(true);
            _openLabel.text = $"Работа · день {report.DayNumber}";
            _heading.text = $"Работа обитателей · день {report.DayNumber}";
            bool hasAssignments = false;
            for (int i = 0; i < report.Results.Count; i++)
            {
                if (i == _rows.Count)
                    _rows.Add(Instantiate(_template, _list.content, false));

                var result = report.Results[i];
                var row = _rows[i];
                hasAssignments |= result.Task != null;
                string task = result.Task == null ? string.Empty : $" · {result.Task.DisplayName}";
                row.text = $"{result.Resident.DisplayName}{task}\n{WorkText.Outcome(result)}";
                row.gameObject.SetActive(true);
            }
            _empty.gameObject.SetActive(report.Results.Count == 0);
            if (hasAssignments && showAutomatically)
                Show();
        }

        private void Show()
        {
            _home.SetActive(false);
            _panel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            _list.verticalNormalizedPosition = 1f;
            EventSystem.current?.SetSelectedGameObject(_back.gameObject);
        }

        private void Back()
        {
            _panel.SetActive(false);
            _home.SetActive(true);
            EventSystem.current?.SetSelectedGameObject(_open.gameObject);
        }
    }
}
