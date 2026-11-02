import sys
sys.dont_write_bytecode=True
from Task11Tools import *
assert not evaluate('return UnityEditor.EditorApplication.isPlaying;')
try:
    command(['console','clear'])
    command(['editor','set_auto_tick','--enable','true','--interval_ms','16'])
    command(['editor.playmode','start']);settle()
    save('visual-ticking.json',command(['job','wait','--condition','{"member":"UnityEngine.Time.frameCount","op":"changed"}','--','--timeout','10']))
    r=command(['script','run','--file','Docs/Setup/Task11Visual.cs','--entry','NeonTunnelRunner.Task11Visual.Run','--','--timeout','150'],timeout=170)
    save('visual-route.json',r)
    assert r['data']['result'].get('success'),r['data']['result']
    assert r['data']['result']['result']['passed']
    print('PASS: saved scene captures and measured lighting',flush=True)
    save('console-after-visual.json',command(['console','get','--tail','100','--level','warn']))
finally:
    if evaluate('return UnityEditor.EditorApplication.isPlaying;'):command(['editor.playmode','stop']);settle()
    command(['editor','set_auto_tick','--enable','false'])
