using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Separate counter-based cosmetic stream; one pending launch, no tunnel backlog.</summary>
    [DisallowMultipleComponent]
    public sealed class FireworkDirector : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private RoadManager roads;
        [SerializeField] private WorldOrigin origin;
        [SerializeField] private ChaseCamera chaseCamera;
        [SerializeField] private VfxController effects;
        [SerializeField] private PresentationConfig configuration;
        private int seed;
        private long sequence;
        public float Remaining { get; private set; }
        public bool Pending => Remaining<=0f;
        public int Launches { get; private set; }
        public double LastLaunchTime { get; private set; }
        public Vector3 LastLaunchPosition { get; private set; }
        internal bool ValidateRequired(GameManager owner,GameplayLoop loop,VfxController controller,PresentationConfig config,out string error)
        {
            error=null;
            if(!isActiveAndEnabled || gameManager!=owner || roads!=loop.Roads || origin!=loop.Origin || chaseCamera!=loop.Camera || effects!=controller || configuration!=config)
            {error="Assign the enabled firework scheduler and its shared road/camera/origin/config dependencies.";return false;}
            return true;
        }
        internal void ResetRun(int runSeed)
        { seed=runSeed; sequence=0; Launches=0; LastLaunchTime=0; LastLaunchPosition=Vector3.zero; Remaining=Interval(configuration.FirstFireworkInterval); }
        private uint Sample(ulong counter) => WorldRandom.Sample(seed,sequence,0x66697265776f726bUL,counter);
        private float Interval(Vector2 range) => Mathf.Lerp(range.x,range.y,Sample(0)/(float)uint.MaxValue);
        internal void Step(float dt)
        {
            if(gameManager.State!=GameState.Playing) return;
            Remaining=Mathf.Max(0f,Remaining-dt);
            if(!Pending || effects.ActiveFireworks>=configuration.MaximumVisibleFireworks) return;
            if(!TryGetLaunch(out var position)) return;
            if(effects.TryPlay(PoolKey.Firework,position,(double)position.z+origin.Offset,(int)Sample(2),sequence))
            {
                LastLaunchPosition=position; LastLaunchTime=gameManager.Session.Snapshot.ElapsedTime;
                Launches++; sequence++; Remaining=Interval(configuration.FireworkInterval);
            }
            else
            {
                // Optional miss is consumed, never retried every tick or accumulated as a volley.
                sequence++; Remaining=Interval(configuration.FireworkInterval);
            }
        }
        internal bool TryGetLaunch(out Vector3 position)
        {
            var s=gameManager.Session.Snapshot;
            float side=(Sample(1)&1)==0 ? -1f : 1f;
            float forward=Mathf.Max(100f,s.ActualSpeed*2.4f+40f);
            position=new Vector3(side*16f,10f,(float)(s.Distance+forward-origin.Offset));
            // Verify the actual covered road between camera, launch and future flight corridor.
            // A roof anywhere on this sightline blocks a launch, even with the player in open air.
            double from=chaseCamera.ViewCamera.transform.position.z+origin.Offset;
            if(!roads.IsOpenSkySpan(from,s.Distance+forward+20d)) return false;
            var camera=chaseCamera.ViewCamera;
            Vector3 a=camera.WorldToViewportPoint(position);
            Vector3 b=camera.WorldToViewportPoint(position+Vector3.up*11f-Vector3.forward*(s.ActualSpeed*2.2f));
            return a.z>0 && b.z>0 && a.x>.12f && a.x<.88f && a.y>.58f &&
                b.x>.12f && b.x<.88f && b.y<.94f;
        }
    }
}
