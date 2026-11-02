"""Index original runtime frames and compare matching Bloom views; no image editing."""
import json
from pathlib import Path
from PIL import Image,ImageChops,ImageStat
ROOT=Path(__file__).resolve().parents[2];E=ROOT/'Evidence/Task12'
def main():
 report={'modes':[],'comparisons':[]}
 lines=['# Normal saved-scene opening sequence','','Original runtime camera renders, unmodified. No forced firework call, effect video, concept image or skybox effect. Both sizes render the same saved Game camera at the recorded simulation instant. Native paused Game-view frames are included separately.','', '| Frame | Playing time | Effect age | Rocket / child particles | Rocket or child mean world position |', '| --- | ---: | ---: | --- | --- |']
 for mode in ['normal','fast']:
  r=json.loads((E/(mode+'-visual.json')).read_text())['data']['result']['result']
  assert r['passed']
  captures=r['captures'];report['modes'].append({k:v for k,v in r.items() if k!='captures'})
  for label in ['opening-launch','opening-ascent','opening-late-ascent','opening-burst-ignition','opening-burst']:
   c=next(c for c in captures if c['path'].endswith(mode+'-'+label+'.png'))
   effect=next(e for e in c['effects'] if e['kind']=='Firework');children=effect['children'];counts='/'.join(str(p['particleCount']) for p in children)
   mean=next((p['meanWorld'] for p in children if p['particleCount']),[])
   lines.append(f"| [{mode} {label}]({mode}-{label}.png) · [4:3]({mode}-{label}-4x3.png) | {c['ElapsedTime']:.3f}s | {effect['Age']:.3f}s | {counts} | {mean} |")
  for base in ['opening-burst','neon','horizon-cap']:
   for suffix in ['','-4x3']:
    on=E/f'{mode}-{base}-bloom-on{suffix}.png' if base!='horizon-cap' else E/f'{mode}-horizon-cap{suffix}.png'
    off=E/f'{mode}-{base}-bloom-off{suffix}.png'
    if not on.exists():continue
    a,b=Image.open(on),Image.open(off);diff=ImageChops.difference(a,b);bbox=diff.getbbox();stats=ImageStat.Stat(diff)
    pixels=sum(any(channel for channel in p) for p in diff.getdata())
    assert bbox and pixels>0
    ca=next(c for c in captures if c['path'].endswith(on.name));cb=next(c for c in captures if c['path'].endswith(off.name))
    assert ca['frame']==cb['frame'] and ca['ElapsedTime']==cb['ElapsedTime'] and ca['Distance']==cb['Distance'] and ca['state']==cb['state']
    report['comparisons'].append({'on':on.name,'off':off.name,'sameFrame':ca['frame'],'samePlayingTime':ca['ElapsedTime'],'sameState':ca['state'],'changedPixels':pixels,'meanAbsoluteRgbDelta':stats.mean,'differenceBounds':bbox})
  report['modes'][-1]['capturedExhaustPeak']=max(c['exhaust'] for c in captures)
  report['modes'][-1]['captureCount']=len(captures)
 lines+=['','The ascent pause adds ten wall seconds without changing playing time, effect age or particles. The sequence resumes from that same state. `normal-visual.json` and `fast-visual.json` contain all timestamps, source states, counts, launch-sky checks and mean particle positions. `normal-native-ascent-paused.png` and `fast-native-ascent-paused.png` are native Game-view captures.','', 'Bloom pairs, pickups, collision/terminal feedback, tunnel transitions, cap horizon and rebases are indexed in those JSON files.']
 (E/'OpeningSequence.md').write_text('\n'.join(lines)+'\n');(E/'visual-review.json').write_text(json.dumps(report,indent=2)+'\n')
 print([(m['reloads'],m['launchTime'],m['maxParticles'],m['maxMaterials'],m['capturedExhaustPeak'],m['captureCount']) for m in report['modes']])
 print('Matching Bloom comparisons:',len(report['comparisons']))
if __name__=='__main__':main()
