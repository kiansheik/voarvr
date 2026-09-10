# Repository map

| Path | Responsibility / entry point |
| --- | --- |
| `unity/Assets/Game/Input/` | IFlightInput, frame/wing values, XR/gamepad/synthetic providers |
| `unity/Assets/Game/Flight/` | Force-integrated controller, IFlightEnvironment/FlightContactSolver, runtime driver, analytic wing IK and rig presentation |
| `unity/Assets/Game/Core/` | Bootstrap scene transition, desktop/XR FlightCamera |
| `unity/Assets/Game/Debug/` | FlightDiagnostics inspector fields and desktop overlay |
| `unity/Assets/Game/Editor/` | ProjectSetup.Configure/BuildQuest plus explicit duck import/wiring and DuckReview/WorldReview evidence helpers |
| `unity/Assets/Game/Tests/` | EditMode simulation/calibration and PlayMode scene/rig/camera assemblies |
| `unity/Assets/Game/World/` | WorldSpace/WorldStreamer/WorldChunk/WorldTerrain; pure AtmosphereModel, WindField/WindVisualizer; UnityFlightEnvironment and LandingGuide/Surface |
| `unity/Assets/Scenes/` | Bootstrap, Menu/CharacterSelect and Prototypes/BirdFlight authored scenes |
| `unity/Assets/Art/Materials/`, `Art/Models/` | Prototype materials/shaders and curated rigged duck FBX |
| `unity/Assets/{Art,Audio,ThirdParty}/` | Curated runtime assets; most folders initially empty |
| `unity/Packages/`, `ProjectSettings/` | Pinned dependency manifest and initial shared settings |
| `blender/scripts/` | Static/rigged validation/export smoke tests and reproducible duck/dragon generation |
| `blender/source/` | Authored LFS `.blend` sources, including DuckV1 and the broad membrane DragonV1 rig |
| `tools/scripts/` | doctor, check_repo, Unity CLI wrapper and host tests |
| `docs/`, `design/` | Operational architecture and intended gameplay; distinguish intended from real |

Follow GUID-preserving Unity asset workflows. Setup-created settings live under `Assets/Settings/` / `Assets/XR/` after import; they are shared sources, not ignored caches. Never search/edit generated Library blobs to change first-party behavior.

Endless-world ownership and budgets: [architecture](../ARCHITECTURE.md). Reproducible evidence lives under ignored `artifacts/reviews/infinite-world-landing-v1/`; WorldReview exposes explicit glide, actual traversal, contact and camera captures. Scene changes use Unity editor serialization.

Flight instruments: `UI/FlightHud.cs` and `World/BirdAirflowTrails.cs`, installed by BirdFlightDriver after essential rig wiring. Explicit `Editor/FlightInstrumentReview.cs` captures HUD/wake/thermal evidence; `AtmosphereLiftReview.cs` reproduces failed and successful assistance candidates. Current evidence: `artifacts/reviews/flight-instruments-v1/`.

## Magpie and local motion evidence

- `Flight/BirdMorphology.cs`, `AvianWingPresentation.cs`: optional avian morphology and coupled feather/manus/alula/tail capability; legacy Duck and membrane Dragon preserve defaults.
- `Telemetry/`: versioned scalar binary schema, bounded writer, runtime capture and simple calibrated replay. Offline `tools/scripts/telemetry.py`; operational contract `docs/development/telemetry.md`.
- `blender/scripts/create_magpie.py`, `magpie_measurements.py`: original63-bone asset and overlap-aware rest-area validation. Research provenance in `docs/research/species/magpie.md`.
- `Editor/MagpieSetup.cs`, `MagpieReview.cs`, `TelemetryBuildStamp.cs`: explicit catalog setup, actual-frame pose evidence, build identity capture. No scene rewrites or runtime MCP dependency.
