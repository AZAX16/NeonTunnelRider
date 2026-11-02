using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Immutable authoring values. Presentation never writes these at runtime.</summary>
    [CreateAssetMenu(menuName = "Neon Tunnel Runner/Presentation Config")]
    public sealed class PresentationConfig : ScriptableObject
    {
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 4.2f, -8f);
        [SerializeField, Min(1f)] private float lookAhead = 12f;
        [SerializeField, Range(30f, 90f)] private float fieldOfView = 65f;
        [SerializeField, Min(.01f)] private float nearClip = .1f;
        [SerializeField, Min(130f)] private float farClip = 280f;
        [SerializeField, Min(.001f)] private float lateralTimeConstant = .08f;
        [SerializeField, Range(0f, 1f)] private float maximumLateralLag = .65f;
        [SerializeField, Range(0f, 8f)] private float maximumRoll = 6f;
        [SerializeField, Min(.001f)] private float rollTimeConstant = .09f;
        [SerializeField, Range(0f, .08f)] private float bobAmplitude = .06f;
        [SerializeField, Min(0f)] private float bobFrequency = 1.4f;
        [SerializeField, Range(0f, .12f)] private float impulseMagnitude = .085f;
        [SerializeField, Min(.01f)] private float impulseDuration = .24f;
        public Vector3 CameraOffset => cameraOffset;
        public float LookAhead => lookAhead;
        public float FieldOfView => fieldOfView;
        public float NearClip => nearClip;
        public float FarClip => farClip;
        public float LateralTimeConstant => lateralTimeConstant;
        public float MaximumLateralLag => Mathf.Clamp(maximumLateralLag, 0f, 1f);
        public float MaximumRoll => Mathf.Clamp(maximumRoll, 0f, 8f);
        public float RollTimeConstant => rollTimeConstant;
        public float BobAmplitude => Mathf.Clamp(bobAmplitude, 0f, .08f);
        public float BobFrequency => bobFrequency;
        public float ImpulseMagnitude => Mathf.Clamp(impulseMagnitude, 0f, .12f);
        public float ImpulseDuration => impulseDuration;
    }
}
