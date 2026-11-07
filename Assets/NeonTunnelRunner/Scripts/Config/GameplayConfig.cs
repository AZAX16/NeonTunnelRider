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
        [SerializeField, Min(0.01f)] private float laneTransitionDuration = 0.12f;
        [SerializeField, Min(0f)] private float playerRootHeight = 0.6f;
        [SerializeField] private Vector3 playerHalfExtents = new Vector3(0.85f, 0.45f, 1.8f);

        [Header("Survival and score authoring")]
        [SerializeField, Min(0f)] private float damageGraceDuration = 1f;
        [SerializeField, Min(0f)] private float fuelDrainPerSecond = 2f;
        [SerializeField, Min(0f)] private float fuelRefill = 30f;
        [SerializeField, Min(0)] private int coinScore = 100;
        [SerializeField, Min(1f)] private float difficultyDuration = 180f;
        [SerializeField, Min(1f)] private float difficultyDistance = 6000f;
        [SerializeField, Range(0f, 1f)] private float startingOccupancy = 0.25f;
        [SerializeField, Range(0f, 1f)] private float maximumOccupancy = 0.8f;

        [Header("Traffic expansion")]
        [SerializeField, Range(1f, 2f)] private float trafficDensityMultiplier = 1.3f;
        [SerializeField, Range(.8f,1f)] private float maximumTrafficOccupancy = .95f;
        [SerializeField] private Vector2 overtakerMargin = new Vector2(6f,12f);
        [SerializeField] private Vector2 trafficDecisionInterval = new Vector2(1.5f,4.5f);
        [SerializeField, Min(.1f)] private float trafficLaneDuration = .30f;
        [Header("Near miss (real playing seconds)")]
        [SerializeField, Range(.1f,1f)] private float nearMissTimeScale = .45f;
        [SerializeField, Min(.1f)] private float nearMissDuration = 6f;
        [SerializeField, Min(0f)] private float nearMissCooldown = 12f;
        [SerializeField, Min(.01f)] private float nearMissEntry = .15f, nearMissExit = .35f;
        [SerializeField, Range(0f,12f)] private float nearMissZoom = 7f;
        // Lane spacing 3.2 - (player half width .85 + car half width 1.1) = 1.25m.
        [SerializeField, Min(0f)] private float nearMissClearance = 1.35f;
        public float TrafficDensityMultiplier => Mathf.Clamp(trafficDensityMultiplier,1f,2f);
        public float MaximumTrafficOccupancy => Mathf.Clamp(maximumTrafficOccupancy,.8f,1f);
        public Vector2 OvertakerMargin => overtakerMargin;
        public Vector2 TrafficDecisionInterval => trafficDecisionInterval;
        public float TrafficLaneDuration => Mathf.Max(.1f,trafficLaneDuration);
        [SerializeField, Min(.1f)] private float rearWarningDuration = 1.5f;
        public float RearWarningDuration => Mathf.Max(.1f,rearWarningDuration);
        [SerializeField, Min(0f)] private float nearMissDodgeReactionTime = .06f;
        public float NearMissDodgeReactionTime => Mathf.Max(0f,nearMissDodgeReactionTime);
        public float NearMissApproachRatio => 2.5f/6f;
        public float NearMissPassRatio => 1f/6f;
        public float NearMissTimeScale => Mathf.Clamp(nearMissTimeScale,.1f,1f);
        public float NearMissDuration => Mathf.Max(.1f,nearMissDuration);
        public float NearMissCooldown => Mathf.Max(nearMissDuration,nearMissCooldown);
        public float NearMissEntry => Mathf.Max(.01f,nearMissEntry);
        public float NearMissExit => Mathf.Max(.01f,nearMissExit);
        public float NearMissZoom => Mathf.Clamp(nearMissZoom,0f,12f);
        public float NearMissClearance => Mathf.Max(0f,nearMissClearance);

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
        public float StartingOccupancy => startingOccupancy;
        public float MaximumOccupancy => maximumOccupancy;

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
            startingOccupancy = Mathf.Clamp01(startingOccupancy);
            maximumOccupancy = Mathf.Clamp(maximumOccupancy, startingOccupancy, 1f);
        }
    }
}
