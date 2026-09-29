# Hands-first bird flight

This is the implementation contract for the Meta VR Start 2026 hand-flight work. Android now selects the hand adapter by default; the controller adapter remains in source. Implementation is separate from Quest acceptance: synthetic tests and desktop captures do not establish tracking quality, hand-only route completion or wearer comfort.

## Architecture decision

Do **not** rewrite the aerodynamic solver.

The current dependency direction is already correct:

```
device tracking -> IFlightInput -> FlightInputFrame -> BirdFlightController -> presentation
```

`BirdFlightController` consumes tracking-local wing position, orientation and velocity after calibration. Active stroke work is computed from the component of down/back wing velocity against the calibrated wing pressure normal. That means a hand wrist/palm transform can replace a controller transform while preserving the established flight model, species tuning, contact solver, wind, Route Home logic and telemetry.

The implemented adapter is:

```
MetaHandFlightInput : ITrackedFlightInput : IFlightInput
```

`ITrackedFlightInput` shares calibration and tracking-reset lifecycle with `XRFlightInput`. Meta APIs and hand skeleton details stay under `Game/Input/`; the solver remains device-independent. `UI/HandGazePointer` drives the existing UGUI Buttons directly from head gaze and a confidence-filtered index pinch. Meta Interaction SDK is not required for this path.

## Why hand flight is harder than controller flight

The physical motion we want is unusually hostile to camera-only hand tracking:

- the hands spend substantial time beside the headset rather than centered in front;
- a downstroke can be fast;
- the body/head can occlude one hand;
- direct tracking can disappear and later reacquire at a different reported pose;
- the simulator derives active work from velocity, so a one-frame pose jump can become a large false force.

This is not a reason to move the mechanic in front of the face. It is a reason to make tracking quality a first-class input.

Meta's current hand stack provides two useful modes:

- **Wide Motion Mode (WMM):** uses Inside Out Body Tracking to maintain plausible hand poses outside the camera field of view. `OVRHand.PoseSourceInferred` distinguishes inferred WMM poses from camera-based poses.
- **Fast Motion Mode (FMM):** increases tracking from the default 30 Hz to 60 Hz for rapid hand motion, with a documented jitter tradeoff. Meta recommends testing default mode first and enabling FMM when fast movement actually causes tracking loss.

References:

- https://developers.meta.com/horizon/design/hands-technology/
- https://developers.meta.com/horizon/documentation/unity/unity-wide-motion-mode/
- https://developers.meta.com/horizon/documentation/unity/fast-motion-mode/
- https://developers.meta.com/horizon/documentation/unity/unity-handtracking-unextrapolated-poses/

## Wing pose source

`HandInteraction` selects the OpenXR hand skeleton and reads Meta hand root poses in tracking-local Unity coordinates. Accepted calibration captures each hand's neutral orientation rather than assuming the controller grip basis applies. Display pose, source classification, standard sample timestamp, and available unextrapolated pose/timestamp enter `WingInput`.

Implemented per-hand data:

- predicted/display wrist root position + orientation;
- whether the hand is tracked;
- confidence classified into DirectHigh or DirectLow;
- direct camera vs WMM inferred pose source;
- unextrapolated root pose + capture timestamp when available.

`HandPoseContinuity` keeps trusted derivative history and bounded recovery/gap timers internally. Separate confidence values, exported last-direct age and an explicit serialized continuity state remain future instrumentation; do not infer them from the current schema.

The unextrapolated API is specifically useful here because Meta exposes the real capture timestamp and recommends it for motion analysis, replay and custom prediction. Record it; do not necessarily render from it.

## Tracking-quality state machine

The adapter distinguishes four semantic states alongside `Tracked`. `Unknown` is retained for legacy controller records.

### DirectHigh

Camera-tracked and sufficiently confident.

- full pose authority;
- full measured motion authority;
- eligible to create flap work;
- update direct-pose history and velocity estimator.

### DirectLow

Camera source exists but confidence is poor.

- keep a smoothed pose if visually useful;
- suppress gesture edges;
- initially suppress active flap work;
- do not let noisy motion update the trusted velocity model.

Only promote low-confidence motion to partial authority if Quest recordings demonstrate a clear benefit.

