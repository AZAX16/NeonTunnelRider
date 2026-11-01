using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    // Pickups sort before obstacles only at equal TOI. Task 07 extends the same dispatch path.
    public enum InteractionCategory { Pickup = 0, Obstacle = 1 }

    public readonly struct Encounter
    {
        public PoolKey Kind { get; }
        public ulong ActivationId { get; }
        public long SpawnSequence { get; }
        public int Lane { get; }
        public double AbsoluteZ { get; }
        public Vector3 LocalPosition { get; }
        public double PlayingTime { get; }
        public Encounter(PoolKey kind, ulong activationId, long sequence, int lane, double absoluteZ, Vector3 position, double playingTime)
        { Kind=kind; ActivationId=activationId; SpawnSequence=sequence; Lane=lane; AbsoluteZ=absoluteZ; LocalPosition=position; PlayingTime=playingTime; }
    }

    public readonly struct InteractionContext
    {
        public RunSession Session { get; }
        public PlayerSurvival Survival { get; }
        public Encounter Encounter { get; }
        public InteractionContext(RunSession session, PlayerSurvival survival, Encounter encounter)
        { Session=session; Survival=survival; Encounter=encounter; }
    }

    [RequireComponent(typeof(BoxCollider))]
    public abstract class Interactable : PooledObject
    {
        [SerializeField] private Vector3 halfExtents = new Vector3(1.1f,0.6f,2.2f);
        [SerializeField] private Transform visual;
        private Vector3 visualPosition, visualScale;
        private Quaternion visualRotation;
        private bool visualCached;
        private Rigidbody body;
        private BoxCollider box;
        public SpawnDirector Registry { get; private set; }
        public bool Encountered { get; private set; }
        public int Lane { get; private set; }
        public float Speed { get; private set; }
        public long SpawnSequence { get; private set; }
        public Vector3 HalfExtents => halfExtents;
        public Vector3 PreviousPosition { get; private set; }
        public Vector3 CurrentPosition { get; private set; }
        public double PreviousAbsoluteZ { get; private set; }
        public double AbsoluteZ { get; private set; }
        public virtual InteractionCategory Category => InteractionCategory.Obstacle;
        protected virtual float WorldSpeed => 0f;

        public override void OnRent(SpawnContext context)
        {
            base.OnRent(context);
            box = GetComponent<BoxCollider>(); body = GetComponent<Rigidbody>();
            if (!visualCached && visual)
            { visualPosition=visual.localPosition; visualRotation=visual.localRotation; visualScale=visual.localScale; visualCached=true; }
            Encountered=false; Lane=context.Lane; Speed=WorldSpeed; SpawnSequence=context.SpawnSequence;
            PreviousPosition=CurrentPosition=context.LocalPosition;
            PreviousAbsoluteZ=AbsoluteZ=context.AbsoluteDistance;
            transform.SetPositionAndRotation(CurrentPosition,Quaternion.identity); transform.localScale=Vector3.one;
            RestoreVisual(); box.enabled=true;
            if (body) { body.position=CurrentPosition; body.rotation=Quaternion.identity; }
            Registry=context.Registry;
            if (Registry) Registry.Register(this,ActivationId);
        }
        public override void OnReturn()
        {
            if (Registry) Registry.Unregister(this,ActivationId);
            Registry=null; Encountered=false; Lane=0; Speed=0; SpawnSequence=0;
            PreviousPosition=CurrentPosition=Vector3.zero; PreviousAbsoluteZ=AbsoluteZ=0d;
            if (box) box.enabled=false;
            RestoreVisual();
            base.OnReturn();
        }
        private void RestoreVisual()
        { if (visual && visualCached) { visual.localPosition=visualPosition; visual.localRotation=visualRotation; visual.localScale=visualScale; } }

        public bool TryInteract(InteractionContext context)
        {
            if (!IsRented || Encountered || context.Encounter.ActivationId != ActivationId ||
                context.Encounter.SpawnSequence != SpawnSequence || context.Session == null || !context.Survival) return false;
            var runOwner=context.Survival.Owner;
            if (!runOwner || !runOwner.IsOwner || runOwner.IsTransitioning || runOwner.State!=GameState.Playing ||
                context.Session!=runOwner.Session || !Owner || Owner.Owner!=runOwner) return false;
            Encountered=true; // Before callbacks, including grace rejection.
            OnInteract(context);
            return true;
        }
        protected virtual void OnInteract(InteractionContext context) { }
        internal void PredictStep(float dt, double offset)
        {
            PreviousPosition=CurrentPosition; PreviousAbsoluteZ=AbsoluteZ;
            AbsoluteZ += (double)Speed*dt;
            var next=CurrentPosition; next.z=(float)(AbsoluteZ-offset); CurrentPosition=next;
        }
        internal void CommitPose()
        { if (body) body.MovePosition(CurrentPosition); else transform.position=CurrentPosition; }
        internal void ShiftOrigin(double offset)
        {
            var previous=PreviousPosition; previous.z=(float)(PreviousAbsoluteZ-offset); PreviousPosition=previous;
            var current=CurrentPosition; current.z=(float)(AbsoluteZ-offset); CurrentPosition=current;
            if (body) { var interpolation=body.interpolation; body.interpolation=RigidbodyInterpolation.None;
                body.position=current; body.rotation=Quaternion.identity; transform.SetPositionAndRotation(current,Quaternion.identity); body.interpolation=interpolation; }
            else transform.SetPositionAndRotation(current,Quaternion.identity);
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        { Gizmos.color=Encountered ? Color.gray : Color.yellow; Gizmos.DrawWireCube(Application.isPlaying && IsRented ? CurrentPosition : transform.position,halfExtents*2f); }
#endif
    }
}
