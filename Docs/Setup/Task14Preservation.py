"""Protect all prior asset GUIDs, tuning, packages and gameplay/presentation composition."""
import json,re,hashlib,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE='16fa7cc'
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
def docs(data):return {int(i):(int(k),body) for k,i,body in re.findall(r'--- !u!(\d+) &(-?\d+)\n(.*?)(?=--- !u!|\Z)',data,re.S)}
def main():
 changed=[];n=0
 allowed={'Assets/NeonTunnelRunner/Scripts/Editor/NeonTunnelRunner.Editor.asmdef','Assets/NeonTunnelRunner/Tests/PlayMode/NeonTunnelRunner.PlayModeTests.asmdef'}
 for p in git('ls-tree','-r','--name-only',BASE).decode().splitlines():
  protected=p.endswith(('.meta','.asmdef')) or p.startswith(('Packages/','ProjectSettings/','Preproduction/','Assets/NeonTunnelRunner/Input/','Assets/NeonTunnelRunner/Materials/','Assets/NeonTunnelRunner/Meshes/','Assets/NeonTunnelRunner/Textures/','Assets/NeonTunnelRunner/Configs/','Assets/NeonTunnelRunner/Settings/','Assets/NeonTunnelRunner/Prefabs/')) or p=='.gitignore'
  if protected and p not in allowed:
   n+=1
   if (ROOT/p).read_bytes()!=git('show',BASE+':'+p):changed.append(p)
 p='Assets/NeonTunnelRunner/Scenes/Game.unity';before=docs(git('show',BASE+':'+p).decode());after=docs((ROOT/p).read_text());scene=0
 for i,(kind,body) in before.items():
  assert i in after,(p,i)
  if kind in [114,1001,54,65,20,108,81]:
   new=re.sub(r'^  (hud|menus|uiModule):.*\n','',after[i][1],flags=re.M)
   assert new==body,(p,i,kind);scene+=1
 missing=[];duplicates=[];guids={}
 for p in (ROOT/'Assets').rglob('*'):
  if p.suffix!='.meta' and not p.name.startswith('.') and not p.with_name(p.name+'.meta').exists():missing.append(str(p.relative_to(ROOT)))
 for p in (ROOT/'Assets').rglob('*.meta'):
  m=re.search(r'^guid: (\S+)$',p.read_text(),re.M)
  if m:
   if m[1] in guids:duplicates.append([guids[m[1]],str(p.relative_to(ROOT))])
   guids[m[1]]=str(p.relative_to(ROOT))
 ignore=(ROOT/'.gitignore').read_bytes();report=dict(base=BASE,protectedFiles=n,changedProtected=changed,protectedSceneComponents=scene,missingMetas=missing,duplicateGuids=duplicates,ignoreSha256=hashlib.sha256(ignore).hexdigest(),ignoreMatchesAssignment=ignore==(ROOT/'Preproduction/source/assignment.gitignore').read_bytes(),approvedConcurrentChange='RunnerMixer Master snapshot +4.552889 dB retained at user request')
 report['passed']=not changed and not missing and not duplicates and report['ignoreMatchesAssignment'];(ROOT/'Evidence/Task14/preservation.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2));assert report['passed']
if __name__=='__main__':main()
