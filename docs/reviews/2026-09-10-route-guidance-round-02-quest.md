# Route Home guidance — independent Quest technical reassessment, round 2

The technical source/Editor gate passes for this focused revision: no remaining blocker/high defect or required source correction was established. This is not Quest performance or wearer acceptance.

## Fresh evidence inspected

- Current `JourneyPresentation`, `RouteBearing`, driver connection, `FlightCamera`, shared Skyward shader and `RouteBearingLifecycleTests`.
- Parsed `artifacts/reviews/route-guidance/round-02/playmode-results.xml`: **37 actual tests, 37 passed, 0 failed**. All three new bearing lifecycle tests and five journey-world tests are present and passed.
- Parsed the same folder's `editmode-results.xml`: **240 actual tests, 240 passed, 0 failed**. Zero-test attempts are not counted as validation.
- Parsed the fresh runtime-fixture JSON: **46 frames, zero with flight text visible**; Unity **6000.6.0f1**, `Assets/Scenes/Prototypes/BirdFlight.unity`. The fixtures use actual presentation/wind/bearing code and `FlightCamera.ApplyPose`, with explicitly staged checkpoints, locations and tracked poses. The storage check reports original PlayerPrefs unchanged.
- Visually inspected nine fresh PNGs: the 1138 m seed-behind and reacquired views; Soar; seed approach; garden landing; Magpie transfer midpoint and settled carry; Dragon ordinary third-person and advanced embodied carry. No other critic's reassessment was read.

The manifest accurately limits these captures: they are not an input-only replay, stereo render, worn test, Quest profiler recording or proof of perceived pickup timing. The far-target reacquisition fixture still places the target near the lower edge, and correctly retains the bearing; it does not demonstrate the cue hiding after fully centering the target. The scene lifecycle test separately verifies that behavior.

## Resolution of round-1 findings

| Finding | Fresh evidence | Result |
| --- | --- | --- |
| Final camera-pose ordering | `RouteBearing.ApplyFinalPose` uses `BeforeRenderOrder(100)` after the camera's default callback. It recomputes only presentation; ordinary updates schedule sound. The runtime test changes camera translation/rotation, invokes this final callback and verifies the cue's new world position against the latest view. | Source/Editor defect addressed. Actual worn rapid-turn stability remains unverified. |
| Presentation disable leaves stale cues | `JourneyPresentation.OnDisable` clears navigation and deactivates its separate world root; `Present` rejects disabled components. The live test disables/re-enables the owner and verifies bearing suppression/recovery. | Addressed. |
| Missing configured-bearing lifecycle coverage | New scene tests cover hidden HUD, target reacquisition, guidance/audio preferences, pause, streaming, own focus/application pause, menu, head/hand tracking, owner disable, final pose, repeated configuration and destruction of cue/voice/mesh/clip. | Addressed within Editor scope. |

The trail now uses six outlined bars instead of twelve, reducing its mesh from 288 to 144 vertices. Its open form sits below the approach line and yields within 16 m while the target marker remains. The pickup shrinks before approaching the eye socket. Fresh images show the seed aperture, landing ring and held seed rendered with the intended material; no magenta shader failure or filled-arrow artifact is visible in the inspected views.

## Scores and remaining limits

| Dimension | Score | Basis |
| --- | --- | --- |
| Bounded geometry/resource ownership | 8/10 | Five chapter renderers plus one bearing allocated, bounded meshes, shared material, explicit release and repeat-configuration test. |
| Material/shader suitability | 8/10 | Existing opaque single-pass vertex-color shader and stereo support; no new textures, transparency or shadow pass on cues. Fresh Editor renders agree. |
| Logical coordinates/objective coupling | 8/10 | Actual targets and pickup radius, sampled useful air, rebase handling, near-arrival target retention. |
| Lifecycle and runtime regression coverage | 8/10 | Real passing scene tests now cover the added ownership and control gates; current full suites passed. |
| Quiet-view contract/scope | 9/10 | All 46 fixtures retain hidden flight text. Guidance remains separate from flight-force authority. |
| Sustained Quest performance/stereo comfort/audio quality | Unverified | No new APK/hardware evidence was available to this critic. |

One **medium — unverified risk** remains for the release check: thin outlined geometry and the view-relative bearing have not been observed during worn stereo movement on this APK, and their incremental CPU/GPU cost has not been measured on Quest. The source is bounded, but that does not establish frame delivery or readable edges after headset antialiasing. Install the built revision, inspect both-eye rendering and rapid head turns, then measure a representative route with streaming active. This requires hardware evidence rather than another speculative source change.

No third source-polish round is recommended on technical grounds. Proceed with the build/device checks and retain player/visual acceptance as separate gates. The 24 existing unused TMP GUID findings remain outside this change.
