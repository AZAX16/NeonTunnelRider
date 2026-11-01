using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Bounded value-data planning only. Task 06 validates moving envelopes before any traffic rental.</summary>
    [DisallowMultipleComponent]
    public sealed class SpawnDirector : MonoBehaviour
    {
        [SerializeField] private WorldConfig configuration;
        [SerializeField] private GameplayConfig gameplay;
        private readonly BlockPlan[] plans = new BlockPlan[4];
        private int seed;
        public WorldConfig Configuration => configuration;
        public GameplayConfig Gameplay => gameplay;
        public int PlanCount { get; private set; }
        public BlockPlan GetPlan(int index) => index >= 0 && index < PlanCount ? plans[index] : throw new ArgumentOutOfRangeException(nameof(index));
        internal void ResetRun(int runSeed) { Clear(); seed = runSeed; }
        internal void Clear() { Array.Clear(plans, 0, plans.Length); PlanCount = 0; }
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
