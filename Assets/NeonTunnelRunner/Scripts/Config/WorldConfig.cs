using UnityEngine;

namespace NeonTunnelRunner
{
    [CreateAssetMenu(menuName = "Neon Tunnel Runner/World Config")]
    public sealed class WorldConfig : ScriptableObject
    {
        public const double SegmentLength = 60d;
        public const float RoadWidth = 9.6f;
        public const double BlockLength = 480d;
        public const double TrafficSpan = 360d;
        public const double TransitionBuffer = 120d;
        public const int RowsPerBlock = 6;
        [SerializeField] private float ahead = 300f;
        [SerializeField] private float behind = 60f;
        [SerializeField] private float rebaseThreshold = 1000f;
        [SerializeField] private float reactionTime = 0.7f;
        [SerializeField] private float transitionMargin = 0.3f;
        [SerializeField, Range(0f, 1f)] private float candidateProbability = 0.65f;
        public double Ahead => ahead;
        public double Behind => behind;
        public double RebaseThreshold => rebaseThreshold;
        public float ReactionTime => reactionTime;
        public float TransitionMargin => transitionMargin;
        public float CandidateProbability => candidateProbability;
        public int MaximumSegments => (int)System.Math.Ceiling((Ahead + Behind) / SegmentLength) + 1;

        public bool TryValidate(GameplayConfig gameplay, out string error)
        {
            if (!gameplay || !float.IsFinite(ahead) || !float.IsFinite(behind) || ahead < 300f || behind < 60f ||
                Ahead + Behind > 360d || MaximumSegments > RoadManager.WindowCapacity || !float.IsFinite(rebaseThreshold) || rebaseThreshold != 1000f ||
                !float.IsFinite(reactionTime) || reactionTime < 0f || !float.IsFinite(transitionMargin) || transitionMargin < 0f ||
                !float.IsFinite(candidateProbability) || candidateProbability < 0f || candidateProbability > 1f ||
                gameplay.LaneWidth != 3.2f || !float.IsFinite(gameplay.MaximumSpeed) || gameplay.MaximumSpeed <= 0f ||
                !float.IsFinite(gameplay.LaneTransitionDuration) || gameplay.LaneTransitionDuration <= 0f)
            { error = "Invalid WorldConfig coverage, timing, probability or three-lane gameplay configuration."; return false; }
            if (!PatternValidator.CanTransition(-1, 1, this, gameplay))
            { error = "WorldConfig transition buffer is unsafe at maximum player speed."; return false; }
            error = null;
            return true;
        }
    }
}
