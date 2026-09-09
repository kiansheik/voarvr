# Agent log

## 2026-09-08 - Foundation bootstrap

Inspected an empty repository and confirmed required agent wiki files were absent. Created the Unity/Blender/tooling/design/documentation foundation, preserving the no-commit/no-push boundary. Offline checks passed; engine, DCC and headset validation remain unrun because their tools/hardware were unavailable. See [session handoff](session-handoffs/2026-09-08-foundation.md) for commands and verification limits.

## 2026-09-08 - First editor playtest

Unity 6000.6.0f1 was now actually installed (pin updated to match); `tool_discovery.py` replaced brittle PATH-only lookup in `doctor.py`/`unity.py`. Ran Configure Foundation (desktop-only; Android Build Support module absent, confirmed via `BuildPipeline.IsBuildTargetSupported`), then PlayMode tests (pass) and a manual Play session through Unity MCP: no console errors, synthetic input drives the bird forward (confirmed by world-Z movement across screenshots) with ground/perches rendering and the `FlightDiagnostics` HUD reporting live state. Placeholder-primitive graphics only. See [session handoff](session-handoffs/2026-09-08-first-playtest.md).

## 2026-09-09 - Duck avatar, articulated wings and force flight

Built and imported a reproducible low-poly mallard with a nine-bone skinned wing rig, controller calibration/IK, first-person eye anchor and measured flight references. Replaced kinematic travel with bounded force integration for gravity, lift/drag, oriented strokes, bank, tuck, flare, stall, perch and ground takeoff. Expanded deterministic and rendered-mesh regression coverage. Three independent visual/player/technical rounds closed all blocker/high desktop findings. Unity, Blender and repository checks pass; the development Android APK builds, while Quest install/on-device validation awaits a connected headset. Tracked `.utmp` cleanup awaits explicit deletion approval. See [session handoff](session-handoffs/2026-09-09-duck-flight-v1.md).

## 2026-09-09 - Quest 3 runtime recovery

Diagnosed a "latest commit but not working" report against a connected Quest 3 (rather than guessing): real adb/logcat/screencap evidence showed GameActivity (the suspected regression vs the prior plain-Activity baseline) actually launches and renders a correct stereo swapchain, falsifying the headline hypothesis. Found and fixed a real, independently-justified gap - Quest 3's Touch Plus controller profile was never enabled in OpenXR, only the older Oculus Touch profile - via `ProjectSetup.ConfigureXR()`/`BuildQuest()`, and added editor-time pre-build validation (`ValidateCharacterCatalog()`) so a successful build can no longer ship a broken character/rig/scene reference silently. A stale pre-session APK's `RectOffset` IL2CPP-stripping crash did not reproduce from a fresh HEAD build. One issue was surfaced but not root-caused: CharacterSelect reliably auto-advances to BirdFlight ~8-13s after building, with no physical input, on every unattended run - flagged for a hands-on follow-up rather than guessed at. Final APK (SHA-256 `e7117a7d85...`) installs, launches, stays alive, and reaches BirdFlight cleanly on real Quest 3 hardware; no commit was made. See [session handoff](session-handoffs/2026-09-09-quest-recovery.md).

## 2026-09-09 - Dragon flight v2 and moving procedural world

Reconciled the interrupted branch and preserved Quest recovery work. Dragon now has ~7m continuously skinned wings and species lift; explicit primary calibration stays in place, platform recenter returns to spawn, and spread-controller torso turning preserves free head look. Moving thermals and wind traces accompany a shaded procedural city/forest practice course. Final Unity tests pass 51 EditMode/8 PlayMode; development APK builds and installs on connected Quest 3. Three critique rounds leave no blocker/high for user testing, with honest remaining art richness, forward wing visibility, high-altitude cues and unmeasured worn comfort/performance limits. See [handoff](session-handoffs/2026-09-09-dragon-flight-v2.md).

## 2026-09-09 - Dragon effort v3 after worn feedback

User confirmed better art but exhausting/unusable Dragon flight. Corrected human-effort scaling and missing neutral-stroke forward thrust; added force cap and moderate takeoff. Still-air controller arcs now sustain30s, allow3s rest and take off from ground. A now restarts+captures, platform recenter captures in place after stable tracking, left Menu returns to character selection.53 EditMode/10 PlayMode pass; review stability-window correction additionally passes2 recovery tests. Quest package-manager confirms persistent install; documented Library/Unknown Sources discovery. See [handoff](session-handoffs/2026-09-09-dragon-effort-v3.md).
