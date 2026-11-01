using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class PoolLifecycleTests
    {
        private GameObject root, template;
        private GameplayConfig gameplay;
        private PoolConfig catalog;
        private GameManager manager;
        private PoolService pools;
        private float originalTimeScale;

        [SetUp] public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            Assert.That(GameManager.Instance, Is.Null);
            gameplay = ScriptableObject.CreateInstance<GameplayConfig>();
            catalog = ScriptableObject.CreateInstance<PoolConfig>();
            template = new GameObject("PoolProbeTemplate");
            template.SetActive(false);
            template.AddComponent<PoolProbe>();
            SetEntries(new PoolConfig.Entry(PoolKey.Coin, template, 4));
            root = new GameObject("PoolLifecycleFixture");
            root.SetActive(false);
            var loop = root.AddComponent<GameplayLoop>();
            manager = root.AddComponent<GameManager>();
            Assign(manager, "gameplayConfig", gameplay);
            Assign(manager, "gameplayLoop", loop);
            Assign(loop, "gameManager", manager);
            PlayModeMovementRig.Wire(root, manager, loop, gameplay);
            pools = root.GetComponent<PoolService>();
            Assign(pools, "configuration", catalog);
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            if (manager) manager.Shutdown();
            if (root) Object.Destroy(root);
            if (template) Object.Destroy(template);
            if (catalog) Object.Destroy(catalog);
            if (gameplay) Object.Destroy(gameplay);
            Time.timeScale = originalTimeScale;
            yield return null;
            Assert.That(GameManager.Instance, Is.Null);
        }

        private void Bootstrap()
        {
            root.SetActive(true);
            Assert.That(manager.IsOwner, Is.True);
            Assert.That(pools.IsReady, Is.True, pools.LastError);
        }
        private SpawnContext Context(PoolKey key = PoolKey.Coin, long sequence = 1, Transform parent = null) =>
            new SpawnContext(key, new Vector3(3.2f, 0.6f, 10f), Quaternion.Euler(0f, 20f, 0f), parent,
                1, 123456789.123456789d, 73, 22f, sequence);
        private PoolProbe Rent(long sequence = 1)
        {
            Assert.That(pools.TryRent(Context(sequence: sequence), out var item), Is.True, pools.LastRentFailure.ToString());
            return (PoolProbe)item;
        }
        private void AssertRestored(int capacity = 4, int roadActive = 0)
        {
            Assert.That(pools.ActiveCount, Is.EqualTo(roadActive));
            Assert.That(pools.InactiveCount, Is.EqualTo(capacity - roadActive));
            Assert.That(pools.Capacity, Is.EqualTo(capacity));
            Assert.That(pools.TotalCreated, Is.EqualTo((ulong)capacity));
            Assert.That(pools.TotalDestroyed, Is.Zero);
        }

        [Test] public void PrewarmStagesActivePrefabAndInitializesExactlyOnce()
        {
            template.SetActive(true);
            Bootstrap();
            Assert.That(pools.TotalCreated, Is.EqualTo(4));
            Assert.That(pools.Initialize(), Is.True);
            Assert.That(pools.Initialize(), Is.True);
            foreach (var probe in pools.StorageRoot.GetComponentsInChildren<PoolProbe>(true))
            {
                Assert.That(probe.Enables, Is.Zero);
                Assert.That(probe.Rents, Is.Zero);
                Assert.That(probe.IsRented || probe.gameObject.activeSelf, Is.False);
            }
            AssertRestored();
        }

        [Test] public void ThousandRentReturnsForEveryKeyPreserveIdentityAndCounts()
        {
            var entries = new PoolConfig.Entry[12];
            for (int i = 0; i < entries.Length; i++) entries[i] = new PoolConfig.Entry((PoolKey)i, template, 2);
            SetEntries(entries);
            Bootstrap();
            var ids = new HashSet<EntityId>();
            ulong lastToken = 0;
            for (int key = 0; key < 12; key++)
            {
                for (int i = 0; i < 1000; i++)
                {
                    var context = Context((PoolKey)key, i);
                    Assert.That(pools.TryRent(context, out var item), Is.True);
                    var probe = (PoolProbe)item;
                    ids.Add(item.GetEntityId());
                    Assert.That(item.ActivationId, Is.GreaterThan(lastToken));
                    lastToken = item.ActivationId;
                    Assert.That(item.Context.AbsoluteDistance, Is.EqualTo(context.AbsoluteDistance));
                    Assert.That(item.Context.SpawnSequence, Is.EqualTo(i));
                    Assert.That(probe.RentedWhileInactive && probe.EnableSawCurrentContext, Is.True);
                    probe.DirtyValue = 999;
                    Assert.That(item.ReturnToPool(lastToken), Is.True);
                    Assert.That(probe.DirtyValue, Is.Zero);
                    Assert.That(probe.CleanupSawUnregistered, Is.True);
                    Assert.That(item.IsRented || item.gameObject.activeSelf, Is.False);
                }
                Assert.That(pools.TryGetCounters((PoolKey)key, out var count), Is.True);
                Assert.That(count.Created, Is.EqualTo(2));
                Assert.That(count.Active, Is.Zero);
                Assert.That(count.Inactive, Is.EqualTo(2));
                Assert.That(count.PeakActive, Is.EqualTo(1));
            }
            Assert.That(ids.Count, Is.EqualTo(12)); // LIFO reuse of exactly one identity per key.
            Assert.That(pools.TotalRents, Is.EqualTo(12000));
            Assert.That(pools.TotalReturns, Is.EqualTo(12000));
            AssertRestored(24);
            Debug.Log($"[Task04] 12000 rents/returns; created={pools.TotalCreated}, destroyed={pools.TotalDestroyed}, capacity={pools.Capacity}.");
        }

        [Test] public void ExhaustionAndUnknownKeysFailWithoutGrowing()
        {
            Bootstrap();
            var ids = new HashSet<EntityId>();
            for (int i = 0; i < 4; i++) ids.Add(Rent().GetEntityId());
            Assert.That(ids.Count, Is.EqualTo(4));
            Assert.That(pools.TryRent(Context(), out var missing), Is.False);
            Assert.That(missing, Is.Null);
            Assert.That(pools.LastRentFailure, Is.EqualTo(PoolRentFailure.Exhausted));
            Assert.That(pools.TryRent(Context(PoolKey.Fuel), out _), Is.False);
            Assert.That(pools.LastRentFailure, Is.EqualTo(PoolRentFailure.UnknownKey));
            Assert.That(pools.LastMissKey, Is.EqualTo(PoolKey.Fuel));
            Assert.That(pools.TryRent(Context((PoolKey)(-1)), out _), Is.False);
            Assert.That(pools.Misses, Is.EqualTo(3));
            Assert.That(pools.TryGetCounters(PoolKey.Coin, out var counters), Is.True);
            Assert.That(counters.Misses, Is.EqualTo(1));
            Assert.That(counters.PeakActive, Is.EqualTo(4));
            pools.ReturnAll(); pools.ReturnAll();
            AssertRestored();
        }

        [Test] public void CapturedOldTokenDoubleAndForeignReturnsCannotEndCurrentRental()
        {
            Bootstrap();
            var item = Rent();
            ulong capturedOldToken = item.ActivationId;
            Func<bool> delayedCallback = () => item.ReturnToPool(capturedOldToken);
            Assert.That(pools.Return(item, capturedOldToken), Is.True);
            Assert.That(pools.Return(item, capturedOldToken), Is.False);
            var reused = Rent(2);
            Assert.That(reused, Is.SameAs(item));
            Assert.That(delayedCallback(), Is.False);
            Assert.That(pools.Return(template.GetComponent<PoolProbe>(), reused.ActivationId), Is.False);
            var foreign = new GameObject("ForeignPool").AddComponent<PoolService>();
            try { Assert.That(foreign.Return(reused, reused.ActivationId), Is.False); }
            finally { Object.Destroy(foreign.gameObject); }
            Assert.That(reused.IsRented, Is.True);
            Assert.That(pools.ActiveCount, Is.EqualTo(1));
            Assert.That(reused.Returns, Is.EqualTo(1));
            pools.ReturnAll();
            AssertRestored();
        }

        [TestCase(false)] [TestCase(true)]
        public void ReturnFromRentOrEnableCancelsRentalWithoutExposingIt(bool fromEnable)
        {
            Bootstrap();
            foreach (var probe in pools.StorageRoot.GetComponentsInChildren<PoolProbe>(true))
            {
                Action<PoolProbe> callback = item => item.ReturnToPool(item.ActivationId);
                if (fromEnable) probe.EnableAction = callback;
                else probe.RentAction = callback;
            }
            Assert.That(pools.TryRent(Context(), out var cancelled), Is.False);
            Assert.That(cancelled, Is.Null);
            Assert.That(pools.LastRentFailure, Is.EqualTo(PoolRentFailure.ReturnedDuringCallback));
            Assert.That(pools.TotalReturns, Is.EqualTo(1));
            AssertRestored();
        }

        [Test] public void CallbackReturnAllAndNestedOperationsAreDeterministic()
        {
            Bootstrap();
            var first = Rent();
            var second = Rent();
            first.ReturnAction = item =>
            {
                Assert.That(item.ReturnToPool(item.ActivationId), Is.False);
                Assert.That(pools.TryRent(Context(), out _), Is.False);
                Assert.That(pools.LastRentFailure, Is.EqualTo(PoolRentFailure.Busy));
                pools.ReturnAll();
                pools.Return(second, second.ActivationId);
            };
            second.ReturnAction = _ => pools.ReturnAll();
            pools.ReturnAll(); pools.ReturnAll();
            Assert.That(first.Returns, Is.EqualTo(1));
            Assert.That(second.Returns, Is.EqualTo(1));
            AssertRestored();
        }

        [Test] public void ReturnAllFromRentHookReturnsOlderRentalsAndRejectsNestedRent()
        {
            Bootstrap();
            var older = Rent();
            foreach (var probe in pools.StorageRoot.GetComponentsInChildren<PoolProbe>(true))
                probe.RentAction = self =>
                {
                    Assert.That(pools.TryRent(Context(), out _), Is.False);
                    pools.ReturnAll();
                };
            Assert.That(pools.TryRent(Context(), out _), Is.False);
            Assert.That(older.Returns, Is.EqualTo(1));
            Assert.That(pools.TotalReturns, Is.EqualTo(2));
            AssertRestored();
        }

        [Test] public void ExternalDisableRetainsOwnershipUntilExplicitReturnAndDoesNotRecurse()
        {
            Bootstrap();
            var item = Rent();
            ulong token = item.ActivationId;
            item.gameObject.SetActive(false);
            Assert.That(item.IsRented, Is.True);
            Assert.That(pools.ActiveCount, Is.EqualTo(1));
            Assert.That(item.Returns, Is.Zero);
            item.DisableAction = probe => Assert.That(probe.ReturnToPool(token), Is.False);
            item.gameObject.SetActive(true);
            Assert.That(item.ReturnToPool(token), Is.True);
            Assert.That(item.Returns, Is.EqualTo(1));
            AssertRestored();
        }

        [Test] public void RentUsesActiveParentAndRestoresAuthoredPoseAndScale()
        {
            template.transform.localScale = new Vector3(2f, 3f, 4f);
            Bootstrap();
            var parent = new GameObject("ActiveWorld").transform;
            parent.SetParent(root.transform, false);
            parent.localPosition = Vector3.one * 100f;
            var context = Context(parent: parent);
            Assert.That(pools.TryRent(context, out var item), Is.True);
            Assert.That(item.transform.parent, Is.SameAs(parent));
            Assert.That(item.transform.localPosition, Is.EqualTo(context.LocalPosition));
            Assert.That(Quaternion.Angle(item.transform.localRotation, context.LocalRotation), Is.LessThan(0.001f));
            Assert.That(item.transform.localScale, Is.EqualTo(template.transform.localScale));
            Assert.That(item.gameObject.activeInHierarchy, Is.True);
            item.transform.localScale = Vector3.one * 99;
            pools.ReturnAll();
            Assert.That(item.transform.parent, Is.SameAs(pools.StorageRoot));
            Assert.That(pools.TryRent(Context(parent: pools.StorageRoot), out _), Is.False);
            Assert.That(pools.LastRentFailure, Is.EqualTo(PoolRentFailure.InactiveParent));
            parent.gameObject.SetActive(false);
            Assert.That(pools.TryRent(Context(parent: parent), out _), Is.False);
            AssertRestored();
        }

        [Test] public void FiftyRestartsGateInputBeforeCleanupAndPreservePools()
        {
            var realRoads = UnityEditor.AssetDatabase.LoadAssetAtPath<PoolConfig>("Assets/NeonTunnelRunner/Configs/Pools.asset");
            SetEntries(realRoads.GetEntry(0), realRoads.GetEntry(1), realRoads.GetEntry(2), realRoads.GetEntry(3),
                new PoolConfig.Entry(PoolKey.Coin, template, 4));
            Bootstrap();
            Assert.That(manager.StartRun(), Is.True);
            ulong previousToken = 0;
            int cleanupCallbacks = 0;
            for (int cycle = 0; cycle < 50; cycle++)
            {
                var first = Rent(cycle);
                var second = Rent(cycle);
                Assert.That(first.ActivationId, Is.GreaterThan(previousToken));
                previousToken = second.ActivationId;
                Action<PoolProbe> onReturn = _ =>
                {
                    cleanupCallbacks++;
                    Assert.That(manager.Loop.IsRunning, Is.False);
                    Assert.That(manager.Loop.Input.Actions.FindActionMap("Gameplay").enabled, Is.False);
                    Assert.That(manager.Loop.Step(0.1f), Is.False);
                };
                first.ReturnAction = second.ReturnAction = onReturn;
                if (cycle % 2 == 0)
                {
                    manager.Pause();
                    Assert.That(first.IsRented && second.IsRented, Is.True);
                    manager.Resume();
                    Assert.That(second.ActivationId, Is.EqualTo(previousToken));
                    Assert.That(pools.ActiveCount, Is.EqualTo(8));
                    manager.Pause();
                }
                else
                {
                    manager.EndRun(GameOverReason.Lives);
                    Assert.That(pools.ActiveCount, Is.EqualTo(8));
                }
                Assert.That(manager.Restart(), Is.True);
                AssertRestored(28, 6);
                Assert.That(manager.Loop.Player.LaneIndex, Is.Zero);
                Assert.That(manager.Loop.Input.Actions.FindActionMap("Gameplay").enabled, Is.True);
            }
            Assert.That(cleanupCallbacks, Is.EqualTo(100));
            Assert.That(pools.TotalReturns, Is.EqualTo(400));
            Rent();
            manager.Pause();
            Assert.That(manager.ReturnToMenu(), Is.True);
            AssertRestored(28);
            Assert.That(manager.StartRun(), Is.True);
            Debug.Log($"[Task04] 50 restarts + menu; created={pools.TotalCreated}, destroyed={pools.TotalDestroyed}, cleanupCallbacks={cleanupCallbacks}.");
        }

        [TestCase(false)] [TestCase(true)]
        public void EmptyOrInvalidCatalogBlocksRequiredWorldWithoutPartialRun(bool invalid)
        {
            if (invalid) SetEntries(new PoolConfig.Entry(PoolKey.Coin, null, 4));
            else SetEntries();
            root.SetActive(true);
            var before = manager.Session.Snapshot;
            int events = 0;
            manager.StateChanged += (_, __) => events++;
            if (invalid) LogAssert.Expect(LogType.Error, "[GameManager] Cannot start: Pools entry 0 (Coin): assign a prefab. Fix PoolConfig before StartRun.");
            else LogAssert.Expect(LogType.Error, "[GameManager] Cannot start: Required road pool RoadOpen needs capacity >= 6.");
            Assert.That(manager.StartRun(), Is.False);
            {
                Assert.That(manager.State, Is.EqualTo(GameState.Menu));
                Assert.That(manager.Session.Snapshot, Is.EqualTo(before));
                Assert.That(events, Is.Zero);
                Assert.That(manager.Loop.Input.Actions.FindActionMap("Gameplay").enabled, Is.False);
                Assert.That(manager.Loop.IsRunning, Is.False);
            }
            Assert.That(pools.TotalCreated, Is.Zero);
        }

        [Test] public void ShutdownFromCallbackIsDeferredAndDestroysOnlyAtShutdown()
        {
            Bootstrap();
            var item = Rent();
            item.ReturnAction = _ => pools.Shutdown();
            Assert.That(item.ReturnToPool(item.ActivationId), Is.True);
            Assert.That(item.Returns, Is.EqualTo(1));
            Assert.That(pools.IsReady, Is.False);
            Assert.That(pools.TotalDestroyed, Is.EqualTo(4));
            Assert.That(pools.ActiveCount + pools.InactiveCount, Is.Zero);
            pools.Shutdown();
            Assert.That(pools.TotalDestroyed, Is.EqualTo(4));
        }

        [Test] public void SuccessfulSteadyStatePoolOperationsAllocateNoManagedBytes()
        {
            Bootstrap();
            var context = Context();
            for (int i = 0; i < 32; i++)
            {
                pools.TryRent(context, out var warm);
                pools.Return(warm, warm.ActivationId);
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            bool success = true;
            for (int i = 0; i < 1000; i++)
            {
                success &= pools.TryRent(context, out var item);
                success &= pools.Return(item, item.ActivationId);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(success, Is.True);
            Assert.That(allocated, Is.Zero, "Measured only synchronous pool operations after warmup.");
            Debug.Log($"[Task04] 1000 measured steady-state rents/returns: {allocated} managed bytes on current thread.");
        }

        private void SetEntries(params PoolConfig.Entry[] entries) => Assign(catalog, "entries", entries);
        private static void Assign(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
