using System.Collections.Generic;
using ProjectBelow.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    [DisallowMultipleComponent]
    public sealed class RationsHud : MonoBehaviour
    {
        private static readonly Color NormalColor = new(0.89f, 0.72f, 0.47f);
        private static readonly Color OverBudgetColor = new(0.98f, 0.60f, 0.51f);

        [SerializeField] private HubHud _hub;
        [SerializeField] private GameObject _home;
        [SerializeField] private GameObject _page;
        [SerializeField] private ScrollRect _list;
        [SerializeField] private RationRowView _template;
        [SerializeField] private Text _day;
        [SerializeField] private Text _food;
        [SerializeField] private Text _budget;
        [SerializeField] private Text _empty;
        [SerializeField] private Button _open;
        [SerializeField] private Button _back;
        [SerializeField] private Button _confirm;

        private readonly List<RationRowView> _rows = new();
        private FoodRation[] _choices;
        private long _draftDay;

        private void Start()
        {
            if (_hub == null || _home == null || _page == null || _list == null || _list.content == null
                || _template == null || _day == null || _food == null || _budget == null || _empty == null
                || _open == null || _back == null || _confirm == null)
            {
                Debug.LogError("Assign the shelter ration widgets.", this);
                enabled = false;
                return;
            }
            _open.onClick.AddListener(Open);
            _back.onClick.AddListener(Close);
            _confirm.onClick.AddListener(Confirm);
        }

        private void OnDestroy()
        {
            if (_open != null)
                _open.onClick.RemoveListener(Open);
            if (_back != null)
                _back.onClick.RemoveListener(Close);
            if (_confirm != null)
                _confirm.onClick.RemoveListener(Confirm);
        }

        private void Open()
        {
            var session = _hub.Session;
            if (session == null || session.IsLoading || session.Day.Phase != DayPhase.Evening)
                return;

            if (_choices == null)
                BuildRows(session);
            if (_draftDay != session.Day.Number)
                _draftDay = session.Day.Number;
            for (int i = 0; i < _choices.Length; i++)
                _choices[i] = session.Residents[i].PlannedRation;
            _day.text = $"День {_draftDay} · Вечер";
            _home.SetActive(false);
            _page.SetActive(true);
            Refresh();
            Canvas.ForceUpdateCanvases();
            _list.verticalNormalizedPosition = 1f;
            EventSystem.current?.SetSelectedGameObject(_back.gameObject);
        }

        private void BuildRows(ExpeditionSession session)
        {
            _choices = new FoodRation[session.Residents.Count];
            for (int i = 0; i < session.Residents.Count; i++)
            {
                int index = i;
                var definition = session.Residents[i].Definition;
                var row = Instantiate(_template, _list.content, false);
                row.name = definition.DisplayName;
                row.Bind(definition, session.Rations, ration => Pick(index, ration));
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }
            _empty.gameObject.SetActive(_rows.Count == 0);
        }

        private void Pick(int index, FoodRation ration)
        {
            var session = _hub.Session;
            if (!session.TryPlanRation(session.Residents[index], ration))
                return;
            _choices[index] = ration;
            Refresh();
        }

        private void Refresh()
        {
            var session = _hub.Session;
            long total = 0;
            for (int i = 0; i < _rows.Count; i++)
            {
                var stats = session.Residents[i].Stats;
                var ration = _choices[i];
                total += session.Rations.GetCost(ration);
                _rows[i].Render(stats, session.Rations.Predict(stats, ration), ration);
            }
            long available = session.FoodStock;
            long balance = available - total;
            _food.text = $"В запасе  {available} Food   ·   Выбрано  {total}";
            _budget.text = balance < 0 ? $"Не хватает еды · {-balance}" : $"После раздачи · {balance} Food";
            _budget.color = balance < 0 ? OverBudgetColor : NormalColor;
            _confirm.interactable = balance >= 0 && !session.IsLoading
                && session.Day.Phase == DayPhase.Evening && session.Day.Number == _draftDay;
        }

        private void Confirm()
        {
            if (_hub.Session.TryEndDay(_draftDay, _choices))
                Close();
            else
                Refresh();
        }

        private void Close()
        {
            _page.SetActive(false);
            _home.SetActive(true);
            _hub.FocusDayAction();
        }
    }
}
