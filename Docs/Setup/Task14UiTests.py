import sys,time,json
sys.dont_write_bytecode=True
from Task14Tools import *
settle();label=sys.argv[1] if len(sys.argv)>1 else 'ui'
save(label+'-start.json',command(['test','run','--mode','playmode','--filter','UiBindingTests','--async_tests','true']))
while True:
 time.sleep(2);r=command(['test','status']);s=r['data']['result']
 if s.get('status')=='completed':
  save(label+'-results.json',r);print(s['summary']);print(json.dumps([x for x in s['results'] if x['Status']!='Passed'],indent=2));break
