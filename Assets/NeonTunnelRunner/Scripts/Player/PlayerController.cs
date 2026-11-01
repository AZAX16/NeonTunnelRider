using System;
using UnityEngine;

namespace NeonTunnelRunner
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameplayConfig configuration;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform cameraAnchor;
        public readonly struct MotionLeg
        {
            public Vector3 Start { get; }
            public Vector3 End { get; }
            public float StartTime { get; }
            public float EndTime { get; }
            internal MotionLeg(Vector3 start, Vector3 end, float from, float to)
            { Start=start; End=end; StartTime=from; EndTime=to; }
        }
        private readonly MotionLeg[] motionLegs = new MotionLeg[3];
        public int MotionLegCount { get; private set; }
        public MotionLeg GetMotionLeg(int index) => index >= 0 && index < MotionLegCount ? motionLegs[index] : throw new ArgumentOutOfRangeException(nameof(index));
        private int laneIndex;
        private int queuedCommand;
        private float transitionStartX;
        private double transitionElapsed;
        private bool transitioning;
        private Vector3 previousPosition, currentPosition;
        private double previousAbsoluteZ, currentAbsoluteZ, originOffset;

        public GameplayConfig Configuration => configuration;
        public Rigidbody Body => body;
        public Transform CameraAnchor => cameraAnchor;
        public int LaneIndex => laneIndex;
        public int QueuedCommand => queuedCommand;
        public bool IsTransitioning => transitioning;
        public float TransitionProgress => transitioning ? (float)(transitionElapsed / configuration.LaneTransitionDuration) : 0f;
        public Vector3 PreviousPosition => previousPosition;
        public Vector3 CurrentPosition => currentPosition;

        internal void Initialize(GameplayConfig config)
        {
            if (!body || configuration != config)
                throw new InvalidOperationException("Assign the player's Rigidbody and shared GameplayConfig.");
            ResetRun();
        }

        internal void ResetRun()
        {
            MotionLegCount = 0;
            laneIndex = configuration.StartingLane;
            queuedCommand = 0;
            transitioning = false;
            transitionElapsed = 0d;
            previousPosition = currentPosition = configuration.StartingPosition;
            previousAbsoluteZ = currentAbsoluteZ = originOffset = 0d;
            transitionStartX = currentPosition.x;
            transform.localScale = Vector3.one;
            HoldPose();
        }

        public bool RequestLane(int direction)
        {
            if (direction != -1 && direction != 1) return false;
            if (laneIndex + direction < -1 || laneIndex + direction > 1) return false;
            if (transitioning) queuedCommand = direction;
            else BeginTransition(direction);
            return true;
        }

        private void BeginTransition(int direction)
        {
            transitionStartX = currentPosition.x;
            laneIndex += direction;
            transitionElapsed = 0d;
            transitioning = true;
        }

        public void ClearQueuedCommand() => queuedCommand = 0;

        /// <summary>Predict authoritative poses before physics commit; future swept contacts use these values.</summary>
        public double PredictStep(float deltaTime, float speed)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0f)
                throw new ArgumentOutOfRangeException(nameof(speed));
            previousPosition = currentPosition;
            previousAbsoluteZ = currentAbsoluteZ;
            MotionLegCount = 0;
            double remaining = deltaTime;
            double elapsed = 0d;
            while (transitioning)
            {
                double duration = configuration.LaneTransitionDuration;
                double consumed = Math.Min(remaining, duration - transitionElapsed);
                Vector3 legStart = currentPosition;
                double from = elapsed;
                elapsed += consumed;
                transitionElapsed += consumed;
                remaining -= consumed;
                currentPosition.x = Mathf.Lerp(transitionStartX, laneIndex * configuration.LaneWidth,
                    (float)Math.Min(1d, transitionElapsed / duration));
                currentPosition.z=(float)(previousAbsoluteZ+(double)speed*elapsed-originOffset);
                if (consumed > 0d) motionLegs[MotionLegCount++]=new MotionLeg(legStart,currentPosition,(float)(from/deltaTime),(float)(elapsed/deltaTime));
                // Accommodate float/rational timestep quantization without changing project timing.
                if (duration - transitionElapsed > 0.000001d) break;
                currentPosition.x = laneIndex * configuration.LaneWidth;
                transitioning = false;
                transitionElapsed = 0d;
                int queued = queuedCommand;
                queuedCommand = 0;
                if (queued != 0) BeginTransition(queued);
                if (remaining <= 0d) break;
            }
            if (remaining > 0d)
            {
                Vector3 end=currentPosition;
                end.z=(float)(previousAbsoluteZ+(double)speed*deltaTime-originOffset);
                motionLegs[MotionLegCount++]=new MotionLeg(currentPosition,end,(float)(elapsed/deltaTime),1f);
            }
            double travel = (double)speed * deltaTime;
            currentAbsoluteZ += travel;
            currentPosition.z = (float)(currentAbsoluteZ - originOffset);
            currentPosition.y = configuration.StartingPosition.y;
            return travel;
        }

        internal void SetAbsoluteForward(double absoluteZ, double offset)
        {
            currentAbsoluteZ = absoluteZ;
            originOffset = offset;
            currentPosition.z = (float)(absoluteZ - offset);
        }

        internal void ShiftOrigin(double newOffset)
        {
            originOffset = newOffset;
            previousPosition.z = (float)(previousAbsoluteZ - newOffset);
            currentPosition.z = (float)(currentAbsoluteZ - newOffset);
            for (int i=0;i<MotionLegCount;i++)
            {
                var leg=motionLegs[i]; var start=leg.Start; var end=leg.End;
                start.z=(float)(previousAbsoluteZ+(currentAbsoluteZ-previousAbsoluteZ)*leg.StartTime-newOffset);
                end.z=(float)(previousAbsoluteZ+(currentAbsoluteZ-previousAbsoluteZ)*leg.EndTime-newOffset);
                motionLegs[i]=new MotionLeg(start,end,leg.StartTime,leg.EndTime);
            }
            // Teleport cancels the pending MovePosition and resets any Rigidbody interpolation history.
            var interpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            HoldPose();
            body.interpolation = interpolation;
        }

        public void CommitPose() => body.MovePosition(currentPosition);

        internal void HoldPose()
        {
            body.position = currentPosition;
            body.rotation = Quaternion.identity;
            transform.SetPositionAndRotation(currentPosition, Quaternion.identity);
        }
    }
}
