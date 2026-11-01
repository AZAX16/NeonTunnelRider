using UnityEngine;

namespace NeonTunnelRunner
{
    public interface IPoolable
    {
        void OnRent(SpawnContext context);
        void OnReturn();
    }

    /// <summary>Caller-owned spawn data. Identity and activation tokens are assigned only by PoolService.</summary>
    public readonly struct SpawnContext
    {
        public PoolKey Key { get; }
        public Vector3 LocalPosition { get; }
        public Quaternion LocalRotation { get; }
        public Transform ActiveParent { get; }
        public int Lane { get; }
        public double AbsoluteDistance { get; }
        public int Seed { get; }
        public float Speed { get; }
        public long SpawnSequence { get; }
        public SpawnDirector Registry { get; }

        public SpawnContext(PoolKey key, Vector3 localPosition, Quaternion localRotation,
            Transform activeParent = null, int lane = 0, double absoluteDistance = 0d,
            int seed = 0, float speed = 0f, long spawnSequence = 0, SpawnDirector registry = null)
        {
            Key = key;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            ActiveParent = activeParent;
            Lane = lane;
            AbsoluteDistance = absoluteDistance;
            Seed = seed;
            Speed = speed;
            SpawnSequence = spawnSequence;
            Registry = registry;
        }
    }
}
