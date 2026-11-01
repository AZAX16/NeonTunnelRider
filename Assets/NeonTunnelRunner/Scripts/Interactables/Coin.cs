namespace NeonTunnelRunner
{
    public sealed class Coin : Interactable
    {
        public override InteractionCategory Category => InteractionCategory.Pickup;
        protected override void OnInteract(InteractionContext context)
        {
            HidePickup();
            try
            {
                int before=context.Session.Snapshot.Score;
                context.Progression.AddCoin();
                context.Session.PublishCollection(new CollectedEvent(context.Session.Snapshot.RunId,context.Encounter,
                    context.Progression.Owner.Configuration.CoinScore,context.Session.Snapshot.Score-before));
            }
            finally { RecyclePickup(context.Encounter.ActivationId); }
        }
    }
}
