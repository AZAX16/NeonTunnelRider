using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    internal interface IHighScorePreferences
    {
        int Read(string key);
        void Write(string key, int value);
        void Save();
    }

    /// <summary>One loaded best and a pending candidate. Only Flush touches disk.</summary>
    public sealed class HighScoreStore
    {
        public const string ProductionKey = "NTR.HighScore.v1";
        private readonly string key;
        private readonly IHighScorePreferences preferences;
        private bool loaded;
        private int best;
        public bool IsDirty { get; private set; }
        public int Best => Load();

        public HighScoreStore(string key = ProductionKey) : this(key, new PlayerPreferences()) { }
        internal HighScoreStore(string key, IHighScorePreferences preferences)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A preference key is required.", nameof(key));
            this.key = key;
            this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        }
        public int Load()
        {
            if (!loaded) { best = Math.Max(0, preferences.Read(key)); loaded = true; }
            return best;
        }
        public bool SetIfHigher(int score)
        {
            if (score <= Load()) return false;
            best = score;
            IsDirty = true;
            return true;
        }
        public bool Flush()
        {
            if (!IsDirty) return false;
            // Retain a higher value written by another owner of the same key.
            best = Math.Max(best, Math.Max(0, preferences.Read(key)));
            preferences.Write(key, best);
            preferences.Save();
            IsDirty = false;
            return true;
        }
        private sealed class PlayerPreferences : IHighScorePreferences
        {
            public int Read(string key) => PlayerPrefs.GetInt(key, 0);
            public void Write(string key, int value) => PlayerPrefs.SetInt(key, value);
            public void Save() => PlayerPrefs.Save();
        }
    }
}
