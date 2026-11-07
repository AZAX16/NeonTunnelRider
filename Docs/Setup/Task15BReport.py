"""Index executed Task15B evidence without changing Unity state or historical reports."""
import json, pathlib, datetime
root=pathlib.Path(__file__).resolve().parents[2]
out=root/'Evidence/Task15B'
def read(file):return json.loads((root/file).read_text())
def result(file):
 d=read(file)['data']['result'];return d.get('result',d)
def latest(step):
 matches=[]
 for p in sorted((root/'Evidence/Task15/Runs').glob('*'+step+'/run-manifest.json')):
  d=json.loads(p.read_text())
  if d.get('status')=='PASS' and d.get('step')==step:matches.append((p,d))
 if not matches:raise RuntimeError('No executed passing '+step)
 p,d=matches[-1];return str(p.parent.relative_to(root)),d

def main():
 runs={};manifests={}
 for kind in ['tests','fairness','scene','smoke','build']:runs[kind],manifests[kind]=latest(kind)
 native=read('Evidence/Task15B/native-summary.json')
 d={'recorded':datetime.datetime.now(datetime.timezone.utc).isoformat(),
    'state':'Functional expansion verified; long performance/import/delivery limits remain open',
    'ownerDecisions':['Only the player passing a car ahead qualifies; faster rear cars never trigger',
      'Rear lane hazard flashes for1.5 real Playing seconds, then revalidated rear entry',
      'Stay directly behind the same car/same lane for2.5s; early departure/contact cancels; then1s adjacent clean pass; only success earns2.5s after and the full six-second pitch1 cue window'],
    'task16':'DEFERRED','soak':'STOPPED; not resumed','uncommitted':True,'bestScorePreserved':27316,
    'currentRuns':runs,'tests':manifests['tests'].get('summary'),
    'native':native,'build':result(runs['build']+'/build-result.json'),
    'density':result('Evidence/Task15B/density.json'),
    'historicalAdjacentStartVisual':{k:v for k,v in result('Evidence/Task15B/staged-visual-result.json').items() if k!='timeline'},
    'historicalAdjacentStartPhaseMeasurements':read('Evidence/Task15B/staged-phase-measurements.json'),
    'rearVisual':{k:v for k,v in result('Evidence/Task15B/staged-traffic-visual-result.json').items() if k!='trace'},
    'warning':{k:v for k,v in result('Evidence/Task15B/warning-focused-result.json').items() if k!='trace'},
    'cueWindow':result('Evidence/Task15B/BehindGate/cue-window.json'),
    'behindGate':{k:v for k,v in result('Evidence/Task15B/BehindGate/live-cases-final.json').items() if k!='trace'},
    'behindGateReport':'Docs/Verification/15b-behind-gate.md',
    'mix':read('Evidence/Task15B/staged-mix-measurement.json'),
    'allocations':read('Evidence/Task15B/allocation-final-summary.json'),
    'limits':['No full steady-state zero-GC/frame-time/GPU/memory certification','No resumed long measurement or30-minute soak',
      'Fresh isolated clone import and paired Bloom comparison remain open',
      'Owner approved excerpt sound before final timing refinements; no separate final-staging listening verdict',
      'Development build is not final Task16 delivery'],
    'canonicalNote':'Docs/Verification/15b-traffic-and-near-miss.md'}
 (out/'summary.json').write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':main()
