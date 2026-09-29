# Current state

## September 29 — hand controls revision 2 installed on Quest

First wearer session (`6a5e0f27`) compared with five controller sessions: inputs matched controller speeds, but 16% of airborne time was an unintended flare (span wider than the compact calibration) and natural shoulder-plane hands dropped to Wide Motion Mode's inferred pose, which jumps 25–39 cm at handoffs and correctly earns no flap force. Revision: no span flare for hands, forward-sweep "backstroke" flare latched until a normal downstroke, calibration 0.25–0.65 m per side, continuous inferred handoff, 0.06 s recovery, perched wings mirror arm spread. EditMode 324/324, PlayMode 70/70. APK `fe75c541…` installed and running on Quest 3 (no fatal or Unity errors). Awaiting wearer feedback. [Telemetry review](../reviews/2026-09-29-hand-controls-telemetry.md).

## September 29 — hands-first flow closed out locally; ready for a Quest test

Controller-free Magpie Route Home is implemented end to end in source: gaze + pinch preflight, calibration perched on an authored ridge lookout (`FlightRegions.DepartureLookout*`) with the bird visible, measured takeoff, two-pinch rest, settings, recalibration, results and return. Cards draw over terrain; gaze uses a two-tone reticle and hover. Round-3 visual/player critique drove fixes for calibration exits, hands resume, focus return and card re-anchoring (technical critic was cut off by a usage limit). **EditMode 318/318, PlayMode 70/70, host 29/29.** Scripted Magpie pilot completes from the lookout in 215.85 simulated s. Fresh Development APK built from this tree (see handoff for hash, manifest and `VerifyApk()` result). Not installed; no Quest evidence. Deferred items (HUD-hidden coaching, moth tally, chase-camera horizon roll, afterglow) are listed in the [review](../reviews/2026-09-28-hand-flight-implementation.md). [Handoff](session-handoffs/2026-09-28-hand-flight-implementation.md). No commit/push.

## September 28 — Quest discovery subnet fix

`quest.py` now uses the default interface and actual IPv4 netmask, instead of hardcoding `/24`. This Mac is on `192.168.68.0/22`; all1022 hosts are covered with bounded discovery.27 host tests pass. Live full-subnet discovery found no TCP5555 listener; cached address192.168.68.52 responds to ping but refuses5555, and ADB/mDNS discovery is empty. Network presence is not an enabled wireless-debugging session. No install/launch occurred. [Handoff](session-handoffs/2026-09-28-quest-discovery.md).

## September 28 — local competition preflight executed; hands-first candidate pending

User confirmed US/Start membership. Reconnected Unity6000.6.0f1 and ran the merged branch: **297/297 EditMode**, **58/58 full PlayMode**, **12/12 final affected player-facing tests**, **23/23 host tests**. Independent visual/player/technical review drove bounded fixes for rival landability, course input recovery, v4 turn/estimated-motion replay, objective/reward feedback and UI layout. [Full review](../reviews/2026-09-28-competition-preflight.md), [handoff](session-handoffs/2026-09-28-competition-preflight.md).

Fresh evidence includes72 staged lifecycle/UI captures across all five courses,46 runtime-camera guidance fixtures and a202.073s Magpie input-only Route Home with seed, real landing and saved restoration. The final screenshot agrees with completion. These do not establish physical completion of the five courses, authored cold-start onboarding or Quest acceptance. Saves are unchanged:311 gameplay files, no additions, no gameplay preference changes. Temporary EditorSettings/XR preloads are restored byte-for-byte; no authored scene/prefab was changed.

Scoped UI/guidance/safety scores are8/10; world composition/materials remain6/10 and stylization7/10. The overall visual gate is not met. SDK/hand adapter/continuity, compact calibration, gaze/pinch UI and the perched Magpie judge opening are still missing. V4 now preserves supported yaw and estimated-motion flags; future hand signals require another schema version and activity/wingbeat accounting still needs provenance. No Quest install/recording, commit or push. The conflict is resolved; the merge remains uncommitted. The checker retains24 known TMP findings.

