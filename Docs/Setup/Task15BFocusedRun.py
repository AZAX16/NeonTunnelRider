"""Focused real-frame cue-window and rear maneuver capture, under one short Play session."""
import sys,json,time
import Task14Tools as u
from Task15Run import preserve_player_best
out=u.PROJECT/'Evidence/Task15B';u.EVIDENCE=out
preserve_player_best(out)
try:
 u.command(['editor','set_auto_tick','--enable','true']);u.command(['editor.playmode','start']);u.settle();time.sleep(2)
 if not u.evaluate('return UnityEditor.EditorApplication.isPlaying && NeonTunnelRunner.GameManager.Instance && NeonTunnelRunner.GameManager.Instance.State==NeonTunnelRunner.GameState.Menu;'):raise RuntimeError('Expected live Menu after Play Mode reload')
 u.save('cue-window.json',u.run('Docs/Setup/Task15BCueWindow.cs','NeonTunnelRunner.Task15BCueWindow.Run',45))
 u.save('traffic-focused-visual.json',u.run('Docs/Setup/Task15BTrafficVisual.cs','NeonTunnelRunner.Task15BTrafficVisual.Run',60))
finally:
 u.command(['editor.playmode','stop']);u.settle()
