"""Native roll/resume/reset images; leaves no active Play session."""
import sys
sys.dont_write_bytecode=True
from Task10Tools import command,evaluate,save,settle,EVIDENCE
import json

def main():
    assert not evaluate('return UnityEditor.EditorApplication.isPlaying;')
    index=json.loads((EVIDENCE/'viewport-setup.json').read_text())['data']['result']['result']['addedIndex']
    evaluate('var t=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"); var v=UnityEditor.EditorWindow.GetWindow(t); t.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(v,VIEWPORT_INDEX); v.Repaint(); return true;'.replace('VIEWPORT_INDEX',str(index)))
    try:
        command(['editor','set_auto_tick','--enable','true','--interval_ms','16'])
        command(['editor.playmode','start']); settle()
        result=command(['script','run','--file','Docs/Setup/Task10Motion.cs','--entry','NeonTunnelRunner.Task10Motion.Run','--','--timeout','30'])
        save('native-motion.json',result)
        assert result['data']['result']['success'] and result['data']['result']['result']['passed'],result
    finally:
        if evaluate('return UnityEditor.EditorApplication.isPlaying;'):
            command(['editor.playmode','stop']); settle()
        command(['editor','set_auto_tick','--enable','false'])

if __name__=='__main__': main()
