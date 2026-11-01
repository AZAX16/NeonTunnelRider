namespace NeonTunnelRunner
{
    public sealed class FuelCan : Interactable
    {
        public override InteractionCategory Category => InteractionCategory.Pickup;
        protected override void OnInteract(InteractionContext context)
        {
            HidePickup();
            try
            {
                float before=context.Session.Snapshot.Fuel;
                float amount=context.Survival.Owner.Configuration.FuelRefill;
                context.Survival.AddFuel(amount);
                context.Session.PublishCollection(new CollectedEvent(context.Session.Snapshot.RunId,context.Encounter,
                    amount,context.Session.Snapshot.Fuel-before));
            }
            finally { RecyclePickup(context.Encounter.ActivationId); }
        }
    }
}
