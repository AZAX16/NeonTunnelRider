using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonTunnelRunner
{
    public enum PoolRentFailure
    {
        None, NotReady, Busy, UnknownKey, Exhausted, InactiveParent, InvalidPose,
        InvalidInstance, ActivationIdsExhausted, ReturnedDuringCallback, CallbackFailed
    }

    public readonly struct PoolCounters
    {
        public int Capacity { get; }
        public int Created { get; }
        public int Active { get; }
        public int Inactive { get; }
        public int PeakActive { get; }
        public ulong Misses { get; }
        internal PoolCounters(int capacity, int created, int active, int inactive, int peak, ulong misses)
        {
            Capacity = capacity; Created = created; Active = active; Inactive = inactive;
            PeakActive = peak; Misses = misses;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PoolService : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PoolConfig configuration;
        [SerializeField] private Transform storageRoot;

        private sealed class Bucket
        {
            internal readonly PooledObject[] Instances;
            internal readonly Stack<PooledObject> Inactive;
            internal readonly HashSet<PooledObject> Active;
            internal int Created, Peak;
            internal ulong Misses;
            internal Bucket(int capacity)
            {
                Instances = new PooledObject[capacity];
                Inactive = new Stack<PooledObject>(capacity);
                Active = new HashSet<PooledObject>(capacity);
            }
            internal PoolCounters Counters => new PoolCounters(Instances.Length, Created, Active.Count, Inactive.Count, Peak, Misses);
        }

        private Bucket[] buckets;
        private int operationDepth;
        private bool shutdownRequested, shuttingDown;
        private ulong nextActivationId;
        public GameManager Owner => gameManager;
        public PoolConfig Configuration => configuration;
        public Transform StorageRoot => storageRoot;
        public bool IsReady { get; private set; }
        public bool IsBusy => operationDepth != 0 || shuttingDown;
        public string LastError { get; private set; }
        public PoolRentFailure LastRentFailure { get; private set; }
        public PoolKey LastMissKey { get; private set; }
        public int Capacity { get; private set; }
        public int ActiveCount { get; private set; }
        public int InactiveCount { get; private set; }
        public int PeakActive { get; private set; }
        public ulong TotalCreated { get; private set; }
        public ulong TotalDestroyed { get; private set; }
        public ulong TotalRents { get; private set; }
        public ulong TotalReturns { get; private set; }
        public ulong Misses { get; private set; }

        /// <summary>Explicit bootstrap only. A ready service never recreates its catalog on restart.</summary>
        public bool Initialize()
        {
            if (IsReady) return true;
            if (IsBusy) return FailInitialization("Pool initialization cannot run inside a pool callback.");
            if (gameManager && gameManager.State != GameState.Menu)
                return FailInitialization("Prewarm pools in Menu before Playing; runtime pool growth is disabled.");
            // The composition root's early Awake can run before this component's OnEnable.
            if (!enabled || !gameObject.activeInHierarchy || !configuration)
                return FailInitialization("Enable PoolService and assign Pools.asset before StartRun.");
            if (!storageRoot || storageRoot == transform || transform.IsChildOf(storageRoot))
                return FailInitialization("Assign a separate Pools storage root outside the Systems ancestry.");
            if (!configuration.TryValidate(out string error)) return FailInitialization(error);
            storageRoot.gameObject.SetActive(false);
            buckets = new Bucket[PoolConfig.KeyCount];
            operationDepth++;
            try
            {
                for (int i = 0; i < configuration.EntryCount; i++)
                {
                    PoolConfig.Entry entry = configuration.GetEntry(i);
                    var bucket = new Bucket(entry.Capacity);
                    buckets[(int)entry.Key] = bucket;
                    Capacity += entry.Capacity;
                    for (int j = 0; j < entry.Capacity; j++)
                    {
                        // Inactive staging suppresses OnEnable even for an active prefab.
                        var clone = Instantiate(entry.Prefab, storageRoot, false);
                        var instance = clone.GetComponent<PooledObject>();
                        bucket.Instances[j] = instance;
                        bucket.Created++;
                        TotalCreated++;
                        clone.SetActive(false);
                        instance.Owner = this;
                        instance.Key = entry.Key;
                        instance.AuthoredScale = clone.transform.localScale;
                        instance.Status = PooledObject.RentalState.Inactive;
                        instance.OnPrewarm();
                        bucket.Inactive.Push(instance);
                        InactiveCount++;
                    }
                }
                IsReady = true;
                LastError = null;
                return true;
            }
            catch (Exception exception)
            {
                LastError = "Pool prewarm failed: " + exception.Message;
                shutdownRequested = true;
                return false;
            }
            finally { ExitOperation(); }
        }

        private bool FailInitialization(string error) { LastError = error; return false; }

        public bool TryGetCounters(PoolKey key, out PoolCounters counters)
        {
            Bucket bucket = GetBucket(key);
            counters = bucket == null ? default : bucket.Counters;
            return bucket != null;
        }

        /// <summary>No fallback allocation. Inspect LastRentFailure, LastMissKey and counters on failure.</summary>
        public bool TryRent(SpawnContext context, out PooledObject instance)
        {
            instance = null;
            Bucket bucket = GetBucket(context.Key);
            if (!IsReady || !isActiveAndEnabled) return Miss(context.Key, bucket, PoolRentFailure.NotReady);
            // Return and ReturnAll are allowed from hooks; nested rentals are deterministically rejected.
            if (IsBusy || (context.Registry && context.Registry.IsDispatching)) return Miss(context.Key, bucket, PoolRentFailure.Busy);
            if (bucket == null) return Miss(context.Key, null, PoolRentFailure.UnknownKey);
            if (bucket.Inactive.Count == 0) return Miss(context.Key, bucket, PoolRentFailure.Exhausted);
            Transform parent = context.ActiveParent;
            if (parent && (!parent.gameObject.activeInHierarchy || parent == storageRoot || parent.IsChildOf(storageRoot)))
                return Miss(context.Key, bucket, PoolRentFailure.InactiveParent);
            if (!Finite(context.LocalPosition) || !Finite(context.LocalRotation))
                return Miss(context.Key, bucket, PoolRentFailure.InvalidPose);
            PooledObject candidate = bucket.Inactive.Peek();
            if (!candidate || !candidate.enabled || (parent && (parent == candidate.transform || parent.IsChildOf(candidate.transform))))
                return Miss(context.Key, bucket, PoolRentFailure.InvalidInstance);
            if (nextActivationId == ulong.MaxValue) return Miss(context.Key, bucket, PoolRentFailure.ActivationIdsExhausted);

            operationDepth++;
            ulong token = ++nextActivationId;
            try
            {
                bucket.Inactive.Pop();
                InactiveCount--;
                candidate.ActivationId = token;
                candidate.Context = context;
                candidate.Status = PooledObject.RentalState.Rented;
                bucket.Active.Add(candidate);
                ActiveCount++;
                bucket.Peak = Math.Max(bucket.Peak, bucket.Active.Count);
                PeakActive = Math.Max(PeakActive, ActiveCount);
                TotalRents++;
                var pose = candidate.transform;
                pose.SetParent(parent, false);
                pose.localPosition = context.LocalPosition;
                pose.localRotation = context.LocalRotation;
                pose.localScale = candidate.AuthoredScale;
                candidate.OnRent(context);
                if (candidate.IsRented && !shutdownRequested) candidate.gameObject.SetActive(true);
                // OnRent or OnEnable may explicitly return this activation or request shutdown.
                if (!candidate.IsRented || candidate.ActivationId != token || shutdownRequested)
                    return Miss(context.Key, bucket, PoolRentFailure.ReturnedDuringCallback);
                instance = candidate;
                LastRentFailure = PoolRentFailure.None;
                return true;
            }
            catch (Exception exception)
            {
                if (candidate && candidate.IsRented) Return(candidate, token);
                Debug.LogException(exception, this);
                return Miss(context.Key, bucket, PoolRentFailure.CallbackFailed);
            }
            finally { ExitOperation(); }
        }

        public bool Return(PooledObject instance, ulong expectedActivationId)
        {
            if (!instance || instance.Owner != this || !instance.IsRented || expectedActivationId == 0 ||
                instance.ActivationId != expectedActivationId) return false;
            Bucket bucket = GetBucket(instance.Key);
            if (bucket == null || !bucket.Active.Contains(instance)) return false;
            if (instance is Interactable traffic && traffic.Registry && traffic.Registry.IsDispatching)
                return traffic.Registry.DeferReturn(traffic, expectedActivationId);
            operationDepth++;
            try
            {
                instance.Status = PooledObject.RentalState.Returning;
                bucket.Active.Remove(instance);
                ActiveCount--;
                try { instance.OnReturn(); }
                catch (Exception exception) { Debug.LogException(exception, this); }
                finally
                {
                    instance.gameObject.SetActive(false);
                    instance.transform.SetParent(storageRoot, false);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = instance.AuthoredScale;
                    instance.Context = default;
                    instance.Status = PooledObject.RentalState.Inactive;
                    bucket.Inactive.Push(instance);
                    InactiveCount++;
                    TotalReturns++;
                }
                return true;
            }
            finally { ExitOperation(); }
        }

        private bool returningAll;
        /// <summary>Stable bootstrap arrays tolerate nested returns; recursive ReturnAll is a no-op.</summary>
        public void ReturnAll()
        {
            if (returningAll || buckets == null) return;
            operationDepth++;
            returningAll = true;
            try
            {
                for (int key = 0; key < buckets.Length; key++)
                {
                    Bucket bucket = buckets[key];
                    if (bucket == null) continue;
                    for (int i = 0; i < bucket.Instances.Length; i++)
                    {
                        PooledObject item = bucket.Instances[i];
                        if (item && item.IsRented) Return(item, item.ActivationId);
                    }
                }
            }
            finally { returningAll = false; ExitOperation(); }
        }

        public void Shutdown()
        {
            if (shuttingDown) return;
            IsReady = false;
            shutdownRequested = true;
            if (operationDepth == 0) FinishShutdown();
        }

        private void FinishShutdown()
        {
            shuttingDown = true;
            try
            {
                ReturnAll();
                if (buckets != null)
                    foreach (Bucket bucket in buckets)
                    {
                        if (bucket == null) continue;
                        foreach (PooledObject item in bucket.Instances)
                        {
                            if (!item) continue;
                            item.Owner = null;
                            if (Application.isPlaying) Destroy(item.gameObject);
                            else DestroyImmediate(item.gameObject);
                            TotalDestroyed++;
                        }
                    }
                buckets = null;
                Capacity = ActiveCount = InactiveCount = PeakActive = 0;
            }
            finally { shutdownRequested = shuttingDown = false; }
        }

        private void ExitOperation()
        {
            operationDepth--;
            if (operationDepth == 0 && shutdownRequested && !shuttingDown) FinishShutdown();
        }
        private Bucket GetBucket(PoolKey key) => buckets != null && PoolConfig.IsValidKey(key) ? buckets[(int)key] : null;
        private bool Miss(PoolKey key, Bucket bucket, PoolRentFailure reason)
        {
            Misses++;
            if (bucket != null) bucket.Misses++;
            LastMissKey = key;
            LastRentFailure = reason;
            return false;
        }
        private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        private static bool Finite(Quaternion q) => float.IsFinite(q.x) && float.IsFinite(q.y) &&
            float.IsFinite(q.z) && float.IsFinite(q.w) && (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w) > 0f;
        private void OnDestroy() => Shutdown();
    }
}
