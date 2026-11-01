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
        private RunSession session;
        private bool runPrepared;

        public GameManager Owner => gameManager;
        public InputRouter Input => inputRouter;
        public PlayerController Player => player;
        public RoadManager Roads => roads;
        public WorldOrigin Origin => worldOrigin;
        public SpawnDirector Planner => spawnDirector;
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
            CleanupRun();
        }

        internal bool ValidateWorld(out string error)
        {
            error = null;
            if (!roads || !worldOrigin || !spawnDirector || !spawnDirector.enabled || !spawnDirector.gameObject.activeInHierarchy ||
                roads.Pools != gameManager.Pools || worldOrigin.Player != player || worldOrigin.Roads != roads ||
                spawnDirector.Configuration != roads.Configuration || spawnDirector.Gameplay != gameManager.Configuration)
            { error = "Assign RoadManager, WorldOrigin and SpawnDirector with shared configuration and explicit owners."; return false; }
            return roads.ValidateRequired(gameManager.Configuration, out error) && worldOrigin.TryValidate(out error);
        }

        internal bool ResetRun(int seed)
        {
            TickIndex = 0;
            inputRouter.Clear();
            worldOrigin.ResetRun();
            roads.ResetRun(seed);
            spawnDirector.ResetRun(seed);
            player.ResetRun();
            WorldFailure = null;
            if (!roads.EnsureCoverage(0d, 0d)) { WorldFailure = roads.LastError; return false; }
            spawnDirector.FillWindow(0d);
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
            if (roads) roads.ReleaseAll();
            if (spawnDirector) spawnDirector.Clear();
        }

        internal void ApplyState(GameState state)
        {
            if (state != GameState.Playing)
            {
                player.ClearQueuedCommand();
                player.HoldPose();
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
            if (!IsRunning) return false;
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                throw new System.ArgumentOutOfRangeException(nameof(deltaTime));
            // Reserve the target coverage before mutating the player/session. Even a multi-window
            // catch-up either commits a complete window or leaves the existing road intact.
            double targetDistance = session.Snapshot.Distance + (double)session.Snapshot.CruiseSpeed * deltaTime;
            if (!roads.EnsureCoverage(targetDistance, worldOrigin.Offset))
            {
                WorldFailure = roads.LastError;
                Debug.LogError("[GameplayLoop] " + WorldFailure, this);
                gameManager.Pause();
                return false;
            }
            int command = inputRouter.ConsumeLaneCommand();
            if (command != 0) player.RequestLane(command);
            double travel = player.PredictStep(deltaTime, session.Snapshot.CruiseSpeed);
            player.SetAbsoluteForward(targetDistance, worldOrigin.Offset);
            player.CommitPose();
            session.RecordMovement(deltaTime, travel);
            TickIndex++;
            // Contact dispatch/pose commit precede this explicit world phase (Task 06 extends it).
            spawnDirector.FillWindow(targetDistance);
            worldOrigin.RebaseIfNeeded(targetDistance);
            session.PublishSnapshot();
            return true;
        }
    }
}
