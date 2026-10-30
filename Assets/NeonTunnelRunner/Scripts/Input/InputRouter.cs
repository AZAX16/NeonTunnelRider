using UnityEngine;
using UnityEngine.InputSystem;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public sealed class InputRouter : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputActionAsset actionsAsset;
        private InputActionAsset runtimeActions;
        private InputActionMap gameplay, system, ui;
        private InputAction left, right, pause;
        private bool leftPressed, rightPressed, pausePressed;
        private int pendingLaneCommand;

        public GameManager Owner => gameManager;
        public InputActionAsset SourceActions => actionsAsset;
        public InputActionAsset Actions => runtimeActions;

        internal void Initialize(GameManager owner)
        {
            if (owner != gameManager || !actionsAsset)
                throw new System.InvalidOperationException("Assign InputRouter's owner and Runner actions.");
            Shutdown();
            runtimeActions = Instantiate(actionsAsset);
            runtimeActions.name = "Runner (runtime)";
            runtimeActions.hideFlags = HideFlags.DontSave;
            gameplay = runtimeActions.FindActionMap("Gameplay", true);
            system = runtimeActions.FindActionMap("System", true);
            ui = runtimeActions.FindActionMap("UI", true);
            left = gameplay.FindAction("Left", true);
            right = gameplay.FindAction("Right", true);
            pause = system.FindAction("Pause", true);
            // Buttons wait for a new press. Re-enabling Gameplay cannot synthesize a held-key press.
            left.wantsInitialStateCheck = right.wantsInitialStateCheck = pause.wantsInitialStateCheck = false;
            left.performed += OnLeft;
            right.performed += OnRight;
            pause.performed += OnPause;
            InputSystem.onAfterUpdate += FinishInputUpdate;
            ApplyState(owner.State);
        }

        private void OnEnable()
        {
            if (Application.isPlaying && gameManager && gameManager.IsOwner)
            {
                if (!runtimeActions) Initialize(gameManager);
                else ApplyState(gameManager.State);
            }
        }

        private void OnDisable() => Shutdown();
        private void OnDestroy() => Shutdown();

        private void OnLeft(InputAction.CallbackContext context)
        {
            if (CanReadGameplay) leftPressed = true;
        }

        private void OnRight(InputAction.CallbackContext context)
        {
            if (CanReadGameplay) rightPressed = true;
        }

        private void OnPause(InputAction.CallbackContext context) => pausePressed = true;

        private bool CanReadGameplay => gameManager && gameManager.IsOwner &&
            gameManager.isActiveAndEnabled && gameManager.State == GameState.Playing;

        private void FinishInputUpdate()
        {
            bool togglePause = pausePressed;
            bool hadLanePress = leftPressed || rightPressed;
            int lane = leftPressed == rightPressed ? 0 : leftPressed ? -1 : 1;
            leftPressed = rightPressed = pausePressed = false;
            if (!gameManager || !gameManager.IsOwner || !gameManager.isActiveAndEnabled) { Clear(); return; }
            if (togglePause)
            {
                Clear(); // A pause command wins over lane presses in the same update.
                if (gameManager.State == GameState.Playing) gameManager.Pause();
                else if (gameManager.State == GameState.Paused) gameManager.Resume();
                return;
            }
            if (CanReadGameplay && hadLanePress) pendingLaneCommand = lane;
        }

        public int ConsumeLaneCommand()
        {
            int command = CanReadGameplay ? pendingLaneCommand : 0;
            pendingLaneCommand = 0;
            return command;
        }

        public void Clear()
        {
            pendingLaneCommand = 0;
            leftPressed = rightPressed = pausePressed = false;
        }

        internal void ApplyState(GameState state)
        {
            Clear();
            if (!runtimeActions || !isActiveAndEnabled) return;
            SetEnabled(gameplay, state == GameState.Playing);
            // Leave System enabled across Playing/Paused, including an Escape held through the toggle.
            SetEnabled(system, state == GameState.Playing || state == GameState.Paused);
            SetEnabled(ui, state != GameState.Playing);
        }

        private static void SetEnabled(InputActionMap map, bool enabled)
        {
            if (map.enabled == enabled) return;
            if (enabled) map.Enable(); else map.Disable();
        }

        internal void Shutdown()
        {
            InputSystem.onAfterUpdate -= FinishInputUpdate;
            Clear();
            if (!runtimeActions) return;
            left.performed -= OnLeft;
            right.performed -= OnRight;
            pause.performed -= OnPause;
            runtimeActions.Disable();
            if (Application.isPlaying) Destroy(runtimeActions); else DestroyImmediate(runtimeActions);
            runtimeActions = null;
        }
    }
}
