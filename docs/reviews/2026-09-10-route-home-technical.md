# A Route Home — independent Quest technical review

Scope: the first adventure, interrupted-session recovery, explicit pause help, separate view comfort and bounded world polish. Independently reassessed through round03 on 2026-09-10 local time; no live Editor mutation and no other critic's conclusions consulted. The scoped implementation passes this technical desktop review. Current-build Quest performance and wearer acceptance remain unverified.

## Evidence and limits

Inspected the current runtime changes in `BirdFlightDriver`, `BirdFlightController`, `FlightCamera`, `FlightActionGate`, `FlightFeedback`, `FlightMenu`, `FlightPreferences`, `FlightChallenge`, `FlightJourneyStore`, `ExpeditionDirector`, `SkyForaging`, `JourneyPresentation`, `SkyArchipelago`, `SkyWeatherPresentation` and `WorldStreamer`, plus the new tests and explicit `RouteHomeReview` harness.

Independently parsed `artifacts/reviews/route-home/round-01/editmode.xml` and `playmode.xml`: **233/233 EditMode and 32/32 PlayMode tests pass**, with no failures, inconclusive tests or skips. Parsed round03 `playmode-feedback.xml`: **32/32 pass** after the restoration sound change, including its amended source-reuse, pause, mute and destruction assertions. This is actual Unity executable evidence; it is not Quest testing. The supported-recovery test uses an actual native test perch above the garden, deliberately isolating the recovery contract from decorative geometry.

Read all three round01 controller-route JSON/logs: each starts at the normal logical spawn, uses semantic input through the real controller/driver with streamed native collision, physically lands, saves restoration and reloads it from isolated storage. Duck completes in 212.80 s with four contacts; Magpie in 242.82 s with 17,083 contacts; Dragon in 569.01 s with one contact. **The original Magpie run is not a clean traversal**: its pilot pushes against the arch for roughly 55 s. Dragon spends 398.12 s between seed-stage entry and pickup, so that original run cannot establish good seed-approach pacing. These reports prove eventual completion, not a pleasant human route or sustained exercise.

Round03 `magpie-controller-route-v2.json` provides a better controlled traversal after a harness-only approach revision: **202.07 s, one contact frame and one physical landing**, maximum reported impact 3.238 m/s, no contacts in stages 0–3, restoration after reload and unchanged PlayerPrefs. `dragon-controller-route-v2.json` completes in **500.58 s**, likewise with one contact frame/landing, maximum impact 2.806 m/s and no contacts before the landing stage. Its seed stage still lasts **309.61 s**, so the revised pilot does not close the seed-approach pacing question. Both saves reload the restored garden without changing PlayerPrefs. The original adverse records remain available. Runtime flight authority was not retuned to make the pilots pass.

Inspected fresh round03 cloud-ascent, restored-garden, Dragon stabilized first-person carry and flight-menu images, plus the fixture logs. Cloud seams are welded in source; four-puff families now have 248 vertices, below the former 364. The crown uses the transformed authored trunk cap. Carry position accounts for each species' authored eye location and remains attached to the bird. The current menu renders without missing-shader magenta in the inspected capture. These staged images establish appearance at selected poses, not binocular comfort or tracking behavior. Fixture terrain totals remain approximately 512k–552k vertices across 25 chunks; that is not a count of visible GPU vertices or proof of device budget compliance.

Read `artifacts/telemetry/route-home-baseline-2026-09-10/baseline-report.md`: two actual Quest 3 captures match the installed advanced-neutral source stamp, close cleanly and report no writer drops. They predate this adventure. Magpie reaches the old arch stage without completion. Median render interval is approximately 13.888 ms; generation maxima are 28.041/30.038 ms. The Dragon timestamp gap and invalid extreme GPU sample prevent treating the entire capture as clean continuous timing evidence. No current-adventure APK, sustained device overhead or wearer acceptance is established by this baseline.

The current repository check still reports the documented unrelated TMP unresolved GUIDs. The new menu uses the built-in Canvas/Image/Text path; it does not add a dependency on those TMP examples.

## Findings

