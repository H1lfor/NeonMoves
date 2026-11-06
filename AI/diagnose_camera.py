"""Brief live camera/service check. Reports metadata only; never saves a frame."""
import argparse
import json
from pathlib import Path
import secrets
import socket
import subprocess
import sys
import time
import urllib.request

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--index',type=int,default=0)
    parser.add_argument('--bridge',type=Path,default=Path(__file__).with_name('bridge.py'))
    parser.add_argument('--retry',action='store_true',help='Also verify same-device reconnect and off/on')
    args=parser.parse_args()
    with socket.socket() as sock:
        sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
    token=secrets.token_hex(16)
    process=subprocess.Popen([sys.executable,str(args.bridge),'--port',str(port),'--token',token],stdout=subprocess.DEVNULL)
    def request(path,body=None):
        req=urllib.request.Request(f'http://127.0.0.1:{port}{path}',data=json.dumps(body).encode() if body is not None else None,headers={'X-Neonmoves-Token':token})
        with urllib.request.urlopen(req,timeout=2) as response:return response.status,response.read()
    try:
        for _ in range(50):
            try:request('/state');break
            except OSError:time.sleep(.1)
        for revision,phase in enumerate(('startup','reconnect','off/on') if args.retry else ('startup',)):
            if phase=='off/on':
                request('/configure',{'camera':False,'voice':False,'cameraIndex':args.index,'cameraRevision':revision})
                time.sleep(.35)
                code,frame=request('/frame')
                if code!=204 or frame:raise RuntimeError('Camera frame remains available after disable')
            request('/configure',{'camera':True,'voice':False,'cameraIndex':args.index,'cameraRevision':revision})
            deadline=time.monotonic()+25
            while time.monotonic()<deadline:
                _,body=request('/state');state=json.loads(body)
                if state['cameraStatus'] in ('ready','error'):
                    if state['cameraStatus']=='ready':
                        # Allow auto-exposure to settle before measuring image brightness.
                        for _ in range(10):
                            time.sleep(.2)
                            _,body=request('/state');state=json.loads(body)
                    code,frame=request('/frame')
                    metadata={'phase':phase,'cameraIndex':args.index,'cameraStatus':state['cameraStatus'],'cameraError':state['cameraError'],'backend':state.get('cameraBackend'),'fps':state['fps'],'frameHTTP':code,'frameBytes':len(frame),'scoreCount':len(state['scores'])}
                    if frame:
                        import cv2
                        import numpy as np
                        pixels=cv2.imdecode(np.frombuffer(frame,dtype=np.uint8),cv2.IMREAD_GRAYSCALE)
                        metadata.update(previewWidth=pixels.shape[1],previewHeight=pixels.shape[0],meanBrightness=round(float(pixels.mean()),2),brightnessStdDev=round(float(pixels.std()),2))
                    print(json.dumps(metadata,ensure_ascii=True),flush=True)
                    if state['cameraStatus']!='ready' or not frame:return 1
                    break
                time.sleep(.2)
            else:
                print('Camera startup timed out',flush=True);return 1
        return 0
    finally:
        try:request('/shutdown',{})
        except OSError:pass
        try:process.wait(timeout=5)
        except subprocess.TimeoutExpired:process.kill();process.wait(timeout=3)

if __name__=='__main__':sys.exit(main())
