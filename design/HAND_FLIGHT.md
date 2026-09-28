# Hands-first bird flight

This is the implementation contract for the Meta VR Start 2026 hand-flight work. The existing controller path remains a fallback; the competition path must require no controller.

## Architecture decision

Do **not** rewrite the aerodynamic solver.

The current dependency direction is already correct:

```
device tracking -> IFlightInput -> FlightInputFrame -> BirdFlightController -> presentation
```

`BirdFlightController` consumes tracking-local wing position, orientation and velocity after calibration. Active stroke work is computed from the component of down/back wing velocity against the calibrated wing pressure normal. That means a hand wrist/palm transform can replace a controller transform while preserving the established flight model, species tuning, contact solver, wind, Route Home logic and telemetry.

Implement a new device adapter, provisionally:

```
MetaHandFlightInput : IFlightInput
```

Meta APIs, Interaction SDK types and hand skeleton details stay under `Game/Input/`. The flight solver must remain device-independent.

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
- https://developers.meta.com/horizon/documentation/unity/unity-handtracking-fast-motion-mode/
- https://developers.meta.com/horizon/documentation/unity/unity-handtracking-unextrapolated-poses/

## Wing pose source

Use the OpenXR hand skeleton and derive each wing control pose from a stable wrist/palm basis. Do not hardcode a global palm axis and assume it matches the old controller grip. On accepted calibration, capture the hand's neutral basis exactly as the controller path already captures a neutral grip.

For each hand retain at least:

- predicted/display wrist root position + orientation;
- whether the hand is tracked;
- hand confidence;
- direct camera vs WMM inferred pose source;
- unextrapolated root pose + capture timestamp when available;
- last direct high-confidence sample time;
- continuity state and inferred gap age.

The unextrapolated API is specifically useful here because Meta exposes the real capture timestamp and recommends it for motion analysis, replay and custom prediction. Record it; do not necessarily render from it.

## Tracking-quality state machine

Treat hand data as four semantic states rather than a single `Tracked` bit.

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

The current branch adds `WingInput.MotionEstimated`; `BirdFlightController` refuses active `Stroke`/stroke force when this flag is true while still accepting the tracked pose for the rest of the flight signals.

If later telemetry shows WMM motion is trustworthy enough to support flaps, replace the boolean safety gate with a measured 0..1 stroke-authority signal. Do not guess a fractional value first.

### Lost

No usable direct or inferred pose.

For a short gap, continuity may hold/predict presentation, but predicted motion must be marked estimated. Velocity should decay toward zero. After the bounded grace period, stop moving the wing rather than extrapolating indefinitely.

A lost hand must never cause:
- an impulse;
- an automatic dive/tuck;
- a heading snap;
- a false landing brake;
- a scored flap.

## Reacquisition invariant

**Tracking recovery must not create energy.**

The controller adapter already suppresses velocity on the first sample after `Tracked=false`. Hand tracking needs the stronger version because WMM may keep the hand nominally tracked while the pose source changes.

On any Direct ↔ Inferred or Lost → Direct transition:

1. detect the source edge;
2. reset trusted finite-difference history;
3. crossfade presentation pose over a short measured window;
4. keep `MotionEstimated=true` until at least one stable direct interval establishes a velocity again;
5. only then restore active stroke authority.

No one-frame position delta across a source transition may be fed to the squared stroke-force calculation.

## Compact seated mapping

Competition mode should not require literal full human wingspan. The current bird calibration already separates human neutral pose from species presentation scale, so preserve that concept.

Target:

- hands roughly 0.35–0.55 m from the body centerline at neutral, adjusted to comfort;
- elbows may stay bent;
- hands can sit somewhat forward of the shoulder plane when that improves visibility;
- the authored bird still displays its natural full span;
- the player performs compact but recognizable down/up strokes.

Do not simply multiply all hand velocity by a large constant. That would amplify jitter and tracking jumps. Scale the **pose envelope** into bird space while tuning active-work response from measured sessions.

Keep a Full Wing / Fitness option for the original larger motion, but the competition default must pass the airplane-seat test.

## Flight verbs without buttons

