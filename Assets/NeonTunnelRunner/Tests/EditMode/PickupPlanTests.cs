using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class PickupPlanTests
    {
        private GameObject root;
        private SpawnDirector planner;
        private WorldConfig world;
        private GameplayConfig gameplay;
        [SetUp] public void Setup()
        {
            root=new GameObject("PickupPlanFixture"); planner=root.AddComponent<SpawnDirector>();
            world=ScriptableObject.CreateInstance<WorldConfig>(); gameplay=ScriptableObject.CreateInstance<GameplayConfig>();
            Assign(planner,"configuration",world); Assign(planner,"gameplay",gameplay);
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(root); Object.DestroyImmediate(world); Object.DestroyImmediate(gameplay); }
        [Test] public void ThousandSeedsCoverTenKilometresOfContinuousFuelRoutesAndPickupBounds()
        {
            long fuels=0, coins=0, rejectedCoins=0, transitions=0;
            double maximumGap=0d;
            for (int seed=0;seed<1000;seed++)
            {
                planner.ResetRun(seed);
                PickupPlacement previous=default; bool hasPrevious=false;
                for (long block=0;block<21;block++)
                {
                    var plan=planner.PlanAt(block); uint acceptedCoins=0;
                    Assert.That(PatternValidator.ValidateFuelRoute(plan,world,gameplay),Is.True,$"seed={seed}, block={block}");
                    if (plan.ProtectedLane!=plan.NextProtectedLane) transitions++;
                    for (int slot=0;slot<BlockPlan.FuelCount;slot++)
                    {
                        var fuel=plan.GetFuel(slot); fuels++;
                        Assert.That(PatternValidator.ValidatePickup(planner,fuel,out var reason),Is.True,$"seed={seed}, block={block}, slot={slot}, reason={reason}");
                        double gap=fuel.AbsolutePosition-(hasPrevious ? previous.AbsolutePosition : 0d);
                        Assert.That(gap,Is.GreaterThan(0d).And.LessThanOrEqualTo(220d)); maximumGap=Math.Max(maximumGap,gap);
                        if (hasPrevious) Assert.That(PatternValidator.ValidateFuelLink(planner,previous,fuel),Is.True);
                        // Independent route timing: stay protected until the clear buffer, react,
                        // traverse every intervening lane, and arrive before the expanded pickup.
                        if (slot==2)
                        {
                            double deadline=(fuel.AbsolutePosition-2.3d-plan.BufferStart)/50d;
                            Assert.That(deadline,Is.GreaterThan(world.ReactionTime+Math.Abs(plan.NextProtectedLane-plan.ProtectedLane)*gameplay.LaneTransitionDuration+world.TransitionMargin));
                        }
                        previous=fuel; hasPrevious=true;
                    }
                    for (int slot=0;slot<BlockPlan.CoinSlots;slot++)
                    {
                        var coin=plan.GetCoin(slot);
                        if (!PatternValidator.ValidatePickup(planner,coin,out _)) { rejectedCoins++; continue; }
                        coins++; acceptedCoins|=1u<<slot;
                        for (int f=0;f<BlockPlan.FuelCount;f++)
                            Assert.That(coin.Lane!=plan.GetFuel(f).Lane || Math.Abs(coin.AbsolutePosition-plan.GetFuel(f).AbsolutePosition)>1d,Is.True);
                        for (int other=0;other<slot;other++)
                        { var c=plan.GetCoin(other); Assert.That(c.Lane!=coin.Lane || Math.Abs(c.AbsolutePosition-coin.AbsolutePosition)>1d,Is.True); }
                    }
                    // Independently check actual accepted stationary/moving footprints at both
                    // introduction frontiers against every accepted pickup in this block.
                    for (int row=0;row<6;row++) for (int lane=-1;lane<=1;lane++)
                    {
                        if (!plan.TryGetCandidate(row,lane,out var car)) continue;
                        foreach (double ahead in new[] {300d,180d})
                        {
                            double player=Math.Max(0d,car.AbsolutePosition-ahead);
                            if (!planner.TrySelectTraffic(car,player,out var kind,out _)) continue;
                            double end=car.AbsolutePosition+(kind==PoolKey.Moving ? 8d*(car.AbsolutePosition-player+60d+2.2d)/14d : 0d);
                            for (int f=0;f<BlockPlan.FuelCount;f++) AssertSeparated(plan.GetFuel(f),lane,car.AbsolutePosition,end);
                            for (int c=0;c<BlockPlan.CoinSlots;c++) if ((acceptedCoins&(1u<<c))!=0) AssertSeparated(plan.GetCoin(c),lane,car.AbsolutePosition,end);
                        }
                    }
                }
            }
            Assert.That(fuels,Is.EqualTo(63000)); Assert.That(coins,Is.GreaterThan(84000)); Assert.That(rejectedCoins,Is.GreaterThan(0));
            Assert.That(transitions,Is.GreaterThan(1000)); Assert.That(maximumGap,Is.EqualTo(180d));
            Debug.Log($"[Task07] seeds=0..999, 21x480=10080m/seed; max speed=50, min=22, reaction=.7, transition=.28, margin=.3; required fuel={fuels}, accepted coins={coins}, rejected coins={rejectedCoins}, lane changes={transitions}, max fuel gap={maximumGap}m.");
        }
        [Test] public void FuelCannotBeMovedToAnEmptyButUnreachableLaneOrEarlyBufferPoint()
        {
            planner.ResetRun(12345); var plan=planner.PlanAt(0); var fuel=plan.GetFuel(2);
            var wrong=new PickupPlacement(12345,0,2,PoolKey.Fuel,plan.ProtectedLane==1 ? -1 : 1,fuel.AbsolutePosition,true);
            Assert.That(PatternValidator.ValidatePickup(planner,wrong,out _),Is.False);
            wrong=new PickupPlacement(12345,0,2,PoolKey.Fuel,plan.NextProtectedLane,365d,true);
            Assert.That(PatternValidator.ValidatePickup(planner,wrong,out _),Is.False);
            Assign(gameplay,"maximumSpeed",80f);
            Assert.That(PatternValidator.ValidateFuelRoute(new BlockPlan(1,1,-1,1,0),world,gameplay),Is.False);
        }
        [Test] public void PickupOnTrafficAndMovingEncounterEnvelopeIsRejected()
        {
            bool testedMoving=false, testedStationary=false;
            for (int seed=0;seed<100 && !testedMoving;seed++)
            {
                planner.ResetRun(seed); var plan=planner.PlanAt(1);
                for (int row=0;row<6;row++) for (int lane=-1;lane<=1;lane++)
                {
                    if (!plan.TryGetCandidate(row,lane,out var candidate) || !planner.TrySelectTraffic(candidate,candidate.AbsolutePosition-300d,out var kind,out _)) continue;
                    var atCar=new PickupPlacement(seed,1,3,PoolKey.Coin,lane,candidate.AbsolutePosition,false);
                    Assert.That(PatternValidator.ValidatePickup(planner,atCar,out var rejection),Is.False); Assert.That(rejection,Is.EqualTo(PatternRejection.PickupOverlap)); testedStationary=true;
                    if (kind!=PoolKey.Moving) continue;
                    var downRoute=new PickupPlacement(seed,1,3,PoolKey.Coin,lane,candidate.AbsolutePosition+20d,false);
                    Assert.That(PatternValidator.ValidatePickup(planner,downRoute,out _),Is.False); testedMoving=true;
                }
            }
            Assert.That(testedMoving && testedStationary,Is.True);
        }
        [Test] public void StartupRejectsTimingThatClearsTheBufferButCannotReachItsFuel()
        {
            Assign(gameplay,"maximumSpeed",75f);
            Assert.That(PatternValidator.CanTransition(-1,1,world,gameplay),Is.True);
            Assert.That(world.TryValidate(gameplay,out var error),Is.False);
            Assert.That(error,Does.Contain("fuel is unreachable"));
        }
        [Test] public void PlansRemainIndependentOfTraversalOriginAndOptionalSelection()
        {
            planner.ResetRun(12345); var expected=planner.PlanAt(5);
            var fuel=expected.GetFuel(2); var coin=expected.GetCoin(3);
            for (int i=100;i>=0;i--) { planner.FillWindow(i*480d); planner.PlanAt(i).GetCoin(i%8); }
            Assert.That(planner.PlanAt(5),Is.EqualTo(expected)); Assert.That(planner.PlanAt(5).GetFuel(2),Is.EqualTo(fuel));
            Assert.That(planner.PlanAt(5).GetCoin(3),Is.EqualTo(coin));
            const long large=1000000000000L;
            var a=planner.PlanAt(large).GetFuel(2); var b=planner.PlanAt(large+1).GetFuel(0);
            Assert.That(b.AbsolutePosition-a.AbsolutePosition,Is.EqualTo(150d)); Assert.That(PatternValidator.ValidateFuelLink(planner,a,b),Is.True);
            planner.ResetRun(12345); Assert.That(planner.PlanAt(5).GetFuel(2),Is.EqualTo(fuel));
        }
        private static void AssertSeparated(PickupPlacement pickup,int lane,double start,double end)
        { Assert.That(pickup.Lane!=lane || pickup.AbsolutePosition+2.7d<start || pickup.AbsolutePosition-2.7d>end,Is.True,
            $"Pickup overlap seed={pickup.Seed}, block={pickup.BlockIndex}, slot={pickup.Slot}, car=[{start},{end}]"); }
        private static void Assign(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    }
}
