"""Real saved-scene overlay captures and action-event UI smoke; isolated Editor fixtures."""
import sys,hashlib,time,json
sys.dont_write_bytecode=True
from Task14Tools import *
def main():
 original=evaluate('return new {playing=UnityEditor.EditorApplication.isPlaying,enabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,options=(int)UnityEditor.EditorSettings.enterPlayModeOptions};')
 assert not original['playing']
 before={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (PROJECT/'Assets/NeonTunnelRunner/Configs').glob('*.asset')}
 report={'original':original,'sessions':[],'passed':False}
 try:
  for fast,width,height in [(False,1280,720),(True,960,720),(False,1920,1080),(True,2520,1080)]:
   label=('fast-' if fast else 'normal-')+f'{width}x{height}-'
   evaluate('UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload|UnityEditor.EnterPlayModeOptions.DisableSceneReload;UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(fast).lower()+';return true;')
   (EVIDENCE/'viewport.txt').write_text(f'{width}x{height}')
   viewport=run('Docs/Setup/Task14Viewport.cs','NeonTunnelRunner.Task14Viewport.Configure')
   if not (EVIDENCE/'viewport-original.json').exists():save('viewport-original.json',viewport)
   (EVIDENCE/'capture-prefix.txt').write_text(label)
   command(['console','clear']);command(['editor','set_auto_tick','--enable','true','--interval_ms','16'])
   command(['editor.playmode','start']);settle()
   result=run('Docs/Setup/Task14SceneSmoke.cs','NeonTunnelRunner.Task14SceneSmoke.Run',180)
   save(label+'smoke.json',result);assert result['data']['result']['result']['passed']
   report['sessions'].append(result['data']['result']['result']);save('play-sessions.json',report)
   save(label+'console.json',command(['console','get','--tail','100','--level','warn']))
   command(['editor.playmode','stop']);settle()
   assert evaluate('return NeonTunnelRunner.GameManager.Instance==null && UnityEngine.Time.timeScale==1f;')
   print('PASS',label,flush=True)
  after={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (PROJECT/'Assets/NeonTunnelRunner/Configs').glob('*.asset')}
  report.update(configBefore=before,configAfter=after,passed=before==after);assert report['passed']
 finally:
  if evaluate('return UnityEditor.EditorApplication.isPlaying;'):command(['editor.playmode','stop']);settle()
  evaluate('UnityEditor.EditorSettings.enterPlayModeOptions=(UnityEditor.EnterPlayModeOptions)'+str(original['options'])+';UnityEditor.EditorSettings.enterPlayModeOptionsEnabled='+str(original['enabled']).lower()+';return true;')
  command(['editor','set_auto_tick','--enable','false']);save('play-sessions.json',report)
if __name__=='__main__':main()
