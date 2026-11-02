using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    public enum RoadEnvironment { Open, Tunnel, Entrance, Exit }

    public sealed class RoadSegment : PooledObject
    {
        [SerializeField] private RoadEnvironment environment;
        [SerializeField] private Transform entry;
        [SerializeField] private Transform exit;
        [SerializeField] private Light streetlight;
        public Light Streetlight => streetlight;
        public RoadEnvironment Environment => environment;
        public Transform Entry => entry;
        public Transform Exit => exit;
        public long SegmentIndex { get; private set; }
        public double AbsoluteStart { get; private set; }
        public uint DecorationVariant { get; private set; }
        public double Length => WorldConfig.SegmentLength;

        public bool IsValidFor(PoolKey key) => (int)environment == (int)key && entry && exit &&
            entry.parent == transform && exit.parent == transform && entry.localPosition == Vector3.zero &&
            exit.localPosition == new Vector3(0f, 0f, 60f) && transform.localRotation == Quaternion.identity &&
            transform.localScale == Vector3.one;

        public override void OnRent(SpawnContext context)
        {
            base.OnRent(context);
            if (!IsValidFor(context.Key)) throw new InvalidOperationException("Invalid road prefab endpoints/environment.");
            SegmentIndex = context.SpawnSequence;
            AbsoluteStart = context.AbsoluteDistance;
            DecorationVariant = WorldRandom.Sample(context.Seed, SegmentIndex, WorldRandom.RoadStream) % 3;
            if (streetlight) streetlight.enabled = false;
        }
        public override void OnReturn()
        {
            if (streetlight) streetlight.enabled = false;
            SegmentIndex = 0;
            AbsoluteStart = 0d;
            DecorationVariant = 0;
            base.OnReturn();
        }
        internal void SetOrigin(double offset) => transform.localPosition = new Vector3(0f, 0f, (float)(AbsoluteStart - offset));
        // One source per 60m module and a 170m selection window bound enabled sources to three.
        internal void RefreshLighting(double playerDistance)
        { if (streetlight) streetlight.enabled = IsRented && Math.Abs(AbsoluteStart + 30d - playerDistance) <= 85d; }
    }

    // Counter-based streams: no traversal/history dependency, UnityEngine.Random or cosmetic state.
    internal static class WorldRandom
    {
        internal const ulong RoadStream = 0x726f6164UL;
        internal const ulong GameplayStream = 0x67616d65UL;
        internal const ulong PickupStream = 0x7069636bUL;
        internal static uint Sample(int seed, long index, ulong stream, ulong counter = 0)
        {
            unchecked
            {
                ulong value = (ulong)index ^ ((ulong)(uint)seed << 32) ^ stream ^ (counter * 0x9e3779b97f4a7c15UL);
                value += 0x9e3779b97f4a7c15UL;
                value = (value ^ (value >> 30)) * 0xbf58476d1ce4e5b9UL;
                value = (value ^ (value >> 27)) * 0x94d049bb133111ebUL;
                return (uint)(value ^ (value >> 31));
            }
        }
    }
}
