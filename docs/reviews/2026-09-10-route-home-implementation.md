# A Route Home — implementation and verification

The user selected a complete first adventure: core fixes, a story payoff and focused visual polish. This implements that milestone from the [critique and roadmap](2026-09-10-game-critique-and-roadmap.md); it does not complete the entire 24-work-order production plan.

## Shipped behavior in source

- **A Route Home:** find real rising air, gain height by flapping or soaring, collect the living seed through swept physical proximity, cross the stone arch, then actually land on the garden. The garden blooms and remains restored after saving/reloading. Duck, Dragon and Magpie use their existing flight profiles.
- **Reward and rest:** adventure completion is untimed with a fixed reward; Training has a separate soaring-efficiency record. Active flight, perch rest, wing-stroke time and techniques have distinct counters. Movement time is an input/activity measure, not exercise intensity or calories.
- **Explicit preflight:** choose the route and flyer, read the task, then Begin/Resume/Restart. Resume explains that A restarts; right trigger while paused opens help for Recalibrate here.
- **Durable progress:** versioned validated checkpoints, previous-generation backup, legacy record migration and preserved malformed/unsupported raw saves. Stage, rest, pause, focus loss and departure trigger saving. Foraging bests also flush on a bounded cadence. Failed Save + leave keeps the session available. Unsupported/newer saves remain protected.
- **Recovery:** load the checkpoint's logical location, load collision and validate actual support before placing the bird. Missing support falls back to departure; failure remains paused. Recalibrate here preserves position and progress. Recovery resets observation/catch/trick continuity, so relocation cannot fabricate rewards. Menu controls require release before returning to flight.
- **Camera and help:** first-person steady horizon is independent of Beginner/Acrobatic authority; embodied remains the advanced default. Chase placement sweeps against the existing environment. Pausing does not automatically display text. Explicit help temporarily hides instruments and calibration text, and exposes recovery, audio, haptics and guidance settings.
- **Feedback and art:** distinct rising-air availability, actual sustained climb, catch, trick, mode/view and seed cues; spatial restoration sound comes from the crown. Shared-material winged seed, body-attached species-aware carried seed, dormant/bloom crown, optional actual-wind guidance, landmark-preserving island LOD, less crowded sanctuary and three closed cloud families. New runtime cloud meshes are bounded to248vertices; journey meshes to392, with four renderers. Existing authored assets and collision route were retained.

## Validation evidence

Unity6000.6.0f1, Android target. Full233 EditMode and32 PlayMode tests pass;21 Python tests pass. The scene suite includes supported recovery without synthetic completion, actual takeoff afterward, failed-save scene retention, storage isolation, UI ownership, cloud closure and restoration-audio cleanup/pause/mute. These are executed Unity tests, not source scans.

The initial actual-input routes all completed and restored the garden after loading isolated saves: Duck212.80s, Dragon569.01s, Magpie242.82s. The Magpie pilot pushed an arch leg for about55s and accumulated17,083 solver contacts; Dragon circled the seed setup for about398s. Those runs prove reachability, not smooth play or human pacing. The improved editor-only pilot uses a longer aligned approach and records contact severity. Its Magpie run completes202.07s and Dragon500.58s, each with one contact frame at the final landing, no earlier contacts and persistent restoration. Dragon still spends309.61s in the seed stage, a remaining automated-pilot/pacing limitation. The first improved pilot attempt timed out because its departure-height rule kept reacquiring the thermal; the corrected harness latches that completed climb. Original evidence remains available.

Fresh screenshots and runtime reports live under `artifacts/reviews/route-home/round-01`, `round-02` and `round-03`; failed pilot evidence is retained separately. Staged visual fixtures are explicitly labeled and do not prove route progression. The harness checks PlayerPrefs isolation; PlayMode tests use editor-only memory adapters. No save archives are deleted by tests.

Three independent reviews: [visual](2026-09-10-route-home-visual.md), [player](2026-09-10-route-home-player.md), [technical](2026-09-10-route-home-technical.md). Revisions fixed save failure handling, resumed calibration wording, seed acknowledgement, crown attachment, cloud seams, payoff direction and carried-seed visibility. The three-round cap applies: broader lighting/material work remains below the strict all-8 art target. No screenshot score establishes Quest comfort or frame rate.

Fresh preceding-build Quest telemetry confirms the advanced neutral correction is installed, including22.58s of zero control pitch and zero aerodynamic pitch in Magpie. It also retains28–30ms atomic generation maxima and a Dragon timestamp/GPU anomaly; that baseline cannot establish this milestone's device performance. The overall repo check still fails on the24 previously documented TextMesh Pro GUID defects; the new work adds no structural findings. Blender was not changed, so no Blender export validation was required.

## Roadmap status and next gates

| Work orders | Implemented here | Remaining acceptance/work |
| --- | --- | --- |
| W01 | Refreshed post-neutral device evidence; reproducible controller/scene checks | Controlled wearer neutral/HUD/menu acceptance and representative sustained Quest measurements |
| W02/W06 | Untimed adventure, separate efficiency best, active/rest/movement counters | Authored movement/mastery goals and human exertion/recovery pacing |
| W03/W04/W11 | Explicit quiet help, independent view option, chase obstruction checks | Worn readability/comfort and route clearance across real approaches |
| W05 | Validated durable checkpoints, safe resume and bounded foraging flush | OS interruption/force-close/relaunch acceptance on the new device build |
| W07/W08/W09 | First chapter, optional leaf/air cues, persistent visible payoff | HUD-off human route comprehension, payoff noticing and actual fun/retention |
| W10/W16 | Focused kit composition/cloud variation and bounded procedural/spatial cues | Authored hero assets, material/lighting quality, creature/ambience/audio direction |
| W12–W15/W17–W23 | Foundations only where explicitly described above | Companion, authored foraging, maneuver school, comparable records, broader content, profiling/soak and production acceptance |
| W24 | Updated roadmap status, critique synthesis and agent handoff | Keep evidence/status current after wearer testing |

Next useful session: play A Route Home with instruments hidden, test a mid-route pause/save/leave/resume, confirm the carried seed and garden sound are understandable, and compare a comfortable Beginner or steady-horizon run with the desired embodied advanced view. Capture the installed source stamp and report wearer comments alongside telemetry. Do not infer exercise effectiveness from the scripted route duration.

Quest build/install status is recorded in the [session handoff](../agent/session-handoffs/2026-09-10-route-home-implementation.md).
