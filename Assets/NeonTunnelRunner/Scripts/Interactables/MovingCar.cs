namespace NeonTunnelRunner
{
    public sealed class MovingCar : Interactable
    {
        protected override float WorldSpeed => 8f;
        protected override void OnInteract(InteractionContext context) => context.Survival.TryDamage(context.Encounter);
    }
}
