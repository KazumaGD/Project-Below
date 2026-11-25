using ProjectBelow.Combat;
using UnityEngine;

namespace ProjectBelow.Exploration
{
    [DisallowMultipleComponent]
    public sealed class DungeonExit : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameModeController _gameMode;

        public bool CanInteract => isActiveAndEnabled && _gameMode != null && _gameMode.CurrentMode == GameMode.Exploration;
        public string InteractionPrompt => "Вернуться в убежище";

        public void Interact()
        {
            if (CanInteract)
                _gameMode.TryOpenReturnConfirmation();
        }
    }
}
