# Route Home guidance — independent Quest technical review, round 1

Scope: the current `JourneyPresentation`, `RouteBearing`, driver binding, `JourneyWorldTests`, `RouteBearingTests`, existing camera/world-coordinate code and shared Skyward shader. The user requested hidden text, stronger world markers and pickup cues. No flight-physics change is assessed here. This review read source independently before other critics' conclusions and did not mutate Unity.

At initial inspection, `artifacts/reviews/route-guidance/` contained no capture or test-result files. The findings below are source evidence, not successful compilation, wearer observation or measured Quest performance. Fresh runtime evidence must be attached before closing the loop.

## Findings

| Severity / classification | Evidence and impact | Action / current status |
| --- | --- | --- |
| Medium — objective pose-order defect, revised during review | Initially `RouteBearing` positioned its view-relative 4 m mesh only in `LateUpdate`; `FlightCamera.ApplyPose` also runs in `Application.onBeforeRender`. A newer final camera pose could leave the cue at the previous pose within the same rendered frame. | Revised source now registers `ApplyFinalPose` at `BeforeRenderOrder(100)`, after the camera's default order, and keeps sound scheduling in the ordinary update. Callback removal is present. Add runtime evidence with a changed head pose between these phases; worn rapid-turn stability remains unverified. |
| Medium — objective lifecycle gap | `JourneyPresentation` owns a separate world root and exposes `NavigationAvailable`, but initially has neither `OnDisable` cleanup nor an enabled-state check in `Tick`/`Present`. Disabling only that component can leave its last marker set and a live bearing visible. Scene destruction does destroy the root and meshes. | Hide owned presentation and clear navigation when disabled; verify disable/re-enable and destruction independently. Parent has accepted this revision. |
| Medium — unverified runtime coverage | Projection unit tests cover behind/above/below targets and a translated coordinate fixture. World tests exercise real `Tick` for lift and StillAir, but the inspected test set did not yet exercise the configured bearing through focus, pause, menu, tracking loss, disable, rebase and destruction. Pure projection assertions cannot establish these component connections or stereo visibility. | Add a scene-level lifecycle test and fresh final-pose captures. Run actual Unity tests and check the Console before building. |

## Technical assessment

| Dimension | Score | Evidence / limit |
| --- | --- | --- |
| Bounded geometry and ownership | 8/10 | Five allocated chapter renderers plus one bearing renderer; shared seed geometry; chapter mesh generators are bounded at 480 vertices per mesh and run on configuration. Scene/component destruction releases owned meshes, audio clip and world roots. Disable gap listed above. |
| Material and shader suitability | 8/10 | Existing opaque, single-pass Skyward vertex-color material, no new textures or transparent layers, shadows disabled on cues, existing stereo macros present. This is a source assessment; Android rendering has not been observed for this revision. |
| Logical-coordinate and objective correctness | 8/10 | World marker derives from the challenge's real target; lift samples actual wind, StillAir suppresses lift advertising, pickup aperture uses the real seed radius. Presentation rebases target positions and pickup interpolation state. Bearing reads the marker's current local-world position. |
| Quiet-view contract and subsystem scope | 9/10 | No new text object or HUD visibility mutation. No controller or flight-force change in the guidance implementation. Sound remains preference-gated and bearing visibility checks menu/pause/tracking. |
| Runtime validation coverage | 6/10 at initial inspection | Focused tests exist, but new bearing lifecycle tests, current compilation/results and captures were not yet available. Rescore only from fresh evidence. |
| Sustained Quest frame delivery, stereo comfort and audio quality | Unverified | No device profiler, worn motion or current APK evidence was available to this critic. Screenshot scores cannot establish these outcomes. |

The extra lift search is bounded to seven real wind samples per update; it is not a new physics simulation. No claim of zero allocations, draw-call totals or acceptable Quest frame cost follows from this source inspection.

Priority revisions are limited to final-pose synchronization (source revision inspected), explicit presentation disable handling, and runtime lifecycle/evidence coverage. No blocker or high source defect was established. A short fresh-evidence reassessment is useful; extra asset or rendering systems are not justified by these findings. The repository's 24 known unused TMP GUID findings remain outside this change.
