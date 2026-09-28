# Meta VR Start 2026 hands-first flight

## Goal

Position VoarVR for the 2026 Meta VR Start Developer Competition and leave an implementation-ready plan for a Unity/MCP agent. The selected product direction is not broad feature expansion: convert the existing controller-based bird flight into a polished, end-to-end hands-first, compact seated A Route Home experience whose first meaningful payoff occurs within roughly five minutes.

## Competition decision

- Track: **Gaming**
- Division: **Adapted / Significantly Updated Experience**
- Primary award path: Best Adapted / Significantly Updated Gaming Experience
- Natural secondary positioning: Best First Five Minutes, Boldest Original Concept, Best Reason to Come Back
- Deadline: November 18, 2026 at 12:00 PM PT
- Required judge path: playable without ever pairing a controller

This repository predates September 24, so New Experience is not the correct division. The rules explicitly cite implementing hand interactions as a significant-update example.

## Files inspected

`AGENTS.md`; agent index/current-state/repo-map/open-questions/quality-loop; README; package manifest; `design/INPUT.md`, `design/FLIGHT.md`; `IFlightInput`, `FlightInputFrame`, `XRFlightInput`; `BirdFlightController`, `BirdFlightDriver`; Duck flight tests; telemetry design/runtime/replay files; Route Home implementation/current validation history.

External research used the current Devpost overview/rules/judging/special-awards pages and current Meta hand-tracking documentation for hand modes, Wide Motion Mode, Fast Motion Mode and unextrapolated hand poses.

## Files changed

- `docs/competition/meta-vr-start-2026.md`: submission classification, narrow product slice, rubric mapping, schedule, definition of done and submission narrative.
- `design/HAND_FLIGHT.md`: device architecture, WMM/FMM strategy, direct/inferred/lost semantics, compact mapping, telemetry v4, replay fault injection and ordered agent work.
- `unity/Assets/Game/Input/FlightInputFrame.cs`: added `WingInput.MotionEstimated`.
- `unity/Assets/Game/Flight/BirdFlightController.cs`: inferred/predicted motion cannot produce active stroke power/force.
- `unity/Assets/Game/Tests/EditMode/HandTrackingSafetyTests.cs` (+ meta): regression proving an estimated 2.5m/s downstroke produces zero active stroke force while the identical direct downstroke still does.
- agent current state/log and this handoff.

No Meta SDK package or authored scene/prefab change is included in this planning/safety PR.

## Key source finding

The hand conversion is substantially smaller than a flight rewrite. `BirdFlightController` already consumes device-neutral `FlightInputFrame` data through `IFlightInput`. Stroke work depends on torso-relative wing velocity projected against the calibrated wing normal. A hand wrist/palm transform can therefore replace the controller transform while retaining the flight solver, species profiles, world, thermals, contact, Route Home and most presentation code.

The difficult boundary is tracking quality: hands are often lateral, can leave direct camera coverage, and reacquisition can produce a pose discontinuity that a finite difference would misread as a high velocity. Because stroke force is quadratic in trusted stroke speed, that discontinuity must never enter active work.

## Hand-tracking decision

Use OpenXR + Meta XR Core/Interaction SDK v207+ rather than replacing the backend.

Evaluate:

- OpenXR hand skeleton for palm/wrist basis.
- Wide Motion Mode for lateral/out-of-FoV wing poses.
- `OVRHand.PoseSourceInferred` to distinguish camera-based and WMM-inferred poses.
- unextrapolated hand state + capture timestamp for telemetry/replay/custom smoothing.
- default 30Hz first; Fast Motion Mode 60Hz only if measured fast-stroke loss makes it worthwhile.
- gaze + pinch for menu/UI actions when hands are deliberately brought into the reliable forward interaction zone.

Initial safety policy: direct high-confidence motion may flap; inferred/predicted motion may maintain pose/steering but cannot add active stroke energy. Telemetry can later justify a fractional authority model.

## Validation performed

Source-level review only. The two solver guards are intentionally backward-compatible because existing providers leave `MotionEstimated=false`.

No Unity editor/MCP instance was available through this GitHub-only work, so the new test has **not** been executed and the branch has **not** been compiled, built or installed. Do not report the hand safety regression as passing until a real Unity Test Runner run completes.

## Remaining implementation

1. Import compatible Meta XR Core + Interaction SDK v207+ and prove clean compile on existing Unity/OpenXR project.
2. Add `MetaHandFlightInput : IFlightInput`; do not leak Meta SDK types into the solver.
3. Add tracking-source/confidence state and continuity filtering, including explicit Direct ↔ Inferred and Lost → Direct derivative resets/crossfade.
4. Add EditMode source-transition/dropout tests.
5. Add telemetry schema v4 and backwards-compatible Python reader changes.
6. Add synthetic dropout/source-transition fault injection over existing recordings.
7. Add complete gaze/pinch menu path and compact seated calibration.
8. Build to Quest; capture WMM off/on and default/FMM comparisons.
9. Tune from actual marked recordings and deterministic replay.
10. Tighten Route Home to the first-five-minute competition slice.
11. Run the normal independent visual/player/Quest quality loop.
12. Freeze submission candidate, build Competition release channel, video and adapted-experience changelog.

## Suggested next prompt

> Continue the Meta VR Start 2026 hand-flight branch. Read `AGENTS.md`, `docs/agent/current-state.md`, `docs/competition/meta-vr-start-2026.md` and `design/HAND_FLIGHT.md` first. Use the normal Unity MCP/editor workflow. Begin by importing the minimum compatible Meta XR Core + Interaction SDK v207+ dependencies while preserving OpenXR; compile and run the existing suites before implementing behavior. Then build `MetaHandFlightInput : IFlightInput` around wrist/palm hand poses, hand confidence and WMM `PoseSourceInferred`. Preserve the invariant that inferred/predicted/reacquisition motion cannot inject active flap energy. Add deterministic source-transition/dropout tests before Quest tuning. Do not change Route Home pacing or add new content until a complete controller-free launch/glide/flap/bank/pause/recalibrate/land path works and is recorded on Quest. Update the hand design and agent handoff with exact SDK versions, tests, device evidence and remaining gaps.
