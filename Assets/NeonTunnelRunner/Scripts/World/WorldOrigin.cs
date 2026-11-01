using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Extension point for an independently owned active root or future interactable registry.
    /// Implementations teleport their bodies and update logical histories/caches from absolute data.
    /// A registry owns its children; never also register a child or inactive storage.</summary>
    public interface IWorldOriginParticipant
    {
        Transform OriginRoot { get; }
        void ShiftOrigin(double newOffset, double delta);
    }

    [DisallowMultipleComponent]
    public sealed class WorldOrigin : MonoBehaviour
    {
        [SerializeField] private WorldConfig configuration;
        [SerializeField] private PlayerController player;
        [SerializeField] private RoadManager roads;
        [SerializeField] private Transform chaseCamera;
        [SerializeField] private MonoBehaviour[] additionalParticipants = Array.Empty<MonoBehaviour>();
        public WorldConfig Configuration => configuration;
        public PlayerController Player => player;
        public RoadManager Roads => roads;
        public Transform ChaseCamera => chaseCamera;
        public double Offset { get; private set; }
        public long RebaseCount { get; private set; }

        public bool TryValidate(out string error)
        {
            error = null;
            if (!enabled || !gameObject.activeInHierarchy || !configuration || !player || !roads || roads.Configuration != configuration ||
                !chaseCamera || !chaseCamera.IsChildOf(player.transform) || player.transform.IsChildOf(roads.ActiveRoot))
            { error = "Assign WorldOrigin, player, roads and the existing player-owned chase camera."; return false; }
            for (int i = 0; i < additionalParticipants.Length; i++)
            {
                var participant = additionalParticipants[i] as IWorldOriginParticipant;
                Transform root = participant?.OriginRoot;
                if (!root || Overlaps(root, player.transform) || Overlaps(root, roads.ActiveRoot) || Overlaps(root, roads.Pools.StorageRoot))
                { error = "Origin participant overlaps a primary root or pool storage."; return false; }
                for (int j = 0; j < i; j++)
                    if (Overlaps(root, ((IWorldOriginParticipant)additionalParticipants[j]).OriginRoot))
                    { error = "Origin participants must own disjoint roots."; return false; }
            }
            return true;
        }
        private static bool Overlaps(Transform a, Transform b) => a && b && (a == b || a.IsChildOf(b) || b.IsChildOf(a));
        internal void ResetRun() { Offset = 0d; RebaseCount = 0; }
        /// <summary>Explicit end-of-tick world phase; never invoke during contact dispatch.</summary>
        internal bool RebaseIfNeeded(double absolutePlayerPosition)
        {
            double local = absolutePlayerPosition - Offset;
            if (local < configuration.RebaseThreshold) return false;
            double delta = Math.Floor(local / WorldConfig.SegmentLength) * WorldConfig.SegmentLength;
            Offset += delta;
            player.ShiftOrigin(Offset);
            roads.SetOrigin(Offset);
            // Camera/anchor are children of the teleported player: shifted once by that owner.
            // There is no follow/damping/interpolation component in the Task 03 camera.
            foreach (var component in additionalParticipants)
                ((IWorldOriginParticipant)component).ShiftOrigin(Offset, delta);
            RebaseCount++;
            return true;
        }
    }
}
