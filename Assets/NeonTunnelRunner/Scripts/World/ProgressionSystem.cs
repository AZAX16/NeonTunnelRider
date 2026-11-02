using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Pure progression rules; all run values remain in RunSession.</summary>
    [DisallowMultipleComponent]
    public sealed class ProgressionSystem : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private int debugCoins;
        [SerializeField] private int debugScore;
        [SerializeField] private int debugBest;
        [SerializeField] private float debugDifficulty;
        [SerializeField] private float debugTargetSpeed;
        [SerializeField] private float debugActualSpeed;
        [SerializeField] private float debugOccupancy;
        private RunSession session;
        public GameManager Owner => gameManager;
        internal void Initialize(RunSession run) { session=run; RefreshDebug(); }
        public static float EvaluateDifficulty(double elapsedPlayingSeconds, double absoluteDistance, GameplayConfig config)
        {
            if (!double.IsFinite(elapsedPlayingSeconds) || elapsedPlayingSeconds < 0d) throw new ArgumentOutOfRangeException(nameof(elapsedPlayingSeconds));
            if (!double.IsFinite(absoluteDistance) || absoluteDistance < 0d) throw new ArgumentOutOfRangeException(nameof(absoluteDistance));
            return (float)Math.Clamp(Math.Max(elapsedPlayingSeconds / config.DifficultyDuration, absoluteDistance / config.DifficultyDistance), 0d, 1d);
        }
        public static float Occupancy(float difficulty, GameplayConfig config) =>
            Mathf.Lerp(config.StartingOccupancy, config.MaximumOccupancy, difficulty);
        internal void EvaluateTickStart()
        {
            var s = session.Snapshot;
            float d = EvaluateDifficulty(s.ElapsedTime, s.Distance, gameManager.Configuration);
            session.SetProgression(d, Mathf.Lerp(gameManager.Configuration.StartingSpeed, gameManager.Configuration.MaximumSpeed, d),
                Occupancy(d, gameManager.Configuration));
        }
        public void AddCoin()
        {
            if (session == null || !gameManager || !gameManager.IsOwner || gameManager.IsTransitioning ||
                gameManager.State != GameState.Playing) return;
            int coins=session.Snapshot.Coins;
            if (coins < int.MaxValue) coins++;
            session.SetScore(coins,ComputeScore(session.Snapshot.Distance,coins,gameManager.Configuration.CoinScore));
            gameManager.CaptureBest();
            RefreshDebug();
        }
        internal void RecomputeScore()
        {
            var snapshot=session.Snapshot;
            session.SetScore(snapshot.Coins,ComputeScore(snapshot.Distance,snapshot.Coins,gameManager.Configuration.CoinScore));
            gameManager.CaptureBest();
            RefreshDebug();
        }
        public static int ComputeScore(double distance,int coins,int coinValue)
        {
            if (!double.IsFinite(distance) || distance < 0d) throw new ArgumentOutOfRangeException(nameof(distance));
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            if (coinValue < 0) throw new ArgumentOutOfRangeException(nameof(coinValue));
            long reward=(long)coins*coinValue;
            if (reward >= int.MaxValue || distance >= int.MaxValue-reward) return int.MaxValue;
            return (int)Math.Floor(distance)+(int)reward;
        }
        internal void RefreshDebug()
        {
            if (session == null) return;
            var s = session.Snapshot;
            debugCoins=s.Coins; debugScore=s.Score; debugBest=s.BestScore;
            debugDifficulty=s.Difficulty; debugTargetSpeed=s.CruiseSpeed;
            debugActualSpeed=s.ActualSpeed; debugOccupancy=s.AllowedSlotOccupancy;
        }
    }
}
