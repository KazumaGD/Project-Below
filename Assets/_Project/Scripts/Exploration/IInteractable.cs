namespace ProjectBelow.Exploration
{
    public interface IInteractable
    {
        bool CanInteract { get; }
        string InteractionPrompt { get; }
        void Interact();
    }
}
