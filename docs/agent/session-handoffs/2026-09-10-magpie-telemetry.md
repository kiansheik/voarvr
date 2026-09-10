# Magpie and telemetry — built, awaiting Quest

## Goal
Complete attachment1417a58a's anatomy-informed Magpie, articulated feathers and local development telemetry/analysis/replay. Preserve Duck/Dragon, controls and preceding dirty comfort/ground/feedback work. No commits/push. Full25-item [validation report](../../development/magpie-telemetry-validation.md).

## Files inspected
Mandatory agent docs, comfort handoff, latest brief, character/flight/input/rig/world sources, Blender generators/validation/export, Unity build/catalog/review helpers, tool discovery/quest scripts, Featherbase/AVONET and specified functional-anatomy literature. Code is authoritative.

## Files changed
New research/species/magpie.md and development telemetry/report docs; Blender create_magpie.py, magpie_measurements.py, authored MagpieV1.blend and curated Magpie.fbx; rig_smoke_test extension; BirdMorphology, AvianWingPresentation, optional character capability/profile integration; BirdRigDriver bounded shoulder controls; FlightInputFrame/XRFlightInput raw snapshot/marker; TelemetrySample/Writer/FlightTelemetry/ReplayInput/Replay; Python telemetry.py/test_telemetry.py; MagpieSetup/MagpieReview/TelemetryBuildStamp; MagpieAndTelemetryTests, TelemetryCalibrationReplayTests, MagpieRigTests and existing ground Play test extension; catalog assets saved through Unity API; .gitignore excludes captures/build stamp. Mandatory current-state/log/repo-map updated. Existing ground/comfort changes preserved.

## Commands run
Unity MCP refresh/Console/execute, MagpieSetup.Configure, actual Play-mode review captures, full EditMode job a82729f8508040e0b7ba2a625c238f8c (135/135), PlayMode b34d496356f94bd19363037aa16e20b6 (17/17). Python unittest discover18/18. Blender5.2.1 generator/validation/Magpie FBX roundtrip, scanline projected-union measurement and actual Blender MCP posed inspection. Python telemetry summarize on real Editor file. check_repo.py (24 existing TMP GUID issues); git diff --check. Restored exact Android XR preloaded subassets through Editor API, then ProjectSetup.BuildQuest. quest.py connect failed to find a device. User wake request pending.

## What worked
Final63-bone model .56m span, .061706m² union area, .2421m central tail, corrected outward surface normals. Final Blender MCP .165608m maximum wing deformation, rest restored; original dirty Duck authoring scene preserved. Tests cover progressive fan/fold, fixed bone lengths/tracking loss, forced-deployed alula passive energy, actual3-character ground rigs, binary roundtrip/ring pressure, asymmetric nonzero calibration replay with exact pre-step XR gate and variable dt, torso-relative wrist analysis. Real Editor capture29,251 frames, clean close, zero drops; partial capture CPU p95 .0071ms, not Quest performance.

Development APK built successfully in101.767s,82,005,021 bytes, SHA256 b068a99f5f5dc1402ff2743c44aa6264afabbdbe855845f78c6b78cdc149fdd7. HEAD fa63762e32b25bbdd4840c4b8e8dfb97188f18f5 / SourceSHA256 785c3c26efdf03f232c3c800ab7854c7b76a5e5b1c7bf93e502b09c87ec2c53e. Unity MCP build call returned an empty false response while Editor.log confirms PlayerBuildInfo success; rely on build log and new hash. Restored only generated .utmp configure_fingerprint.bin from HEAD after build.

## What failed / remaining
Initial proportions/feather marking, overlap, calibration replay gate and wrist analysis issues were corrected through three reviews. Final visual overall7/fold6; no high/blocker but all8 art gate not met. Folded secondaries remain lateral, localized oblique overlap/stippling and rounded coverts are prototype limitations. Do not start an unbounded fourth cosmetic round. Final technical desktop scores8; player embodiment7 requires actual wearer.

Quest currently offline. No new APK installed, no actual wearer recording/pull/analysis or measured headset telemetry overhead. Prior installed APK89582c05… remains baseline. No unsupported velocity features fabricated. No zero-allocation claim; allocation counter unavailable. Biological wingbeat frequency unavailable. Replay does not reconstruct streamed collisions/rebases or dropped frames.

## Remaining questions
Headset/wearer availability. After installation: select Magpie, A comfortable neutral, fly30–60s, include glide/flap/turn/brake, both grips + left-stick click marks, left Menu closes file. Keep device awake for telemetry.py list/pull/summarize; confirm actual raw pose variation, native availability, marker/clean close/drop count/timing, and update report with results. Actual overhead requires profiling beyond capture_cpu_ms.

## Suggested next prompt
Quest is awake: install the tested b068a99f APK with quest.py run, verify startup logs, request a short marked wearer flight, pull/decode/analyze using telemetry.py and finish the hardware sections in this handoff/current-state/report. Preserve all work; do not commit or push.
