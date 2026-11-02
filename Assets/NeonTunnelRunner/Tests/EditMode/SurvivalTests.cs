using System;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class SurvivalTests
    {
        private GameplayConfig config;
        private RunSession session;
        [SetUp] public void Setup() { config=ScriptableObject.CreateInstance<GameplayConfig>(); session=new RunSession(config); session.Reset(12345,700); }
        [TearDown] public void Cleanup() => Object.DestroyImmediate(config);

        [TestCase(.01f)] [TestCase(.02f)] [TestCase(.1f)] [TestCase(.5f)]
        public void DoubleFuelAccumulatorReachesZeroAtFiftySecondsAndNeverEarlier(float dt)
        {
            int ticks=(int)Math.Round(50d/dt);
            for (int i=0;i<ticks;i++)
            {
                Assert.That(session.Snapshot.Fuel,Is.GreaterThan(0f),"No premature depletion.");
                session.RecordMovement(dt,0d); session.DrainFuel(dt);
                Assert.That(session.Snapshot.Fuel,Is.InRange(0f,100f));
            }
            Assert.That(session.Snapshot.ElapsedTime,Is.EqualTo(50d).Within(.000005d));
            Assert.That(session.Snapshot.Fuel,Is.Zero);
            session.DrainFuel(dt); Assert.That(session.Snapshot.Fuel,Is.Zero);
        }
        [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
        public void InvalidDrainRejectsWithoutPartialMutation(float dt)
        {
            var before=session.Snapshot;
            Assert.Throws<ArgumentOutOfRangeException>(()=>session.DrainFuel(dt));
            Assert.That(session.Snapshot,Is.EqualTo(before));
        }
        [Test] public void ZeroDrainIsAnExactNoOp()
        { var before=session.Snapshot; session.DrainFuel(0f); Assert.That(session.Snapshot,Is.EqualTo(before)); }
        [TestCase(95f,30f,100f)] [TestCase(100f,30f,100f)] [TestCase(10f,30f,40f)]
        public void RestorationClampsTheAuthoritativeResource(float start,float added,float expected)
        { session.SetResources(3,start); session.RestoreFuel(added); Assert.That(session.Snapshot.Fuel,Is.EqualTo(expected)); }
        [Test] public void LifeChangesDoNotRoundFuelBackThroughTheFloatSnapshot()
        {
            for (int i=0;i<2500;i++) { session.DrainFuel(.02f); if (i==100 || i==1000) session.RemoveLife(); }
            Assert.That(session.Snapshot.Fuel,Is.Zero); Assert.That(session.Snapshot.Lives,Is.EqualTo(1));
            Assert.That(config.FuelDrainPerSecond,Is.EqualTo(2f)); Assert.That(config.FuelCapacity,Is.EqualTo(100f));
            session.Reset(12345,700); Assert.That(session.Snapshot.Fuel,Is.EqualTo(100f));
        }
    }
}
