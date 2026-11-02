using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class GameStateTests
    {
        private GameObject root;
        private GameManager manager;
        private GameplayLoop loop;
        private GameplayConfig config;
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            Assert.That(GameManager.Instance, Is.Null, "Another fixture retained singleton ownership.");
            config = ScriptableObject.CreateInstance<GameplayConfig>();
            root = new GameObject("LifecycleFixture");
            root.SetActive(false);
            loop = root.AddComponent<GameplayLoop>();
            manager = root.AddComponent<GameManager>();
            Assign(manager, "gameplayConfig", config);
            Assign(manager, "gameplayLoop", loop);
            Assign(manager, "runSeed", 84);
            Assign(manager, "initialBestScore", 900);
            Assign(loop, "gameManager", manager);
            EditModeMovementRig.Wire(root, manager, loop, config);
            root.SetActive(true);
            Assert.That(manager.Initialize(), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
            if (manager) manager.Shutdown();
            if (root) Object.DestroyImmediate(root);
            if (config) Object.DestroyImmediate(config);
            Time.timeScale = originalTimeScale;
            Assert.That(GameManager.Instance, Is.Null);
        }

        [Test]
        public void InitialMenuHasNoActiveRunAndDoesNotSimulate()
        {
            Assert.That(manager.State, Is.EqualTo(GameState.Menu));
            Assert.That(manager.EndReason, Is.EqualTo(GameOverReason.None));
            Assert.That(manager.Session.Snapshot.RunId, Is.EqualTo(Guid.Empty));
            Assert.That(manager.Session.Snapshot.Lives, Is.EqualTo(3));
            Assert.That(manager.Session.Snapshot.Fuel, Is.EqualTo(100f));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(loop.Step(1f), Is.False);
            Assert.That(loop.TickIndex, Is.Zero);
        }

        [TestCase(GameState.Menu, "Start", GameState.Playing)]
        [TestCase(GameState.Playing, "Pause", GameState.Paused)]
        [TestCase(GameState.Paused, "Resume", GameState.Playing)]
        [TestCase(GameState.Paused, "Restart", GameState.Playing)]
        [TestCase(GameState.Paused, "Menu", GameState.Menu)]
        [TestCase(GameState.Playing, "End", GameState.GameOver)]
        [TestCase(GameState.GameOver, "Restart", GameState.Playing)]
        public void PermittedTransitionCommitsDataBeforeOneStateEvent(GameState from, string command, GameState to)
        {
            Reach(from);
            int states = 0;
            int snapshots = 0;
            manager.Session.SnapshotChanged += snapshot =>
            {
                snapshots++;
                Assert.That(manager.State, Is.EqualTo(to));
                Assert.That(Time.timeScale, Is.EqualTo(to == GameState.Paused || to == GameState.GameOver ? 0f : 1f));
            };
            manager.StateChanged += (oldState, newState) =>
            {
                states++;
                Assert.That(oldState, Is.EqualTo(from));
                Assert.That(newState, Is.EqualTo(to));
            };
            Assert.That(Command(command), Is.True);
            Assert.That(manager.State, Is.EqualTo(to));
            Assert.That(states, Is.EqualTo(1));
            Assert.That(snapshots, Is.EqualTo(1));
        }

        [TestCase(GameState.Menu, "Pause")]
        [TestCase(GameState.Menu, "Resume")]
        [TestCase(GameState.Menu, "Restart")]
        [TestCase(GameState.Menu, "Menu")]
        [TestCase(GameState.Menu, "End")]
        [TestCase(GameState.Playing, "Start")]
        [TestCase(GameState.Playing, "Resume")]
        [TestCase(GameState.Playing, "Restart")]
        [TestCase(GameState.Playing, "Menu")]
        [TestCase(GameState.Playing, "Quit")]
        [TestCase(GameState.Paused, "Start")]
        [TestCase(GameState.Paused, "Pause")]
        [TestCase(GameState.Paused, "End")]
        [TestCase(GameState.Paused, "Quit")]
        [TestCase(GameState.GameOver, "Start")]
        [TestCase(GameState.GameOver, "Pause")]
        [TestCase(GameState.GameOver, "Resume")]
        [TestCase(GameState.GameOver, "Menu")]
        [TestCase(GameState.GameOver, "End")]
        [TestCase(GameState.GameOver, "Quit")]
        public void RejectedCommandsDoNotMutateOrPublish(GameState state, string command)
        {
            Reach(state);
            RunSnapshot before = manager.Session.Snapshot;
            GameOverReason reason = manager.EndReason;
            ulong tick = loop.TickIndex;
            float scale = Time.timeScale;
            int states = 0, snapshots = 0, ends = 0;
            manager.StateChanged += (_, __) => states++;
            manager.Session.SnapshotChanged += _ => snapshots++;
            manager.RunEnded += (_, __) => ends++;
            Assert.That(Command(command), Is.False);
            Assert.That(manager.Session.Snapshot, Is.EqualTo(before));
            Assert.That(manager.EndReason, Is.EqualTo(reason));
            Assert.That(manager.State, Is.EqualTo(state));
            Assert.That(loop.TickIndex, Is.EqualTo(tick));
            Assert.That(Time.timeScale, Is.EqualTo(scale));
            Assert.That(states + snapshots + ends, Is.Zero);
        }

        [Test]
        public void PauseAndResumePreserveTheRunAndCruiseTarget()
        {
            manager.StartRun();
            manager.Session.Advance(2f, 123.123456789d);
            manager.Session.SetResources(2, 73f);
            manager.Session.SetScore(4, 523);
            manager.Session.SetCruiseSpeed(31f);
            loop.Step(0.25f);
            RunSnapshot playing = manager.Session.Snapshot;
            manager.Pause();
            RunSnapshot paused = manager.Session.Snapshot;
            Assert.That(paused.ActualSpeed, Is.Zero);
            Assert.That(paused.CruiseSpeed, Is.EqualTo(31f));
            Assert.That(loop.Step(5f), Is.False);
            Assert.That(manager.Session.Snapshot, Is.EqualTo(paused));
            Assert.That(manager.Resume(), Is.True);
            Assert.That(manager.Session.Snapshot, Is.EqualTo(playing));
            Assert.That(loop.TickIndex, Is.EqualTo(1));
        }

        [Test]
        public void RestartResetsTransientDataAndPreservesOnlyBestAndSeed()
        {
            RunSession session = manager.Session;
            manager.StartRun();
            RunSnapshot first = session.Snapshot;
            session.Advance(12f, 321.123456789d);
            session.SetResources(1, 14f);
            session.SetScore(12, 1521);
            session.RetainBestScore(2000);
            session.SetCruiseSpeed(40f);
            loop.Step(0.2f);
            manager.Pause();
            manager.Restart();
            RunSnapshot reset = session.Snapshot;
            Assert.That(manager.Session, Is.SameAs(session));
            Assert.That(reset.RunId, Is.Not.EqualTo(first.RunId));
            Assert.That(reset.Seed, Is.EqualTo(84));
            Assert.That(reset.Lives, Is.EqualTo(3));
            Assert.That(reset.Fuel, Is.EqualTo(100f));
            Assert.That(reset.ElapsedTime, Is.Zero);
            Assert.That(reset.Distance, Is.Zero);
            Assert.That(reset.Coins, Is.Zero);
            Assert.That(reset.Score, Is.Zero);
            Assert.That(reset.BestScore, Is.EqualTo(2000));
            Assert.That(reset.CruiseSpeed, Is.EqualTo(22f));
            Assert.That(reset.ActualSpeed, Is.EqualTo(22f));
            Assert.That(loop.TickIndex, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [TestCase(GameOverReason.Lives, GameOverReason.Fuel)]
        [TestCase(GameOverReason.Fuel, GameOverReason.Lives)]
        public void FirstTerminalResultIsRetained(GameOverReason first, GameOverReason repeated)
        {
            manager.StartRun();
            int ended = 0;
            manager.RunEnded += (snapshot, reason) =>
            {
                ended++;
                Assert.That(reason, Is.EqualTo(first));
                Assert.That(snapshot.ActualSpeed, Is.Zero);
                Assert.That(manager.State, Is.EqualTo(GameState.GameOver));
            };
            Assert.That(manager.EndRun(first), Is.True);
            RunSnapshot terminal = manager.Session.Snapshot;
            Assert.That(manager.EndRun(repeated), Is.False);
            Assert.That(manager.EndRun(first), Is.False);
            Assert.That(loop.Step(3f), Is.False);
            Assert.That(manager.EndReason, Is.EqualTo(first));
            Assert.That(manager.Session.Snapshot, Is.EqualTo(terminal));
            Assert.That(ended, Is.EqualTo(1));
            manager.Restart();
            Assert.That(manager.EndReason, Is.EqualTo(GameOverReason.None));
        }

        [TestCase(GameOverReason.None)]
        [TestCase((GameOverReason)99)]
        public void UndefinedTerminalReasonDoesNotEndTheRun(GameOverReason reason)
        {
            manager.StartRun();
            Assert.That(manager.EndRun(reason), Is.False);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(manager.EndReason, Is.EqualTo(GameOverReason.None));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void ReturnToMenuCleansLoopAndNextStartCreatesANewRun()
        {
            manager.StartRun();
            loop.Step(0.5f);
            manager.Pause();
            Guid previousRun = manager.Session.Snapshot.RunId;
            manager.ReturnToMenu();
            Assert.That(loop.TickIndex, Is.Zero);
            Assert.That(loop.Step(1f), Is.False); // Menu has timeScale 1, but still cannot simulate.
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            manager.StartRun();
            Assert.That(manager.Session.Snapshot.RunId, Is.Not.EqualTo(previousRun));
            Assert.That(manager.Session.Snapshot.ElapsedTime, Is.Zero);
        }

        [Test]
        public void ReentrantCommandsCannotInterruptACommittedTransition()
        {
            int events = 0;
            manager.Session.SnapshotChanged += _ => Assert.That(manager.Pause(), Is.False);
            manager.StateChanged += (_, __) =>
            {
                events++;
                Assert.That(manager.EndRun(GameOverReason.Lives), Is.False);
                Assert.That(loop.Step(1f), Is.False);
            };
            Assert.That(manager.StartRun(), Is.True);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(manager.Session.Snapshot.ElapsedTime, Is.Zero);
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void SnapshotIsAValueAndModelRejectsInvalidPartialMutations()
        {
            manager.StartRun();
            RunSnapshot previous = manager.Session.Snapshot;
            int publications = 0;
            manager.Session.SnapshotChanged += _ => publications++;
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Session.SetResources(2, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.Session.SetScore(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => loop.Step(float.PositiveInfinity));
            Assert.That(manager.Session.Snapshot, Is.EqualTo(previous));
            Assert.That(loop.TickIndex, Is.Zero);
            Assert.That(publications, Is.Zero);
            manager.Session.SetResources(1, 50f);
            manager.Session.PublishSnapshot();
            Assert.That(previous.Lives, Is.EqualTo(3));
            Assert.That(previous.Fuel, Is.EqualTo(100f));
            Assert.That(publications, Is.EqualTo(1));
        }

        [Test]
        public void ZeroResourcesResolveLivesAfterTheCommittedTick()
        {
            manager.StartRun();
            manager.Session.SetResources(0, 0f);
            loop.Step(0.25f);
            Assert.That(manager.State, Is.EqualTo(GameState.GameOver));
            Assert.That(manager.EndReason, Is.EqualTo(GameOverReason.Lives));
            Assert.That(manager.Session.Snapshot.Fuel, Is.Zero);
            Assert.That(manager.Session.Snapshot.Distance, Is.EqualTo(22d * 0.25f));
            Assert.That(manager.Session.Snapshot.Score, Is.EqualTo(5));
        }

        [Test]
        public void EditorQuitLogsOnceWithoutChangingTheSession()
        {
            RunSnapshot before = manager.Session.Snapshot;
            LogAssert.Expect(LogType.Log, "[GameManager] Quit requested (Editor intent only).");
            Assert.That(manager.Quit(), Is.True);
            Assert.That(manager.Quit(), Is.False);
            Assert.That(manager.State, Is.EqualTo(GameState.Menu));
            Assert.That(manager.Session.Snapshot, Is.EqualTo(before));
        }

        private void Reach(GameState state)
        {
            if (state == GameState.Menu) return;
            manager.StartRun();
            if (state == GameState.Paused) manager.Pause();
            else if (state == GameState.GameOver) manager.EndRun(GameOverReason.Lives);
        }

        private bool Command(string command)
        {
            switch (command)
            {
                case "Start": return manager.StartRun();
                case "Pause": return manager.Pause();
                case "Resume": return manager.Resume();
                case "Restart": return manager.Restart();
                case "Menu": return manager.ReturnToMenu();
                case "End": return manager.EndRun(GameOverReason.Lives);
                case "Quit": return manager.Quit();
                default: throw new ArgumentException(command);
            }
        }

        private static void Assign(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
