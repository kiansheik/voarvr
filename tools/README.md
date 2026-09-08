# Repository tools

Run from the repository root with Python 3.10+. These scripts require only the Python standard library; Unity/Blender supply their own runtime dependencies.

| Command | Purpose |
| --- | --- |
| `python3 tools/scripts/doctor.py` | Read-only executable discovery and Git status; prints missing tools, does not install them. |
| `python3 tools/scripts/check_repo.py` | Offline Python/JSON, metadata, source-reference, documentation-link and ignore-rule checks; not C# compilation. |
| `python3 -m unittest discover -s tools/scripts -p 'test_*.py'` | Fail-closed Unity result parser tests. |
| `python3 tools/scripts/unity.py configure` | Run explicit foundation settings initialization. |
| `python3 tools/scripts/unity.py test-edit` | Deterministic simulation tests. |
| `python3 tools/scripts/unity.py test-play` | Bootstrap/prototype connection smoke test. |
| `python3 tools/scripts/unity.py build-quest` | Development ARM64 Android APK in `builds/quest/VoarVR.apk`. |

Set `UNITY_EDITOR` to the actual executable inside the pinned editor's Unity.app. Close this project's editor before batch commands. Logs/results get a unique directory in `artifacts/unity/`, so stale results cannot pass a fresh test run. Test commands intentionally omit Unity's `-quit` flag; the runner quits after results are written.

Set `BLENDER` to the actual Blender executable and use [bpy commands](../docs/BLENDER_PIPELINE.md). `ADB` is a convenience override for doctor and documented device commands. No script installs tools, accepts licenses, trusts devices, commits or pushes. [README](../README.md) includes the full Mac setup.
