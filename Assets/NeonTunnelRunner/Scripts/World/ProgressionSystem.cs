using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Score rules only. RunSession remains the sole owner of distance, coins and score.</summary>
    [DisallowMultipleComponent]
    public sealed class ProgressionSystem : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private int debugCoins;
        [SerializeField] private int debugScore;
        private RunSession session;
        public GameManager Owner => gameManager;
        internal void Initialize(RunSession run) { session=run; RefreshDebug(); }
        public void AddCoin()
        {
            if (session == null || !gameManager || !gameManager.IsOwner || gameManager.IsTransitioning ||
                gameManager.State != GameState.Playing) return;
            int coins=session.Snapshot.Coins;
            if (coins < int.MaxValue) coins++;
            session.SetScore(coins,ComputeScore(session.Snapshot.Distance,coins,gameManager.Configuration.CoinScore));
            RefreshDebug();
        }
        internal void RecomputeScore()
        {
            var snapshot=session.Snapshot;
            session.SetScore(snapshot.Coins,ComputeScore(snapshot.Distance,snapshot.Coins,gameManager.Configuration.CoinScore));
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
        { debugCoins=session == null ? 0 : session.Snapshot.Coins; debugScore=session == null ? 0 : session.Snapshot.Score; }
    }
}
