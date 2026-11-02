"""Verify Task11 visual replacement did not change gameplay authoring or existing identity."""
import hashlib,json,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE='397a8af'
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
def docs(data):return {int(id):(int(kind),body) for kind,id,body in re.findall(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)',data,re.S)}
def main():
    preserved=[];changed=[]
    allowed={'Assets/NeonTunnelRunner/Configs/Presentation.asset','Assets/NeonTunnelRunner/Settings/URP/PC_RPAsset.asset'}
    for path in git('ls-tree','-r','--name-only',BASE).decode().splitlines():
        if path.endswith('.meta') or path.startswith(('Packages/','ProjectSettings/','Preproduction/','Assets/NeonTunnelRunner/Input/')) or path.endswith('.asmdef') or path=='.gitignore' or (path.startswith(('Assets/NeonTunnelRunner/Configs/','Assets/NeonTunnelRunner/Settings/')) and path not in allowed):
            preserved.append(path)
            if (ROOT/path).read_bytes()!=git('show',BASE+':'+path):changed.append(path)
    prefabChecks=[]
    for path in (ROOT/'Assets/NeonTunnelRunner/Prefabs').rglob('*.prefab'):
        rel=str(path.relative_to(ROOT));before=docs(git('show',BASE+':'+rel).decode());after=docs(path.read_text());checked=0
        for id,(kind,body) in before.items():
            if kind in [54,65,114]:
                new=after.get(id,(None,''))[1]
                if kind==114:
                    new=re.sub(r'^  (leftHeadlight|rightHeadlight|underglowLight|underglowRenderer|streetlight):.*\n','',new,flags=re.M)
                assert new==body,(rel,id,kind)
                checked+=1
            if kind==4 and ('  m_Father: {fileID: 0}\n' in body or re.search(r'  m_Name: (Visual|CameraAnchor)\n',before.get(int(re.search(r'm_GameObject: \{fileID: (\d+)\}',body).group(1)),(0,''))[1])):
                new=after[id][1]
                for field in ['m_LocalPosition','m_LocalRotation','m_LocalScale']:
                    assert re.search(r'^  '+field+r':.*$',body,re.M).group()==re.search(r'^  '+field+r':.*$',new,re.M).group(),(rel,id,field)
                checked+=1
        prefabChecks.append({'path':rel,'protectedComponentsAndPoses':checked})
    # All existing scene behavior/owners and prefab overrides are unchanged; only new URP light data is added.
    path='Assets/NeonTunnelRunner/Scenes/Game.unity';before=docs(git('show',BASE+':'+path).decode());after=docs((ROOT/path).read_text())
    sceneChecked=0
    for id,(kind,body) in before.items():
        if kind in [114,1001,54,65]:assert after[id][1]==body,(path,id);sceneChecked+=1
    missing=[];guids={};duplicates=[]
    for p in (ROOT/'Assets').rglob('*'):
        if not p.name.startswith('.') and p.suffix!='.meta' and not p.with_name(p.name+'.meta').exists():missing.append(str(p.relative_to(ROOT)))
    for p in (ROOT/'Assets').rglob('*.meta'):
        match=re.search(r'^guid: (\S+)$',p.read_text(),re.M)
        if match:
            guid=match.group(1)
            if guid in guids:duplicates.append([guids[guid],str(p.relative_to(ROOT))])
            guids[guid]=str(p.relative_to(ROOT))
    ignore=(ROOT/'.gitignore').read_bytes()
    report={'base':git('rev-parse',BASE).decode().strip(),'protectedFileCount':len(preserved),'changedProtected':changed,'prefabs':prefabChecks,'sceneProtectedComponents':sceneChecked,'missingMetas':missing,'duplicateGuids':duplicates,'ignoreSha256':hashlib.sha256(ignore).hexdigest(),'ignoreMatchesAssignment':ignore==(ROOT/'Preproduction/source/assignment.gitignore').read_bytes(),'passed':not changed and not missing and not duplicates}
    (ROOT/'Evidence/Task11/preservation.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2));assert report['passed'] and report['ignoreMatchesAssignment']
if __name__=='__main__':main()
