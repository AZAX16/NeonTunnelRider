"""Current normal development build: no autostart, temporary source, or extra define."""
import sys,subprocess,hashlib,time
sys.dont_write_bytecode=True
from Task14Tools import *
settle();assert not evaluate('return UnityEditor.EditorApplication.isPlaying;')
files={str(p.relative_to(PROJECT)):hashlib.sha256(p.read_bytes()).hexdigest() for base in ['Assets','Packages','ProjectSettings'] for p in sorted((PROJECT/base).rglob('*')) if p.is_file()}
save('build-inputs.json',{'baseCommit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=PROJECT,text=True).strip(),'utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),'productionFiles':files,'scope':'Normal current saved Game, Development Linux/Mono, no fixture or extra define'})
result=run('Docs/Setup/Task14Build.cs','NeonTunnelRunner.Task14Build.Run',600);save('build.json',result)
r=result['data']['result']['result'];assert r['result']=='Succeeded' and r['totalErrors']==0,r
print({k:v for k,v in r.items() if k!='steps'},flush=True)
