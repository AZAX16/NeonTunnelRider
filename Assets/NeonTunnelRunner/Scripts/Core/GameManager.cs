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
        public NearMissSequence NearMiss { get; } = new NearMissSequence();
        private TrafficLease nearMissTraffic;
        private float nearMissSimulationScale=1f;
        private int nearMissApproachLane;
        private float nearMissApproachX;
        public bool NearMissPassCompleted { get; private set; }
        public TrafficLease NearMissTraffic => nearMissTraffic;
        private double presentationRealtime;
        internal void ResetPresentationClock() => presentationRealtime=Time.realtimeSinceStartupAsDouble;
        private float baselineFixedDelta;
        private bool clockCaptured;
        internal void ComposeClock()
        {
            float scale = State == GameState.Paused || State == GameState.GameOver ||
                (NearMiss.Active && gameplayLoop && gameplayLoop.Audio && gameplayLoop.Audio.ApplicationSuspended) ? 0f :
                State == GameState.Playing ? Mathf.Lerp(1f,nearMissSimulationScale,NearMiss.Weight) : 1f;
            Time.timeScale = scale;
            if (clockCaptured)
            {
                if (scale==0f || scale==1f) RestoreFixedStep();
                else
                {
                    float step=baselineFixedDelta*scale;
                    if (Time.fixedDeltaTime!=step) Time.fixedDeltaTime=step;
                }
            }
        }
        private void RestoreFixedStep()
        {
            if (Time.fixedDeltaTime==baselineFixedDelta) return;
            Time.fixedDeltaTime=baselineFixedDelta;
            // Unity 6.6 stores a rational tick count. A float round-trip can truncate
            // one rational tick; the next representable input restores the saved getter
            // exactly, without feeding a previously scaled/quantized value back into it.
            if (Time.fixedDeltaTime<baselineFixedDelta)
                Time.fixedDeltaTime=BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(baselineFixedDelta)+1);
        }
        private void Update()
        {
            double now=Time.realtimeSinceStartupAsDouble;
            float elapsed=(float)System.Math.Max(0d,now-presentationRealtime);
            presentationRealtime=now;
            AdvancePresentation(elapsed);
        }
        internal void AdvancePresentation(float realPlayingSeconds)
        {
            if (!float.IsFinite(realPlayingSeconds) || realPlayingSeconds<=0f) return;
            if (!IsOwner || State != GameState.Playing || transitioning) return;
            if (NearMiss.Active && gameplayLoop.Audio && gameplayLoop.Audio.ApplicationSuspended)
            { ComposeClock(); return; }
            NearMiss.Advance(realPlayingSeconds,gameplayConfig);
            UpdateNearMissMotion();
            ComposeClock();
            if (gameplayLoop.Audio) gameplayLoop.Audio.RenderNearMiss(NearMiss);
        }
        internal float NearMissPassEntryGap(Interactable car)
        {
            float relative=Mathf.Max(0f,Session.Snapshot.CruiseSpeed-car.Speed);
            // Leave room to react and perform one continuous adjacent move at the
            // start of the one-second pass window. No collision immunity is granted.
            return gameplayConfig.PlayerHalfExtents.z+car.HalfExtents.z+
                relative*(gameplayConfig.LaneTransitionDuration+gameplayConfig.NearMissDodgeReactionTime)+.5f;
        }
        private bool HoldingBehind(Interactable car,int lane,float x)
        {
            var player=gameplayLoop.Player;
            if (player.IsTransitioning || player.QueuedCommand!=0 || player.LaneIndex!=lane || car.Lane!=lane ||
                Mathf.Abs(player.CurrentPosition.x-x)>.2f || Mathf.Abs(player.CurrentPosition.x-car.CurrentPosition.x)>.2f ||
                car is MovingCar changing && changing.ChangingLane) return false;
            for(int i=0;i<player.MotionLegCount;i++)
            {
                var leg=player.GetMotionLeg(i);
                if(Mathf.Abs(leg.Start.x-x)>.2f || Mathf.Abs(leg.End.x-x)>.2f) return false;
            }
            return car.AbsoluteZ-Session.Snapshot.Distance>gameplayConfig.PlayerHalfExtents.z+car.HalfExtents.z+.01d;
        }
        internal bool CanStartNearMissBehind(TrafficLease traffic)
        {
            if(!traffic.IsCurrent || traffic.Item.Encountered || traffic.Item.Category!=InteractionCategory.Obstacle) return false;
            var car=traffic.Item;var player=gameplayLoop.Player;
            float relative=Session.Snapshot.CruiseSpeed-car.Speed;
            if(relative<=0f || !HoldingBehind(car,player.LaneIndex,player.CurrentPosition.x)) return false;
            float approach=gameplayConfig.NearMissDuration*gameplayConfig.NearMissApproachRatio;
            return car.AbsoluteZ-Session.Snapshot.Distance>=NearMissPassEntryGap(car)+relative*.04f*approach;
        }
        internal bool ValidateNearMissEncounter()
        {
            if(!NearMiss.Active || !nearMissTraffic.Item) return NearMiss.Active;
            if(!nearMissTraffic.IsCurrent)
            {
                if(NearMissPassCompleted) return true; // Genuine passed traffic may recycle during aftermath.
                CancelNearMiss(); return false;
            }
            var car=nearMissTraffic.Item;var player=gameplayLoop.Player;
            if(car.Encountered) { CancelNearMiss();return false; }
            if(NearMiss.Age<NearMiss.ApproachEnd)
            {
                if(!HoldingBehind(car,nearMissApproachLane,nearMissApproachX)) { CancelNearMiss();return false; }
                return true;
            }
            float halfZ=gameplayConfig.PlayerHalfExtents.z+car.HalfExtents.z;
            double gap=car.AbsoluteZ-Session.Snapshot.Distance;
            float clearance=Mathf.Abs(player.CurrentPosition.x-car.CurrentPosition.x)-gameplayConfig.PlayerHalfExtents.x-car.HalfExtents.x;
            if(!NearMissPassCompleted)
            {
                int destination=Mathf.Clamp(player.LaneIndex+player.QueuedCommand,-1,1);
                if(Mathf.Abs(destination-nearMissApproachLane)>1 ||
                    Mathf.Abs(player.CurrentPosition.x-car.CurrentPosition.x)>gameplayConfig.LaneWidth+.2f)
                { CancelNearMiss();return false; }
                if(gap < -halfZ && clearance>0f && clearance<=gameplayConfig.NearMissClearance)
                    NearMissPassCompleted=true;
                if(NearMiss.Age>=NearMiss.PassEnd && !NearMissPassCompleted)
                { CancelNearMiss();return false; }
            }
            return true;
        }
        private void UpdateNearMissMotion()
        {
            nearMissSimulationScale=gameplayConfig.NearMissTimeScale;
            if(!NearMiss.Active) { nearMissTraffic=default;return; }
            if(!ValidateNearMissEncounter() || !nearMissTraffic.IsCurrent) return;
            if(NearMiss.Age>=NearMiss.PassEnd) return;
            var car=nearMissTraffic.Item;
            float relative=Session.Snapshot.CruiseSpeed-car.Speed;
            if(relative<=0f) { CancelNearMiss();return; }
            double gap=car.AbsoluteZ-Session.Snapshot.Distance;
            if(NearMiss.Age<NearMiss.ApproachEnd)
            {
                float left=NearMiss.ApproachEnd-NearMiss.Age;
                float rampLeft=Mathf.Max(0f,gameplayConfig.NearMissEntry-NearMiss.Age);
                float rampArea=rampLeft*(1f-NearMiss.Weight)*.5f;
                nearMissSimulationScale=Mathf.Clamp(((float)(gap-NearMissPassEntryGap(car))/relative-rampArea)/Mathf.Max(.001f,left-rampArea),.04f,1f);
            }
            else
            {
                float halfZ=gameplayConfig.PlayerHalfExtents.z+car.HalfExtents.z;
                float remaining=Mathf.Max(.001f,NearMiss.PassEnd-NearMiss.Age);
                nearMissSimulationScale=Mathf.Clamp(((float)gap+halfZ+.15f)/(relative*remaining),.04f,1f);
            }
        }
        internal void ContactNearMiss(TrafficLease contact)
        {
            if (NearMiss.Active && contact.Item==nearMissTraffic.Item && contact.ActivationId==nearMissTraffic.ActivationId)
                CancelNearMiss();
        }
        internal bool AcceptNearMiss(TrafficLease traffic=default)
        {
            if (!IsOwner || State != GameState.Playing || transitioning || Session.Snapshot.Lives <= 0 || Session.Snapshot.Fuel <= 0) return false;
            float duration = gameplayConfig.NearMissDuration;
            var clip = gameplayLoop.Audio ? gameplayLoop.Audio.Configuration.NearMiss : null;
            if (clip) duration = Mathf.Min(duration,clip.length);
            if(traffic.Item && !CanStartNearMissBehind(traffic)) return false;
            if (!NearMiss.Begin(duration,gameplayConfig)) return false;
            nearMissApproachLane=gameplayLoop.Player.LaneIndex;
            nearMissApproachX=gameplayLoop.Player.CurrentPosition.x;
            NearMissPassCompleted=false;
            nearMissTraffic=traffic; nearMissSimulationScale=gameplayConfig.NearMissTimeScale; ResetPresentationClock();
            if (gameplayLoop.Audio) gameplayLoop.Audio.BeginNearMiss();
            ComposeClock(); return true;
        }
        internal void CancelNearMiss()
        {
            NearMiss.Cancel(); NearMissPassCompleted=false; nearMissApproachLane=0; nearMissApproachX=0f; nearMissTraffic=default; nearMissSimulationScale=1f;
            if (gameplayLoop.Audio) gameplayLoop.Audio.EndNearMiss();
            ComposeClock();
        }
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
            if (!clockCaptured) { baselineFixedDelta=Time.fixedDeltaTime; clockCaptured=true; }
            NearMiss.Reset(); ResetPresentationClock(); ComposeClock();
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
            State = next; ResetPresentationClock();
            debugState = next;
            CaptureBest();
            Session.SetSimulationActive(next == GameState.Playing);
            if (next == GameState.Menu || next == GameState.GameOver) CancelNearMiss();
            ComposeClock();
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
            NearMiss.Reset(); Time.timeScale=1f;
            if (clockCaptured) RestoreFixedStep();
            clockCaptured=false;
        }

    }
}
