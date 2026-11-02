"""Native 960x720 Game view, plus restoration of temporary viewport settings."""
import sys
sys.dont_write_bytecode=True
from Task10Tools import command,evaluate,save,settle

def main():
    assert not evaluate('return UnityEditor.EditorApplication.isPlaying;')
    source=(__import__('pathlib').Path('Docs/Setup/Task10Viewport.cs').read_text()
            .replace('Task10Viewport','Task10Viewport4x3').replace('1280,720','960,720'))
    # Ephemeral script stays outside Assets and is removed after execution.
    path=__import__('pathlib').Path('Docs/Setup/Task10Viewport4x3.cs')
    try:
        path.write_text(source)
        setup=command(['script','run','--file',str(path),'--entry','NeonTunnelRunner.Task10Viewport4x3.Configure'])
        save('viewport-4x3-setup.json',setup)
        assert setup['data']['result']['success'],setup
        command(['editor','set_auto_tick','--enable','true','--interval_ms','16'])
        command(['editor.playmode','start']); settle()
        result=command(['script','run','--file','Docs/Setup/Task10Aspect.cs','--entry','NeonTunnelRunner.Task10Aspect.Run','--','--timeout','45'])
        save('aspect-4x3.json',result)
        assert result['data']['result']['success'],result
    finally:
        if evaluate('return UnityEditor.EditorApplication.isPlaying;'):
            command(['editor.playmode','stop']); settle()
        command(['editor','set_auto_tick','--enable','false'])
        path.unlink(missing_ok=True)

if __name__=='__main__': main()
