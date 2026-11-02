using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class PlayerLifecycleTests : InputTestFixture
    {
        private GameObject root;
        private GameplayConfig config;
        private GameManager manager;
        private GameplayLoop loop;
        private PlayerController player;
        private Keyboard keyboard;
        private float originalTimeScale;

        public override void Setup()
        {
            base.Setup();
            originalTimeScale = Time.timeScale;
            config = ScriptableObject.CreateInstance<GameplayConfig>();
            root = PlayModeMovementRig.Create("PlayerLifecycleFixture", config, 700);
            manager = root.GetComponent<GameManager>();
            loop = manager.Loop;
            player = loop.Player;
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            if (root) Object.DestroyImmediate(root);
            if (config) Object.DestroyImmediate(config);
            Time.timeScale = originalTimeScale;
            base.TearDown();
        }

        [TestCase(0.01f)]
        [TestCase(0.02f)]
        [TestCase(0.1f)]
        [TestCase(0.5f)]
        public void LoopAdvancesTimeDistanceAndSpeedExactlyOnce(float dt)
        {
            Assert.That(loop.Step(dt), Is.False);
            manager.StartRun();
            for (int i = 0; i < 100; i++) { SurvivalRoute.Guide(loop); loop.Step(dt); }
            var snapshot = manager.Session.Snapshot;
            Assert.That(snapshot.ElapsedTime, Is.EqualTo(100d * dt).Within(0.0000001d));
            Assert.That(snapshot.Distance, Is.EqualTo(22d * dt * 100).Within(0.0000001d));
            Assert.That(snapshot.ActualSpeed, Is.EqualTo(22f));
            Assert.That(player.CurrentPosition.z, Is.EqualTo(snapshot.Distance - loop.Origin.Offset).Within(0.002d));
            manager.Pause();
            var paused = manager.Session.Snapshot;
            Vector3 position = player.CurrentPosition;
            Time.timeScale = 1f;
            Assert.That(loop.Step(dt), Is.False);
            Assert.That(manager.Session.Snapshot, Is.EqualTo(paused));
            Assert.That(player.CurrentPosition, Is.EqualTo(position));
            manager.Resume(); manager.EndRun(GameOverReason.Lives);
            Assert.That(loop.Step(dt), Is.False);
            Assert.That(player.CurrentPosition, Is.EqualTo(position));
        }

        [Test]
        public void PausingPreservesCurrentTransitionButClearsQueuedAndPendingInput()
        {
            manager.StartRun();
            Press(keyboard.dKey); loop.Step(0.07f);
            Release(keyboard.dKey); Press(keyboard.aKey); loop.Step(0.01f); // Queue left.
            Release(keyboard.aKey); Press(keyboard.dKey); // Pending, not consumed yet.
            var before = player.CurrentPosition;
            float progress = player.TransitionProgress;
            manager.Pause();
            Assert.That(player.QueuedCommand, Is.Zero);
            Assert.That(loop.Input.ConsumeLaneCommand(), Is.Zero);
            Assert.That(player.CurrentPosition, Is.EqualTo(before));
            Assert.That(player.TransitionProgress, Is.EqualTo(progress));
            manager.Resume();
            loop.Step(0.2f);
            Assert.That(player.CurrentPosition.x, Is.EqualTo(3.2f));
            Assert.That(player.IsTransitioning, Is.False);
            loop.Step(0.28f);
            Assert.That(player.LaneIndex, Is.EqualTo(1));
        }

        [Test]
        public void RestartResetsPoseBuffersInputAndCameraAnchorWhileKeepingBest()
        {
            manager.StartRun();
            Press(keyboard.dKey); loop.Step(0.07f);
            Release(keyboard.dKey); Press(keyboard.aKey); loop.Step(0.01f);
            Release(keyboard.aKey); Press(keyboard.dKey);
            manager.Session.RetainBestScore(1500);
            manager.Pause(); manager.Restart();
            Assert.That(player.CurrentPosition, Is.EqualTo(config.StartingPosition));
            Assert.That(player.PreviousPosition, Is.EqualTo(config.StartingPosition));
            Assert.That(player.Body.position, Is.EqualTo(config.StartingPosition));
            Assert.That(player.LaneIndex, Is.Zero);
            Assert.That(player.QueuedCommand, Is.Zero);
            Assert.That(player.IsTransitioning, Is.False);
            Assert.That(loop.Input.ConsumeLaneCommand(), Is.Zero);
            Assert.That(manager.Session.Snapshot.Distance, Is.Zero);
            Assert.That(manager.Session.Snapshot.BestScore, Is.EqualTo(1500));
            InputSystem.Update(); loop.Step(0.28f); // The D still held through restart must not become a new press.
            Assert.That(player.LaneIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RigidbodyAndStableAnchorFollowTheLogicalPlayer()
        {
            var camera = new GameObject("CameraFixture").transform;
            camera.SetParent(player.CameraAnchor, false);
            try
            {
                manager.StartRun(); Press(keyboard.dKey);
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(player.CurrentPosition.z, Is.GreaterThan(0f));
                Assert.That(Vector3.Distance(player.Body.position, player.CurrentPosition), Is.LessThan(0.001f));
                Assert.That(player.Body.isKinematic, Is.True);
                Assert.That(player.Body.useGravity, Is.False);
                Assert.That(camera.position - player.transform.position, Is.EqualTo(player.CameraAnchor.localPosition));
                manager.Pause();
                var paused = player.Body.position;
                yield return null; yield return null;
                Assert.That(player.Body.position, Is.EqualTo(paused));
            }
            finally { if (camera) Object.DestroyImmediate(camera.gameObject); }
        }
    }

    internal static class PlayModeMovementRig
    {
        internal static GameObject Create(string name, GameplayConfig config, int bestScore = 0)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            var loop = root.AddComponent<GameplayLoop>();
            var manager = root.AddComponent<GameManager>();
            Assign(manager, "gameplayConfig", config);
            Assign(manager, "gameplayLoop", loop);
            Assign(manager, "initialBestScore", bestScore);
            Assign(loop, "gameManager", manager);
            Wire(root, manager, loop, config);
            root.SetActive(true);
            return root;
        }

        internal static void Wire(GameObject root, GameManager manager, GameplayLoop loop, GameplayConfig config)
        {
            var pools = root.AddComponent<PoolService>();
            var storage = new GameObject("Pools");
            storage.transform.SetParent(root.transform, false);
            storage.SetActive(false);
            Assign(pools, "gameManager", manager);
            Assign(pools, "storageRoot", storage.transform);
#if UNITY_EDITOR
            Assign(pools, "configuration", UnityEditor.AssetDatabase.LoadAssetAtPath<PoolConfig>("Assets/NeonTunnelRunner/Configs/Pools.asset"));
#endif
            Assign(manager, "poolService", pools);
            var playerObject = new GameObject("PlayerFixture");
            playerObject.transform.SetParent(root.transform, false);
            var player = playerObject.AddComponent<PlayerController>();
            var body = playerObject.GetComponent<Rigidbody>();
            body.isKinematic = true; body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            var anchor = new GameObject("CameraAnchor").transform;
            anchor.SetParent(playerObject.transform, false);
            anchor.localPosition = new Vector3(0f, 4.4f, -10f);
            Assign(player, "configuration", config);
            Assign(player, "body", body);
            Assign(player, "cameraAnchor", anchor);
            var router = root.AddComponent<InputRouter>();
            Assign(router, "gameManager", manager);
#if UNITY_EDITOR
            Assign(router, "actionsAsset", UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/NeonTunnelRunner/Input/Runner.inputactions"));
#endif
            Assign(loop, "inputRouter", router);
            Assign(loop, "player", player);
            var roads = root.AddComponent<RoadManager>();
            var origin = root.AddComponent<WorldOrigin>();
            var planner = root.AddComponent<SpawnDirector>();
            var active = new GameObject("RoadSegments").transform;
            active.SetParent(root.transform, false);
#if UNITY_EDITOR
            var world = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldConfig>("Assets/NeonTunnelRunner/Configs/World.asset");
            Assign(roads, "configuration", world);
            Assign(origin, "configuration", world);
            Assign(planner, "configuration", world);
#endif
            Assign(roads, "pools", pools); Assign(roads, "activeRoot", active);
            Assign(origin, "player", player); Assign(origin, "roads", roads); Assign(origin, "chaseCamera", player.CameraAnchor);
            Assign(planner, "gameplay", config);
            Assign(planner,"pools",pools);
            var traffic=new GameObject("Interactables").transform; traffic.SetParent(root.transform,false);
            Assign(planner,"activeRoot",traffic);
            var survival=playerObject.AddComponent<PlayerSurvival>(); Assign(survival,"gameManager",manager);
            var progression=root.AddComponent<ProgressionSystem>(); Assign(progression,"gameManager",manager); Assign(loop,"progression",progression);
            var sensor=playerObject.AddComponent<InteractionSensor>();
            Assign(sensor,"player",player); Assign(sensor,"registry",planner); Assign(sensor,"survival",survival);
            Assign(sensor,"progression",progression);
            Assign(loop,"interactionSensor",sensor); Assign(loop,"survival",survival);
            var visualObject=new GameObject("Visual"); visualObject.transform.SetParent(player.transform,false);
            var renderer=visualObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/NeonTunnelRunner/Materials/ReferenceMarking.mat");
            var visuals=player.gameObject.AddComponent<VehicleVisuals>();
            Assign(visuals,"gameManager",manager); Assign(visuals,"flashRenderer",renderer); Assign(loop,"vehicleVisuals",visuals);
            Assign(origin,"additionalParticipants",new MonoBehaviour[] { planner });
            Assign(loop, "roads", roads); Assign(loop, "worldOrigin", origin); Assign(loop, "spawnDirector", planner);

        }

        private static void Assign(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
