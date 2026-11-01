using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    public enum PatternRejection { None, InvalidLane, UnsafeTiming, ProtectedLaneOccupied, InvalidOccupancy, WarmupOccupied, InvalidEnvelope, TransitionBuffer, ProtectedEnvelope, LookaheadLimit, PickupOverlap, FuelRoute }

    /// <summary>Value-only proposal. Bits 0..2 per row encode lanes -1,0,+1; never a live object reference.</summary>
    public readonly struct BlockPlan : IEquatable<BlockPlan>
    {
        public int Seed { get; }
        public long BlockIndex { get; }
        public int ProtectedLane { get; }
        public int NextProtectedLane { get; }
        public uint PackedRows { get; }
        public double AbsoluteStart => (double)BlockIndex * WorldConfig.BlockLength;
        public double ProtectedEnd => AbsoluteStart + WorldConfig.TrafficSpan;
        public double BufferStart => ProtectedEnd;
        public double AbsoluteEnd => AbsoluteStart + WorldConfig.BlockLength;
        public BlockPlan(int seed, long blockIndex, int protectedLane, int nextProtectedLane, uint packedRows)
        { Seed = seed; BlockIndex = blockIndex; ProtectedLane = protectedLane; NextProtectedLane = nextProtectedLane; PackedRows = packedRows; }
        public int OccupancyAt(int row)
        {
            if ((uint)row >= WorldConfig.RowsPerBlock) throw new ArgumentOutOfRangeException(nameof(row));
            return (int)((PackedRows >> (row * 3)) & 7);
        }
        public double RowPosition(int row)
        {
            if ((uint)row >= WorldConfig.RowsPerBlock) throw new ArgumentOutOfRangeException(nameof(row));
            return AbsoluteStart + row * WorldConfig.SegmentLength;
        }
        public bool TryGetCandidate(int row, int lane, out TrafficCandidate candidate)
        {
            candidate = default;
            if (lane < -1 || lane > 1 || (OccupancyAt(row) & (1 << (lane + 1))) == 0) return false;
            candidate = new TrafficCandidate(Seed, BlockIndex, row, lane, RowPosition(row));
            return true;
        }
        public const int FuelCount = 3;
        public const int CoinSlots = 8;
        public PickupPlacement GetFuel(int slot)
        {
            if ((uint)slot >= FuelCount) throw new ArgumentOutOfRangeException(nameof(slot));
            return new PickupPlacement(Seed,BlockIndex,slot,PoolKey.Fuel,slot==2 ? NextProtectedLane : ProtectedLane,
                AbsoluteStart+(slot==0 ? 120d : slot==1 ? 300d : 450d),true);
        }
        public PickupPlacement GetCoin(int slot)
        {
            if ((uint)slot >= CoinSlots) throw new ArgumentOutOfRangeException(nameof(slot));
            int lane=ProtectedLane;
            if ((slot&1)!=0)
            {
                lane=(int)(WorldRandom.Sample(Seed,BlockIndex,WorldRandom.PickupStream,(ulong)slot)%2)-1;
                if (lane >= ProtectedLane) lane++;
            }
            return new PickupPlacement(Seed,BlockIndex,FuelCount+slot,PoolKey.Coin,lane,AbsoluteStart+60d+(slot/2)*90d,false);
        }
        public bool Equals(BlockPlan other) => Seed == other.Seed && BlockIndex == other.BlockIndex &&
            ProtectedLane == other.ProtectedLane && NextProtectedLane == other.NextProtectedLane && PackedRows == other.PackedRows;
        public override bool Equals(object obj) => obj is BlockPlan other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Seed, BlockIndex, ProtectedLane, NextProtectedLane, PackedRows);
    }

    public readonly struct PickupPlacement
    {
        public int Seed { get; }
        public long BlockIndex { get; }
        public int Slot { get; }
        public PoolKey Kind { get; }
        public int Lane { get; }
        public double AbsolutePosition { get; }
        public bool Required { get; }
        // Separate from the unchanged traffic sequence/address domain.
        public long SpawnSequence => checked((1L<<62)+BlockIndex*16+Slot);
        internal PickupPlacement(int seed,long block,int slot,PoolKey kind,int lane,double z,bool required)
        { Seed=seed; BlockIndex=block; Slot=slot; Kind=kind; Lane=lane; AbsolutePosition=z; Required=required; }
        internal TrafficCandidate DiagnosticCandidate => new TrafficCandidate(Seed,BlockIndex,Slot,Lane,AbsolutePosition);
    }

    public readonly struct TrafficCandidate
    {
        public int Seed { get; }
        public long BlockIndex { get; }
        public int Row { get; }
        public int Lane { get; }
        public double AbsolutePosition { get; }
        // Iterate blocks, rows, then lanes -1..+1. Gaps in sequence numbers represent omitted slots.
        public long SpawnSequence => checked(BlockIndex * 18 + Row * 3 + Lane + 1);
        internal TrafficCandidate(int seed, long block, int row, int lane, double position)
        { Seed = seed; BlockIndex = block; Row = row; Lane = lane; AbsolutePosition = position; }
    }

    public static class PatternValidator
    {
        public static readonly Vector3 PickupHalfExtents = new Vector3(0.5f,0.6f,0.5f);
        public static bool ValidateFuelRoute(BlockPlan plan,WorldConfig world,GameplayConfig gameplay)
        {
            if (!CanTransition(plan.ProtectedLane,plan.NextProtectedLane,world,gameplay)) return false;
            double padding=PickupHalfExtents.z+gameplay.PlayerHalfExtents.z;
            double required=world.ReactionTime+Math.Abs(plan.NextProtectedLane-plan.ProtectedLane)*(double)gameplay.LaneTransitionDuration+world.TransitionMargin;
            return plan.GetFuel(0).AbsolutePosition-padding >= plan.AbsoluteStart &&
                plan.GetFuel(1).AbsolutePosition+padding < plan.BufferStart &&
                (plan.GetFuel(2).AbsolutePosition-padding-plan.BufferStart)/gameplay.MaximumSpeed > required &&
                plan.GetFuel(2).AbsolutePosition+padding < plan.AbsoluteEnd;
        }
        public static bool ValidateFuelLink(SpawnDirector planner,PickupPlacement previous,PickupPlacement next)
        {
            if (!previous.Required || !next.Required || previous.Kind!=PoolKey.Fuel || next.Kind!=PoolKey.Fuel ||
                previous.Seed!=next.Seed || next.AbsolutePosition<=previous.AbsolutePosition || next.AbsolutePosition-previous.AbsolutePosition>220d ||
                checked(previous.BlockIndex*3+previous.Slot+1)!=checked(next.BlockIndex*3+next.Slot)) return false;
            var a=planner.PlanAt(previous.BlockIndex); var b=planner.PlanAt(next.BlockIndex);
            return MatchesFuel(a,previous) && MatchesFuel(b,next) && ValidateFuelRoute(a,planner.Configuration,planner.Gameplay) &&
                ValidateFuelRoute(b,planner.Configuration,planner.Gameplay) &&
                (a.BlockIndex==b.BlockIndex || a.NextProtectedLane==b.ProtectedLane);
        }
        private static bool MatchesFuel(BlockPlan plan,PickupPlacement placement)
        {
            if ((uint)placement.Slot>=BlockPlan.FuelCount) return false;
            var expected=plan.GetFuel(placement.Slot);
            return expected.Seed==placement.Seed && expected.Lane==placement.Lane && expected.AbsolutePosition==placement.AbsolutePosition;
        }
        public static bool ValidatePickup(SpawnDirector planner,PickupPlacement pickup,out PatternRejection rejection)
        {
            rejection=PatternRejection.None;
            var world=planner.Configuration; var gameplay=planner.Gameplay;
            if (pickup.Lane < -1 || pickup.Lane > 1 || !double.IsFinite(pickup.AbsolutePosition) || pickup.AbsolutePosition<0d ||
                gameplay.StartingSpeed<=8f || !float.IsFinite(gameplay.StartingSpeed))
            { rejection=PatternRejection.InvalidEnvelope; return false; }
            var ownPlan=planner.PlanAt(pickup.BlockIndex);
            if (pickup.Required && (!MatchesFuel(ownPlan,pickup) || !ValidateFuelRoute(ownPlan,world,gameplay)))
            { rejection=PatternRejection.FuelRoute; return false; }
            // All potentially accepted traffic, including later introductions whose shorter horizon
            // permits a moving proposal rejected at the earliest frontier. Never rely on a pool miss.
            double maxTravel=8d*(world.Ahead+world.Behind+2.2d)/(gameplay.StartingSpeed-8d);
            double padding=2.2d+PickupHalfExtents.z;
            long first=Math.Max(0L,(long)Math.Floor((pickup.AbsolutePosition-maxTravel-padding)/WorldConfig.BlockLength));
            long last=(long)Math.Floor((pickup.AbsolutePosition+padding)/WorldConfig.BlockLength);
            if (last-first>8) { rejection=PatternRejection.LookaheadLimit; return false; }
            for (long block=first;block<=last;block++)
            {
                var plan=planner.PlanAt(block);
                for (int row=0;row<WorldConfig.RowsPerBlock;row++)
                {
                    if (!plan.TryGetCandidate(row,pickup.Lane,out var candidate) ||
                        !ValidateMovingEnvelope(planner,pickup.Lane,candidate.AbsolutePosition,candidate.AbsolutePosition,0f,new Vector3(1.1f,0.6f,2.2f),out _)) continue;
                    double end=candidate.AbsolutePosition;
                    if (SpawnDirector.ProposesMoving(candidate))
                        end=Math.Min(end+maxTravel,plan.BufferStart-(2.2d+gameplay.PlayerHalfExtents.z));
                    if (pickup.AbsolutePosition+padding>=candidate.AbsolutePosition && pickup.AbsolutePosition-padding<=end)
                    { rejection=PatternRejection.PickupOverlap; return false; }
                }
            }
            for (int i=0;i<BlockPlan.FuelCount;i++)
            {
                var fuel=ownPlan.GetFuel(i);
                if (!pickup.Required && pickup.Lane==fuel.Lane && Math.Abs(pickup.AbsolutePosition-fuel.AbsolutePosition)<=2d*PickupHalfExtents.z)
                { rejection=PatternRejection.PickupOverlap; return false; }
            }
            return true;
        }
        // A bounded reconstruction of every reservation reached, including blocks outside FillWindow.
        public static bool ValidateMovingEnvelope(SpawnDirector planner, int lane, double spawnZ, double playerZ,
            float trafficSpeed, Vector3 halfExtents, out PatternRejection rejection)
        {
            rejection=PatternRejection.None;
            var world=planner.Configuration; var gameplay=planner.Gameplay;
            double minimum=gameplay.StartingSpeed, maximum=8d;
            double denominator=minimum-maximum;
            if (lane < -1 || lane > 1 || !double.IsFinite(spawnZ) || !double.IsFinite(playerZ) ||
                !float.IsFinite(trafficSpeed) || trafficSpeed < 0 || trafficSpeed > maximum || !double.IsFinite(denominator) || denominator <= 0 ||
                !float.IsFinite(halfExtents.z) || halfExtents.z <= 0 || halfExtents.z > 2.2f ||
                !float.IsFinite(halfExtents.x) || halfExtents.x <= 0 || halfExtents.x > 1.1f ||
                !float.IsFinite(halfExtents.y) || halfExtents.y <= 0 || halfExtents.y > 0.6f)
            { rejection=PatternRejection.InvalidEnvelope; return false; }
            double horizon=(spawnZ-playerZ+world.Behind+halfExtents.z)/denominator;
            double padding=(double)halfExtents.z+gameplay.PlayerHalfExtents.z;
            double low=spawnZ-padding, high=spawnZ+trafficSpeed*Math.Max(0d,horizon)+padding;
            if (low < 180d) { rejection=PatternRejection.WarmupOccupied; return false; }
            long first=(long)Math.Floor(low/WorldConfig.BlockLength), last=(long)Math.Floor(high/WorldConfig.BlockLength);
            // Real coverage permits at most two blocks. Reject pathological inputs without unbounded work.
            if (first < 0 || last < first || last-first > 8 || high > long.MaxValue)
            { rejection=PatternRejection.LookaheadLimit; return false; }
            for (long block=first;block<=last;block++)
            {
                var plan=planner.PlanAt(block);
                if (low <= plan.AbsoluteEnd && high >= plan.BufferStart)
                { rejection=PatternRejection.TransitionBuffer; return false; }
                if (lane==plan.ProtectedLane && low <= plan.ProtectedEnd && high >= plan.AbsoluteStart)
                { rejection=PatternRejection.ProtectedEnvelope; return false; }
            }
            return true;
        }

        public static bool CanTransition(int from, int to, WorldConfig world, GameplayConfig gameplay)
        {
            if (from < -1 || from > 1 || to < -1 || to > 1 || !world || !gameplay ||
                !float.IsFinite(gameplay.MaximumSpeed) || gameplay.MaximumSpeed <= 0f ||
                !float.IsFinite(gameplay.LaneTransitionDuration) || gameplay.LaneTransitionDuration <= 0f ||
                !float.IsFinite(world.ReactionTime) || world.ReactionTime < 0f ||
                !float.IsFinite(world.TransitionMargin) || world.TransitionMargin < 0f) return false;
            return WorldConfig.TransitionBuffer / gameplay.MaximumSpeed > world.ReactionTime +
                Math.Abs(to - from) * (double)gameplay.LaneTransitionDuration + world.TransitionMargin;
        }

        public static bool Validate(BlockPlan plan, WorldConfig world, GameplayConfig gameplay, out PatternRejection rejection)
        {
            rejection = PatternRejection.None;
            if (plan.BlockIndex < 0 || plan.ProtectedLane < -1 || plan.ProtectedLane > 1 || plan.NextProtectedLane < -1 || plan.NextProtectedLane > 1)
                rejection = PatternRejection.InvalidLane;
            else if (!CanTransition(plan.ProtectedLane, plan.NextProtectedLane, world, gameplay) || !ValidateFuelRoute(plan,world,gameplay)) rejection = PatternRejection.UnsafeTiming;
            else if ((plan.PackedRows >> 18) != 0) rejection = PatternRejection.InvalidOccupancy;
            else for (int row = 0; row < WorldConfig.RowsPerBlock; row++)
            {
                int mask = plan.OccupancyAt(row);
                if (mask == 7) { rejection = PatternRejection.InvalidOccupancy; break; }
                if ((mask & (1 << (plan.ProtectedLane + 1))) != 0) { rejection = PatternRejection.ProtectedLaneOccupied; break; }
                if (plan.RowPosition(row) < 180d && mask != 0) { rejection = PatternRejection.WarmupOccupied; break; }
            }
            return rejection == PatternRejection.None;
        }
    }
}
