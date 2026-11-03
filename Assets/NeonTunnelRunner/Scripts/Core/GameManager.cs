using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameplayConfig gameplayConfig;
        [SerializeField] private GameplayLoop gameplayLoop;
        [SerializeField] private PoolService poolService;
        [SerializeField] private HudPresenter hud;
        [SerializeField] private MenuPresenter menus;
        [SerializeField] private int runSeed = 12345;
        [SerializeField, Min(0)] private int initialBestScore;
        [SerializeField] private GameState debugState;
        [SerializeField] private GameOverReason debugEndReason;
        private bool initialized;
        private bool ownsInstance;
        private bool transitioning;
        private bool quitRequested;
        private HighScoreStore highScores;
#if UNITY_EDITOR
        // Scoped by the test suites/smoke harness; never present in a Player.
        internal static Func<HighScoreStore> HighScoreTestFactory;
#endif
        internal void ConfigureHighScores(HighScoreStore store)
        {
            if (initialized) throw new InvalidOperationException("Configure storage before bootstrap.");
            highScores = store ?? throw new ArgumentNullException(nameof(store));
        }
        public HighScoreStore HighScores => highScores;
        internal void CaptureBest()
        {
            if (Session == null || highScores == null) return;
            var s = Session.Snapshot;
            highScores.SetIfHigher(Math.Max(s.BestScore, s.Score));
            Session.RetainBestScore(highScores.Best);
        }
        private void FlushBest() { CaptureBest(); highScores?.Flush(); }

        public static GameManager Instance { get; private set; }
        public GameState State { get; private set; } = GameState.Menu;
        public GameOverReason EndReason { get; private set; }
        public RunSession Session { get; private set; }
        public RunSnapshot? FinalRunSnapshot { get; private set; }
        public GameplayConfig Configuration => gameplayConfig;
        public GameplayLoop Loop => gameplayLoop;
        public PoolService Pools => poolService;
        public bool IsOwner => initialized && ownsInstance && Instance == this;
        internal bool IsTransitioning => transitioning;

        public event Action<GameState, GameState> StateChanged;
        public event Action<RunSnapshot, GameOverReason> RunEnded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticOwnership() => Instance = null;

        // With both domain and scene reload disabled, existing objects need a fresh play-session bootstrap.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RestoreSceneOwner()
        {
            if (Instance) return;
            foreach (var manager in FindObjectsByType<GameManager>(FindObjectsInactive.Exclude))
                if (manager.isActiveAndEnabled) manager.Initialize();
        }

        private void Awake() => Initialize();

        private void OnEnable()
        {
            if (Application.isPlaying) Initialize();
        }

        /// <summary>Idempotent scene bootstrap; also permits testing the real lifecycle in EditMode.</summary>
        public bool Initialize()
        {
            if (IsOwner) return true;
            if (!isActiveAndEnabled) return false;
            if (Instance && Instance != this)
            {
                Debug.LogWarning("[GameManager] Duplicate rejected; the existing owner is unchanged.", this);
                enabled = false;
                if (Application.isPlaying) Destroy(this);
                return false;
            }
            if (!gameplayConfig || !gameplayLoop || gameplayLoop.Owner != this || !poolService || poolService.Owner != this)
            {
                Debug.LogError("[GameManager] Assign GameplayConfig, GameplayLoop, PoolService and their owner before initialization.", this);
                return false;
            }

            FlushBest();
            StateChanged = null;
            RunEnded = null;
            Session?.DetachObservers();
            State = GameState.Menu;
            EndReason = GameOverReason.None;
            FinalRunSnapshot = null;
            debugEndReason = EndReason;
            debugState = State;
#if UNITY_EDITOR
            highScores ??= HighScoreTestFactory?.Invoke();
#endif
            highScores ??= new HighScoreStore();
            Session = new RunSession(gameplayConfig, Math.Max(highScores.Load(), initialBestScore));
            poolService.Initialize(); // Failure leaves a usable Menu; StartRun reports the actionable error.
            gameplayLoop.Initialize(this, Session);
            Instance = this;
            ownsInstance = initialized = true;
            transitioning = quitRequested = false;
            Time.timeScale = 1f;
            // Explicit composition after readiness; presenters never depend on Awake order.
            if (hud) hud.Initialize(this, Session);
            if (menus) menus.Initialize(this, Session);
            return true;
        }

        public bool StartRun() => Accepts(GameState.Menu) ? BeginRun() : Reject(nameof(StartRun));

        public bool Pause() => Accepts(GameState.Playing) ? ChangeState(GameState.Paused) : Reject(nameof(Pause));

        public bool Resume() => Accepts(GameState.Paused) && gameplayLoop.WorldFailure == null ? ChangeState(GameState.Playing) : Reject(nameof(Resume));

        public bool Restart() => Accepts(GameState.Paused) || Accepts(GameState.GameOver)
            ? BeginRun() : Reject(nameof(Restart));

        public bool ReturnToMenu()
        {
            if (!Accepts(GameState.Paused)) return Reject(nameof(ReturnToMenu));
            if (poolService.IsBusy) return Reject(nameof(ReturnToMenu));
            transitioning = true;
            try
            {
                FlushBest();
                gameplayLoop.CleanupRun();
                poolService.ReturnAll();
                CommitState(GameState.Menu);
                return true;
            }
            finally { transitioning = false; }
        }

        public bool EndRun(GameOverReason reason)
        {
            if (!Accepts(GameState.Playing) || (reason != GameOverReason.Lives && reason != GameOverReason.Fuel))
                return Reject(nameof(EndRun));
            transitioning = true;
            try
            {
                EndReason = reason;
                debugEndReason = reason;
                gameplayLoop.StopRun();
                RunSnapshot result = CommitState(GameState.GameOver);
                FinalRunSnapshot = result;
                FlushBest();
                RunEnded?.Invoke(result, reason);
                return true;
            }
            finally { transitioning = false; }
        }

        public bool Quit()
        {
            if (!Accepts(GameState.Menu) || quitRequested) return Reject(nameof(Quit));
            FlushBest();
            quitRequested = true;
#if UNITY_EDITOR
            Debug.Log("[GameManager] Quit requested (Editor intent only).", this);
#else
            Application.Quit();
#endif
            return true;
        }

        private bool BeginRun()
        {
            if (poolService.IsBusy) return Reject(nameof(BeginRun));
            if (!poolService.isActiveAndEnabled || (!poolService.IsReady &&
                (State != GameState.Menu || !poolService.Initialize())))
            {
                Debug.LogError("[GameManager] Cannot start: " + (poolService.LastError ?? "PoolService is not ready/enabled; prewarm in Menu."), this);
                return false;
            }
            if (!gameplayLoop.ValidateWorld(out string worldError))
            {
                Debug.LogError("[GameManager] Cannot start: " + worldError, this);
                return false;
            }
            transitioning = true;
            try
            {
                FlushBest();
                gameplayLoop.CleanupRun();
                poolService.ReturnAll();
                if (!poolService.IsReady)
                {
                    gameplayLoop.ApplyState(State);
                    Debug.LogError("[GameManager] Cannot start: PoolService shut down during reset cleanup.", this);
                    return false;
                }
                if (!gameplayLoop.ResetRun(runSeed))
                {
                    gameplayLoop.CleanupRun();
                    gameplayLoop.ApplyState(State);
                    Debug.LogError("[GameManager] Cannot start: " + gameplayLoop.WorldFailure, this);
                    return false;
                }
                gameplayLoop.Survival.RefreshDebug();
                gameplayLoop.Progression.RefreshDebug();
                EndReason = GameOverReason.None;
                FinalRunSnapshot = null;
                debugEndReason = EndReason;
                quitRequested = false;
                CommitState(GameState.Playing);
                return true;
            }
            finally { transitioning = false; }
        }

        private bool ChangeState(GameState next)
        {
            transitioning = true;
            try
            {
                CommitState(next);
                return true;
            }
            finally { transitioning = false; }
        }

        private RunSnapshot CommitState(GameState next)
        {
            GameState previous = State;
            State = next;
            debugState = next;
            CaptureBest();
            Session.SetSimulationActive(next == GameState.Playing);
            Time.timeScale = next == GameState.Paused || next == GameState.GameOver ? 0f : 1f;
            gameplayLoop.ApplyState(next);
            gameplayLoop.Progression.RefreshDebug();
            RunSnapshot snapshot = Session.PublishSnapshot();
            StateChanged?.Invoke(previous, next);
            return snapshot;
        }

        private bool Accepts(GameState required) => IsOwner && isActiveAndEnabled && !transitioning && !gameplayLoop.IsStepping && State == required;

        private bool Reject(string command)
        {
            LogRejected(command);
            return false;
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        private void LogRejected(string command) => Debug.Log($"[GameManager] Ignored {command} in {State}.", this);

        private void OnDestroy() => Shutdown();

        private void OnApplicationQuit() => Shutdown();

        private void OnApplicationPause(bool paused)
        {
            if (paused && IsOwner) FlushBest();
        }

        internal void Shutdown()
        {
            if (!ownsInstance || Instance != this) return;
            FlushBest();
            if (hud) hud.Shutdown();
            if (menus) menus.Shutdown();
            gameplayLoop?.Shutdown();
            if (poolService) poolService.Shutdown();
            Session?.DetachObservers();
            StateChanged = null;
            RunEnded = null;
            highScores = null;
            initialized = ownsInstance = false;
            Instance = null;
            Time.timeScale = 1f;
        }

    }
}