The core airborne verbs should not depend on finger gestures that may be unavailable while hands are lateral.

| Verb | Hands-first target |
| --- | --- |
| Calibrate | comfortable compact spread accepted from hand pose; explicit gaze+pinch confirm if needed |
| Take off / flap | down/back wrist/palm motion |
| Glide | spread and quiet hands |
| Bank | asymmetric hand height plus calibrated wrist roll, retaining current mapping |
| Climb/descend | preserve comfortable head-pitch Beginner mapping initially |
| Tuck/dive | shorten span / draw wings inward; do not require trigger |
| Flare/land | raise and hold spread wings, preserving the current physical brake |
| Pause/settings | bring hands into reliable forward interaction space; gaze+pinch menu |
| Recalibrate | hands-first pause menu plus existing platform recenter lifecycle |
| Character/activity selection | gaze+pinch / Interaction SDK UI |

Do not require pinch recognition during a flap. Pinch is for deliberate menus when the player has brought a hand into a well-tracked interaction zone.

## SDK integration plan

1. Keep Unity OpenXR as the backend.
2. Import a competition-compatible Meta XR Core SDK and Interaction SDK v207+ using Meta's supported package path.
3. Enable OpenXR hand skeleton.
4. Build a minimal hand source in isolation before altering scene/UI flow.
5. Expose WMM state and `PoseSourceInferred`.
6. Add gaze+pinch interaction for existing menu actions.
7. Test WMM via the documented adb override before permanently enabling it in project configuration.
8. Compare default tracking against FMM on real Quest captures; enable FMM only when it improves fast-stroke retention enough to justify jitter.
9. Do not add Meta SDK types to `BirdFlightController`, wind, gameplay or world code.

## Telemetry v4

Do not silently append these signals to schema 3. Version the binary schema/reader contract.

Minimum new per-hand fields:

- predicted/display root position and orientation;
- unextrapolated root position/orientation and sample timestamp/availability;
- direct/inferred/lost source;
- hand confidence;
- tracking-source age;
- continuity/reacquisition state;
- `MotionEstimated`;
- FMM enabled;
- WMM enabled;
- raw-to-filtered position delta;
- raw-to-filtered orientation delta.

Also record a hand-input mode identifier in the header.

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

Build an offline fault-injection mode that can:

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
- a source transition cannot create a higher flap impulse than a continuous trusted trajectory;
- no first trusted frame after reacquisition has a derived velocity spike;
- lost tracking does not change tuck/flare by itself;
- heading remains bounded when one/both hands disappear;
- replay of old direct controller recordings is unchanged when `MotionEstimated=false`;
- telemetry readers retain v1/v2/v3 compatibility after v4 ships.

Quest wearer:

- side flaps do not visibly pop more than occasionally;
- player does not need to stare at their hands;
- one normal compact downstroke reliably creates understandable propulsion;
- gliding remains stable through short inferred intervals;
- thermal circling is usable without text coaching;
- landing can be completed hand-only;
- complete Route Home is possible without controllers.

## Agent work order

An implementation agent with the repository's normal Unity MCP/editor access should work in this order:

1. Re-read `AGENTS.md`, agent current state and this design.
2. Import Meta SDK dependencies in a dedicated commit and prove clean compilation before adding behavior.
3. Add a `MetaHandFlightInput` that produces `FlightInputFrame` without changing the solver.
4. Add source/confidence state and continuity filters; wire `MotionEstimated` conservatively.
5. Add EditMode tests for dropout/source transitions before wearer tuning.
6. Add telemetry v4 and Python decoder compatibility tests.
7. Add hand/gaze UI so every essential action is controller-free.
8. Add compact calibration.
9. Build/install on Quest and capture hand sessions with WMM off/on.
10. Decide FMM from measurements.
11. Feed marked good/bad sessions into replay and tune.
12. Only then alter Route Home pacing/polish.
13. Run the repository quality loop with separate visual/player/Quest critics.
14. Update competition documentation with measured final behavior; never upgrade an unverified hand-tracking claim into a fact.

The first hand implementation is successful when it is boringly safe under tracking loss. Fine-grained “bird feel” tuning comes after that boundary is trustworthy.
