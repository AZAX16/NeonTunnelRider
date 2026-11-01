namespace NeonTunnelRunner
{
    public sealed class StationaryCar : Interactable
    {
        protected override float WorldSpeed => 0f;
        protected override void OnInteract(InteractionContext context) => context.Survival.TryDamage(context.Encounter);
    }
}
