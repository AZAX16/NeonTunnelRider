using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NeonTunnelRunner
{
    public sealed class LaneMovementTests
    {
        private GameObject root;
        private GameplayConfig config;
        private PlayerController player;

        [SetUp]
        public void Setup()
        {
            config = ScriptableObject.CreateInstance<GameplayConfig>();
            root = new GameObject("LaneFixture");
            player = EditModeMovementRig.AddPlayer(root.transform, config);
            player.Initialize(config);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(config);
        }

        [TestCase(0.01f)]
        [TestCase(0.02f)]
        [TestCase(0.07f)]
        [TestCase(0.14f)]
        [TestCase(0.28f)]
        [TestCase(0.5f)]
        public void AdjacentTransitionIsLinearAndIndependentOfStepSize(float step)
        {
            player.RequestLane(1);
            float elapsed = 0f;
            while (elapsed < 0.28f - 0.000001f)
            {
                float dt = Mathf.Min(step, 0.28f - elapsed);
                Vector3 before = player.CurrentPosition;
                player.PredictStep(dt, 22f);
                elapsed += dt;
                Assert.That(player.PreviousPosition, Is.EqualTo(before));
                Assert.That(player.CurrentPosition.x, Is.EqualTo(3.2f * elapsed / 0.28f).Within(0.00001f));
                Assert.That(player.CurrentPosition.y, Is.EqualTo(0.6f));
                Assert.That(player.CurrentPosition.x, Is.InRange(before.x, 3.2f));
            }
            Assert.That(player.IsTransitioning, Is.False);
            Assert.That(player.LaneIndex, Is.EqualTo(1));
            Assert.That(player.CurrentPosition.x, Is.EqualTo(3.2f));
            Assert.That(player.CurrentPosition.z, Is.EqualTo(22f * 0.28f).Within(0.00001f));
        }

        [TestCase(0.01f)]
        [TestCase(0.02f)]
        [TestCase(0.1f)]
        [TestCase(0.5f)]
        public void ForwardTravelUsesPassedTimeAndKeepsHeightAndRotation(float dt)
        {
            double travel = 0d;
            for (int i = 0; i < 100; i++) travel += player.PredictStep(dt, 22f);
            Assert.That(travel, Is.EqualTo(22d * dt * 100).Within(0.0000001d));
            Assert.That(player.CurrentPosition.z, Is.EqualTo(travel).Within(0.002d));
            Assert.That(player.CurrentPosition.y, Is.EqualTo(0.6f));
            Assert.That(player.transform.rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(player.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void BoundsRejectRepeatedEdgeCommands()
        {
            foreach (int direction in new[] { -1, 1, 1 })
            {
                player.RequestLane(direction);
                player.PredictStep(0.28f, 22f);
            }
            for (int i = 0; i < 100; i++)
            {
                Assert.That(player.RequestLane(1), Is.False);
                player.PredictStep(0.01f, 22f);
                Assert.That(player.LaneIndex, Is.EqualTo(1));
                Assert.That(player.CurrentPosition.x, Is.EqualTo(3.2f));
            }
        }

        [Test]
        public void LatestValidQueuedPressReplacesOneSlotAndWaitsForTheCurrentLane()
        {
            player.RequestLane(-1);
            player.PredictStep(0.28f, 22f);
            player.RequestLane(1); // -1 -> 0.
            player.PredictStep(0.07f, 22f);
            Assert.That(player.CurrentPosition.x, Is.EqualTo(-2.4f).Within(0.00001f));
            player.RequestLane(1); // Queue 0 -> +1.
            player.RequestLane(-1); // Replace with 0 -> -1.
            Assert.That(player.QueuedCommand, Is.EqualTo(-1));
            player.PredictStep(0.21f, 22f);
            Assert.That(player.CurrentPosition.x, Is.EqualTo(0f).Within(0.00001f));
            Assert.That(player.LaneIndex, Is.EqualTo(-1));
            player.PredictStep(0.14f, 22f);
            Assert.That(player.CurrentPosition.x, Is.EqualTo(-1.6f).Within(0.00001f));
            player.PredictStep(0.14f, 22f);
            Assert.That(player.CurrentPosition.x, Is.EqualTo(-3.2f));
            Assert.That(player.IsTransitioning, Is.False);
        }

        [Test]
        public void LargeStepFinishesAdjacentTransitionsInOrderWithRemainingTime()
        {
            player.RequestLane(-1);
            player.PredictStep(0.28f, 22f);
            player.RequestLane(1);
            player.RequestLane(1);
            player.PredictStep(0.42f, 22f);
            Assert.That(player.CurrentPosition.x, Is.EqualTo(1.6f).Within(0.00001f));
            Assert.That(player.LaneIndex, Is.EqualTo(1));
            Assert.That(player.TransitionProgress, Is.EqualTo(0.5f).Within(0.00001f));
        }

        [Test]
        public void ClearQueuePreservesCurrentTransitionAndResetClearsBothPoses()
        {
            player.RequestLane(1);
            player.PredictStep(0.07f, 22f);
            player.RequestLane(-1);
            player.ClearQueuedCommand();
            player.PredictStep(0.21f, 22f);
            Assert.That(player.CurrentPosition.x, Is.EqualTo(3.2f));
            Assert.That(player.IsTransitioning, Is.False);
            player.ResetRun();
            Assert.That(player.CurrentPosition, Is.EqualTo(new Vector3(0f, 0.6f, 0f)));
            Assert.That(player.PreviousPosition, Is.EqualTo(player.CurrentPosition));
            Assert.That(player.LaneIndex, Is.Zero);
            Assert.That(player.QueuedCommand, Is.Zero);
            Assert.That(player.TransitionProgress, Is.Zero);
        }
    }

    internal static class EditModeMovementRig
    {
        internal static PlayerController AddPlayer(Transform parent, GameplayConfig config)
        {
            var go = new GameObject("PlayerFixture");
            go.transform.SetParent(parent, false);
            var player = go.AddComponent<PlayerController>();
            var body = go.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            var anchor = new GameObject("CameraAnchor").transform;
            anchor.SetParent(go.transform, false);
            Assign(player, "configuration", config);
            Assign(player, "body", body);
            Assign(player, "cameraAnchor", anchor);
            return player;
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
            var player = AddPlayer(root.transform, config);
            var input = root.AddComponent<InputRouter>();
            Assign(input, "gameManager", manager);
            Assign(input, "actionsAsset", UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/NeonTunnelRunner/Input/Runner.inputactions"));
            Assign(loop, "inputRouter", input);
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
            var survival=player.gameObject.AddComponent<PlayerSurvival>(); Assign(survival,"gameManager",manager);
            var progression=root.AddComponent<ProgressionSystem>(); Assign(progression,"gameManager",manager); Assign(loop,"progression",progression);
            var sensor=player.gameObject.AddComponent<InteractionSensor>();
            Assign(sensor,"player",player); Assign(sensor,"registry",planner); Assign(sensor,"survival",survival); Assign(sensor,"progression",progression);
            Assign(loop,"interactionSensor",sensor); Assign(loop,"survival",survival);
            Assign(origin,"additionalParticipants",new MonoBehaviour[] { planner });
            Assign(loop, "roads", roads); Assign(loop, "worldOrigin", origin); Assign(loop, "spawnDirector", planner);

        }

        private static void Assign(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