Fresh local Android development APK succeeded in57.835s (0 errors/32 warnings),100,681,339 bytes, SHA-256 `4067556e…e2a2aba`; embedded source fingerprint matches. This controller regression APK was not installed. Build evidence and warnings are in the current review.

## Earlier September 28 merge-only phase — superseded by executed preflight

The following records the initial merge-only pass; the current verification, eligibility and telemetry status is above.


Feature head `7798d3a` and incoming main `183557f` match origin. Resolved the sole conflict in `log.md` by preserving both September 12 entries and the September 27 competition entry. Auto-merged code retains `MotionEstimated` and both stroke-energy guards alongside the course/session/gameplay work. The merge is uncommitted; prior staged assets and runtime changes are preserved. [Review handoff](session-handoffs/2026-09-28-hand-flight-merge-review.md).

The [competition brief](../competition/meta-vr-start-2026.md#readiness-at-september-28) now tracks concrete exit evidence. Next: compatible hand SDK/adapter, continuity and telemetry v4, then complete hand UI and compact calibration. Session activity/wingbeat accounting and replay must preserve motion provenance before hand input is enabled. Entrant eligibility is separately unconfirmed under the current official rules.

This pass: 21/21 host tests pass; repository checking still reports only the 24 documented TMP GUID findings. Unity 6000.6.0f1 is open for this project but MCP reports zero connected instances, so no fresh Unity tests or APK were produced. Reconnect with **VoarVR → Tools → Reconnect Installed MCP Session**, then run current hand-safety/flight/ground/replay tests through the Test Runner. The September 12 suite counts, source-matched APK and headset connectivity below are historical evidence for that controller build, not validation of this merged branch or current device status.

## Meta VR Start 2026 hands-first competition branch — plan and first safety guard

Competition branch `meta-vr-start-2026-hand-flight` targets **Gaming / Adapted / Significantly Updated Experience**. The narrowed entry is a controller-free, compact seated version of A Route Home with a roughly five-minute launch → thermal → seed → arch → garden payoff. Strategy, rubric mapping, submission milestones and cut scope are in [competition plan](../competition/meta-vr-start-2026.md); tracking architecture and agent work order are in [hands-first flight](../../design/HAND_FLIGHT.md).

First low-risk solver change is implemented: `WingInput.MotionEstimated` lets an inferred/predicted pose remain available for continuity and steering while `BirdFlightController` refuses to turn its estimated velocity into active stroke energy. A new EditMode regression compares an identical 2.5m/s measured versus estimated downstroke. This is the foundation for Wide Motion Mode/source transitions; no Meta XR SDK dependency, hand adapter or gaze UI has been added. The later September28 preflight implements the limited telemetry-v4 contract described above.

Research target is Meta XR Core/Interaction SDK v207+ on the existing OpenXR backend, with Wide Motion Mode evaluated for lateral wing poses, Fast Motion Mode decided from Quest data, and unextrapolated hand poses recorded for replay/tuning. The September 27 changes had source review only; merged Unity regression tests now pass in the September28 preflight above; Quest hand validation remains pending.

## Quest playtest follow-through — built; headset install and wearer acceptance pending

The September 12 wearer capture remains the latest Quest recording: `acb16e8e…`, Magpie/Skyward, 96,393 frames and 1,338.96 simulated seconds with a clean footer and zero writer drops. A fresh read-only check now confirms the remote and local 116,227,660-byte files share SHA-256 `d5185550…b02bc`; no newer remote session exists. The trace and original diagnosis remain in the [evidence brief](../reviews/2026-09-12-quest-playtest-next-round.md).

The requested next round is now implemented in first-party runtime code. It adds four typed moths with rarity/value/behaviour and a persistent goal/value ribbon; profile-scoped compact session history and an explicit Left Menu → Finish Session → two-column results flow; active/flapping/walking/quiet/supported/paused time, wingbeat estimate, flight/walk distance, ascent/descent, speed, landings, tricks, contacts, moth totals and personal records; right-stick supported yaw; diagram-led safe calibration redo; Duck and Dragon tuck/flare presentation; directional/material collision response, chase-only camera recoil and local foliage disturbance; branch and compound landmark collision; HUD-independent campaign guidance; three bounded noncombat rivals; and a data-driven five-level obstacle-course mode with 3-2-1 starts, ordered tasks, local comparable leaderboards and a required six-second rest before retry. There is deliberately no exhaustion meter or fatigue penalty.

Course timing and persistence include safety/fairness handling: pause, reset, tracking loss, recovery, app suspension, control/weather changes, collisions and long frames make an attempt practice/non-ranked; real finish surfaces and deterministic course moths prevent invisible or one-shot softlocks; records are keyed by rules/species/mode/wind/assistance and retain each local player's best. The final audit also closed frame-hitch gate continuity, platform-recenter calibration-context preservation, ambient-moth interference during course quotas, unsupported future-summary preservation and finalized-result escape paths. Finalized sessions are return-only and remain authoritatively paused through focus or recenter transitions. Existing Journey v2 and Foraging v1 data remain separate; typed foraging uses a v2 key and crash drafts commit transactionally.

Unity 6000.6.0f1 passes 286/286 EditMode (job `101b9fb371d047d0adca5115b01857bb`) and 54/54 PlayMode tests (job `6234e8eebc3548438b151791e57ea7d2`); the final focused collider suite passes 5/5 (job `7bc2d7ede2934fa7b63327d157b85908`). Host tooling passes 21/21 and `git diff --check` passes. A fresh seven-screen independent review found no high objective defect and scored legibility, hierarchy, provisional VR comfort, completeness and course guidance 8/10; this is not Quest hardware validation. The repository checker still reports the known 24 unused TextMesh Pro sample GUID findings. Current ignored screenshots are under `artifacts/reviews/playtest-features/`; the implementation handoff is [here](session-handoffs/2026-09-12-playtest-features-implementation.md).

A current Quest development APK was built from the live Unity Editor in 91.360 seconds with BuildReport `Succeeded`, zero errors and five warnings: `builds/quest/VoarVR.apk`, 82,339,095 bytes, SHA-256 `61a4b5ee5e4aca70ab32b82b7c7052dea4aabcfa0078e9b0a5c09cd82d8d36b7`. ZIP integrity is clean and its embedded source SHA-256 `7764cae054cd89b5b93a609cd983bdd309c2dd62c28ede5835daa8efc573fb10` matched the September 12 source. Exact OpenXR preload subassets were restored live and on disk after tests/build, leaving no settings delta. Quest 3 remains `adb offline` and reconnect reports `Host is down`, so install, launch, real-controller completion of all five courses, stereo text size, comfort, collision feel and sustained Quest performance remain acceptance work. No commit or push was made.

## Route Home guidance and seed possession — built; Route Home wearer test deferred

Actual first-adventure Quest capture confirms the seed was never collected: its objective activated126–148° behind motion, once beyond1.1km. User chose to keep text hidden. Added open gold route chevrons, distinct real-target markers, an offscreen turn cue, genuinely sampled lift, early-shrinking pickup transfer and visible species/view-aware carried seed. Existing physics and HUD preference are preserved. [Implementation and evidence](../reviews/2026-09-10-route-guidance-implementation.md), [handoff](session-handoffs/2026-09-10-route-guidance.md).

240 EditMode/37 PlayMode/21 Python tests pass; final carry adjustment reran8 affected PlayMode tests. Three independent review rounds removed destination obstruction, pickup occlusion and Dragon carry readability issues; scoped static navigation/possession8/10, general material finish6/10, wearer comprehension/comfort/performance unverified. Final46 runtime-camera fixtures retain hidden text and unchanged saves. APK3797aa32… originally failed installation when Quest went offline. The 2026-09-12 capture proves that a Quest build with the same selective source fingerprint ran, not that this exact APK was installed. Route Home itself was not selected, so its guidance/carry acceptance remains unverified. Known24 TMP GUID findings remain.

## A Route Home — first adventure implemented

The user selected a complete first adventure with core fixes, story payoff and focused visual polish. Default RouteHome follows lift/ascent → seed → arch → real garden landing; restoration persists. Adventure rewards no longer penalize flapping/rest, Training has a separate efficiency record. Explicit preflight/help, versioned save/recovery, supported-perch resume, in-place calibration, independent view comfort, chase collision checks and distinct/spatial story feedback are implemented. Quiet HUD behavior and advanced neutral flight are retained. [Implementation/status](../reviews/2026-09-10-route-home-implementation.md), [handoff](session-handoffs/2026-09-10-route-home-implementation.md).

233 EditMode/32 PlayMode/21 Python tests pass. Three independent review rounds fixed crown attachment, cloud closure and species-aware carried-seed visibility; assessed static readability is8, rest/agency9. Lighting/material goals remain6–7 and wearer comfort/fun/exercise/pacing are unverified. All three input-only routes completed with saved restoration; revised Magpie202.07s and Dragon500.58s have one contact each at landing. Original rough pilot evidence remains. Existing24 TMP GUID findings persist. Build/install and exact final evidence are recorded in the handoff; do not treat this as completion of all24 roadmap work orders.

## Professional game critique and future roadmap — documentation complete

Saved [game critique and roadmap](../reviews/2026-09-10-game-critique-and-roadmap.md), with independent [visual](../reviews/2026-09-10-visual-critique.md), [player](../reviews/2026-09-10-player-critique.md) and [technical](../reviews/2026-09-10-technical-critique.md) reports. Provisional present content/design assessment is 4–5/10; actual fun, comfort, exercise effectiveness and sustained Quest delivery remain unverified. Proposed direction: a living sky sanctuary, authored flight journeys, separate efficiency/movement rewards, meaningful rest and saved consequences. 24 bounded W work orders include owners, dependencies and acceptance; no new gameplay implementation is implied.

Fresh decoding of latest pre-neutral source b8bb65ee captures confirms Duck and Magpie both complete Training (session 119.78s/score554 and21.52s/score988); Magpie records a BackLoop at161.99s. These are positive actual detector/progression outcomes, not full-expedition or comfort validation. Read-only MCP confirmed the correct Unity6000.6.0f1 Android project and idle BirdFlight scene. No fresh playthrough/build/install in this review. Existing advanced-neutral source/tests and documentation work preserved. [Review handoff](session-handoffs/2026-09-10-game-critique-roadmap.md).

## Advanced neutral equilibrium — installed; ready for wearer test

User confirms the latest experience is much better; remaining request is to stop needing sustained forward wrist twist for neutral advanced flight. New Duck518.72s/Magpie352.12s Quest captures are clean/zero drops and match installedb8bb65ee source. In a6.19s quiet Magpie window, +30.23° wrist input produced+.1651Nm pitch torque against-.1646Nm passive airflow torque. Earlier compensation reverses direction: a fixed wrist offset would mask a speed/air-dependent problem.

Narrow fix in AcrobaticFlight scales passive pitch with deliberate input, making calibrated zero produce zero passive pitch moment, and limits opposing passive pitch to half actual control torque. Existing damping settles residual motion; yaw, roll, camera, Beginner and captured grip are unchanged.190EditMode tests pass, including neutral inclined-air, release damping, both-sign monotonic authority and actual full trajectory loops/energy for all species. [Handoff](session-handoffs/2026-09-10-advanced-neutral.md). APK e30314ba… installed and launched PID15102 without matched startup errors.


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

Third-person default. First launch and A open the diagram-led comfort calibration coach; an accepted redo preserves the current flight, while platform recenter pauses and reopens the context after stable in-place calibration. B toggles first/third; X pauses; Y changes weather. Left Menu opens the session/rest context directly, including Finish Session and results. Right-stick click toggles instruments (Oculus usage `/{Primary2DAxisClick}`); left stick walks and right stick smoothly yaws only while supported. Physical turning remains additive and airborne right-stick yaw is absent.

Instruments still start hidden and remain optional. The separate top gameplay ribbon always carries mission/course progress, direction/distance, typed moth counts, value and combo; campaign world guidance also remains available without gauges. Full-screen calibration, pause, rest and result cards suppress the bird, wakes and course guide lines so presentation cannot draw through them.

Head steering and camera recapture natural head pitch on every accepted calibration. Up deadzone2°/full12°, downward viewing deadzone9°/full30°; neutral is level, preserving the established still-air glide. Wrist orientation no longer commands body pitch; bounded±4° aerodynamic trim preserves ordinary lift while raw visual twist and stroke-force direction remain. Both species sustain moderate strokes at neutral and6° upward relative gaze in deterministic tests; this is not worn comfort validation.

## Ground and presentation

Actual swept top-surface contact on slopes≤35° automatically lands without speed scoring/failure. Short anticipatory flare dissipates approach velocity, but no capture without contact. Walls/steep faces/upward takeoff cannot perch. Supported walking runs1.25m/s with horizontal obstruction/downward support sweeps and bounded right-stick yaw; ledges return to flight and lower surfaces can catch the bird. Pause preserves previous phase, grounded pose and contact counts.

Curated rigs retain their authored wing/root and leg/foot joints. `BirdGroundPresentation` folds wings/primaries, extends feet and alternates a distance-driven gait; airborne pose restores tracked wings. Duck and Dragon now add architecture-specific tuck/flare gross poses after shared IK, while Magpie retains its fine feather/manus/tail response. Bird contact remains a swept species body sphere rather than individual wing/foot physics.

`FlightFeedback` uses contact point, normal, side and material kind for spatial material sounds, side-weighted haptics, bounded bird response and third-person chase recoil; first-person artificial rotation stays suppressed. Tree contacts locally disturb passable foliage while solid trunks/branches remain physical. Focus loss, pause, missing calibration and stopped flight silence/gate cues; resting support is not a crash. Worn audio/haptic balance remains unverified.

## World and flight retained

25 pooled128m chunks, continuous seeded city/forest fields, double horizontal logical coordinates and128m origin shifts beyond768m; nearby3×3 native collision ring and cooperative generation. Finite drifting/tilted air fields and24 pooled visual traces sample shared physics. Gold rising air, cyan ambient, rose sinking. Assisted feathering and established species tuning remain unchanged. Streamed trees bind branch primitives; tapered roofs, Ruin hex pillars, Rock, Spire and Log use exact mesh-derived or compound silhouette/opening profiles rather than invisible full rotated bounds. Course corridors reserve deterministic space. Cosmetic wing clipping remains possible because the bird itself still uses a body-sphere sweep.

## Validation and next gate

September 12 controller validation was 286/286 EditMode, 54/54 PlayMode and 21/21 Python tests, with the focused `SkywardWorldTests` collider run passing 5/5 and a clean diff check. The independent current seven-screen gate passes at 8/10 in every required dimension with no high objective defect; it is not Quest hardware evidence. Repo checking retains 24 pre-existing TMP example GUID failures. A source-matched Quest development APK was built successfully and passed ZIP integrity, but it has not been installed or launched because the headset is offline.

Do not claim zero allocations: `GC.GetAllocatedBytesForCurrentThread` reports zero even for known allocations in this editor. Current stereo readability, real-controller completion, haptic/audio/collision feel, turn/calibration comfort, sustained frame time and thermals require installation and a wearer pass. Installed VoarVR persists under Quest Library→Unknown Sources. Preserve the exact restored Android XR preload subassets around future Unity tests/builds (see handoff/history), and do not call stale-assembly/zero-test jobs successful validation.
