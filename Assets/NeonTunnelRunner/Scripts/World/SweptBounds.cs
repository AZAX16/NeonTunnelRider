using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Pure relative swept AABB. Inclusive slabs retain exact grazing and initial overlap.</summary>
    public static class SweptBounds
    {
        public static bool TryHit(Vector3 a0, Vector3 a1, Vector3 ha, Vector3 b0, Vector3 b1, Vector3 hb, out float time)
        {
            double enter = 0d, exit = 1d;
            for (int axis = 0; axis < 3; axis++)
            {
                double r = (double)a0[axis] - b0[axis];
                double v = ((double)a1[axis] - a0[axis]) - ((double)b1[axis] - b0[axis]);
                double h = (double)ha[axis] + hb[axis];
                if (!double.IsFinite(r) || !double.IsFinite(v) || !double.IsFinite(h) || ha[axis] < 0 || hb[axis] < 0)
                    throw new ArgumentException("Swept bounds require finite poses and nonnegative extents.");
                if (Math.Abs(v) <= 1e-10d) { if (r < -h || r > h) { time = 0; return false; } continue; }
                double t0 = (-h-r)/v, t1 = (h-r)/v;
                if (t0 > t1) (t0,t1) = (t1,t0);
                enter = Math.Max(enter,t0); exit = Math.Min(exit,t1);
                if (enter > exit) { time = 0; return false; }
            }
            time = (float)enter;
            return true;
        }
    }
}
