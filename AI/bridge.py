"""Local AI companion. No device is opened before an explicit game configuration."""
import argparse
import json
import os
from pathlib import Path
import queue
import secrets
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pose_math import evaluate, match_hands, is_start_gesture, parse_command, COMMANDS, POSE_COUNT, REQUIRED, ArmSmoother
from camera_io import open_camera

BASE = Path(__file__).resolve().parent
lock = threading.Lock()
stop = threading.Event()
config = {"camera":False,"voice":False,"cameraIndex":0,"cameraRevision":0}
state = {"service":"NeonmovesAI","version":4,"cameraStatus":"off","voiceStatus":"off","cameraError":"","cameraBackend":"","voiceError":"","visible":False,"confidence":0,"scores":[0.0]*POSE_COUNT,"landmarks":[],"hands":[],"leftGesture":"","rightGesture":"","startGesture":False,"frameId":0,"lowLight":False,"poseAge":999.0,"command":"","commandId":0,"heard":"","fps":0.0}
frame_jpeg = b""
last_pose = 0.0
last_poll = time.monotonic()

def update(**values):
    with lock: state.update(values)

def camera_worker():
    global frame_jpeg, last_pose
    capture = detector = gesture_detector = None
    selected = None
    failed_request = None
    smoother=ArmSmoother()
    empty_frames=0
    while not stop.is_set():
        with lock: enabled, index, revision = config["camera"], config["cameraIndex"], config['cameraRevision']
        request=(index,revision)
        if not enabled:
            failed_request = None
            if capture is not None: capture.release(); capture = None
            if detector is not None: detector.close(); detector = None
            if gesture_detector is not None: gesture_detector.close(); gesture_detector = None
            with lock:
                frame_jpeg = b""; last_pose = 0
            update(cameraStatus="off",visible=False,scores=[0.0]*POSE_COUNT,landmarks=[],hands=[],leftGesture="",rightGesture="",startGesture=False,lowLight=False,cameraError="",cameraBackend="")
            stop.wait(.15); continue
        if failed_request == request:
            stop.wait(.2); continue
        try:
            first_frame=None
            if capture is None or selected != request:
                update(cameraStatus="loading",cameraError="")
                import cv2
                import mediapipe as mp
                if capture is not None: capture.release(); capture=None
                if detector is not None: detector.close(); detector=None
                if gesture_detector is not None: gesture_detector.close(); gesture_detector=None
                smoother.reset()
                model = BASE / "models/pose_landmarker_lite.task"
                if not model.exists(): raise RuntimeError("Не найдена модель поз. Запустите установку AI.")
                options = mp.tasks.vision.PoseLandmarkerOptions(base_options=mp.tasks.BaseOptions(model_asset_path=str(model)), running_mode=mp.tasks.vision.RunningMode.VIDEO, num_poses=1, min_pose_detection_confidence=.5, min_pose_presence_confidence=.5, min_tracking_confidence=.5)
                detector = mp.tasks.vision.PoseLandmarker.create_from_options(options)
                gesture_model=BASE/'models/gesture_recognizer.task'
                if not gesture_model.exists():raise RuntimeError('Не найдена модель жестов. Восстанови папку AI/models рядом с игрой.')
                gesture_detector=mp.tasks.vision.GestureRecognizer.create_from_options(mp.tasks.vision.GestureRecognizerOptions(
                    base_options=mp.tasks.BaseOptions(model_asset_path=str(gesture_model)),running_mode=mp.tasks.vision.RunningMode.VIDEO,
                    num_hands=2,min_hand_detection_confidence=.5,min_hand_presence_confidence=.5,min_tracking_confidence=.5))
                capture,first_frame,backend = open_camera(index,stop,cv2)
                empty_frames=0
                selected = request
                update(cameraBackend=backend)
            started = time.monotonic()
            ok, frame = (True,first_frame) if first_frame is not None else capture.read()
            if not ok or frame is None or not frame.size:
                empty_frames+=1
                if empty_frames<=5:stop.wait(.04);continue
                raise RuntimeError("Нет кадров с камеры. Нажми «Подключить заново».")
            empty_frames=0
            frame = cv2.flip(frame,1)
            h,w = frame.shape[:2]
            image = mp.Image(image_format=mp.ImageFormat.SRGB,data=cv2.cvtColor(frame,cv2.COLOR_BGR2RGB))
            timestamp=int(time.monotonic()*1000)
            result = detector.detect_for_video(image,timestamp)
            hand_result=gesture_detector.recognize_for_video(image,timestamp)
            hands=[]
            for landmarks,categories in zip(hand_result.hand_landmarks,hand_result.gestures):
                top=max(categories,key=lambda category:category.score) if categories else None
                hands.append({'gesture':top.category_name if top else '', 'confidence':top.score if top else 0,
                              'landmarks':[[p.x,p.y,p.z] for p in landmarks]})
            pts = [[p.x,p.y,p.z,p.visibility,p.x*w/h,p.y] for p in result.pose_landmarks[0]] if result.pose_landmarks else []
            pts=smoother.apply(pts,time.monotonic())
            scores,visible,confidence = evaluate(pts,hands,aspect_ratio=w/h)
            matched=match_hands(pts,hands)
            low_light=float(frame.mean())<12
            edges = ((11,12),(11,13),(13,15),(12,14),(14,16))
            for a,b in edges:
                if pts and pts[a][3]>.55 and pts[b][3]>.55:
                    cv2.line(frame,(int(pts[a][0]*w),int(pts[a][1]*h)),(int(pts[b][0]*w),int(pts[b][1]*h)),(215,245,70),2,cv2.LINE_AA)
            for hand in hands:
                points=hand['landmarks']
                for chain in ((0,1,2,3,4),(0,5,6,7,8),(5,9,10,11,12),(9,13,14,15,16),(13,17,18,19,20),(0,17)):
                    for a,b in zip(chain,chain[1:]):
                        cv2.line(frame,(int(points[a][0]*w),int(points[a][1]*h)),(int(points[b][0]*w),int(points[b][1]*h)),(80,195,255),2,cv2.LINE_AA)
            _,encoded = cv2.imencode(".jpg",cv2.resize(frame,(640,max(1,round(640*h/w)))),[cv2.IMWRITE_JPEG_QUALITY,75])
            elapsed = max(time.monotonic()-started,.001)
            with lock:
                if not config['camera'] or (config['cameraIndex'],config['cameraRevision'])!=request:continue
                frame_jpeg = encoded.tobytes(); last_pose = time.monotonic()
                state.update(cameraStatus="ready",cameraError="",visible=visible,confidence=confidence,scores=scores,
                    startGesture=is_start_gesture(hands),frameId=state['frameId']+1,
                    landmarks=[{"x":pts[i][0],"y":pts[i][1],"visibility":pts[i][3]} for i in REQUIRED] if pts else [],hands=hands,
                    leftGesture=matched[0]['gesture'] if matched[0] else '',rightGesture=matched[1]['gesture'] if matched[1] else '',lowLight=low_light,fps=round(1/elapsed,1))
            stop.wait(max(0,.05-elapsed))
        except Exception as exc:
            with lock:frame_jpeg=b'';last_pose=0
            update(cameraStatus="error",cameraError=str(exc),visible=False,scores=[0.0]*POSE_COUNT,landmarks=[],hands=[],leftGesture="",rightGesture="",startGesture=False,lowLight=False)
            failed_request = request
            if capture is not None: capture.release(); capture=None
            if detector is not None: detector.close(); detector=None
            if gesture_detector is not None: gesture_detector.close(); gesture_detector=None
    if capture is not None: capture.release()
    if detector is not None: detector.close()
    if gesture_detector is not None: gesture_detector.close()

