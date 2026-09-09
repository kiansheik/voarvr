# Development loops

Start with [README setup](../README.md) and [agent current state](agent/current-state.md). Run commands from the repository root. Keep a single Unity instance on this project; close it before batch commands.

## Code-only

Inspect the related implementation and tests, make a narrow change, run `python3 tools/scripts/check_repo.py`, then `python3 tools/scripts/unity.py test-edit` for flight/input logic. Pure simulation accepts supplied frames and explicit delta time; avoid reading real hardware or Time in tests. The Python checker checks syntax/structure, not C# compilation. Host tooling tests use `python3 -m unittest discover -s tools/scripts -p 'test_*.py'`.

## Unity editor

Open the pinned `unity/` project in Hub. After the first import run VoarVR > Configure Foundation and restart if Input System requests it. Inspect Console, open Bootstrap and play. Select Bird to set input mode before Play; in Synthetic mode, adjust Gesture during Play. For duck scene reconstruction use VoarVR > Configure Duck Prototype explicitly. Run EditMode and PlayMode tests through Window > General > Test Runner. For scene changes check camera framing, scale, skinned deformation and material rendering in Game View and save outside Play mode.

Batch equivalents:

```sh
python3 tools/scripts/unity.py configure
python3 tools/scripts/unity.py test-edit
python3 tools/scripts/unity.py test-play
```

Configure creates missing pipeline/XR assets and reasserts foundation player settings/build scenes. It is an explicit initialization command, not an automatic migration service. After tuning settings later, inspect the command before rerunning it. It preserves scenes and existing URP asset tuning. Unity-generated `Packages/packages-lock.json`, project settings and settings `.asset`/`.meta` files should be reviewed and retained as shared source in a future authorized commit. No lockfile is supplied until a real editor resolution succeeds. Empty Unity folders are represented by folder `.meta`; Unity recreates them on a fresh clone.

## Quest hardware

Follow [VR setup](VR_SETUP.md), switch Android, run OpenXR Project Validation, then build a development APK using the editor or `python3 tools/scripts/unity.py build-quest`. The CLI adds `-buildTarget Android` and fails if foundation XR/URP settings are absent. It does not authorize devices, install the APK or modify a connected headset. Install with adb or use Build And Run. Check head/controller poses, reset/pause, visual stereo correctness and profiler/frame timing separately from tests. The prototype doesn't implement collision or boundaries in its virtual world.

## Blender assets

Save source in `blender/source/`, validate and export through the scripts. Inspect the output and promote only approved runtime FBX into `unity/Assets/Art/Models/`. In Unity confirm meter scale, pivot, forward axis, bone/skin bindings, deformation, materials and import settings. Run the static and rig smoke tests after pipeline changes. See [Blender pipeline](BLENDER_PIPELINE.md).

## Agent-assisted work

MCP is optional editor access. Discover the connected project, inspect before editing, use repository commands for repeatable changes and record test/Console evidence. If a capability is absent, use menu/CLI/bpy workflows. Update current-state/log/handoff after significant work. Do not commit/push without explicit instruction.

## Troubleshooting

- Package resolution: inspect Package Manager and `artifacts/unity/.../editor.log`; confirm the exact editor and network access. URP/Test Framework are editor-core packages, so the generic registry listing is not authoritative for those versions. Do not invent a resolved lockfile.
- Pink/missing geometry: confirm Configure Foundation completed, URP is assigned in Graphics and all Quality levels, and PrototypeUnlit shader compiled.
- Empty test result: the wrapper fails closed. Check license, compiler and project-lock errors in that run's fresh log.
- Input appears inactive: configure Input System, restart Unity, connect the gamepad before entering Play or choose Synthetic. Physical XR is tested in Android builds on Mac.
- Android build error: check Hub's exact editor modules and External Tools before installing unrelated SDK versions.
