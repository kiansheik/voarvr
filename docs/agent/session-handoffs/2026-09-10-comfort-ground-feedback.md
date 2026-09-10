# Comfortable flight, automatic landing, walking and feedback

## Goal

Implement wearer feedback: optional HUD, forgiving automatic land/perch and left-stick walking, comfortable calibrated head pitch for both creatures, meaningful wind/contact haptics and sound. Preserve existing flight articulation, streamed-world collision and recovery controls. No commit/push.

## Files inspected

Agent index/current-state/repo-map/open-questions/quality-loop; input adapters/frame, flight controller/contact solver, calibration/camera, character profiles, rig driver, HUD, landing guide, world/environment, Blender sources/scripts, relevant Edit/Play tests, character importer/build tooling. Unity MCP skill and live instance were checked. Unity6000.6.0f1 Android, scene `Assets/Scenes/Prototypes/BirdFlight.unity`; Blender5.2.1.

## Files changed

- Input frame/XR/gamepad and HUD: right-stick click toggles, HUD canvas initially disabled and hidden readouts do not format. H toggles on desktop; pause coach exposes binding.
- Controller/contact: automatic top-surface capture, short dissipative approach flare, supported1.25m/s walking, native wall/ledge protection, restore pre-pause phase, measured contact impact strength.
- Calibration/camera: capture natural head pitch for camera and steering on every accepted calibration; up deadzone2/full12 degrees, down deadzone9/full30. Level neutral glide retained. Wrist no longer pitches the body; aerodynamic wrist trim capped at4 degrees, raw rig/stroke orientation preserved.
- New `BirdGroundPresentation`: selected-root binding, folded wings, foot extension, alternating distance-driven gait, pause-preserved pose and airborne restoration. Duck/Dragon each have two authored legs; no invented quadruped gait.
- `blender/scripts/add_ground_rig.py`, rig smoke check, both curated FBXs: add4 leg/foot bones to existing9 wing/root bones, weight existing leg/foot mesh islands, retain geometry and authored source blends. Generated intermediates are ignored; existing Unity meta GUIDs retained.
- New `FlightFeedback`: bounded impact/touchdown impulses/sounds, independently sampled tracked wingtip wind pulses, quiet stereo breeze, focus/pause/calibration gating and clip cleanup.
- Added ground/pitch/calibration/native-boundary/feedback-mapping/OpenXR-binding tests, updated superseded landing/pitch assertions and HUD test. New explicit editor ground evidence helper.

## Commands run

- Unity MCP refresh/Console, EditMode/PlayMode jobs; explicit native ground/roof captures via `GroundExperienceReview.Capture()` in Play mode.
- Blender headless `add_ground_rig.py` on authored Duck/Dragon sources; headless `rig_smoke_test.py` on both generated Ground blends, with `--python-exit-code 1`.
- `python3 -m unittest discover -s tools/scripts -p 'test_*.py'` (12 pass).
- `python3 tools/scripts/check_repo.py` (same24 pre-existing TMP GUID errors).
- Targeted `git diff --check`, status/diff, ADB device check. Build/install result below.

## What worked

122 physics/input tests plus1 actual Oculus binding test passed including preserved neutral glide/energy, both species' moderate wingbeats with neutral and6-degree upward look, automatic landing, support, walking wall/ledge/lower landing, calibration and feedback bounds. Updated16 Play tests pass including selected rig foot count, visible joint alternation, pause/resume support and restored flight IK. Both Blender FBX roundtrips preserve bones/skin and imported wrist deformation. OpenXR HUD click must bind `/{Primary2DAxisClick}`: name-style `/primary2DAxisClick` resolves zero controls on Oculus Touch.

## What failed and corrections

Initial2-degree nose-up neutral cut Duck glide distance and harmed thermal entry; reverted to level neutral. Persistent20-degree wrist rotation exposed Duck lift reversal; bounded aerodynamic trim instead of adding force. Old scene rig survives until end-of-frame destruction: ground binding must follow newly wired wing root. Pausing had lost perched state and could repeat touchdown; fixed. Synchronous multi-pose `camera.Render` reused stale skinning; evidence now yields actual frames. Low side camera intersected terrain; use a real flat generated roof, start with zero speed, and require actual `Perched` before capturing. Default-sandbox Blender crashed at startup; approved external invocation passed. Git LFS status needs metadata writes outside default sandbox. An attempted synthetic bitfield button event test was unsuitable in Edit Mode; final test checks actual Oculus control resolution/handedness. Stale-assembly/zero-test jobs are not validation.

## Quality review

Final independent round02: no blocker/high, technical8 and interaction8. Grounded embodiment7/fold6 means the full art gate is not met; proceed as a functional first pass, with future authored resting poses/clearer foot contact. Latest ground Play rerun passed after final fold.

Evidence and independent review synthesis: `artifacts/reviews/comfort-ground-feedback/round-01/` and fresh `round-02/`. Addressed pause, selected-root binding, HUD discovery, tracked-hand wind gating, native boundaries and pose evidence. Hardware sound/haptic balance, head/neck comfort and sustained Quest performance require wearer testing. No zero-GC claim: this editor's allocation counter returns zero even for known allocations.

## Build and device

Development APK built successfully in51.117s (Unity PlayerBuildInfo and Build Finished: Success). MCP build response was empty/false despite successful build; verified actual build log and new APK. SHA256 `89582c0541322b8b726c70492e1a139a7b9baf1ff2ec63a5f330e8e401a16c97`,81,879,529bytes. Restored exact Android XR preload objects after tests; build fingerprint churn removed. ADB streamed replacement install on Quest3 `192.168.68.52:5555` returned Success. Launched `com.voarvr.prototype/com.unity3d.player.UnityPlayerGameActivity`; process9863 remained alive. PID-filtered `Unity:E`/`AndroidRuntime:E` startup log was empty. This verifies installation/startup, not wearer flight, audio/haptic feel or sustained performance. Final whitespace check passed.

Preloads: GUID `7fef7708042ff4695af6de34cbab4089` localID `-7519165060713318301`; GUID `769f6f731e1c3442e9ea38a5ccf6bae0` localID `-6908929336186598598`. Select exact subassets via editor API, SetPreloadedAssets, SaveAssets. No ProjectSettings delta remains.

## Remaining questions

Wearer acceptance of auto landing, folded silhouette/walk, neutral/+small-up flight and haptic/audio strength. Body sphere collision still permits cosmetic wing clipping; camera wall avoidance remains outside this iteration. Equal-strength rising/falling drafts share haptic magnitude; wind colors carry directional meaning.

## Suggested next prompt

Test Duck and Dragon: calibrate with comfortable arms and head; fly level and with a slight upward gaze, land on ground/roof, walk with left stick, pause/resume and flap off. Right-stick click toggles HUD, B changes view, A starts+calibrates, Meta recalibrates in place, left Menu returns to characters. Report excessive wind buzzing, repeated contact sounds or uncomfortable neck motion.
