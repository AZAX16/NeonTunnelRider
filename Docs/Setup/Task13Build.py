"""Build the current saved Game with a one-build, isolated audio fixture."""
import sys,shutil,subprocess,json,hashlib,time
sys.dont_write_bytecode=True
from Task13Tools import *
def main():
    assert not evaluate('return UnityEditor.EditorApplication.isPlaying;')
    source=PROJECT/'Docs/Setup/Task13PlayerProbe.cs';target=PROJECT/'Assets/NeonTunnelRunner/Scripts/Task13PlayerProbe.cs'
    if target.exists():raise RuntimeError('Temporary fixture path already exists; inspect first')
    files={str(p.relative_to(PROJECT)):hashlib.sha256(p.read_bytes()).hexdigest() for base in ['Assets','Packages','ProjectSettings'] for p in sorted((PROJECT/base).rglob('*')) if p.is_file()}
    record={'baseCommit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=PROJECT,text=True).strip(),'utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),'fixtureSha256':hashlib.sha256(source.read_bytes()).hexdigest(),'productionFiles':files,'scope':'Current Game/production audio + temporary NTR_AUDIO_VERIFY fixture; not final delivery'}
    save('build-inputs.json',record)
    try:
        shutil.copy2(source,target)
        evaluate('UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);return true;')
        # Wait outside Pipeline while the new fixture causes assembly/domain reload.
        time.sleep(10)
        settle()
        state=evaluate('return new {compiling=UnityEditor.EditorApplication.isCompiling,updating=UnityEditor.EditorApplication.isUpdating,failed=UnityEditor.EditorUtility.scriptCompilationFailed};')
        if state['compiling'] or state['updating'] or state['failed']:raise RuntimeError(state)
        save('build.json',run('Docs/Setup/Task13Build.cs','NeonTunnelRunner.Task13Build.Run',600))
        result=json.loads((EVIDENCE/'build.json').read_text())['data']['result']['result']
        assert result['result']=='Succeeded' and result['totalErrors']==0,result
        print({k:v for k,v in result.items() if k!="steps"},flush=True)
    finally:
        target.unlink(missing_ok=True);target.with_suffix('.cs.meta').unlink(missing_ok=True)
        evaluate('UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);return true;')
        time.sleep(10)
        settle()
if __name__=='__main__': main()
