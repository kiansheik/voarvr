# Route Home guidance — independent visual review, round 1

Brief: keep flight text hidden, make the route discoverable through world markers and make seed possession visually clear. Inspected current `JourneyPresentation`, `RouteBearing`, the capture helper, exact runtime JSON and 24 representative images from the 42-frame set in `artifacts/reviews/route-guidance/round-01/`. No other critic's conclusions were read; no source or live Editor was changed.

Evidence uses real presentation `Tick`, wind sampling and `FlightCamera.ApplyPose`, with deliberately staged checkpoints/positions and injected head poses. Pickup crosses the actual challenge pickup radius. These are labeled runtime visual fixtures, not a human playthrough.

## Findings

| Severity / classification | Concrete evidence and impact | Action |
| --- | --- | --- |
| **High — objective defect** | The three crossed path chevrons collapse into a large black/gold plus sign along the approach axis. `runtime-guidance-duck-stage-0-discover-lift.png` covers about 500 horizontal pixels of the 1440-pixel frame; stage 2 covers about 450 pixels directly over the seed, and `duck-pickup-before` about 780 pixels. The route aid obscures the exact object/aperture the player should approach. Stage 3 similarly blocks the arch opening; in stage 4 it becomes an upright black/gold slab ahead of landing. | Replace the crossed center geometry with thinner open or lateral directional marks. Keep a clear central opening and put the short trail below/beside the target sightline. Reduce its angular footprint and withdraw the trail earlier near arrival while retaining the actual destination marker. Recapture the same five stages and pickup approach. |
| **Medium — objective defect** | `duck-pickup-transfer-start` and `duck-pickup-transfer-midpoint` show the seed taking roughly 1,000 pixels or more across the frame; the settled seed is about 100 pixels. Translating and scaling with the same interpolation preserves a very large apparent object until late in the 0.9-second transfer. The visual confirmation therefore hides much of the flight scene during acquisition. | Shrink the carried object earlier/faster than its approach, or otherwise bound its apparent size during transfer. Retain recognizable seed-to-carrier continuity and the existing sound/haptic confirmation; capture start, quarter, midpoint and settled states. |
| **Medium — unverified risk** | The carried seed is visible in neutral first-person Duck, Magpie and Dragon captures. The Dragon advanced-stabilized image clips part of its enlarged seed at the left edge, while shifted-head images naturally look away from the body and cannot establish whether a player can recheck possession comfortably. Third-person captures look 60° away and show neither bird nor seed. | After reducing route obstruction, add a normal forward third-person carry capture and a small head turn back toward the carried object for each species. Use wearer testing to establish whether body-attached ownership is reliably readable; do not make the seed follow the head merely to keep it always visible. |

The small offscreen bearing is legible against both sky and dark island geometry in the two recorded-position Magpie fixtures. It supplies a clear left/down turn cue without text. The above-column fixture retains a downward cue; StillAir removes the lift cue. The seed's gold ring and coral/ivory silhouette are visibly distinct when unobstructed. Those changes are useful and should survive the revision.

## Scores

| Dimension | Score | Reason |
| --- | --- | --- |
| Composition / scene hierarchy | 3/10 | Path symbols dominate and conceal the destination. |
| Destination silhouette / readability | 5/10 | Rings, brackets and landing circle have differentiated silhouettes, but the foreground trail masks them. |
| Offscreen bearing readability | 8/10 | Compact, contrasting arrow reads on sky and island surfaces. |
| Seed ownership, assessed static views | 7/10 | Distinct settled object across species; transient scale and third-person confirmation need attention. |
| Scale / visual artifacts | 4/10 | Oversized center crosses and pickup transfer overwhelm the frame. |
| Material / stylization coherence | 6/10 | Opaque faceted gold/charcoal is consistent internally, but its heavy outline is harsher than the soft world palette. This is a preference, secondary to readability. |
| Stereo readability, motion comfort, human comprehension | Unverified | Monocular staged images and injected head poses cannot establish these. |

Priorities are the three actions above, with route obstruction first. Another round is necessary: a confirmed high defect remains and the visible hierarchy is below the quality gate. Do not interpret these scores as Quest performance, wearer comfort or fun ratings.
