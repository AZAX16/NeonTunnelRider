using System;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace NeonTunnelRunner
{
    public sealed class TrafficExpansionTests
    {
        GameObject root; GameplayConfig config; GameManager gm; GameplayLoop loop;
        [SetUp] public void Setup()
        {
            config=ScriptableObject.CreateInstance<GameplayConfig>();
            root=PlayModeMovementRig.Create("Task15B",config); gm=root.GetComponent<GameManager>(); loop=gm.Loop;
            Assert.That(gm.StartRun(),Is.True); loop.Planner.ReleaseAll();
        }
        [TearDown] public void Cleanup() { if(root) Object.DestroyImmediate(root); if(config) Object.DestroyImmediate(config); }
        Interactable Rent(double z,int lane,float speed=0f)
        {
            var key=speed>0 ? PoolKey.Moving : PoolKey.Stationary;
            Assert.That(gm.Pools.TryRent(new SpawnContext(key,new Vector3(lane*3.2f,.6f,(float)z),Quaternion.identity,
                loop.Planner.OriginRoot,lane,z,12345,speed,900000+loop.Planner.ActiveCount,loop.Planner),out var item),Is.True);
            return (Interactable)item;
        }
        // Drive precisely the same predict/contact/feedback phases as the loop without adding generation.
        void Cross(float dt,float speed)
        {
            loop.Player.PredictStep(dt,speed); loop.Planner.PredictTraffic(dt,0d);
            loop.Sensor.CollectContacts(); loop.Sensor.DispatchContacts(gm.Session,dt);
            loop.Sensor.ResolveNearMisses(gm,true);
        }
        [TestCase(1)] [TestCase(-1)] [TestCase(0)]
        public void CrossingWithoutTheBehindHoldNeverStartsFeedbackAndContactsStillResolve(int lane)
        {
            var car=Rent(10,lane);Cross(1f,50f);
            Assert.That(gm.NearMiss.Active,Is.False);
            Assert.That(car.Encountered,Is.EqualTo(lane==0));
            Assert.That(car.PassConsumed,Is.True);
        }
        [Test] public void FasterRearCarPassingThePlayerDoesNotTriggerNearMissFeedback()
        {
            var car=Rent(-10,1,62f);Cross(1.5f,50f);
            Assert.That(gm.NearMiss.Active,Is.False);Assert.That(car.Encountered,Is.False);
            Assert.That(car.PassConsumed,Is.True);
        }
        void AdvanceAttempt(float seconds,bool dodge=false,int direction=1)
        {
            int steps=(int)Math.Ceiling(seconds/.01f);
            for(int i=0;i<steps;i++)
            {
                gm.AdvancePresentation(.01f);
                if(dodge && gm.NearMiss.Active && gm.NearMiss.Age>=gm.NearMiss.ApproachEnd && !loop.Player.IsTransitioning && loop.Player.LaneIndex==0)
                    loop.Player.RequestLane(direction);
                Assert.That(loop.Step(.01f*Time.timeScale),Is.True);
            }
        }
        [TestCase(-1,30d)] [TestCase(1,30d)] [TestCase(-1,0d)] [TestCase(1,0d)]
        public void BeingBesideTheCarCannotArmTheApproach(int lane,double z)
        {
            Rent(z,lane);AdvanceAttempt(3f);
            Assert.That(gm.NearMiss.Accepted,Is.Zero);Assert.That(gm.NearMiss.Active,Is.False);
        }
        [TestCase(.1f)] [TestCase(1.2f)] [TestCase(2.48f)]
        public void LeavingTheSameLaneBeforeTwoPointFiveSecondsCancelsImmediately(float age)
        {
            var car=Rent(30,0);loop.Step(.01f);Assert.That(gm.NearMiss.Active,Is.True);
            AdvanceAttempt(age);Assert.That(gm.NearMiss.Active,Is.True);
            loop.Player.RequestLane(-1);loop.Step(.001f*Time.timeScale);
            Assert.That(gm.NearMiss.Active,Is.False);Assert.That(gm.NearMissPassCompleted,Is.False);
            Assert.That(Time.timeScale,Is.EqualTo(1f));Assert.That(car.PassConsumed,Is.True);
        }
        [Test] public void StayingBehindWithoutDoingThePassDoesNotEarnTheAftermath()
        {
            var car=Rent(30,0);loop.Step(.01f);AdvanceAttempt(3.6f);
            Assert.That(gm.NearMiss.Active,Is.False);Assert.That(gm.NearMissPassCompleted,Is.False);
            Assert.That(car.Encountered,Is.True);Assert.That(gm.Session.Snapshot.Lives,Is.EqualTo(2));
        }
        [TestCase(false)] [TestCase(true)]
        public void HittingTheTrackedCarCancelsEvenWhenDamageIsRejectedByGrace(bool grace)
        {
            if(grace)loop.Survival.TryDamage(new Encounter(PoolKey.Stationary,1,1,0,0,Vector3.zero,0));
            int lives=gm.Session.Snapshot.Lives;
            var car=Rent(14,0);loop.Step(.01f);Assert.That(gm.NearMiss.Active,Is.True);
            loop.Step(.7f);
            Assert.That(car.Encountered,Is.True);Assert.That(gm.NearMiss.Active,Is.False);
            Assert.That(gm.NearMissPassCompleted,Is.False);Assert.That(gm.Session.Snapshot.Lives,Is.EqualTo(grace ? lives : lives-1));
        }
        [Test] public void APassWithoutTheHoldCannotBypassTheGateDuringACatchUpTick()
        {
            Rent(30,1);loop.Step(2f);
            Assert.That(gm.NearMiss.Accepted,Is.Zero);Assert.That(gm.NearMiss.Active,Is.False);
        }
        [TestCase(0f,0f,-1)] [TestCase(90f,8f,1)] [TestCase(180f,8f,1)]
        public void OnlyACompletedHoldAndCleanAdjacentPassEarnsTheFullSixSeconds(float progressionAge,float leadSpeed,int dodge)
        {
            if(progressionAge>0)gm.Session.Advance(progressionAge,0d);
            loop.Progression.EvaluateTickStart();
            float relative=gm.Session.Snapshot.CruiseSpeed-leadSpeed;
            float entryGap=4f+relative*(config.LaneTransitionDuration+config.NearMissDodgeReactionTime)+.5f;
            double z=entryGap+relative*(config.NearMissTimeScale*(2.5f-config.NearMissEntry)+config.NearMissEntry*(1f+config.NearMissTimeScale)*.5f);
            var car=Rent(z,0,leadSpeed);loop.Step(.01f);Assert.That(gm.NearMiss.Active,Is.True);
            float passStart=0f;bool started=false;
            for(int i=0;i<601;i++)
            {
                gm.AdvancePresentation(.01f);
                if(!started && gm.NearMiss.Age>=2.5f)
                {started=true;passStart=(float)(car.AbsoluteZ-gm.Session.Snapshot.Distance);loop.Player.RequestLane(dodge);}
                Assert.That(loop.Step(.01f*Time.timeScale),Is.True);
                if(gm.NearMiss.Age>3.51f)Assert.That(gm.NearMissPassCompleted,Is.True);
            }
            Assert.That(started,Is.True);Assert.That(passStart,Is.EqualTo(entryGap).Within(.8f));
            Assert.That(gm.NearMissPassCompleted,Is.True);Assert.That(gm.NearMiss.Active,Is.False);
            Assert.That(gm.NearMiss.Duration,Is.EqualTo(6f));Assert.That(gm.Session.Snapshot.Lives,Is.EqualTo(3));
            Assert.That(car.Encountered,Is.False);
        }
        [Test] public void RearWarningPrecedesRentalByOnePointFiveRealPlayingSecondsAndPauseHoldsIt()
        {
            bool found=false;
            for(int tick=0;tick<600;tick++)
            { SurvivalRoute.Guide(loop);loop.Step(.02f);if(loop.Planner.RearWarningCount>0){found=true;break;} }
            Assert.That(found,Is.True);ulong spawned=loop.Planner.SpawnedOvertakers;
            int lane=-2;for(int i=-1;i<=1;i++)if(loop.Planner.RearWarningRemaining(i)>0)lane=i;
            Assert.That(lane,Is.InRange(-1,1));Assert.That(loop.Planner.RearWarningRemaining(lane),Is.EqualTo(1.5f));
            loop.Planner.AdvanceRearWarnings(1.49f);Assert.That(loop.Planner.SpawnedOvertakers,Is.EqualTo(spawned));
            gm.Pause();float left=loop.Planner.RearWarningRemaining(lane);loop.Planner.AdvanceRearWarnings(10f);
            Assert.That(loop.Planner.RearWarningRemaining(lane),Is.EqualTo(left));gm.Resume();
            loop.Planner.AdvanceRearWarnings(.02f);Assert.That(loop.Planner.SpawnedOvertakers,Is.EqualTo(spawned+1));
            gm.Pause();gm.Restart();Assert.That(loop.Planner.RearWarningCount,Is.Zero);
        }
        [Test] public void TwoLaneSeparationCannotQualify()
        {
            loop.Player.RequestLane(-1); loop.Player.PredictStep(.12f,0); loop.Player.HoldPose();
            var car=Rent(10,1); Cross(1f,50);
            Assert.That(gm.NearMiss.Active,Is.False); Assert.That(car.Encountered,Is.False);
        }
        [TestCase(0f)] [TestCase(2f)]
        public void SpawnInsideRegionAndSteadyParallelCannotBecomeFreshPass(float z)
        {
            Rent(z,1,22f); for(int i=0;i<20;i++) Cross(.1f,22f);
            Assert.That(gm.NearMiss.Accepted,Is.Zero);
        }
        [Test] public void NearestAheadCarWinsAndCooldownConsumesOtherEligibleAttempts()
        {
            gm.Session.Advance(180f,0d);loop.Progression.EvaluateTickStart();
            var a=Rent(50,0);var b=Rent(60,0);var beside=Rent(50,1);loop.Step(.01f);
            Assert.That(gm.NearMiss.Accepted,Is.EqualTo(1));Assert.That(gm.NearMissTraffic.Item,Is.SameAs(a));
            Assert.That(a.PassConsumed && b.PassConsumed,Is.True);Assert.That(beside.PassConsumed,Is.False);
            gm.CancelNearMiss();gm.NearMiss.Advance(6f,config);
            loop.Planner.ReleaseAll();Rent(gm.Session.Snapshot.Distance+50d,0);loop.Step(.01f);
            Assert.That(gm.NearMiss.Accepted,Is.EqualTo(1));
        }
        [Test] public void PooledReuseClearsPassAndMotionStateAndStaleTokenCannotReturn()
        {
            var car=(MovingCar)Rent(-10,1,62); ulong old=car.ActivationId; Cross(1f,50);
            car.ReturnToPool(old); var next=(MovingCar)Rent(70,1,8);
            Assert.That(next,Is.SameAs(car)); Assert.That(next.PassConsumed,Is.False);
            Assert.That(next.IsOvertaker || next.ChangingLane,Is.False); Assert.That(next.Speed,Is.EqualTo(8));
            Assert.That(next.ReturnToPool(old),Is.False);
        }
        [Test] public void GraceRejectedContactCannotEmitNearMiss()
        {
            loop.Survival.TryDamage(new Encounter(PoolKey.Stationary,1,1,0,0,Vector3.zero,0));
            var car=Rent(5,0); Cross(.5f,50); Assert.That(car.Encountered,Is.True);
            Assert.That(gm.NearMiss.Active,Is.False); Assert.That(gm.Session.Snapshot.Lives,Is.EqualTo(2));
        }
        [Test] public void DamageAndTerminalClearTimeAndEffectAcrossFiftyResets()
        {
            float baseline=Time.fixedDeltaTime; ulong created=gm.Pools.TotalCreated;
            for(int i=0;i<50;i++)
            {
                Assert.That(gm.AcceptNearMiss(),Is.True); gm.NearMiss.Advance(.3f,config); gm.ComposeClock();
                Assert.That(Time.timeScale,Is.EqualTo(.45f).Within(.00001));
                Assert.That(Time.fixedDeltaTime,Is.EqualTo(baseline*.45f).Within(.00001));
                gm.Pause(); float age=gm.NearMiss.Age;
                Assert.That(loop.Step(10),Is.False); Assert.That(gm.NearMiss.Age,Is.EqualTo(age)); gm.Resume();
                Assert.That(Time.timeScale,Is.EqualTo(.45f).Within(.00001));
                loop.Survival.TryDamage(new Encounter(PoolKey.Stationary,1,1,0,0,Vector3.zero,gm.Session.Snapshot.ElapsedTime));
                Assert.That(gm.NearMiss.Active,Is.False); Assert.That(Time.timeScale,Is.EqualTo(1));
                gm.EndRun(i%2==0 ? GameOverReason.Lives : GameOverReason.Fuel);
                Assert.That(Time.timeScale,Is.Zero); Assert.That(gm.Restart(),Is.True);
                Assert.That(BitConverter.SingleToInt32Bits(Time.fixedDeltaTime),Is.EqualTo(BitConverter.SingleToInt32Bits(baseline))); Assert.That(gm.NearMiss.Cooldown,Is.Zero);
            }
            Assert.That(gm.Pools.TotalCreated,Is.EqualTo(created));
        }
        [Test] public void OriginalForwardMoverYieldsBeforeParkedTrafficInsteadOfOverlapping()
        {
            var moving=(MovingCar)Rent(180,1,8); var parked=Rent(195,1);
            for(int i=0;i<100;i++)
            {
                loop.Planner.PredictTraffic(.1f,0d);
                Assert.That(SweptBounds.TryHit(moving.PreviousPosition,moving.CurrentPosition,moving.HalfExtents,
                    parked.PreviousPosition,parked.CurrentPosition,parked.HalfExtents,out _),Is.False);
            }
            Assert.That(moving.AbsoluteZ,Is.GreaterThan(180)); Assert.That(moving.AbsoluteZ,Is.LessThan(190.6));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void SeededDynamicTrafficPreservesRouteAndActuallyOvertakesAtEachProgressionContext(int context)
        {
            ulong overtakers=0,passes=0,changes=0,rejected=0; int peak=0;
            ulong created=gm.Pools.TotalCreated;
            var beforeLeases=new TrafficLease[loop.Planner.Capacity]; int beforeCount=0;
            bool WasPresent(TrafficLease lease) { for(int k=0;k<beforeCount;k++) if(beforeLeases[k].Item==lease.Item && beforeLeases[k].ActivationId==lease.ActivationId)return true;return false; }
            for(int seed=0;seed<32;seed++)
            {
                gm.Pause(); typeof(GameManager).GetField("runSeed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(gm,seed);
                gm.Restart(); if(context>0)gm.Session.Advance(context*90f,0d);
                for(int tick=0;tick<2400;tick++)
                {
                    beforeCount=loop.Planner.ActiveCount; for(int k=0;k<beforeCount;k++)beforeLeases[k]=loop.Planner.GetActive(k);
                    SurvivalRoute.Guide(loop); Assert.That(loop.Step(.1f),Is.True);
                    Assert.That(gm.State,Is.EqualTo(GameState.Playing));
                    int rear=0;
                    for(int i=0;i<loop.Planner.ActiveCount;i++)
                    {
                        var a=loop.Planner.GetActive(i); if(!(a.Item is MovingCar car) || !car.IsOvertaker)continue;
                        rear++;
                        Assert.That(car.AbsoluteZ-car.HalfExtents.z,Is.GreaterThanOrEqualTo(loop.Roads.CoverageStart));
                        Assert.That(car.AbsoluteZ+car.HalfExtents.z,Is.LessThan(loop.Roads.CoverageEnd));
                        for(int j=0;j<loop.Planner.ActiveCount;j++)
                        {
                            var b=loop.Planner.GetActive(j); if(i==j || b.Item.Category!=InteractionCategory.Obstacle)continue;
                            bool bothPresent=WasPresent(a)&&WasPresent(b);
                            if(SweptBounds.TryHit(bothPresent ? car.PreviousPosition : car.CurrentPosition,car.CurrentPosition,car.HalfExtents,
                                bothPresent ? b.Item.PreviousPosition : b.Item.CurrentPosition,b.Item.CurrentPosition,b.Item.HalfExtents,out _))
                                Assert.Fail($"Traffic overlap context={context} seed={seed} tick={tick} pz={gm.Session.Snapshot.Distance} car={car.SpawnSequence} lane={car.Lane}->{car.TargetLane} z={car.PreviousAbsoluteZ}->{car.AbsoluteZ} x={car.PreviousPosition.x}->{car.CurrentPosition.x} other={b.Item.SpawnSequence} z={b.Item.PreviousAbsoluteZ}->{b.Item.AbsoluteZ} x={b.Item.PreviousPosition.x}->{b.Item.CurrentPosition.x}");
                        }
                    }
                    peak=Math.Max(peak,rear); Assert.That(rear,Is.LessThanOrEqualTo(2));
                }
                overtakers+=loop.Planner.SpawnedOvertakers; passes+=loop.Planner.CompletedOvertakes;
                changes+=loop.Planner.AcceptedLaneChanges; rejected+=loop.Planner.RejectedOvertakers;
            }
            Assert.That(overtakers,Is.GreaterThan(0)); Assert.That(passes,Is.GreaterThan(0));
            if(context==0) Assert.That(changes,Is.GreaterThan(0));
            Assert.That(gm.Pools.TotalCreated,Is.EqualTo(created));
            Debug.Log($"[15B dynamic] context={context} seeds=0..31 overtakers={overtakers} passes={passes} laneChanges={changes} rejectedSpawns={rejected} peakRear={peak} created={created}");
        }
        [Test] public void ActualSeededRunGeneratesRearPassesWithoutPoolGrowth()
        {
            ulong created=gm.Pools.TotalCreated;
            SurvivalRoute.Advance(loop,180f);
            Assert.That(loop.Planner.SpawnedMoving,Is.GreaterThan(0));
            Assert.That(loop.Planner.SpawnedOvertakers,Is.GreaterThan(0));
            Assert.That(loop.Planner.CompletedOvertakes,Is.GreaterThan(0));
            Assert.That(gm.Pools.TotalCreated,Is.EqualTo(created));
            Debug.Log($"[15B] overtakers={loop.Planner.SpawnedOvertakers} passes={loop.Planner.CompletedOvertakes} rejected={loop.Planner.RejectedOvertakers} laneRejected={loop.Planner.RejectedLaneChanges}");
        }
    }
}
