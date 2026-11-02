"""Byte and serialized identity audit against the inspected Task11 checkpoint."""
import json,re,hashlib,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE='6fbb47c'
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
def docs(data):return {int(i):(int(k),body) for k,i,body in re.findall(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)',data,re.S)}
def main():
 protected=[];changed=[]
 allowed={'Assets/NeonTunnelRunner/Configs/Pools.asset','Assets/NeonTunnelRunner/Configs/Presentation.asset','Assets/NeonTunnelRunner/Settings/Volumes/GlobalVolumeProfile.asset'}
 for path in git('ls-tree','-r','--name-only',BASE).decode().splitlines():
  if path.endswith('.meta') or path.startswith(('Packages/','ProjectSettings/','Preproduction/','Assets/NeonTunnelRunner/Input/','Assets/NeonTunnelRunner/Materials/','Assets/NeonTunnelRunner/Meshes/','Assets/NeonTunnelRunner/Textures/','Evidence/Task09/Standalone/')) or path.endswith('.asmdef') or path=='.gitignore' or (path.startswith(('Assets/NeonTunnelRunner/Configs/','Assets/NeonTunnelRunner/Settings/')) and path not in allowed):
   protected.append(path)
   if (ROOT/path).read_bytes()!=git('show',BASE+':'+path):changed.append(path)
 prefab_checks=[]
 for path in git('ls-tree','-r','--name-only',BASE,'Assets/NeonTunnelRunner/Prefabs').decode().splitlines():
  if not path.endswith('.prefab'):continue
  before=docs(git('show',BASE+':'+path).decode());after=docs((ROOT/path).read_text());count=0
  for id,(kind,body) in before.items():
   if kind in [54,65,114]:assert after[id][1]==body,(path,id,kind);count+=1
   if kind==4:
    for field in ['m_LocalPosition','m_LocalRotation','m_LocalScale']:
     old=re.search(r'^  '+field+r':.*$',body,re.M);new=re.search(r'^  '+field+r':.*$',after[id][1],re.M)
     if old:assert old.group()==new.group(),(path,id,field)
    count+=1
  prefab_checks.append({'path':path,'protectedComponentsAndPoses':count})
 path='Assets/NeonTunnelRunner/Scenes/Game.unity';before=docs(git('show',BASE+':'+path).decode());after=docs((ROOT/path).read_text());count=0
 for id,(kind,body) in before.items():
  if kind not in [114,1001,54,65,20,108]:continue
  new=after[id][1]
  if kind==114:
   new=re.sub(r'^  vfx:.*\n','',new,flags=re.M)
   if '  additionalParticipants:' in new:
    new=re.sub(r'(  additionalParticipants:\n(?:  - .*\n){2})  - .*\n',r'\1',new)
  assert new==body,(path,id,kind);count+=1
 missing=[];guids={};duplicates=[]
 for p in (ROOT/'Assets').rglob('*'):
  if not p.name.startswith('.') and p.suffix!='.meta' and not p.with_name(p.name+'.meta').exists():missing.append(str(p.relative_to(ROOT)))
 for p in (ROOT/'Assets').rglob('*.meta'):
  match=re.search(r'^guid: (\S+)$',p.read_text(),re.M)
  if match:
   guid=match[1]
   if guid in guids:duplicates.append([guids[guid],str(p.relative_to(ROOT))])
   guids[guid]=str(p.relative_to(ROOT))
 ignore=(ROOT/'.gitignore').read_bytes();passed=not changed and not missing and not duplicates
 report={'base':git('rev-parse',BASE).decode().strip(),'protectedFileCount':len(protected),'changedProtected':changed,'prefabs':prefab_checks,'sceneProtectedComponents':count,'missingMetas':missing,'duplicateGuids':duplicates,'ignoreSha256':hashlib.sha256(ignore).hexdigest(),'ignoreMatchesAssignment':ignore==(ROOT/'Preproduction/source/assignment.gitignore').read_bytes(),'passed':passed}
 (ROOT/'Evidence/Task12/preservation.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2));assert passed and report['ignoreMatchesAssignment']
if __name__=='__main__':main()
