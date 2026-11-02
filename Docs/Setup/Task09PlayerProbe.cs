#if NTR_PERSISTENCE_VERIFY
using System;
using System.Collections;
using UnityEngine;
namespace NeonTunnelRunner
{
    // Copied temporarily into Assets ONLY for the limited verification build.
    // No controls, normal-player UI, preference identity changes or production-key access.
    internal static class Task09PlayerProbe
    {
        private static HighScoreStore store;
        private static string mode,key;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            var args=Environment.GetCommandLineArgs();
            mode=Value(args,"-ntr-probe"); key=Value(args,"-ntr-key");
            if(key==null || !key.StartsWith("NTR.Tests.Task09.Process.")) { Application.Quit(2); return; }
            store=new HighScoreStore(key);
            var gm=UnityEngine.Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            if(!gm) { Debug.LogError("[Task09Probe] No saved-scene composition before Awake."); Application.Quit(3); return; }
            gm.ConfigureHighScores(store);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Schedule() => new GameObject("Task09PersistenceProbe").AddComponent<Task09ProbeRunner>();

        internal static int Verify()
        {
            try
            {
                var gm=GameManager.Instance;
                if(gm==null || !ReferenceEquals(gm.HighScores,store)) throw new Exception("Isolated store was not composed.");
                int loaded=gm.Session.Snapshot.BestScore;
                if(mode=="establish" && loaded!=0) throw new Exception("Test key must be fresh.");
                if(mode!="establish" && loaded!=4321) throw new Exception("Fresh process lost flushed best.");
                if(!gm.StartRun()) throw new Exception("Saved Game failed StartRun.");
                gm.Session.RecordMovement(.02f,mode=="establish" ? 4321.25d : 123.25d);
                gm.Loop.Progression.RecomputeScore();
                gm.EndRun(GameOverReason.Fuel);
                if(store.IsDirty || new HighScoreStore(key).Load()!=4321) throw new Exception("Final lifecycle flush/lower run failed.");
                Debug.Log($"[Task09Probe] PASS mode={mode} pid={System.Diagnostics.Process.GetCurrentProcess().Id} loaded={loaded} score={gm.Session.Snapshot.Score} best={gm.Session.Snapshot.BestScore} key={key}");
                if(mode=="verify") { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
                return 0;
            }
            catch(Exception ex) { Debug.LogError("[Task09Probe] FAIL "+ex); return 4; }
        }
        private static string Value(string[] args,string name) { int i=Array.IndexOf(args,name); return i>=0 && i+1<args.Length ? args[i+1] : null; }
    }
    internal sealed class Task09ProbeRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(.2f);
            int result=Task09PlayerProbe.Verify();
            yield return new WaitForSecondsRealtime(.2f);
            Application.Quit(result);
        }
    }
}
#endif
