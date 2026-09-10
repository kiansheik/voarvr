# Current state

## Quiet cockpit and embodied advanced view — installed; wearer validation pending

Latest Quest feedback confirms advanced handling improved. Downloaded Magpiev3 sessions15930/433frames,221.24/5.98seconds, both clean/zero drops, matching preceding APK source stampb5cf462d. User reports static accumulating magenta bars. No direct headset reproduction yet (screenshot was passthrough); new runtime Shader.Find backplates are the leading suspect. They now use the existing UI Image/Canvas rendering path with explicit cleanup. Repeat menu/session and component-removal tests pass.

All ongoing coach/objective/food text now follows HUD visibility; required calibration prompts remain. Advanced first-person follows complete bird pitch/roll/yaw from each authored eye midpoint. Third-person and Beginner remain stabilized. Advanced pitch uses body-axis rotation relative to the captured arbitrary grip, with6° deadzone/32° full command; roll/yaw authority unchanged.179Edit/22Play/21Python tests pass, including all species' full-input loops. Independent reviews found no high issue; new Quest visual/comfort acceptance remains pending. [Handoff](session-handoffs/2026-09-10-quiet-cockpit.md). APK `2cab1e99…` built38.536s and installed with app data preserved. PID10335 startup without reported exception; focus loss paused rendering, so the magenta fix still needs wearer confirmation.


## Sky-garden comfort and telemetry v3 — built; install pending

Current pass softens Acrobatic control without removing full-input loops, retains Assisted feather support when arms relax moderately, clarifies historical altitude/quiet-gain objectives, and adds optional swept SunMoth catches with points/combos. Kit now has sixteen meshes, richer jade/ivory/coral details, a matched sky and readable cards. Density reduced after independent review; current sample chunk counts 512k–552k vertices still need Quest measurement.

Telemetry v3 adds control/protection and movement/quiet/catch signals with 1200-byte frames, legacy readers and cached cross-session history. Movement proxies are not physical recovery or calories; excluded-phase coverage is explicit. Actual v3 Editor capture: 24,774 frames, clean footer, zero drops. Prescribed recorded-air replay of the failing Magpie interval changed -16.48m to +20.68m, with zero stalled time; this is not a full spatial replay or wearer validation.

172 EditMode, 20 PlayMode and 21 Python tests pass, including catch pause/reset/jump gating. Current build/install status is recorded in the [handoff](session-handoffs/2026-09-10-sky-garden-comfort.md). No commit/push. Older milestone state below is historical.


## Latest Quest playtest — handling failures confirmed in recordings

Downloaded latest Magpie707s and Dragon352s recordings, clean close/zero drops, matching current FlightGame source fingerprint. Advanced rates are excessive; Magpie lost16.5m in11.55s of3.41m/s rising air with little active flapping, while Dragon had a142m Beginner climb that was not clearly communicated. Soaring progress can show100% while hidden altitude condition blocks advancement. Captured frame timing is largely72Hz; this does not indicate widespread frame collapse. No runtime retuning yet. [Actual playtest diagnosis](../development/quest-flight-game-playtest-2026-09-10.md). Earlier no-install/no-device-data notes below are historical, superseded by these recordings.

## Flight Game v1 — built; quality acceptance pending

Coherent desktop Skyward slice implemented over d62ad94; no commit/push. Optional inertial Acrobatic controls, real trajectory tricks, shared terrain-fed lift, original authored ground/sky kit, activities/physical expedition/result/local best/unlock, telemetry v2. Original Beginner three-species20s glide/turn comparison matches HEAD. Continuous input-only expedition physically completes in180.4simseconds, quiet-wing gain162.7m, score577 saved; human10–20minute pacing unverified.

Final165 EditMode /19 PlayMode /19 Python tests pass. Blender15-mesh validator and actual Blender/Unity MCP inspection pass. Three rounds of five independent critics: no confirmed high/blocker left in reviewed scope, but strict art/readability ~4.5 and overall ~5–5.5 remain below requested gates. No hardware performance/comfort claim. Repo checker retains24 pre-existing TMP GUID findings.

Development APK built successfully48.750s,82,039,714bytes, SHA256 `d25e31279508d9effa7c30da85f4f686ab70c5925350c6328ddbd54ec3b1e6c7`. Quest discovery offline; asked user to wake for install. No new installation, hardware play or telemetry pull. [25-item validation report](../development/flight-game-v1-validation.md), [design](../../design/FLIGHT_GAME_V1.md), [handoff](session-handoffs/2026-09-10-flight-game-v1.md).

## Magpie and telemetry — built; Quest installation pending

The new original Pica pica has63 bones, progressive feather articulation and a distinct lightweight profile. Local development recording captures raw motion, calibration, mapped rigs, flight/world state and timing availability; CLI analysis and calibrated simple-environment replay are implemented. Final Unity6000.6.0f1 suites pass135 Edit/17 Play; Python18/18; Blender validation/roundtrip and actual MCP deformation checks pass. Research and geometry are reconciled in the [ledger](../research/species/magpie.md).

