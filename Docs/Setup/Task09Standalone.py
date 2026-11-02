"""Limited development build + three fresh Player processes, isolated preference key only."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import uuid
import time
sys.dont_write_bytecode = True
from Task09Tests import command, PROJECT, CLI, EVIDENCE

def save(name, value):
    (EVIDENCE / name).write_text(json.dumps(value, indent=2) + '\n')

def settle():
    # The pinned beta.13 compile reply can precede Pipeline's domain-reload restart.
    time.sleep(3)
    ready=subprocess.run([str(CLI),'status','--until-ready','--timeout','45','--project-path',str(PROJECT),'--format','json'],capture_output=True,text=True,timeout=55,check=False)
    if ready.returncode: raise RuntimeError(ready.stdout)
    command(['editor','status'])

def main():
    if '--reuse' in sys.argv:
        manifest=json.loads((EVIDENCE/'build-source-hashes.json').read_text())
        if not all((PROJECT/f).exists() and hashlib.sha256((PROJECT/f).read_bytes()).hexdigest()==digest for f,digest in manifest.items()):
            raise RuntimeError('Build source/config differs; rebuild rather than reusing this probe')
        run_processes()
        return
    staged = PROJECT / 'Assets/NeonTunnelRunner/Scripts/Persistence/Task09PlayerProbe.cs'
    if staged.exists():
        raise RuntimeError('Refusing to overwrite an existing probe source')
    if command(['script','eval','--code','return UnityEditor.EditorApplication.isPlaying;'])['data']['result']['result']:
        raise RuntimeError('Stop the Editor first')
    sources = list((PROJECT/'Assets/NeonTunnelRunner/Scripts').rglob('*.cs')) + list((PROJECT/'Assets/NeonTunnelRunner/Configs').glob('*.asset')) + [PROJECT/'Assets/NeonTunnelRunner/Scenes/Game.unity',PROJECT/'Packages/manifest.json',PROJECT/'Packages/packages-lock.json',PROJECT/'ProjectSettings/ProjectVersion.txt']
    manifest = {str(p.relative_to(PROJECT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}
    manifest['Docs/Setup/Task09PlayerProbe.cs']=hashlib.sha256((PROJECT/'Docs/Setup/Task09PlayerProbe.cs').read_bytes()).hexdigest()
    save('build-source-hashes.json',manifest)
    try:
        staged.write_text((PROJECT/'Docs/Setup/Task09PlayerProbe.cs').read_text())
        compile_result = subprocess.run([str(CLI),'recompile','--project-path',str(PROJECT),'--format','json'],capture_output=True,text=True,timeout=120,check=False)
        save('compile-probe.json',json.loads(compile_result.stdout))
        if compile_result.returncode: raise RuntimeError(compile_result.stdout)
        settle()
        print('Building Game with isolated probe (temporary source, one-build define).',flush=True)
        build=command(['script','run','--file','Docs/Setup/Task09Build.cs','--entry','NeonTunnelRunner.Task09Build.Run','--','--timeout','900'],timeout=930)
        save('persistence-build.json',build)
        result=build['data']['result']
        if not result.get('success') or result['result']['result']!='Succeeded': raise RuntimeError(result)
    finally:
        if staged.exists(): staged.unlink()
        meta=staged.with_name(staged.name+'.meta')
        if meta.exists(): meta.unlink()
        settle()
        cleanup=subprocess.run([str(CLI),'recompile','--project-path',str(PROJECT),'--format','json'],capture_output=True,text=True,timeout=120,check=False)
        save('compile-after-probe.json',json.loads(cleanup.stdout))
    run_processes()

def run_processes():
    executable=PROJECT/'Builds/Task09Persistence/NeonTunnelRunner.x86_64'
    key='NTR.Tests.Task09.Process.'+uuid.uuid4().hex
    report={'key':key,'executable':str(executable.relative_to(PROJECT)),'processes':[],'passed':False}
    for phase in ['establish','lower','verify']:
        log=EVIDENCE/f'process-{phase}.log'
        process=subprocess.Popen([str(executable),'-batchmode','-force-glcore','-screen-width','640','-screen-height','360','-screen-fullscreen','0','-logFile',str(log),'-ntr-probe',phase,'-ntr-key',key],cwd=PROJECT,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE,text=True)
        _,stderr=process.communicate(timeout=90)
        lines=[line for line in log.read_text().splitlines() if '[Task09Probe]' in line]
        passed=process.returncode==0 and any('[Task09Probe] PASS' in line for line in lines)
        report['processes'].append({'phase':phase,'pid':process.pid,'exitCode':process.returncode,'log':str(log.relative_to(PROJECT)),'probeLines':lines,'stderr':stderr,'passed':passed})
        save('standalone-processes.json',report)
        print(phase,process.pid,process.returncode,lines,flush=True)
        if not passed: raise RuntimeError('Player probe failed; inspect retained logs')
    report['passed']=True
    save('standalone-processes.json',report)

if __name__=='__main__': main()
