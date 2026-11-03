using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public sealed class InputRouter : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputActionAsset actionsAsset;
        [SerializeField] private InputSystemUIInputModule uiModule;
        private readonly System.Collections.Generic.List<InputActionReference> uiReferences = new();
        private InputActionAsset runtimeActions;
        private InputActionMap gameplay, system, ui;
        private InputAction left, right, pause, cancel, submit, click;
        private bool leftPressed, rightPressed, pausePressed, cancelPressed;
        private int pendingLaneCommand;

        public GameManager Owner => gameManager;
        public InputActionAsset SourceActions => actionsAsset;
        public InputActionAsset Actions => runtimeActions;
        public InputSystemUIInputModule UiModule => uiModule;
        public ulong UiActivationVersion { get; private set; }

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
            cancel = ui.FindAction("Cancel", true);
            submit = ui.FindAction("Submit", true);
            click = ui.FindAction("Click", true);
            // Buttons wait for a new press. Re-enabling Gameplay cannot synthesize a held-key press.
            left.wantsInitialStateCheck = right.wantsInitialStateCheck = pause.wantsInitialStateCheck = false;
            foreach (var action in ui.actions) action.wantsInitialStateCheck = false;
            // Reopened panels must know a stationary pointer's location. Only Point
            // refreshes held state; Navigate/Submit/Click still require fresh input.
            ui.FindAction("Point", true).wantsInitialStateCheck = true;
            left.performed += OnLeft;
            right.performed += OnRight;
            pause.performed += OnPause;
            cancel.performed += OnCancel;
            submit.performed += OnSubmit;
            click.performed += OnClick;
            if (uiModule)
            {
                uiModule.enabled = false;
                uiModule.actionsAsset = runtimeActions;
                uiModule.point = UiReference("Point");
                uiModule.leftClick = UiReference("Click");
                uiModule.scrollWheel = UiReference("ScrollWheel");
                uiModule.move = UiReference("Navigate");
                uiModule.submit = UiReference("Submit");
                uiModule.cancel = UiReference("Cancel");
            }
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
        private void OnCancel(InputAction.CallbackContext context) => cancelPressed = true;
        private void OnSubmit(InputAction.CallbackContext context) => UiActivationVersion++;
        private void OnClick(InputAction.CallbackContext context)
        {
            // uGUI clicks activate on release; Submit activates on press.
            if (!context.ReadValueAsButton()) UiActivationVersion++;
        }

        private InputActionReference UiReference(string name)
        {
            var reference = InputActionReference.Create(ui.FindAction(name, true));
            reference.hideFlags = HideFlags.DontSave;
            uiReferences.Add(reference);
            return reference;
        }

        private bool CanReadGameplay => gameManager && gameManager.IsOwner &&
            gameManager.isActiveAndEnabled && gameManager.State == GameState.Playing;

        private void FinishInputUpdate()
        {
            bool togglePause = pausePressed;
            bool goBack = cancelPressed;
            bool hadLanePress = leftPressed || rightPressed;
            int lane = leftPressed == rightPressed ? 0 : leftPressed ? -1 : 1;
            leftPressed = rightPressed = pausePressed = cancelPressed = false;
            if (!gameManager || !gameManager.IsOwner || !gameManager.isActiveAndEnabled) { Clear(); return; }
            if (togglePause)
            {
                Clear(); // A pause command wins over lane presses in the same update.
                if (gameManager.State == GameState.Playing) gameManager.Pause();
                else if (gameManager.State == GameState.Paused) gameManager.Resume();
                return;
            }
            // Backspace has exactly one valid back command. Escape remains System's sole owner.
            if (goBack && gameManager.State == GameState.Paused) { Clear(); gameManager.Resume(); return; }
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
            leftPressed = rightPressed = pausePressed = cancelPressed = false;
        }

        internal void ApplyState(GameState state)
        {
            Clear();
            if (!runtimeActions || !isActiveAndEnabled) return;
            // Clear navigation/repeat and pointer state even between two UI states.
            // Cancel while callbacks are still hooked, then re-enable without initial checks.
            if (uiModule)
            {
                ui.Disable();
                uiModule.enabled = false;
            }
            SetEnabled(gameplay, state == GameState.Playing);
            // Leave System enabled across Playing/Paused, including an Escape held through the toggle.
            SetEnabled(system, state == GameState.Playing || state == GameState.Paused);
            SetEnabled(ui, state != GameState.Playing);
            if (uiModule && state != GameState.Playing) uiModule.enabled = true;
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
            if (uiModule) uiModule.enabled = false;
            if (!runtimeActions) return;
            left.performed -= OnLeft;
            right.performed -= OnRight;
            pause.performed -= OnPause;
            cancel.performed -= OnCancel;
            submit.performed -= OnSubmit;
            click.performed -= OnClick;
            runtimeActions.Disable();
            foreach (var reference in uiReferences)
                if (Application.isPlaying) Destroy(reference); else DestroyImmediate(reference);
            uiReferences.Clear();
            if (Application.isPlaying) Destroy(runtimeActions); else DestroyImmediate(runtimeActions);
            runtimeActions = null;
        }
    }
}
