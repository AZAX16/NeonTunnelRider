using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public sealed class RoadManager : MonoBehaviour
    {
        public const int WindowCapacity = 7;
        private static readonly PoolKey[] Schedule = { PoolKey.RoadOpen, PoolKey.RoadOpen, PoolKey.RoadOpen,
            PoolKey.RoadEntrance, PoolKey.RoadTunnel, PoolKey.RoadTunnel, PoolKey.RoadTunnel, PoolKey.RoadExit };
        [SerializeField] private WorldConfig configuration;
        [SerializeField] private PoolService pools;
        [SerializeField] private Transform activeRoot;
        private readonly RoadLease[] active = new RoadLease[WindowCapacity];
        private readonly RoadLease[] desired = new RoadLease[WindowCapacity];
        private int seed;
        public WorldConfig Configuration => configuration;
        public PoolService Pools => pools;
        public Transform ActiveRoot => activeRoot;
        public int ActiveCount { get; private set; }
        public string LastError { get; private set; }
        public double CoverageStart => ActiveCount == 0 ? 0d : active[0].Segment.AbsoluteStart;
        public double CoverageEnd => ActiveCount == 0 ? 0d : active[ActiveCount - 1].Segment.AbsoluteStart + WorldConfig.SegmentLength;
        public RoadLease GetLease(int index) => index >= 0 && index < ActiveCount ? active[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public static PoolKey KeyAt(long index) => Schedule[(int)((index % Schedule.Length + Schedule.Length) % Schedule.Length)];
        public static double AbsoluteStart(long index) => (double)index * WorldConfig.SegmentLength;
        public static float LocalStart(long index, double offset) => (float)(AbsoluteStart(index) - offset);

        public bool ValidateRequired(GameplayConfig gameplay, out string error)
        {
            error = null;
            if (!enabled || !gameObject.activeInHierarchy || !configuration || !pools || !pools.IsReady || !pools.isActiveAndEnabled ||
                !activeRoot || !activeRoot.gameObject.activeInHierarchy || activeRoot.position != Vector3.zero ||
                activeRoot.rotation != Quaternion.identity || activeRoot.lossyScale != Vector3.one ||
                activeRoot == pools.StorageRoot || activeRoot.IsChildOf(pools.StorageRoot))
            { error = "Assign enabled RoadManager, WorldConfig, ready PoolService and a neutral active road root outside storage."; return false; }
            if (!configuration.TryValidate(gameplay, out error)) return false;
            int total = 0;
            for (int key = 0; key < 4; key++)
            {
                int required = key < 2 ? 6 : 2; // Two disjoint final windows; maximum 3/3/1/1 per window.
                if (!pools.TryGetCounters((PoolKey)key, out var counters) || counters.Capacity < required)
                { error = "Required road pool " + (PoolKey)key + " needs capacity >= " + required + "."; return false; }
                bool found = false;
                for (int i = 0; i < pools.Configuration.EntryCount; i++)
                {
                    var entry = pools.Configuration.GetEntry(i);
                    if ((int)entry.Key != key) continue;
                    var segment = entry.Prefab.GetComponent<RoadSegment>();
                    if (!segment || !segment.IsValidFor(entry.Key))
                    { error = "Required road prefab " + entry.Key + " has invalid RoadSegment/endpoints/environment."; return false; }
                    found = true;
                }
                if (!found) { error = "Missing required road prefab " + (PoolKey)key + "."; return false; }
                total += counters.Capacity;
            }
            if (total < configuration.MaximumSegments + 2) { error = "Road pools need at least two spare modules."; return false; }
            return true;
        }

        /// <summary>Actual active coverage and authored half-segment roof footprints, in absolute metres.</summary>
        public bool IsOpenSkySpan(double from, double to)
        {
            if(!double.IsFinite(from) || !double.IsFinite(to) || from>to || from<CoverageStart || to>=CoverageEnd) return false;
            for(int i=0;i<ActiveCount;i++)
            {
                var lease=active[i]; if(!lease.IsCurrent) return false;
                var road=lease.Segment; double start=road.AbsoluteStart, end=start+WorldConfig.SegmentLength;
                if(end<=from || start>to || road.Environment==RoadEnvironment.Open) continue;
                double roofStart=start+(road.Environment==RoadEnvironment.Entrance?30d:0d);
                double roofEnd=end-(road.Environment==RoadEnvironment.Exit?30d:0d);
                if(roofStart<=to && roofEnd>from) return false;
            }
            return true;
        }

        internal void ResetRun(int runSeed) { ReleaseAll(); seed = runSeed; LastError = null; }
        internal void ReleaseAll()
        {
            for (int i = 0; i < ActiveCount; i++) pools.Return(active[i].Segment, active[i].ActivationId);
            Array.Clear(active, 0, active.Length);
            Array.Clear(desired, 0, desired.Length);
            ActiveCount = 0;
        }

        /// <summary>Reserve final window before releasing old leases. Work is bounded even for a large skip.</summary>
        public bool EnsureCoverage(double distance, double originOffset)
        {
            LastError = null;
            if (!double.IsFinite(distance) || distance < 0d || !double.IsFinite(originOffset) || distance / WorldConfig.SegmentLength >= long.MaxValue - 8d) return Fail("Invalid absolute road position.");
            long first = checked((long)Math.Floor((distance - configuration.Behind) / WorldConfig.SegmentLength));
            long last = checked((long)Math.Ceiling((distance + configuration.Ahead) / WorldConfig.SegmentLength) - 1);
            int count = checked((int)(last - first + 1));
            if (count <= 0 || count > desired.Length) return Fail("Road coverage exceeds bounded storage.");
            for (int i = 0; i < ActiveCount; i++)
                if (!active[i].IsCurrent) return Fail("Stale or disabled required road lease; reset the run.");
            for (int i = 0; i < count; i++)
            {
                long index = first + i;
                desired[i] = default;
                for (int j = 0; j < ActiveCount; j++)
                    if (active[j].Segment.SegmentIndex == index) { desired[i] = active[j]; break; }
                if (desired[i].Segment) continue;
                var context = new SpawnContext(KeyAt(index), new Vector3(0f, 0f, LocalStart(index, originOffset)),
                    Quaternion.identity, activeRoot, absoluteDistance: AbsoluteStart(index), seed: seed, spawnSequence: index);
                if (!pools.TryRent(context, out var item))
                {
                    RollbackReservations(i);
                    return Fail("Required road rental failed at segment " + index + " (" + context.Key + "): " + pools.LastRentFailure + ". Simulation stopped before movement.");
                }
                if (!(item is RoadSegment segment))
                {
                    pools.Return(item, item.ActivationId);
                    RollbackReservations(i);
                    return Fail("Required pool returned a non-road instance.");
                }
                desired[i] = new RoadLease(segment, item.ActivationId);
            }
            for (int i = 0; i < ActiveCount; i++)
                if (active[i].Segment.SegmentIndex < first || active[i].Segment.SegmentIndex > last)
                    pools.Return(active[i].Segment, active[i].ActivationId);
            Array.Clear(active, 0, active.Length);
            Array.Copy(desired, active, count);
            Array.Clear(desired, 0, desired.Length);
            ActiveCount = count;
            for (int i = 0; i < ActiveCount; i++) active[i].Segment.RefreshLighting(distance);
            return true;
        }
        private void RollbackReservations(int count)
        {
            for (int i = 0; i < count; i++)
            {
                bool retained = false;
                for (int j = 0; j < ActiveCount; j++) if (active[j].Segment == desired[i].Segment) { retained = true; break; }
                if (!retained && desired[i].Segment) pools.Return(desired[i].Segment, desired[i].ActivationId);
            }
            Array.Clear(desired, 0, desired.Length);
        }
        private bool Fail(string error) { LastError = error; return false; }
        internal void SetOrigin(double offset)
        {
            for (int i = 0; i < ActiveCount; i++) active[i].Segment.SetOrigin(offset);
        }
    }

    public readonly struct RoadLease
    {
        public RoadSegment Segment { get; }
        public ulong ActivationId { get; }
        public bool IsCurrent => Segment && Segment.IsRented && Segment.ActivationId == ActivationId && Segment.gameObject.activeInHierarchy;
        internal RoadLease(RoadSegment segment, ulong token) { Segment = segment; ActivationId = token; }
    }
}
