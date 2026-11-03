using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public sealed class MenuPresenter : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private EventSystem eventSystem;
        [SerializeField] private CanvasGroup mainPanel, pausePanel, overPanel;
        [SerializeField] private UnityEngine.UI.Button startButton, quitButton, resumeButton, pauseRestartButton, mainMenuButton, overRestartButton;
        [SerializeField] private TMP_Text finalScoreText, finalBestText, reasonText, statusText;
        private RunSession session;
        private CanvasGroup visiblePanel;
        private UnityEngine.UI.Button initialSelection;
        private bool buttonsBound;
        private ulong consumedActivation;
        public GameManager Owner => gameManager;

        public bool ValidateRequired(out string error)
        {
            bool valid = gameManager && eventSystem && mainPanel && pausePanel && overPanel && startButton && quitButton &&
                resumeButton && pauseRestartButton && mainMenuButton && overRestartButton && finalScoreText && finalBestText && reasonText && statusText;
            error = valid ? null : "Assign menu owner, EventSystem, three panels, six Buttons, final score/best/reason and status TMP text.";
            return valid;
        }

        private void OnEnable()
        {
            if (!ValidateRequired(out string error)) { Debug.LogError("[MenuPresenter] " + error, this); return; }
            BindButtons();
            if (gameManager.IsOwner) Initialize(gameManager, gameManager.Session);
            else { Show(GameState.Menu); statusText.text = "STARTUP NOT READY - run Validate Project Setup."; }
        }
        internal void Initialize(GameManager owner, RunSession source)
        {
            Shutdown();
            if (!isActiveAndEnabled) return;
            if (owner != gameManager || source == null || !ValidateRequired(out _))
            { Debug.LogError("[MenuPresenter] Invalid menu composition; run Validate Project Setup.", this); return; }
            session = source;
            consumedActivation = owner.Loop.Input.UiActivationVersion;
            BindButtons();
            owner.StateChanged += OnStateChanged;
            owner.RunEnded += OnRunEnded;
            statusText.text = string.Empty;
            Show(owner.State);
            if (owner.State == GameState.GameOver && owner.FinalRunSnapshot.HasValue)
                OnRunEnded(owner.FinalRunSnapshot.Value, owner.EndReason);
        }
        private void BindButtons()
        {
            if (buttonsBound) return;
            startButton.onClick.AddListener(StartRun);
            quitButton.onClick.AddListener(Quit);
            resumeButton.onClick.AddListener(Resume);
            pauseRestartButton.onClick.AddListener(Restart);
            mainMenuButton.onClick.AddListener(MainMenu);
            overRestartButton.onClick.AddListener(Restart);
            buttonsBound = true;
        }
        internal void Shutdown()
        {
            if (gameManager) { gameManager.StateChanged -= OnStateChanged; gameManager.RunEnded -= OnRunEnded; }
            session = null;
            if (!buttonsBound) return;
            startButton.onClick.RemoveListener(StartRun);
            quitButton.onClick.RemoveListener(Quit);
            resumeButton.onClick.RemoveListener(Resume);
            pauseRestartButton.onClick.RemoveListener(Restart);
            mainMenuButton.onClick.RemoveListener(MainMenu);
            overRestartButton.onClick.RemoveListener(Restart);
            buttonsBound = false;
        }
        private void OnDisable()
        {
            Shutdown();
            if (eventSystem && eventSystem.currentSelectedGameObject && eventSystem.currentSelectedGameObject.transform.IsChildOf(transform))
                eventSystem.SetSelectedGameObject(null);
        }
        private void StartRun()
        {
            if (!ConsumeActivation()) return;
            if ((!gameManager.IsOwner && !gameManager.Initialize()) || !gameManager.StartRun()) ShowFailure();
        }
        private bool ConsumeActivation()
        {
            var input = gameManager.Loop.Input;
            if (input.UiActivationVersion == consumedActivation) return false;
            consumedActivation = input.UiActivationVersion;
            return true;
        }
        private void Quit() { if (ConsumeActivation() && !gameManager.Quit()) ShowFailure(); }
        private void Resume() { if (ConsumeActivation() && !gameManager.Resume()) ShowFailure(); }
        private void Restart() { if (ConsumeActivation() && !gameManager.Restart()) ShowFailure(); }
        private void MainMenu() { if (ConsumeActivation() && !gameManager.ReturnToMenu()) ShowFailure(); }
        private void ShowFailure() => statusText.text = "UNABLE TO CONTINUE: " +
            (gameManager.Pools ? gameManager.Pools.LastError ?? gameManager.Loop.WorldFailure ?? "run Validate Project Setup and inspect Console." : "assign PoolService.");
        private void OnStateChanged(GameState previous, GameState next) { statusText.text = string.Empty; Show(next); }
        private void OnRunEnded(RunSnapshot result, GameOverReason reason)
        {
            finalScoreText.text = result.Score.ToString(System.Globalization.CultureInfo.InvariantCulture);
            finalBestText.text = result.BestScore.ToString(System.Globalization.CultureInfo.InvariantCulture);
            reasonText.text = reason == GameOverReason.Lives ? "NO LIVES REMAINING" : "FUEL EMPTY";
        }
        private void Show(GameState state)
        {
            eventSystem.SetSelectedGameObject(null);
            SetVisible(mainPanel, state == GameState.Menu);
            SetVisible(pausePanel, state == GameState.Paused);
            SetVisible(overPanel, state == GameState.GameOver);
            visiblePanel = state == GameState.Menu ? mainPanel : state == GameState.Paused ? pausePanel : state == GameState.GameOver ? overPanel : null;
            initialSelection = state == GameState.Menu ? startButton : state == GameState.Paused ? resumeButton : state == GameState.GameOver ? overRestartButton : null;
            RestoreSelection();
        }
        private static void SetVisible(CanvasGroup panel, bool visible)
        {
            panel.interactable = panel.blocksRaycasts = visible;
            panel.alpha = visible ? 1f : 0f;
            panel.gameObject.SetActive(visible);
        }
        // Only repair absent/invalid focus. Pointer-selected valid buttons keep their selection.
        private void LateUpdate()
        {
            if (!buttonsBound || !visiblePanel) return;
            var selected = eventSystem.currentSelectedGameObject;
            if (!selected || !selected.activeInHierarchy || !selected.transform.IsChildOf(visiblePanel.transform)) RestoreSelection();
        }
        private void RestoreSelection()
        {
            if (initialSelection && initialSelection.isActiveAndEnabled && initialSelection.IsInteractable())
                eventSystem.SetSelectedGameObject(initialSelection.gameObject);
        }
    }
}
