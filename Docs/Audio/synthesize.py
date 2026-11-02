"""Original Task 13 composition/synthesis, Codex-assisted, no sampled/external audio.
Run with Python 3 + NumPy. Produces deterministic 44.1 kHz / PCM16 WAV masters.
The 32-bar, 120 BPM A-minor loop uses Am–F–C–G, an original arpeggio/lead,
four arrangement phrases, synthesized drums and circular stereo delay.
This is offline authoring only; Unity never synthesizes clips at runtime.
"""
from pathlib import Path
import hashlib, json, wave
import numpy as np
RATE = 44100
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/NeonTunnelRunner/Audio/Clips'
OUT.mkdir(parents=True, exist_ok=True)
rng = np.random.default_rng(13041)
inventory = []
def time(seconds): return np.arange(round(seconds * RATE)) / RATE
def sine(f, t): return np.sin(2*np.pi*f*t)
def frequency(midi): return 440 * 2**((midi-69)/12)
def envelope(t, length, attack=.004, release=.04):
    return np.minimum(1,t/attack) * np.minimum(1,np.maximum(0,(length-t)/release))
def save(name, signal, loop=False):
    if signal.ndim == 1: signal=signal[:,None]
    signal=signal-signal.mean(axis=0)
    if not loop:
        n=len(signal); fade=min(220,n//10)
        signal[:fade]*=np.linspace(0,1,fade)[:,None];signal[-fade:]*=np.linspace(1,0,fade)[:,None]
    if not loop:
        compensation=np.sin(np.linspace(0,np.pi,len(signal)))**2
        signal-=compensation[:,None]*(signal.mean(axis=0)/compensation.mean())
    signal=signal*(.65/np.max(np.abs(signal)))
    data=np.rint(signal*32767).astype('<i2')
    path=OUT/(name+'.wav')
    with wave.open(str(path),'wb') as w:
        w.setnchannels(signal.shape[1]);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(data.tobytes())
    inventory.append(dict(file=str(path.relative_to(ROOT)),seconds=len(signal)/RATE,channels=signal.shape[1],sampleRate=RATE,
        peak=float(np.max(np.abs(signal))),rms=float(np.sqrt(np.mean(signal**2))),dc=signal.mean(axis=0).tolist(),
        joinDelta=(signal[0]-signal[-1]).tolist(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),loop=loop))
# A seamless circular score: every delayed tail wraps into the same bar grid.
duration=64.; music=np.zeros((round(duration*RATE),2)); beat=.5
chords=[(45,57,60,64),(41,53,57,60),(48,55,60,64),(43,55,59,62)]
def add(start, sound, pan=0):
    idx=(round(start*RATE)+np.arange(len(sound)))%len(music)
    music[idx,0]+=sound*np.sqrt((1-pan)/2); music[idx,1]+=sound*np.sqrt((1+pan)/2)
for bar in range(32):
    root,*triad=chords[(bar//2)%4]; start=bar*2; phrase=bar//8
    t=time(2.15); pad=sum(sine(frequency(n),t)+.16*sine(frequency(n)*2,t) for n in triad)/3
    add(start,.12*pad*envelope(t,2.15,.16,.25),-.3)
    for step in range(8):
        pos=start+step*.25; note=triad[[0,1,2,1,0,2,1,2][step]]+12
        t=time(.31); tone=(sine(frequency(note),t)+.22*sine(frequency(note)*2,t)+.08*sine(frequency(note)*3,t))
        add(pos,.13*tone*np.exp(-t*13)*envelope(t,.31),.4 if step%2 else -.4)
        if step%2==0:
            t=time(.22); bass=sum(sine(frequency(root)*h,t)/h**1.8 for h in range(1,6))
            add(pos,.28*bass*np.exp(-t*9)*envelope(t,.22,.004,.03))
        if phrase in (1,3) and step in (0,3,5,7):
            lead=[76,79,81,79,72,76,79,74][(bar+step)%8];t=time(.40)
            add(pos,.09*(sine(frequency(lead),t)+.15*sine(frequency(lead)*3,t))*np.exp(-t*7)*envelope(t,.40),.15)
    for b in range(4):
        t=time(.24); phase=2*np.pi*(46*t+(116-46)*.025*(1-np.exp(-t/.025)))
        add(start+b*.5,.42*np.sin(phase)*np.exp(-t*22)*envelope(t,.24,.002,.025))
        if b%2:
            t=time(.14); noise=rng.normal(size=len(t));high=noise-np.roll(noise,1)
            add(start+b*.5,.075*(high+.7*sine(190,t))*np.exp(-t*34)*envelope(t,.14,.002,.02),-.1)
    for eighth in range(8):
        t=time(.045); noise=rng.normal(size=len(t));high=noise-np.roll(noise,1)
        add(start+eighth*.25,.024*high*np.exp(-t*95)*envelope(t,.045,.001,.01),.35)
# Circular eighth-note echo, no nonperiodic tail/cut at the join.
music[:,0]+=.17*np.roll(music[:,1],round(.375*RATE))
music[:,1]+=.17*np.roll(music[:,0],round(.375*RATE))
save('VectorNight',music,True)
# Periodic hover-engine: stable 55 Hz core + harmonic growl and small 2 Hz FM.
t=time(2);phase=2*np.pi*55*t+.18*np.sin(2*np.pi*2*t)
engine=sum(np.sin(h*phase)/h**1.35 for h in range(1,9))
engine*=.88+.08*np.sin(2*np.pi*8*t)+.04*np.sin(2*np.pi*3*t)
engine+=.08*sine(330,t)
save('HoverEngine',engine,True)
t=time(.34);noise=rng.normal(size=len(t));metal=sum(sine(f,t) for f in [733,1223,2171])/3
collision=.45*(noise-np.roll(noise,1))*np.exp(-t*30)+.75*np.sin(2*np.pi*(82*t+1.8*(1-np.exp(-t*18))))*np.exp(-t*17)+.30*metal*np.exp(-t*19)
save('Impact',np.tanh(3*collision)*envelope(t,.34,.002,.03))
t=time(.18);coin=(sine(1318.51,t)+.65*sine(1975.53,t)+.3*sine(2637.02,t))*np.exp(-t*21)
save('CoinChime',coin*envelope(t,.18,.003,.03))
t=time(.34);phase=2*np.pi*(220*t+220*t*t/.68)
fuel=(np.sin(phase)+.35*np.sin(phase*1.5)+.2*sine(660,t))*np.sin(np.pi*t/.34)**.65
save('FuelCharge',fuel*envelope(t,.34,.01,.04))
(ROOT/'Evidence/Task13/masters.json').write_text(json.dumps(inventory,indent=2)+'\n')
print(json.dumps(inventory,indent=2))
