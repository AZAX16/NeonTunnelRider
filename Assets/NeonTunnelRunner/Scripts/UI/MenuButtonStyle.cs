using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NeonTunnelRunner
{
    /// <summary>UI-only visual states; Button remains the command and navigation control.</summary>
    [DisallowMultipleComponent]
    public sealed class MenuButtonStyle : MonoBehaviour, ISelectHandler, IDeselectHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UnityEngine.UI.Image border, fill;
        [SerializeField] private TMP_Text label;
        private bool selected, hovered, pressed;
        private double submitUntil;
        private static readonly Color Ink = new Color32(7, 11, 34, 255);
        private static readonly Color Pale = new Color32(242, 245, 255, 255);
        private static readonly Color Cyan = new Color32(53, 230, 255, 255);
        private static readonly Color Muted = new Color32(154, 167, 201, 255);
        private void OnEnable() { selected = hovered = pressed = false; submitUntil = 0; Refresh(); }
        private void OnDisable() { selected = hovered = pressed = false; submitUntil = 0; }
        public void OnSelect(BaseEventData e) { selected = true; Refresh(); }
        public void OnDeselect(BaseEventData e) { selected = false; Refresh(); }
        public void OnPointerEnter(PointerEventData e) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { hovered = pressed = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = true; Refresh(); }
        public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }
        public void OnSubmit(BaseEventData e) { submitUntil = Time.unscaledTimeAsDouble + .12; Refresh(); }
        private void Update() { if (submitUntil > 0 && Time.unscaledTimeAsDouble >= submitUntil) { submitUntil = 0; Refresh(); } }
        private void Refresh()
        {
            if (!button || !border || !fill || !label) return;
            bool enabledControl = button.IsInteractable();
            bool down = enabledControl && (pressed || submitUntil > Time.unscaledTimeAsDouble);
            border.color = enabledControl && (selected || hovered || down) ? Cyan : Muted;
            fill.color = down ? Pale : selected || hovered ? new Color32(25, 57, 78, 255) : Ink;
            label.color = !enabledControl ? Muted : down ? Ink : Pale;
        }
    }
}
