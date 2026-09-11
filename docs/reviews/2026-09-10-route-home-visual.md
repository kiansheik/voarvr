# A Route Home — independent visual review

Reviewed 2026-09-10 against `docs/agent/quality-loop.md`. Scope: the first adventure's sanctuary composition, distant arch, cloud variety, seed/restoration presentation and explicit pause help. No other critic's conclusions were read. This is a desktop visual assessment, not a Quest acceptance report.

**Final disposition after round 03:** the three observed placement/geometry/visibility defects are resolved in fresh captures. No high/blocker visual finding remains. Stop at the three-round cap; material/lighting production goals and wearer validation remain explicitly deferred. Final reassessment is below; earlier sections preserve the evidence behind each revision.

## Evidence and limits

Inspected the fresh images and inventory in `artifacts/reviews/route-home/round-01/`: `visual-fixture-{garden-dormant,garden-restored,island-approach,split-arch,story-seed,cloud-ascent,flight-menu}.png`, plus `duck-actual-controller-result.png` and its controller-route JSON. World/menu fixtures explicitly stage cameras and presentation; the separate Duck report records actual input-only controller completion. The latter does not establish human noticing, enjoyment or physical effort.

Compared the prior `sky-garden-comfort/round-03/{garden-terrace,island-approach}.png`. Read current `JourneyPresentation`, `SkyArchipelago`, `SkyWeatherPresentation`, `FlightMenu`, `CharacterSelectController` and the capture helper. Subsequently inspected fresh `preflight-new-route.png` and `preflight-resume-route.png` in the same round: both visibly separate flyer choice, route steps and the launch action; resume correctly names paused recalibration and identifies A as restarting. First-person carried-seed visual evidence remains pending at this first assessment. Stereo depth, headset text readability, motion comfort, frame rate and bloom animation timing are unverified.

## Scores

| Assessable dimension | Score / 10 | Rationale |
| --- | --- | --- |
| Approach composition | 8 | Removing the dense tree wall opens the arch-to-garden sightline. The tower forms a clear secondary destination. |
| Landmark silhouette | 8 | The arch now survives in distant island silhouettes, with a readable opening against the sky. Several islands still repeat the same motif. |
| Story object/state readability | 7 | Coral seed and broad restored crown visibly differ from the muted environment and dormant bud. Their connection to the trunk and attention on arrival need work. |
| Palette/material coherence | 7 | Jade, pale stone and coral form a coherent restrained palette. Smooth balloon-like clouds and uniformly flat terrain/stone remain visibly at different finish levels. |
| Lighting/depth | 6 | Sky gradients and distance tint separate layers, but close garden surfaces have little contact grounding or material light response. This remains prototype art. |
| Pause-menu hierarchy | 8 | Clear title, grouped controls, contrasting selected row and consistent spacing. No clipped labels in the captured state. Headset readability is unverified. |
| Preflight hierarchy | 8 | The selected flyer and route remain visible alongside an explicit launch action. Resume/restart state is distinct and the calibration briefing changes appropriately; small controls still need headset inspection. |
| Visible artifact control | 7 | Two small, concrete geometry/placement defects remain below. |

These grades describe the inspected output, not merely the size of the improvement. The strict all-8 visual gate is not met.

## Findings and priority revisions

1. **Medium — objective defect: crown visibly detaches from its supporting tree.** Both matched garden images show a sky gap and lateral offset between the trunk tip and bud/bloom base. `JourneyPresentation.Present` positions the crown with an independent `(1.5,18,-.7)` offset from the plant origin. This weakens the central restoration image. **Fix:** derive or explicitly author a common trunk-tip anchor, attach both meshes there and compare fresh dormant/restored close views; preserve the open landing space.

2. **Medium — objective defect: open cloud seam.** The large right cloud in `visual-fixture-cloud-ascent.png` has a bright vertical slit. `SkyWeatherPresentation.CloudMesh` closes longitude geometrically at `2π`, but radius jitter uses `sin(lon * 2.1 + puff)`, making longitude 12 differ from longitude 0. **Fix:** wrap the longitude sample or use an integer harmonic of the angular coordinate; verify matching seam positions and a fresh close cloud capture.

3. **Medium — unverified risk: the restoration can occur outside the player's view.** The actual Duck completion screenshot faces the tower and shows no restored crown, although its route report records restoration after reload. Matched art fixtures prove a visible state difference when aimed at it, not that a player notices it on landing. **Fix:** add or validate a brief, local completion cue that directs attention toward the tree without moving the player's camera, such as spatial sound or a restrained leaf motion; capture the actual arrival and subsequent look. Avoid making ongoing HUD text mandatory.

