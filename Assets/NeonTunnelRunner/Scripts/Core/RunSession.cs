using System;

namespace NeonTunnelRunner
{
    /// <summary>A value copy; observers cannot mutate the authoritative session or config.</summary>
    public readonly struct RunSnapshot
    {
        public Guid RunId { get; }
        public int Seed { get; }
        public int Lives { get; }
        public float Fuel { get; }
        public double ElapsedTime { get; }
        public double Distance { get; }
        public int Coins { get; }
        public int Score { get; }
        public int BestScore { get; }
        public float CruiseSpeed { get; }
        public float ActualSpeed { get; }

        internal RunSnapshot(Guid runId, int seed, int lives, float fuel, double elapsedTime,
            double distance, int coins, int score, int bestScore, float cruiseSpeed, float actualSpeed)
        {
            RunId = runId;
            Seed = seed;
            Lives = lives;
            Fuel = fuel;
            ElapsedTime = elapsedTime;
            Distance = distance;
            Coins = coins;
            Score = score;
            BestScore = bestScore;
            CruiseSpeed = cruiseSpeed;
            ActualSpeed = actualSpeed;
        }
    }

    /// <summary>
    /// One run's authoritative data. Runtime services mutate internally, then publish once
    /// after a complete lifecycle transaction or simulation tick. No persistence or resource ticking.
    /// </summary>
    public sealed class RunSession
    {
        private readonly GameplayConfig config;
        private Guid runId;
        private int seed;
        private int lives;
        private float fuel;
        private double elapsedTime;
        private double distance;
        private int coins;
        private int score;
        private int bestScore;
        private float cruiseSpeed;
        private float actualSpeed;

        public event Action<RunSnapshot> SnapshotChanged;

        public RunSnapshot Snapshot => new RunSnapshot(runId, seed, lives, fuel, elapsedTime,
            distance, coins, score, bestScore, cruiseSpeed, actualSpeed);

        public RunSession(GameplayConfig config, int bestScore = 0)
        {
            this.config = config ? config : throw new ArgumentNullException(nameof(config));
            this.bestScore = Math.Max(0, bestScore);
            lives = config.StartingLives;
            fuel = config.FuelCapacity;
            cruiseSpeed = config.StartingSpeed;
            // Menu has no active run: Guid.Empty and actual speed zero until StartRun.
        }

        internal void Reset(int runSeed, int preservedBestScore)
        {
            runId = Guid.NewGuid();
            seed = runSeed;
            lives = config.StartingLives;
            fuel = config.FuelCapacity;
            elapsedTime = distance = 0d;
            coins = score = 0;
            bestScore = Math.Max(0, preservedBestScore);
            cruiseSpeed = config.StartingSpeed;
            actualSpeed = 0f;
        }

        internal void Advance(float deltaTime, double travelledDistance)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (double.IsNaN(travelledDistance) || double.IsInfinity(travelledDistance) || travelledDistance < 0d)
                throw new ArgumentOutOfRangeException(nameof(travelledDistance));
            elapsedTime += deltaTime;
            distance += travelledDistance;
        }

        internal void SetResources(int remainingLives, float remainingFuel)
        {
            if (remainingLives < 0 || remainingLives > config.StartingLives)
                throw new ArgumentOutOfRangeException(nameof(remainingLives));
            if (float.IsNaN(remainingFuel) || float.IsInfinity(remainingFuel) || remainingFuel < 0f || remainingFuel > config.FuelCapacity)
                throw new ArgumentOutOfRangeException(nameof(remainingFuel));
            lives = remainingLives;
            fuel = remainingFuel;
        }

        internal void SetScore(int coinCount, int currentScore)
        {
            if (coinCount < 0) throw new ArgumentOutOfRangeException(nameof(coinCount));
            if (currentScore < 0) throw new ArgumentOutOfRangeException(nameof(currentScore));
            coins = coinCount;
            score = currentScore;
        }

        internal void RetainBestScore(int suppliedBestScore)
        {
            bestScore = Math.Max(bestScore, Math.Max(0, suppliedBestScore));
        }

        internal void SetCruiseSpeed(float speed)
        {
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0f || speed > config.MaximumSpeed)
                throw new ArgumentOutOfRangeException(nameof(speed));
            cruiseSpeed = speed;
        }

        internal void SetSimulationActive(bool active)
        {
            actualSpeed = active ? cruiseSpeed : 0f;
        }

        internal RunSnapshot PublishSnapshot()
        {
            RunSnapshot snapshot = Snapshot;
            SnapshotChanged?.Invoke(snapshot);
            return snapshot;
        }

        internal void DetachObservers() => SnapshotChanged = null;
    }
}
