using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace NeonTunnelRunner
{
    public static class Task15BDensity
    {
        static readonly BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
        public static object Run()
        {
            if(EditorApplication.isPlaying)throw new Exception("Idle Editor required");
            var current=AssetDatabase.LoadAssetAtPath<GameplayConfig>("Assets/NeonTunnelRunner/Configs/Gameplay.asset");
            var before=UnityEngine.Object.Instantiate(current); typeof(GameplayConfig).GetField("trafficDensityMultiplier",F).SetValue(before,1f);
            var go=new GameObject("15B density"); var planner=go.AddComponent<SpawnDirector>();
            typeof(SpawnDirector).GetField("configuration",F).SetValue(planner,AssetDatabase.LoadAssetAtPath<WorldConfig>("Assets/NeonTunnelRunner/Configs/World.asset"));
            var reset=typeof(SpawnDirector).GetMethod("ResetRun",F);
            var sample=(Func<int,long,ulong,ulong,uint>)Delegate.CreateDelegate(typeof(Func<int,long,ulong,ulong,uint>),typeof(SpawnDirector).Assembly.GetType("NeonTunnelRunner.WorldRandom").GetMethod("Sample",BindingFlags.Static|BindingFlags.NonPublic));
            var result=new List<object>();
            try {
                foreach(int context in new[]{0,1,2})
                {
                    long oldAccepted=0,newAccepted=0,oldMoving=0,newMoving=0,oldProposed=0,newProposed=0;
                    for(int pass=0;pass<2;pass++)
                    {
                        typeof(SpawnDirector).GetField("gameplay",F).SetValue(planner,pass==0 ? before : current);
                        for(int seed=0;seed<1000;seed++)
                        {
                            reset.Invoke(planner,new object[]{seed});
                            long first=(long)Math.Floor(context*3000d/480d),last=(long)Math.Floor((context*3000d+10000)/480d);
                            for(long block=first;block<=last;block++)
                            {
                                var plan=planner.PlanAt(block);
                                for(int row=0;row<6;row++)for(int lane=-1;lane<=1;lane++)
                                {
                                    if(!plan.TryGetCandidate(row,lane,out var candidate))continue;
                                    double playerZ=Math.Max(0,candidate.AbsolutePosition-300d);
                                    bool accepted; PoolKey key;
                                    if(pass==0)
                                    {
                                        oldProposed++;
                                        bool moving=(sample(seed,block,0x67616d65UL,(ulong)(64+row*3+lane+1))&1u)!=0;
                                        key=moving ? PoolKey.Moving : PoolKey.Stationary;
                                        accepted=PatternValidator.ValidateMovingEnvelope(planner,lane,candidate.AbsolutePosition,playerZ,moving?8f:0f,new Vector3(1.1f,.6f,2.2f),out _);
                                        if(!accepted && moving) { key=PoolKey.Stationary; accepted=PatternValidator.ValidateMovingEnvelope(planner,lane,candidate.AbsolutePosition,playerZ,0f,new Vector3(1.1f,.6f,2.2f),out _); }
                                        if(accepted){oldAccepted++;if(key==PoolKey.Moving)oldMoving++;}
                                    }
                                    else {newProposed++;accepted=planner.TrySelectTraffic(candidate,playerZ,out key,out _);if(accepted){newAccepted++;if(key==PoolKey.Moving)newMoving++;}}
                                }
                            }
                        }
                    }
                    result.Add(new{context,oldProposed,newProposed,oldAccepted,newAccepted,increase=(double)newAccepted/oldAccepted-1d,oldMoving,newMoving});
                }
                return new{passed=true,seeds="0..999",metres=10000,contexts=result,method="Comparable row seeds/distance, actual accepted planner envelopes; old &1 moving selection and unmultiplied occupancy reconstructed. Rear overtakers counted by dynamic native tests separately."};
            }finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(before);}
        }
    }
}
