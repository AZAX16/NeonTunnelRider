using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class InputBindingTests : InputTestFixture
    {
        private GameObject root;
        private GameplayConfig config;
        private GameManager manager;
        private GameplayLoop loop;
        private PlayerController player;
        private InputRouter router;
        private Keyboard keyboard;
        private float originalTimeScale;

        public override void Setup()
        {
            base.Setup();
            originalTimeScale = Time.timeScale;
            config = ScriptableObject.CreateInstance<GameplayConfig>();
            root = PlayModeMovementRig.Create("InputFixture", config);
            manager = root.GetComponent<GameManager>();
            loop = manager.Loop;
            player = loop.Player;
            router = loop.Input;
            keyboard = InputSystem.AddDevice<Keyboard>();
            manager.StartRun();
        }

        public override void TearDown()
        {
            if (root) Object.DestroyImmediate(root);
            if (config) Object.DestroyImmediate(config);
            Time.timeScale = originalTimeScale;
            base.TearDown();
        }

        [TestCase(Key.A, -1)]
        [TestCase(Key.LeftArrow, -1)]
        [TestCase(Key.D, 1)]
        [TestCase(Key.RightArrow, 1)]
        public void EachRequiredBindingChangesOneLane(Key key, int direction)
        {
            Press(keyboard[key]);
            loop.Step(0.28f);
            Assert.That(player.LaneIndex, Is.EqualTo(direction));
            Assert.That(player.CurrentPosition.x, Is.EqualTo(direction * 3.2f));
        }

        [TestCase(Key.D)]
        [TestCase(Key.RightArrow)]
        public void HoldingAButtonDoesNotRepeatAcrossTheCenter(Key key)
        {
            player.RequestLane(-1);
            loop.Step(0.28f);
            Press(keyboard[key]);
            for (int i = 0; i < 100; i++) { InputSystem.Update(); loop.Step(0.02f); }
            Assert.That(player.LaneIndex, Is.Zero);
            Assert.That(player.CurrentPosition.x, Is.Zero);
        }

        [TestCase(Key.A, Key.D)]
        [TestCase(Key.LeftArrow, Key.RightArrow)]
        public void OppositePressesInOneInputUpdateCancel(Key left, Key right)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(left, right));
            InputSystem.Update();
            loop.Step(0.28f);
            Assert.That(player.LaneIndex, Is.Zero);
            Assert.That(player.CurrentPosition.x, Is.Zero);
        }

        [Test]
        public void PendingInputUsesOneLatestBatchAndIsConsumedOnce()
        {
            Press(keyboard.aKey);
            Release(keyboard.aKey);
            Press(keyboard.dKey);
            Assert.That(router.ConsumeLaneCommand(), Is.EqualTo(1));
            Assert.That(router.ConsumeLaneCommand(), Is.Zero);
        }

        [Test]
        public void EscapeTogglesOncePerPressAndOwnsNoUICancelBinding()
        {
            int events = 0;
            manager.StateChanged += (_, __) => events++;
            Press(keyboard.escapeKey);
            Assert.That(manager.State, Is.EqualTo(GameState.Paused));
            for (int i = 0; i < 5; i++) InputSystem.Update();
            Assert.That(events, Is.EqualTo(1));
            Release(keyboard.escapeKey);
            Press(keyboard.escapeKey);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(events, Is.EqualTo(2));
            var cancel = router.Actions.FindAction("UI/Cancel", true);
            Assert.That(cancel.bindings.Count, Is.EqualTo(1));
            Assert.That(cancel.bindings[0].path, Is.EqualTo("<Keyboard>/backspace"));
        }

        [TestCase(Key.D)]
        [TestCase(Key.RightArrow)]
        public void AKeyHeldDuringPauseDoesNotBecomeANewPressAfterResume(Key key)
        {
            Press(keyboard.escapeKey);
            Release(keyboard.escapeKey);
            Press(keyboard[key]);
            Press(keyboard.escapeKey);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            for (int i = 0; i < 3; i++) { InputSystem.Update(); loop.Step(0.28f); }
            Assert.That(player.LaneIndex, Is.Zero);
            Release(keyboard[key]);
            Press(keyboard[key]);
            loop.Step(0.28f);
            Assert.That(player.LaneIndex, Is.EqualTo(1));
        }

        [Test]
        public void PauseWinsOverLaneInputInTheSameUpdate()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Escape));
            InputSystem.Update();
            Assert.That(manager.State, Is.EqualTo(GameState.Paused));
            Assert.That(router.ConsumeLaneCommand(), Is.Zero);
            Assert.That(player.CurrentPosition, Is.EqualTo(config.StartingPosition));
        }

        [Test]
        public void MapsFollowEveryStateWithoutEnablingTheSourceAsset()
        {
            CheckMaps(true, true, false);
            manager.Pause(); CheckMaps(false, true, true);
            manager.Resume(); CheckMaps(true, true, false);
            manager.EndRun(GameOverReason.Fuel); CheckMaps(false, false, true);
            manager.Restart(); CheckMaps(true, true, false);
            manager.Pause(); manager.ReturnToMenu(); CheckMaps(false, false, true);
            Assert.That(router.SourceActions.enabled, Is.False);
            Assert.That(InputSystem.actions, Is.Null);
        }

        [Test]
        public void EnableDisableCyclesDoNotDuplicateOrRetainInputCallbacks()
        {
            for (int i = 0; i < 20; i++) { router.enabled = false; router.enabled = true; }
            int transitions = 0;
            manager.StateChanged += (_, __) => transitions++;
            Press(keyboard.escapeKey);
            Assert.That(manager.State, Is.EqualTo(GameState.Paused));
            Assert.That(transitions, Is.EqualTo(1));
            Release(keyboard.escapeKey);
            Press(keyboard.escapeKey);
            Assert.That(manager.State, Is.EqualTo(GameState.Playing));
            Assert.That(transitions, Is.EqualTo(2));
        }

        private void CheckMaps(bool gameplay, bool system, bool ui)
        {
            Assert.That(router.Actions.FindActionMap("Gameplay").enabled, Is.EqualTo(gameplay));
            Assert.That(router.Actions.FindActionMap("System").enabled, Is.EqualTo(system));
            Assert.That(router.Actions.FindActionMap("UI").enabled, Is.EqualTo(ui));
        }
    }
}
