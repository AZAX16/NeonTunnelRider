"""Pinned local automation; no player dependency."""
import json, subprocess, sys
from pathlib import Path
sys.dont_write_bytecode=True
PROJECT=Path(__file__).resolve().parents[2]
CLI=Path.home()/'.local/bin/unity.previous'
EVIDENCE=PROJECT/'Evidence/Task12'
def command(args,timeout=60):
    p=subprocess.run([str(CLI),'command','--caller','plugin','--skill','unity-cli','--project-path',str(PROJECT),'--format','json','--timeout',str(timeout),*args],capture_output=True,text=True,timeout=timeout+15)
    d=json.loads(p.stdout)
    if p.returncode or not d['success']: raise RuntimeError(d)
    return d
def evaluate(code):
    d=command(['script','eval','--code',code])['data']['result']
    if not d.get('success'): raise RuntimeError(d)
    return d['result']
def run(file,entry,timeout=60):
    d=command(['script','run','--file',file,'--entry',entry,'--','--timeout',str(timeout)],timeout+15)
    if not d['data']['result'].get('success'): raise RuntimeError(d)
    return d
def save(name,value): (EVIDENCE/name).write_text(json.dumps(value,indent=2)+'\n')
def settle():
    p=subprocess.run([str(CLI),'status','--until-ready','--timeout','45','--project-path',str(PROJECT),'--format','json'],capture_output=True,text=True,timeout=55)
    if p.returncode or not json.loads(p.stdout)['success']:raise RuntimeError(p.stdout)
if __name__=='__main__':
    save('setup.json',run('Docs/Setup/Task12Setup.cs','NeonTunnelRunner.Task12Setup.Configure'))
