# September 28 — merged branch pre-Quest review

The merged controller baseline now has executed Unity validation, independent visual/player/technical critique and a bounded repair pass. It is not yet the hands-first competition candidate. US/Start membership is user-confirmed. No Quest install, launch or recording was performed during this review; no commit or push was made.

## Scope and corrections

Reviewed feature `7798d3a` with incoming main `183557f`, including the five courses, typed moths, rivals, session history/results, supported turn, calibration, collision response, guidance and estimated-motion guard. The sole log conflict preserves both branches' records; the merge remains uncommitted.

- Native collision reproduction showed a rival's upward normal could still create a landing despite its zero slope threshold. Explicit `LandingSurface.CanLand` now prevents rival landing/support while retaining collision.
- Course results now reject confirmation during missing head/hand tracking, focus loss or application pause and require release after recovery. Course input ownership no longer rearms the ordinary action gate every valid frame.
- Estimated downstrokes are tested both for zero active force and inability to launch from supported rest.
- Telemetry v4 adds raw/mapped supported yaw and both estimated-motion flags, with binary replay tests and v1–v3 compatibility. Hand source/confidence/continuity signals and activity/wingbeat provenance remain future integration work.
- The goal ribbon exposes both soaring requirements, Training medal/score and actual saved garden restoration. Course results explain practice status. Course preflight keeps descriptions and records in separate rows. Free Flight results have the correct title; calibration text/diagram fit; rest-menu text no longer leaks beneath session results.
- Editor tests/reviews isolate profiles, session history, leaderboards, journey/foraging saves and telemetry. Explicit injected stores retain precedence. The review helper never runs on import.

## Executed evidence

Unity **6000.6.0f1**, Android target, connected instance `unity@a229b2354a2d8f06`. Authored scenes: `Assets/Scenes/Menu/CharacterSelect.unity` and `Assets/Scenes/Prototypes/BirdFlight.unity`. No authored scene/prefab or ProjectSettings delta remains from review operations.

| Check | Result and limit |
| --- | --- |
| Full EditMode | **297/297 pass**, job `2bdf2a7a0853421d931ef626e953b397` |
| Full PlayMode | **58/58 pass**, job `6b3804104ce04923b8b42f648779a266` |
| Final affected player-facing suite | **12/12 pass**, job `468f689d6a7a4bd4b6173f8639478737`, after calibration, all-course preflight and result revisions; includes the added preflight case |
| Host tooling | **23/23 pass**, including legacy/v4 telemetry decoding |
| Runtime lifecycle/UI | **72 fresh staged captures**: all five course count-ins/tasks/practice results/rest/retry, selection, calibration/help, collectibles/rivals, session results and objective/reward states |
| Runtime guidance | **46 fresh labeled camera fixtures** across species/views, with flight text hidden; source-driven cues and seed possession |
| Magpie Route Home | Actual semantic input through driver/solver/native streamed collisions: **202.073 simulated seconds**, score1000, seed collected, one landing/contact, `Perched`, garden restored after reload |
| Player storage | Final **311 existing gameplay files unchanged, zero additions**, no gameplay plist key changes; review scope additionally verifies987 file/preference entries and244 string/6 integer keys |
| Console | Zero error entries after final runtime captures |
| Repository check | Existing **24 unused TMP sample GUID findings**; no new category |
| Whitespace/conflicts | Working-tree `git diff --check` passes; no unmerged entries/conflict markers. Incoming staged metadata retains pre-existing whitespace warnings |

Full-suite XML is under `artifacts/reviews/competition-preflight/round-01/`; final affected tests and72 fixtures under `round-02/`;46 guidance frames under `round-02/guidance/`. The settled, correct final route screenshot/JSON is under `round-03/route/`. Final save verification is `final-player-storage-check.json`. Artifacts are ignored local evidence, not submission footage.

The course helper explicitly injects clock/positions/catches/tricks/landing observations. It verifies lifecycle and UI, **not physical course completion**. The separate Route Home pilot uses flight input and actual collisions but begins at `(0,25,0)`; authored gameplay begins at `(0,12,0)`. It does not prove cold-start onboarding, hand tracking or human pacing. Seed collection is69.947s; arch passage195.416s; final contact speed3.238m/s.

