using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    public readonly struct TrafficLease
    {
        public Interactable Item { get; }
        public ulong ActivationId { get; }
        public bool IsCurrent => Item && Item.IsRented && Item.ActivationId==ActivationId;
        public TrafficLease(Interactable item,ulong token) { Item=item; ActivationId=token; }
    }
    public readonly struct SpawnDiagnostic
    {
        public TrafficCandidate Candidate { get; }
        public PoolKey Kind { get; }
        public PatternRejection Rejection { get; }
        public PoolRentFailure Miss { get; }
        public PickupPlacement Pickup { get; }
        public bool IsPickup { get; }
        public long Sequence => IsPickup ? Pickup.SpawnSequence : Candidate.SpawnSequence;
        public double Ahead { get; }
        public double Behind { get; }
        public float MinimumSpeed { get; }
        public float MaximumSpeed { get; }
        public float Probability { get; }
        internal SpawnDiagnostic(TrafficCandidate candidate,PoolKey kind,PatternRejection rejection,PoolRentFailure miss,WorldConfig world,GameplayConfig gameplay)
        { Candidate=candidate; Kind=kind; Rejection=rejection; Miss=miss; Pickup=default; IsPickup=false; Ahead=world.Ahead; Behind=world.Behind; MinimumSpeed=gameplay.StartingSpeed; MaximumSpeed=gameplay.MaximumSpeed; Probability=ProgressionSystem.Occupancy(ProgressionSystem.EvaluateDifficulty(0d,candidate.AbsolutePosition,gameplay),gameplay); }
        internal SpawnDiagnostic(PickupPlacement pickup,PatternRejection rejection,PoolRentFailure miss,WorldConfig world,GameplayConfig gameplay)
        { Candidate=pickup.DiagnosticCandidate; Kind=pickup.Kind; Rejection=rejection; Miss=miss; Pickup=pickup; IsPickup=true; Ahead=world.Ahead; Behind=world.Behind; MinimumSpeed=gameplay.StartingSpeed; MaximumSpeed=gameplay.MaximumSpeed; Probability=ProgressionSystem.Occupancy(ProgressionSystem.EvaluateDifficulty(0d,pickup.AbsolutePosition,gameplay),gameplay); }
    }
    /// <summary>Deterministic block data and independently leased traffic. All simulation is loop-owned.</summary>
    [DisallowMultipleComponent]
    public sealed class SpawnDirector : MonoBehaviour, IWorldOriginParticipant
    {
        [SerializeField] private WorldConfig configuration;
        [SerializeField] private GameplayConfig gameplay;
        [SerializeField] private PoolService pools;
        [SerializeField] private Transform activeRoot;
        private TrafficLease[] active = Array.Empty<TrafficLease>();
        private TrafficLease[] deferred = Array.Empty<TrafficLease>();
        private int deferredCount;
        private PlayerController trafficPlayer;
        private double nextRearAttempt;
        private ulong rearSequence;
        private struct RearWarning
        { internal bool Active; internal int Lane; internal float Remaining, Margin; internal ulong Sequence; }
        private readonly RearWarning[] rearWarnings=new RearWarning[2];
        public ulong WarnedOvertakers { get; private set; }
        public int RearWarningCount { get { int n=0;for(int i=0;i<rearWarnings.Length;i++)if(rearWarnings[i].Active)n++;return n; } }
        public float RearWarningRemaining(int lane)
        { float left=0f;for(int i=0;i<rearWarnings.Length;i++)if(rearWarnings[i].Active && rearWarnings[i].Lane==lane)left=Mathf.Max(left,rearWarnings[i].Remaining);return left; }
        internal void AdvanceRearWarnings(float realPlayingSeconds)
        {
            if (!pools || !pools.Owner || pools.Owner.State!=GameState.Playing || !float.IsFinite(realPlayingSeconds) || realPlayingSeconds<=0f) return;
            for(int i=0;i<rearWarnings.Length;i++)
            {
                var warning=rearWarnings[i];if(!warning.Active)continue;
                warning.Remaining=Mathf.Max(0f,warning.Remaining-realPlayingSeconds);
                rearWarnings[i]=warning;if(warning.Remaining>0f)continue;
                rearWarnings[i]=default;
                double playerZ=pools.Owner.Session.Snapshot.Distance,offset=pools.Owner.Loop.Origin.Offset;
                float playerSpeed=pools.Owner.Session.Snapshot.CruiseSpeed,speed=playerSpeed+warning.Margin;
                double z=playerZ-16d,horizon=26d/Mathf.Max(1f,warning.Margin-1f),end=z+speed*horizon;
                if (!Forecast(null,warning.Lane,warning.Lane,z,end,speed,horizon,playerZ,playerSpeed)) { RejectedOvertakers++;continue; }
                var context=new SpawnContext(PoolKey.Moving,new Vector3(warning.Lane*gameplay.LaneWidth,gameplay.StartingPosition.y,(float)(z-offset)),
                    Quaternion.identity,activeRoot,warning.Lane,z,seed,speed,checked((1L<<60)+(long)warning.Sequence),this);
                if(!pools.TryRent(context,out var rented)) { TrafficPoolMisses++;continue; }
                ((MovingCar)rented).SetRetirement(end);SpawnedOvertakers++;
            }
        }
        private const uint TrafficStream=0x54524146u;
        public ulong SpawnedOvertakers { get; private set; }
        public ulong RejectedOvertakers { get; private set; }
        public ulong RejectedLaneChanges { get; private set; }
        private readonly ulong[] rearRejections=new ulong[5];
        // 0=range, 1=player lane, 2=transition, 3=protected route, 4=traffic trajectory.
        public ulong RearRejectionCount(int reason) => rearRejections[reason];
        private bool RejectRear(int reason) { rearRejections[reason]++; return false; }
        public ulong AcceptedLaneChanges { get; private set; }
        public ulong CompletedOvertakes { get; private set; }
        internal void BindPlayer(PlayerController player) => trafficPlayer=player;
        private long nextRow;
        private long nextCoinSlot, nextFuelIndex, fuelTransactionStart;
        private TrafficLease[] stagedFuel = Array.Empty<TrafficLease>();
        private int stagedFuelCount;
        private readonly SpawnDiagnostic[] diagnostics = new SpawnDiagnostic[64];
        private int diagnosticCursor;
        [Header("Latest bounded spawn diagnostic (read only during play)")]
        [SerializeField] private int diagnosticSeed;
        [SerializeField] private long diagnosticBlock;
        [SerializeField] private long diagnosticSequence;
        [SerializeField] private PatternRejection diagnosticRejection;
        [SerializeField] private PoolRentFailure diagnosticMiss;
        public PoolService Pools => pools;
        public Transform OriginRoot => activeRoot;
        public int ActiveCount { get; private set; }
        public int Capacity => active.Length;
        public bool IsDispatching { get; private set; }
        public int DiagnosticCount { get; private set; }
        public ulong SpawnedStationary { get; private set; }
        public ulong SpawnedMoving { get; private set; }
        public ulong Omitted { get; private set; }
        public ulong EligibleSlots { get; private set; }
        public ulong ProposedTraffic { get; private set; }
        public ulong AcceptedTraffic => ProposedTraffic - RejectedTraffic;
        public ulong RejectedTraffic { get; private set; }
        public ulong TrafficPoolMisses { get; private set; }
        public ulong MovingFallbacks { get; private set; }
        public ulong SpawnedCoins { get; private set; }
        public ulong SpawnedFuel { get; private set; }
        public ulong OmittedCoins { get; private set; }
        public ulong FuelFailures { get; private set; }
        public ulong FuelReclaims { get; private set; }
        public long NextFuelIndex => nextFuelIndex;
        public string LastError { get; private set; }
        public TrafficLease GetActive(int index) => index >= 0 && index < ActiveCount ? active[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public SpawnDiagnostic GetDiagnostic(int index) => index >= 0 && index < DiagnosticCount ? diagnostics[(diagnosticCursor-DiagnosticCount+index+diagnostics.Length)%diagnostics.Length] : throw new ArgumentOutOfRangeException(nameof(index));
        internal void Initialize()
        {
            int capacity=0, fuelCapacity=0;
            if (pools && pools.Configuration)
                for (int i=0;i<pools.Configuration.EntryCount;i++)
                { var entry=pools.Configuration.GetEntry(i); if (entry.Prefab && entry.Prefab.GetComponent<Interactable>()) capacity+=entry.Capacity; if (entry.Key==PoolKey.Fuel) fuelCapacity=entry.Capacity; }
            active=new TrafficLease[capacity]; deferred=new TrafficLease[capacity];
            stagedFuel=new TrafficLease[fuelCapacity];
        }
        public bool ValidateRequired(out string error)
        {
            error=null;
            if (!pools || !activeRoot || !activeRoot.gameObject.activeInHierarchy || activeRoot.position != Vector3.zero ||
                activeRoot.rotation != Quaternion.identity || activeRoot.lossyScale != Vector3.one)
            { error="Assign neutral independent traffic root and PoolService."; return false; }
            foreach (var key in new[] { PoolKey.Stationary, PoolKey.Moving, PoolKey.Coin, PoolKey.Fuel })
            {
                bool found=false;
                for (int i=0;i<pools.Configuration.EntryCount;i++)
                {
                    var entry=pools.Configuration.GetEntry(i);
                    if (entry.Key != key) continue;
                    var car=entry.Prefab.GetComponent<Interactable>(); var box=entry.Prefab.GetComponent<BoxCollider>();
                    var body=entry.Prefab.GetComponent<Rigidbody>();
                    bool pickup=key==PoolKey.Coin || key==PoolKey.Fuel;
                    found=car && (key==PoolKey.Stationary ? car is StationaryCar : key==PoolKey.Moving ? car is MovingCar : key==PoolKey.Coin ? car is Coin : car is FuelCan) &&
                        entry.Capacity >= (key==PoolKey.Stationary ? 64 : key==PoolKey.Moving ? 32 : key==PoolKey.Coin ? 160 : 24) && box && box.isTrigger && box.center==Vector3.zero &&
                        box.size==car.HalfExtents*2f && car.HalfExtents==(pickup ? PatternValidator.PickupHalfExtents : new Vector3(1.1f,0.6f,2.2f)) &&
                        entry.Prefab.transform.localScale==Vector3.one && entry.Prefab.transform.rotation==Quaternion.identity &&
                        (!pickup || entry.Prefab.transform.Find("Visual")) &&
                        (key!=PoolKey.Moving || (body && body.isKinematic && !body.useGravity));
                }
                if (!found) { error="Missing/invalid required interactable pool "+key+" (64 stationary / 32 moving / 160 coin / 24 fuel)."; return false; }
            }
            if (gameplay.StartingSpeed <= 8f || !float.IsFinite(gameplay.StartingSpeed))
            { error="Minimum player speed must exceed maximum traffic speed 8 m/s."; return false; }
            return true;
        }
        internal void Register(Interactable item, ulong token)
        {
            if (IsDispatching) throw new InvalidOperationException("Cannot register during contact dispatch.");
            for (int i=0;i<ActiveCount;i++) if (active[i].Item==item) throw new InvalidOperationException("Duplicate traffic registration.");
            if (ActiveCount==active.Length) throw new InvalidOperationException("Traffic registry capacity overflow.");
            active[ActiveCount++]=new TrafficLease(item,token);
        }
        internal void Unregister(Interactable item, ulong token)
        {
            if (IsDispatching) throw new InvalidOperationException("Cannot unregister during contact dispatch.");
            for (int i=0;i<ActiveCount;i++) if (active[i].Item==item && active[i].ActivationId==token)
            { Array.Copy(active,i+1,active,i,ActiveCount-i-1); active[--ActiveCount]=default; return; }
        }
        public bool DeferReturn(Interactable item, ulong token)
        {
            var lease=new TrafficLease(item,token);
            if (!lease.IsCurrent || item.Registry != this) return false;
            if (!IsDispatching) return item.ReturnToPool(token);
            for (int i=0;i<deferredCount;i++) if (deferred[i].Item==item && deferred[i].ActivationId==token) return true;
            if (deferredCount==deferred.Length) throw new InvalidOperationException("Deferred traffic return capacity overflow.");
            deferred[deferredCount++]=lease; return true;
        }
        internal void BeginDispatch() { if (IsDispatching) throw new InvalidOperationException("Nested contact dispatch."); IsDispatching=true; }
        internal void EndDispatch()
        {
            IsDispatching=false;
            for (int i=0;i<deferredCount;i++) { var lease=deferred[i]; deferred[i]=default; if (lease.IsCurrent) lease.Item.ReturnToPool(lease.ActivationId); }
            deferredCount=0;
        }
        internal void PredictTraffic(float dt,double offset)
        {
            // Original movers yield before a full swept rear-end envelope, retaining 8m/s when clear.
            for (int i=0;i<ActiveCount;i++)
            {
                if (!active[i].IsCurrent || !(active[i].Item is MovingCar car) || car.IsOvertaker) continue;
                float speed=8f;
                for (int j=0;j<ActiveCount;j++)
                {
                    var other=active[j].Item;
                    if (i==j || !active[j].IsCurrent || other.Category!=InteractionCategory.Obstacle ||
                        Mathf.Abs(other.CurrentPosition.x-car.CurrentPosition.x)>car.HalfExtents.x+other.HalfExtents.x) continue;
                    double gap=other.AbsoluteZ-car.AbsoluteZ-car.HalfExtents.z-other.HalfExtents.z-.2d;
                    if (other.AbsoluteZ>car.AbsoluteZ) speed=Mathf.Min(speed,(float)Math.Max(0d,gap/Math.Max(dt,.7f)));
                }
                car.SetFollowingSpeed(speed);
            }
            // Admit all lateral reservations against the same pre-movement poses. Each
            // accepted target is visible to later decisions; no actor sees half-updated Z.
            for (int i=0;i<ActiveCount;i++)
                if (active[i].IsCurrent && active[i].Item is MovingCar mover) mover.DecideLane(dt);
            for (int i=0;i<ActiveCount;i++) { var lease=active[i]; if (!lease.IsCurrent) throw new InvalidOperationException("Stale traffic lease."); lease.Item.PredictStep(dt,offset); }
        }
        internal void CommitTraffic() { for (int i=0;i<ActiveCount;i++) if (active[i].IsCurrent) active[i].Item.CommitPose(); }
        internal void HoldTraffic() { for (int i=0;i<ActiveCount;i++) if (active[i].IsCurrent) active[i].Item.HoldPose(); }
        public void ShiftOrigin(double newOffset,double delta)
        { for (int i=0;i<ActiveCount;i++) if (active[i].IsCurrent) active[i].Item.ShiftOrigin(newOffset); }
        internal void ReleaseAll()
        {
            if (IsDispatching) throw new InvalidOperationException("Reset during contact dispatch is forbidden.");
            Array.Clear(deferred,0,deferred.Length); deferredCount=0;
            while (ActiveCount>0) { var lease=active[ActiveCount-1]; if (lease.IsCurrent) lease.Item.ReturnToPool(lease.ActivationId); else active[--ActiveCount]=default; }
        }
        public PickupPlacement FuelAt(long index)
        {
            if (index<0) throw new ArgumentOutOfRangeException(nameof(index));
            return PlanAt(index/BlockPlan.FuelCount).GetFuel((int)(index%BlockPlan.FuelCount));
        }
        /// <summary>Reserve actual typed fuel rentals before any travel, including catch-up ticks.
        /// The transaction is rolled back if road preflight subsequently fails.</summary>
        internal bool PrepareFuelSupply(double playerZ,double targetZ,double offset)
        {
            if (stagedFuelCount!=0) throw new InvalidOperationException("Uncommitted fuel transaction.");
            LastError=null; fuelTransactionStart=nextFuelIndex;
            double limit=targetZ+configuration.Ahead;
            if (!double.IsFinite(limit) || limit<0d || limit/WorldConfig.BlockLength>long.MaxValue/32d)
                return FailFuel("Required fuel window is not representable.");
            long block=(long)Math.Floor(limit/WorldConfig.BlockLength);
            double local=limit-block*WorldConfig.BlockLength;
            long count=checked(block*3+(local>=450d ? 3 : local>=300d ? 2 : local>=120d ? 1 : 0));
            if (count-nextFuelIndex>stagedFuel.Length)
                return FailFuel("Required fuel window exceeds reserved capacity; movement stopped.");
            for (long index=nextFuelIndex;index<count;index++)
            {
                var placement=FuelAt(index);
                if (!PatternValidator.ValidatePickup(this,placement,out var reason))
                { Record(placement,reason,PoolRentFailure.None); RollbackFuelSupply(); return FailFuel("Required fuel reservation failed validation."); }
                var context=PickupContext(placement,offset);
                if (!pools.TryRent(context,out var item))
                {
                    Record(placement,PatternRejection.None,pools.LastRentFailure);
                    // Reclaim only our own full bounds behind the current player. Never steal an
                    // active opportunity, a foreign owner's rental, or use a coin as fuel.
                    for (int i=ActiveCount-1;i>=0;i--)
                    {
                        var lease=active[i];
                        if (lease.IsCurrent && lease.Item is FuelCan && lease.Item.AbsoluteZ+lease.Item.HalfExtents.z<playerZ-configuration.Behind)
                        { lease.Item.ReturnToPool(lease.ActivationId); FuelReclaims++; }
                    }
                    if (!pools.TryRent(context,out item))
                    { Record(placement,PatternRejection.None,pools.LastRentFailure); RollbackFuelSupply(); return FailFuel("Required fuel rental failed after safe reclaim: "+pools.LastRentFailure+". Movement stopped."); }
                }
                stagedFuel[stagedFuelCount++]=new TrafficLease((Interactable)item,item.ActivationId);
                nextFuelIndex=index+1;
            }
            return true;
        }
        internal void CommitFuelSupply()
        { SpawnedFuel+=(ulong)stagedFuelCount; Array.Clear(stagedFuel,0,stagedFuelCount); stagedFuelCount=0; }
        internal void RollbackFuelSupply()
        {
            for (int i=stagedFuelCount-1;i>=0;i--) { var lease=stagedFuel[i]; if (lease.IsCurrent) lease.Item.ReturnToPool(lease.ActivationId); stagedFuel[i]=default; }
            stagedFuelCount=0; nextFuelIndex=fuelTransactionStart;
        }
        private bool FailFuel(string error) { LastError=error; FuelFailures++; return false; }
        private SpawnContext PickupContext(PickupPlacement placement,double offset) => new SpawnContext(placement.Kind,
            new Vector3(placement.Lane*gameplay.LaneWidth,gameplay.StartingPosition.y,(float)(placement.AbsolutePosition-offset)),
            Quaternion.identity,activeRoot,placement.Lane,placement.AbsolutePosition,seed,0f,placement.SpawnSequence,this);
        private void MaintainCoins(double playerZ,double offset)
        {
            long firstBlock=Math.Max(0L,(long)Math.Floor(playerZ/WorldConfig.BlockLength));
            long lastBlock=(long)Math.Floor((playerZ+configuration.Ahead)/WorldConfig.BlockLength);
            long first=Math.Max(nextCoinSlot,firstBlock*BlockPlan.CoinSlots);
            for (long index=first;index<(lastBlock+1)*BlockPlan.CoinSlots;index++)
            {
                var placement=PlanAt(index/BlockPlan.CoinSlots).GetCoin((int)(index%BlockPlan.CoinSlots));
                if (placement.AbsolutePosition>playerZ+configuration.Ahead) break;
                nextCoinSlot=index+1;
                // Optional misses/skipped content are final; never retry close to the player.
                if (placement.AbsolutePosition<playerZ+30d) { OmittedCoins++; continue; }
                if (!PatternValidator.ValidatePickup(this,placement,out var rejection))
                { OmittedCoins++; Record(placement,rejection,PoolRentFailure.None); continue; }
                if (!pools.TryRent(PickupContext(placement,offset),out _))
                { OmittedCoins++; Record(placement,PatternRejection.None,pools.LastRentFailure); }
                else SpawnedCoins++;
            }
        }
        internal void MaintainTraffic(double playerZ,double offset)
        {
            for (int i=ActiveCount-1;i>=0;i--) { var lease=active[i]; if (!lease.IsCurrent) continue;
                var mover=lease.Item as MovingCar;
                if (mover && mover.IsOvertaker)
                {
                    if (mover.AbsoluteZ>=mover.RetirementZ || mover.AbsoluteZ>playerZ+10d ||
                        mover.AbsoluteZ-mover.HalfExtents.z<playerZ-configuration.Behind)
                    { if (mover.AbsoluteZ>playerZ+4d) CompletedOvertakes++; mover.ReturnToPool(lease.ActivationId); }
                }
                else if (lease.Item.AbsoluteZ+lease.Item.HalfExtents.z < playerZ-configuration.Behind) lease.Item.ReturnToPool(lease.ActivationId); }
            long first=Math.Max(nextRow,Math.Max(0L,(long)Math.Ceiling((playerZ+180d)/60d)));
            // Initial fill includes rows ahead of the player. Skipped catch-up rows are omitted, never spawned nearby.
            long last=(long)Math.Floor((playerZ+configuration.Ahead)/60d);
            for (long absoluteRow=first;absoluteRow<=last;absoluteRow++)
            {
                long block=absoluteRow/8; int row=(int)(absoluteRow%8);
                if (row>=WorldConfig.RowsPerBlock) continue;
                var plan=PlanAt(block);
                for (int lane=-1;lane<=1;lane++)
                {
                    if (lane != plan.ProtectedLane && absoluteRow*60d >= 180d) EligibleSlots++;
                    if (!plan.TryGetCandidate(row,lane,out var candidate)) continue;
                    ProposedTraffic++;
                    if (!TrySelectTraffic(candidate,playerZ,out var key,out var rejection))
                    { Omitted++; RejectedTraffic++; Record(candidate,key,rejection,PoolRentFailure.None); continue; }
                    if (rejection!=PatternRejection.None) { MovingFallbacks++; Record(candidate,key,rejection,PoolRentFailure.None); }
                    var position=new Vector3(lane*gameplay.LaneWidth,gameplay.StartingPosition.y,(float)(candidate.AbsolutePosition-offset));
                    var context=new SpawnContext(key,position,Quaternion.identity,activeRoot,lane,candidate.AbsolutePosition,seed,
                        key==PoolKey.Moving ? 8f : 0f,candidate.SpawnSequence,this);
                    if (!pools.TryRent(context,out _)) { Omitted++; TrafficPoolMisses++; Record(candidate,key,PatternRejection.None,pools.LastRentFailure); }
                    else if (key==PoolKey.Moving) SpawnedMoving++; else SpawnedStationary++;
                }
            }
            nextRow=Math.Max(nextRow,last+1);
            MaintainCoins(playerZ,offset);
            MaintainRearTraffic(playerZ,offset);
        }
        private void MaintainRearTraffic(double playerZ,double offset)
        {
            if (!trafficPlayer || playerZ<220d || playerZ<nextRearAttempt) return;
            nextRearAttempt=playerZ+12d;
            ulong sequence=rearSequence++;
            int count=0;
            for (int i=0;i<ActiveCount;i++) if (active[i].IsCurrent && active[i].Item is MovingCar m && m.IsOvertaker) count++;
            if (count+RearWarningCount>=2) return;
            float margin=Mathf.Lerp(gameplay.OvertakerMargin.x,gameplay.OvertakerMargin.y,
                WorldRandom.Sample(seed,(long)sequence,TrafficStream,0)/4294967296f);
            float playerSpeed=trafficPlayer.Configuration.StartingSpeed;
            if (pools.Owner && pools.Owner.Session!=null) playerSpeed=pools.Owner.Session.Snapshot.CruiseSpeed;
            float speed=playerSpeed+margin;
            double z=playerZ-16d;
            // Full pass, from off-camera rear to 10m ahead. A small allowance covers speed progression.
            double horizon=26d/Mathf.Max(1f,margin-1f);
            double end=z+speed*horizon;
            int lane=(int)(WorldRandom.Sample(seed,(long)sequence,TrafficStream,1)%3)-1;
            bool safe=false;
            for (int attempt=0;attempt<3;attempt++)
            {
                int proposed=(lane+1+attempt)%3-1;
                if (RearWarningRemaining(proposed)>0f || !Forecast(null,proposed,proposed,z,end,speed,horizon,playerZ,playerSpeed)) continue;
                lane=proposed; safe=true; break;
            }
            if (!safe) { RejectedOvertakers++; return; }
            for(int i=0;i<rearWarnings.Length;i++)
            {
                if(rearWarnings[i].Active)continue;
                rearWarnings[i]=new RearWarning { Active=true,Lane=lane,Remaining=gameplay.RearWarningDuration,Margin=margin,Sequence=sequence };
                WarnedOvertakers++;break;
            }
        }
        internal bool TryReserveLane(MovingCar car,int target)
        {
            double remaining=Mathf.Max(0f,(float)((car.RetirementZ-car.AbsoluteZ)/car.Speed));
            double playerZ=trafficPlayer ? trafficPlayer.CurrentPosition.z+pools.Owner.Loop.Origin.Offset : car.AbsoluteZ;
            float playerSpeed=pools.Owner.Session.Snapshot.CruiseSpeed;
            // No late cut-ins: reserve the entire lateral sweep and the remaining pass, plus reaction time.
            if (remaining<gameplay.TrafficLaneDuration+configuration.ReactionTime ||
                !Forecast(car,car.Lane,target,car.AbsoluteZ,car.RetirementZ,car.Speed,remaining,playerZ,playerSpeed))
            { RejectedLaneChanges++; return false; }
            AcceptedLaneChanges++; return true;
        }
        private bool Forecast(MovingCar self,int from,int to,double z,double end,float speed,double horizon,double playerZ,float playerSpeed)
        {
            const double pad=4.5d;
            long first=(long)Math.Floor((z-pad)/480d),last=(long)Math.Floor((end+pad)/480d);
            if (first<0 || last-first>2 || horizon<=0d) return RejectRear(0);
            float minX=Mathf.Min(from,to)*gameplay.LaneWidth, maxX=Mathf.Max(from,to)*gameplay.LaneWidth;
            // Protect an otherwise stationary lane holder for the entire pass, not just today's gap.
            if (trafficPlayer)
            {
                float currentX=trafficPlayer.CurrentPosition.x;
                int destination=Mathf.Clamp(trafficPlayer.LaneIndex+trafficPlayer.QueuedCommand,-1,1);
                float destinationX=destination*gameplay.LaneWidth;
                if (Mathf.Max(currentX,destinationX)>=minX-1.95f && Mathf.Min(currentX,destinationX)<=maxX+1.95f)
                    return RejectRear(1);
            }
            for (long block=first;block<=last;block++)
            {
                var plan=PlanAt(block);
                if (end+pad>=plan.BufferStart && z-pad<=plan.AbsoluteEnd) return RejectRear(2);
                if (plan.ProtectedLane>=Math.Min(from,to) && plan.ProtectedLane<=Math.Max(from,to)) return RejectRear(3);
                // Already-introduced rows are represented by real leases below, not ghost
                // maximum-travel envelopes. Future rows are forecast at their introduction time.
                for (int row=0;row<WorldConfig.RowsPerBlock;row++)
                for (int lane=Math.Min(from,to);lane<=Math.Max(from,to);lane++)
                {
                    if (block*8+row<nextRow || !plan.TryGetCandidate(row,lane,out var candidate)) continue;
                    double enter=Math.Max(0d,(candidate.AbsolutePosition-configuration.Ahead-playerZ)/playerSpeed);
                    if (enter>horizon) continue;
                    if (!TrySelectTraffic(candidate,Math.Max(playerZ,candidate.AbsolutePosition-configuration.Ahead),out var key,out _)) continue;
                    // A slower car can yield all the way to zero before we reach it.
                    double npcSpeed=0d;
                    double r0=z+speed*enter-candidate.AbsolutePosition;
                    double r1=end-(candidate.AbsolutePosition+npcSpeed*(horizon-enter));
                    if (Math.Min(r0,r1)<=pad && Math.Max(r0,r1)>=-pad) return RejectRear(4);
                }
            }
            for (int i=0;i<ActiveCount;i++)
            {
                var lease=active[i]; if (!lease.IsCurrent || lease.Item==self || lease.Item.Category!=InteractionCategory.Obstacle) continue;
                var item=lease.Item;
                float otherMin=Mathf.Min(item.PreviousPosition.x,item.CurrentPosition.x),otherMax=Mathf.Max(item.PreviousPosition.x,item.CurrentPosition.x);
                if (item is MovingCar m && m.IsOvertaker)
                { otherMin=Mathf.Min(otherMin,m.TargetLane*gameplay.LaneWidth); otherMax=Mathf.Max(otherMax,m.TargetLane*gameplay.LaneWidth); }
                if (maxX+2.2f<otherMin || minX-2.2f>otherMax) continue;
                double r0=z-item.AbsoluteZ, r1=end-(item.AbsoluteZ+(item is MovingCar fast && fast.IsOvertaker ? item.Speed : 0f)*horizon);
                if (Math.Min(r0,r1)<=pad && Math.Max(r0,r1)>=-pad) return RejectRear(4);
            }
            return true;
        }
        internal static bool ProposesMoving(TrafficCandidate candidate) =>
            (WorldRandom.Sample(candidate.Seed,candidate.BlockIndex,WorldRandom.GameplayStream,(ulong)(64+candidate.Row*3+candidate.Lane+1))&3u)!=0;
        public bool TrySelectTraffic(TrafficCandidate candidate,double playerZ,out PoolKey key,out PatternRejection rejection)
        {
            var extent=new Vector3(1.1f,0.6f,2.2f);
            bool proposeMoving=ProposesMoving(candidate);
            key=proposeMoving ? PoolKey.Moving : PoolKey.Stationary;
            if (PatternValidator.ValidateMovingEnvelope(this,candidate.Lane,candidate.AbsolutePosition,playerZ,proposeMoving ? 8f : 0f,extent,out rejection)) return true;
            if (!proposeMoving) return false;
            key=PoolKey.Stationary;
            return PatternValidator.ValidateMovingEnvelope(this,candidate.Lane,candidate.AbsolutePosition,playerZ,0f,extent,out _);
        }
        private void Record(TrafficCandidate candidate,PoolKey key,PatternRejection rejection,PoolRentFailure miss)
        {
            diagnosticSeed=candidate.Seed; diagnosticBlock=candidate.BlockIndex; diagnosticSequence=candidate.SpawnSequence;
            diagnosticRejection=rejection; diagnosticMiss=miss;
            diagnostics[diagnosticCursor]=new SpawnDiagnostic(candidate,key,rejection,miss,configuration,gameplay);
            diagnosticCursor=(diagnosticCursor+1)%diagnostics.Length; DiagnosticCount=Math.Min(DiagnosticCount+1,diagnostics.Length);
        }
        private void Record(PickupPlacement placement,PatternRejection rejection,PoolRentFailure miss)
        {
            diagnosticSeed=placement.Seed; diagnosticBlock=placement.BlockIndex; diagnosticSequence=placement.SpawnSequence;
            diagnosticRejection=rejection; diagnosticMiss=miss;
            diagnostics[diagnosticCursor]=new SpawnDiagnostic(placement,rejection,miss,configuration,gameplay);
            diagnosticCursor=(diagnosticCursor+1)%diagnostics.Length; DiagnosticCount=Math.Min(DiagnosticCount+1,diagnostics.Length);
        }
        private readonly BlockPlan[] plans = new BlockPlan[4];
        private int seed;
        public WorldConfig Configuration => configuration;
        public GameplayConfig Gameplay => gameplay;
        public int PlanCount { get; private set; }
        public BlockPlan GetPlan(int index) => index >= 0 && index < PlanCount ? plans[index] : throw new ArgumentOutOfRangeException(nameof(index));
        internal void ResetRun(int runSeed)
        { Clear(); Array.Clear(rearRejections,0,rearRejections.Length); WarnedOvertakers=0; nextRearAttempt=0d; rearSequence=0; SpawnedOvertakers=RejectedOvertakers=RejectedLaneChanges=CompletedOvertakes=AcceptedLaneChanges=0; seed=runSeed; EligibleSlots=ProposedTraffic=RejectedTraffic=TrafficPoolMisses=MovingFallbacks=0; SpawnedMoving=SpawnedStationary=Omitted=SpawnedCoins=SpawnedFuel=OmittedCoins=FuelFailures=FuelReclaims=0; DiagnosticCount=diagnosticCursor=0; diagnosticSeed=runSeed; diagnosticBlock=diagnosticSequence=0; diagnosticRejection=PatternRejection.None; diagnosticMiss=PoolRentFailure.None; Array.Clear(diagnostics,0,diagnostics.Length); }
        internal void Clear() { ReleaseAll(); Array.Clear(rearWarnings,0,rearWarnings.Length); Array.Clear(plans,0,plans.Length); Array.Clear(stagedFuel,0,stagedFuel.Length); stagedFuelCount=0; PlanCount=0; nextRow=nextCoinSlot=nextFuelIndex=fuelTransactionStart=0; seed=0; LastError=null; }
        /// <summary>Rows finalize by absolute world address, using their distance as the
        /// deterministic progression context (elapsed=0). Queries never commit/redraw state.
        /// This also permits conservative reconstruction beyond the bounded visible window.</summary>
        public float RowOccupancy(long blockIndex, int row) => Mathf.Min(gameplay.MaximumTrafficOccupancy,ProgressionSystem.Occupancy(
            ProgressionSystem.EvaluateDifficulty(0d, (double)blockIndex * WorldConfig.BlockLength + row * WorldConfig.SegmentLength, gameplay), gameplay) * gameplay.TrafficDensityMultiplier);
        public BlockPlan PlanAt(long blockIndex)
        {
            if (blockIndex < 0) throw new ArgumentOutOfRangeException(nameof(blockIndex));
            int lane = LaneAt(blockIndex), nextLane = LaneAt(checked(blockIndex + 1));
            uint rows = 0;
            for (int row = 0; row < WorldConfig.RowsPerBlock; row++)
                for (int candidateLane = -1; candidateLane <= 1; candidateLane++)
                    if (candidateLane != lane && (double)blockIndex * WorldConfig.BlockLength + row * WorldConfig.SegmentLength >= 180d &&
                        WorldRandom.Sample(seed, blockIndex, WorldRandom.GameplayStream, (ulong)(1 + row * 3 + candidateLane + 1)) / 4294967296d < RowOccupancy(blockIndex, row))
                        rows |= 1u << (row * 3 + candidateLane + 1);
            var plan = new BlockPlan(seed, blockIndex, lane, nextLane, rows);
            if (!PatternValidator.Validate(plan, configuration, gameplay, out var reason))
                throw new InvalidOperationException("Unsafe block proposal: " + reason);
            return plan;
        }
        private int LaneAt(long index) => index == 0 ? 0 : (int)(WorldRandom.Sample(seed, index, WorldRandom.GameplayStream) % 3) - 1;
        internal void FillWindow(double distance)
        {
            long first = Math.Max(0, (long)Math.Floor((distance - configuration.Behind) / WorldConfig.BlockLength));
            long last = (long)Math.Floor((distance + configuration.Ahead) / WorldConfig.BlockLength) + 1;
            PlanCount = checked((int)(last - first + 1));
            if (PlanCount > plans.Length) throw new InvalidOperationException("Planning window exceeds fixed storage.");
            for (int i = 0; i < PlanCount; i++) plans[i] = PlanAt(first + i);
        }
    }
}
