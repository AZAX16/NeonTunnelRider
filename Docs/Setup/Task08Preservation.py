"""Read-only preservation audit against the inspected Task 07 checkpoint."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
BASE = '9399136'

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def main():
    protected = []
    changed = []
    for name in git('ls-tree', '-r', '--name-only', BASE).decode().splitlines():
        if (name.endswith('.meta') or name.startswith(('Packages/', 'Preproduction/')) or
            (name.startswith('ProjectSettings/') and name != 'ProjectSettings/TagManager.asset') or
            name.endswith('.asmdef') or name.startswith('Assets/NeonTunnelRunner/Prefabs/') or
            name.startswith('Assets/NeonTunnelRunner/Input/') or name in [
                '.gitignore', 'Assets/NeonTunnelRunner/Configs/World.asset', 'Assets/NeonTunnelRunner/Configs/Gameplay.asset']):
            if name == 'Assets/NeonTunnelRunner/Prefabs/Player/PlayerHovercar.prefab':
                continue  # Task08 explicitly extends this prefab; old GUID is protected separately.
            protected.append(name)
            if (ROOT/name).read_bytes() != git('show', BASE+':'+name):
                changed.append(name)
    missing = []
    guids = {}
    duplicates = []
    for p in (ROOT/'Assets').rglob('*'):
        if p.name.startswith('.') or p.suffix == '.meta':
            continue
        if not p.with_name(p.name+'.meta').exists():
            missing.append(str(p.relative_to(ROOT)))
    for p in (ROOT/'Assets').rglob('*.meta'):
        match = re.search(r'^guid: (\S+)$', p.read_text(), re.M)
        if match:
            guid = match.group(1)
            if guid in guids:
                duplicates.append([guids[guid], str(p.relative_to(ROOT))])
            guids[guid] = str(p.relative_to(ROOT))
    assignment = ROOT/'Preproduction/source/assignment.gitignore'
    report = {'base': git('rev-parse', BASE).decode().strip(), 'protectedCount': len(protected),
              'changedProtected': changed, 'missingMetas': missing, 'duplicateGuids': duplicates,
              'ignoreSha256': hashlib.sha256((ROOT/'.gitignore').read_bytes()).hexdigest(),
              'ignoreMatchesAssignment': (ROOT/'.gitignore').read_bytes() == assignment.read_bytes(),
              'passed': not changed and not missing and not duplicates}
    (ROOT/'Evidence/Task08/preservation.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(report, indent=2))
    assert report['passed'] and report['ignoreMatchesAssignment']

if __name__ == '__main__':
    main()
