using System;
using TMPro;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Read-only projection. Cached integers are display keys, never run state.</summary>
    [DisallowMultipleComponent]
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text fuelText;
        [SerializeField] private TMP_Text fuelLabel;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private UnityEngine.UI.Image fuelFill;
        [SerializeField] private UnityEngine.UI.Image[] lifeIcons;
        [SerializeField] private CanvasGroup[] rearHazards;
        private RunSession session;
        private float warningFlashAge;
        private void LateUpdate()
        {
            if(!gameManager || rearHazards==null)return;
            bool playing=gameManager.State==GameState.Playing;
            if(playing)warningFlashAge+=Time.unscaledDeltaTime;
            for(int i=0;i<rearHazards.Length;i++)
            {
                var indicator=rearHazards[i];if(!indicator)continue;
                bool pending=gameManager.Loop.Planner.RearWarningRemaining(i-1)>0f;
                float alpha=playing && pending ? (.3f+.7f*(.5f+.5f*Mathf.Sin(warningFlashAge*22f))) : 0f;
                indicator.alpha=alpha;
            }
        }
        private int speed = -1, percent = -1, lives = -1, score = -1, best = -1;
        private bool lowFuel;
        private float fill = -1f;
        private readonly char[] scoreDigits = new char[11];
        private readonly char[] bestDigits = new char[11];
        private static readonly Color Live = new Color32(242, 245, 255, 255);
        private static readonly Color Lost = new Color32(75, 83, 116, 255);
        public GameManager Owner => gameManager;

        public bool ValidateRequired(out string error)
        {
            bool valid = gameManager && hudPanel && speedText && fuelText && fuelLabel && livesText &&
                scoreText && bestText && fuelFill && lifeIcons != null && lifeIcons.Length == 3 &&
                Array.TrueForAll(lifeIcons, icon => icon) && gameManager.Configuration &&
                float.IsFinite(gameManager.Configuration.FuelCapacity) && gameManager.Configuration.FuelCapacity > 0f;
            error = valid ? null : "Assign HUD owner, five TMP fields, fuel label/fill, three life icons and a positive fuel capacity.";
            return valid;
        }

        internal void Initialize(GameManager owner, RunSession source)
        {
            Shutdown();
            if (!isActiveAndEnabled) return;
            if (owner != gameManager || source == null || !ValidateRequired(out _))
            { Debug.LogError("[HudPresenter] Invalid HUD references or fuel capacity; run Validate Project Setup.", this); return; }
            session = source;
            session.SnapshotChanged += Render;
            owner.StateChanged += OnStateChanged;
            speed = percent = lives = score = best = -1;
            fill = -1f;
            fuelLabel.text = "FUEL";
            lowFuel = false;
            OnStateChanged(owner.State, owner.State);
        }

        private void OnEnable()
        {
            if (gameManager && gameManager.IsOwner) Initialize(gameManager, gameManager.Session);
        }
        private void OnDisable() => Shutdown();
        internal void Shutdown()
        {
            if (session != null) session.SnapshotChanged -= Render;
            if (gameManager) gameManager.StateChanged -= OnStateChanged;
            session = null; warningFlashAge=0f;
            if(rearHazards!=null)for(int i=0;i<rearHazards.Length;i++)if(rearHazards[i])rearHazards[i].alpha=0f;
        }
        private void OnStateChanged(GameState previous, GameState next)
        {
            hudPanel.SetActive(next != GameState.Menu);
            Render(session.Snapshot); // Includes the first visible frame and frozen actual speed.
        }
        private void Render(RunSnapshot snapshot)
        {
            int nextSpeed = Mathf.FloorToInt(snapshot.ActualSpeed * 3.6f + .5f);
            float normalized = Mathf.Clamp01(snapshot.Fuel / gameManager.Configuration.FuelCapacity);
            int nextPercent = Mathf.FloorToInt(normalized * 100f + .5f);
            if (speed != nextSpeed) { speed = nextSpeed; speedText.SetText("{0} km/h", speed); }
            if (percent != nextPercent) { percent = nextPercent; fuelText.SetText("{0}%", percent); }
            if (fill != normalized) { fill = normalized; fuelFill.fillAmount = fill; }
            bool nextLow = normalized < .25f;
            if (lowFuel != nextLow) { lowFuel = nextLow; fuelLabel.text = lowFuel ? "LOW FUEL" : "FUEL"; }
            if (lives != snapshot.Lives)
            {
                lives = snapshot.Lives;
                livesText.SetText("{0} / 3", lives);
                for (int i = 0; i < lifeIcons.Length; i++) lifeIcons[i].color = i < lives ? Live : Lost;
            }
            // Keep exact 32-bit integers without allocating formatted strings each tick.
            if (score != snapshot.Score) { score = snapshot.Score; SetInteger(scoreText, score, scoreDigits); }
            if (best != snapshot.BestScore) { best = snapshot.BestScore; SetInteger(bestText, best, bestDigits); }
        }

        private static void SetInteger(TMP_Text text, int value, char[] buffer)
        {
            uint magnitude = value < 0 ? (uint)-(long)value : (uint)value;
            int cursor = buffer.Length;
            do
            {
                buffer[--cursor] = (char)('0' + magnitude % 10);
                magnitude /= 10;
            } while (magnitude != 0);
            if (value < 0) buffer[--cursor] = '-';
            text.SetCharArray(buffer, cursor, buffer.Length - cursor);
        }
    }
}
