using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Value state driven only by GameManager's real Playing clock; no coroutine or global time writes.</summary>
    public sealed class NearMissSequence
    {
        public bool Active { get; private set; }
        public float Age { get; private set; }
        public float Duration { get; private set; }
        public float ApproachEnd => Duration*(2.5f/6f);
        public float PassEnd => Duration*(3.5f/6f);
        public float PassDuration => Duration/6f;
        public float Cooldown { get; private set; }
        public float Weight { get; private set; }
        public ulong Accepted { get; private set; }
        public bool Begin(float duration, GameplayConfig config)
        {
            if (Active || Cooldown > 0f) return false;
            Active=true; Age=0f; Duration=duration; Weight=0f;
            Cooldown=config.NearMissCooldown; Accepted++; return true;
        }
        public void Advance(float realPlayingSeconds, GameplayConfig config)
        {
            if (!float.IsFinite(realPlayingSeconds) || realPlayingSeconds <= 0f) return;
            Cooldown=Mathf.Max(0f,Cooldown-realPlayingSeconds);
            if (!Active) return;
            Age=Mathf.Min(Duration,Age+realPlayingSeconds);
            float entry=Mathf.Min(config.NearMissEntry,Duration*.5f);
            float exit=Mathf.Min(config.NearMissExit,Duration*.5f);
            Weight=Mathf.SmoothStep(0f,1f,Mathf.Min(Age/entry,(Duration-Age)/exit));
            if (Age >= Duration) { Active=false; Weight=0f; }
        }
        public void Cancel() { Active=false; Weight=0f; Age=0f; }
        public void Reset() { Cancel(); Cooldown=0f; Duration=0f; Accepted=0; }
    }
}