Development APK SHA256 `b068a99f5f5dc1402ff2743c44aa6264afabbdbe855845f78c6b78cdc149fdd7` (82,005,021 bytes) built successfully in101.767s. Quest discovery currently fails; user has been asked to wake it. No new install, wearer capture, ADB pull or device-overhead claim yet. Existing installed comfort/ground build below remains the device baseline. Editor recording decoded29,251 frames with clean close and zero drops; this is not headset evidence. Three critique rounds found no high/blocker, but visual overall7/fold6 means the all8 art gate remains unmet. Full [validation report](../development/magpie-telemetry-validation.md) and [handoff](session-handoffs/2026-09-10-magpie-telemetry.md). No commit/push.

Updated2026-09-10 for comfortable pitch, automatic landing/walking and immersive feedback. No commit/push. Code and checked-in configuration remain authoritative; preserve the current worktree. Latest [handoff](session-handoffs/2026-09-10-comfort-ground-feedback.md); previous [HUD/thermals](session-handoffs/2026-09-09-flight-instruments-v1.md).

## Controls and comfort

Third-person default. A returns to start, captures comfortable arm/head neutral and selects third-person; B toggles first/third; Meta recenter captures in place after stable tracking, preserving flight/view. Left Menu returns to character select; X pause; Y weather. Right-stick click toggles HUD (Oculus uses control usage `/{Primary2DAxisClick}`); H is desktop equivalent. Left stick walks while supported. Pause coach exposes the HUD binding.

HUD now starts hidden; enabling shows the existing camera-parented world-space instruments at2m: relative airspeed knots, loaded-ground clearance metres, actual climb/sink, heading/bank/pitch and actual vertical-air cue. Hidden text does not format. Two20-point wingtip histories remain1.6–4m long, cyan→violet with speed; reset/rebase handled.

Head steering and camera recapture natural head pitch on every accepted calibration. Up deadzone2°/full12°, downward viewing deadzone9°/full30°; neutral is level, preserving the established still-air glide. Wrist orientation no longer commands body pitch; bounded±4° aerodynamic trim preserves ordinary lift while raw visual twist and stroke-force direction remain. Both species sustain moderate strokes at neutral and6° upward relative gaze in deterministic tests; this is not worn comfort validation.

## Ground and presentation

Actual swept top-surface contact on slopes≤35° automatically lands without speed scoring/failure. Short anticipatory flare dissipates approach velocity, but no capture without contact. Walls/steep faces/upward takeoff cannot perch. Supported walking runs1.25m/s with horizontal obstruction and downward support sweeps; ledges return to flight, lower surfaces can catch the bird. Pause preserves previous phase, grounded pose and contact counts.

Both curated FBXs now retain nine wing/root bones plus four leg/foot joints, weighted to existing foot/leg islands. Authored `.blend` sources remain unchanged; `add_ground_rig.py` produces ignored Ground blends and exports. Both current creatures have two legs. `BirdGroundPresentation` follows the newly wired model root, folds wings/primaries, extends feet and alternates a distance-driven gait; airborne pose restores tracked wings. This remains procedural prototype animation, with body-sphere contact rather than wing/individual-foot collision.

`FlightFeedback` creates short procedural collision/touchdown sounds and a quiet stereo breeze once per scene. Measured inward impact speed determines brief bounded controller impulses; tracked left/right wingtips independently sample real wind for softer intermittent pulses. Focus loss, pause, missing calibration and stopped flight silence/gate cues; resting support is not a crash. Worn audio/haptic balance is unverified.

## World and flight retained

25 pooled128m chunks, continuous seeded city/forest fields, double horizontal logical coordinates and128m origin shifts beyond768m; nearby3×3 native collision ring and cooperative generation. Finite drifting/tilted air fields and24 pooled visual traces sample shared physics. Gold rising air, cyan ambient, rose sinking. Assisted plumes strength9.4/radius48/altitude30–95m and streets5.2; bounded Assisted-only aerodynamic feathering prevents deep incidence stall, no extra force. Touring/Wild/StillAir remain available. Neutral still-air glide is Duck43.12m versus Dragon81.80m per10m drop; still-air energy does not increase. Camera wall avoidance and cosmetic wing clipping remain limitations.

## Validation and next gate

122 physics/input Edit tests plus the actual Oculus binding test pass;16 Play tests and12 Python tests pass, with the final fold regression also passes. APK89582c05… built in51.117s, installed and launched on Quest3 (PID9863), with no PID-filtered Unity/Android runtime startup errors. Details in the latest handoff. Both Blender rig roundtrips pass. Repo checker retains24 pre-existing TMP GUID failures. Independent visual/player/technical review evidence is under `artifacts/reviews/comfort-ground-feedback/`; final review and APK status are recorded in the handoff.

Do not claim zero allocations: `GC.GetAllocatedBytesForCurrentThread` reports zero even for known allocations in this editor. Standalone frame time, haptic/audio quality, visible gait and neck comfort require Quest playtesting. Installed VoarVR persists under Quest Library→Unknown Sources. Restore exact Android XR preloaded subassets after Unity tests before building (see handoff/history), and do not call stale-assembly/zero-test jobs successful validation.
