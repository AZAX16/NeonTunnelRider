"""Genuine supplementary Editor captures. No video, soak, or Windows acceptance claim."""
import json
import tempfile
import time
import hashlib
from pathlib import Path
import Task14Tools as u
from Task15Run import preserve_player_best

out = u.PROJECT / 'Evidence/Task16'
u.EVIDENCE = out
preserve_player_best(out)
original = u.evaluate('return new { playing=UnityEditor.EditorApplication.isPlaying, background=UnityEngine.Application.runInBackground, autoTick=UnityEditor.SessionState.GetBool("Unity.Pipeline.AutoTick.Enabled",true) };')
if original['playing']:
    raise RuntimeError('Requires stopped Editor; never take over an existing Play session')
viewport = None
with tempfile.TemporaryDirectory(prefix='neon-task16-capture-') as scratch:
    def adapted(file, entry, replacements, timeout=90):
        source = (u.PROJECT / file).read_text()
        for before, after in replacements.items():
            source = source.replace(before, after)
        executed = out / (Path(file).stem + '-executed.cs')
        executed.write_text(source)
        path = Path(scratch) / Path(file).name
        path.write_text(source)
        return u.run(str(path), entry, timeout)
    try:
        u.command(['editor', 'set_auto_tick', '--enable', 'true'])
        (out / 'viewport.txt').write_text('1920x1080\n')
        viewport = adapted('Docs/Setup/Task14Viewport.cs', 'NeonTunnelRunner.Task14Viewport.Configure',
                           {'Evidence/Task14': 'Evidence/Task16', 'Task14 verification': 'Task16 delivery capture'})
        u.save('capture-viewport.json', viewport)
        u.command(['editor.playmode', 'start']); u.settle(); time.sleep(2)
        u.evaluate('UnityEngine.Application.runInBackground=true; return true;')
        # Compile-only first: no persistent gameplay code or scene change.
        u.save('capture-dry-run.json', u.command(['script', 'run', '--file', 'Docs/Setup/Task16Capture.cs', '--dry_run', 'true']))
        u.save('capture-result.json', u.run('Docs/Setup/Task16Capture.cs', 'NeonTunnelRunner.Task16Capture.Run', 120))
        u.save('near-miss-capture-result.json', adapted(
            'Docs/Setup/Task15BBehindGateVisual.cs', 'NeonTunnelRunner.Task15BBehindGateVisual.Run',
            {'Evidence/Task15B/BehindGate/Visual': 'delivery/Screenshots/NearMiss'}, 60))
    finally:
        u.command(['editor.playmode', 'stop']); u.settle()
        u.evaluate('UnityEngine.Application.runInBackground=' + str(original['background']).lower() + '; return true;')
        if viewport is not None:
            index = viewport['data']['result']['result']['oldIndex']
            code = '''var f=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static;
var a=typeof(UnityEditor.Editor).Assembly; var t=a.GetType("UnityEditor.GameViewSizes");
var s=typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(t).GetProperty("instance",f).GetValue(null);
var g=t.GetProperty("currentGroup",f).GetValue(s); var c=(System.Collections.IList)g.GetType().GetField("m_Custom",f).GetValue(g);
for(int i=c.Count-1;i>=0;i--)if((string)c[i].GetType().GetProperty("baseText",f).GetValue(c[i])=="Task16 delivery capture")c.RemoveAt(i);
t.GetMethod("Changed",f).Invoke(s,null); var v=a.GetType("UnityEditor.GameView");var w=UnityEditor.EditorWindow.GetWindow(v);
v.GetProperty("selectedSizeIndex",f).SetValue(w,''' + str(index) + ''');w.Repaint();return true;'''
            u.evaluate(code)
        u.command(['editor', 'set_auto_tick', '--enable', str(original['autoTick']).lower()])

files = []
for p in sorted((u.PROJECT / 'delivery/Screenshots').rglob('*.png')):
    files.append({'path': str(p.relative_to(u.PROJECT / 'delivery/Screenshots')),
                  'sha256': hashlib.sha256(p.read_bytes()).hexdigest(), 'bytes': p.stat().st_size,
                  'origin': 'Unity Editor Play Mode on Linux; genuine unedited runtime screenshot',
                  'releaseSourceCommit': __import__('subprocess').check_output(['git', 'rev-parse', 'HEAD'], cwd=u.PROJECT, text=True).strip()})
(u.PROJECT / 'delivery/Screenshots/capture-manifest.json').write_text(json.dumps({
    'files': files, 'methodEvidence': ['Evidence/Task16/capture-result.json', 'Evidence/Task16/near-miss-capture-result.json'],
    'scope': '1920x1080 Editor supplementary evidence; controlled pool rentals, safe-route pre-roll and normal lane requests disclosed in method evidence. No Windows standalone capture.'
}, indent=2) + '\n')
print('Captured', len(files), 'genuine Editor screenshots')
