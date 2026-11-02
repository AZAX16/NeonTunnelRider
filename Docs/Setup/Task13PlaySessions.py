"""Saved-scene audio and visual regressions; Editor-only fixtures, both reload modes."""
import sys,hashlib
sys.dont_write_bytecode=True
from Task13Tools import *
def main():
    original=evaluate('return new {playing=UnityEditor.EditorApplication.isPlaying,enabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,options=(int)UnityEditor.EditorSettings.enterPlayModeOptions};')
    assert not original['playing']
    configs=list((PROJECT/'Assets/NeonTunnelRunner/Configs').glob('*.asset'))
    before={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in configs}
    report={'original':original,'cycles':[],'passed':False}
    try:
        for fast in [False,True]:
            prefix='fast-' if fast else 'normal-'
            evaluate('UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload|UnityEditor.EnterPlayModeOptions.DisableSceneReload;UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(fast).lower()+';return true;')
            (EVIDENCE/'capture-prefix.txt').write_text(prefix)
            command(['console','clear']);command(['editor','set_auto_tick','--enable','true','--interval_ms','16'])
            command(['editor.playmode','start']);settle()
            result=run('Docs/Setup/Task13SceneSmoke.cs','NeonTunnelRunner.Task13SceneSmoke.Run',140)
            save(prefix+'scene-smoke.json',result);assert result['data']['result']['result']['passed']
            report['cycles'].append({'fast':fast,'result':result['data']['result']['result']})
            save(prefix+'console.json',command(['console','get','--tail','100','--level','warn']))
            command(['editor.playmode','stop']);settle()
            assert evaluate('return NeonTunnelRunner.GameManager.Instance==null && UnityEngine.Time.timeScale==1f;')
            print('PASS saved-scene audio, reloads enabled='+str(not fast),flush=True)
        after={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in configs}
        report.update(configBefore=before,configAfter=after,passed=before==after);assert report['passed']
    finally:
        if evaluate('return UnityEditor.EditorApplication.isPlaying;'):command(['editor.playmode','stop']);settle()
        evaluate('UnityEditor.EditorSettings.enterPlayModeOptions=(UnityEditor.EnterPlayModeOptions)'+str(original['options'])+';UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(original['enabled']).lower()+';return true;')
        command(['editor','set_auto_tick','--enable','false']);save('play-sessions.json',report)
if __name__=='__main__': main()
