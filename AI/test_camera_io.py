import unittest
from camera_io import open_camera

class Frame:
    size=10
class Stop:
    stopped=False
    def is_set(self):return self.stopped
    def wait(self,seconds):pass
class Capture:
    def __init__(self,opened=True,frames=None):
        self.opened=opened;self.frames=list(frames or []);self.released=False;self.settings=[]
    def isOpened(self):return self.opened
    def set(self,*args):self.settings.append(args)
    def read(self):return self.frames.pop(0) if self.frames else (False,None)
    def release(self):self.released=True
class CV:
    CAP_DSHOW=700;CAP_MSMF=1400;CAP_PROP_FRAME_WIDTH=3;CAP_PROP_FRAME_HEIGHT=4
    def __init__(self,*captures):self.captures=list(captures);self.calls=[]
    def VideoCapture(self,index,backend):
        self.calls.append((index,backend));result=self.captures.pop(0)
        if isinstance(result,Exception):raise result
        return result

class CameraTests(unittest.TestCase):
    def test_requests_hd_resolution(self):
        capture=Capture(frames=[(True,Frame())]);cv=CV(capture);open_camera(0,Stop(),cv)
        self.assertIn((cv.CAP_PROP_FRAME_WIDTH,1280),capture.settings)
        self.assertIn((cv.CAP_PROP_FRAME_HEIGHT,720),capture.settings)
    def test_unsupported_hd_falls_back_to_standard_resolution(self):
        capture=Capture(frames=[(False,None)]*8+[(True,Frame())]);cv=CV(capture)
        self.assertIs(open_camera(0,Stop(),cv)[0],capture)
        self.assertIn((cv.CAP_PROP_FRAME_WIDTH,640),capture.settings)
        self.assertIn((cv.CAP_PROP_FRAME_HEIGHT,480),capture.settings)
    def test_success_uses_selected_index_without_opening_other_backend(self):
        capture=Capture(frames=[(True,Frame())]);cv=CV(capture)
        result,frame,name=open_camera(2,Stop(),cv)
        self.assertIs(result,capture);self.assertEqual(cv.calls,[(2,cv.CAP_DSHOW)]);self.assertFalse(capture.released);self.assertEqual(name,'DirectShow')
    def test_failed_backend_released_before_fallback(self):
        first=Capture(False);second=Capture(frames=[(True,Frame())]);cv=CV(first,second)
        result,_,name=open_camera(0,Stop(),cv)
        self.assertTrue(first.released);self.assertIs(result,second);self.assertEqual(name,'Media Foundation')
    def test_empty_startup_frames_are_retried(self):
        capture=Capture(frames=[(False,None),(True,None),(True,Frame())]);cv=CV(capture)
        self.assertIs(open_camera(0,Stop(),cv)[0],capture);self.assertEqual(len(cv.calls),1)
    def test_no_frames_reports_error_and_releases_both_devices(self):
        first=Capture();second=Capture();cv=CV(first,second)
        with self.assertRaisesRegex(RuntimeError,'Камера № 3'):open_camera(3,Stop(),cv)
        self.assertTrue(first.released and second.released)
    def test_stopped_request_opens_no_device(self):
        stop=Stop();stop.stopped=True;cv=CV()
        with self.assertRaises(RuntimeError):open_camera(0,stop,cv)
        self.assertEqual(cv.calls,[])
    def test_native_backend_exception_uses_fallback(self):
        second=Capture(frames=[(True,Frame())]);cv=CV(OSError('driver error'),second)
        self.assertIs(open_camera(0,Stop(),cv)[0],second)

if __name__=='__main__':unittest.main(verbosity=2)