## Deferred art work and disposition

The remaining lighting/material score is a broader production-finish limitation, not justification for an unrelated renderer or asset-pack change in this milestone. A future bounded art pass should establish contact grounding and a common surface-lighting treatment on one garden before expanding it. Variation is now visible, but the cloud family and repeated arch islands still read as a small reusable kit.

No blocker/high visual defect was found in the inspected evidence. A small revision round is useful for the two objective defects and the completion attention risk. Do not call the all-8 art gate or Quest hardware gate passed after those changes without fresh evidence.

## Round 02 reassessment

Inspected fresh `round-02/visual-fixture-{garden-dormant,garden-restored,cloud-ascent}.png` and the first-person inventory plus all three species' advanced embodied/stabilized captures and Duck/Dragon Beginner captures. These remain explicitly staged views with no tracked head offset. No other critic's report was consulted.

- **Resolved:** both crown states now visibly meet the trunk. The shared transformed trunk-tip anchor fixes the composition defect without closing the landing area.
- **Resolved:** the visible slit on the close cloud is gone. Current source uses periodic angular jitter and shared seam/pole topology; fresh output supports closure.
- **New medium objective visibility defect:** the carried seed is absent from all six advanced forward-view captures while `SeedCollected=True`; the visible three small distant leaves are guidance, not the carried object. Duck Beginner shows the carrier low left. A single body offset `(-.34,-.1,.5)` is behind Dragon's authored advanced eye at `z=1.32`, and outside the smaller birds' captured forward frusta. This removes a useful HUD-free confirmation that the cargo is still present. Use a species-aware body socket in front of the authored eye/beak, then verify forward and downward views without attaching the object to the camera or obstructing the horizon.
- **Still unverified:** whether a wearer notices the bloom after landing. No fresh actual arrival/attention evidence has replaced the initial completion view.

Visible artifact control improves to **8/10** for the corrected world geometry. Forward-view carried-state readability is **6/10** in the newly available evidence. Other round-01 scores retain their stated scope; the material/lighting limitations and all-8 gate remain open. One final bounded carry/arrival cue revision is useful; wider art redesign is deferred.

## Round 03 final reassessment

Independently inspected all nine fresh `round-03/visual-fixture-{duck,dragon,magpie}-carried-seed-{beginner,advanced-embodied,advanced-stabilized}.png` views and their fixture inventory. Also inspected fresh matched dormant/restored garden, cloud-ascent and island-approach images. Read the revised actor-relative carry position and `FlightFeedback.StoryCue` source. No other critic's conclusions were read.

The carried seed is now visible in every tested species/view combination. It remains distinct from the distant guidance leaves and does not cover the arch's flight opening or central navigation view in these poses. Advanced embodied views place it consistently to the lower left; stabilized Dragon moves it higher left as the actor banks, which is consistent with keeping the object attached to the actor. Source derives the socket from the authored eye anchor and applies the bird rotation, without attaching the object to the camera. **The medium carried-state visibility defect is resolved in the tested views.** Forward-view carried-state readability improves to **8/10**; stereo proximity and visibility during unrestricted head/body motion remain unverified.

Fresh world images retain the attached bud/bloom, closed cloud seam and open approach composition. Story object/state visual readability is now **8/10** and visible artifact control remains **8/10** within this inspected scope. Approach composition and landmark silhouette remain **8/10**. Palette/material coherence remains **7/10** and lighting/depth remains **6/10**: these are unchanged production-finish limitations, not resolved by cargo placement. The previously inspected preflight/pause hierarchy scores retain their original evidence scope.

The new restoration sound is emitted from an AudioSource on the crown with spatial blend enabled. This is an appropriate bounded implementation response to the arrival-attention risk while preserving player camera control. Source inspection does not establish audible localization, acceptable level, accessibility with sound disabled or whether a wearer actually looks toward the payoff. That **medium unverified player-attention risk** remains a headset playtest question; it is not a confirmed remaining visual defect.

**Stop at the three-round cap.** No additional runtime revision is requested from this visual review. There is no high/blocker visual finding in the assessed milestone. The strict all-8 art gate remains unmet because lighting/material finish is deferred to a future bounded garden art pass. Quest text legibility, nearby cargo comfort, full-motion occlusion, spatial sound and perceived arrival payoff require wearer evidence; screenshots cannot close those gates.
