using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class RestartTests
    {
        private GameObject root;
        private GameplayConfig config;
        private GameManager manager;
        private GameplayLoop loop;
        private float originalTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalTimeScale = Time.timeScale;
            Assert.That(GameManager.Instance, Is.Null, "A previous fixture retained its scene owner.");
            config = ScriptableObject.CreateInstance<GameplayConfig>();
            root = CreateFixture("LifecycleFixture", config, 700);
            manager = root.GetComponent<GameManager>();
            loop = root.GetComponent<GameplayLoop>();
            yield return null;
            Assert.That(GameManager.Instance, Is.SameAs(manager));
            Assert.That(manager.State, Is.EqualTo(GameState.Menu));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Destroy is safe even when the owner paused the simulation; yield an unscaled frame.
            if (root) Object.Destroy(root);
            yield return null;
            if (config) Object.Destroy(config);
            Time.timeScale = originalTimeScale;
            Assert.That(GameManager.Instance, Is.Null);
        }

        [UnityTest]
        public IEnumerator FixedUpdateRunsOnlyWhilePlayingAndResumePreservesValues()
        {
            Assert.That(loop.TickIndex, Is.Zero);
            manager.StartRun();
            yield return new WaitForFixedUpdate();
            Assert.That(loop.TickIndex, Is.GreaterThan(0));
            Assert.That(manager.Session.Snapshot.ElapsedTime, Is.GreaterThan(0d));
            manager.Pause();
            RunSnapshot paused = manager.Session.Snapshot;
            ulong tick = loop.TickIndex;
            // Explicit calls must also respect state if another system changes timeScale.
            Time.timeScale = 1f;
            Assert.That(loop.Step(10f), Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(loop.TickIndex, Is.EqualTo(tick));
            Assert.That(manager.Session.Snapshot, Is.EqualTo(paused));
            Time.timeScale = 0f;
            manager.Resume();
            Assert.That(manager.Session.Snapshot.RunId, Is.EqualTo(paused.RunId));
            Assert.That(manager.Session.Snapshot.ElapsedTime, Is.EqualTo(paused.ElapsedTime));
            Assert.That(manager.Session.Snapshot.Fuel, Is.EqualTo(100f));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            manager.EndRun(GameOverReason.Fuel);
            Assert.That(loop.Step(10f), Is.False);
        }

        [UnityTest]
        public IEnumerator FiftyRestartsResetDataAndKeepSingleSubscriptions()
        {
            manager.StartRun();
            RunSession session = manager.Session;
            int transitions = 0, snapshots = 0;
            Action<GameState, GameState> listener = (_, __) => transitions++;
            Action<RunSnapshot> snapshotListener = _ => snapshots++;
            manager.StateChanged += listener;
            session.SnapshotChanged += snapshotListener;
            for (int i = 0; i < 50; i++)
            {
                session.Advance(5f, 123.123456789d);
                session.SetResources(1, 10f);
                session.SetScore(9, 1023);
                session.RetainBestScore(1500);
                loop.Step(0.1f);
                Guid previous = session.Snapshot.RunId;
                if (i % 2 == 0) manager.Pause();
                else manager.EndRun(GameOverReason.Lives);
                manager.enabled = false;
                manager.enabled = true;
                Assert.That(manager.Restart(), Is.True);
                RunSnapshot reset = session.Snapshot;
                Assert.That(manager.Session, Is.SameAs(session));
                Assert.That(reset.RunId, Is.Not.EqualTo(previous));
                Assert.That(reset.Lives, Is.EqualTo(3));
                Assert.That(reset.Fuel, Is.EqualTo(100f));
                Assert.That(reset.ElapsedTime, Is.Zero);
                Assert.That(reset.Distance, Is.Zero);
                Assert.That(reset.Coins, Is.Zero);
                Assert.That(reset.Score, Is.Zero);
                Assert.That(reset.BestScore, Is.EqualTo(1500));
                Assert.That(reset.ActualSpeed, Is.EqualTo(22f));
                Assert.That(manager.EndReason, Is.EqualTo(GameOverReason.None));
                Assert.That(loop.TickIndex, Is.Zero);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            Assert.That(transitions, Is.EqualTo(100));
            Assert.That(snapshots, Is.EqualTo(150)); // One step and two transitions per cycle.
            manager.StateChanged -= listener;
            session.SnapshotChanged -= snapshotListener;
            manager.Pause();
            Assert.That(transitions, Is.EqualTo(100));
            Assert.That(snapshots, Is.EqualTo(150));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RejectedDuplicateCannotReplaceOwnerOrUnpauseOnDestruction()
        {
            manager.StartRun();
            manager.Pause();
            RunSnapshot before = manager.Session.Snapshot;
            LogAssert.Expect(LogType.Warning, "[GameManager] Duplicate rejected; the existing owner is unchanged.");
            GameObject duplicate = CreateFixture("DuplicateFixture", config, 0);
            try
            {
                Assert.That(GameManager.Instance, Is.SameAs(manager));
                Assert.That(Time.timeScale, Is.Zero);
                yield return null; // Rejected component's deferred Destroy/OnDestroy runs now.
                Assert.That(duplicate.GetComponent<GameManager>(), Is.Null);
                Assert.That(GameManager.Instance, Is.SameAs(manager));
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(manager.Session.Snapshot, Is.EqualTo(before));
                Assert.That(manager.Resume(), Is.True);
            }
            finally { Object.Destroy(duplicate); }
        }

        [UnityTest]
        public IEnumerator OwnerTeardownAtZeroTimeClearsSingletonAndObservers()
        {
            manager.StartRun();
            manager.EndRun(GameOverReason.Lives);
            RunSession oldSession = manager.Session;
            int notifications = 0;
            oldSession.SnapshotChanged += _ => notifications++;
            Object.Destroy(root);
            yield return null;
            Assert.That(GameManager.Instance, Is.Null);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            oldSession.PublishSnapshot();
            Assert.That(notifications, Is.Zero);
            root = CreateFixture("NextSceneOwner", config, 700);
            manager = root.GetComponent<GameManager>();
            loop = root.GetComponent<GameplayLoop>();
            Assert.That(manager.State, Is.EqualTo(GameState.Menu));
            Assert.That(manager.Session, Is.Not.SameAs(oldSession));
            Assert.That(manager.StartRun(), Is.True);
            manager.Pause(); // Teardown must also handle Paused, not just GameOver.
        }

        [UnityTest]
        public IEnumerator RepeatedSceneLoadsStartInMenuWithOneWiredOwner()
        {
            // Release only this test's fixture before loading the real saved scene.
            Object.Destroy(root);
            yield return null;
            string fixtureScene = SceneManager.GetActiveScene().name;
            Scene loaded = default;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
                    loaded = SceneManager.GetSceneByName("Game");
                    var owner = GameManager.Instance;
                    Assert.That(owner, Is.Not.Null);
                    Assert.That(owner.gameObject.scene, Is.EqualTo(loaded));
                    Assert.That(owner.gameObject.name, Is.EqualTo("Systems"));
                    Assert.That(owner.Loop.Owner, Is.SameAs(owner));
                    Assert.That(owner.Configuration, Is.Not.Null);
                    Assert.That(owner.State, Is.EqualTo(GameState.Menu));
                    Assert.That(owner.Session.Snapshot.BestScore, Is.Zero);
                    owner.StartRun();
                    owner.Pause();
                    yield return SceneManager.UnloadSceneAsync(loaded);
                    loaded = default;
                    Assert.That(GameManager.Instance, Is.Null);
                    Assert.That(Time.timeScale, Is.EqualTo(1f));
                }
                Assert.That(SceneManager.GetSceneByName(fixtureScene).isLoaded, Is.True);
            }
            finally
            {
                if (loaded.IsValid() && loaded.isLoaded)
                {
                    var owner = GameManager.Instance;
                    if (owner && owner.gameObject.scene == loaded) owner.Shutdown();
                    SceneManager.UnloadSceneAsync(loaded);
                }
            }
        }

        private static GameObject CreateFixture(string name, GameplayConfig config, int bestScore)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            var loop = go.AddComponent<GameplayLoop>();
            var manager = go.AddComponent<GameManager>();
            Assign(manager, "gameplayConfig", config);
            Assign(manager, "gameplayLoop", loop);
            Assign(manager, "initialBestScore", bestScore);
            Assign(loop, "gameManager", manager);
            go.SetActive(true);
            return go;
        }

        private static void Assign(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
