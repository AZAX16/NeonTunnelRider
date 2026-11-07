using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>One render pose source: the stable player Transform (Rigidbody interpolation=None).
    /// Only lateral framing is filtered. Never feed the cosmetic impulse back into basePosition.</summary>
    [DisallowMultipleComponent]
    public sealed class ChaseCamera : MonoBehaviour, IWorldOriginParticipant
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlayerController player;
        [SerializeField] private PresentationConfig configuration;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private WorldOrigin worldOrigin;
        private RunSession session, subscribedSession;
        private Vector3 basePosition, targetPosition, impulseSeed, impulseOffset;
        private float impulseAge, zoomModifier;
        private bool prepared;
        public GameManager Owner => gameManager;
        public PlayerController Player => player;
        public PresentationConfig Configuration => configuration;
        public Camera ViewCamera => viewCamera;
        public WorldOrigin Origin => worldOrigin;
        public Transform OriginRoot => transform;
        public Vector3 BasePosition => basePosition;
        public Vector3 TargetPosition => targetPosition;
        public Vector3 ImpulseOffset => impulseOffset;
        public ulong AcceptedImpulses { get; private set; }

        internal void Initialize(RunSession run)
        {
            Unsubscribe();
            if (!player || !configuration || !viewCamera || viewCamera.transform != transform || !worldOrigin ||
                transform.IsChildOf(player.transform))
                throw new InvalidOperationException("Assign the independent chase camera, stable player, configuration and origin owner.");
            session = run; prepared = false;
            AcceptedImpulses = 0;
            viewCamera.fieldOfView = configuration.FieldOfView;
            viewCamera.nearClipPlane = configuration.NearClip;
            viewCamera.farClipPlane = configuration.FarClip;
            ResetPresentation();
            Subscribe();
        }
        private void OnEnable() { if (session != null) ResetPresentation(); Subscribe(); }
        private void OnDisable() { Unsubscribe(); ResetPresentation(); }
        private void Subscribe()
        {
            if (!prepared || !isActiveAndEnabled || session == null || subscribedSession == session) return;
            Unsubscribe(); subscribedSession = session; subscribedSession.Damaged += OnDamage;
        }
        private void Unsubscribe()
        { if (subscribedSession != null) subscribedSession.Damaged -= OnDamage; subscribedSession = null; }
        internal void PrepareRun() { prepared = true; ResetPresentation(); Subscribe(); }
        internal void CleanupRun() { prepared = false; Unsubscribe(); ResetPresentation(); }
        internal void Shutdown() { CleanupRun(); session = null; }
        private void OnDamage(DamageEvent damage)
        {
            if (!isActiveAndEnabled || !gameManager || gameManager.State != GameState.Playing || gameManager.IsTransitioning ||
                session == null || damage.RunId != session.Snapshot.RunId) return;
            // Deterministic cosmetic kick; no gameplay or Unity random stream is consumed.
            impulseSeed = Vector3.ClampMagnitude(impulseOffset + new Vector3(.45f, .89f, 0f).normalized * configuration.ImpulseMagnitude, .12f);
            impulseOffset = impulseSeed; impulseAge = 0f; AcceptedImpulses++;
            ApplyPose(); // A terminal accepted hit remains visible even when no Playing LateUpdate follows.
        }
        private void LateUpdate() => Render(Time.unscaledDeltaTime);
        internal void Render(float deltaTime)
        {
            if (!isActiveAndEnabled || session == null || !gameManager || !float.IsFinite(deltaTime) || deltaTime <= 0f) return;
            if (gameManager.State!=GameState.Paused)
            {
                if (prepared && gameManager.NearMiss.Active) zoomModifier=gameManager.Configuration.NearMissZoom*gameManager.NearMiss.Weight;
                else zoomModifier=Mathf.MoveTowards(zoomModifier,0f,gameManager.Configuration.NearMissZoom*deltaTime/gameManager.Configuration.NearMissExit);
            }
            if (!gameManager.Loop.IsRunning) { if (gameManager.State==GameState.GameOver) ApplyPose(); return; }
            targetPosition = player.transform.position;
            var desired = targetPosition + configuration.CameraOffset;
            float blend = 1f - Mathf.Exp(-deltaTime / Mathf.Max(.001f, configuration.LateralTimeConstant));
            basePosition.x = Mathf.Lerp(basePosition.x, desired.x, blend);
            basePosition.x = Mathf.Clamp(basePosition.x, desired.x - configuration.MaximumLateralLag, desired.x + configuration.MaximumLateralLag);
            basePosition.y = desired.y; basePosition.z = desired.z;
            impulseAge = Mathf.Min(impulseAge + deltaTime, configuration.ImpulseDuration);
            float u = impulseAge / Mathf.Max(.01f, configuration.ImpulseDuration);
            impulseOffset = impulseSeed * ((1f-u)*(1f-u)*Mathf.Cos(u*Mathf.PI*4f));
            ApplyPose();
        }
        internal void HoldView()
        {
            if (session == null || !player || !configuration) return;
            // ApplyState holds the final logical root immediately, possibly before this frame's
            // LateUpdate. Reconcile that final translation without advancing smoothing or feedback.
            var pose=player.transform.position;
            basePosition.y=pose.y+configuration.CameraOffset.y;
            basePosition.z=pose.z+configuration.CameraOffset.z;
            float desiredX=pose.x+configuration.CameraOffset.x;
            basePosition.x=Mathf.Clamp(basePosition.x,desiredX-configuration.MaximumLateralLag,desiredX+configuration.MaximumLateralLag);
            targetPosition=pose;
            ApplyPose();
        }
        public void ResetPresentation()
        {
            impulseAge = zoomModifier = 0f; impulseSeed = impulseOffset = Vector3.zero;
            if (!player || !configuration) return;
            targetPosition = player.transform.position;
            basePosition = targetPosition + configuration.CameraOffset;
            ApplyPose();
        }
        private void ApplyPose()
        {
            if (viewCamera) viewCamera.fieldOfView = configuration.FieldOfView - zoomModifier;
            // FOV is a bounded modifier of the authored baseline.
            var look = new Vector3(basePosition.x, targetPosition.y, targetPosition.z + configuration.LookAhead);
            transform.SetPositionAndRotation(basePosition + impulseOffset, Quaternion.LookRotation(look-basePosition, Vector3.up));
        }
        public void ShiftOrigin(double newOffset, double delta)
        {
            var shift = Vector3.back * (float)delta;
            basePosition += shift; targetPosition += shift; transform.position += shift;
            // impulseSeed/Offset are displacement vectors. There is no velocity or spring cache.
        }
    }
}
