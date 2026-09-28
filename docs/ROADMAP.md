# Roadmap

First implementation milestone: [A Route Home and remaining acceptance gates](reviews/2026-09-10-route-home-implementation.md). The untimed chapter, durable recovery and focused presentation changes are implemented; broader content/art and wearer acceptance remain open.

For the current proposed production sequence, read the [2026-09-10 game critique and 24 work orders](reviews/2026-09-10-game-critique-and-roadmap.md). It prioritizes current-build acceptance, reward/rest alignment, durable progress and one polished story route. These are recommendations; implementation status belongs in [current state](agent/current-state.md). The original foundation milestones below are historical planning context, not a current completion checklist.

## Current local gameplay pass (2026-09-12)

The [Quest playtest follow-up](reviews/2026-09-12-quest-playtest-next-round.md) is implemented in source and covered by editor-side tests/evidence. A fresh Android build, install and worn Quest acceptance pass are still pending; nothing in this table claims headset comfort, performance or usability validation.

| Area | Implemented locally | Remaining acceptance gate |
| --- | --- | --- |
| First-flight comfort and controls | Diagram-led A calibration/recalibration; Left Menu session menu; independent right-stick yaw while supported; left-stick walking retained. | Verify pose readability, physical steering comfort and Meta recenter behavior on Quest. |
| Game information and catching | Persistent goal/course ribbon survives optional-gauge hiding; Sun/Moon/Ember/Crown moths have distinct rarity, behavior and values 10/20/35/60; counts, value and combo are visible. | Tune legibility, catch radii, rarity and reward cadence from headset play. |
| Sessions and players | Local player profiles, recoverable session drafts/history, explicit finish/results and personal-record comparisons for activity/rest, estimated wingbeats, flight/walking distance, gross climb/descent, top speeds, catches, tricks, contacts and landings. There is no exhaustion meter. | Confirm metric usefulness and classification against real tracked play; do not present medical/calorie accuracy. |
| Skill courses | Five data-driven 30-second courses, 3-2-1 start, ordered gates/corridors/altitude/trick/moth/landing tasks, local personal bests/leaderboards and a required six-second rest before retry. | Complete every route on Quest, tune difficulty/timing and verify ranked invalidation/fairness at device frame rates. |
| World response and flyers | Silhouette-following compound colliders for landmarks/rocks/tree wood, permeable foliage disturbance, directional bird/camera bump, spatial surface sound, side-weighted haptics and three noncombat moving rivals outside ranked courses. | Validate contact alignment, disorientation limits, audio/haptic strength and performance on hardware. Actual enemies remain future content. |

| Milestone | Outcome / acceptance direction |
| --- | --- |
| M0 - repository/tooling foundation | Current scaffold; complete first Unity import, tests, Blender roundtrip and Quest smoke run before marking verified. |
| M1 - satisfying bird flight in a small test area | Tune a small deterministic motion model with synthetic regression cases and clear movement feedback. |
| M2 - VR physical flapping and gliding | Calibrate arm poses/velocities, tracking loss, comfort, head pose and controller mappings on Quest. |
| M3 - perching/landing | Collision/approach/flare/landing transitions on simple perches. |
| M4 - movement records/recovery pacing | Keep the locally implemented session records and short-course rest gates; deliberately avoid an exhaustion meter and make no medical or calorie accuracy claims. |
| M5 - collectible skill loops | Tune the locally implemented typed moth/value/combo loop and course-specific collection tasks through repeat Quest play. |
| M6 - other flyers/enemies | Tune the locally implemented noncombat rivals first; design predators or other enemies only after collision comfort and intent are clear. |
| M7 - environment/world expansion | Expand only once flight and hardware performance hold up. |
| M8 - non-VR gamepad mode | Complete controls, camera and UX; the M0 adapter is plumbing only. |
| M9 - optimization/portability | Profile representative content, establish budgets, separate platform adaptations. |
| M10 - investigate Switch port | Evaluate developer access, Unity support, hardware and actual port scope. |

Species/size variation belongs after the core flight feel is understood. Empty source folders are organizational slots, not commitments to implement every system now.
