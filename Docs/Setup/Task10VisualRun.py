"""Capture native saved-scene framing and a second actual camera viewport."""
import sys
sys.dont_write_bytecode=True
from Task10Tools import command,evaluate,save,settle

def main():
    assert not evaluate('return UnityEditor.EditorApplication.isPlaying;')
    try:
        command(['editor','set_auto_tick','--enable','true','--interval_ms','16'])
        command(['editor.playmode','start']); settle()
        tick=command(['job','wait','--condition','{"member":"UnityEngine.Time.frameCount","op":"changed"}','--','--timeout','10'])
        save('visual-ticking.json',tick)
        result=command(['script','run','--file','Docs/Setup/Task10Visual.cs','--entry','NeonTunnelRunner.Task10Visual.Run','--','--timeout','180'],timeout=200)
        save('visual-route.json',result)
        assert result['data']['result']['success'] and result['data']['result']['result']['passed'],result
        save('console-after-visual.json',command(['console','get','--tail','100','--level','warn']))
    finally:
        if evaluate('return UnityEditor.EditorApplication.isPlaying;'):
            command(['editor.playmode','stop']); settle()
        command(['editor','set_auto_tick','--enable','false'])

if __name__=='__main__': main()
