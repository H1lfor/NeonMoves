"""Deterministic, camera-independent feature extraction and pose scoring."""
import math
import json
from itertools import permutations
from pathlib import Path

BASE=Path(__file__).resolve().parent
CATALOG=BASE.parent/'Assets/Resources/PoseCatalog.json'
if not CATALOG.exists():CATALOG=BASE/'PoseCatalog.json'
CATALOG_DATA=json.loads(CATALOG.read_text(encoding='utf-8'))
POSES=CATALOG_DATA['poses']
GESTURES={g['id']:g for g in CATALOG_DATA['gestures']}
TARGETS=[tuple(pose['arms']) for pose in POSES]
POSE_COUNT=len(POSES)
REQUIRED = (11,12,13,14,15,16)
ARM_CORE = (11,12,13,14)

class ArmSmoother:
    """Dampen small landmark jitter; never invent joints in a missing/occluded frame."""
    def __init__(self):self.reset()
    def reset(self):self.previous=[];self.time=None
    def apply(self,points,now):
        if len(points)!=33:
            self.reset();return points
        result=[list(p) for p in points]
        dt=now-self.time if self.time is not None else 1
        if len(self.previous)==33 and 0<dt<.25:
            alpha=1-math.exp(-dt/.06)
            for i in ARM_CORE:
                current,old=points[i],self.previous[i]
                if min(current[3],old[3])<.5:continue
                if not all(0<=p[axis]<=1 for p in (current,old) for axis in (0,1)):continue
                # Large intentional moves must follow immediately, not trail behind the player.
                if math.hypot(current[0]-old[0],current[1]-old[1])>.06:continue
                for axis in (0,1,2,4,5):
                    if axis<len(current):result[i][axis]=old[axis]+alpha*(current[axis]-old[axis])
        self.previous=result;self.time=now
        return result

def angle(a, b, outward):
    return math.degrees(math.atan2(a[1] - b[1], (b[0] - a[0]) * outward))

def angular_error(a,b):
    return abs((a-b+180)%360-180)

def match_hands(points,hands):
    """Associate each hand with its arm wrist, even when arms cross; never reuse a hand."""
    if len(points)<17:return [None,None]
    left,right=(15,16) if points[11][0]<points[12][0] else (16,15)
    valid=[h for h in hands if len(h.get('landmarks',[]))==21 and all(-.02<=p[0]<=1.02 and -.02<=p[1]<=1.02 for p in h['landmarks'])]
    if len(valid)<2:return [None,None]
    span=math.hypot(points[11][0]-points[12][0],points[11][1]-points[12][1])
    limit=max(.06,span*.65)
    def distance(wrist,hand):return math.hypot(points[wrist][0]-hand['landmarks'][0][0],points[wrist][1]-hand['landmarks'][0][1])
    a,b=min(permutations(valid,2),key=lambda pair:distance(left,pair[0])+distance(right,pair[1]))
    return [a if distance(left,a)<=limit else None,b if distance(right,b)<=limit else None]

def evaluate(points,hands=None,aspect_ratio=1.0):
    """Score shoulders/arms and two recognized hand gestures. Lower body is never used."""
    if len(points) != 33:
        return [0.0]*POSE_COUNT, False, 0.0
    visible = all(points[i][3] >= .5 and -.04 <= points[i][0] <= 1.04 and -.04 <= points[i][1] <= 1.04 for i in ARM_CORE)
    if not visible:
        return [0.0]*POSE_COUNT, False, min(points[i][3] for i in ARM_CORE)
    # Aspect-correct coordinates supplied in slots 4/5, normalized in 0/1 for framing.
    p = [(x[4],x[5]) if len(x)>5 else (x[0],x[1]) for x in points]
    left = (11,13,15) if p[11][0] < p[12][0] else (12,14,16)
    right = (12,14,16) if left[0] == 11 else (11,13,15)
    matched=match_hands(points,hands or [])
    if not all(matched):return [0.0]*POSE_COUNT,False,0.0
    # The dedicated hand model measures the wrist more precisely than the body model.
    # Both currently visible hands must still be uniquely associated with this player's arms.
    for arm,hand in zip((left,right),matched):
        wrist=hand['landmarks'][0]
        p[arm[2]]=(wrist[0]*aspect_ratio,wrist[1])
    features = (angle(p[left[0]],p[left[1]],-1), angle(p[right[0]],p[right[1]],1), angle(p[left[1]],p[left[2]],-1), angle(p[right[1]],p[right[2]],1))
    scores = []
    for i,target in enumerate(TARGETS):
        arm = sum(max(0, 1-angular_error(a,b)/65) for a,b in zip(features,target))/4
        # A different or uncertain gesture cannot be compensated by perfect arm angles.
        correct=all(h.get('gesture')==expected and h.get('confidence',0)>=.55 for h,expected in zip(matched,POSES[i]['hands']))
        hand_confidence=min(h.get('confidence',0) for h in matched)
        score=arm*.8+hand_confidence*.2 if correct else 0.0
        if arm<.65:score=min(score,.5)
        scores.append(round(max(0,min(1,score)),4))
    return scores,True,min(sum(points[i][3] for i in ARM_CORE)/len(ARM_CORE),min(h.get('confidence',0) for h in matched))

def is_start_gesture(hands):
    """A single clearly visible thumbs-up can start calibration without a full arm pose."""
    return any(hand.get('gesture')=='Thumb_Up' and hand.get('confidence',0)>=.7
               and len(hand.get('landmarks',[]))==21
               and all(0<=p[0]<=1 and 0<=p[1]<=1 for p in hand['landmarks']) for hand in hands)

COMMANDS = {"пауза":"pause", "продолжить":"resume", "старт":"start", "дальше":"next", "меню":"menu", "повтор":"retry", "начать":"start"}
def parse_command(text):
    return COMMANDS.get(text.strip().lower(), "")