def voice_worker():
    failed = False
    model = None
    while not stop.is_set():
        with lock: enabled = config["voice"]
        if not enabled:
            failed = False; update(voiceStatus="off",voiceError=""); stop.wait(.2); continue
        if failed: stop.wait(.2); continue
        try:
            update(voiceStatus="loading",voiceError="")
            import sounddevice as sd
            from vosk import Model, KaldiRecognizer, SetLogLevel
            SetLogLevel(-1)
            if model is None: model = Model(str(BASE / "models/vosk-model-small-ru-0.22"))
            sample_rate = int(sd.query_devices(kind="input")["default_samplerate"])
            rec = KaldiRecognizer(model,sample_rate,json.dumps(list(COMMANDS)+["[unk]"],ensure_ascii=False))
            rec.SetWords(True)
            audio = queue.Queue(maxsize=24)
            def callback(data, frames, timing, status):
                try: audio.put_nowait(bytes(data))
                except queue.Full: pass
            with sd.RawInputStream(samplerate=sample_rate,blocksize=4096,dtype="int16",channels=1,callback=callback):
                update(voiceStatus="ready")
                while not stop.is_set():
                    with lock: enabled = config["voice"]
                    if not enabled: break
                    try: data = audio.get(timeout=.2)
                    except queue.Empty: continue
                    if rec.AcceptWaveform(data):
                        result = json.loads(rec.Result()); heard = result.get("text","")
                        command = parse_command(heard)
                        words = result.get("result",[])
                        confidence = min((w.get("conf",0) for w in words),default=0)
                        with lock:
                            state["heard"] = heard
                            if command and confidence >= .7:
                                state["command"] = command; state["commandId"] += 1
        except Exception as exc:
            failed=True; update(voiceStatus="error",voiceError=str(exc))

