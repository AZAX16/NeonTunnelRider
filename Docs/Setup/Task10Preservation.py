"""Read-only preservation audit against the inspected Task 09 checkpoint."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
BASE = 'd41f4f6'

def git(*args):
    return subprocess.check_output(['git', *args], cwd=ROOT)

def main():
    protected = []
    changed = []
    for name in git('ls-tree', '-r', '--name-only', BASE).decode().splitlines():
        if (name.endswith('.meta') or name.startswith(('Packages/', 'Preproduction/')) or
            (name.startswith('ProjectSettings/') and name != 'ProjectSettings/TagManager.asset') or
            name.startswith('Assets/NeonTunnelRunner/Configs/') or name.endswith('.asmdef') or name.startswith('Assets/NeonTunnelRunner/Prefabs/') or
            name.startswith(('Assets/NeonTunnelRunner/Input/', 'Assets/NeonTunnelRunner/Settings/', 'Assets/Settings/')) or name in [
                '.gitignore', 'Assets/NeonTunnelRunner/Configs/World.asset', 'Assets/NeonTunnelRunner/Configs/Gameplay.asset']):
            if name == 'Assets/NeonTunnelRunner/Prefabs/Player/PlayerHovercar.prefab':
                continue  # Explicit Task10 visual references; collider/body data separately audited.
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
    prefab = 'Assets/NeonTunnelRunner/Prefabs/Player/PlayerHovercar.prefab'
    references = ('  player: {fileID: 482812753612654963}\n'
                  '  configuration: {fileID: 11400000, guid: 8cc0bd840e8337a3fbe4656ff60cee73, type: 2}\n'
                  '  visualPivot: {fileID: 4465797103070491600}\n')
    prefab_only_references = (ROOT/prefab).read_text().replace(references, '', 1).encode() == git('show', BASE+':'+prefab)
    report = {'playerPrefabOnlyVisualReferences': prefab_only_references, 'base': git('rev-parse', BASE).decode().strip(), 'protectedCount': len(protected),
              'changedProtected': changed, 'missingMetas': missing, 'duplicateGuids': duplicates,
              'ignoreSha256': hashlib.sha256((ROOT/'.gitignore').read_bytes()).hexdigest(),
              'ignoreMatchesAssignment': (ROOT/'.gitignore').read_bytes() == assignment.read_bytes(),
              'passed': not changed and not missing and not duplicates and prefab_only_references}
    (ROOT/'Evidence/Task10/preservation.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(report, indent=2))
    assert report['passed'] and report['ignoreMatchesAssignment']

if __name__ == '__main__':
    main()
