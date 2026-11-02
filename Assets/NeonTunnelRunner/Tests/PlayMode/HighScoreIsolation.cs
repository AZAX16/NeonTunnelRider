using NUnit.Framework;
namespace NeonTunnelRunner
{
    [SetUpFixture]
    public sealed class PlayModeHighScoreIsolation
    {
        [OneTimeSetUp] public void Begin() => GameManager.HighScoreTestFactory = () => new HighScoreStore("NTR.Tests.Memory", new MemoryPreferences());
        [OneTimeTearDown] public void End() => GameManager.HighScoreTestFactory = null;
        private sealed class MemoryPreferences : IHighScorePreferences
        {
            private int best;
            public int Read(string key) => best;
            public void Write(string key,int value) => best=value;
            public void Save() { }
        }
    }
}
