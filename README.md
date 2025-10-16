# TacticalMR

Multiplayer soccer simulation system built in Unity that integrates with Scenic for automated scenario generation and demonstration recording. The system supports VR headsets, desktop observers, and AI-controlled players in a networked environment.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Core Systems](#core-systems)
- [Player Systems](#player-systems)
- [Game Objects](#game-objects)
- [Data Flow](#data-flow)
- [Network Architecture](#network-architecture)
- [File Structure](#file-structure)
- [Getting Started](#getting-started)

## Architecture Overview

```mermaid
graph TD
    A[GameManager] --> B[Photon Fusion Network]
    A --> C[Scenic Integration]
    
    C --> D[ZMQServer]
    C --> E[ScenicParser]
    C --> F[ObjectsList]
    
    B --> G[Human Players]
    B --> H[AI Players]
    B --> I[Game Objects]
    
    G --> J[HumanInterface]
    G --> K[VR Input]
    G --> L[Desktop Input]
    
    H --> M[PlayerInterface]
    H --> N[ActionAPI]
    
    I --> O[SoccerBall]
    I --> P[Goals/Field]
    
    Q[Recording System] --> R[Video Recording]
    Q --> S[Audio Transcription]
    Q --> T[JSON Export]
    
    U[Annotation System] --> V[User Interactions]
    U --> W[AI Analysis]
```

## Core Systems

### 1. Game Management (`GameManager.cs`)
Central coordinator that handles:
- **Network Mode Detection**: Automatically determines host/client roles
- **Platform Support**: VR headset (host) + laptop observer (client) + laptop-only mode
- **Photon Fusion Integration**: Multiplayer networking setup
- **Component Initialization**: Scenic communication and camera management

```mermaid
graph LR
    A[GameManager] --> B[Host Mode<br/>VR Headset]
    A --> C[Client Mode<br/>Laptop Observer]
    A --> D[Laptop Mode<br/>Single Player]
    
    B --> E[Scenic Enabled<br/>Observer Camera OFF]
    C --> F[Scenic Disabled<br/>Observer Camera ON]
    D --> G[Scenic Enabled<br/>Observer Camera ON]
```

### 2. Scenic Integration
Connects Unity with external AI simulation system:

#### ZMQ Communication (`ZMQServer.cs`, `ZMQRequester.cs`)
- **Bidirectional Communication**: Unity ↔ Scenic AI system
- **JSON Protocol**: Structured data exchange
- **State Synchronization**: Real-time game state sharing

#### Scenic Parser (`ScenicParser.cs`)
- **Object Creation**: Spawns players, balls, goals from Scenic commands
- **Action Translation**: Converts Scenic actions to Unity method calls
- **Coordinate System**: Transforms between Scenic and Unity coordinate systems

### 3. Timeline & Recording (`TimelineManager.cs`, `ProgramSynthesisManager.cs`)
- **Segment Recording**: Start/stop demonstration capture
- **Pause Control**: Timeline manipulation for annotations
- **Video Integration**: Automated video recording during segments
- **Rewind System**: TODO: Future implementation for playback

## Player Systems

### Human Players (`HumanInterface.cs`)

```mermaid
graph TD
    A[HumanInterface] --> B[VR Mode]
    A --> C[Desktop Mode]
    
    B --> D[Head Tracking]
    B --> E[Hand Controllers]
    B --> F[Room Scale Movement]
    
    C --> G[Mouse/Keyboard]
    C --> H[Gamepad Support]
    
    A --> I[Ball Possession]
    A --> J[Soccer Actions]
    A --> K[Annotation Creation]
    
    J --> L[Pass to Player]
    J --> M[Through Pass]
    J --> N[Shoot Goal]
    J --> O[Intercept Ball]
```

**Key Features:**
- **Cross-Platform Input**: VR controllers, keyboard/mouse, gamepad
- **Ball Physics**: Possession detection, passing mechanics
- **Action Logging**: Automatic annotation of player actions
- **Network Synchronization**: State sharing across clients

### AI Players (`PlayerInterface.cs`)

```mermaid
graph TD
    A[PlayerInterface] --> B[Scenic Commands]
    A --> C[ActionAPI Execution]
    A --> D[Team Allegiance]
    
    B --> E[Movement Data]
    B --> F[Behavior State]
    B --> G[Action Functions]
    
    C --> H[Soccer Actions]
    C --> I[Movement Control]
    C --> J[Animations]
    
    D --> K[Defense Team<br/>Blue Shirts]
    D --> L[Offense Team<br/>Red Shirts]
```

**Capabilities:**
- **Scenic Control**: Receives movement and action commands from AI
- **Pathfinding**: AI navigation using A* pathfinding
- **Team Coordination**: Automatic passing and positioning
- **Behavior Display**: Visual feedback of current AI state

### Action System (`ActionAPI.cs`)

Central hub for all player actions:

```mermaid
graph LR
    A[ActionAPI] --> B[Movement Actions]
    A --> C[Ball Actions]
    A --> D[Goalkeeper Actions]
    A --> E[Factory Actions]
    
    B --> F[MoveToPos<br/>DribbleToPos<br/>LookAt]
    
    C --> G[GroundPass<br/>AirPass<br/>Shoot<br/>Chip]
    
    D --> H[CatchBall<br/>DropKick<br/>OverhandThrow]
    
    E --> I[PickUp<br/>PutDown<br/>Packaging]
```

## Game Objects

### Soccer Ball (`SoccerBall.cs`)
Advanced physics system with intelligent movement:

```mermaid
graph TD
    A[SoccerBall] --> B[Destination Seeking]
    A --> C[Ground Management]
    A --> D[Possession System]
    
    B --> E[Guidance Forces]
    B --> F[Overshoot Prevention]
    B --> G[Smooth Stopping]
    
    C --> H[Height Maintenance]
    C --> I[Bounce Prevention]
    C --> J[Rolling Physics]
    
    D --> K[Owner Detection]
    D --> L[Automatic Clearing]
    D --> M[Collision Handling]
```

### Visual Systems
- **Arrow Generator** (`ArrowGenerator.cs`): Procedural 3D arrows for direction indication
- **Ground Selection** (`GroundSelection.cs`): Interactive position marking
- **FSM Visualizer** (`FSMVisualizer.cs`): State machine diagram rendering

## Data Flow

### Recording Pipeline

```mermaid
sequenceDiagram
    participant User
    participant Timeline
    participant Recording
    participant Audio
    participant Video
    participant JSON
    participant Export
    
    User->>Timeline: Start Segment
    Timeline->>Recording: Begin Data Capture
    Timeline->>Audio: Start Audio Recording
    Timeline->>Video: Start Video Recording
    
    User->>Timeline: Create Annotations
    Recording->>JSON: Log Interactions
    
    User->>Timeline: Stop Segment
    Timeline->>Audio: Stop & Transcribe
    Timeline->>Video: Stop & Process
    Audio->>JSON: Merge Transcription
    JSON->>Export: Generate Final JSON
```

### Annotation System (`AnnotationManager.cs`)

```mermaid
graph TD
    A[User Interactions] --> B[Annotation Creation]
    B --> C[Object References]
    B --> D[Position Markers]
    B --> E[Action Events]
    
    C --> F[Player Clicks]
    C --> G[Ball Interactions]
    
    D --> H[Ground Positions]
    D --> I[Spatial References]
    
    E --> J[Pass Events]
    E --> K[Intercept Events]
    E --> L[Goal Attempts]
    
    F --> M[Network Sync]
    G --> M
    H --> M
    I --> M
    J --> M
    K --> M
    L --> M
    
    M --> N[JSON Export]
    N --> O[AI Analysis]
```

## Network Architecture

### Multiplayer Synchronization

```mermaid
graph TD
    A[Host - VR Player] --> B[Photon Fusion]
    C[Client - Observer] --> B
    
    B --> D[Player States]
    B --> E[Ball Physics]
    B --> F[Game Events]
    
    A --> G[Scenic Commands]
    G --> H[AI Player Control]
    H --> I[Action Execution]
    I --> B
    
    A --> J[Annotation Sync]
    J --> K[RPC Distribution]
    K --> C
    
    A --> L[Video Recording]
    A --> M[Audio Processing]
    L --> N[File Export]
    M --> N
```

### Data Synchronization (`JSONToLLM.cs`)

```mermaid
graph LR
    A[Server] --> B[Token Dictionary]
    A --> C[Annotation Data]
    
    B --> D[Chunked Transfer]
    C --> E[Chunked Transfer]
    
    D --> F[Client Reconstruction]
    E --> F
    
    F --> G[JSON Generation]
    G --> H[File Export]
```

## File Structure

### Core Script Organization

```
Scripts/
├── Environment/
│   ├── Rewindable.cs               # Pause/resume functionality
│   ├── Fade.cs                     # VR transition effects
│   └── ScenarioManager.cs          # Scenario type management
│
├── FSM/
│   └── FSMVisualizer.cs            # State machine diagrams
│
├── Human/
│   ├── HumanInterface.cs           # Human player controller
│   ├── ControllerInput.cs          # Gamepad support
│   ├── KeyboardInput.cs            # Desktop input handling
│   └── ExitScenario.cs             # VR interaction controls
│
├── Input/
│   └── (Input-related scripts)     # Additional input handling
│
├── LLM/
│   ├── JSONToLLM.cs                # Data export to JSON
│   ├── JSONDirectory.cs            # File organization
│   └── JSONStatusMaker.cs          # Game state serialization
│
├── Multiplayer/
│   ├── GameManager.cs              # Main game coordinator
│   ├── PlayerInterface.cs          # AI player controller
│   ├── HumanInterface.cs           # Human multiplayer interface
│   ├── BallInterface.cs            # Ball networking
│   ├── GoalInterface.cs            # Goal networking
│   ├── LineInterface.cs            # Field marking networking
│   ├── ObjectsList.cs              # Scene object management
│   └── BallOwnership.cs            # Ball possession tracking
│
├── Program Synthesis/
│   ├── ProgramSynthesisManager.cs  # Recording coordination
│   ├── AnnotationManager.cs        # User interaction tracking
│   └── RecorderManager.cs          # Video recording
│
├── Scene Management/
│   ├── TimelineManager.cs          # Timeline and pause control
│   └── (Scene management scripts)
│
├── Scenic/
│   ├── ZMQServer.cs                # Scenic communication server
│   ├── ZMQRequester.cs             # ZMQ network handler
│   ├── RunAbleThread.cs            # Background thread base
│   ├── ScenicParser.cs             # JSON command parsing
│   ├── ScenicMovementData.cs       # Movement data structures
│   ├── InstantiateScenicObject.cs  # Object spawning
│   ├── IObjectInterface.cs         # Scenic control interface
│   └── ActionAPI.cs                # Player action execution
│
└── UI/
    ├── GroundSelection.cs          # Position marking
    ├── GroundDeselection.cs        # Position clearing
    ├── ArrowGenerator.cs           # 3D arrow generation
    └── SoccerBall.cs               # Advanced ball physics
```

### Scene Hierarchy Structure

```
zmq_demo_controller_main/
├── Managers/
│   ├── ZMQManager                  # Scenic communication
│   ├── VideoRecorderManager        # Video recording system
│   ├── ScenarioManager             # Scenario control
│   ├── MultiplayerManager          # Network coordination
│   ├── TimelineManager             # Timeline control
│   ├── Program Synthesis Manager   # Recording workflow
│   └── AudioRecorder               # Audio capture
│
├── Camera                          # Main camera system
├── Real Canvas                     # Main UI system
├── Field Components/
│   ├── extended ground             # Ground interaction
│   └── field                       # Soccer field
│
├── Interfaces/
│   ├── GPT Interface               # AI communication
│   ├── ScenicSynth                 # Scenic synthesis (UNUSED)
│   └── SynthConnect                # Connection management (UNUSED)
│
├── Input Management/
│   ├── keyboard                    # Keyboard input
│   └── EventSystem                 # Unity event system
│
├── UI Systems/
│   ├── Save Demonstration Canvas   # Demo saving UI
│   ├── GroundHighlight            # Position markers
│   ├── Buttons Canvas             # Control buttons
│   └── FSMSystem/
│       ├── FSMCanvas              # State machine UI
│       └── FSMVisualizer          # FSM diagram display
│
└── Utilities/
    ├── Axis Labels                # Debug visualization
    └── Grid                       # Scene grid
```

### Output Organization (`JSONDirectory.cs`)

```
output/
├── participant1/
│   └── Test/
│       ├── demonstration0/
│       │   ├── videos/
│       │   │   └── participant1_demo0_segment0.mp4
│       │   └── json_segments/
│       │       └── participant1_demo0_segment0.json
│       └── usable_demonstrations.json
└── system_recordings/
    └── transcript0/
        ├── demonstration0/
        └── usable_demonstrations.json
```

### JSON Data Format

```json
{
  "scene": {
    "id": "drill_name",
    "language": "transcribed_explanation",
    "step": 0.02,
    "objects": [
      {
        "id": "player_name",
        "type": "Teammate|Opponent|Coach",
        "position": [{"x": 0, "y": 0}],
        "velocity": [{"x": 0, "y": 0}],
        "ballPossession": [true, false],
        "behavior": "current_behavior"
      }
    ],
    "annotations": [
      {
        "id": "0",
        "type": "Pass",
        "from": "player1",
        "to": "player2"
      }
    ],
    "tokens": {
      "1.5": ["The", "player", "[0]", "passes"],
      "3.2": ["to", "teammate", "[1]"]
    }
  }
}
```

## Getting Started

### Prerequisites
- Unity 6.0.x LTS or later (Currently using 6.0.33f1)
- Scenic 3

### Setup
- Complete setup and installation instructions here: https://docs.google.com/document/d/1d0ErVx8w58e4359or7g-fBZrm-HRHs2ZPT3zxF9jCNg/edit?usp=sharing

For more documentation, click here (WIP): https://docs.google.com/document/d/1qMuEYCB1tztLdYwF7BSN8K2Y1f2QmMxFVMbeIjhTTsg/edit?tab=t.0

#### The main scene to run for desktop is `zmq_demo_controller_main.unity`. For VR it is `zmq_demo_vr.unity`. Enable/Disable the FSMSystem gameobject to hide/unhide the FSM diagram.

### Usage Modes

#### VR Host + Desktop Observer
```
VR Player (Host): Wears headset, controls game, interacts with AI
Desktop Observer (Client): Watches remotely, no direct control, records video/scene information
```

#### Laptop-Only Mode
```
Single Player: Desktop controls, observer camera, Scenic integration, supports both kbm & controller
```

### Key Controls

#### KBM (Note not all functionality is supported on KBM)
- **WASD**: Movement
- **P**: Pause/Unpause
- **E**: Restart scenario
- **B**: Start/Stop recording segment
- **Mouse**: Interact with objects and ground

#### Game Controller Controls
- **Left Joystick**: Movement
- **A Button**: Pause/Unpause
- **Y Button**: Restart scenario  
- **X Button**: Start/Stop recording
- **Left Trigger**: Intercept Ball
- **Right Trigger**: Pass
- **Left Shoulder**: Trigger Pass (Calling for teammate with ball to pass)
- **Right Shoulder**: Shoot to Goal

#### VR Controls
![alt text](questbuttons.png)

### Development Notes

- **Coordinate Systems**: Scenic uses (x,y,z) where z=up, Unity uses (x,y,z) where y=up
- **Network Authority**: Host controls Scenic and game state, clients observe
- **File Naming**: Can rename recording saves in ZMQManager gameobject, JSON Directory component.
