using ProjectBelow.Hub;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectBelow.UI
{
    public sealed class DialoguePortraitView : MonoBehaviour
    {
        [SerializeField] private Image _portrait;
        [SerializeField] private Graphic _highlight;
        [SerializeField] private Text _name;

        private static readonly Color Accent = new(0.89f, 0.72f, 0.47f);
        private static readonly Color Muted = new(0.62f, 0.57f, 0.53f);

        internal void Render(CharacterDefinition character, bool active)
        {
            _portrait.sprite = character.Portrait;
            _portrait.enabled = character.Portrait != null;
            _portrait.color = active ? Color.white : new Color(0.49f, 0.46f, 0.42f);
            _highlight.enabled = active;
            _name.text = character.DisplayName;
            _name.color = active ? Accent : Muted;
        }
    }
}