### Inferred

WMM/body tracking is supplying the pose.

- preserve the wing silhouette, span, bank/steering continuity and low-frequency body-frame estimate;
- mark the wing's motion as estimated;
- **do not create active flap energy from inferred velocity in the first implementation**;
- allow glide and thermal riding to continue naturally.

`WingInput.MotionEstimated` blocks active stroke force while a tracked pose can still provide bounded presentation and steering. Effort, session activity and wingbeat accounting also exclude estimated or non-direct hand motion.

If later telemetry shows WMM motion is trustworthy enough to support flaps, replace the boolean safety gate with a measured 0..1 stroke-authority signal. Do not guess a fractional value first.

### Lost

No usable direct or inferred pose.

The implementation holds the last pose for at most 0.18 seconds with zero velocity and `MotionEstimated=true`, then marks it untracked. It does not extrapolate a lost hand indefinitely.

A lost hand must never cause:
- an impulse;
- an automatic dive/tuck;
- a heading snap;
- a false landing brake;
- a scored flap.

## Reacquisition invariant

**Tracking recovery must not create energy.**

The controller adapter already suppresses velocity on the first sample after `Tracked=false`. Hand tracking needs the stronger version because WMM may keep the hand nominally tracked while the pose source changes.

Implemented transitions, including a change between standard and unextrapolated motion streams:

1. detect the source edge;
2. reset trusted finite-difference history;
3. smooth presentation toward the new pose during recovery;
4. keep `MotionEstimated=true` through a 0.06-second warmup (0.12 s before the first Quest session) and until a stable direct capture interval establishes velocity again;
5. only then restore active stroke authority.

No one-frame position delta across a source transition may enter the squared stroke-force calculation. Current conservative bounds also reject a capture-position jump above 0.3 m, stale/reversed timestamps and invalid poses; trusted velocity is capped at 8 m/s. These limits need evaluation against recorded Quest motion, not assumed physiological validity.

## Compact seated mapping

Hands mode uses compact calibration by default. Bird calibration retains the distinction between human neutral pose and species presentation scale.

Current accepted pose and intended movement:

- each hand 0.25–0.65 m laterally from the tracked body centerline at neutral (widened after the first Quest session to include the recorded controller posture);
- elbows may stay bent;
- hands may sit forward of the shoulder plane; mean forward offset must remain within 0.55 m;
- the authored bird still displays its natural full span;
- the player performs compact but recognizable down/up strokes.

Do not simply multiply all hand velocity by a large constant. That would amplify jitter and tracking jumps. Scale the **pose envelope** into bird space while tuning active-work response from measured sessions.

A separate Full Wing / Fitness hand option is future work. Compact calibration is implemented; the airplane-seat comfort test remains a wearer acceptance gate.

## Flight verbs without buttons

The core airborne verbs should not depend on finger gestures that may be unavailable while hands are lateral.

