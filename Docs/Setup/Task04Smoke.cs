using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NeonTunnelRunner
{
    // Ephemeral Editor recipe. Native Input System events and actual FixedUpdates, no direct movement.
    public static class Task04Smoke
    {
        public static async Task<object> Run()
        {
            var manager = GameManager.Instance;
            Require(Application.isPlaying && manager && manager.State == GameState.Menu, "Enter Game Play Mode in Menu.");
            Require(manager.Pools.IsReady && manager.Pools.Capacity == 0, "Expected the valid empty scene catalog.");
            var previousInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            var previousBackground = InputSystem.settings.backgroundBehavior;
            bool previousRunBackground = Application.runInBackground;
            var actions = manager.Loop.Input.Actions;
            var keyboard = InputSystem.AddDevice<Keyboard>("Task04SmokeKeyboard");
            var entries = new List<object>();
            int performed = 0;
            Action<InputAction.CallbackContext> onLane = _ => performed++;
            var left = actions.FindAction("Gameplay/Left", true);
            var right = actions.FindAction("Gameplay/Right", true);
            try
            {
                Application.runInBackground = true;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                actions.devices = new InputDevice[] { keyboard };
                left.performed += onLane; right.performed += onLane;
                Context(manager, "ContextStart");
                Require(manager.State == GameState.Playing, "Editor StartRun context command failed.");
                foreach (Key key in new[] { Key.A, Key.D, Key.LeftArrow, Key.RightArrow })
                {
                    int target = key == Key.A || key == Key.LeftArrow ? -1 : 1;
                    ulong initialTick = manager.Loop.TickIndex;
                    int initialCallbacks = performed;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                    await Until(() => manager.Loop.Player.LaneIndex == target && !manager.Loop.Player.IsTransitioning);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    await Task.Delay(80);
                    var snapshot = manager.Session.Snapshot;
                    Require(manager.Loop.TickIndex > initialTick && performed == initialCallbacks + 1, "Expected native ticks and one action per press.");
                    Require(snapshot.Distance < 250d, "Smoke must stay on the static 300m road.");
                    entries.Add(new { key = key.ToString(), lane = manager.Loop.Player.LaneIndex,
                        tick = manager.Loop.TickIndex, snapshot.Distance, callbacks = performed - initialCallbacks });
                    manager.Pause();
                    Context(manager, "ContextRestart");
                    Require(manager.State == GameState.Playing && manager.Loop.Player.LaneIndex == 0, "Restart failed to center the player.");
                }
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                await Until(() => manager.State == GameState.Paused);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                ulong pausedTick = manager.Loop.TickIndex;
                var paused = manager.Session.Snapshot;
                await Task.Delay(180);
                Require(manager.Loop.TickIndex == pausedTick && manager.Session.Snapshot.Distance == paused.Distance, "Pause advanced simulation.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                await Until(() => manager.State == GameState.Playing && manager.Loop.TickIndex > pausedTick);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                Require(manager.Session.Snapshot.RunId == paused.RunId, "Resume reset the run.");
                manager.EndRun(GameOverReason.Fuel);
                Context(manager, "ContextRestart");
                Require(manager.State == GameState.Playing && manager.Loop.TickIndex == 0, "GameOver restart failed.");
                manager.Pause();
                Context(manager, "ContextReturnToMenu");
                Require(manager.State == GameState.Menu && !manager.Loop.IsRunning &&
                    !actions.FindActionMap("Gameplay").enabled && manager.Pools.IsReady, "Return to Menu failed.");
                return new { passed = true, bindings = entries.ToArray(), editorContextStart = true,
                    nativeEscapePauseResume = true, pausedRestart = true, gameOverRestart = true,
                    returnToMenu = true, finalState = manager.State.ToString(), frame = Time.frameCount,
                    ready = manager.Pools.IsReady, capacity = manager.Pools.Capacity,
                    created = manager.Pools.TotalCreated, destroyed = manager.Pools.TotalDestroyed };
            }
            finally
            {
                left.performed -= onLane; right.performed -= onLane;
                actions.devices = null;
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode = previousInput;
                InputSystem.settings.backgroundBehavior = previousBackground;
                Application.runInBackground = previousRunBackground;
                if (manager.State == GameState.Playing) manager.Pause();
            }
        }

        private static void Context(GameManager manager, string method) =>
            typeof(GameManager).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private static async Task Until(Func<bool> predicate)
        {
            for (int waited = 0; waited < 4000; waited += 20)
            {
                if (predicate()) return;
                await Task.Delay(20);
            }
            throw new TimeoutException("Native input/FixedUpdate smoke condition did not complete.");
        }
    }
}
