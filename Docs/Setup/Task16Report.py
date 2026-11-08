"""Render the editable Persian Markdown subset through Chromium's RTL/shaping engine.

No new dependency or font download. The HTML is retained as rendering evidence.
After printing, inspect rasterized PDF pages; successful printing alone is not acceptance.
"""
import base64
import html
import re
import subprocess
import tempfile
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = root / 'Docs/ImplementationReport.fa.md'


def inline(value):
    # Protect English identifiers and URLs from surrounding Persian directionality.
    value = html.escape(value)
    value = value.replace('علیرضا پویان — Alireza Pooyan — 400414039',
                          'علیرضا پویان — <bdi dir="ltr">Alireza Pooyan</bdi> — <bdi dir="ltr">400414039</bdi>')
    value = re.sub(r'`([^`]+)`', r'<code dir="ltr">\1</code>', value)
    value = re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', value)
    value = re.sub(r'https://[^\s<؛]+', lambda m: '<a dir="ltr" href="' + m[0] + '">' + m[0] + '</a>', value)
    return value


blocks = []
paragraph = []
listing = False
pre = False


def flush():
    if paragraph:
        blocks.append('<p>' + inline(' '.join(paragraph)) + '</p>')
        paragraph.clear()


for line in source.read_text().splitlines():
    if line.startswith('<div ') or line == '</div>':
        continue
    if line.startswith('<pre'):
        flush(); pre = True; blocks.append(line); continue
    if pre:
        if '</pre>' in line:
            blocks.append(html.escape(line.replace('</pre>', '')) + '</pre>'); pre = False
        else:
            blocks.append(html.escape(line) + '\n')
        continue
    if not line:
        flush()
        if listing:
            blocks.append('</ul>'); listing = False
        continue
    if line.startswith('#'):
        flush(); level = len(line) - len(line.lstrip('#'))
        blocks.append(f'<h{level}>' + inline(line[level:].strip()) + f'</h{level}>'); continue
    image = re.fullmatch(r'!\[(.*)\]\((.*)\)', line)
    if image:
        flush(); path = (source.parent / image[2]).resolve()
        encoded = base64.b64encode(path.read_bytes()).decode()
        blocks.append('<figure><img src="data:image/png;base64,' + encoded + '"><figcaption>' + inline(image[1]) + '</figcaption></figure>'); continue
    if line.startswith('- '):
        flush()
        if not listing:
            blocks.append('<ul>'); listing = True
        blocks.append('<li>' + inline(line[2:]) + '</li>'); continue
    paragraph.append(line)
flush()
if listing:
    blocks.append('</ul>')
css = '''
@page { size: A4; margin: 14mm 16mm 15mm; }
@font-face { font-family: Report; src: url(file:///usr/share/fonts/truetype/dejavu/DejaVuSans.ttf); }
@font-face { font-family: Report; font-weight: bold; src: url(file:///usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf); }
body { font-family: Report, sans-serif; direction: rtl; text-align: right; color: #172337; font-size: 9.5pt; line-height: 1.9; }
h1 { color: #123c57; font-size: 21pt; line-height: 1.6; margin: 0 0 5mm; }
h2 { color: #124f69; font-size: 13pt; border-bottom: 1px solid #a5bbc6; padding-bottom: 2mm; margin-top: 7mm; break-after: avoid; }
p { margin: 2mm 0; orphans: 3; widows: 3; }
code { font-family: Report, sans-serif; font-size: 8.2pt; direction: ltr; unicode-bidi: isolate; overflow-wrap: anywhere; }
a { color: #165d7a; direction: ltr; unicode-bidi: isolate; overflow-wrap: anywhere; font-size: 8pt; }
pre { direction: ltr; text-align: left; font: 8pt/1.6 monospace; white-space: pre-wrap; background: #eef3f5; padding: 3mm; break-inside: avoid; }
li { margin-bottom: 1.5mm; }
figure { break-inside: avoid; margin: 5mm 0; padding-bottom: 2mm; }
figure img { display: block; max-width: 100%; max-height: 77mm; margin: auto; object-fit: contain; }
figcaption { font-size: 8.3pt; line-height: 1.7; margin: 2mm 0; color: #34485b; }
'''
document = '<!doctype html><html lang="fa" dir="rtl"><meta charset="utf-8"><title>گزارش علیرضا پویان — Neon Tunnel Runner</title><style>' + css + '</style><body>' + '\n'.join(blocks) + '</body></html>'
out = root / 'Evidence/Task16/report-render.html'
out.write_text(document)
profile = tempfile.mkdtemp(prefix='neon-task16-pdf-chrome-')
result = subprocess.run(['google-chrome', '--headless', '--no-sandbox', '--disable-gpu', '--no-pdf-header-footer',
                         '--user-data-dir=' + profile, '--print-to-pdf=' + str(root / 'ImplementationReport.pdf'), out.as_uri()],
                        capture_output=True, text=True, timeout=90)
(root / 'Evidence/Task16/report-render-log.txt').write_text(result.stdout + result.stderr)
if result.returncode:
    raise RuntimeError('Chromium PDF print failed; see report-render-log.txt')
print('Printed', root / 'ImplementationReport.pdf', '(visual inspection still required)')
