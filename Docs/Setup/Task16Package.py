"""Package every emitted Windows file and independently verify CRC, PE target and extraction.

Run after report generation: python3 -B Docs/Setup/Task16Package.py
Does not claim a Windows launch, repository publication or course upload.
"""
import hashlib
import json
import struct
import subprocess
import tempfile
import zipfile
from pathlib import Path, PurePosixPath

root = Path(__file__).resolve().parents[2]
archive = root / 'GD_Hw2_Alireza_Pooyan_400414039.zip'
evidence = root / 'Evidence/Task16'


def sha(path):
    digest = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()


def main():
    exe = root / 'Build/Windows/NeonTunnelRunner.exe'
    data = exe.read_bytes()
    pe = struct.unpack_from('<I', data, 0x3c)[0]
    assert data[:2] == b'MZ' and data[pe:pe + 4] == b'PE\0\0'
    assert struct.unpack_from('<H', data, pe + 4)[0] == 0x8664, 'Must be Windows AMD64 PE'
    assert (exe.parent / 'UnityPlayer.dll').is_file()
    assert (exe.parent / 'NeonTunnelRunner_Data/globalgamemanagers').is_file()
    assert (root / 'ImplementationReport.pdf').read_bytes().startswith(b'%PDF-')
    assert (root / '.gitignore').read_bytes() == (root / 'Preproduction/source/assignment.gitignore').read_bytes()
    sources = {}
    for p in sorted((root / 'Build/Windows').rglob('*')):
        if p.is_file():
            sources[str(p.relative_to(root))] = p
    metadata_path = sorted(evidence.glob('Build-*/build-metadata.json'))[-1]
    metadata = json.loads(metadata_path.read_text())
    assert metadata['result'] == 'Succeeded' and metadata['errors'] == 0
    assert metadata['target'] == 'StandaloneWindows64'
    assert not any(metadata[k] for k in ['development', 'connectProfiler', 'deepProfiling', 'scriptDebugging'])
    assert set(sources) == {f['path'] for f in metadata['files']}, 'Build inventory changed since Unity emitted it'
    for f in metadata['files']:
        assert sha(sources[f['path']]) == f['sha256'], f['path']
    for name in ['repository-link.txt', 'README-run.txt']:
        sources[name] = root / 'delivery' / name
    for name in ['ImplementationReport.pdf', 'CREDITS.md']:
        sources[name] = root / name
    for p in sorted((root / 'delivery/Screenshots').rglob('*')):
        if p.is_file() and p.suffix.lower() in {'.png', '.json', '.md', '.txt'}:
            sources['Screenshots/' + str(p.relative_to(root / 'delivery/Screenshots'))] = p
    assert len([n for n in sources if n.lower().endswith('.png')]) > 0
    assert not archive.exists(), 'Retain an existing archive before explicitly rebuilding'
    with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as z:
        for name, path in sources.items():
            z.write(path, name)
    # Independently compare every packaged path and byte to its actual input.
    extracted = Path(tempfile.mkdtemp(prefix='neon-task16-extracted-'))
    with zipfile.ZipFile(archive) as z:
        assert len(z.namelist()) == len(set(z.namelist()))
        for name in z.namelist():
            p = PurePosixPath(name)
            assert not p.is_absolute() and '..' not in p.parts and '\\' not in name and ':' not in name
        assert set(z.namelist()) == set(sources)
        assert z.testzip() is None
        z.extractall(extracted)
    for name, source in sources.items():
        assert sha(source) == sha(extracted / name), name
    result = subprocess.run(['python3', str(root / 'Preproduction/tools/verify_submission.py'), str(archive)],
                            capture_output=True, text=True)
    (evidence / 'supplied-archive-verifier.txt').write_text(result.stdout + result.stderr)
    assert result.returncode == 0, result.stdout + result.stderr
    digest = sha(archive)
    Path(str(archive) + '.sha256').write_text(digest + '  ' + archive.name + '\n')
    record = {
        'status': 'PASS', 'filename': archive.name, 'sha256': digest, 'bytes': archive.stat().st_size,
        'entries': len(sources), 'zipCrc': 'PASS', 'safePaths': 'PASS', 'exactEntryAndHashComparison': 'PASS',
        'extractionDirectory': str(extracted), 'extractionAndEveryFileHash': 'PASS',
        'peTarget': 'AMD64 (0x8664)', 'executableSha256': sha(exe),
        'suppliedVerifierExitCode': result.returncode,
        'nativeWindowsLaunch': 'BLOCKED: Linux host; student confirmed no Windows machine/VM',
        'courseUpload': 'PENDING: no authorized course session', 'postUploadDownload': 'PENDING',
        'files': [{'path': name, 'bytes': p.stat().st_size, 'sha256': sha(p)} for name, p in sources.items()]
    }
    (evidence / 'archive-verification.json').write_text(json.dumps(record, indent=2) + '\n')
    print(json.dumps({k: v for k, v in record.items() if k != 'files'}, indent=2))


if __name__ == '__main__':
    main()
