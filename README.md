# NEONMOVES

Unity 6000.6.6f1 · Windows x64 · Russian interface.

An original upper-body dance game: Nova and Pulse compete across three neon arenas. Each character has six arm-and-hand poses (12 total), articulated fingers and a different animation style. Play seated or standing: only shoulders, elbows, wrists and fingers are used. The complete pose list is in `Docs/Poses.txt`.

## Project decisions

- Two authored, articulated 3D characters and a 2D interface.
- Camera mode: local MediaPipe Pose Landmarker plus Gesture Recognizer for both hands; voice commands: local Vosk Russian model.
- Separate keyboard practice mode; keyboard results never count as camera results.
- Three stages, pose-hold and beat-hit challenges, adaptive difficulty, achievements and local results.
- No shop or payments. Commercialization is described in documentation only.
- GitHub and repository history are the owner's responsibility.

## Development

Open the project in Unity 6000.6.6f1. Use **Neonmoves → Prepare project** and open `Assets/Scenes/Neonmoves.unity`. The scene is generated from authored scripts; all character geometry and music are produced locally.

The editor build entry point is `Neonmoves.Editor.ProjectBuilder.BuildWindows`. Windows output is `Build/Neonmoves/Neonmoves.exe`.

The AI service is loopback-only, starts without opening devices, and activates devices only when enabled inside the game. Camera frames and microphone audio are processed in memory and are not saved. Python packages and model weights are downloaded during setup, not during play.

Detailed controls, architecture, actual validation results and limitations are maintained in `Docs/`.
