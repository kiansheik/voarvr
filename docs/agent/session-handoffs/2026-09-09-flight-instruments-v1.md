# Flight instruments, wing airflow and readable lift — 2026-09-09

## Goal

Respond to wearer request for sparse aircraft-like instruments, speed/body-position visual cues and more useful/intuitive wind tunnels. Preserve Duck still-air feel, calibrated controls and the just-fixed native ground collisions. No commit/push.

## Files inspected

Agent index/current-state/repo-map/open-questions, Unity MCP skill and live editor/project resources; FlightCamera, character-selection UI/font setup, BirdFlightDriver/Controller/RigDriver, AtmosphereModel/WindField/WindVisualizer, material shaders, bootstrap/atmosphere/streaming tests and prior handoff.

## Files changed

New UI/FlightHud, World/BirdAirflowTrails, Editor/FlightInstrumentReview and AtmosphereLiftReview; new instrument EditMode and HUD/trail PlayMode tests. Driver installs optional instruments after essential rig setup; ProceduralFlightWorld exposes serialized airflow material. WindVisualizer classifies actual vertical wind with shared gold/cyan/rose colors. Atmosphere assistance tuning is measured separately from the HUD. Docs updated with final evidence below.

## Behavior

World-space UI follows the camera at2m focal distance, with no interactive ray targets. Upper-left airspeed in knots is magnitude of ground velocity minus sampled air velocity; upper-right altitude is actual loaded-terrain clearance in metres. Climb/sink is world vertical bird velocity. A small heading/bank/pitch indicator describes the simulated body independently of headset look. Lower lift cue separates rising air from actual climb; cyan ambient, gold rising, rose sinking. StillAir and landed states have explicit messages. Numbers update5Hz with0.4s smoothing; attitude updates every frame.

Two20-point pooled wingtip histories originate on actual animated bones, advect with local wind, fade and shift cyan→violet with airspeed. The speed readout shares that scale. Reset/teleport clears histories, origin shift translates them, pause follows simulation time, perched trails hide. Materials clone existing serialized dependencies; no runtime Shader.Find or new imported art.

## Commands run

Targeted rg/reads, git status/diff; Unity MCP resource/state/Console, explicit refresh and scene Play inspection, captures and scene-independent measurements; Python host tests; Unity suites/build/device checks recorded below. No Blender/source changes.

## What worked / failed

Initial screenshot exposed tiny readouts, so panel positions/font sizes were increased while keeping the center free. A naive Assisted9.4m/s peak caused a major Duck stall in a20s core-circle fixture (−138m versus−36.6m still air); rejected as final behavior. Broader useful air must be evaluated with both species and multiple bank/entry positions, not just a peak-wind scalar. Pure candidate matrix added for this reason.

## Final validation and device result

Final112/112EditMode job`2c17eac12be246f9a62db3b411f2caab`;15/15PlayMode job`ad8fbc29728d4c7d9d46defe85973317` after final visual revision;12host tests; clean diff whitespace and Console. Repo checker still fails on24 pre-existing TMP GUIDs. Three independent rounds: enlarged attitude, moved overlapping legend, conditioned brake advice, exposed actual gold lift and shortened visible wake history so fast violet streams remain visible. Final assessable scores8, no high/blocker; worn gates remain.

Final Assisted field retains9.4peak/48radius/lower30–95altitude/street5.2, with bounded automatic aerodynamic feathering through optional IWindAssistance. It subtracts at most45° excess positive incidence above StallAngle−4 only in nonzero Assisted air, bypassing explicit brake/tuck. No added force, no raw visual/input changes. StillAir/Touring/Wild remain unassisted. The actual diagnosis was crosswind-induced deep stall: Duck reached~72°AoA within5s while windY=0. Smooth wind caps and gust compensation failed; automatic feathering succeeded. Core20s no-flap gains Duck48.438/46.988m at8/22m offset; Dragon57.598/53.707m. Same-input still air loses36.605/16.061m. Both-species40s entry, zero-wind bit-identical Duck, head-down, explicit brake/tuck regressions pass.

Memory-counter correction: GetAllocatedBytesForCurrentThread reports0 even for a known1MB allocation. All prior zero-GC claims based on it are invalid, including old world handoff. HUD5Hz string formatting allocates.1200 advancing ticks with current systems take63.2187ms desktop CPU, but synchronous capture does not measure rendered Canvas/GPU cost. Fixed two20-point wake buffers are additionally resampled into two20-point drawing buffers, capped1.6–4m by species; all history points use current bird-local wind (bounded approximation). Shader minimum pulse default.24; wake clone.7 for steady visibility.

APK build succeeded in35.485s. SHA256`5fa34cfaef2cee0492ef6cb2e4fc34e1604bc5951f99ac1a7eda69e2002d71d2`, path`builds/quest/VoarVR.apk`. Exact Android XR preloads restored after tests, build fingerprint restored afterward. Quest TCP connect timed out; wearer wake request sent. Installation/runtime verification currently pending connection; do not claim installed.

## Remaining questions

Wearer must judge HUD legibility, clutter, comfort in both views, speed cues and whether the lift guidance now helps. Desktop measurements are not worn Quest comfort/performance validation.

## Suggested next prompt

“Playtest the flight instruments update: check knots/ground clearance/climb readouts, wingtip trails and riding Assisted thermals with both species; keep Duck still-air flight and solid ground intact.”
