import argparse
import hashlib
import json
import pathlib
import re
import subprocess
from datetime import datetime, timezone

ROOT = pathlib.Path.cwd()
OUT = pathlib.Path(__file__).resolve().parent
EXPECTED = 'c735b75517ecf18563ce4bf269eb4fe158a5539f7d554823578a85eaec795f45'
COMMITS = ['e97572aba25d8bcff7e336c4bf6ef8b6e3c540fc', 'd9578524cfa2018b5f04ca7f86155fdae3de995b']

def digest(data):
    return hashlib.sha256(data).hexdigest()

def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')

def run(args, cwd=ROOT, data=None, allowed=(0,)):
    p = subprocess.run(args, cwd=cwd, input=data, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    record = {'time_utc': datetime.now(timezone.utc).isoformat(), 'cwd': str(cwd), 'argv': args, 'exit_code': p.returncode}
    with (OUT / 'commands.jsonl').open('a') as f:
        f.write(json.dumps(record) + '\n')
    if p.returncode not in allowed:
        raise RuntimeError(f'{args[:3]} exited {p.returncode}; inspect locally without exposing credentials')
    return p.stdout

def tree(repo=ROOT):
    rows = run(['git', 'ls-tree', '-rlz', 'HEAD'], cwd=repo).split(b'\0')
    files = {}
    for row in filter(None, rows):
        info, name = row.split(b'\t', 1)
        mode, kind, oid, size = info.split()
        files[name.decode()] = {'mode': mode.decode(), 'oid': oid.decode(), 'size': int(size)}
    return files

def snapshot():
    paths = set(run(['git', 'ls-files', '-co', '--exclude-standard', '-z']).decode().split('\0')) - {''}
    paths.update(str(p.relative_to(ROOT)) for p in (ROOT / 'Build/Windows').rglob('*') if p.is_file())
    paths.add('.git/info/exclude')
    values = {}
    for name in sorted(paths):
        p = ROOT / name
        if p.is_file():
            values[name] = {'sha256': digest(p.read_bytes()), 'bytes': p.stat().st_size}
    save('preservation-before.json', values)
    print(json.dumps({'preserved_file_count': len(values)}))

def inspect(repo=ROOT):
    files = tree(repo)
    names = set(files)
    missing = []
    folders = set()
    for name in names:
        if not name.startswith('Assets/'):
            continue
        if not name.endswith('.meta') and pathlib.PurePosixPath(name).name != '.gitkeep':
            if name + '.meta' not in names:
                missing.append(name)
        parent = pathlib.PurePosixPath(name).parent
        while str(parent) not in ('Assets', '.'):
            folders.add(str(parent))
            parent = parent.parent
    missing.extend(folder for folder in folders if folder + '.meta' not in names)
    orphan = [n for n in names if n.startswith('Assets/') and n.endswith('.meta') and n[:-5] not in names and n[:-5] not in folders]
    generated = [n for n in names if n.split('/')[0].lower() in {'library','temp','obj','build','builds','logs','usersettings','memorycaptures','recordings'} or n.endswith('.slnx')]
    essentials = ['Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','Assets/NeonTunnelRunner/Scenes/Game.unity','CREDITS.md','Docs/Environment.md','Docs/Verification/16-delivery.md','ImplementationReport.pdf','delivery/repository-link.txt']
    absent = [n for n in essentials if n not in names]
    ignored = run(['git','ls-files','-ci','--exclude-standard'], cwd=repo).decode().splitlines()
    hashes = {n: digest(run(['git','show','HEAD:' + n], cwd=repo)) for n in ['.gitignore','Preproduction/source/assignment.gitignore']}
    result = {'file_count':len(names), 'total_bytes':sum(v['size'] for v in files.values()), 'missing_metas':sorted(set(missing)), 'orphan_metas':sorted(orphan), 'generated_tracked':sorted(generated), 'tracked_ignored':ignored, 'missing_essentials':absent, 'gitignore_sha256':hashes, 'gitkeep_files':sorted(n for n in names if n.endswith('/.gitkeep')), 'largest_files':sorted([{'path':n, **v} for n,v in files.items()], key=lambda x:x['size'], reverse=True)[:25]}
    result['status'] = 'PASS' if not any([missing, orphan, generated, ignored, absent]) and all(v == EXPECTED for v in hashes.values()) else 'FAIL'
    return result

def preflight():
    commands = [
        ['git','status','--short'], ['git','branch','--show-current'], ['git','remote','-v'],
        ['git','log','--oneline','--decorate','-10'], ['git','rev-parse','HEAD'],
        ['git','rev-parse','e97572a'], ['git','rev-parse','d957852'],
        ['git','show','--stat','e97572a'], ['git','show','--stat','d957852'],
        ['git','diff-tree','--no-commit-id','--name-status','-r','e97572a'],
        ['git','diff-tree','--no-commit-id','--name-status','-r','d957852'],
        ['git','ls-files'], ['git','status','--short','--ignored'],
        ['git','check-ignore','-v','Library','Temp','Obj','Builds'],
        ['git','check-ignore','-v','Library/','Temp/','Obj/','Build/','Builds/','Logs/','UserSettings/','NeonTunnelRunner.slnx'],
        ['git','show','-s','--format=%H%n%P%n%s','e97572a','d957852'],
        ['sha256sum','.gitignore','Preproduction/source/assignment.gitignore'],
        ['git','diff','--cached','--stat']]
    for i, cmd in enumerate(commands):
        (OUT / f'preflight-{i:02}.txt').write_bytes(run(cmd))
    for commit in COMMITS:
        run(['git','merge-base','--is-ancestor',commit,'main'])
    result = inspect()
    result['working_gitignore_sha256'] = digest((ROOT / '.gitignore').read_bytes())
    result['slnx_local_exclusion'] = '/*.slnx' in (ROOT / '.git/info/exclude').read_text().splitlines()
    save('preflight.json', result)
    print(json.dumps(result, indent=2))

def secrets():
    patterns = {
        'private_key': rb'-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----',
        'github_token': rb'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})',
        'cloud_or_service_key': rb'\b(?:AKIA[0-9A-Z]{16}|AIza[0-9A-Za-z_-]{35}|sk-(?:proj-)?[A-Za-z0-9_-]{30,}|xox[baprs]-[A-Za-z0-9-]{20,})',
        'credential_url': rb'https?://[^\s/"<>]{1,100}:[^\s/@"<>]{3,100}@',
        'credential_assignment': rb'(?i)["\x27]?(?:api[_-]?key|access[_-]?token|refresh[_-]?token|client[_-]?secret|password|passwd)["\x27]?\s*[:=]\s*["\x27][^"\x27\r\n]{8,}["\x27]',
        'credential_flag': rb'(?i)--?(?:accessToken|refreshToken|password|client-secret)\s+[A-Za-z0-9._~+/-]{8,}',
        'jwt': rb'\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}',
    }
    objects = run(['git','rev-list','--objects','--all']).splitlines()
    proc = subprocess.Popen(['git','cat-file','--batch'], cwd=ROOT, stdin=subprocess.PIPE, stdout=subprocess.PIPE)
    findings = []
    scanned = 0
    blobs = 0
    largest = []
    for obj in objects:
        parts = obj.split(b' ',1)
        oid = parts[0]
        name = parts[1].decode(errors='replace') if len(parts)>1 else ''
        proc.stdin.write(oid + b'\n'); proc.stdin.flush()
        header = proc.stdout.readline().split()
        size = int(header[2]); content = proc.stdout.read(size); proc.stdout.read(1)
        if header[1] != b'blob':
            continue
        blobs += 1
        if size >= 1000000:
            largest.append({'path':name, 'oid':oid.decode(), 'bytes':size})
        if b'\0' in content[:8192]:
            continue
        scanned += 1
        for label, pattern in patterns.items():
            for m in re.finditer(pattern, content):
                findings.append({'path':name,'oid':oid.decode(),'line':content[:m.start()].count(b'\n')+1,'rule':label})
    proc.stdin.close(); proc.wait()
    value = {'scope':'All reachable Git objects, all history; text blobs scanned without printing matched values', 'objects':len(objects), 'blobs':blobs,'text_blobs_scanned':scanned,'findings':findings,'blobs_at_least_1MB':sorted(largest,key=lambda x:x['bytes'],reverse=True),'status':'PASS' if not findings else 'REVIEW'}
    save('sensitive-file-review.json', value)
    print(json.dumps(value,indent=2))

def verify(repo):
    repo = pathlib.Path(repo)
    result = inspect(repo)
    result['head'] = run(['git','rev-parse','HEAD'],cwd=repo).decode().strip()
    result['branch'] = run(['git','branch','--show-current'],cwd=repo).decode().strip()
    result['commit_reachability'] = {}
    for commit in COMMITS:
        run(['git','merge-base','--is-ancestor',commit,'HEAD'],cwd=repo)
        result['commit_reachability'][commit]='PASS'
    result['fsck'] = run(['git','fsck','--full'],cwd=repo).decode().strip()
    result['working_tree'] = run(['git','status','--porcelain=v1'],cwd=repo).decode()
    result['git_tree'] = run(['git','rev-parse','HEAD^{tree}'],cwd=repo).decode().strip()
    save('fresh-clone.json',result)
    print(json.dumps(result,indent=2))

def preservation():
    before = json.loads((OUT/'preservation-before.json').read_text())
    allowed = {'delivery/repository-link.txt'}
    changed = [n for n,v in before.items() if n not in allowed and (not (ROOT/n).is_file() or digest((ROOT/n).read_bytes())!=v['sha256'])]
    actual_allowed = [n for n in allowed if digest((ROOT/n).read_bytes()) != before[n]['sha256']]
    result={'status':'PASS' if not changed else 'FAIL','original_files_checked':len(before),'allowed_change_candidates':sorted(allowed),'actual_allowed_changes':actual_allowed,'unexpected_changed_or_missing':changed}
    save('preservation-after.json',result); print(json.dumps(result))

if __name__ == '__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('mode', choices=['snapshot','preflight','secrets','verify','preservation']); parser.add_argument('repository',nargs='?'); args=parser.parse_args()
    if args.mode=='verify': verify(args.repository)
    else: globals()[args.mode]()
