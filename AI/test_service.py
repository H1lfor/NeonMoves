import json
from pathlib import Path
import secrets
import socket
import subprocess
import sys
import time
import urllib.request
import urllib.error

base=Path(__file__).resolve().parent
sock=socket.socket();sock.bind(('127.0.0.1',0));port=sock.getsockname()[1];sock.close()
token=secrets.token_hex(16)
process=subprocess.Popen([sys.executable,str(base/'bridge.py'),'--port',str(port),'--token',token],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
results=[]
def request(path,body=None,auth=True):
    headers={'X-Neonmoves-Token':token} if auth else {}
    data=json.dumps(body).encode() if body is not None else None
    req=urllib.request.Request(f'http://127.0.0.1:{port}{path}',data=data,headers=headers)
    try:
        with urllib.request.urlopen(req,timeout=2) as r:return r.status,r.read()
    except urllib.error.HTTPError as e:return e.code,e.read()
def check(name,condition):
    results.append({'test':name,'passed':bool(condition)})
    if not condition:raise AssertionError(name)
try:
    for _ in range(40):
        try:code,body=request('/state');break
        except OSError:time.sleep(.1)
    snapshot=json.loads(body)
    check('Camera start protocol advertised',snapshot['version']==4 and len(snapshot['scores'])==12)
    check('No stale start gesture on launch',snapshot['startGesture'] is False and snapshot['frameId']==0)
    check('No stale hand data at startup',snapshot['hands']==[] and snapshot['leftGesture']=='' and snapshot['rightGesture']=='')
    check('Starts with devices off',snapshot['cameraStatus']=='off' and snapshot['voiceStatus']=='off')
    check('Initial pose cannot earn points',snapshot['visible'] is False and max(snapshot['scores'])==0 and snapshot['poseAge']>1)
    check('Unauthenticated read rejected',request('/state',auth=False)[0]==403)
    check('Unauthenticated device configuration rejected',request('/configure',{'camera':True},auth=False)[0]==403)
    check('Invalid camera index rejected',request('/configure',{'cameraIndex':'bad'})[0]==400)
    check('Invalid retry revision rejected',request('/configure',{'cameraRevision':'bad'})[0]==400)
    check('Non-object configuration rejected',request('/configure',[])[0]==400)
    check('Unknown endpoint returns 404',request('/missing')[0]==404)
    check('No frame exposed with camera off',request('/frame')[0]==204)
    check('Device-off configuration accepted',request('/configure',{'camera':False,'voice':False,'cameraIndex':0})[0]==200)
    check('Unauthorized shutdown rejected',request('/shutdown',{},False)[0]==403)
    check('Authenticated shutdown accepted',request('/shutdown',{})[0]==200)
    process.wait(timeout=5)
    check('Service exits cleanly',process.returncode==0)
finally:
    if process.poll() is None:process.kill();process.wait(timeout=3)
    (base.parent/'Docs/Testing/service-results.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(f'{len(results)} service checks passed')
