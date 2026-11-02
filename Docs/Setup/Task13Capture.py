"""Capture ONLY the current verification player's PulseAudio sink; no desktop/media capture.
The isolated null sink permits reproducible output capture while the desktop is used.
This is digital output evidence, NOT a perceptual listening PASS.
"""
import subprocess,os,time,json,wave,signal,hashlib,sys
from pathlib import Path
import numpy as np
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Evidence/Task13/Standalone';BUILD=ROOT/'Builds/Task13Audio';SINK='ntr_task13_capture'
def main():
    OUT.mkdir(parents=True,exist_ok=True)
    timeline=OUT/'timeline.jsonl'
    if timeline.exists():raise RuntimeError('Preserve/rename previous timeline before a new capture')
    module=subprocess.check_output(['pactl','load-module','module-null-sink',f'sink_name={SINK}','rate=48000','channels=2'],text=True).strip()
    raw=BUILD/'capture.raw';recorder=None;player=None;started=time.time()
    try:
        with raw.open('wb') as f:
            recorder=subprocess.Popen(['parec','--device='+SINK+'.monitor','--format=s16le','--rate=48000','--channels=2'],stdout=f)
            player=subprocess.Popen([str(BUILD/'NeonTunnelRunner.x86_64'),'-force-glcore','-screen-width','1280','-screen-height','720','-screen-fullscreen','0','-logFile',str(OUT/'player.log'),'-ntr-evidence',str(OUT)],cwd=ROOT,env={**os.environ,'PULSE_SINK':SINK},stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
            # FMOD/ALSA does not honor PULSE_SINK here. Move only this player's
            # PulseAudio input, identified by the exact child process id.
            routed=[];deadline=time.monotonic()+15
            while not routed:
                inputs=json.loads(subprocess.check_output(['pactl','--format=json','list','sink-inputs']))
                for item in inputs:
                    if item.get('properties',{}).get('application.process.id')==str(player.pid):
                        subprocess.run(['pactl','move-sink-input',str(item['index']),SINK],check=True)
                        routed.append({'input':item['index'],'pid':player.pid,'fromSink':item['sink'],'toSink':SINK})
                if time.monotonic()>deadline:raise TimeoutError('Current player audio input was not found')
                if not routed:time.sleep(.05)
            (OUT/'routing.json').write_text(json.dumps(routed,indent=2)+'\n')
            code=player.wait(timeout=130)
            recorder.send_signal(signal.SIGINT);recorder.wait(timeout=10)
        stamps=[json.loads(l) for l in timeline.read_text().splitlines()]
        assert code==0 and stamps[-1]['label']=='50-reset-cycles-complete',(code,stamps)
        assert all(s['sources']==10 and s['pooled']==352 and s['misses']==0 for s in stamps)
        values=np.fromfile(raw,dtype='<i2').reshape(-1,2);signal_float=values/32768
        # Recorder starts before Player initialization: locate first nonzero output onset.
        occupied=np.flatnonzero(np.max(np.abs(values),axis=1)>2);onset=occupied[0]/48000
        # Music continues across focus pause (1 s retained), so seam is near second 65.
        parts=[('interactions.wav',max(0,onset-.05),min(onset+24,len(values)/48000)),('music-loop-join.wav',max(0,onset+62),min(len(values)/48000,onset+67))]
        for name,a,b in parts:
            with wave.open(str(OUT/name),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(48000);w.writeframes(values[int(a*48000):int(b*48000)].tobytes())
        report={'utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),'playerExitCode':code,'seconds':len(values)/48000,'rate':48000,'channels':2,'captureSink':SINK,'scope':'Non-headless current player, isolated PulseAudio monitor; no auditory observer; NOT a listening PASS','peak':float(np.abs(signal_float).max()),'rms':float(np.sqrt(np.mean(signal_float**2))),'clippedSamples':int(np.count_nonzero(np.abs(values.astype(np.int32))>=32767)),'firstSignalSeconds':onset,'timeline':stamps,'files':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.glob('*.wav')}}
        (OUT/'capture.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k not in ['timeline','files']},indent=2),flush=True)
    finally:
        if player and player.poll() is None:player.terminate();player.wait(timeout=10)
        if recorder and recorder.poll() is None:recorder.terminate();recorder.wait(timeout=10)
        subprocess.run(['pactl','unload-module',module],check=True)
if __name__=='__main__':main()
