import math
import unittest
from pose_math import evaluate, match_hands, is_start_gesture, TARGETS, POSES, POSE_COUNT, GESTURES, parse_command, angular_error, ArmSmoother

def fixture(pose,mirror=False,scale=1.0,dx=0):
    points=[[.5,.5,0,1] for _ in range(33)]
    a,b,c,d=TARGETS[pose]
    for shoulder,elbow,wrist,side,upper,lower in ((11,13,15,-1,a,c),(12,14,16,1,b,d)):
        points[shoulder]=[.5+side*.13,.30,0,1]
        points[elbow]=[points[shoulder][0]+side*math.cos(math.radians(upper))*.15,points[shoulder][1]-math.sin(math.radians(upper))*.15,0,1]
        points[wrist]=[points[elbow][0]+side*math.cos(math.radians(lower))*.14,points[elbow][1]-math.sin(math.radians(lower))*.14,0,1]
    # Lower-body landmarks are deliberately invisible and outside the image.
    for i in range(23,33):points[i]=[.5,2.0,0,0]
    for p in points:
        p[0]=.5+(p[0]-.5)*scale+dx;p[1]=.5+(p[1]-.5)*scale
        if mirror:p[0]=1-p[0]
    return points

def hands_for(pose,points=None):
    points=points or fixture(pose)
    return [{'gesture':gesture,'confidence':.98,'landmarks':[[points[wrist][0],points[wrist][1],0] for _ in range(21)]}
            for wrist,gesture in zip((15,16),POSES[pose]['hands'])]

def score(pose,**options):
    points=fixture(pose,**options)
    return evaluate(points,hands_for(pose,points))

class PoseTests(unittest.TestCase):
    def test_all_reference_poses_match(self):
        for pose in range(POSE_COUNT):
            with self.subTest(pose=pose):
                scores,visible,_=score(pose);self.assertTrue(visible);self.assertGreater(scores[pose],.9);self.assertEqual(scores.index(max(scores)),pose)
    def test_no_person_has_zero_scores(self):self.assertEqual(evaluate([]),([0.0]*POSE_COUNT,False,0.0))
    def test_hand_model_recovers_uncertain_body_wrist(self):
        pts=fixture(0);pts[15][3]=.2
        scores,visible,_=evaluate(pts,hands_for(0,pts));self.assertTrue(visible);self.assertGreater(scores[0],.9)
    def test_missing_hand_still_blocks_with_uncertain_body_wrist(self):
        pts=fixture(0);pts[15][3]=.2
        self.assertFalse(evaluate(pts,hands_for(0,pts)[:1])[1])
    def test_occluded_elbow_blocks_scoring(self):
        pts=fixture(0);pts[13][3]=.2
        scores,visible,_=evaluate(pts,hands_for(0,pts));self.assertFalse(visible);self.assertEqual(max(scores),0)
    def test_hand_wrist_corrects_body_wrist_jitter(self):
        pts=fixture(0);hands=hands_for(0,pts)
        pts[15][0]+=.035;pts[15][1]-=.035
        self.assertAlmostEqual(evaluate(pts,hands)[0][0],score(0)[0][0])
    def test_hd_wrist_coordinates_use_frame_aspect(self):
        pts=fixture(0);hands=hands_for(0,pts);aspect=16/9
        for point in pts:point[0]/=aspect
        for hand in hands:
            for point in hand['landmarks']:point[0]/=aspect
        pts=[p+[p[0]*aspect,p[1]] for p in pts]
        self.assertAlmostEqual(evaluate(pts,hands,aspect)[0][0],score(0)[0][0])
    def test_invisible_or_moving_lower_body_does_not_affect_score(self):
        pts=fixture(0);before=evaluate(pts,hands_for(0,pts))
        for i in range(23,33):pts[i]=[99,-99,99,1]
        self.assertEqual(before,evaluate(pts,hands_for(0,pts)));self.assertTrue(before[1])
    def test_translation_and_scale_invariance(self):
        for pose in range(POSE_COUNT):self.assertAlmostEqual(score(pose)[0][pose],score(pose,scale=.7,dx=.05)[0][pose],places=3)
    def test_mirrored_side_changes_expected_pose(self):
        scores,_,_=score(2,mirror=True);self.assertEqual(scores.index(max(scores)),3)
    def test_wrong_fingers_cannot_pass_perfect_arm_pose(self):
        pts=fixture(5);hands=hands_for(5);hands[0]['gesture']='Closed_Fist'
        self.assertEqual(evaluate(pts,hands)[0][5],0)
    def test_missing_hand_freezes_tracking_and_scores_zero(self):
        scores,visible,_=evaluate(fixture(0),hands_for(0)[:1]);self.assertFalse(visible);self.assertEqual(max(scores),0)
    def test_arms_without_finger_detection_cannot_score(self):
        self.assertEqual(evaluate(fixture(0)),([0.0]*POSE_COUNT,False,0.0))
    def test_uncertain_gesture_cannot_score(self):
        hands=hands_for(0);hands[0]['confidence']=.4
        self.assertEqual(evaluate(fixture(0),hands)[0][0],0)
    def test_unknown_gesture_cannot_score(self):
        hands=hands_for(0);hands[0]['gesture']='None'
        self.assertEqual(evaluate(fixture(0),hands)[0][0],0)
    def test_wrong_arm_pose_cannot_pass_on_fingers_alone(self):
        self.assertLess(score(1)[0][0],.72)
    def test_cropped_fingertip_blocks_scoring(self):
        hands=hands_for(0);hands[0]['landmarks'][8][0]=1.2
        self.assertFalse(evaluate(fixture(0),hands)[1])
    def test_wrapped_angles(self):self.assertEqual(angular_error(179,-179),2)
    def test_correct_aspect_features(self):
        pts=fixture(0);pts=[p+[p[0],p[1]] for p in pts];self.assertGreater(evaluate(pts,hands_for(0,pts))[0][0],.9)
    def test_missing_landmarks(self):self.assertFalse(evaluate(fixture(0)[:20])[1])
    def test_two_distinct_six_pose_sets(self):
        self.assertEqual(POSE_COUNT,12)
        self.assertEqual(len({(tuple(p['arms']),tuple(p['hands'])) for p in POSES}),12)
        self.assertTrue(all('squat' not in p for p in POSES))
        self.assertEqual(len(GESTURES),6)
    def test_pulse_mirror_pairs(self):
        for original,mirrored in ((7,8),(8,7),(9,10),(10,9)):
            scores,visible,_=score(original,mirror=True);self.assertTrue(visible);self.assertEqual(scores.index(max(scores)),mirrored)
    def test_identical_arms_require_distinct_fingers(self):
        for a,b in ((5,6),(5,11),(6,11)):
            self.assertEqual(score(a)[0][b],0);self.assertEqual(score(b)[0][a],0)
    def test_hand_order_does_not_swap_sides(self):
        pts=fixture(2);hands=hands_for(2)
        self.assertEqual(evaluate(pts,hands),evaluate(pts,list(reversed(hands))))
    def test_crossed_hands_follow_arm_wrist_not_screen_position(self):
        pts=fixture(4);hands=hands_for(4)
        self.assertGreater(pts[15][0],pts[16][0])
        self.assertIs(match_hands(pts,list(reversed(hands)))[0],hands[0])
    def test_far_away_hands_cannot_be_attached_to_player(self):
        hands=hands_for(0)
        for hand in hands:
            for point in hand['landmarks']:point[0]=.99;point[1]=.99
        self.assertFalse(evaluate(fixture(0),hands)[1])