class Handler(BaseHTTPRequestHandler):
    def log_message(self,*args): pass
    def send_data(self,body,ctype="application/json",code=200):
        self.send_response(code); self.send_header("Content-Type",ctype); self.send_header("Content-Length",str(len(body))); self.send_header("Cache-Control","no-store"); self.end_headers()
        try: self.wfile.write(body)
        except (BrokenPipeError,ConnectionResetError): pass
    def authorized(self):
        return secrets.compare_digest(self.headers.get("X-Neonmoves-Token",""),self.server.token)
    def do_GET(self):
        global last_poll
        if not self.authorized(): self.send_data(b'{}',code=403); return
        last_poll = time.monotonic()
        if self.path == "/state":
            with lock:
                payload = dict(state); payload["poseAge"] = time.monotonic()-last_pose if last_pose else 999
            self.send_data(json.dumps(payload,ensure_ascii=False).encode("utf-8"))
        elif self.path == "/frame":
            with lock: data = frame_jpeg
            self.send_data(data,"image/jpeg",200 if data else 204)
        else: self.send_data(b'{}',code=404)
    def do_POST(self):
        global frame_jpeg,last_pose
        if not self.authorized(): self.send_data(b'{}',code=403); return
        try:
            size = int(self.headers.get("Content-Length","0"))
            if size < 0 or size > 1024: raise ValueError("Invalid request size")
            body = json.loads(self.rfile.read(size) or b'{}')
            if not isinstance(body,dict):raise ValueError('Expected an object')
            if self.path == "/configure":
                camera=body.get('camera') is True
                index=max(0,min(4,int(body.get('cameraIndex',0))))
                revision=int(body.get('cameraRevision',0))
                with lock:
                    if not camera or index!=config['cameraIndex'] or revision!=config['cameraRevision'] or not config['camera']:
                        frame_jpeg=b'';last_pose=0
                        state.update(cameraStatus='loading' if camera else 'off',cameraError='',visible=False,scores=[0.0]*POSE_COUNT,landmarks=[],hands=[],leftGesture='',rightGesture='',startGesture=False,lowLight=False)
                    config.update(camera=camera,voice=body.get("voice") is True,cameraIndex=index,cameraRevision=revision)
            elif self.path == "/shutdown": stop.set()
            else: self.send_data(b'{}',code=404); return
            self.send_data(b'{"ok":true}')
        except (ValueError,TypeError): self.send_data(b'{"error":"invalid request"}',code=400)

def main():
    parser=argparse.ArgumentParser(); parser.add_argument("--port",type=int,required=True); parser.add_argument("--token",required=True)
    args=parser.parse_args()
    server=ThreadingHTTPServer(("127.0.0.1",args.port),Handler); server.token=args.token; server.timeout=.25
    threads=[threading.Thread(target=camera_worker,daemon=True),threading.Thread(target=voice_worker,daemon=True)]
    for thread in threads: thread.start()
    print("Neonmoves AI service ready",flush=True)
    while not stop.is_set():
        server.handle_request()
        # A crashed/closed game cannot leave recording devices open indefinitely.
        if time.monotonic()-last_poll>12: stop.set()
    server.server_close(); stop.set()
    for thread in threads: thread.join(timeout=3)

if __name__ == "__main__": main()
