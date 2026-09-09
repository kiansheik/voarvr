# First editor playtest - 2026-09-08

## Goal

Pick up the foundation bootstrap now that Unity/MCP sandboxing is stable, and actually get the prototype running so the user can play it (desktop Play mode, no headset) for the first time.

## Files inspected

`AGENTS.md`, `docs/agent/current-state.md`, `docs/agent/log.md`, `docs/agent/session-handoffs/2026-09-08-foundation.md`, `docs/agent/open-questions.md`, diffs of `tools/scripts/doctor.py`/`unity.py`/`tool_discovery.py` (new discovery helper), `unity/Assets/Game/Editor/ProjectSetup.cs` and `BootstrapSmokeTests.cs`, live Unity Editor state/console/scene hierarchy/build settings via Unity MCP.

## Files changed

`docs/agent/current-state.md`, `docs/agent/log.md`, `docs/agent/open-questions.md` (this handoff). No engine/source files changed — this session only validated work already on disk.

## Commands run

- `git status --porcelain`, `git diff --stat`, targeted `git diff` on changed scripts/settings.
- Unity MCP: `editor/state` and `instances` resources; `read_console` (errors/warnings, then filtered); `run_tests` (PlayMode) + `get_test_job`; `manage_editor` play/stop; `manage_scene get_hierarchy` / `get_build_settings`; `manage_camera` screenshot (default follow-cam, then a positioned wide shot); `execute_code` to check `BuildPipeline.IsBuildTargetSupported(Android)` and the Android XR loader config directly.

## What worked

Unity 6000.6.0f1 is installed and connected via MCP (`unity@a229b2354a2d8f06`); `BirdFlight.unity` is the active scene with Bird (`BirdFlightDriver`, `FlightDiagnostics`), Ground, three perches and Main Camera (`FlightCamera`) all wired. No compile errors; only a harmless Unity Account API timeout and an MCP "Claude CLI not found" auto-config notice. `BootstrapSmokeTests.BootstrapLoadsConnectedPrototype` passes in PlayMode (also asserts every renderer has a supported shader/material, added since the foundation session). Manually entered Play mode: no runtime errors, the `FlightDiagnostics` HUD showed `Synthetic / Glide`, 3 m/s, and the bird's world Z moved from ~0 to ~129 over a few seconds of Play — genuine simulation, not a frozen frame. A positioned screenshot confirmed the ground plane and three perch blocks render (placeholder-primitive art, as documented). Stopped Play mode cleanly afterward.

## What failed / was unavailable

Android Build Support module is not installed in this Unity editor (`BuildPipeline.IsBuildTargetSupported(Android) == False`, confirmed live), so `ConfigureXR` in `ProjectSetup.cs` takes its early-return desktop-only branch — Quest/Android builds remain untested. The existing `Assets/XR/**` config assets (loader, OpenXR settings) predate or were created outside this constraint and still report an assigned Android loader in the config object, but that can't be exercised without the module. Blender was not touched this session.

## Remaining questions

Install Android Build Support (SDK/NDK/OpenJDK) via Unity Hub to unblock Quest builds; install/verify Blender 4.5 LTS and run its smoke test; decide whether the placeholder Play-mode camera/framing is acceptable before real playtesting begins. See [open questions](../open-questions.md).

## Suggested next prompt

"Android Build Support is now installed in Unity Hub. Re-run Configure Foundation, confirm Android XR configures cleanly, then build and sideload a Quest development APK; report the desktop-vs-hardware validation gap explicitly."
