using System;

namespace NeonTunnelRunner
{
    public enum PatternRejection { None, InvalidLane, UnsafeTiming, ProtectedLaneOccupied, InvalidOccupancy, WarmupOccupied }

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
        public bool Equals(BlockPlan other) => Seed == other.Seed && BlockIndex == other.BlockIndex &&
            ProtectedLane == other.ProtectedLane && NextProtectedLane == other.NextProtectedLane && PackedRows == other.PackedRows;
        public override bool Equals(object obj) => obj is BlockPlan other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Seed, BlockIndex, ProtectedLane, NextProtectedLane, PackedRows);
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
            else if (!CanTransition(plan.ProtectedLane, plan.NextProtectedLane, world, gameplay)) rejection = PatternRejection.UnsafeTiming;
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
