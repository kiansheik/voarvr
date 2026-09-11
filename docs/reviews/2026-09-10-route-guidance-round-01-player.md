# Route Home guidance — independent player review, round 1

The offscreen bearing addresses the recorded loss of direction, but the new forward route geometry currently obstructs the destination. Revise before treating HUD-off navigation as ready for another wearer trial.

Reviewed the actual Quest baseline summary and current `JourneyPresentation`, `RouteBearing`, relevant tests and `RouteHomeReview` fixture contract. Independently inspected the round-1 runtime JSON and 18 representative screenshots before accessing any other critic's conclusions. Evidence: `artifacts/reviews/route-guidance/round-01/`; filenames below omit the `runtime-guidance-` prefix. No runtime code or live editor mutations performed.

The baseline is a clean 37,899-frame Magpie recording with no dropped frames. Both seed-stage entries occur with the seed behind travel direction (about 148° and 126°), at 1,138 m and 370 m. No pickup occurred. This supports prioritizing reacquisition; it does not measure new-version comprehension.

| Finding | Severity / classification | Evidence and player impact | Specific revision |
| --- | --- | --- | --- |
| Forward trail hides the thing to follow | **High — objective defect** | `duck-stage-0-discover-lift.png`, `duck-stage-2-find-seed.png` and `duck-stage-3-arch-with-seed.png`: crossed solid chevrons become a black/gold plus sign roughly 400–510 px wide in a 1,440 px frame. The seed and arch opening sit behind it. `magpie-pickup-before.png` is substantially more obstructed at close range. The apparent cross also fails to communicate an arrow's forward direction. | Replace the center-crossing fins with small, separated cues below/beside the route. Preserve an empty aperture and reduce/fade nearby trail geometry before entering the pickup/landing area. Re-capture the same stages and approach distances. |
| Pickup cue stays large while moving close | **Medium — objective defect; comfort consequence unverified** | `magpie-pickup-transfer-start.png` at simulation 104.1 and `...-midpoint.png` at 104.5 show the seed/halo spanning roughly 1,000–1,200 px across the 1,440 px image. JSON confirms this is a real swept pickup followed by active transfer. A conspicuous confirmation is useful, but much of the next-route view remains covered halfway through the animation. | Contract the seed substantially before bringing it near the creature; retain a short, legible transfer and the distinct audio/haptic cue. Check intermediate frames and moving pickup in the headset. |

The two `magpie-quest-position-seed-behind-*` images show a readable gold back-left bearing over both sky and dark terrain. The above-column Soar JSON retains real navigation; StillAir deliberately suppresses unsupported lift cues. All 42 states record `FlightTextVisible=false`.

Settled possession is visible in the inspected neutral Duck, Magpie and Dragon views and differs from the surrounding scenery. At injected head yaw 60°, upward look 30° and translation (0.18, 0.08, 0.14) m, the carried object stays with the body and leaves the view, while the offscreen route bearing remains available. This is consistent with embodiment; it does **not** establish that a wearer will remember or know where to inspect possession. Do not solve that uncertainty by forcing the object to follow the head.

| Assessable dimension | Score / 10 | Rationale |
| --- | --- | --- |
| Offscreen target reacquisition | 8 | Correct and legible bearing in the two recorded failure positions and inspected turned-head views. |
| Forward spatial/action readability | 4 | Destination semantics improve, but the central cross obstructs them. |
| Pickup/ownership visual feedback | 7 | Transfer and persistent object are explicit; transfer occlusion and body-relative inspection need refinement/validation. |
| Quiet-view preference compliance | 10 | No flight text restored; direction is conveyed without words. |
| Camera/head-pose behavior in inspected fixtures | 8 | Real camera code accepts the injected poses; the world and carried object retain spatial relationships. |

Motion feel, stereo readability, perceived size, audio/haptic balance, comfort, fun and sustained exercise are **unverified**. These are staged runtime screenshots, not a new wearer session or input-only playthrough. Make the two bounded revisions above, then use fresh evidence for round 2. The next wearer check should establish whether the player can reacquire the seed after a wrong turn, cross its aperture and identify possession without enabling text or receiving verbal hints.
