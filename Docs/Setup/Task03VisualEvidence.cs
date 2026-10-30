using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace NeonTunnelRunner
{
    // Editor-only recipe outside Assets. Injects real actions; never directly moves the player.
    public static class Task03VisualEvidence
    {
        public static async Task<object> CheckPausePresses()
        {
            var manager = GameManager.Instance;
            if (!manager || manager.State != GameState.Paused)
                throw new InvalidOperationException("Run MoveToLane first and leave the owner Paused.");
            var actions = manager.Loop.Input.Actions;
            var previousInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            var previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>("EvidencePauseKeyboard");
            actions.devices = new InputDevice[] { keyboard };
            int changes = 0;
            Action<GameState, GameState> onState = (_, __) => changes++;
            manager.StateChanged += onState;
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                await Task.Delay(250);
                if (manager.State != GameState.Playing || changes != 1)
                    throw new InvalidOperationException("One held Escape must resume exactly once.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                await Task.Delay(100);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                await Task.Delay(250);
                if (manager.State != GameState.Paused || changes != 2)
                    throw new InvalidOperationException("A second held Escape must pause exactly once.");
                return new { stateChanges = changes, state = manager.State.ToString(), timeScale = Time.timeScale };
            }
            finally
            {
                manager.StateChanged -= onState;
                InputSystem.RemoveDevice(keyboard);
                actions.devices = null;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousInputBehavior;
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            }
        }

        public static async Task<object> MoveToLane(int target)
        {
            var manager = GameManager.Instance;
            if (!Application.isPlaying || !manager || target < -1 || target > 1)
                throw new InvalidOperationException("Enter Game Play Mode and choose lane -1, 0 or 1.");
            var player = manager.Loop.Player;
            var actions = manager.Loop.Input.Actions;
            // CLI commands do not focus Game View. Restore both focus policies in finally.
            var previousInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            var previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            var keyboard = InputSystem.AddDevice<Keyboard>("EvidenceKeyboard");
            actions.devices = new InputDevice[] { keyboard };
            if (manager.State == GameState.Menu) manager.StartRun();
            else if (manager.State == GameState.Paused) manager.Resume();
            ulong firstTick = manager.Loop.TickIndex;
            int performed = 0, updates = 0;
            var watched = actions.FindAction(target < player.LaneIndex ? "Gameplay/Left" : "Gameplay/Right", true);
            Action<InputAction.CallbackContext> onPerformed = context => performed++;
            Action onUpdate = () => updates++;
            watched.performed += onPerformed;
            InputSystem.onAfterUpdate += onUpdate;
            try
            {
                while (player.LaneIndex != target)
                {
                    int direction = Math.Sign(target - player.LaneIndex);
                    int next = player.LaneIndex + direction;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(direction < 0 ? Key.A : Key.D));
                    int waited = 0;
                    while (player.LaneIndex != next && waited < 2000)
                    {
                        await Task.Delay(25); waited += 25;
                    }
                    if (player.LaneIndex != next) throw new InvalidOperationException(
                        $"Input diagnostic: performed={performed}, updates={updates}, controls={watched.controls.Count}, value={watched.ReadValue<float>()}, phase={watched.phase}, deviceEnabled={keyboard.enabled}, map={watched.actionMap.enabled}, loop={manager.Loop.IsRunning}, ticks={manager.Loop.TickIndex - firstTick}.");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                }
                int settling = 0;
                while (player.IsTransitioning && settling < 2000) { await Task.Delay(25); settling += 25; }
                await Task.Delay(300);
                if (player.IsTransitioning || manager.Loop.TickIndex <= firstTick)
                    throw new InvalidOperationException("The player loop did not advance; focus the Game view.");
                manager.Pause();
                var snapshot = manager.Session.Snapshot;
                var camera = Camera.main;
                return new
                {
                    lane = player.LaneIndex, position = player.CurrentPosition.ToString("F3"),
                    previous = player.PreviousPosition.ToString("F3"), snapshot.Distance, snapshot.ElapsedTime,
                    tick = manager.Loop.TickIndex, frame = Time.frameCount,
                    performedCallbacks = performed, inputUpdates = updates, runId = snapshot.RunId.ToString(),
                    cameraPosition = camera.transform.position.ToString("F3"), cameraParent = camera.transform.parent.name,
                    state = manager.State.ToString(), roadLength = 300
                };
            }
            finally
            {
                if (manager.State == GameState.Playing) manager.Pause();
                InputSystem.RemoveDevice(keyboard);
                actions.devices = null;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousInputBehavior;
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
                watched.performed -= onPerformed;
                InputSystem.onAfterUpdate -= onUpdate;
            }
        }
    }
}
