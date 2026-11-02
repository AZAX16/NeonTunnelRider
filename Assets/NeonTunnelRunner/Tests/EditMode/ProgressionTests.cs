using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
namespace NeonTunnelRunner
{
    public sealed class ProgressionTests
    {
        private GameplayConfig config;
        [SetUp] public void Setup() => config=ScriptableObject.CreateInstance<GameplayConfig>();
        [TearDown] public void Cleanup() => Object.DestroyImmediate(config);
        [TestCase(0d,0d,0f)] [TestCase(90d,0d,.5f)] [TestCase(0d,3000d,.5f)]
        [TestCase(180d,0d,1f)] [TestCase(0d,6000d,1f)] [TestCase(1e100d,1e100d,1f)]
        public void IndependentInputsAndCaps(double time,double distance,float expected)
        {
            float d=ProgressionSystem.EvaluateDifficulty(time,distance,config);
            Assert.That(d,Is.EqualTo(expected));
            Assert.That(Mathf.Lerp(config.StartingSpeed,config.MaximumSpeed,d),Is.EqualTo(22f+28f*expected));
            Assert.That(ProgressionSystem.Occupancy(d,config),Is.EqualTo(.25f+.55f*expected).Within(1e-6));
        }
        [Test] public void BothInputsAreMonotonicAndUseMaximum()
        {
            float previous=0;
            for (int i=0;i<1000;i++)
            {
                float d=ProgressionSystem.EvaluateDifficulty(i*.3d,i*7d,config);
                Assert.That(d,Is.GreaterThanOrEqualTo(previous).And.InRange(0f,1f)); previous=d;
            }
            Assert.That(ProgressionSystem.EvaluateDifficulty(90d,600d,config),Is.EqualTo(.5f));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ProgressionSystem.EvaluateDifficulty(double.NaN,0,config));
        }
        [Test] public void SeededDensityMeasuresProposalsSeparatelyFromSafetyAtStartMiddleAndCap()
        {
            var go=new GameObject("DensityFixture"); var planner=go.AddComponent<SpawnDirector>();
            var world=ScriptableObject.CreateInstance<WorldConfig>();
            Set(planner,"configuration",world); Set(planner,"gameplay",config);
            try
            {
                double previous=0;
                foreach(float d in new[] {0f,.5f,1f})
                {
                    Set(config,"startingOccupancy",.25f+.55f*d); Set(config,"maximumOccupancy",.25f+.55f*d);
                    long eligible=0,proposed=0,accepted=0,moving=0,rejected=0,fallbacks=0;
                    for(int seed=0;seed<1000;seed++)
                    {
                        planner.ResetRun(seed);
                        for(long block=0;block<21;block++)
                        {
                            var plan=planner.PlanAt(block);
                            Assert.That(PatternValidator.Validate(plan,world,config,out _),Is.True);
                            for(int row=0;row<6;row++) for(int lane=-1;lane<=1;lane++)
                            {
                                if(lane==plan.ProtectedLane || plan.RowPosition(row)<180d) continue;
                                eligible++;
                                if(!plan.TryGetCandidate(row,lane,out var c)) continue;
                                proposed++;
                                if(!planner.TrySelectTraffic(c,Math.Max(0,c.AbsolutePosition-world.Ahead),out var kind,out var reason)) { rejected++; continue; }
                                accepted++; if(kind==PoolKey.Moving) moving++; if(reason!=PatternRejection.None) fallbacks++;
                                Assert.That(PatternValidator.ValidateMovingEnvelope(planner,lane,c.AbsolutePosition,Math.Max(0,c.AbsolutePosition-world.Ahead),kind==PoolKey.Moving?8f:0f,new Vector3(1.1f,.6f,2.2f),out _),Is.True);
                            }
                            for(int f=0;f<3;f++) Assert.That(PatternValidator.ValidatePickup(planner,plan.GetFuel(f),out _),Is.True);
                            Assert.That(plan.GetFuel(1).AbsolutePosition-plan.GetFuel(0).AbsolutePosition,Is.EqualTo(180d));
                        }
                    }
                    double rate=(double)proposed/eligible;
                    // 246,000 independently addressed eligible slots per level; .006 is >6 binomial SE.
                    Assert.That(rate,Is.EqualTo(.25d+.55d*d).Within(.006d));
                    Assert.That(rate,Is.GreaterThan(previous)); previous=rate;
                    Assert.That(moving,Is.GreaterThan(1000)); Assert.That(accepted+rejected,Is.EqualTo(proposed));
                    Debug.Log($"[Task09 density] d={d}, eligible={eligible}, proposed={proposed}, accepted={accepted}, rejected={rejected}, moving={moving}, fallbacks={fallbacks}, poolMisses=NA(pure planner), rate={rate}");
                }
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(world); }
        }
        private static void Set(object o,string f,object v)=>o.GetType().GetField(f,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
    }
}