| Severity / classification | Evidence and impact | Action |
| --- | --- | --- |
| Medium / unverified risk | The cooperative streaming budget already admits 28–30 ms atomic generation samples on the baseline Quest. The new journey adds little geometry, but saved-perch recovery can regenerate destination terrain and enable island collision. Source bounds alone cannot establish absence of a visible device hitch. | Measure a current-build route, cold departure and remote saved-perch resume on Quest. Correlate generation/collision work with valid frame timing; keep recovery paused until support is ready. Do not call the baseline a performance pass for this build. |
| Medium / unverified risk | The original Magpie pilot has prolonged arch contact and the original Dragon pilot circles the seed for 398 s. Magpie v2 completes without that contact episode, supporting a pilot-path explanation; Dragon v2 still spends 310 s approaching the seed. Neither establishes that an uncoached wearer will find the approach legible. Existing Quest baseline contacts also include clustered small impulses. | Preserve adverse results, report contacts as frames/episodes as well as solver counts, and test an uncoached wearer route. Do not present raw contact totals as forceful-crash counts or scripted seconds as human pacing. |
| Medium / objective defect, resolved | Initially `ReturnToCharacterSelect` discarded the return values of `SaveCheckpoint` and `SaveBest` before loading the menu. A writable-storage failure could silently lose the current checkpoint after choosing “Save + leave.” The revised method remains paused, retains the scene and displays a retry message. `SaveAndLeaveFailureKeepsTheSessionAndEarnedCheckpoint` passes in the actual PlayMode XML. | Retain the fix and failure regression. The test proves failed-flush retention; it does not simulate a later successful menu retry. Intentionally read-only sessions retain their ability to leave. |

No remaining source-confirmed blocker or high issue was found in the scoped implementation.

## Assessable scores

| Dimension | Score | Reason |
| --- | --- | --- |
| Runtime geometry/material discipline | 8/10 | Four bounded opaque chapter renderers share the existing kit material. Seed/bud/bloom/guide meshes stay below 400 vertices each. No new story collider, transparent particle volume or texture set. |
| Distance representation and bounded world ownership | 8/10 | Nine pooled islands reuse four near/far templates; arch and tower survive at distance. An 80 m hysteresis band avoids boundary oscillation. Sixteen cloud slots reuse three geometry families; scene-owned meshes/materials and the standalone chapter root have cleanup. |
| Save compatibility and recovery architecture | 8/10 | Explicit resume, schema/content validation, legacy separation, monotonic restoration, preserved corrupt data and an older valid generation are backed by passing tests. Actual loaded support is checked before recovery; observation/catch history is invalidated. Save-and-leave failure retains the current scene. |
| Feedback/UI resource ownership | 8/10 | Explicit help has one reusable Canvas with cleanup. Three bird-owned audio sources separate guidance/events/breeze; at most one additional world source lives on the crown for restoration. Generated clips and all four sources have cleanup; the new lifecycle test passes. Audio/haptic preferences and pause/focus/tracking gates are explicit. |
| Executable correctness within tested scope | 8/10 | 233 Edit/32 Play pass; physical route completion and isolated restoration reload are observed for all species. Revised Magpie approach removes the prolonged-contact pilot failure. Synthetic input and test perches limit generalization to a human session. |
| Sustained standalone performance, audio quality and worn comfort | Unverified | No current-adventure Quest playthrough supplied; source and screenshots cannot grade these. |

## Remaining priorities and stop decision

1. Capture a representative current-build Quest timing run, especially cold generation and remote-perch recovery, using valid timing samples and renderer/physics measurements.
2. Observe an uncoached complete wearer route and interrupted resume. Verify seed/arch comprehension, near-eye carry visibility during real head motion and the audibility/localization of the restoration cue.
3. Keep clean and adverse scripted results distinct. Use the improved contact-frame/stage metrics for future contact investigations; do not retune otherwise preserved flight merely to improve an automated pilot score.

Stop this technical desktop revision loop: no remaining confirmed blocker/high, relevant executable checks pass and assessable rubric dimensions meet 8/10. More speculative geometry/architecture changes are not justified. This decision explicitly does not approve standalone performance, headset comfort, exercise effectiveness or release readiness.
