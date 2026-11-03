"""Compare current native-player shader messages against the actual Task13 log."""
import re,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Evidence/Task14'
pattern=re.compile(r"^Shader '.*PostProcessing render passes will not execute\.$",re.M)
baseline=pattern.findall((ROOT/'Evidence/Task13/Standalone/player.log').read_text())
report={'baseline':'Evidence/Task13/Standalone/player.log','baselineShaderWarnings':baseline,'players':[]}
for p in sorted((OUT/'Standalone').glob('*-player.log')):
 s=p.read_text(errors='replace');messages=pattern.findall(s)
 errors=[line for line in s.splitlines() if re.search(r'NullReferenceException|InvalidOperationException|ArgumentException|^Error\b|The character with Unicode|missing.*glyph',line,re.I)]
 other=[line for line in s.splitlines() if re.search(r'\bwarning\b',line,re.I) and not re.search(r'LogWarning|Shader warning',line)]
 report['players'].append({'path':str(p.relative_to(ROOT)),'shaderWarnings':messages,'matchesTask13':messages==baseline,'runtimeOrGlyphErrors':errors,'otherWarningLines':other,'displayDpiDiagnostics':s.count("Could not fetch DPI for display (0): Couldn't get DPI")})
report['passed']=len(baseline)==4 and len(report['players'])>=2 and all(x['matchesTask13'] and not x['runtimeOrGlyphErrors'] and not x['otherWarningLines'] for x in report['players'])
(OUT/'player-log-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
