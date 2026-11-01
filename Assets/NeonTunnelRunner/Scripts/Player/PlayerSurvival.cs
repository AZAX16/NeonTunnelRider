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
        private RunSession session;
        private double graceUntil;
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
                encounter.PlayingTime < graceUntil || Lives <= 0) return false;
            session.SetResources(Math.Max(0,Lives-1),session.Snapshot.Fuel);
            graceUntil=encounter.PlayingTime+gameManager.Configuration.DamageGraceDuration;
            RefreshDebug();
            session.PublishDamage(new DamageEvent(session.Snapshot.RunId, encounter, Lives));
            return true;
        }
        public void AddFuel(float amount)
        {
            if (!float.IsFinite(amount) || amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            if (session == null || !gameManager || gameManager.State != GameState.Playing || gameManager.IsTransitioning) return;
            session.SetResources(Lives,Mathf.Min(gameManager.Configuration.FuelCapacity,session.Snapshot.Fuel+amount));
        }
        internal void RefreshDebug() { debugLives=Lives; debugGraceRemaining=(float)GraceRemaining; }
    }
}
