using UnityEngine;

namespace ProjectBelow.Exploration
{
    [DisallowMultipleComponent]
    public sealed class TestLever : MonoBehaviour, IInteractable
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Transform _handlePivot;
        [SerializeField] private Renderer _indicator;
        [SerializeField] private AudioSource _switchSound;
        [SerializeField] private bool _isOn;

        private MaterialPropertyBlock _indicatorProperties;

        public bool CanInteract => isActiveAndEnabled && _handlePivot != null && _indicator != null;
        public string InteractionPrompt => _isOn ? "Выключить индикатор" : "Включить индикатор";

        public void Interact()
        {
            if (!CanInteract)
                return;

            _isOn = !_isOn;
            ApplyState();
            if (_switchSound != null && _switchSound.clip != null)
                _switchSound.PlayOneShot(_switchSound.clip);
        }

        private void Awake()
        {
            if (_handlePivot == null || _indicator == null)
            {
                Debug.LogError("Assign the test lever's handle pivot and indicator renderer.", this);
                enabled = false;
                return;
            }

            _indicatorProperties = new MaterialPropertyBlock();
            ApplyState();
        }

        private void ApplyState()
        {
            _handlePivot.localRotation = Quaternion.Euler(0f, 0f, _isOn ? -30f : 30f);
            _indicator.GetPropertyBlock(_indicatorProperties);
            _indicatorProperties.SetColor(BaseColor,
                _isOn ? new Color(0.20f, 0.95f, 0.45f) : new Color(0.95f, 0.28f, 0.18f));
            _indicator.SetPropertyBlock(_indicatorProperties);
        }
    }
}
