# Repository map

| Path | Responsibility / entry point |
| --- | --- |
| `unity/Assets/Game/Input/` | IFlightInput, frame/wing values, XR/gamepad/synthetic providers |
| `unity/Assets/Game/Flight/` | Force-integrated BirdFlightController, runtime driver, analytic wing IK and rig presentation |
| `unity/Assets/Game/Core/` | Bootstrap scene transition, desktop/XR FlightCamera |
| `unity/Assets/Game/Debug/` | FlightDiagnostics inspector fields and desktop overlay |
| `unity/Assets/Game/Editor/` | ProjectSetup.Configure/BuildQuest plus explicit duck import/wiring and review helpers |
| `unity/Assets/Game/Tests/` | EditMode simulation/calibration and PlayMode scene/rig/camera assemblies |
| `unity/Assets/Game/World/` | Deterministic moving WindField and generated spirit-city/forest course |
| `unity/Assets/Scenes/` | Bootstrap, Menu/CharacterSelect and Prototypes/BirdFlight authored scenes |
| `unity/Assets/Art/Materials/`, `Art/Models/` | Prototype materials/shaders and curated rigged duck FBX |
| `unity/Assets/{Art,Audio,ThirdParty}/` | Curated runtime assets; most folders initially empty |
| `unity/Packages/`, `ProjectSettings/` | Pinned dependency manifest and initial shared settings |
| `blender/scripts/` | Static/rigged validation/export smoke tests and reproducible duck/dragon generation |
| `blender/source/` | Authored LFS `.blend` sources, including DuckV1 and the broad membrane DragonV1 rig |
| `tools/scripts/` | doctor, check_repo, Unity CLI wrapper and host tests |
| `docs/`, `design/` | Operational architecture and intended gameplay; distinguish intended from real |

Follow GUID-preserving Unity asset workflows. Setup-created settings live under `Assets/Settings/` / `Assets/XR/` after import; they are shared sources, not ignored caches. Never search/edit generated Library blobs to change first-party behavior.
