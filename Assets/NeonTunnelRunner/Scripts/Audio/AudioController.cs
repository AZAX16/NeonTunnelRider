using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    public enum AudioCue { Collision, Coin, Fuel }

    /// <summary>Audio-only observer. Ten authored sources; each voice has one generation-owned clip.</summary>
    [DisallowMultipleComponent]
    public sealed class AudioController : MonoBehaviour
    {
        public const int VoiceCount = 8;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private AudioConfig configuration;
        [SerializeField] private AudioSource musicSource, engineSource;
        [SerializeField] private AudioSource[] voices;
        private struct Playback
        {
            internal bool Busy, Paused;
            internal AudioCue Cue;
            internal ulong Generation, Order;
            internal double Deadline, Remaining;
        }
        private readonly Playback[] playback = new Playback[VoiceCount];
        private RunSession session, subscribed;
        private bool initialized, prepared, musicRunning, engineRunning, enginePaused, focusLost, applicationPaused;
        private ulong generation, order;
        public AudioConfig Configuration => configuration;
        public GameManager Owner => gameManager;
        public AudioSource MusicSource => musicSource;
        public AudioSource EngineSource => engineSource;
        public AudioSource VoiceSource(int index) => voices[index];
        public bool VoiceBusy(int index) => playback[index].Busy;
        public bool VoicePaused(int index) => playback[index].Paused;
        public ulong VoiceGeneration(int index) => playback[index].Generation;
        public AudioCue VoiceCue(int index) => playback[index].Cue;
        public int ActiveVoices { get; private set; }
        public int PeakVoices { get; private set; }
        public ulong Requests { get; private set; }
        public ulong CollisionRequests { get; private set; }
        public ulong CoinRequests { get; private set; }
        public ulong FuelRequests { get; private set; }
        public ulong Dropped { get; private set; }
        public ulong Stolen { get; private set; }
        public ulong MusicStarts { get; private set; }
        public ulong EngineStarts { get; private set; }
        public bool EnginePaused => enginePaused;
        private bool Suspended => focusLost || applicationPaused || (gameManager && gameManager.State == GameState.Paused);

        public bool ValidateRequired(GameplayLoop loop, out string error)
        {
            error = null;
            if (!enabled || !gameObject.activeInHierarchy || !gameManager || !loop || !loop.Player || gameManager != loop.Owner || !configuration)
            { error = "AudioController: assign the enabled observer, GameManager and Configs/Audio.asset."; return false; }
            if (!configuration.TryValidate(out error)) return false;
            if (!musicSource || !engineSource || musicSource == engineSource || voices == null || voices.Length != VoiceCount ||
                !engineSource.transform.IsChildOf(loop.Player.transform))
            { error = "AudioController: assign Music, player-attached Engine and exactly eight distinct authored voices."; return false; }
            for (int i = 0; i < VoiceCount; i++)
            {
                if (!ValidSource(voices[i], configuration.SfxGroup, false) || voices[i] == musicSource || voices[i] == engineSource)
                { error = "AudioController: SFX voice " + i + " needs an enabled 2D source, SFX routing, Doppler 0, Loop/PlayOnAwake off."; return false; }
                for (int j = 0; j < i; j++)
                    if (voices[i] == voices[j]) { error = "AudioController: duplicate SFX source at index " + i + "."; return false; }
            }
            if (!ValidSource(musicSource, configuration.MusicGroup, true) || musicSource.clip != configuration.Music ||
                !ValidSource(engineSource, configuration.EngineGroup, true) || engineSource.clip != configuration.Engine)
            { error = "AudioController: Music/Engine need assigned clips, matching mixer routing, enabled 2D looping sources, Doppler 0 and PlayOnAwake off."; return false; }
            return true;
        }
        private static bool ValidSource(AudioSource source, UnityEngine.Audio.AudioMixerGroup group, bool loop) =>
            source && source.enabled && source.gameObject.activeInHierarchy && source.outputAudioMixerGroup == group && source.loop == loop &&
            !source.playOnAwake && source.spatialBlend == 0 && source.dopplerLevel == 0 && !source.ignoreListenerPause && !source.mute;

        internal void Initialize(RunSession run)
        {
            Shutdown();
            session = run;
            if (!gameManager) { Debug.LogError("AudioController: assign GameManager before initialization.", this); return; }
            if (!ValidateRequired(gameManager.Loop, out string error)) { Debug.LogError(error, this); return; }
            initialized = true;
            focusLost = applicationPaused = false;
            ApplyState(GameState.Menu);
        }
        internal void PrepareRun()
        {
            CleanupRun();
            prepared = initialized;
            Requests = CollisionRequests = CoinRequests = FuelRequests = Dropped = Stolen = 0;
            PeakVoices = 0;
            Subscribe();
        }
        private void Subscribe()
        {
            if (!initialized || !prepared || !isActiveAndEnabled || session == null || subscribed == session) return;
            Unsubscribe(); subscribed = session;
            subscribed.Damaged += Damaged; subscribed.Collected += Collected; subscribed.SnapshotChanged += Snapshot;
        }
        private void Unsubscribe()
        {
            if (subscribed != null)
            { subscribed.Damaged -= Damaged; subscribed.Collected -= Collected; subscribed.SnapshotChanged -= Snapshot; }
            subscribed = null;
        }
        private void OnEnable()
        {
            if (!initialized || !gameManager || !gameManager.IsOwner) return;
            Subscribe(); ApplyState(gameManager.State);
        }
        private void OnDisable() { Unsubscribe(); StopAllPlayback(); }
        private void OnDestroy() => Shutdown();
        internal void Shutdown()
        {
            Unsubscribe(); StopAllPlayback(); initialized = prepared = false; session = null;
        }
        private void Damaged(DamageEvent e) { if (Accept(e.RunId)) Request(AudioCue.Collision); }
        private void Collected(CollectedEvent e)
        { if (Accept(e.RunId) && (e.Kind == PoolKey.Coin || e.Kind == PoolKey.Fuel)) Request(e.Kind == PoolKey.Coin ? AudioCue.Coin : AudioCue.Fuel); }
        private bool Accept(Guid run) => initialized && prepared && isActiveAndEnabled && session != null &&
            run == session.Snapshot.RunId && gameManager.State == GameState.Playing && !gameManager.IsTransitioning;
        private void Snapshot(RunSnapshot snapshot) => RefreshEngine(snapshot.ActualSpeed);

        // Free voice first; a collision may steal the oldest reward, then oldest collision.
        // Rewards never steal. There is no delayed queue or gameplay/VFX dependency.
        internal bool Request(AudioCue cue)
        {
            if (!Accept(session == null ? Guid.Empty : session.Snapshot.RunId) || Suspended) return false;
            Requests++;
            if (cue == AudioCue.Collision) CollisionRequests++; else if (cue == AudioCue.Coin) CoinRequests++; else FuelRequests++;
            RefreshPlayback();
            int index = -1;
            for (int i = 0; i < VoiceCount; i++) if (!playback[i].Busy) { index = i; break; }
            if (index < 0 && cue == AudioCue.Collision)
            {
                for (int pass = 0; pass < 2 && index < 0; pass++)
                    for (int i = 0; i < VoiceCount; i++)
                        if ((pass == 1 || playback[i].Cue != AudioCue.Collision) &&
                            (index < 0 || playback[i].Order < playback[index].Order)) index = i;
                Release(index); Stolen++;
            }
            if (index < 0) { Dropped++; return false; }
            var clip = configuration.Clip(cue);
            var source = voices[index];
            source.clip = clip; source.pitch = 1; source.volume = configuration.Level(cue);
            playback[index] = new Playback { Busy = true, Cue = cue, Generation = ++generation, Order = ++order,
                Remaining = clip.length, Deadline = AudioSettings.dspTime + clip.length + .02d };
            ActiveVoices++; PeakVoices = Math.Max(PeakVoices, ActiveVoices);
            source.Play(); return true;
        }
        private void Update() { if (initialized) RefreshPlayback(); }
        internal void RefreshPlayback()
        {
            double now = AudioSettings.dspTime;
            for (int i = 0; i < VoiceCount; i++) TryCompleteVoice(i, playback[i].Generation, now);
        }
        internal bool TryCompleteVoice(int index, ulong token, double dspTime)
        {
            var p = playback[index];
            if (!p.Busy || p.Paused || p.Generation != token || dspTime < p.Deadline) return false;
            Release(index); return true;
        }
        private void Release(int index)
        {
            if (voices != null && index < voices.Length && voices[index])
            { voices[index].Stop(); voices[index].clip = null; voices[index].volume = 0; voices[index].pitch = 1; }
            if (playback[index].Busy) ActiveVoices--;
            playback[index] = default;
        }
        public void ResetVoices() { for (int i = 0; i < VoiceCount; i++) Release(i); }
        private void PauseVoices(bool pause)
        {
            double now = AudioSettings.dspTime;
            for (int i = 0; i < VoiceCount; i++)
            {
                var p = playback[i]; if (!p.Busy || p.Paused == pause) continue;
                if (pause) { p.Remaining = Math.Max(0, p.Deadline - now); voices[i].Pause(); }
                else { p.Deadline = now + p.Remaining; voices[i].UnPause(); }
                p.Paused = pause; playback[i] = p;
            }
        }
        private void RefreshEngine(float actualSpeed)
        {
            if (!initialized || !engineSource) return;
            engineSource.pitch = configuration.EnginePitch(actualSpeed);
            engineSource.volume = configuration.EngineLevel(actualSpeed);
        }
        internal void ApplyState(GameState state)
        {
            if (!initialized || !isActiveAndEnabled) return;
            if (!musicRunning && !focusLost && !applicationPaused)
            { musicSource.Play(); musicRunning = true; MusicStarts++; }
            musicSource.volume = configuration.MusicLevel(state);
            if (state == GameState.Menu) CleanupRun();
            if (state == GameState.Menu || state == GameState.GameOver)
            { engineSource.Stop(); engineRunning = enginePaused = false; }
            else if (state == GameState.Playing && prepared && !engineRunning)
            { engineSource.Play(); engineRunning = true; enginePaused = false; EngineStarts++; }
            if (engineRunning)
            {
                if (Suspended && !enginePaused) { engineSource.Pause(); enginePaused = true; }
                else if (!Suspended && enginePaused) { engineSource.UnPause(); enginePaused = false; }
            }
            RefreshEngine(session == null ? 0 : session.Snapshot.ActualSpeed);
            PauseVoices(Suspended);
        }
        internal void CleanupRun()
        {
            prepared = false; Unsubscribe(); ResetVoices();
            if (engineSource) engineSource.Stop(); engineRunning = enginePaused = false;
        }
        private void StopAllPlayback()
        { ResetVoices(); if (musicSource) musicSource.Stop(); if (engineSource) engineSource.Stop(); musicRunning = engineRunning = enginePaused = false; }
        private void OnApplicationFocus(bool focused) => SetApplicationSuspension(!focused, applicationPaused);
        private void OnApplicationPause(bool paused) => SetApplicationSuspension(focusLost, paused);
        internal void SetApplicationSuspension(bool lostFocus, bool paused)
        {
            bool wasSuspended = focusLost || applicationPaused;
            focusLost = lostFocus; applicationPaused = paused;
            if (!initialized || !isActiveAndEnabled) return;
            bool suspended = focusLost || applicationPaused;
            if (suspended && !wasSuspended) musicSource.Pause();
            else if (!suspended && wasSuspended) musicSource.UnPause();
            // Restores audio for the current GM state; never commands Resume or changes simulation.
            ApplyState(gameManager.State);
        }
    }
}
