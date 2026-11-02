using UnityEngine;
using UnityEngine.Audio;

namespace NeonTunnelRunner
{
    /// <summary>Immutable authoring data; playback state belongs to AudioController.</summary>
    [CreateAssetMenu(menuName = "Neon Tunnel Runner/Audio Config")]
    public sealed class AudioConfig : ScriptableObject
    {
        [SerializeField] private AudioClip music, engine, collision, coin, fuel;
        [SerializeField] private AudioMixerGroup musicGroup, engineGroup, sfxGroup;
        [SerializeField, Range(0, 1)] private float musicLevel = .24f;
        [SerializeField, Range(0, 1)] private float menuMusicLevel = .16f;
        [SerializeField, Range(0, 1)] private float pausedMusicLevel = .08f;
        [SerializeField, Range(0, 1)] private float gameOverMusicLevel = .14f;
        [SerializeField, Range(0, 1)] private float engineLowLevel = .05f, engineHighLevel = .075f;
        [SerializeField, Range(0, 1)] private float collisionLevel = .14f, coinLevel = .14f, fuelLevel = .12f;
        [SerializeField, Min(1)] private float maximumSpeed = 50f;
        public AudioClip Music => music;
        public AudioClip Engine => engine;
        public AudioClip Collision => collision;
        public AudioClip Coin => coin;
        public AudioClip Fuel => fuel;
        public AudioMixerGroup MusicGroup => musicGroup;
        public AudioMixerGroup EngineGroup => engineGroup;
        public AudioMixerGroup SfxGroup => sfxGroup;
        public float MusicLevel(GameState state) => state == GameState.Playing ? musicLevel :
            state == GameState.Paused ? pausedMusicLevel : state == GameState.GameOver ? gameOverMusicLevel : menuMusicLevel;
        public float EnginePitch(float actualSpeed) => Mathf.Lerp(.8f, 1.5f, SpeedFraction(actualSpeed));
        public float EngineLevel(float actualSpeed) => Mathf.Lerp(engineLowLevel, engineHighLevel, SpeedFraction(actualSpeed));
        private float SpeedFraction(float speed) => Mathf.Clamp01(float.IsFinite(speed) ? speed / maximumSpeed : 0);
        internal AudioClip Clip(AudioCue cue) => cue == AudioCue.Collision ? collision : cue == AudioCue.Coin ? coin : fuel;
        internal float Level(AudioCue cue) => cue == AudioCue.Collision ? collisionLevel : cue == AudioCue.Coin ? coinLevel : fuelLevel;
        public bool TryValidate(out string error)
        {
            error = null;
            var clips = new[] { music, engine, collision, coin, fuel };
            var names = new[] { "Music", "Engine", "Collision", "Coin", "Fuel" };
            for (int i = 0; i < clips.Length; i++)
                if (!clips[i] || clips[i].samples <= 0 || clips[i].frequency <= 0 || clips[i].length <= 0 ||
                    clips[i].loadState == AudioDataLoadState.Failed)
                { error = "AudioConfig: assign an imported, playable " + names[i] + " clip in Configs/Audio.asset."; return false; }
            if (engine.channels != 1)
            { error = "AudioConfig: Engine must be a mono loop."; return false; }
            if (!musicGroup || !engineGroup || !sfxGroup || musicGroup == engineGroup || musicGroup == sfxGroup || engineGroup == sfxGroup ||
                musicGroup.audioMixer != engineGroup.audioMixer || musicGroup.audioMixer != sfxGroup.audioMixer)
            { error = "AudioConfig: assign distinct Music, Engine and SFX groups from RunnerMixer."; return false; }
            if (!float.IsFinite(maximumSpeed) || maximumSpeed <= 0)
            { error = "AudioConfig: maximumSpeed must be finite and positive."; return false; }
            foreach (float level in new[] { musicLevel, menuMusicLevel, pausedMusicLevel, gameOverMusicLevel, engineLowLevel,
                engineHighLevel, collisionLevel, coinLevel, fuelLevel })
                if (!float.IsFinite(level) || level < 0 || level > 1)
                { error = "AudioConfig: all source levels must be finite in 0..1."; return false; }
            return true;
        }
    }
}