| Verb | Implemented hands path |
| --- | --- |
| Calibrate | hands-free: hold the natural glide pose, look left and right (learns each hand's inferred-pose bias), gaze+pinch START with arms free, return to the pose, 3-2-1 countdown captures the last frame and launches a glide; redo/recenter/resume skip the look-around |
| Take off / flap | down/back wrist/palm motion |
| Glide | spread and quiet hands |
| Bank | asymmetric hand height plus calibrated wrist roll, retaining current mapping |
| Climb/descend | preserve comfortable head-pitch Beginner mapping initially |
| Tuck/dive | shorten span / draw wings inward; do not require trigger |
| Flare/land | sweep both hands forward ("flap backwards") to latch the brake until a normal downstroke; raised, held spread wings near a perch still brake. Spreading wider than calibration is **not** a flare for hands (it was the main cause of lost height in the first Quest session) |
| Walk / turn on a perch | pinch then move the left hand to walk, or the right hand sideways to turn; solver consumes these axes only while supported |
| Pause/settings | bring both hands forward, hold both index pinches for 0.6 seconds, then release; use gaze+pinch in the rest menu |
| View / controls / wind / instruments | Flight settings page in the rest menu; guidance, audio and horizon comfort remain available |
| Recalibrate | rest-menu coach and platform-recenter lifecycle both require explicit READY confirmation |
| Character/activity/course selection | head gaze + index pinch on existing UGUI Buttons; default species is Magpie |
| Finish / return | explicit session results and saved return through gaze+pinch Buttons |

Do not require pinch recognition during a flap. Pinch is used for deliberate menus and supported ground movement when the player has brought a hand into a well-tracked interaction zone.

Gaze targets require a released pinch before every activation and after tracking loss, panel opening or page changes. After initial calibration, a non-course flight recovers to a real supported departure perch with zero initial takeoff; measured motion must launch it. Courses retain their authored starting positions/countdown. This is an implemented lifecycle, not proof that the complete judge route is now comfortable on Quest.

The departure perch is the authored ridge lookout `FlightRegions.DepartureLookout*` (logical 72, ~23.3, 60; heading 18° toward the departure thermal's mean centre), not the spawn floor. The spawn sits in an 85 m flattened bowl, so a grounded bird there faces rising terrain in every direction. A 10 m scatter clearing keeps the perch open. If the lookout cannot support the bird, recovery falls back to the spawn floor and the departure still resumes; a final failure opens the rest menu in hands mode. A scripted semantic-input Magpie pilot completes Route Home from this perch in 215.85 simulated seconds and finds the lift edge 2.2 s after launch. Whether that early success helps a first-time wearer is a Quest question.

Rest, settings, calibration and result cards are drawn over world geometry (`UI/WorldCardRendering`: a copy of the built-in canvas material with `unity_GUIZTestMode = Always` and render queue 4000). At roughly 2 m with the chase view pitched down, a perch slope otherwise cuts through their lower rows, including the results card's only exit button. Stereo depth comfort of an always-on-top card is unverified on Quest.

## Implemented SDK integration and device gates

The package manifest pins **Meta XR Core 207.0.0**, whose imported OVRPlugin is **1.207.0**. Unity OpenXR remains the backend. Meta hand skeleton and source/unextrapolated APIs are used through the first-party adapter. The current UGUI gaze/pinch path does not install Meta Interaction SDK.

On Android the adapter requests `com.oculus.permission.BODY_TRACKING`, then calls `SetWideMotionMode2HandPosesEnabled(true)` after permission is granted and reads `IsWideMotionMode2HandPosesEnabled()`. Permission refusal/unavailable APIs must not be reported as working WMM. The request and reported state are implemented; actual inferred tracking retention and behavior remain unverified on hardware.

Hand tracking frequency stays at the SDK default, with no FMM request. Compare default tracking against FMM on real Quest captures before changing this. Keep Meta SDK types out of the aerodynamic solver, wind, gameplay and world code.

Verify API capability as well as package version before selecting dependencies: Meta documents `OVRHand.PoseSourceInferred` / `OVRPlugin.GetHandPoseSourceInferred` as experimental and requiring OVRPlugin **1.115.0+**. Record exact imported versions and confirm the API works with WMM enabled; a `v207+` package label alone is insufficient evidence. [Meta WMM documentation, checked September 28](https://developers.meta.com/horizon/documentation/unity/unity-wide-motion-mode/).

## Telemetry: implemented v5 and remaining instrumentation

Schema 5 contains **325 signals / 1336 bytes per frame**, preserving v4's complete 301-field / 1224-byte prefix and legacy readers. V4 retains raw/mapped supported turn and `MotionEstimated`; v5 adds each raw hand's source classification, standard sample timestamp, unextrapolated availability/pose/timestamp, plus SDK-reported WMM2 enabled state and app-requested FMM state. The FMM request signal is currently zero and does not measure actual OS tracking frequency. See the [telemetry contract](../docs/development/telemetry.md#schema-5-hand-source-and-capture-provenance).

The existing raw position/orientation fields contain the continuity-filtered pose consumed by flight, not a separate unfiltered display snapshot. Confidence is represented through DirectHigh/DirectLow classification; Inferred/Lost have no independent confidence field. Four native capture timestamps retain float64 precision and their tracking-runtime clock domain. Header `input` identifies the adapter, and `handConvention` explains provenance.

`BirdFlightDriver.UpdateSessionTracking`, `FlightSessionTracker`, runtime effort and offline cadence analysis exclude estimated/low-confidence/recovery motion from measured activity and wingbeat credit while retaining actual flight displacement. Legacy Unknown input retains controller meaning when tracked and not estimated. V5 replay restores recorded provenance; it does not rerun the live continuity filter or enable SDK modes.

Future instrumentation, requiring an explicit later schema revision:

- a separate unfiltered display-root pose;
- independent hand confidence where the API provides it;
- tracking-source age;
- continuity/reacquisition state;
- actual FMM activation/frequency if a supported observation becomes available;
- raw-to-filtered position delta;
- raw-to-filtered orientation delta.

Useful derived analysis:

- direct/inferred/lost fraction by flight phase;
- dropout duration histogram;
- source-transition count;
- direct vs mapped stroke-speed distributions;
- false-force guard activations;
- flap cadence and mechanical work;
- symmetry between hands;
- route completion time;
- tracking quality specifically during takeoff, thermal circling and landing.

## Reuse existing recordings before new hand data exists

The existing controller telemetry is valuable because it contains real comfortable arm trajectories and replay timing.

Further work: build an offline recorded-session fault-injection mode that can:

1. take a recorded controller session;
2. erase one/both hand poses using synthetic side-FoV dropout windows;
3. mark portions inferred;
4. add a reacquisition offset;
5. feed the result through the hand continuity layer;
6. replay the mapped frame into the existing deterministic simulation.

This cannot prove real hand-tracking quality, but it can regression-test the dangerous mathematical boundary before a headset session.

Required fixtures:

- 50/100/200/500 ms single-hand gap during a downstroke;
- both-hand gap while gliding;
- Direct → Inferred → Direct with 5–20 cm pose discontinuity;
- one hand occluded during bank;
- recovery while entering/leaving a thermal;
- loss during flare/landing.

## Acceptance invariants

Automated:

- `MotionEstimated` wing motion produces zero active stroke force;
- an estimated downstroke cannot trigger takeoff from supported rest;
- estimated/recovery motion cannot earn measured activity or wingbeat credit; v4/v5 replay preserves the energy guard;
- a source transition cannot create a higher flap impulse than a continuous trusted trajectory;
- no first trusted frame after reacquisition has a derived velocity spike;
- lost tracking does not change tuck/flare by itself;
- heading remains bounded when one/both hands disappear;
- replay of old direct controller recordings is unchanged when `MotionEstimated=false`;
- telemetry readers retain v1–v4 compatibility while the writer emits v5.

Quest wearer:

- side flaps do not visibly pop more than occasionally;
- player does not need to stare at their hands;
- one normal compact downstroke reliably creates understandable propulsion;
- gliding remains stable through short inferred intervals;
- thermal circling is usable without text coaching;
- landing can be completed hand-only;
- complete Route Home is possible without controllers.

## Remaining agent work order

Adapter, continuity, compact calibration, UGUI interaction and v5 source instrumentation now exist. Continue from the current tree:

1. Re-read `AGENTS.md`, agent current state and this design.
2. Run source/continuity/replay tests and the explicit isolated `HandFlightReview` helper. Its synthetic poses and Button callbacks do not prove device gesture recognition.
3. Build and inspect the Android manifest, then install the source-matched build when authorized. Commit only when requested.
4. Capture real hand sessions, including denied permission, WMM availability, dropout/recovery, calibration, ground controls, rest/settings/results and the first supported takeoff.
5. Complete Route Home and each course with physical hand input; record stereo readability, sustained performance and wearer comfort separately.
6. Compare WMM behavior and decide FMM from measurements; feed marked good/bad sessions into replay and tune.
7. Refine Route Home pacing/polish and run the quality loop with separate visual/player/Quest critics.
8. Update competition documentation with measured final behavior; never upgrade an unverified hand-tracking claim into a fact.

The first hand implementation is successful when it is boringly safe under tracking loss. Fine-grained “bird feel” tuning comes after that boundary is trustworthy.

First Quest hands session findings and the resulting revision: [hand controls telemetry review](../docs/reviews/2026-09-29-hand-controls-telemetry.md). Inferred poses jump a median 25–39 cm at source edges, so they remain motion-estimated; the wing now stays continuous across the handoff instead.
