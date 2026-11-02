"""Pinned Editor automation for Task11. Never included in player assemblies."""
import json,subprocess,sys
sys.dont_write_bytecode=True
from Task11Tests import command,CLI,PROJECT,EVIDENCE

def evaluate(code):
    r=command(['script','eval','--code',code])['data']['result']
    if not r.get('success'):raise RuntimeError(r)
    return r['result']
def save(name,result):
    (EVIDENCE/name).write_text(json.dumps(result,indent=2)+'\n')
def settle():
    r=subprocess.run([str(CLI),'status','--until-ready','--timeout','45','--project-path',str(PROJECT),'--format','json'],capture_output=True,text=True,timeout=55)
    if r.returncode or not json.loads(r.stdout)['success']:raise RuntimeError(r.stdout)
