"""Real model inference on the public images used by Google's Gesture Recognizer sample."""
import json
from pathlib import Path
import cv2
import mediapipe as mp
import numpy as np
from pose_math import is_start_gesture

base=Path(__file__).resolve().parent
results=[]
options=mp.tasks.vision.GestureRecognizerOptions(base_options=mp.tasks.BaseOptions(model_asset_path=str(base/'models/gesture_recognizer.task')),num_hands=2)
with mp.tasks.vision.GestureRecognizer.create_from_options(options) as detector:
    for filename,expected in (('victory.jpg','Victory'),('thumbs_up.jpg','Thumb_Up'),('pointing_up.jpg','Pointing_Up')):
        frame=cv2.imread(str(base/'fixtures/gestures'/filename))
        assert frame is not None,filename
        for mirror in (False,True):
            rgb=cv2.cvtColor(cv2.flip(frame,1) if mirror else frame,cv2.COLOR_BGR2RGB)
            found=detector.recognize(mp.Image(image_format=mp.ImageFormat.SRGB,data=rgb))
            labels=[category for hand in found.gestures for category in hand]
            passed=any(c.category_name==expected and c.score>=.55 for c in labels) and all(len(h)==21 for h in found.hand_landmarks)
            payload=[{'gesture':categories[0].category_name,'confidence':categories[0].score,'landmarks':[[p.x,p.y,p.z] for p in landmarks]} for landmarks,categories in zip(found.hand_landmarks,found.gestures) if categories]
            start=is_start_gesture(payload)
            passed=passed and start==(expected=='Thumb_Up')
            results.append({'image':filename,'mirrored':mirror,'expected':expected,'passed':passed,'startGesture':start,'detected':[{'gesture':c.category_name,'confidence':round(c.score,4)} for c in labels]})
    for brightness in (0,255):
        found=detector.recognize(mp.Image(image_format=mp.ImageFormat.SRGB,data=np.full((480,640,3),brightness,dtype=np.uint8)))
        results.append({'blank':brightness,'passed':not found.hand_landmarks})
(base.parent/'Docs/Testing/gesture-model-results.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(json.dumps(results,indent=2))
assert all(item['passed'] for item in results),'Gesture inference check failed'
print(f'{len(results)} real model checks passed')
