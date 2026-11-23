using ProjectBelow.Exploration;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    [DisallowMultipleComponent]
    public sealed class InteractionHud : MonoBehaviour
    {
        [SerializeField] private PlayerInteraction _interaction;
        [SerializeField] private GameObject _crosshair;
        [SerializeField] private GameObject _promptPanel;
        [SerializeField] private Text _prompt;

        private void OnEnable()
        {
            if (_interaction == null || _crosshair == null || _promptPanel == null || _prompt == null)
            {
                Debug.LogError("Assign the interaction controller, crosshair and prompt text.", this);
                enabled = false;
                return;
            }

            _interaction.PresentationChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_interaction != null)
                _interaction.PresentationChanged -= Refresh;
        }

        private void Refresh()
        {
            _crosshair.SetActive(_interaction.IsInteractionActive);

            string prompt = _interaction.CurrentPrompt;
            bool showPrompt = !string.IsNullOrEmpty(prompt);
            if (showPrompt)
            {
                string binding = _interaction.GetInteractionBindingLabel();
                _prompt.text = string.IsNullOrEmpty(binding) ? prompt : $"[{binding}]  {prompt}";
            }

            _promptPanel.SetActive(showPrompt);
        }
    }
}
