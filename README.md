# TacticalMR

Multiplayer soccer simulation in Unity, driven by [Scenic](https://scenic-lang.readthedocs.io/) scenarios, used to record narrated demonstrations in VR and to review and give feedback on the programs synthesized from them.

This branch holds the whole soccer study in one project:

| What | Where |
|---|---|
| Record narrated demonstrations (Quest headset + laptop spectator) | `Assets/Scenes/zmq_demo_vr.unity` |
| View the FSM, run synthesized programs, record feedback (laptop only, offline) | `Assets/Scenes/zmq_demo_controller.unity` |
| View the final synthesized program in VR | `Assets/Scenes/zmq_demo_vr_viewer.unity` |

The factory study lives on the `factory-new` branch (Unity 6000.0.33f1).

## Requirements

- **Unity 2022.3.13f1** with the **Android Build Support** module ([editor archive](https://unity.com/releases/editor/archive)). Stay on 2022.3: the Unity 6 / newer Meta package upgrade on `main` broke the soccer setup. A later 2022.3 patch (2022.3.21+) fixes an A* bug (see Troubleshooting) but hasn't been tested with this project yet.
- **Python 3.8+** and the Scenic in this repo (`Scenic-main`, version 3.0.0b2). Upstream Scenic lacks the Unity simulator used here.
- **Meta Quest 3** on the same Wi-Fi network as the laptop (VR steps only).
- The [narrated_demo](https://github.com/ek65/narrated_demo) repo, branch `soccer-vr`, for program synthesis.
- Your own **OpenAI** and **ElevenLabs** API keys. Never commit them.

## Setup

### Scenic

1. Create and activate a Python environment, then `python -m pip install --upgrade pip`.
2. `cd TacticalMR/Scenic-main` and `python -m pip install -e .`
   - On Apple Silicon, `python-fcl` may need a manual install first; see [Scenic's install notes](https://scenic-lang.readthedocs.io/en/latest/install_notes.html#installing-python-fcl-on-apple-silicon). Clone `python-fcl` outside this repo.
3. `scenic --version` should print 3.0.0b2.

### Unity

1. In Unity Hub, choose **Add → Add project from disk** and select `TacticalMR/UnityProject`. Open it with 2022.3.13f1.
2. Go to **File → Build Settings** and switch to **Android**. The laptop-only scene also runs with Android selected.
3. API keys:
   - **OpenAI:** select `Assets/Resources/OpenAIConfiguration` and paste your key into **Api Key**. Without it, the OpenAI client logs an `InvalidCredentialException` on start.
   - **ElevenLabs** (speech-to-text for narrations and feedback): in each scene, select the **Scribe** object and set **Eleven Labs Api Key**.
4. Quest builds only: open **Edit → Project Settings → Player → Publishing Settings**. Use the project keystore `UnityProject/key.keystore` with key alias `tactical`. The passwords are in the lab setup doc.
5. In the **Game** view, add and select a **1200 × 1080** resolution.

### narrated_demo (synthesis pipeline)

1. Clone [narrated_demo](https://github.com/ek65/narrated_demo) and check out `soccer-vr`.
2. Create `v2/apiKey.py` containing `OPENAI_API_KEY = '...'` and `GEMINI_API_KEY = '...'`.
3. Run `conda env create -f environment.yml`.
4. Point the scripts at your clone of this repo:

   | Variable | File(s) | Value |
   |---|---|---|
   | `TACTICAL_MR_DIR` | `v2/auto_synthesis.py`, `v2/auto_feedback.py` | `<path>/TacticalMR` |
   | `DATA_BASE_PATH` | `v2/auto_synthesis.py`, `v2/auto_fsm.py`, `v2/auto_feedback.py` | `<path>/narrated_demo/v2/data` |
   | `UNITY_FSM_PATH` | `v2/auto_fsm.py` | `<path>/TacticalMR/UnityProject/Assets/Resources/_FSM` |
   | `TACTICAL_MR_OUTPUT` | `v2/auto_feedback.py` | `<path>/TacticalMR/output/participant0/Test` |

## Running the soccer study

Unity is the ZMQ server on port 5555. Scenic connects to `param address`, which defaults to `localhost` in `Scenic-main/src/scenic/simulators/unity/model.scenic`. For the headset, pass `--param address <headset-ip>` rather than editing `model.scenic`. Run every `scenic` command from `Scenic-main` with your environment active.

Scenarios: `examples/unity/check.scenic` (the "lure" scenario), `examples/unity/distribute.scenic` and `examples/unity/overlap.scenic`.

### 1. Record narrated demonstrations (VR)

1. Open `zmq_demo_vr`. On **ZMQManager → ZMQ Server**, set **Ip** to the headset's IP. On the Quest, find it under Settings → Wi-Fi → your network → Network details (or Advanced).
2. On **ZMQManager → JSON Directory**, set the participant number and drill name. Recordings go to `TacticalMR/output/participant<N>/<drill>/`.
3. In **File → Build Settings**, set **Run Device** to your Quest 3 and tick only `Scenes/zmq_demo_vr`. Then click **Build And Run**. Rebuild whenever the headset IP changes. On the headset, the app is in Library → Unknown Sources as "TacticalMR".
4. Press **Play** in the Unity Editor on the laptop. The laptop joins the headset's session as a spectator with a top-down view, and records the video and JSON.
5. Run a scenario, for example: `scenic examples/unity/check.scenic -S -b --param address <headset-ip>`
6. The participant pauses, starts recording, and narrates; see the controls below. Record **two** demonstrations. Each one saves a JSON file and a video.

### 2. Synthesize a program

1. In `narrated_demo/v2/data/_NARRATED_DEMOS/`, create a folder (e.g. `pilot0`). Copy the demonstration folders from `TacticalMR/output` into it, named `<scenario>-<pilot>`, e.g. `check-pilot0`, `distribute-pilot0` or `overlap-pilot0`.
2. Run `python v2/auto_synthesis.py pilot0` (from `narrated_demo`). This writes `Scenic-main/examples/unity/_SYNTHESIZED_PROGRAM/synthesized_program.scenic`.

### 3. View the FSM and record feedback on it (laptop)

1. Run `python v2/auto_fsm.py pilot0`. This writes `UnityProject/Assets/Resources/_FSM/fsm.json`.
2. Open `zmq_demo_controller`, enable the **FSMCanvas** object, and press **Play**. The FSM is read when the scene starts, so let Unity reimport `fsm.json` first.
3. Press **B** to start recording feedback and **B** again to stop. The recording is saved to `output/participant0/Test`.
4. If feedback was given, run `python v2/auto_feedback.py pilot0 --fsm`. This replaces `synthesized_program.scenic`.

### 4. Run the synthesized program and record feedback (laptop)

1. In `zmq_demo_controller`, disable **FSMCanvas** and press **Play**. The ZMQ Server IP is `localhost` in this scene.
2. Run `scenic examples/unity/_SYNTHESIZED_PROGRAM/synthesized_program.scenic -S -b`
3. The participant watches one full run. To give feedback, press **E**, choose **No** (restart without saving), then **P** right away to pause.
4. Press **B** to start recording, then **P** to play. Pause with **P** at any time. Press **B** to stop.
5. Run `python v2/auto_feedback.py pilot0 --feedback`

### 5. Final FSM and program

- Run `python v2/auto_fsm.py pilot0 --fsm`, or `--feedback` if feedback was given on the program. Then view it as in step 3.
- Run the program as in step 4.

### 6. View the final program in VR

1. Open `zmq_demo_vr_viewer` and set the ZMQ Server IP to the headset's IP, as in step 1.
2. Build And Run with only `Scenes/zmq_demo_vr_viewer` ticked.
3. Run `scenic examples/unity/_SYNTHESIZED_PROGRAM/synthesized_program.scenic -S -b --param address <headset-ip>`. The headset shows the program from a third-person view.

## Controls

**Keyboard and mouse (laptop):**
- WASD: move
- **P**: pause or unpause
- **B**: start or stop a recording segment (this also pauses)
- **E**: open the save & restart prompt. **Yes** marks the demonstration usable and restarts; **No** just restarts.
- Mouse: click the ground or players to annotate

**Gamepad:**
- Left stick: move
- A: pause
- Y: save & restart prompt
- X: start or stop recording
- Left trigger: intercept
- Right trigger: pass
- Right bumper: through pass

**Quest controllers:**

![Quest controller buttons](UnityProject/Assets/tacticalmr%20quest%20controller%20buttons.png)

## Platform notes

- **Windows vs macOS:** `ZMQServer` calls `NetMQConfig.Cleanup` only on Windows. Unity hangs on exit without it on Windows, and it crashes the editor on macOS.
- **Unity on Windows, Scenic in WSL2:** create `C:\Users\<Username>\.wslconfig` containing:
  ```
  [wsl2]
  networkingMode=mirrored
  ```

## Troubleshooting

- **Scenic players spawn but don't move, and the Console shows `The referenced script (Unknown) on this Behaviour is missing!`.** This is a Unity 2022.3.13 bug in which A* Pathfinding components (`Seeker`, `AIDestinationSetter`, `RVOController`) stop loading after a recompile. A* warns about it on startup. Fix: in the Project window, right-click **Packages → A\* Pathfinding Project → Reimport**. The permanent fix is Unity 2022.3.21 or later.
- **`InvalidCredentialException: apiKey` when you press Play.** The OpenAI key in `Assets/Resources/OpenAIConfiguration` is empty.
- **One `ArgumentOutOfRangeException` from `ZMQServer.ApplyMovement` right after a restart.** This is harmless: for one frame, Scenic is still sending the previous players.

## How it fits together

- **`Scripts/Multiplayer/GameManager.cs`** starts Photon Fusion:
  - **Laptop mode** (`laptopMode`, used by `zmq_demo_controller`): `GameMode.Single`, fully offline. `LaptopModeSceneManager` adopts the open scene, so it doesn't need to be in Build Settings.
  - **Headset/laptop pair** (`autoIsHost`, used by the VR scenes): the Quest is host and runs the Scenic connection; the laptop joins as a spectating client that records.
- **Scenic bridge** (`Scripts/Scenic/`):
  - `ZMQServer`/`ZMQRequester` exchange JSON with Scenic every tick.
  - `ScenicParser` and `InstantiateScenicObject` spawn the players, ball, goals and lines.
  - `PlayerInterface`, `HumanInterface` and `ActionAPI` apply Scenic's movement and actions (pass, shoot, intercept, …).
  - `JSONStatusMaker` sends Unity's state back to Scenic.
- **Recording** (`Scripts/Program Synthesis/`):
  - `ProgramSynthesisManager` handles pause, record segments and restart.
  - `AnnotationManager` logs clicks and actions.
  - `JSONToLLM` builds the demonstration JSON.
  - `JSONDirectory` picks output folders.
  - `RecorderManager` (in `Scene Managers`) records video in the editor.
  - `Scripts/LLM/ElevenLabs` transcribes the narration.
- **FSM view:** `Scripts/FSM/FSMVisualizer.cs` draws `Resources/_FSM/fsm.json` onto the FSMCanvas.

Output layout:

```
output/
└── participant<N>/<drill>/
    ├── usable_demonstrations.json
    └── demonstration<K>/
        ├── videos/participant<N>_demo<K>_segment<S>.mp4
        └── json_segments/participant<N>_demo<K>_segment<S>.json
```
