using UnityEngine;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public class PooledObject : MonoBehaviour, IPoolable
    {
        internal enum RentalState { Inactive, Rented, Returning }
        internal RentalState Status { get; set; }
        internal Vector3 AuthoredScale { get; set; }

        public PoolService Owner { get; internal set; }
        public PoolKey Key { get; internal set; }
        public ulong ActivationId { get; internal set; }
        public bool IsRented => Status == RentalState.Rented;
        public SpawnContext Context { get; internal set; }

        /// <summary>Capture ActivationId when scheduling work; pass that captured value here.</summary>
        public bool ReturnToPool(ulong expectedActivationId) => Owner && Owner.Return(this, expectedActivationId);

        // PoolService calls these once per accepted rental/return. Derived types reset their own
        // colliders, pose history, consumed flags, timers, particles, audio, trails and visuals here.
        // OnRent runs inactive with new context/identity; OnReturn runs after unregistration.
        public virtual void OnPrewarm() { }
        public virtual void OnRent(SpawnContext context) { }
        public virtual void OnReturn() { }

        // Deliberately no return-on-OnDisable: explicit lifecycle ownership prevents recursion.
    }
}
