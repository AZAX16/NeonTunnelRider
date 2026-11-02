"""Pinned local Editor helpers; external to all Unity/player assemblies."""
import json
import subprocess
import sys
sys.dont_write_bytecode = True
from Task10Tests import command, PROJECT, CLI, EVIDENCE

def evaluate(code):
    r=command(['script','eval','--code',code])['data']['result']
    if r.get('success') is False:
        raise RuntimeError(r)
    return r['result']

def settle():
    r=subprocess.run([str(CLI),'status','--until-ready','--timeout','45','--project-path',str(PROJECT),'--format','json'],capture_output=True,text=True,timeout=55)
    if r.returncode or not json.loads(r.stdout)['success']: raise RuntimeError(r.stdout)

def save(name, result):
    (EVIDENCE/name).write_text(json.dumps(result,indent=2)+'\n')

if __name__=='__main__':
    settle()
    if sys.argv[1]=='setup':
        save('setup.json',command(['script','run','--file','Docs/Setup/Task10Setup.cs','--entry','NeonTunnelRunner.Task10Setup.Configure']))
    elif sys.argv[1]=='audit':
        save('audit-final.json',command(['script','run','--file','Docs/Setup/Task10Setup.cs','--entry','NeonTunnelRunner.Task10Setup.Inspect']))
    else:
        raise ValueError(sys.argv)
