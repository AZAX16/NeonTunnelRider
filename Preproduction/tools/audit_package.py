#!/usr/bin/env python3
"""Validate preproduction links, traceability and prompt structure; no Unity tests."""
from pathlib import Path
import json
import re
import sys

root = Path(__file__).resolve().parents[1]
requirements = json.loads((root/'planning/requirements.json').read_text())
cases = json.loads((root/'planning/validation-cases.json').read_text())
case_ids = {v['id'] for v in cases}
prompts = {p.name[:2]: p for p in (root/'codex-prompts').glob('[0-9]*.md')}
errors = []
for p in root.rglob('*.md'):
    for dest in re.findall(r'\]\(([^)]+)\)', p.read_text()):
        if re.match(r'\w+://|#', dest):
            continue
        if not (p.parent/dest.split('#')[0]).exists():
            errors.append(f'Broken link: {p.relative_to(root)} -> {dest}')
for row in requirements:
    for number in row['tasks'].split(','):
        if number not in prompts or row['id'] not in prompts[number].read_text():
            errors.append(f'Unmapped prompt: {row["id"]} -> {number}')
    for test in row['validation'].split(','):
        if test not in case_ids:
            errors.append(f'Unmapped test: {row["id"]} -> {test}')
    for field in ('original','explanation','status','implementation','systems','acceptance','validation_method'):
        if not row.get(field):
            errors.append(f'Missing {field} on {row["id"]}')
sections = ['Task name and objective','Working contract and current assumptions',
            'Relevant HW2 requirements','Integration dependencies','Exact classes and files',
            'Expected behavior and required architecture','Inspector, scene and prefab setup',
            'Edge cases','Tests and acceptance criteria','Definition of Done','Explicit non-goals']
for p in prompts.values():
    text = p.read_text()
    for section in sections:
        if '## '+section not in text:
            errors.append(f'Missing prompt section: {p.name}: {section}')
    if '6000.6.3f1' not in text:
        errors.append(f'Missing user Editor baseline: {p.name}')
if (root/'.gitignore').read_bytes() != (root/'source/assignment.gitignore').read_bytes():
    errors.append('Supplied .gitignore not preserved exactly.')
if len(requirements)!=54 or len(cases)!=25 or len(prompts)!=16:
    errors.append('Unexpected requirement/case/prompt counts.')
if len(list((root/'art').rglob('*.png')))!=12:
    errors.append('Expected 12 final concept references.')
print(json.dumps({'result':'FAIL' if errors else 'PASS',
                  'requirements':len(requirements),'validation_cases':len(cases),
                  'prompts':len(prompts),'images':len(list((root/'art').rglob('*.png'))),
                  'errors':errors,'unity_tests_executed':False}, indent=2))
sys.exit(bool(errors))
