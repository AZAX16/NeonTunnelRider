using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonTunnelRunner
{
    /// <summary>Detached effect registry and event observer. Never retains an interactable or changes rewards.</summary>
    [DisallowMultipleComponent]
    public sealed class VfxController : MonoBehaviour, IWorldOriginParticipant
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PoolService pools;
        [SerializeField] private WorldOrigin origin;
        [SerializeField] private Transform activeRoot;
        [SerializeField] private PresentationConfig configuration;
        [SerializeField] private FireworkDirector fireworks;
        [SerializeField] private ParticleSystem[] exhaust;
        // Capacities match the four bounded pools. Stale leases are never acted on.
        private readonly PooledVfx[] active=new PooledVfx[48];
        private readonly ulong[] tokens=new ulong[48];
        private struct Request { internal PoolKey Key; internal Vector3 Point; internal double AbsoluteZ; internal long Sequence; }
        private readonly Request[] requests=new Request[64];
        private int queued;
        private RunSession session, subscribed;
        private bool prepared, terminalFlush;
        public Transform OriginRoot => activeRoot;
        public int ActiveCount { get; private set; }
        public int ActiveFireworks { get; private set; }
        public ulong DroppedRequests { get; private set; }
        public ulong PlayedSparks { get; private set; }
        public ulong PlayedCoins { get; private set; }
        public ulong PlayedFuel { get; private set; }
        public int QueuedCount => queued;
        public FireworkDirector Fireworks => fireworks;
        public IReadOnlyList<ParticleSystem> Exhaust => exhaust; // Authored references, never replaced during play.
        public PooledVfx GetActive(int index) => active[index];
        internal bool ValidateRequired(GameplayLoop loop,out string error)
        {
            error=null;
            if(!isActiveAndEnabled || gameManager!=loop.Owner || pools!=gameManager.Pools || origin!=loop.Origin ||
                !activeRoot || activeRoot.position!=Vector3.zero || activeRoot.rotation!=Quaternion.identity || activeRoot.lossyScale!=Vector3.one ||
                activeRoot.IsChildOf(pools.StorageRoot) || !origin.HasParticipant(this) || !configuration || !fireworks || exhaust==null || exhaust.Length!=4)
            {error="Assign the enabled VFX observer, neutral detached root, config, scheduler, exhaust and one origin participant.";return false;}
            int total=0;
            foreach(var system in exhaust)
            {
                if(!system || !system.transform.IsChildOf(loop.Visuals.VisualPivot) || system.main.playOnAwake || system.main.simulationSpace!=ParticleSystemSimulationSpace.Local)
                {error="Assign four attached local-space exhaust systems with Play On Awake disabled.";return false;}
                total+=system.main.maxParticles;
            }
            if(total>160) {error="Exhaust exceeds its 160-particle budget.";return false;}
            for(int k=(int)PoolKey.Spark;k<=(int)PoolKey.Firework;k++)
            {
                if(!pools.TryGetCounters((PoolKey)k,out var c) || c.Capacity!=(k<10?16:8))
                {error="Assign the four bounded VFX pools (16/16/8/8).";return false;}
            }
            return fireworks.ValidateRequired(gameManager,loop,this,configuration,out error);
        }
        internal void Initialize(RunSession run)
        {
            CleanupRun(); session=run;
            foreach(var system in exhaust)
            {system.Simulate(1f,false,true,false);system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
        }
        internal void PrepareRun(int seed)
        {
            CleanupRun(); prepared=true; DroppedRequests=PlayedSparks=PlayedCoins=PlayedFuel=0;
            fireworks.ResetRun(seed); Subscribe();
            for(int i=0;i<exhaust.Length;i++)
            {
                exhaust[i].useAutoRandomSeed=false; exhaust[i].randomSeed=(uint)(i+1);
                exhaust[i].Play(false); exhaust[i].Pause(false);
            }
        }
        private void Subscribe()
        {
            if(!prepared || !isActiveAndEnabled || session==null || subscribed==session) return;
            Unsubscribe(); subscribed=session; subscribed.Damaged+=Damage; subscribed.Collected+=Collected;
        }
        private void Unsubscribe()
        { if(subscribed!=null) { subscribed.Damaged-=Damage; subscribed.Collected-=Collected; } subscribed=null; }
        private void OnEnable() => Subscribe();
        private void OnDisable() => CleanupRun();
        private bool Accept(Guid run) => prepared && session!=null && run==session.Snapshot.RunId &&
            gameManager.State==GameState.Playing && !gameManager.IsTransitioning;
        private void Damage(DamageEvent e) { if(Accept(e.RunId)) Queue(PoolKey.Spark,e.Encounter); }
        private void Collected(CollectedEvent e)
        { if(Accept(e.RunId) && (e.Kind==PoolKey.Coin || e.Kind==PoolKey.Fuel)) Queue(e.Kind==PoolKey.Coin?PoolKey.CoinBurst:PoolKey.FuelBurst,e.Encounter); }
        private void Queue(PoolKey key,Encounter e)
        {
            if(queued==requests.Length) { DroppedRequests++; return; }
            requests[queued++]=new Request { Key=key,Point=e.LocalPosition,AbsoluteZ=e.AbsoluteZ,Sequence=e.SpawnSequence };
        }
        internal void FlushRequests()
        {
            for(int i=0;i<queued;i++)
            {
                var r=requests[i]; var point=r.Point;
                // The event's z was local to its dispatch frame. AbsoluteZ survives deferred rebasing.
                point.z=(float)(r.AbsoluteZ-origin.Offset);
                TryPlay(r.Key,point,r.AbsoluteZ,session.Snapshot.Seed,r.Sequence);
            }
            Array.Clear(requests,0,queued); queued=0;
        }
        internal bool TryPlay(PoolKey key,Vector3 position,double absoluteZ,int seed,long sequence)
        {
            if(!prepared || !isActiveAndEnabled || (gameManager.State!=GameState.Playing && !terminalFlush) || ActiveCount==active.Length) { DroppedRequests++; return false; }
            if(key==PoolKey.Firework && ActiveFireworks>=configuration.MaximumVisibleFireworks) { DroppedRequests++; return false; }
            if(!pools.TryRent(new SpawnContext(key,position,Quaternion.identity,activeRoot,
                absoluteDistance:absoluteZ,seed:seed,spawnSequence:sequence),out var item)) { DroppedRequests++; return false; }
            if(!(item is PooledVfx effect)) { item.ReturnToPool(item.ActivationId); DroppedRequests++; return false; }
            active[ActiveCount]=effect; tokens[ActiveCount++]=effect.ActivationId;
            effect.Begin();
            if(key==PoolKey.Spark) PlayedSparks++;
            if(key==PoolKey.CoinBurst) PlayedCoins++;
            if(key==PoolKey.FuelBurst) PlayedFuel++;
            if(key==PoolKey.Firework) ActiveFireworks++;
            return true;
        }
        internal void Step(float dt)
        {
            if(!prepared || !isActiveAndEnabled || gameManager.State!=GameState.Playing) return;
            RefreshExhaust();
            foreach(var system in exhaust) { system.Simulate(dt,false,false,false); system.Pause(false); }
            for(int i=ActiveCount-1;i>=0;i--)
            {
                var effect=active[i];
                if(effect && effect.IsRented && effect.ActivationId==tokens[i]) effect.Advance(dt);
                if(!effect || !effect.IsRented || effect.ActivationId!=tokens[i])
                {
                    if(effect && effect.Key==PoolKey.Firework) ActiveFireworks--;
                    ActiveCount--; active[i]=active[ActiveCount]; tokens[i]=tokens[ActiveCount]; active[ActiveCount]=null; tokens[ActiveCount]=0;
                }
            }
            FlushRequests();
            // Terminal contacts get initial feedback, but cannot schedule another rocket.
            if(gameManager.Loop.Survival.EvaluateEnd()==GameOverReason.None) fireworks.Step(dt);
        }
        private void RefreshExhaust()
        {
            float u=Mathf.InverseLerp(0,configuration.NeonMaximumSpeed,session==null?0:session.Snapshot.ActualSpeed);
            for(int i=0;i<exhaust.Length;i++)
            {
                var emission=exhaust[i].emission; emission.rateOverTime=Mathf.Lerp(i%2==0?24:6,i%2==0?64:12,u);
                var main=exhaust[i].main; main.startSpeed=Mathf.Lerp(i%2==0?2:1,i%2==0?5:2,u);
            }
        }
        internal void ApplyState(GameState state)
        {
            if(state==GameState.Menu) { CleanupRun(); return; }
            RefreshExhaust();
            if(state==GameState.GameOver)
            {
                terminalFlush=true;
                try { FlushRequests(); } finally { terminalFlush=false; }
            }
            // All systems remain paused between explicit playing ticks; pause never clears feedback.
            foreach(var system in exhaust) system.Pause(false);
        }
        internal void CleanupRun()
        {
            prepared=false; Unsubscribe(); Array.Clear(requests,0,requests.Length); queued=0;
            for(int i=0;i<ActiveCount;i++) if(active[i]) active[i].ReturnToPool(tokens[i]);
            Array.Clear(active,0,active.Length); Array.Clear(tokens,0,tokens.Length); ActiveCount=ActiveFireworks=0;
            if(exhaust!=null) foreach(var system in exhaust) if(system) system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void ShiftOrigin(double newOffset,double delta)
        {
            // The neutral registry root itself stays at zero; each currently leased effect moves once.
            // Particle and trail buffers are local, so neither buffer needs a second translation.
            for(int i=0;i<ActiveCount;i++) if(active[i] && active[i].IsRented && active[i].ActivationId==tokens[i])
                active[i].transform.localPosition-=Vector3.forward*(float)delta;
        }
    }
}
