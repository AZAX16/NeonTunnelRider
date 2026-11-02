using UnityEngine;

namespace NeonTunnelRunner
{
    [CreateAssetMenu(menuName = "Neon Tunnel Runner/Gameplay Config")]
    public sealed class GameplayConfig : ScriptableObject
    {
        [Header("Run defaults")]
        [SerializeField, Min(1)] private int startingLives = 3;
        [SerializeField, Min(1f)] private float fuelCapacity = 100f;
        [SerializeField, Min(0f)] private float startingSpeed = 22f;
        [SerializeField, Min(0f)] private float maximumSpeed = 50f;

        [Header("Lane authoring (movement added in task 03)")]
        [SerializeField, Min(0.01f)] private float laneWidth = 3.2f;
        [SerializeField, Min(0.01f)] private float laneTransitionDuration = 0.28f;
        [SerializeField, Min(0f)] private float playerRootHeight = 0.6f;
        [SerializeField] private Vector3 playerHalfExtents = new Vector3(0.85f, 0.45f, 1.8f);

        [Header("Survival and score authoring")]
        [SerializeField, Min(0f)] private float damageGraceDuration = 1f;
        [SerializeField, Min(0f)] private float fuelDrainPerSecond = 2f;
        [SerializeField, Min(0f)] private float fuelRefill = 30f;
        [SerializeField, Min(0)] private int coinScore = 100;
        [SerializeField, Min(1f)] private float difficultyDuration = 180f;
        [SerializeField, Min(1f)] private float difficultyDistance = 6000f;

        public int StartingLives => startingLives;
        public float FuelCapacity => fuelCapacity;
        public float StartingSpeed => startingSpeed;
        public float MaximumSpeed => maximumSpeed;
        public int StartingLane => 0;
        public float LaneWidth => laneWidth;
        public float LaneTransitionDuration => laneTransitionDuration;
        public Vector3 StartingPosition => new Vector3(StartingLane * laneWidth, playerRootHeight, 0f);
        public Vector3 PlayerHalfExtents => playerHalfExtents;
        public float DamageGraceDuration => damageGraceDuration;
        public float FuelDrainPerSecond => fuelDrainPerSecond;
        public float FuelRefill => fuelRefill;
        public int CoinScore => coinScore;
        public float DifficultyDuration => difficultyDuration;
        public float DifficultyDistance => difficultyDistance;

        // Authoring validation only. Runtime code reads these values and never writes them.
        private void OnValidate()
        {
            startingLives = Mathf.Max(1, startingLives);
            fuelCapacity = Mathf.Max(1f, fuelCapacity);
            startingSpeed = Mathf.Max(0f, startingSpeed);
            maximumSpeed = Mathf.Max(startingSpeed, maximumSpeed);
            laneWidth = Mathf.Max(0.01f, laneWidth);
            laneTransitionDuration = Mathf.Max(0.01f, laneTransitionDuration);
            playerRootHeight = Mathf.Max(0f, playerRootHeight);
            playerHalfExtents = Vector3.Max(Vector3.one * 0.01f, playerHalfExtents);
            damageGraceDuration = Mathf.Max(0f, damageGraceDuration);
            fuelDrainPerSecond = Mathf.Max(0f, fuelDrainPerSecond);
            fuelRefill = Mathf.Clamp(fuelRefill, 0f, fuelCapacity);
            coinScore = Mathf.Max(0, coinScore);
            difficultyDuration = Mathf.Max(1f, difficultyDuration);
            difficultyDistance = Mathf.Max(1f, difficultyDistance);
        }
    }
}