class SmoothingTests(unittest.TestCase):
    def test_small_jitter_is_damped(self):
        smoother=ArmSmoother();first=fixture(0);smoother.apply(first,0)
        second=fixture(0);second[13][0]+=.02
        result=smoother.apply(second,.05)
        self.assertGreater(result[13][0],first[13][0]);self.assertLess(result[13][0],second[13][0])
        self.assertEqual(second[13][0],first[13][0]+.02)
    def test_large_deliberate_movement_has_no_lag(self):
        smoother=ArmSmoother();smoother.apply(fixture(0),0)
        second=fixture(1);self.assertEqual(smoother.apply(second,.05)[13],second[13])
    def test_missing_frame_never_reuses_last_pose(self):
        smoother=ArmSmoother();smoother.apply(fixture(0),0)
        self.assertEqual(smoother.apply([],.05),[])
        second=fixture(0);second[13][0]+=.02
        self.assertEqual(smoother.apply(second,.1),second)
    def test_low_confidence_is_not_hidden(self):
        smoother=ArmSmoother();smoother.apply(fixture(0),0)
        second=fixture(0);second[13][0]+=.02;second[13][3]=.2
        result=smoother.apply(second,.05)
        self.assertEqual(result[13],second[13]);self.assertFalse(evaluate(result,hands_for(0))[1])
    def test_long_gap_resets_smoothing(self):
        smoother=ArmSmoother();smoother.apply(fixture(0),0)
        second=fixture(0);second[13][0]+=.02
        self.assertEqual(smoother.apply(second,1),second)
    def test_frame_edge_is_not_pulled_back_inside(self):
        smoother=ArmSmoother();first=fixture(0);first[13][0]=.99;smoother.apply(first,0)
        second=fixture(0);second[13][0]=1.045
        self.assertEqual(smoother.apply(second,.05)[13],second[13])

class VoiceTests(unittest.TestCase):
    def test_all_commands(self):
        for text,result in (("пауза","pause"),("продолжить","resume"),("старт","start"),("дальше","next"),("меню","menu"),("повтор","retry")):self.assertEqual(parse_command(text),result)
    def test_unknown_speech_ignored(self):self.assertEqual(parse_command("случайный разговор"),"")
    def test_partial_word_ignored(self):self.assertEqual(parse_command("пауз"),"")
    def test_embedded_command_ignored(self):self.assertEqual(parse_command("не говори пауза"),"")
    def test_whitespace_case(self):self.assertEqual(parse_command("  ПАУЗА  "),"pause")
    def test_start_synonyms(self):
        for word in ('старт','начать','  НАЧАТЬ  '):self.assertEqual(parse_command(word),'start')
    def test_start_is_not_extracted_from_conversation(self):
        for text in ('не надо начать','перестарт','начать потом'):self.assertEqual(parse_command(text),'')

class StartGestureTests(unittest.TestCase):
    def test_one_thumb_without_full_body_can_start(self):self.assertTrue(is_start_gesture(hands_for(9)[:1]))
    def test_either_hand_can_start(self):self.assertTrue(is_start_gesture(hands_for(10)[1:]))
    def test_other_gestures_cannot_start(self):
        for pose in (0,1,2,3,4,5,6,7,8,11):self.assertFalse(is_start_gesture(hands_for(pose)))
    def test_uncertain_thumb_cannot_start(self):
        hand=hands_for(9)[0];hand['confidence']=.69;self.assertFalse(is_start_gesture([hand]))
    def test_partial_thumb_cannot_start(self):
        hand=hands_for(9)[0];hand['landmarks'][4][1]=-.05;self.assertFalse(is_start_gesture([hand]))
    def test_missing_hand_cannot_start(self):self.assertFalse(is_start_gesture([]))

if __name__=="__main__":unittest.main(verbosity=2)
