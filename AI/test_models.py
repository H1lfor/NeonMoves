"""Reproducible model checks; synthetic speech is not a human accuracy benchmark."""
import json
from pathlib import Path
import wave
import numpy as np
import mediapipe as mp
from vosk import Model, KaldiRecognizer, SetLogLevel
from pose_math import COMMANDS, parse_command

root=Path(__file__).resolve().parent
output={"speech_source":"Windows SAPI synthetic speech, two voices; not microphone/user testing","speech":[],"pose":[]}
SetLogLevel(-1)
model=Model(str(root/'models/vosk-model-small-ru-0.22'))
for path in sorted((root/'testdata').glob('*.wav')):
    with wave.open(str(path),'rb') as wav:
        rec=KaldiRecognizer(model,wav.getframerate(),json.dumps(list(COMMANDS)+['[unk]'],ensure_ascii=False));rec.SetWords(True)
        rec.AcceptWaveform(wav.readframes(wav.getnframes())+bytes(wav.getframerate()*2))
        result=json.loads(rec.FinalResult());heard=result.get('text','');confidence=min((w.get('conf',0) for w in result.get('result',[])),default=0)
        expected=list(COMMANDS.values())[int(path.stem.rsplit('_',1)[1])]
        actual=parse_command(heard) if confidence>=.7 else ''
        output['speech'].append(dict(file=path.name,expected=expected,heard=heard,confidence=confidence,actual=actual,passed=actual==expected))
options=mp.tasks.vision.PoseLandmarkerOptions(base_options=mp.tasks.BaseOptions(model_asset_path=str(root/'models/pose_landmarker_lite.task')))
with mp.tasks.vision.PoseLandmarker.create_from_options(options) as detector:
    for name,data in [('black',np.zeros((480,640,3),dtype=np.uint8)),('white',np.full((480,640,3),255,dtype=np.uint8)),('noise',np.random.default_rng(19).integers(0,256,(480,640,3),dtype=np.uint8))]:
        result=detector.detect(mp.Image(image_format=mp.ImageFormat.SRGB,data=data))
        output['pose'].append(dict(input=name,people=len(result.pose_landmarks),passed=len(result.pose_landmarks)==0))
out=root.parent/'Docs/Testing/model-results.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({"speech_passed":sum(x['passed'] for x in output['speech']),"speech_total":len(output['speech']),"empty_image_checks":output['pose']},ensure_ascii=False))
