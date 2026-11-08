using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Supplementary real Editor pixels, with controlled scenarios disclosed.</summary>
    public static class Task16Capture
    {
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static GameManager m;
        static readonly List<object> shots = new List<object>();
        static void Check(bool value, string reason) { if (!value) throw new Exception(reason); }
        static void Guide(RunSnapshot snapshot)
        {
            if (m.State != GameState.Playing) return;
            var loop = m.Loop; var plan = loop.Planner.PlanAt((long)(snapshot.Distance / 480d));
            int target = snapshot.Distance - plan.AbsoluteStart >= 360d + snapshot.CruiseSpeed * loop.Planner.Configuration.ReactionTime
                ? plan.NextProtectedLane : plan.ProtectedLane;
            if (!loop.Player.IsTransitioning && loop.Player.LaneIndex != target)
                loop.Player.RequestLane(Math.Sign(target - loop.Player.LaneIndex));
        }
        static async Task Shot(string name, string method)
        {
            int frame = Time.frameCount;
            ScreenCapture.CaptureScreenshot("delivery/Screenshots/" + name + ".png");
            await Task.Delay(180);
            Check(Time.frameCount > frame, "Player loop did not advance during capture");
            shots.Add(new { name, method, origin = "Unity 6000.6.3f1 Editor Play Mode on Linux; NOT Windows standalone",
                frameBefore = frame, frameAfter = Time.frameCount, state = m.State.ToString(),
                score = m.Session.Snapshot.Score, best = m.Session.Snapshot.BestScore,
                speed = m.Session.Snapshot.CruiseSpeed, distance = m.Session.Snapshot.Distance,
                fov = m.Loop.Camera.ViewCamera.fieldOfView });
        }
        static async Task Menu()
        {
            if (m.State == GameState.GameOver) Check(m.Restart(), "Restart cleanup");
            if (m.State == GameState.Playing) Check(m.Pause(), "Pause cleanup");
            if (m.State == GameState.Paused) Check(m.ReturnToMenu(), "Menu cleanup");
            await Task.Delay(80);
        }
        static void Begin()
        {
            Check(m.State == GameState.Menu && m.StartRun(), "Start run");
            typeof(AudioController).GetMethod("SetApplicationSuspension", F).Invoke(m.Loop.Audio, new object[] { false, false });
        }
        static Interactable Rent(PoolKey key, float gap)
        {
            var l = m.Loop; double z = m.Session.Snapshot.Distance + gap;
            Check(m.Pools.TryRent(new SpawnContext(key, new Vector3(l.Player.CurrentPosition.x, .6f, (float)(z - l.Origin.Offset)),
                Quaternion.identity, l.Planner.OriginRoot, l.Player.LaneIndex, z, 12345, 0, 990000 + l.Planner.ActiveCount, l.Planner), out var item), "Controlled rental");
            return (Interactable)item;
        }
        static void ClearTraffic() => typeof(SpawnDirector).GetMethod("ReleaseAll", F).Invoke(m.Loop.Planner, null);
        public static async Task<object> Run()
        {
            m = GameManager.Instance; Check(m && m.State == GameState.Menu, "Saved Game Menu required");
            Directory.CreateDirectory("delivery/Screenshots");
            var field = typeof(GameManager).GetField("highScores", F); object original = field.GetValue(m);
            string key = "NTR.Tests.Task16.Capture." + Guid.NewGuid().ToString("N");
            int retainedBest = PlayerPrefs.GetInt("NTR.HighScore.v1", 0);
            PlayerPrefs.SetInt(key, retainedBest); field.SetValue(m, new HighScoreStore(key));
            try
            {
                await Shot("editor-main-menu", "Saved Game startup; production best retained");
                Begin(); m.Session.SnapshotChanged += Guide;
                await Task.Delay(600);
                await Shot("editor-open-road-low-speed", "Normal real frames; fixture follows protected route with ordinary lane requests");
                var deadline = DateTime.UtcNow.AddSeconds(12);
                while (m.Loop.Vfx.Fireworks.Launches == 0) { Check(DateTime.UtcNow < deadline, "No natural firework launch"); await Task.Delay(16); }
                await Task.Delay(900);
                await Shot("editor-fireworks", "Naturally scheduled pooled firework; real playing frames");
                Check(m.Pause(), "Pause"); await Shot("editor-pause", "Production Pause command/menu");
                Check(m.Resume(), "Resume"); await Menu();
                Begin();
                for (int tick = 0; tick < 12000 && RoadManager.KeyAt((long)(m.Session.Snapshot.Distance / 60d)) != PoolKey.RoadTunnel; tick++) m.Loop.Step(.02f);
                for (int tick = 0; tick < 60; tick++) m.Loop.Step(.02f);
                m.Loop.Camera.ResetPresentation(); await Task.Delay(100);
                await Shot("editor-tunnel", "Deterministic gameplay pre-roll into tunnel, then real frames; no geometry edits"); await Menu();
                Begin();
                for (int tick = 0; tick < 12000 && m.Session.Snapshot.CruiseSpeed < 49.99f; tick++) {
                    Check(m.State == GameState.Playing, "High-speed route ended"); m.Loop.Step(.02f);
                }
                Check(m.Session.Snapshot.CruiseSpeed >= 49.99f, "Cap not reached");
                m.Loop.Camera.ResetPresentation(); await Task.Delay(80);
                await Shot("editor-high-speed-neon", "Deterministic safe-route pre-roll to actual 50 m/s; no speed/config/resource grant"); await Menu();
                Begin();
                for (int tick = 0; tick < 3000 && m.Loop.Planner.RearWarningCount == 0; tick++) m.Loop.Step(.02f);
                Check(m.Loop.Planner.RearWarningCount > 0, "No seeded rear warning");
                m.Loop.Camera.ResetPresentation(); await Task.Delay(180);
                await Shot("editor-rear-warning", "Seeded forecast warning after gameplay pre-roll; no direct warning trigger"); await Menu();
                m.Session.SnapshotChanged -= Guide;
                foreach (var keyType in new[] { PoolKey.Coin, PoolKey.Fuel, PoolKey.Stationary }) {
                    Begin(); ClearTraffic();
                    var item = Rent(keyType, 5f); var limit = DateTime.UtcNow.AddSeconds(4);
                    while (item.IsRented && !item.Encountered) {
                        Check(DateTime.UtcNow < limit && m.State == GameState.Playing, "Controlled contact timeout"); await Task.Delay(8);
                    }
                    await Shot("editor-" + (keyType == PoolKey.Coin ? "coin-pickup" : keyType == PoolKey.Fuel ? "fuel-pickup" : "collision-sparks"),
                        "Controlled actual pooled " + keyType + " rental ahead in player lane; production swept contact/reward/VFX; real frames");
                    await Menu();
                }
                Begin();
                for (int hit = 0; hit < 3; hit++) {
                    ClearTraffic(); Rent(PoolKey.Stationary, 5f);
                    int before = m.Session.Snapshot.Lives; var end = DateTime.UtcNow.AddSeconds(6);
                    while (m.State == GameState.Playing && m.Session.Snapshot.Lives == before) {
                        Check(DateTime.UtcNow < end, "Damage timeout"); await Task.Delay(8);
                    }
                    if (m.State == GameState.Playing) await Task.Delay(1150);
                }
                Check(m.State == GameState.GameOver, "Life-zero Game Over required");
                await Shot("editor-game-over", "Three real controlled obstacle contacts separated by grace; production Game Over");
                await Menu();
                return new { passed = true, shots, retainedBest, diskBestAfter = PlayerPrefs.GetInt("NTR.HighScore.v1", 0),
                    limitation = "Supplementary Editor screenshots only; staging is not Windows runtime acceptance or whole-game human listening/performance certification" };
            }
            finally {
                m.Session.SnapshotChanged -= Guide; await Menu(); field.SetValue(m, original);
                PlayerPrefs.DeleteKey(key); PlayerPrefs.Save();
            }
        }
    }
}
