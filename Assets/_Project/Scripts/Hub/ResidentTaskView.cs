using System.Collections.Generic;
using ProjectBelow.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    public sealed class ResidentTaskView : MonoBehaviour
    {
        [SerializeField] private Button _previous;
        [SerializeField] private Button _next;
        [SerializeField] private Text _task;
        [SerializeField] private Text _forecast;
        [SerializeField] private Text _details;

        private readonly List<TaskDefinition> _options = new();
        private ExpeditionSession _session;
        private CharacterState _resident;

        private void Awake()
        {
            _previous.onClick.AddListener(Previous);
            _next.onClick.AddListener(Next);
        }

        private void OnDestroy()
        {
            _previous.onClick.RemoveListener(Previous);
            _next.onClick.RemoveListener(Next);
        }

        internal void Render(ExpeditionSession session, CharacterState resident)
        {
            _session = session;
            _resident = resident;
            bool morning = session.Day.Phase == DayPhase.Morning && !session.IsLoading;
            _options.Clear();
            _options.Add(null);
            foreach (var task in session.Tasks)
                if (task.Allows(resident.Definition.Specialization))
                    _options.Add(task);

            bool isBuilding = session.IsBuilding(resident);
            bool canChoose = morning && !isBuilding && _options.Count > 1;
            _previous.gameObject.SetActive(canChoose);
            _next.gameObject.SetActive(canChoose);
            var current = resident.AssignedTask;
            _task.text = current == null ? "Без поручения" : current.DisplayName;
            _forecast.text = string.Empty;
            _details.text = morning ? string.Empty : "Поручения назначаются утром";
            if (!morning || current == null)
                return;

            foreach (var result in session.PreviewWork().Results)
            {
                if (result.Resident != resident.Definition)
                    continue;

                _forecast.text = result.Outcome == TaskOutcome.InvalidDefinition ? string.Empty : WorkText.Forecast(result);
                _details.text = isBuilding ? "Оплачено · завершится вечером"
                    : result.Outcome == TaskOutcome.Completed ? current.Description : WorkText.Outcome(result);
                break;
            }
        }

        private void Previous() => Choose(-1);
        private void Next() => Choose(1);

        private void Choose(int offset)
        {
            if (_resident == null || _options.Count < 2)
                return;

            int index = Mathf.Max(0, _options.IndexOf(_resident.AssignedTask));
            var next = _options[(index + offset + _options.Count) % _options.Count];
            if (_session.TryAssignTask(_resident, next))
                Render(_session, _resident);
        }
    }
}
