using System;
using NUnit.Framework;

namespace NeonTunnelRunner
{
    public sealed class PickupScoreTests
    {
        [TestCase(0d,0,0)] [TestCase(1.999d,0,1)] [TestCase(1234.75d,3,1534)]
        [TestCase(2147483646.9d,0,2147483646)] [TestCase(2147483600d,1,int.MaxValue)]
        [TestCase(1e100d,1,int.MaxValue)] [TestCase(0d,int.MaxValue,int.MaxValue)]
        public void ScoreUsesDoubleDistancePlusCoinsWithSafeSaturation(double distance,int coins,int expected)
        { Assert.That(ProgressionSystem.ComputeScore(distance,coins,100),Is.EqualTo(expected)); }
        [Test] public void ConfiguredRewardsAndInvalidArgumentsAreHandled()
        {
            Assert.That(ProgressionSystem.ComputeScore(5.5d,2,75),Is.EqualTo(155));
            Assert.That(ProgressionSystem.ComputeScore(0d,int.MaxValue,int.MaxValue),Is.EqualTo(int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ProgressionSystem.ComputeScore(double.NaN,0,100));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ProgressionSystem.ComputeScore(-1d,0,100));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ProgressionSystem.ComputeScore(0d,-1,100));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ProgressionSystem.ComputeScore(0d,1,-1));
        }
    }
}
