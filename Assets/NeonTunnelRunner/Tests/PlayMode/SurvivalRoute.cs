using System;
using NUnit.Framework;

namespace NeonTunnelRunner
{
    // Fixture driver follows the real protected route; it never changes resources or poses.
    internal static class SurvivalRoute
    {
        internal static void Guide(GameplayLoop loop)
        {
            double distance=loop.Owner.Session.Snapshot.Distance;
            var plan=loop.Planner.PlanAt((long)(distance/480d));
            int target=distance-plan.AbsoluteStart>=360d+loop.Owner.Session.Snapshot.CruiseSpeed*loop.Planner.Configuration.ReactionTime
                ? plan.NextProtectedLane : plan.ProtectedLane;
            if (!loop.Player.IsTransitioning && loop.Player.LaneIndex!=target)
                loop.Player.RequestLane(Math.Sign(target-loop.Player.LaneIndex));
        }
        internal static void Advance(GameplayLoop loop,float duration)
        {
            double left=duration;
            while (left>0.00000001d)
            {
                Guide(loop);
                float dt=(float)Math.Min(.1d,left);
                Assert.That(loop.Step(dt),Is.True);
                Assert.That(loop.Owner.State,Is.EqualTo(GameState.Playing),"The real route must survive with drain and damage enabled.");
                left-=dt;
            }
        }
    }
}
