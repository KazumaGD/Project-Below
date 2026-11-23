using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectBelow.Exploration
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private Camera _viewCamera;
        [SerializeField, Min(0f)] private float _interactionDistance = 2.5f;
        [SerializeField] private bool _interactionEnabled = true;

        private PlayerInput _playerInput;
        private InputAction _interactAction;

        public event Action PresentationChanged;
        public bool IsInteractionActive { get; private set; }
        public string CurrentPrompt { get; private set; }

        public bool InteractionEnabled
        {
            get => _interactionEnabled;
            set
            {
                _interactionEnabled = value;
                if (!value)
                    PublishPresentation(false, null);
            }
        }

        public string GetInteractionBindingLabel()
        {
            return _interactAction?.GetBindingDisplayString(
                InputBinding.DisplayStringOptions.DontIncludeInteractions,
                group: _playerInput.currentControlScheme);
        }

        private bool CanReadInput => isActiveAndEnabled && _interactionEnabled
            && _interactAction is { enabled: true }
            && _viewCamera != null && _viewCamera.isActiveAndEnabled
            && Application.isFocused && Cursor.lockState == CursorLockMode.Locked;

        private void Start()
        {
            _playerInput = GetComponent<PlayerInput>();
            _interactAction = _playerInput.actions?.FindActionMap("Player")?.FindAction("Interact");
            if (_viewCamera == null || _interactAction == null)
            {
                Debug.LogError("Assign a view camera and the Player/Interact input action.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            PublishPresentation(false, null);
        }

        private void LateUpdate()
        {
            if (!CanReadInput)
            {
                PublishPresentation(false, null);
                return;
            }

            // The template action uses Hold. Read the press edge so quick taps also interact.
            IInteractable target = FindTarget();
            if (target != null && _interactAction.WasPressedThisFrame())
            {
                target.Interact();
                target = CanReadInput ? FindTarget() : null;
            }

            PublishPresentation(CanReadInput, target?.InteractionPrompt);
        }

        private IInteractable FindTarget()
        {
            Vector3 direction = _viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f)).direction;
            if (!Physics.Raycast(_viewCamera.transform.position, direction, out RaycastHit hit,
                    _interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return null;

            // The nearest solid collider owns the hit, even when it is only an obstruction.
            var target = hit.collider.GetComponentInParent<IInteractable>();
            if (target is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                return null;

            return target?.CanInteract == true ? target : null;
        }

        private void PublishPresentation(bool active, string prompt)
        {
            if (IsInteractionActive == active && CurrentPrompt == prompt)
                return;

            IsInteractionActive = active;
            CurrentPrompt = prompt;
            PresentationChanged?.Invoke();
        }
    }
}
