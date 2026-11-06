"""Open a Windows camera and wait for its first usable frame before accepting it."""
def open_camera(index,stop,cv2):
    failures=[]
    for backend,name in ((cv2.CAP_DSHOW,'DirectShow'),(cv2.CAP_MSMF,'Media Foundation')):
        if stop.is_set():raise RuntimeError('Подключение камеры остановлено.')
        capture=None
        try:
            capture=cv2.VideoCapture(index,backend)
            if not capture.isOpened():
                failures.append(name+': устройство не открылось');continue
            # More source pixels help the hand crop retain finger detail. Drivers can
            # negotiate their nearest supported mode; startup still verifies actual frames.
            if hasattr(cv2,'CAP_PROP_FOURCC'):capture.set(cv2.CAP_PROP_FOURCC,cv2.VideoWriter_fourcc(*'MJPG'))
            if hasattr(cv2,'CAP_PROP_FPS'):capture.set(cv2.CAP_PROP_FPS,30)
            if hasattr(cv2,'CAP_PROP_BUFFERSIZE'):capture.set(cv2.CAP_PROP_BUFFERSIZE,1)
            for width,height in ((1280,720),(640,480)):
                capture.set(cv2.CAP_PROP_FRAME_WIDTH,width)
                capture.set(cv2.CAP_PROP_FRAME_HEIGHT,height)
                for _ in range(8):
                    if stop.is_set():break
                    ok,frame=capture.read()
                    if ok and frame is not None and frame.size:
                        ready=capture;capture=None
                        return ready,frame,name
                    stop.wait(.1)
            failures.append(name+': нет кадров')
        except Exception as exc:failures.append(name+': '+str(exc))
        finally:
            if capture is not None:capture.release()
    raise RuntimeError('Камера № '+str(index)+' не передаёт изображение. Закрой другие приложения с камерой, проверь её шторку и доступ Windows к камере для классических приложений, затем нажми «Подключить заново». ('+'; '.join(failures)+')')
