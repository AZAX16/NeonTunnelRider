using NUnit.Framework;
using UnityEngine;

namespace NeonTunnelRunner
{
    public sealed class SweptBoundsTests
    {
        private static readonly Vector3 Player=new Vector3(0.85f,0.45f,1.8f);
        private static readonly Vector3 Car=new Vector3(1.1f,0.6f,2.2f);
        [TestCase(22f,0f)] [TestCase(50f,0f)] [TestCase(22f,8f)] [TestCase(50f,8f)]
        public void CrossingWithDisjointEndpointsReturnsRelativeTimeOfImpact(float playerSpeed,float trafficSpeed)
        {
            var b0=new Vector3(0,0,10f); var a1=Vector3.forward*playerSpeed*2f; var b1=b0+Vector3.forward*trafficSpeed*2f;
            Assert.That(SweptBounds.TryHit(Vector3.zero,a1,Player,b0,b1,Car,out float toi),Is.True);
            Assert.That(toi,Is.EqualTo(6f/(2f*(playerSpeed-trafficSpeed))).Within(0.000001f));
            Assert.That(Mathf.Abs((a1-b1).z),Is.GreaterThan(4f));
        }
        [TestCase(0f,true)] [TestCase(1.95f,true)] [TestCase(1.9501f,false)] [TestCase(3.2f,false)]
        public void ParallelAxesIncludeExactGrazing(float x,bool expected)
        {
            Assert.That(SweptBounds.TryHit(Vector3.zero,new Vector3(0,0,50),Player,new Vector3(x,0,10),new Vector3(x,0,18),Car,out _),Is.EqualTo(expected));
        }
        [Test] public void InitialOverlapAndEqualMotionHaveTimeZero()
        { Assert.That(SweptBounds.TryHit(Vector3.zero,Vector3.forward,Player,Vector3.one,Vector3.one+Vector3.forward,Car,out float t),Is.True); Assert.That(t,Is.Zero); }
        [Test] public void NearZeroRelativeAxisOutsideSlabMisses()
        { Assert.That(SweptBounds.TryHit(Vector3.zero,new Vector3(1e-12f,0,50),Player,new Vector3(3,0,10),new Vector3(3,0,18),Car,out _),Is.False); }
        [Test] public void ThinBoxAndLateralCrossingDoNotNeedEndpointOverlap()
        { Assert.That(SweptBounds.TryHit(new Vector3(-4,0,0),new Vector3(4,0,0),Vector3.one*0.01f,Vector3.zero,Vector3.zero,Vector3.one*0.01f,out float t),Is.True); Assert.That(t,Is.EqualTo(0.4975f).Within(0.000001f)); }
        [Test] public void TargetMovingAwayFasterMisses()
        { Assert.That(SweptBounds.TryHit(Vector3.zero,Vector3.forward*22,Player,Vector3.forward*10,Vector3.forward*60,Car,out _),Is.False); }
        [Test] public void NegativeOrNonFiniteBoundsFailVisibly()
        { Assert.Throws<System.ArgumentException>(()=>SweptBounds.TryHit(Vector3.zero,Vector3.zero,-Vector3.one,Vector3.zero,Vector3.zero,Car,out _)); }
    }
}
