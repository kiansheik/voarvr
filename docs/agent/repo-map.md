# Repository map

| Path | Responsibility / entry point |
| --- | --- |
| `unity/Assets/Game/Input/` | IFlightInput, frame/wing values, XR/gamepad/synthetic providers |
| `unity/Assets/Game/Flight/` | Plain BirdFlightController state/Step and MonoBehaviour BirdFlightDriver |
| `unity/Assets/Game/Core/` | Bootstrap scene transition, desktop/XR FlightCamera |
| `unity/Assets/Game/Debug/` | FlightDiagnostics inspector fields and desktop overlay |
| `unity/Assets/Game/Editor/` | ProjectSetup.Configure / BuildQuest, editor-only assembly |
| `unity/Assets/Game/Tests/` | EditMode simulation and PlayMode scene-wiring assemblies |
| `unity/Assets/Game/{Birds,World,AI,Interaction,Fitness}/` | Reserved empty domains; no systems implemented |
| `unity/Assets/Scenes/` | Bootstrap and Prototypes/BirdFlight authored scenes |
| `unity/Assets/Art/Materials/` | Small URP unlit prototype shader, bird/ground/perch materials |
| `unity/Assets/{Art,Audio,ThirdParty}/` | Curated runtime assets; most folders initially empty |
| `unity/Packages/`, `ProjectSettings/` | Pinned dependency manifest and initial shared settings |
| `blender/scripts/` | asset_common, validate_asset, export_asset, smoke_test |
| `blender/source/` | Authored assets, initially empty; LFS .blend |
| `tools/scripts/` | doctor, check_repo, Unity CLI wrapper and host tests |
| `docs/`, `design/` | Operational architecture and intended gameplay; distinguish intended from real |

Follow GUID-preserving Unity asset workflows. Setup-created settings live under `Assets/Settings/` / `Assets/XR/` after import; they are shared sources, not ignored caches. Never search/edit generated Library blobs to change first-party behavior.
