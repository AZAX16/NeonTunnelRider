using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>One captured lease owns all local-space particles and their persistent trails.
    /// Simulation is explicit: no player-loop emission, stop callbacks or wall-clock timers.</summary>
    public sealed class PooledVfx : PooledObject
    {
        [SerializeField] private ParticleSystem primary;
        [SerializeField] private ParticleSystem[] systems;
        [SerializeField] private ParticleSystem colorfulBurst;
        private Vector3[] positions, scales;
        private Quaternion[] rotations;
        private float completionNotBefore, quietTime, trailTail;
        private ulong lease;
        private bool started;
        public float Age { get; private set; }
        public ParticleSystem Primary => primary;
        public int ParticleCount
        { get { int count=0; foreach(var system in systems) count+=system.particleCount; return count; } }
        // Native IsAlive remains true on manually paused subemitters in this pinned Editor.
        // Poll all particle counts behind a conservative graph horizon, then a full trail tail.
        public bool HasLiveState => started && (Age < completionNotBefore || ParticleCount>0 || quietTime<trailTail);
        private static float Horizon(ParticleSystem system)
        {
            var main=system.main;
            float lifetime=main.startLifetime.constantMax;
            float tail=system.trails.enabled ? system.trails.lifetime.constantMax*Mathf.Max(1f,lifetime) : 0f;
            float child=0f; var sub=system.subEmitters;
            for(int i=0;i<sub.subEmittersCount;i++) child=Mathf.Max(child,Horizon(sub.GetSubEmitterSystem(i)));
            return main.startDelay.constantMax+main.duration+lifetime+tail+child+.04f;
        }
        public override void OnPrewarm()
        {
            positions=new Vector3[systems.Length]; scales=new Vector3[systems.Length]; rotations=new Quaternion[systems.Length];
            for(int i=0;i<systems.Length;i++)
            {
                var t=systems[i].transform;positions[i]=t.localPosition;scales[i]=t.localScale;rotations[i]=t.localRotation;
                if(systems[i].trails.enabled) trailTail=Mathf.Max(trailTail,systems[i].trails.lifetime.constantMax*Mathf.Max(1f,systems[i].main.startLifetime.constantMax));
            }
            completionNotBefore=Horizon(primary);
            // Native particle/trail buffers are exercised once during Menu prewarm.
            primary.Play(false);
            for(float t=0;t<completionNotBefore;t+=.02f) primary.Simulate(.02f,true,false,false);
            Clear();
        }
        public override void OnRent(SpawnContext context)
        {
            Clear(); lease=ActivationId; Age=quietTime=0f;
            uint seed=WorldRandom.Sample(context.Seed,context.SpawnSequence,0x766678UL);
            for(int i=0;i<systems.Length;i++)
            {
                systems[i].useAutoRandomSeed=false;
                systems[i].randomSeed=(seed+(uint)i*7919u)|1u;
            }
            if(colorfulBurst)
            {
                var main=colorfulBurst.main;
                float hue=(seed%360)/360f;
                Color a=Color.HSVToRGB(hue,1f,1f), b=Color.HSVToRGB((hue+.16f)%1f,1f,1f);
                a.a=b.a=1f; main.startColor=new ParticleSystem.MinMaxGradient(a,b);
            }
        }
        internal void Begin()
        {
            if(!IsRented || started) return;
            started=true;
            primary.Play(false);
            Advance(.02f); // A real, readable initial state also exists on a terminal contact frame.
        }
        internal void Advance(float dt)
        {
            if(!IsRented || !started || dt<=0f) return;
            // Stable sub-emitter timing even when a fixture/slow frame supplies a coarse delta.
            while(dt>0f && IsRented)
            {
                float step=Mathf.Min(.02f,dt);dt-=step;
                primary.Simulate(step,true,false,false); primary.Pause(true); Age+=step;
                quietTime=ParticleCount==0 ? quietTime+step : 0f;
                if(!HasLiveState) ReturnToPool(lease);
            }
        }
        public override void OnReturn() { Clear(); lease=0; Age=0f; }
        private void Clear()
        {
            started=false;
            // Clearing every child removes trails and death-subemitter residue, including paused state.
            for(int i=0;i<systems.Length;i++)
            {
                systems[i].Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                if(positions!=null)
                {var t=systems[i].transform;t.localPosition=positions[i];t.localRotation=rotations[i];t.localScale=scales[i];}
            }
        }
    }
}
