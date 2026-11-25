using System.Collections.Generic;
using ProjectBelow.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectBelow.Hub
{
    [DisallowMultipleComponent]
    public sealed class FacilitiesHud : MonoBehaviour
    {
        [SerializeField] private HubHud _hub;
        [SerializeField] private GameObject _home;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button _open;
        [SerializeField] private Button _back;
        [SerializeField] private Text _status;
        [SerializeField] private ScrollRect _list;
        [SerializeField] private FacilityCardView _template;

        private readonly List<FacilityCardView> _cards = new();
        private ExpeditionSession _session;

        private void Awake()
        {
            _open.onClick.AddListener(Show);
            _back.onClick.AddListener(Back);
        }

        private void OnDestroy()
        {
            _open.onClick.RemoveListener(Show);
            _back.onClick.RemoveListener(Back);
            if (_session != null)
                _session.Day.Changed -= Refresh;
        }

        private void Show()
        {
            if (_hub.Session == null || _hub.Session.IsLoading)
                return;

            if (_session == null)
            {
                _session = _hub.Session;
                _session.Day.Changed += Refresh;
                foreach (var facility in _session.Facilities)
                {
                    var card = Instantiate(_template, _list.content, false);
                    card.name = facility.Definition.DisplayName;
                    card.Bind(facility, TryBuild);
                    card.gameObject.SetActive(true);
                    _cards.Add(card);
                }
            }

            _home.SetActive(false);
            _panel.SetActive(true);
            Refresh();
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

        private void TryBuild(FacilityState facility)
        {
            if (_session.TryStartConstruction(facility))
                _hub.RefreshStock();
            Refresh();
        }

        private void Refresh()
        {
            if (!_panel.activeSelf)
                return;

            string status = "Строительство не запланировано";
            var order = _session.Construction;
            if (order != null)
            {
                string project = $"{order.Facility.Definition.DisplayName}, уровень {order.TargetLevel}";
                status = order.IsCompleted ? $"Завершено · {project} · бонус с дня {order.Facility.BenefitsFromDay}"
                    : $"В работе · {project} · {order.Builder.Definition.DisplayName} · до вечера";
            }
            _status.text = _cards.Count == 0 ? "В убежище пока нет помещений" : status;
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].Render(_session, _session.Facilities[i]);
        }
    }
}
