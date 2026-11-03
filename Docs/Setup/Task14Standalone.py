"""Normal player acceptance through native X11 mouse/keyboard events, without game hooks.
Capture only the exact launched player's window. No gameplay mutation, autostart, or test define.
"""
import os,sys,time,json,hashlib,subprocess
import numpy as np
from pathlib import Path
from Xlib import X,XK,display
from Xlib.ext import xtest
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Evidence/Task14/Standalone';BUILD=ROOT/'Builds/Task14UI'
OUT.mkdir(parents=True,exist_ok=True)
D=display.Display();window=None;process=None;timeline=[]
def stamp(label,**data):
 r={'utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),'elapsed':time.monotonic()-begun,'label':label,**data};timeline.append(r);(OUT/(tag+'-timeline.json')).write_text(json.dumps(timeline,indent=2)+'\n');print(label,data,flush=True)
def focus():
 deadline=time.monotonic()+10
 while window.get_attributes().map_state!=X.IsViewable:
  if time.monotonic()>deadline:raise TimeoutError("Player window did not map")
  time.sleep(.05)
 active=D.screen().root.get_full_property(D.intern_atom('_NET_ACTIVE_WINDOW'),X.AnyPropertyType)
 if active is None or int(active.value[0])!=window.id:subprocess.run(['wmctrl','-i','-a',hex(window.id)],check=True)
 window.configure(stack_mode=X.Above);window.set_input_focus(X.RevertToParent,X.CurrentTime);D.sync()
def key(name):
 focus();pressed=time.monotonic();code=D.keysym_to_keycode(XK.string_to_keysym(name));xtest.fake_input(D,X.KeyPress,code);D.sync();time.sleep(.09);xtest.fake_input(D,X.KeyRelease,code);D.sync();time.sleep(.18);stamp('key',key=name);return pressed
def click(x,y):
 focus();p=D.screen().root.translate_coords(window,x,y);xtest.fake_input(D,X.MotionNotify,x=p.x,y=p.y);D.sync();time.sleep(.1);xtest.fake_input(D,X.ButtonPress,1);D.sync();time.sleep(.09);xtest.fake_input(D,X.ButtonRelease,1);D.sync();time.sleep(.22);stamp('click',x=x,y=y)
def capture(name):
 focus();time.sleep(.1);g=window.get_geometry();im=window.get_image(0,0,g.width,g.height,X.ZPixmap,0xffffffff);rgb=Image.frombytes('RGB',(g.width,g.height),im.data,'raw','BGRX');path=OUT/(tag+'-'+name+'.png');rgb.save(path);digest=hashlib.sha256(rgb.tobytes()).hexdigest();stamp('capture',image=path.name,width=g.width,height=g.height,pixelsSha256=digest);return digest
def sample(index,counter=0):
 mask=(1<<64)-1;x=index^(12345<<32)^0x67616d65^((counter*0x9e3779b97f4a7c15)&mask);x=(x+0x9e3779b97f4a7c15)&mask;x=((x^(x>>30))*0xbf58476d1ce4e5b9)&mask;x=((x^(x>>27))*0x94d049bb133111eb)&mask;return (x^(x>>31))&0xffffffff
def planned_lane(block):return 0 if block==0 else sample(block)%3-1
def guide(seconds,avoid_fuel=False,started=None):
 # External keyboard tester using the documented seed/speed schedule. Never reads or writes
 # the Player's session, world, clock, resources or commands; real action bindings do all driving.
 current=0;started=started if started is not None else time.monotonic()-.27;last_capture=0
 stamp('native-keyboard-route',seconds=seconds,avoidFuel=avoid_fuel,seed=12345)
 while time.monotonic()-started<seconds:
  focus();elapsed=time.monotonic()-started;distance=22*elapsed+(28/360)*elapsed*elapsed
  block=int(distance//480);local=distance-block*480;wanted=planned_lane(block+1) if local>=395 else planned_lane(block)
  if avoid_fuel:
   for fuel_local in [120,300,450]:
    if fuel_local-24<=local<=fuel_local+12:
     fuel_lane=planned_lane(block+1) if fuel_local==450 else planned_lane(block)
     def danger(lane):
      blocked=0
      for row in range(6):
       z=block*480+row*60;density=.25+.55*min(1,z/6000)
       if abs(row*60-fuel_local)<=70 and z>=180 and lane!=planned_lane(block) and sample(block,1+row*3+lane+1)/4294967296<density:blocked+=1 if tag=="keyboard-lives-final" else (10 if row*60==fuel_local else 1)
      return blocked*10+abs(lane-current)
     wanted=min([lane for lane in [-1,0,1] if lane!=fuel_lane],key=danger)
  while current!=wanted:
   direction=1 if wanted>current else -1;key('Right' if direction>0 else 'Left');current+=direction
  if elapsed-last_capture>=20:capture('route-'+str(int(elapsed)));last_capture=elapsed
  time.sleep(.02)
 capture('route-complete')
 stamp('route-inputs-complete',estimatedDistance=22*seconds+(28/360)*seconds*seconds,lane=current)
 return current
def await_terminal(seconds=120):
 # Render stalling is not death. Require the actual GAME OVER title and reason glyphs.
 title_rect=(440,225,840,275);reason_rect=(390,280,890,318)
 def mask(im,rect):return np.max(np.asarray(im.crop(rect)),axis=2)>180
 refs={kind:Image.open(ROOT/('Evidence/Task14/normal-1280x720-over-'+kind+'.png')).convert('RGB') for kind in ['Lives','Fuel']}
 title=mask(refs['Lives'],title_rect)
 def similarity(a,b):return 2*np.count_nonzero(a&b)/max(1,np.count_nonzero(a)+np.count_nonzero(b))
 started=time.monotonic();last_capture=0
 while time.monotonic()-started<seconds:
  focus();time.sleep(.15);g=window.get_geometry();raw=window.get_image(0,0,g.width,g.height,X.ZPixmap,0xffffffff);im=Image.frombytes('RGB',(g.width,g.height),raw.data,'raw','BGRX')
  score=similarity(mask(im,title_rect),title)
  if score>.97:
   scores={kind:similarity(mask(im,reason_rect),mask(ref,reason_rect)) for kind,ref in refs.items()};kind=max(scores,key=scores.get)
   if scores[kind]>.97:capture('terminal');stamp('visible-game-over',reason=kind,titleSimilarity=score,reasonSimilarity=scores[kind],seconds=time.monotonic()-started);return kind
  elapsed=time.monotonic()-started
  if elapsed-last_capture>=10:capture('run-'+str(int(elapsed)));last_capture=elapsed
  time.sleep(.5)
 raise TimeoutError('No visible Game Over title/reason within natural run window; inspect captures.')
def launch():
 global process,window
 process=subprocess.Popen([str(BUILD/'NeonTunnelRunner.x86_64'),'-force-glcore','-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-logFile',str(OUT/(tag+'-player.log'))],cwd=ROOT,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
 atom=D.intern_atom('_NET_WM_PID');deadline=time.monotonic()+30
 while time.monotonic()<deadline:
  for w in D.screen().root.get_full_property(D.intern_atom('_NET_CLIENT_LIST'),X.AnyPropertyType).value:
   candidate=D.create_resource_object('window',int(w));p=candidate.get_full_property(atom,X.AnyPropertyType)
   if p is not None and int(p.value[0])==process.pid:window=candidate;break
  if window is not None:break
  time.sleep(.1)
 if window is None:raise TimeoutError('Current player window not found')
 subprocess.run(['wmctrl','-i','-r',hex(window.id),'-e','0,160,100,1280,720'],check=True)
 focus();time.sleep(10);stamp('launched',pid=process.pid,window=hex(window.id));capture('main')
def finish():
 code=process.wait(timeout=15);stamp('quit-process-exit',pid=process.pid,exitCode=code);assert code==0
if __name__=='__main__':
 tag=sys.argv[1] if len(sys.argv)>1 else 'pointer-left';begun=time.monotonic()
 try:
  launch()
  if tag=='verify-best':
   key('Return');time.sleep(.6);capture('after-lower-run-best');key('Escape');key('Down');key('Down');key('Return');key('Down');key('Return');finish();sys.exit(0)
  if tag=='pointer-fuel-final':
   click(640,371);key('Escape');click(640,349);key('Escape');click(640,413);key('Escape');click(640,477);run_started=key('Return');guide(51,avoid_fuel=True,started=run_started)
  elif tag=='pointer-lives':
   click(640,371);key('Right');key('Right')
  elif tag.startswith('pointer'):
   click(640,371);time.sleep(2);capture('hud');key('Escape');time.sleep(.2);a=capture('paused');time.sleep(10.1);b=capture('paused-after-10s');
   pa=np.asarray(Image.open(OUT/(tag+'-paused.png'))).astype(np.int16);pb=np.asarray(Image.open(OUT/(tag+'-paused-after-10s.png'))).astype(np.int16);diff=np.abs(pa-pb);fraction=np.count_nonzero(np.max(diff,axis=2))/(1280*720)
   stamp('pause-image-comparison',exactEqual=a==b,maxChannelDifference=int(diff.max()),changedPixelFraction=float(fraction),criterion='max<=5 and changed fraction<.01; HUD/pose visually inspected separately');assert diff.max()<=5 and fraction<.01
   key('Return');key('Escape');key('Down');key('Return');capture('paused-restart-hud');key('Escape');click(640,477);capture('return-main');run_started=key('Return');lane=guide(92,started=run_started);
   while lane<1:key('Right');lane+=1
  else:
   run_started=key('Return');time.sleep(.6);capture('relaunch-best-hud');guide(51,avoid_fuel=True,started=run_started)
  await_terminal();click(640,461) if tag.startswith('pointer') else key('Return');time.sleep(.5);capture('terminal-restart-hud');key('Escape')
  if not tag.startswith('pointer'):key('BackSpace');key('Escape')
  if tag.startswith('pointer'):click(640,477);capture('before-quit');click(640,435)
  else:key('Down');key('Down');key('Return');capture('before-quit');key('Down');key('Return')
  finish()
 except Exception as e:
  stamp('FAILED',error=str(e))
  if process and process.poll() is None:process.terminate();process.wait(timeout=10);stamp('failure-cleanup-terminated',exitCode=process.returncode)
  raise
 finally:D.close()