Earlier failed runs remain diagnostic evidence: an initial new input fixture omitted tracked-head state; a timed course fixture was replaced with a controlled clock; full PlayMode attempts paused when Unity lost focus, then passed with Unity foregrounded. A review-only plist byte comparison flagged legitimate engine-session changes; semantic comparison now excludes exactly five engine/test keys and checks every other key. Earlier route screenshots captured a stale original/throttled ribbon; fixture ownership and a .4s settle wait produce the final `GARDEN RESTORED` image. These are not hidden failed checks.

A fresh **Android development APK succeeded** in57.835s with **0 errors / 32 warnings**: `builds/quest/VoarVR.apk`,100,681,339 bytes, SHA-256 `4067556e97b52fcd3a9650384a7d631d6f796da4322883b422cedaa7ae2a2aba`. ZIP integrity, APK v2 signature (Unity bundled OpenJDK) and Android manifest checks pass. Its embedded selective source fingerprint `bbd4b97b4a2a671ebe129d2918aa1840a62ae2b6139dad901c0a450ecb37466d` matches current first-party code/model/catalog inputs. Warnings concern TMP shader pragmas/large generated C++ methods and Metal compute-shader limits on older Apple hardware; the full BuildReport is saved in `artifacts/reviews/competition-preflight/build-result.txt`. Build-tool RPC completion failed, so success was established from the fresh Android BuildReport and artifact identity. This is a controller regression build, not the absent hands-first submission candidate; it was not installed.

## Independent critique and disposition

Three roles inspected sources/current artifacts independently before synthesis. The player critic subsequently implemented some UI fixes; the separate visual critic and root checked the fresh results. Two broad capture rounds plus one targeted route-capture correction reached the documented three-round cap.

| Dimension | Final assessment |
| --- | --- |
| Menu hierarchy, text/layout, navigation/possession, bird silhouette | **8/10** |
| Controller preflight, calibration/pause, progression/reward, course result/rest/retry | **8/10** |
| Safety, collision integrity, recording/replay, persistence isolation | **8/10** within local evidence |
| World composition, lighting/material finish | **6/10** |
| Stylization coherence | **7/10** |
| Hands-first submission completeness | **2/10** |
| Quest comfort/performance, stereo readability, flight feel, hand usability | **Unverified**, no fabricated score |

No remaining blocker/high objective defect was found in the bounded controller corrections. This does not satisfy the overall candidate gate:

1. **Blocker, objective:** hand SDK/adapter, source continuity, gaze/pinch UI and compact calibration are absent. Android still uses `XRFlightInput`; essential screens require controller actions. Implement the [hand-flight contract](../../design/HAND_FLIGHT.md) before submission claims.
2. **High, objective:** the judge opening is not protected. Current selection defaults to Duck and exposes six activities; flight begins gliding. The brief specifies Magpie, a perch and a deliberate first downstroke. Implement that opening as part of the hands-first slice.
3. **High, unverified:** none of the five course fixtures demonstrates physical input-only completion within its time limit. Validate real trajectories for every exposed course, then wearer completion; hide extras from the judge path until accepted.
4. **Medium, subjective art limitation:** repeated towers, broad terrain/material treatments and rectangular course frames still look like a prototype. After hand-flight acceptance, focus an art pass on the short hero route's composition, material hierarchy and lighting, preserving gold navigation cues.
5. **Unverified hardware gates:** uncoached comprehension, compact seated reach, pickup timing, stereo text, comfort, focus/resume and sustained frame pacing require the actual candidate on Quest.

Further small UI changes would have lower value than closing these gates. The local controller regression APK is a build check; the submission channel, frozen candidate and actual Quest trailer remain unprepared. See the [competition readiness table](../competition/meta-vr-start-2026.md#readiness-at-september-28) and [session handoff](../agent/session-handoffs/2026-09-28-competition-preflight.md).
