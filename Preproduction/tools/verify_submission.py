#!/usr/bin/env python3
"""Read-only structural/CRC check for the future HW2 course ZIP. Does not run it."""
import argparse
import hashlib
from pathlib import Path, PurePosixPath
import re
import sys
import zipfile

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', type=Path)
    args = parser.parse_args()
    path = args.archive
    errors = []
    if not path.is_file():
        parser.error('Archive does not exist.')
    if not re.fullmatch(r'GD_Hw2_[^_\[\]]+_[^_\[\]]+_[^_\[\]]+\.zip', path.name):
        errors.append('Filename must be GD_Hw2_firstName_lastName_StudentId.zip with real fields.')
    if any(x in path.stem for x in ('firstName', 'lastName', 'StudentId', 'FirstName', 'LastName')):
        errors.append('Replace identity placeholders with actual student details.')
    try:
        with zipfile.ZipFile(path) as z:
            names = z.namelist()
            for name in names:
                p = PurePosixPath(name)
                if p.is_absolute() or '..' in p.parts or '\\' in name or ':' in p.parts[0]:
                    errors.append('Unsafe archive path: ' + name)
            corrupt = z.testzip()
            if corrupt:
                errors.append('CRC failed: ' + corrupt)
            if 'repository-link.txt' not in names:
                errors.append('Missing root repository-link.txt.')
            else:
                link = z.read('repository-link.txt').decode('utf-8-sig').strip()
                if not re.search(r'https://[^\s<>]+', link):
                    errors.append('repository-link.txt needs a real HTTPS repository URL.')
            if not any(n.startswith('Build/') and not n.endswith('/') for n in names):
                errors.append('Missing complete Build/ directory.')
            if not any(re.fullmatch(r'ImplementationReport\.(pdf|md|docx|html)', n, re.I) for n in names):
                errors.append('Missing root ImplementationReport in a readable report format.')
            if 'README-run.txt' not in names:
                errors.append('Missing README-run.txt (project delivery requirement).')
            print(f'Archive entries: {len(names)}')
    except (OSError, zipfile.BadZipFile, RuntimeError, UnicodeError) as e:
        errors.append(str(e))
    digest = hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(1024*1024), b''):
            digest.update(chunk)
    print('SHA-256:', digest.hexdigest())
    if errors:
        for error in errors:
            print('FAIL:', error)
        return 1
    print('PASS: structural and CRC checks only.')
    print('Still required: inspect report/screenshots/credits, verify repository access,')
    print('extract and launch full build on target OS, then re-download after upload and repeat.')
    return 0

if __name__ == '__main__':
    sys.exit(main())
