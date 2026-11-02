using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Owns mesh-only motion and damage color. Never moves the gameplay body.</summary>
    [DisallowMultipleComponent]
    public sealed class VehicleVisuals : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Renderer flashRenderer;
        [SerializeField] private PlayerController player;
        [SerializeField] private PresentationConfig configuration;
        [SerializeField] private Transform visualPivot;
        private Vector3 neutralPosition, neutralScale;
        private Quaternion neutralRotation;
        private bool neutralCached;
        private float bobPhase, roll;
        public Transform VisualPivot => visualPivot;
        public PresentationConfig Configuration => configuration;
        public float Roll => roll;
        public float BobOffset => neutralCached ? visualPivot.localPosition.y-neutralPosition.y : 0f;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.25f;
        [SerializeField] private Color flashColor = new Color(1f, 0.18f, 0.1f, 1f);
        [SerializeField] private float debugFlashRemaining;
        private RunSession session, subscribedSession;
        private MaterialPropertyBlock properties;
        private Color baselineColor;
        private int colorProperty;
        private double flashUntil;
        private bool cached, flashing, prepared;
        public GameManager Owner => gameManager;
        public Renderer FlashRenderer => flashRenderer;
        public bool IsFlashing => flashing;
        public double FlashRemaining => flashing && session != null ? Math.Max(0d, flashUntil-session.Snapshot.ElapsedTime) : 0d;
        public ulong AcceptedFlashes { get; private set; }

        internal void Initialize(RunSession run)
        {
            Unsubscribe();
            ResetVisuals();
            session=run; prepared=false;
            AcceptedFlashes=0;
            CacheRenderer();
            CacheNeutral();
            Subscribe();
        }

        private void CacheRenderer()
        {
            if (cached) return;
            var material=flashRenderer ? flashRenderer.sharedMaterial : null;
            if (!material || (!material.HasProperty("_BaseColor") && !material.HasProperty("_Color")))
                throw new InvalidOperationException("Assign a player visual renderer with a supported base color property.");
            colorProperty=Shader.PropertyToID(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color");
            properties=new MaterialPropertyBlock();
            flashRenderer.GetPropertyBlock(properties);
            baselineColor=properties.HasColor(colorProperty) ? properties.GetColor(colorProperty) : material.GetColor(colorProperty);
            cached=true;
        }

        private void CacheNeutral()
        {
            if (neutralCached) return;
            if (!player || !configuration || !visualPivot || visualPivot.parent != player.transform ||
                visualPivot.GetComponentInChildren<Collider>(true) || !flashRenderer.transform.IsChildOf(visualPivot))
                throw new InvalidOperationException("Assign the collider-free visual pivot, player and presentation config.");
            neutralPosition=visualPivot.localPosition; neutralRotation=visualPivot.localRotation;
            neutralScale=visualPivot.localScale; neutralCached=true;
        }
        private void LateUpdate() => Render(Time.deltaTime);
        internal void Render(float deltaTime)
        {
            if (!isActiveAndEnabled || !neutralCached || session == null || !gameManager || !gameManager.Loop.IsRunning ||
                !float.IsFinite(deltaTime) || deltaTime <= 0f) return;
            // Logical lateral motion supplies direction; no new controls or render-position differencing.
            float direction=Mathf.Sign(player.CurrentPosition.x-player.PreviousPosition.x);
            if (Mathf.Abs(player.CurrentPosition.x-player.PreviousPosition.x)<.00001f) direction=0f;
            float targetRoll=-direction*configuration.MaximumRoll;
            roll=Mathf.Lerp(roll,targetRoll,1f-Mathf.Exp(-deltaTime/Mathf.Max(.001f,configuration.RollTimeConstant)));
            roll=Mathf.Clamp(roll,-configuration.MaximumRoll,configuration.MaximumRoll);
            float speed=Mathf.InverseLerp(0f,gameManager.Configuration.MaximumSpeed,session.Snapshot.ActualSpeed);
            bobPhase=(bobPhase+deltaTime*configuration.BobFrequency*Mathf.Lerp(.8f,1.2f,speed)*Mathf.PI*2f)%(Mathf.PI*2f);
            visualPivot.localPosition=neutralPosition+Vector3.up*(Mathf.Sin(bobPhase)*configuration.BobAmplitude*speed);
            visualPivot.localRotation=neutralRotation*Quaternion.AngleAxis(roll,Vector3.forward);
        }
        private void ResetMotion()
        {
            bobPhase=roll=0f;
            if (!neutralCached || !visualPivot) return;
            visualPivot.localPosition=neutralPosition; visualPivot.localRotation=neutralRotation; visualPivot.localScale=neutralScale;
        }
        private void OnEnable() => Subscribe();
        private void OnDisable() { Unsubscribe(); ResetVisuals(); }
        private void Subscribe()
        {
            if (!prepared || !isActiveAndEnabled || session == null || subscribedSession == session) return;
            Unsubscribe();
            subscribedSession=session;
            subscribedSession.Damaged+=OnDamage;
        }
        private void Unsubscribe()
        {
            if (subscribedSession != null) subscribedSession.Damaged-=OnDamage;
            subscribedSession=null;
        }
        private void OnDamage(DamageEvent damage)
        {
            if (!gameManager || gameManager.State != GameState.Playing || gameManager.IsTransitioning ||
                session == null || damage.RunId != session.Snapshot.RunId) return;
            flashUntil=damage.Encounter.PlayingTime+flashDuration;
            flashing=true;
            AcceptedFlashes++;
            ApplyColor(flashColor);
            debugFlashRemaining=(float)FlashRemaining;
        }
        internal void FinishTick()
        {
            if (!isActiveAndEnabled || !flashing) return;
            debugFlashRemaining=(float)FlashRemaining;
            if (debugFlashRemaining <= 0f) ResetFlash();
        }
        internal void PrepareRun() { prepared=true; ResetVisuals(); Subscribe(); }
        internal void CleanupRun() { prepared=false; Unsubscribe(); ResetVisuals(); }
        public void ResetVisuals() { ResetFlash(); ResetMotion(); }
        private void ResetFlash()
        {
            if (flashing) ApplyColor(baselineColor);
            flashing=false;
            flashUntil=0d;
            debugFlashRemaining=0f;
        }
        private void ApplyColor(Color color)
        {
            if (!cached || !flashRenderer) return;
            // Merge just the owned color; preserve unrelated property changes made since the hit.
            flashRenderer.GetPropertyBlock(properties);
            properties.SetColor(colorProperty,color);
            flashRenderer.SetPropertyBlock(properties);
        }
    }
}
