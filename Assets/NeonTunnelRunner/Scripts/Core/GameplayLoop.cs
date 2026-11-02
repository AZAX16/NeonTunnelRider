using UnityEngine;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public sealed class GameplayLoop : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private PlayerController player;
        [SerializeField] private RoadManager roads;
        [SerializeField] private WorldOrigin worldOrigin;
        [SerializeField] private SpawnDirector spawnDirector;
        [SerializeField] private InteractionSensor interactionSensor;
        [SerializeField] private PlayerSurvival survival;
        [SerializeField] private ProgressionSystem progression;
        [SerializeField] private VehicleVisuals vehicleVisuals;
        private RunSession session;
        private bool runPrepared;

        public GameManager Owner => gameManager;
        public InputRouter Input => inputRouter;
        public PlayerController Player => player;
        public RoadManager Roads => roads;
        public WorldOrigin Origin => worldOrigin;
        public SpawnDirector Planner => spawnDirector;
        public InteractionSensor Sensor => interactionSensor;
        public PlayerSurvival Survival => survival;
        public ProgressionSystem Progression => progression;
        public VehicleVisuals Visuals => vehicleVisuals;
        public bool IsStepping { get; private set; }
        public string WorldFailure { get; private set; }
        public ulong TickIndex { get; private set; }
        public bool IsRunning => isActiveAndEnabled && gameManager && gameManager.isActiveAndEnabled &&
            gameManager.IsOwner && !gameManager.IsTransitioning && gameManager.State == GameState.Playing &&
            player && player.isActiveAndEnabled && runPrepared;

        internal void Initialize(GameManager owner, RunSession runSession)
        {
            if (gameManager != owner || runSession == null)
                throw new System.InvalidOperationException("GameplayLoop requires its serialized owner and session.");
            session = runSession;
            if (!player || !inputRouter || inputRouter.Owner != owner)
                throw new System.InvalidOperationException("Assign the player, InputRouter and its owner.");
            player.Initialize(owner.Configuration);
            inputRouter.Initialize(owner);
            spawnDirector.Initialize();
            if (!survival || survival.Owner != owner || !interactionSensor || interactionSensor.Player != player ||
                interactionSensor.Registry != spawnDirector || interactionSensor.Survival != survival ||
                !progression || progression.Owner!=owner || interactionSensor.Progression!=progression ||
                !vehicleVisuals || vehicleVisuals.Owner!=owner)
                throw new System.InvalidOperationException("Assign sensor, survival and their explicit owners.");
            survival.Initialize(session);
            progression.Initialize(session);
            vehicleVisuals.Initialize(session);
            interactionSensor.Initialize();
            CleanupRun();
        }

        internal bool ValidateWorld(out string error)
        {
            error = null;
            if (!roads || !worldOrigin || !spawnDirector || !spawnDirector.enabled || !spawnDirector.gameObject.activeInHierarchy ||
                roads.Pools != gameManager.Pools || worldOrigin.Player != player || worldOrigin.Roads != roads ||
                spawnDirector.Configuration != roads.Configuration || spawnDirector.Gameplay != gameManager.Configuration)
            { error = "Assign RoadManager, WorldOrigin and SpawnDirector with shared configuration and explicit owners."; return false; }
            return roads.ValidateRequired(gameManager.Configuration, out error) && worldOrigin.TryValidate(out error) &&
                spawnDirector.ValidateRequired(out error) && ValidateInteractions(out error);
        }

        private bool ValidateInteractions(out string error)
        {
            error=null;
            if (!survival.isActiveAndEnabled || !interactionSensor.isActiveAndEnabled || !progression.isActiveAndEnabled || !vehicleVisuals.isActiveAndEnabled || spawnDirector.Pools != gameManager.Pools ||
                !worldOrigin.HasParticipant(spawnDirector) || interactionSensor.Capacity < spawnDirector.Capacity)
            { error="Enable and wire sensor/survival/traffic with one origin participant and sufficient contact capacity."; return false; }
            return true;
        }

        internal bool ResetRun(int seed)
        {
            TickIndex = 0;
            inputRouter.Clear();
            worldOrigin.ResetRun();
            roads.ResetRun(seed);
            spawnDirector.ResetRun(seed);
            session.Reset(seed, session.Snapshot.BestScore);
            progression.RefreshDebug();
            player.ResetRun();
            survival.ResetRun();
            vehicleVisuals.ResetVisuals();
            interactionSensor.Clear();
            WorldFailure = null;
            if (!roads.EnsureCoverage(0d, 0d)) { WorldFailure = roads.LastError; return false; }
            if (!spawnDirector.PrepareFuelSupply(0d,0d,0d)) { WorldFailure=spawnDirector.LastError; return false; }
            spawnDirector.CommitFuelSupply();
            spawnDirector.FillWindow(0d);
            spawnDirector.MaintainTraffic(0d,0d);
            runPrepared = true;
            return true;
        }

        internal void StopRun() => runPrepared = false;

        internal void CleanupRun()
        {
            runPrepared = false;
            TickIndex = 0;
            if (inputRouter) inputRouter.ApplyState(GameState.Menu);
            if (player) player.ClearQueuedCommand();
            if (interactionSensor) interactionSensor.Clear();
            if (spawnDirector) spawnDirector.Clear();
            if (survival) survival.ResetRun();
            if (vehicleVisuals) vehicleVisuals.ResetVisuals();
            if (roads) roads.ReleaseAll();
        }

        internal void ApplyState(GameState state)
        {
            if (state != GameState.Playing)
            {
                player.ClearQueuedCommand();
                player.HoldPose();
                spawnDirector.HoldTraffic();
            }
            inputRouter.ApplyState(state);
        }

        internal void Shutdown()
        {
            CleanupRun();
            if (inputRouter) inputRouter.Shutdown();
        }

        private void FixedUpdate() => Step(Time.fixedDeltaTime);

        /// <summary>Deterministic entry point shared by FixedUpdate and integration tests.</summary>
        public bool Step(float deltaTime)
        {
            if (!IsRunning || IsStepping) return false;
            if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(deltaTime));
            if (deltaTime == 0f) return false;
            // Reserve the target coverage before mutating the player/session. Even a multi-window
            // catch-up either commits a complete window or leaves the existing road intact.
            double targetDistance = session.Snapshot.Distance + (double)session.Snapshot.CruiseSpeed * deltaTime;
            if (!spawnDirector.PrepareFuelSupply(session.Snapshot.Distance,targetDistance,worldOrigin.Offset))
            {
                WorldFailure=spawnDirector.LastError;
                Debug.LogError("[GameplayLoop] "+WorldFailure,this);
                gameManager.Pause();
                return false;
            }
            if (!roads.EnsureCoverage(targetDistance, worldOrigin.Offset))
            {
                spawnDirector.RollbackFuelSupply();
                WorldFailure = roads.LastError;
                Debug.LogError("[GameplayLoop] " + WorldFailure, this);
                gameManager.Pause();
                return false;
            }
            spawnDirector.CommitFuelSupply();
            GameOverReason terminal=GameOverReason.None;
            IsStepping=true;
            try
            {
                int command = inputRouter.ConsumeLaneCommand();
                if (command != 0) player.RequestLane(command);
                double travel = player.PredictStep(deltaTime, session.Snapshot.CruiseSpeed);
                player.SetAbsoluteForward(targetDistance, worldOrigin.Offset);
                spawnDirector.PredictTraffic(deltaTime,worldOrigin.Offset);
                interactionSensor.CollectContacts();
                interactionSensor.DispatchContacts(session,deltaTime);
                player.CommitPose();
                spawnDirector.CommitTraffic();
                session.RecordMovement(deltaTime, travel);
                progression.RecomputeScore();
                TickIndex++;
                survival.StepFuel(deltaTime);
                vehicleVisuals.FinishTick();
                terminal=survival.EvaluateEnd();
                if (terminal == GameOverReason.None)
                {
                    spawnDirector.FillWindow(targetDistance);
                    spawnDirector.MaintainTraffic(targetDistance,worldOrigin.Offset);
                    worldOrigin.RebaseIfNeeded(targetDistance);
                    session.PublishSnapshot();
                }
            }
            finally { IsStepping=false; }
            // Contact callbacks cannot end/reset a transaction. Release that guard only
            // after commit/drain, then let the existing lifecycle guard publish one frozen result.
            if (terminal != GameOverReason.None) gameManager.EndRun(terminal);
            return true;
        }
    }
}
