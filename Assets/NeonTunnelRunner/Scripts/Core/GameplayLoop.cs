using UnityEngine;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public sealed class GameplayLoop : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputRouter inputRouter;
        [SerializeField] private PlayerController player;
        private RunSession session;
        private bool runPrepared;

        public GameManager Owner => gameManager;
        public InputRouter Input => inputRouter;
        public PlayerController Player => player;
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

        internal void ResetRun()
        {
            TickIndex = 0;
            inputRouter.Clear();
            player.ResetRun();
            runPrepared = true;
        }

        internal void StopRun() => runPrepared = false;

        internal void CleanupRun()
        {
            runPrepared = false;
            TickIndex = 0;
            if (inputRouter) inputRouter.Clear();
            if (player) player.ClearQueuedCommand();
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
            int command = inputRouter.ConsumeLaneCommand();
            if (command != 0) player.RequestLane(command);
            double travel = player.PredictStep(deltaTime, session.Snapshot.CruiseSpeed);
            player.CommitPose();
            session.RecordMovement(deltaTime, travel);
            TickIndex++;
            session.PublishSnapshot();
            return true;
        }
    }
}
