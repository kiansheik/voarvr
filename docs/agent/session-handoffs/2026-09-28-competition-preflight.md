# September 28 — competition preflight before Quest

## Goal

Resolve/read the current feature merge and run/critique all current gameplay changes and the proposed submission route before Quest testing/recording. User confirmed US/Start membership. Preserve existing work, player saves and the uncommitted merge; do not install or record on Quest in this phase.

## Files inspected

- Required agent wiki and `quality-loop.md`; competition brief, `design/HAND_FLIGHT.md`, September12 implementation evidence and telemetry contract.
- `Input/{FlightInputFrame,XRFlightInput}`, `Flight/{BirdFlightController,BirdFlightDriver}`, session/leaderboard/profile/course domain and persistence, challenge/expedition state, bootstrap/spawn and character selection.
- Course/ribbon/calibration/pause UI, rival/landing/native collision paths, telemetry sample/writer/readers/replay and relevant EditMode/PlayMode/host tests.
- Unity6000.6.0f1 current scenes and runtime artifacts; branch/index/remote identities; original ProjectSettings, EditorSettings and player-data snapshots.

## Files changed

- Runtime: `BirdFlightDriver`, `ObstacleCourseDirector`, `FlightChallenge`, `GameplayRibbon`, `CharacterSelectController`, `CalibrationCoach`, `FlightMenu`, `LandingSurface`, `SkyRivals`, `UnityFlightEnvironment`.
- Telemetry: `FlightTelemetry`, `TelemetrySample`, `TelemetryWriter`, `TelemetryReplayInput`, `tools/scripts/{telemetry,test_telemetry}.py`, telemetry documentation.
- Review/save isolation: `PlayerProfileCatalog`, `FlightSessionHistoryStore`, `CourseLeaderboardStore` Editor-only overrides; `PlayModeSaveIsolation`; new explicit `Editor/CompetitionPreflightReview.cs` and Unity-generated `.meta`; `Editor/RouteHomeReview.cs` ownership/capture settling.
- Regressions: `HandTrackingSafetyTests`, `UnityFlightEnvironmentTests`, `MagpieAndTelemetryTests`, `CourseLeaderboardStoreTests`, `FlightSessionFoundationTests`, `PlayerFacingFeatureTests`.
- Competition brief, hand-flight contract, agent index/current-state/open-questions/repo-map/log, historical merge handoff banner, [durable critique](../../reviews/2026-09-28-competition-preflight.md) and this handoff.

The prior staged merge work is preserved. Only the earlier conflict resolution was staged; later corrections are left reviewable in the working tree. No commit/push. No authored scene/prefab edit. Original settings/preloads restored through Unity APIs and checked byte-identical.

## Commands run

- `git status --short`, diff/log/remote/index inspection, `git ls-files -u`, targeted `rg` and file reads; conflict-marker/whitespace scans.
- Unity menu **VoarVR / Tools / Reconnect Installed MCP Session**, `Assets/Refresh`; MCP instance/project-state inspection, `run_tests`/`get_test_job`, Console checks and native collision reproduction.
- Full EditMode job `2bdf2a7a0853421d931ef626e953b397`:297/297. Full PlayMode job `6b3804104ce04923b8b42f648779a266`:58/58. Final affected12 PlayMode job `468f689d6a7a4bd4b6173f8639478737`:12/12. Saved XML beneath `artifacts/reviews/competition-preflight/`.
- `python3 -m unittest discover -s tools/scripts -p 'test_*.py'`:23/23.
- In idle EditMode set temporary domain-reload-disabled review options, `CompetitionPreflightReview.InstallIsolation()`, enter Play, `Run()`, `VerifyIsolation()`. Exit Play then `RestoreIsolation()` before any script refresh. Three scopes restored successfully.
- Within isolated Play, load Magpie/BirdFlight; `RouteHomeReview.RunSpecies("Magpie")` and `CaptureGuidanceFixtures()`. The final route image is `round-03/route/magpie-actual-controller-result-v2.png`; full lifecycle/guidance is in `round-02/`.
- Restore `EditorSettings` options (enabled, None) and exact Android OpenXR/XRGeneralSettings preload subassets through APIs; `cmp` against initial snapshots.
- Independent SHA-256 comparison of311 existing gameplay JSON/bak/tmp files and semantic macOS plist comparison; no added/changed gameplay data.
- `python3 tools/scripts/check_repo.py`:24 pre-existing TMP GUID findings. `git diff --check`:pass. Incoming staged `.meta` whitespace warnings are inherited from main, not changed here.
- Local development APK via `ProjectSetup.BuildQuest()`; Android BuildReport Succeeded in57.835s,0errors/32warnings; APK100,681,339bytes, SHA-256 `4067556e97b52fcd3a9650384a7d631d6f796da4322883b422cedaa7ae2a2aba`. ZIP/manifest/signature and embedded-source matching pass; details in the review. No adb/device operation.

## What worked

Executed regression suites cover the combined branch and final UI revisions. Rival non-landability is explicit, course input recovers safely, and v4 replay preserves new turn/estimated inputs. Fresh independent critiques confirm the corrected controller UI/guidance at8/10. Magpie input-only Route Home completes in202.073 simulated seconds, with seed, one landing/contact, final Perched state and restored garden after reload. The corrected final screenshot agrees. Review storage scope passes244 string/6 integer keys and987 file/preference entries; final global snapshot confirms311 gameplay files unchanged, zero additions and no gameplay preference changes. Console has zero errors after runtime review.

## What failed

Initial MCP EditMode job timed out initializing even though Unity XML passed; a later complete rerun passed. Early new fixtures omitted tracked-head input or relied on wall-clock timing; corrected to valid tracking and controlled course time. Foreground-sensitive full PlayMode attempts paused as designed; foregrounded rerun passed. The original binary-plist isolation comparison flagged only Unity engine/test-session changes; the corrected semantic comparison excludes exactly `PT_Run`, `unity.player_session_count`, `unity.player_sessionid`, `unity_connect.mega_session_id`, `unity_connect.session_id` and verifies all others. Original/throttled ribbon contamination in accelerated Route Home screenshots was corrected with owned UI and a.4s final settle; earlier artifacts are retained.

Build RPC returned no usable result; fresh Android BuildReport and artifact checks established success. APK signature check initially lacked default Java; use Unity's bundled OpenJDK.

The repository checker still reports24 existing unused TextMesh Pro sample GUIDs. World composition/materials6/10 and stylization7/10 remain below the overall visual gate. Hands-first submission completeness is2/10. No claim of Quest comfort, performance, stereo readability or physical completion of all five courses.

## Remaining questions

Eligibility question is closed by user confirmation. No new permission question blocks the documented implementation path. The missing work is concrete: SDK/adapter/source continuity, provenance-aware activity/wingbeats, versioned hand telemetry, compact calibration/gaze UI, perched Magpie cold start, physical course pilots and then wearer acceptance. Preserve the distinction between staged course observations, scripted input-only Route Home and actual uncoached Quest play.

## Suggested next prompt

“Implement the first hands-first competition milestone from HAND_FLIGHT.md: pin compatible Meta hand dependencies without replacing OpenXR, compile, then add source-aware safe input and compact calibration with replay/dropout tests. Protect Magpie Route Home as the judge path. Complete local implementation/review before installing or recording a candidate on Quest.”
