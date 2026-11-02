"""Native saved-scene VFX QA in both established reload modes; restore Editor settings."""
import sys,hashlib
sys.dont_write_bytecode=True
from Task12Tools import *

def main():
    original=evaluate('return new { playing=UnityEditor.EditorApplication.isPlaying, enabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled, options=(int)UnityEditor.EditorSettings.enterPlayModeOptions };')
    assert not original['playing']
    report={'original':original,'cycles':[],'passed':False}
    configs=list((PROJECT/'Assets/NeonTunnelRunner/Configs').glob('*.asset'))
    before={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in configs}
    viewport=run('Docs/Setup/Task12Viewport.cs','NeonTunnelRunner.Task12Viewport.Configure');save('viewport-setup.json',viewport)
    old=viewport['data']['result']['result']['oldIndex']
    try:
        for fast in [False,True]:
            evaluate('UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload|UnityEditor.EnterPlayModeOptions.DisableSceneReload;UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(fast).lower()+'; return true;')
            (EVIDENCE/'capture-prefix.txt').write_text('fast-' if fast else 'normal-')
            save(('fast-' if fast else 'normal-')+'reload-before-start.json',evaluate('return new { enabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled, options=(int)UnityEditor.EditorSettings.enterPlayModeOptions };'))
            assert evaluate('return UnityEditor.EditorSettings.enterPlayModeOptionsEnabled;')==fast
            command(['console','clear'])
            command(['editor','set_auto_tick','--enable','true','--interval_ms','16'])
            command(['editor.playmode','start']);settle()
            save(('fast-' if fast else 'normal-')+'ticking.json',command(['job','wait','--condition','{"member":"UnityEngine.Time.frameCount","op":"changed"}','--','--timeout','10']))
            r=run('Docs/Setup/Task12Visual.cs','NeonTunnelRunner.Task12Visual.Run',180);save(('fast-' if fast else 'normal-')+'visual.json',r)
            assert r['data']['result']['result']['passed']
            report['cycles'].append({'fast':fast,'result':r['data']['result']['result']})
            save(('fast-' if fast else 'normal-')+'console.json',command(['console','get','--tail','100','--level','warn']))
            command(['editor.playmode','stop']);settle()
            assert evaluate('return NeonTunnelRunner.GameManager.Instance==null && UnityEngine.Time.timeScale==1f;')
            print('PASS saved-scene QA; reloads enabled='+str(not fast),flush=True)
        after={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in configs}
        report.update({'configBefore':before,'configAfter':after,'passed':before==after})
        assert report['passed']
    finally:
        if evaluate('return UnityEditor.EditorApplication.isPlaying;'):command(['editor.playmode','stop']);settle()
        evaluate('UnityEditor.EditorSettings.enterPlayModeOptions=(UnityEditor.EnterPlayModeOptions)'+str(original['options'])+';UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(original['enabled']).lower()+';return true;')
        command(['editor','set_auto_tick','--enable','false'])
        code='''var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static;
var a=typeof(UnityEditor.Editor).Assembly;var t=a.GetType("UnityEditor.GameViewSizes");var singleton=typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance",flags).GetValue(null);var group=t.GetProperty("currentGroup",flags).GetValue(singleton);var custom=(System.Collections.IList)group.GetType().GetField("m_Custom",flags).GetValue(group);int removed=0;for(int i=custom.Count-1;i>=0;i--){var size=custom[i];if((string)size.GetType().GetProperty("baseText",flags).GetValue(size)=="Task12 verification"){custom.RemoveAt(i);removed++;}}t.GetMethod("Changed",flags).Invoke(singleton,null);var vt=a.GetType("UnityEditor.GameView");var view=UnityEditor.EditorWindow.GetWindow(vt);vt.GetProperty("selectedSizeIndex",flags).SetValue(view,OLD_INDEX);view.Repaint();return new{removed,selected=(int)vt.GetProperty("selectedSizeIndex",flags).GetValue(view)};'''.replace('OLD_INDEX',str(old))
        save('viewport-restored.json',evaluate(code));save('play-sessions.json',report)
if __name__=='__main__':main()
