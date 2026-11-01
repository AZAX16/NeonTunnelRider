using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    // Test-only runtime template, never saved as a gameplay prefab or shipped in Pools.asset.
    public sealed class PoolProbe : PooledObject
    {
        [NonSerialized] public int Rents, Returns, Enables, Disables, DirtyValue;
        [NonSerialized] public bool RentedWhileInactive, EnableSawCurrentContext, CleanupSawUnregistered;
        [NonSerialized] public Action<PoolProbe> RentAction, ReturnAction, EnableAction, DisableAction;
        public override void OnRent(SpawnContext context)
        {
            Rents++;
            DirtyValue = 0;
            RentedWhileInactive = !gameObject.activeSelf && !gameObject.activeInHierarchy;
            RentAction?.Invoke(this);
        }
        public override void OnReturn()
        {
            Returns++;
            CleanupSawUnregistered = !IsRented;
            DirtyValue = 0;
            ReturnAction?.Invoke(this);
        }
        private void OnEnable()
        {
            if (!Owner) return; // Template itself has no pool identity.
            Enables++;
            EnableSawCurrentContext = IsRented && ActivationId > 0 && Context.Key == Key && DirtyValue == 0 &&
                transform.parent == Context.ActiveParent && transform.localPosition == Context.LocalPosition;
            EnableAction?.Invoke(this);
        }
        private void OnDisable()
        {
            Disables++;
            DisableAction?.Invoke(this);
        }
    }
}
