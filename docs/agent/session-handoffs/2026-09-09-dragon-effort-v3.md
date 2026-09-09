# Dragon effort and recovery v3 — 2026-09-09

## Goal
Respond to worn feedback: Dragon looked better but could not sustain flight despite exhausting effort. Make moderate wingbeats usable while retaining broad-winged momentum/slow turn feel; A restart+calibrate, Meta recenter calibrate in place, return to character selection, explain persistent Quest install.

## Files inspected
Agent index/current-state/repo-map/open-questions; previous v2 handoff; flight profile/force solver/calibration; XR/gamepad/synthetic/body input; driver/camera/menu; tests, character assets, build setup and product ID. Unity MCP instances/editor state and skill; Meta current developer documentation for Library/Unknown Sources. Preserved existing uncommitted work. No commit/push.

## Files changed this iteration
- BirdCharacterDefinition, BirdFlightController, Dragon.asset: stroke coefficient mass normalization (Duck unchanged), optional forward downstroke work, human-speed force cap and usable launch threshold/speed; updated Dragon description.
- BirdFlightDriver, XRFlightInput, FlightInputFrame, GamepadFlightInput, FlightCamera: A resets+captures; Meta origin changes queue automatic stable capture in place; left Menu/Escape/gamepad East returns to selection; discoverable prompts.
- DuckFlightTests: moderate controller-arc flight/takeoff/rest regressions and updated prompt contract. New FlightRecoveryTests + meta: reset/capture, stable origin recapture/state preservation, scene return/time reset.
- README, design INPUT/FLIGHT, architecture, VR setup, agent wiki/log/index/open questions and this handoff.

## Commands and validation
- Unity 6000.6.0f1 via MCP: **53/53 EditMode**, job `9bfe7b9e2fd04e5d9ddf53ea262b76bb`; **10/10 PlayMode**, job `741fa43e497b43439758435556eb1e6d`.
- After review, anchored the0.35s stability window and checked wrist orientations; added90Hz continuous-motion rejection. Narrow final recovery run **2/2 pass**, job `f696b68716684abe8ff69414d123633e`. Physics/art unchanged after full suites.
- Still-air controller probe:±0.18m at0.8Hz through TrackedBodyFrame, natural arm-span shortening, neutral wrists, no triggers. After30s from12m: altitude18.472m, forward219.975m, speed7.210m/s. After3s rest: altitude16.942m, speed4.420m/s. From ground/zero initial velocity after10s: altitude3.164m, forward73.043m, speed7.224m/s.
- Unity rendered current character menu and start/recenter prompts. Independent visual/player/technical critiques/evidence in `artifacts/reviews/dragon-effort-v3/round-01/`; no blocker/high, assessable delta8–9. Medium consecutive-frame stability issue fixed and independently rechecked; worn fatigue/legibility/binding timing remain unverified.
- `git -c filter.lfs.process= -c filter.lfs.required=false diff --check` passes; normal LFS diff cannot write sandboxed `.git/lfs/tmp`. Host/Blender sources unchanged this iteration; no needless repeat of their earlier passed checks.
- Quest adb reconnect and `shell pm path com.voarvr.prototype` confirmed persistent installed package, independent of a connected editor. Official Meta documentation says sideloads appear in Library → Unknown Sources, not main app grid: https://developers.meta.com/horizon/documentation/android-apps/enable-developer-mode/ .

## What worked
The previous Dragon coefficient26.4 divided by10.912kg gave approximately one-third Duck's stroke acceleration for identical hand motion, with no forward reaction for neutral downstrokes. Coefficient now130.944, forward ratio0.8, force capped above1.6m/s hands, launch threshold0.72m/s and forward8.4m/s. It keeps mass10.912kg, wing area6.16m², slower attitude and wider turns. Still-air moderate arcs now sustain flight, unlike v2. No-motion/upstroke generates no active thrust. Duck baseline remains unchanged.

A restarts even if capture is invalid, leaving neutral wings with a retry prompt. Meta is not directly rebound: XRInputSubsystem.trackingOriginUpdated preserves flight state and queues fresh stable recapture. Left Menu clears stale selection and restores scene time before return. All requested controls have regression coverage, with physical input delivery still a headset check.

## What failed / remaining limits
Initial moderate ground launch reached only2.27m after10s; revised forward launch8.4m/s now passes3m target. Quest initially asleep/offline; user woke it and reconnect succeeded. Existing incomplete TMP import still causes24 structural GUID findings. No new art/world changes. Mechanical/ergonomic regressions are deliberately more realistic than the prior no-flap thermal test, but do not establish perceived fatigue for this wearer. Unknown Sources is Meta's sideload library surface; no promise of normal Store-grid/global-search placement.

## Build/device result
Unity BuildQuest succeeded in33.55s (editor log); long MCP call returned an empty failure envelope after completion, so inspected final log and new APK. SHA-256 `289a3618fb8bc11c06b1b55795be2db7bcd4ee38edbfa5c71a89db73aa38a9a2`. `adb install -r` returned Success; explicit GameActivity launch succeeded. Restored only this build’s generated configure fingerprint to its clean turn-start content; project settings have no remaining test churn. Final diff whitespace passes; repository check has only the24 known TMP GUID findings. Physical playtest remains pending.

## Remaining questions
Does v3 now launch/sustain on moderate physical strokes and allow comfortable glide breaks? Verify A, Meta stable capture, left Menu and reselecting Dragon on-device; check sensitivity to non-neutral wrists or partial tracking. Earlier menu unattended auto-selection and actual Quest frame times remain separate unverified issues.

## Suggested next prompt
Play the installed v3 Dragon with slow moderate strokes and glide breaks; report takeoff/sustain effort, A reset+capture, Meta capture without teleport, and left Menu return. Locate the persistent app in Library → Unknown Sources → VoarVR. Capture physical control traces before further physics changes if it still feels wrong.
