"""Short scene/audio/allocation diagnostic. No soak or full performance certification."""
import sys,json,subprocess,datetime,wave,time,os
from pathlib import Path
import Task14Tools as u
from Task15Run import preserve_player_best
out=u.PROJECT/'Evidence/Task15B';out.mkdir(exist_ok=True);u.EVIDENCE=out
preserve_player_best(out)
state=u.evaluate('return new { enabled=UnityEngine.Profiling.Profiler.enabled,binary=UnityEngine.Profiling.Profiler.enableBinaryLog,path=UnityEngine.Profiling.Profiler.logFile,stacks=UnityEngine.Profiling.Profiler.enableAllocationCallstacks };')
u.save('profiler-before.json',state)
recorder=None;raw=None
try:
 u.command(['editor','set_auto_tick','--enable','true'])
 if not u.evaluate('return UnityEditor.EditorApplication.isPlaying;'):
  u.command(['editor.playmode','start']);u.settle();time.sleep(8)
 if not u.evaluate('return UnityEditor.EditorApplication.isPlaying && NeonTunnelRunner.GameManager.Instance && NeonTunnelRunner.GameManager.Instance.State==NeonTunnelRunner.GameState.Menu;'):raise RuntimeError('Wait for live Menu after domain reload')
 sink=subprocess.check_output(['pactl','get-default-sink'],text=True).strip()
 raw=(out/'staged-mixed-output.raw').open('wb');started=datetime.datetime.now(datetime.timezone.utc).isoformat()
 recorder=subprocess.Popen(['parec','--device='+sink+'.monitor','--format=s16le','--rate=44100','--channels=2'],stdout=raw)
 result=u.run('Docs/Setup/Task15BVisual.cs','NeonTunnelRunner.Task15BVisual.Run',120)
 u.save('staged-visual-result.json',result)
 traffic=u.run('Docs/Setup/Task15BTrafficVisual.cs','NeonTunnelRunner.Task15BTrafficVisual.Run',90)
 u.save('staged-traffic-visual-result.json',traffic)
 u.save('warning-visual-result.json',u.run('Docs/Setup/Task15BWarningVisual.cs','NeonTunnelRunner.Task15BWarningVisual.Run',45))
finally:
 if recorder is not None:recorder.terminate();recorder.wait(timeout=5);raw.close()
 try:u.command(['editor.playmode','stop']);u.settle()
 finally:
  u.evaluate('UnityEngine.Profiling.Profiler.enabled=false; UnityEngine.Profiling.Profiler.enableBinaryLog='+str(state['binary']).lower()+';UnityEngine.Profiling.Profiler.logFile='+json.dumps(state['path'])+';UnityEngine.Profiling.Profiler.enableAllocationCallstacks='+str(state['stacks']).lower()+';UnityEngine.Profiling.Profiler.enabled='+str(state['enabled']).lower()+';return true;')
 if (out/'staged-mixed-output.raw').exists():
  data=(out/'staged-mixed-output.raw').read_bytes()
  with wave.open(str(out/'staged-mixed-output.wav'),'wb') as wav:wav.setnchannels(2);wav.setsampwidth(2);wav.setframerate(44100);wav.writeframes(data)
  (out/'staged-mixed-output.raw').unlink()
  u.save('staged-capture-origin.json',dict(started=started,finished=datetime.datetime.now(datetime.timezone.utc).isoformat(),sink=sink,seconds=len(data)/176400,description='Physical analog sink monitor; controlled close passes in saved Game, four current viewports, short focused Editor allocation capture. Listening acceptance separate.'))
