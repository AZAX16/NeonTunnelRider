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
        public double Ahead { get; }
        public double Behind { get; }
        public float MinimumSpeed { get; }
        public float MaximumSpeed { get; }
        public float Probability { get; }
        internal SpawnDiagnostic(TrafficCandidate candidate,PoolKey kind,PatternRejection rejection,PoolRentFailure miss,WorldConfig world,GameplayConfig gameplay)
        { Candidate=candidate; Kind=kind; Rejection=rejection; Miss=miss; Ahead=world.Ahead; Behind=world.Behind; MinimumSpeed=gameplay.StartingSpeed; MaximumSpeed=gameplay.MaximumSpeed; Probability=world.CandidateProbability; }
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
        private long nextRow;
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
        public TrafficLease GetActive(int index) => index >= 0 && index < ActiveCount ? active[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public SpawnDiagnostic GetDiagnostic(int index) => index >= 0 && index < DiagnosticCount ? diagnostics[(diagnosticCursor-DiagnosticCount+index+diagnostics.Length)%diagnostics.Length] : throw new ArgumentOutOfRangeException(nameof(index));
        internal void Initialize()
        {
            int capacity=0;
            if (pools && pools.Configuration)
                for (int i=0;i<pools.Configuration.EntryCount;i++)
                { var entry=pools.Configuration.GetEntry(i); if (entry.Prefab && entry.Prefab.GetComponent<Interactable>()) capacity+=entry.Capacity; }
            active=new TrafficLease[capacity]; deferred=new TrafficLease[capacity];
        }
        public bool ValidateRequired(out string error)
        {
            error=null;
            if (!pools || !activeRoot || !activeRoot.gameObject.activeInHierarchy || activeRoot.position != Vector3.zero ||
                activeRoot.rotation != Quaternion.identity || activeRoot.lossyScale != Vector3.one)
            { error="Assign neutral independent traffic root and PoolService."; return false; }
            foreach (var key in new[] { PoolKey.Stationary, PoolKey.Moving })
            {
                bool found=false;
                for (int i=0;i<pools.Configuration.EntryCount;i++)
                {
                    var entry=pools.Configuration.GetEntry(i);
                    if (entry.Key != key) continue;
                    var car=entry.Prefab.GetComponent<Interactable>(); var box=entry.Prefab.GetComponent<BoxCollider>();
                    var body=entry.Prefab.GetComponent<Rigidbody>();
                    found=car && (key==PoolKey.Stationary ? car is StationaryCar : car is MovingCar) &&
                        entry.Capacity >= (key==PoolKey.Stationary ? 64 : 32) && box && box.isTrigger && box.center==Vector3.zero &&
                        box.size==car.HalfExtents*2f && car.HalfExtents==new Vector3(1.1f,0.6f,2.2f) &&
                        entry.Prefab.transform.localScale==Vector3.one && entry.Prefab.transform.rotation==Quaternion.identity &&
                        (key!=PoolKey.Moving || (body && body.isKinematic && !body.useGravity));
                }
                if (!found) { error="Missing/invalid required traffic pool "+key+" (64 stationary / 32 moving)."; return false; }
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
        { for (int i=0;i<ActiveCount;i++) { var lease=active[i]; if (!lease.IsCurrent) throw new InvalidOperationException("Stale traffic lease."); lease.Item.PredictStep(dt,offset); } }
        internal void CommitTraffic() { for (int i=0;i<ActiveCount;i++) if (active[i].IsCurrent) active[i].Item.CommitPose(); }
        public void ShiftOrigin(double newOffset,double delta)
        { for (int i=0;i<ActiveCount;i++) if (active[i].IsCurrent) active[i].Item.ShiftOrigin(newOffset); }
        internal void ReleaseAll()
        {
            if (IsDispatching) throw new InvalidOperationException("Reset during contact dispatch is forbidden.");
            Array.Clear(deferred,0,deferred.Length); deferredCount=0;
            while (ActiveCount>0) { var lease=active[ActiveCount-1]; if (lease.IsCurrent) lease.Item.ReturnToPool(lease.ActivationId); else active[--ActiveCount]=default; }
        }
        internal void MaintainTraffic(double playerZ,double offset)
        {
            for (int i=ActiveCount-1;i>=0;i--) { var lease=active[i]; if (lease.IsCurrent && lease.Item.AbsoluteZ+lease.Item.HalfExtents.z < playerZ-configuration.Behind) lease.Item.ReturnToPool(lease.ActivationId); }
            long first=Math.Max(nextRow,Math.Max(0L,(long)Math.Ceiling((playerZ+180d)/60d)));
            // Initial fill includes rows ahead of the player. Skipped catch-up rows are omitted, never spawned nearby.
            long last=(long)Math.Floor((playerZ+configuration.Ahead)/60d);
            for (long absoluteRow=first;absoluteRow<=last;absoluteRow++)
            {
                long block=absoluteRow/8; int row=(int)(absoluteRow%8);
                if (row>=WorldConfig.RowsPerBlock) continue;
                var plan=PlanAt(block);
                for (int lane=-1;lane<=1;lane++) if (plan.TryGetCandidate(row,lane,out var candidate))
                {
                    if (!TrySelectTraffic(candidate,playerZ,out var key,out var rejection))
                    { Omitted++; Record(candidate,key,rejection,PoolRentFailure.None); continue; }
                    if (rejection!=PatternRejection.None) Record(candidate,key,rejection,PoolRentFailure.None);
                    var position=new Vector3(lane*gameplay.LaneWidth,gameplay.StartingPosition.y,(float)(candidate.AbsolutePosition-offset));
                    var context=new SpawnContext(key,position,Quaternion.identity,activeRoot,lane,candidate.AbsolutePosition,seed,
                        key==PoolKey.Moving ? 8f : 0f,candidate.SpawnSequence,this);
                    if (!pools.TryRent(context,out _)) { Omitted++; Record(candidate,key,PatternRejection.None,pools.LastRentFailure); }
                    else if (key==PoolKey.Moving) SpawnedMoving++; else SpawnedStationary++;
                }
            }
            nextRow=Math.Max(nextRow,last+1);
        }
        public bool TrySelectTraffic(TrafficCandidate candidate,double playerZ,out PoolKey key,out PatternRejection rejection)
        {
            var extent=new Vector3(1.1f,0.6f,2.2f);
            bool proposeMoving=(WorldRandom.Sample(candidate.Seed,candidate.BlockIndex,WorldRandom.GameplayStream,(ulong)(64+candidate.Row*3+candidate.Lane+1))&1u)!=0;
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
        private readonly BlockPlan[] plans = new BlockPlan[4];
        private int seed;
        public WorldConfig Configuration => configuration;
        public GameplayConfig Gameplay => gameplay;
        public int PlanCount { get; private set; }
        public BlockPlan GetPlan(int index) => index >= 0 && index < PlanCount ? plans[index] : throw new ArgumentOutOfRangeException(nameof(index));
        internal void ResetRun(int runSeed)
        { Clear(); seed=runSeed; nextRow=0; SpawnedMoving=SpawnedStationary=Omitted=0; DiagnosticCount=diagnosticCursor=0; diagnosticSeed=runSeed; diagnosticBlock=diagnosticSequence=0; diagnosticRejection=PatternRejection.None; diagnosticMiss=PoolRentFailure.None; Array.Clear(diagnostics,0,diagnostics.Length); }
        internal void Clear() { ReleaseAll(); Array.Clear(plans, 0, plans.Length); PlanCount = 0; nextRow=0; }
        public BlockPlan PlanAt(long blockIndex)
        {
            if (blockIndex < 0) throw new ArgumentOutOfRangeException(nameof(blockIndex));
            int lane = LaneAt(blockIndex), nextLane = LaneAt(checked(blockIndex + 1));
            uint rows = 0;
            for (int row = 0; row < WorldConfig.RowsPerBlock; row++)
                for (int candidateLane = -1; candidateLane <= 1; candidateLane++)
                    if (candidateLane != lane && (double)blockIndex * WorldConfig.BlockLength + row * WorldConfig.SegmentLength >= 180d &&
                        WorldRandom.Sample(seed, blockIndex, WorldRandom.GameplayStream, (ulong)(1 + row * 3 + candidateLane + 1)) / 4294967296d < configuration.CandidateProbability)
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
