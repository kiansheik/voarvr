# Quiet cockpit and calibrated advanced view — 2026-09-10

## Goal
Download current Quest feedback; investigate accumulating magenta bars; silence ongoing text unless HUD on; provide full bird rotation/authored eyes in advanced first-person; soften wrist pitch and honor comfortable calibrated grips.

## Files inspected
Agent wiki/quality loop and Unity MCP skill; latest two Quest recordings/logcat/package/screenshot; Core/FlightCamera; Flight controller/driver/calibration/rig/Acrobatic; UI HUD/cards; gameplay text ownership; authored Blender eye locations; character assets; relevant tests/build tooling.

## Files changed
Core/FlightCamera; Flight/AcrobaticFlight,BirdFlightController,BirdFlightDriver,BirdCharacterDefinition; UI/FlightCard; Gameplay/ExpeditionDirector,SkyForaging; Editor/CharacterEyeSetup; three character resources via EditorAPI; EditMode QuietCockpitTests/AcrobaticFlightTests; PlayMode QuietUiLifecycleTests; docs current-state/log/INPUT and this handoff. Existing dirty work preserved; no commit/push.

## Commands run
Bounded ADB reconnect/offline recovery, pull two current recordings, logcat/package/screencap; telemetry summarize and targeted interval analysis. Unity6000.6.0f1 MCP resources/refresh/Console, explicit eye-anchor asset setup,179Edit/22Playtests, actual-camera quiet/inverted/HUD screenshots.21Python tests; check_repo; git diff --check; XR preloads restored; Quest build. Blender sources read only.

## What worked
Magpiev3 15930/433frames both clean0drops. Long session221.24s,124.63s Acrobatic. Pitch ratep95 89.80°/s, roll65.28°/s; multiple accepted calibrations. Source stamp matches prior buildb5cf462d. New tests exercise±90° grips, recalibration zero, pitch commands and inverted camera basis. Full-input loops still pass all species. HUD off is clear; HUD on readable. Repeated menu/flight sessions do not retain cards; component-only removal destroys its backing.179Edit/22Play/21Python passed. Three independent scoped critics found no high defect; evidence artifacts/reviews/quiet-cockpit/round-01.

## What failed / uncertainty
Initial ADB connection stale offline; recovered for download. Headset screenshot was passthrough and logcat no retained relevant shader failure, so magenta root cause is not confirmed. Runtime shader-by-name backplate and incomplete component-only cleanup were removed; device acceptance still required. Repo checker retains24 existing TMP GUID findings, no missing metadata. Device became offline again during tests.

## Remaining questions
Confirm absence of bars with HUD on and after two character returns on the new APK. Wearer confirmation of calibrated grip and intentionally full-rotation first-person required. Mode/view switches deliberately change camera basis/position; A recovery and stabilized third-person remain available. Prior UI root-cause hypothesis must not be reported as proven. No new physiology inference.

## Suggested next prompt
Play the new build, recalibrate with my preferred grip, try B first-person loops, toggle HUD and return/select twice; then pull logs and verify the magenta bars are gone and pitch neutral feels natural.

## Build/install
APK built successfully38.536s;82,057,947bytes;SHA256 `2cab1e99de6fe1131ef4409452670d11af34c356345c38f405e77eaad1c5ccff`. Source stamp `b8bb65eeae510c0f0d0925a521417d3d89961779e221975e0ec140a8b0bfdc14` verified embedded in APK. MCP returned empty false but Unity PlayerBuildInfo confirms success. Installed via ADB install -r successfully after user woke headset; Launched process PID10335; PID-filtered startup log has no reported exception/error. Headset lost focus and paused the app; captured display was black, so rendered gameplay and magenta elimination are not hardware-verified. User-facing acceptance test: natural-grip calibration, B advanced first-person, HUD toggle, two menu/character returns.
