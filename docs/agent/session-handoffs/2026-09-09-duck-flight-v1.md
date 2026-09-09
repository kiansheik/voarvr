# Duck flight v1 - 2026-09-09

## Goal

Deliver the first recognizable low-poly duck avatar with articulated controller-driven wings, explicit XR calibration, force-based flight behavior, a more legible scene, deterministic validation, an Android development build and a three-round independent player-facing quality loop. Preserve source assets and avoid commits/pushes.

## Files inspected

Agent wiki/operating manual; flight/input/camera/diagnostics/editor/test assemblies; BirdFlight scene and live Unity hierarchy/console/tests; OpenXR and Android player settings/package validation; Blender asset validation/export scripts and Duck sources; generated FBX/import metadata; quality-loop evidence and three independent critic reports; git status/diffs/ignore behavior.

## Files changed

- Rigged art/pipeline: `blender/scripts/{asset_common,export_asset,create_duck,rig_smoke_test}.py`, `blender/source/DuckV1.blend`, `unity/Assets/Art/Models/Duck.fbx`, duck material/shader and metadata.
- Runtime: `FlightInputFrame`, `XRFlightInput`, `SyntheticFlightInput`, `BirdFlightController`, `BirdFlightDriver`, `WingRigSolver`, `BirdRigDriver`, `FlightCamera`, `FlightDiagnostics`.
- Editor/scene/settings: `DuckSetup`, `DuckReview`, `ProjectSetup`, `BirdFlight.unity`, Android/OpenXR settings, `.gitignore`.
- Tests: `DuckFlightTests`, updated `FlightControllerTests`, `DuckRigTests` and metadata.
- Docs: README, architecture, development, Blender pipeline, flight/input design, agent state/map/questions/log/index and this handoff.

The original `blender/source/Duck.blend` was preserved after automated review rejected overwriting an authored source; the corrected reproducible source is `DuckV1.blend`. No generated Unity cache/vendor files were intentionally edited.

## Commands run

- Targeted `rg`, `sed`, `git diff/status/check/check-ignore`; `python3 tools/scripts/check_repo.py`; host unittest discovery; `doctor.py`.
- Blender 5.2.1: static smoke test; DuckV1 rig smoke test; duck generation/validation/export earlier in the session.
- Unity MCP: live scene/component/settings inspection, explicit duck setup, Play-mode pose captures, BakeMesh evidence, console reads, 33-test EditMode run, 5-test PlayMode run, Configure Foundation and clean Android development builds.
- Unity Android SDK adb: `devices -l`.

## What worked

- Recognizable stylized mallard: 4,450 imported vertices, 6,000 triangles, 4 skinned renderers, 9 bones, one opaque vertex-color material and no textures.
- Both wings follow full XYZ controller position and bounded twist through non-stretching analytic IK. Local smoothing remains stable while the bird translates/turns. Glide, downstroke and tuck visibly deform the baked mesh; tuck span is 47% of glide in captured evidence.
- Calibration accepts only deliberate tracked spread, scales all arm axes to rig span and leaves head motion at physical scale. Uncalibrated XR wings remain neutral; tracking loss/reacquisition cannot inject a flap.
- Persistent force integration produces glide energy loss, banked turns, faster tuck descent, flare braking without hover, stall/fall/recovery and inertial takeoff. Stroke direction remains invariant to captured room heading.
- 33/33 Unity EditMode and 5/5 PlayMode tests pass. The MCP EditMode job missed its callback and timed out after Unity had already written a fresh 33/33 passing TestResults.xml; the XML is the result evidence. Structural checker passes 297 files; 12 Python tests pass. Blender static and rig smoke tests pass outside the filesystem sandbox.
- Three-round quality loop stopped at its cap with no blocker/high final finding. Final assessable scores are 8-9/10; subjective duck proportions/wing silhouette remain 7/10.
- Final clean development APK build succeeds in 154.6 s with zero build errors/warnings. The artifact is 48,510,652 bytes with SHA-256 `e6966232338431e716429c8f793cc7192c1195c408350ccc9dee823a53f29d4f`. OpenXR's optional latency recommendation was applied to source/configuration before this rebuild.

## What failed / was unavailable

- First sandboxed Blender invocations crashed during Blender/USD startup; normal macOS execution passed. One initial rig invocation omitted the required `DuckV1.blend`; the corrected documented command passed.
- MCP briefly lost its Unity websocket during script reload; pinning the live instance restored control.
- `adb devices -l` shows no device. APK install/launch and Quest hardware evidence could not be collected.
- Automated approval review rejected deleting 45 tracked generated files in `unity/.utmp/`. They are ignored for future untracked output and restored to their pre-build tracked contents, but remain in the repository pending explicit user approval.

## Remaining questions

- On Quest: validate stereo prompt readability, physical wing alignment, uncalibrated startup glide, valid/invalid recalibration, different room headings, tracking loss/recovery, sustained flap comfort and device frame/thermal behavior.
- Decide future art direction for the intentionally toy-like proportions and segmented flight-surface silhouette after motion is judged in headset.
- Decide whether general terrain collision, wind/gusts, accessibility effort modes or biome/content work is the next milestone.
- Explicitly approve removal of the tracked `unity/.utmp/` cache files if desired.

## Suggested next prompt

"The Quest is connected and authorized. Install and launch the existing development APK, validate calibration/recenter/tracking-loss and a short flight on-device, capture logs/performance evidence, then explicitly remove the tracked `unity/.utmp/` cache files."
