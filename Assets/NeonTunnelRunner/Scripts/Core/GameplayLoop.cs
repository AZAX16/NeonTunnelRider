using UnityEngine;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public sealed class GameplayLoop : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        private RunSession session;
        private bool runPrepared;

        public GameManager Owner => gameManager;
        public ulong TickIndex { get; private set; }
        public bool IsRunning => isActiveAndEnabled && gameManager && gameManager.isActiveAndEnabled &&
            gameManager.IsOwner && !gameManager.IsTransitioning && gameManager.State == GameState.Playing && runPrepared;

        internal void Initialize(GameManager owner, RunSession runSession)
        {
            if (gameManager != owner || runSession == null)
                throw new System.InvalidOperationException("GameplayLoop requires its serialized owner and session.");
            session = runSession;
            CleanupRun();
        }

        internal void ResetRun()
        {
            TickIndex = 0;
            runPrepared = true;
        }

        internal void StopRun() => runPrepared = false;

        internal void CleanupRun()
        {
            runPrepared = false;
            TickIndex = 0;
        }

        private void FixedUpdate() => Step(Time.fixedDeltaTime);

        /// <summary>Deterministic entry point shared by FixedUpdate and integration tests.</summary>
        public bool Step(float deltaTime)
        {
            if (!IsRunning) return false;
            // Task 02 owns the run clock only. Travel, fuel and scoring arrive with their systems.
            session.Advance(deltaTime, 0d);
            session.SetSimulationActive(true);
            TickIndex++;
            session.PublishSnapshot();
            return true;
        }
    }
}
