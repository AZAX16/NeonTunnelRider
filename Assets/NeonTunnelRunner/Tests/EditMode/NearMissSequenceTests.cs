using NUnit.Framework;
using UnityEngine;
namespace NeonTunnelRunner
{
    public sealed class NearMissSequenceTests
    {
        [TestCase(.01666667f)] [TestCase(.03333333f)] [TestCase(.00694444f)]
        public void SixRealPlayingSecondsIncludesSmoothEntryExitAndNoExtension(float dt)
        {
            var config=ScriptableObject.CreateInstance<GameplayConfig>();
            try {
                var s=new NearMissSequence(); Assert.That(s.Begin(6,config),Is.True);
                for(float t=0;t<5.9f;t+=dt) { s.Advance(dt,config); Assert.That(s.Begin(6,config),Is.False); }
                Assert.That(s.Active,Is.True); s.Advance(.1f+dt,config); Assert.That(s.Active,Is.False);
                Assert.That(s.Weight,Is.Zero); Assert.That(s.Cooldown,Is.GreaterThan(5));
                s.Advance(6,config); Assert.That(s.Begin(6,config),Is.True);
            } finally { Object.DestroyImmediate(config); }
        }
        [TestCase(.075f)] [TestCase(2f)] [TestCase(5.8f)]
        public void PausedClockRetainsEveryPhaseExactly(float age)
        {
            var config=ScriptableObject.CreateInstance<GameplayConfig>();
            try { var s=new NearMissSequence(); s.Begin(6,config); s.Advance(age,config);
                float w=s.Weight, cooldown=s.Cooldown; for(int i=0;i<100;i++)s.Advance(0,config);
                Assert.That(s.Age,Is.EqualTo(age)); Assert.That(s.Weight,Is.EqualTo(w)); Assert.That(s.Cooldown,Is.EqualTo(cooldown));
            } finally { Object.DestroyImmediate(config); }
        }
    }
}
