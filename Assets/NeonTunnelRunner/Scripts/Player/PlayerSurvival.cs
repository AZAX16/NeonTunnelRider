using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    public sealed class PlayerSurvival : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private int debugLives;
        [SerializeField] private float debugGraceRemaining;
        [SerializeField] private float debugFuel;
        private RunSession session;
        private double graceUntil;
        // TOI is float; allow at most one microsecond of contact-time quantization.
        internal const double GraceBoundaryTolerance = 0.000001d;
        public GameManager Owner => gameManager;
        public int Lives => session == null ? 0 : session.Snapshot.Lives;
        public double GraceUntil => graceUntil;
        public double GraceRemaining => session == null ? 0d : Math.Max(0d,graceUntil-session.Snapshot.ElapsedTime);
        internal void Initialize(RunSession run) { session=run; ResetRun(); }
        internal void ResetRun() { graceUntil=0d; RefreshDebug(); }
        public bool TryDamage(Encounter encounter)
        {
            if (!gameManager || gameManager.State != GameState.Playing || gameManager.IsTransitioning ||
                session == null || !double.IsFinite(encounter.PlayingTime) || encounter.PlayingTime < session.Snapshot.ElapsedTime ||
                encounter.PlayingTime + GraceBoundaryTolerance < graceUntil || Lives <= 0) return false;
            gameManager.CancelNearMiss();
            session.RemoveLife();
            graceUntil=encounter.PlayingTime+gameManager.Configuration.DamageGraceDuration;
            RefreshDebug();
            session.PublishDamage(new DamageEvent(session.Snapshot.RunId, encounter, Lives));
            return true;
        }
        public void AddFuel(float amount)
        {
            if (!float.IsFinite(amount) || amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            if (session == null || !gameManager || gameManager.State != GameState.Playing || gameManager.IsTransitioning) return;
            session.RestoreFuel(amount);
            RefreshDebug();
        }
        internal void StepFuel(float deltaTime)
        {
            if (!float.IsFinite(deltaTime) || deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (session == null || !gameManager || gameManager.State != GameState.Playing || gameManager.IsTransitioning) return;
            session.DrainFuel(deltaTime);
            RefreshDebug();
        }
        public GameOverReason EvaluateEnd()
        {
            if (session == null || !gameManager || gameManager.State != GameState.Playing || gameManager.IsTransitioning)
                return GameOverReason.None;
            var snapshot=session.Snapshot;
            return snapshot.Lives <= 0 ? GameOverReason.Lives : snapshot.Fuel <= 0f ? GameOverReason.Fuel : GameOverReason.None;
        }
        internal void RefreshDebug() { debugLives=Lives; debugGraceRemaining=(float)GraceRemaining; debugFuel=session == null ? 0f : session.Snapshot.Fuel; }
    }
}
