using System;

namespace NeonTunnelRunner
{
    // Test-only pickup category; no rewards or future production pickup implementation.
    public sealed class InteractionProbe : Interactable
    {
        public Action<InteractionContext> Interacted;
        public override InteractionCategory Category => InteractionCategory.Pickup;
        protected override void OnInteract(InteractionContext context) => Interacted?.Invoke(context);
        public override void OnReturn() { Interacted=null; base.OnReturn(); }
    }
}
