"""Prepare a relocatable, project-local Python runtime and model assets."""
import pathlib
import subprocess
import sys
import urllib.request
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
AI = ROOT / "AI"
RUNTIME = AI / "runtime"
MODELS = AI / "models"

def download(url, path):
    if path.exists():
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    print(f"Downloading {path.name}", flush=True)
    temp = path.with_suffix(path.suffix + ".download")
    urllib.request.urlretrieve(url, temp)
    temp.replace(path)

def main():
    RUNTIME.mkdir(parents=True, exist_ok=True)
    MODELS.mkdir(parents=True, exist_ok=True)
    archive = AI / "python-3.12.8-embed-amd64.zip"
    download("https://www.python.org/ftp/python/3.12.8/python-3.12.8-embed-amd64.zip", archive)
    if not (RUNTIME / "python.exe").exists():
        with zipfile.ZipFile(archive) as z:
            z.extractall(RUNTIME)
    (RUNTIME / "python312._pth").write_text("python312.zip\n.\nLib/site-packages\n..\nimport site\n", encoding="utf-8")
    bootstrap = AI / "get-pip.py"
    download("https://bootstrap.pypa.io/get-pip.py", bootstrap)
    python = str(RUNTIME / "python.exe")
    if not (RUNTIME / "Lib/site-packages/pip").exists():
        subprocess.check_call([python, str(bootstrap), "--no-warn-script-location"])
    subprocess.check_call([python, "-m", "pip", "install", "--no-warn-script-location", "setuptools", "wheel"])
    subprocess.check_call([python, "-m", "pip", "install", "--no-build-isolation", "--no-warn-script-location", "--disable-pip-version-check", "-r", str(AI / "requirements.txt")])
    download("https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/1/pose_landmarker_lite.task", MODELS / "pose_landmarker_lite.task")
    download("https://storage.googleapis.com/mediapipe-models/gesture_recognizer/gesture_recognizer/float16/1/gesture_recognizer.task", MODELS / "gesture_recognizer.task")
    voice = AI / "vosk-model-small-ru-0.22.zip"
    download("https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip", voice)
    if not (MODELS / "vosk-model-small-ru-0.22").exists():
        with zipfile.ZipFile(voice) as z:
            z.extractall(MODELS)
    font_dir = ROOT / "Assets/Resources/Fonts"
    for name in ("NotoSans-Regular.ttf", "NotoSans-Bold.ttf"):
        download("https://raw.githubusercontent.com/notofonts/noto-fonts/main/hinted/ttf/NotoSans/" + name, font_dir / name)
    download("https://raw.githubusercontent.com/notofonts/noto-fonts/main/LICENSE", ROOT / "ThirdParty/NotoSans-LICENSE.txt")
    print("AI runtime, models and fonts ready.", flush=True)

if __name__ == "__main__":
    main()
