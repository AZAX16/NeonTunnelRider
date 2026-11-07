using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>The original forward mover and its faster seeded role share one pool/lifecycle.</summary>
    public sealed class MovingCar : Interactable
    {
        protected override float WorldSpeed => 8f;
        public bool IsOvertaker { get; private set; }
        public bool ChangingLane { get; private set; }
        public int TargetLane { get; private set; }
        public float DecisionRemaining { get; private set; }
        public double RetirementZ { get; private set; }
        private float lateralStart, lateralAge;
        private uint decisionIndex;
        private int trafficSeed;
        public ulong AcceptedLaneChanges { get; private set; }
        public override void OnRent(SpawnContext context)
        {
            base.OnRent(context);
            IsOvertaker=context.Speed>8f;
            if (IsOvertaker) Speed=context.Speed;
            TargetLane=Lane; ChangingLane=false; lateralAge=0f; lateralStart=CurrentPosition.x;
            trafficSeed=context.Seed; decisionIndex=0; AcceptedLaneChanges=0;
            RetirementZ=AbsoluteZ; DecisionRemaining=NextInterval();
        }
        internal void SetFollowingSpeed(float speed) { if (!IsOvertaker) Speed=Mathf.Clamp(speed,0f,8f); }
        internal void SetRetirement(double z) => RetirementZ=z;
        private float Sample() => WorldRandom.Sample(trafficSeed,SpawnSequence,0x54524146u,decisionIndex++)/4294967296f;
        private float NextInterval()
        {
            var range=Registry ? Registry.Gameplay.TrafficDecisionInterval : new Vector2(1.5f,4.5f);
            return Mathf.Lerp(range.x,range.y,Sample());
        }
        internal void DecideLane(float dt)
        {
            if (IsOvertaker && Registry)
            {
                DecisionRemaining-=dt;
                if (!ChangingLane && DecisionRemaining<=0f)
                {
                    DecisionRemaining=NextInterval();
                    int target=Lane+(Sample()<.5f ? -1 : 1);
                    if (target>=-1 && target<=1 && Registry.TryReserveLane(this,target))
                    {
                        TargetLane=target; lateralStart=CurrentPosition.x; lateralAge=0f;
                        ChangingLane=true; AcceptedLaneChanges++;
                    }
                }
            }
        }
        internal override void PredictStep(float dt,double offset)
        {
            base.PredictStep(dt,offset);
            if (!ChangingLane) return;
            lateralAge+=dt;
            var p=CurrentPosition;
            p.x=Mathf.Lerp(lateralStart,TargetLane*Registry.Gameplay.LaneWidth,lateralAge/Registry.Gameplay.TrafficLaneDuration);
            CurrentPosition=p;
            if (lateralAge>=Registry.Gameplay.TrafficLaneDuration) { Lane=TargetLane; ChangingLane=false; }
        }
        public override void OnReturn()
        {
            IsOvertaker=ChangingLane=false; TargetLane=0; DecisionRemaining=lateralAge=lateralStart=0f;
            RetirementZ=0d; decisionIndex=0; trafficSeed=0; AcceptedLaneChanges=0;
            base.OnReturn();
        }
        protected override void OnInteract(InteractionContext context) => context.Survival.TryDamage(context.Encounter);
    }
}
