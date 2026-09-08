# Current state

Updated 2026-09-08. Foundation / prototype stage; initial repository was empty with no commits.

Implemented: two authored Unity scenes and stable metadata; primitive bird, ground and three perches; shared FlightInputFrame/IFlightInput; XR/gamepad/synthetic providers; pure kinematic BirdFlightController; runtime driver/camera/diagnostics; deterministic EditMode and scene-wiring PlayMode tests; explicit URP/OpenXR/Android editor setup and development build methods; Blender static-mesh validation/export/roundtrip scripts; offline tools, Git ignores/LFS patterns and development/design/agent docs.

Pinned baseline: Unity 6000.3.21f1, URP 17.3.0, Input System 1.20.0, XR Management 4.5.3, OpenXR 1.16.1, Test Framework 1.6.0. Blender authoring recommendation: 4.5 LTS. Core package lines were checked against Unity's versioned manual; non-core package metadata/API sources were checked against Unity's public registry. Compatibility is not claimed as editor-tested.

Passed: offline structural checks, host Python tooling tests, Unity YAML syntax/reference inspection and whitespace review. Unity/Blender/Git LFS were not installed in checked locations. C# compilation, actual package resolution, scene/shader import, EditMode/PlayMode execution, FBX roundtrip, Android build and Quest validation remain **unrun**. No resolved lockfile or editor-created URP/XR settings are fabricated; first Configure Foundation creates those settings. Review/preserve the real lockfile, ProjectSettings and asset metadata after import.

No aerodynamics, collision/perching, stamina, food, AI, finished gamepad UX, custom Meta SDK features or Switch tooling. Existing input and camera mappings are provisional. Desktop diagnostics are disabled in XR. No third-party art, downloaded binaries, commits or pushes.

Next: install/locate the pinned editor and Blender, configure/import, run actual Unity and Blender tests, then authorize Quest USB and make the first development build. Resolve any real editor errors before starting M1. See [handoff](session-handoffs/2026-09-08-foundation.md).
