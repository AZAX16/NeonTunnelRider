using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Damage feedback only. The gameplay tick owns time; this never moves the body.</summary>
    [DisallowMultipleComponent]
    public sealed class VehicleVisuals : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Renderer flashRenderer;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.25f;
        [SerializeField] private Color flashColor = new Color(1f, 0.18f, 0.1f, 1f);
        [SerializeField] private float debugFlashRemaining;
        private RunSession session, subscribedSession;
        private MaterialPropertyBlock properties;
        private Color baselineColor;
        private int colorProperty;
        private double flashUntil;
        private bool cached, flashing;
        public GameManager Owner => gameManager;
        public Renderer FlashRenderer => flashRenderer;
        public bool IsFlashing => flashing;
        public double FlashRemaining => flashing && session != null ? Math.Max(0d, flashUntil-session.Snapshot.ElapsedTime) : 0d;
        public ulong AcceptedFlashes { get; private set; }

        internal void Initialize(RunSession run)
        {
            Unsubscribe();
            ResetVisuals();
            session=run;
            AcceptedFlashes=0;
            CacheRenderer();
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

        private void OnEnable() => Subscribe();
        private void OnDisable() { Unsubscribe(); ResetVisuals(); }
        private void Subscribe()
        {
            if (!isActiveAndEnabled || session == null || subscribedSession == session) return;
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
            if (debugFlashRemaining <= 0f) ResetVisuals();
        }
        public void ResetVisuals()
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
