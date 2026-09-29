# September 28 — hands-first implementation completion

## Goal

Finish the interrupted hands-first milestone: verify the last camera fix, capture fresh controller-free evidence, repair what that evidence exposed, check the packaged hands-only launch configuration, run the independent quality loop and record the state. No Quest install, commit or push.

## Files inspected

- Agent wiki, `design/HAND_FLIGHT.md`, competition brief, quality loop, VR setup, telemetry contract; previous handoffs (preflight, merge review, Quest discovery) and `artifacts/reviews/hand-flight/round-01/technical.md`.
- New/changed hand sources from the interrupted session: `Input/{ITrackedFlightInput,HandPoseContinuity,MetaHandFlightInput,FlightInputFrame}`, `UI/{HandGazePointer,CalibrationCoach,CharacterSelectController,FlightMenu}`, `Core/FlightCamera`, `Flight/{BirdFlightDriver,WingRigSolver}`, `Editor/{HandFlightReview,HandFlightSetup,LocalSdkBuildSettings,CompetitionPreflightReview,RouteHomeReview,TelemetryBuildStamp}`, hand tests.
- World/route: `World/{WorldTerrain,WorldChunk,FlightRegions}`, `Gameplay/{FlightChallenge,RouteHomeChapter}`, `BirdFlightController.TryRecoverToPerch`, `UnityFlightEnvironment` landability.
- Imported Meta Core 207 source (read-only): `OVRPlugin` WMM/source APIs, `OVRHand`, `OVRManifestPreprocessor`, `MetaXROperatorBuildProcessor` and its AAR manifest.
- Live editor state: the interrupted session had left Play Mode running with round-02 storage isolation installed and `EditorSettings` Enter Play Mode options set to disable domain reload.

## Files changed

- Runtime: `World/FlightRegions.cs` (authored `DepartureLookout*`, clearing helper), `World/WorldChunk.cs` (10 m lookout scatter clearing), `Flight/BirdFlightDriver.cs` (departure → ridge lookout with heading; SavedPerch → Departure → SpawnFloor fallback that preserves the departure's automatic resume; hands final failure opens the rest menu), new `UI/WorldCardRendering.cs` (+ Unity `.meta`), `UI/FlightMenu.cs` and `UI/CalibrationCoach.cs` (cards draw over world geometry).
- Editor: `HandFlightReview.cs` (bird pose in evidence), `RouteHomeReview.cs` (`RunSpeciesFromLookout`), `LocalSdkBuildSettings.cs` (`VerifyApk` exempts only Unity's Development `player-connection-ip` line in `boot.config`).
- Tests: `EditMode/WorldTerrainTests` (lookout flat/clear/open-view/heading), `PlayMode/HandFlightLifecycleTests` (lookout position/heading/open view; unsupported-lookout fallback resumes), `PlayMode/HandMenuTests` (cards draw over world in hands and controller modes).
- Docs: `design/HAND_FLIGHT.md`, `docs/competition/meta-vr-start-2026.md`, `docs/VR_SETUP.md`, `docs/ARCHITECTURE.md`, agent current-state/log/repo-map/open-questions/index, [hand-flight review](../../reviews/2026-09-28-hand-flight-implementation.md) and this handoff.
- The interrupted session's uncommitted work is otherwise preserved. No scene/prefab/asset authoring edits.

## Commands run

- Unity MCP: instance/editor state; `HandFlightReview.Run()` in the already-isolated round-02 Play session; stop Play, `CompetitionPreflightReview.RestoreIsolation()`, restore `EditorSettings` (byte-identical to the pre-review snapshot).
- Offline terrain/scatter replicas (Python) of `WorldTerrain.Elevation`, `RiverCenter` and the `WorldChunk` scatter hash to choose and check the lookout.
- Refresh/compile; full EditMode and PlayMode runs; round-03 isolated `HandFlightReview.Run()`; isolated `RouteHomeReview.RunSpeciesFromLookout("Magpie")`; settings snapshots compared with `cmp` after every Unity run.
- `ProjectSetup.BuildQuest()` (MCP RPC returned nothing; BuildReport read from `Library/LastBuild.buildreport`); `aapt2 dump badging/xmltree`; `LocalSdkBuildSettings.VerifyApk()`; masked `boot.config` inspection; full-entry token/address scan; embedded source-fingerprint recomputation.
- `python3 -m unittest discover -s tools/scripts -p 'test_*.py'`, `python3 tools/scripts/check_repo.py`, `git diff --check`.

## What worked

- Round-02 capture (after the interrupted camera fix) proved the perched view was no longer below ground, but exposed two new defects: the spawn is the centre of an 85 m flattened bowl, so the perch faced a wall of rising terrain, and terrain cut through the lower rows of every perched card, hiding the results card's only exit button.
- Fixes verified in round-03: perched Magpie on the ridge lookout (72, 23.32, 60), yaw 18°, zero takeoffs, open horizon and route chevrons; rest, settings, recalibration and results cards complete, including the exit button. Storage isolation PASS for every review run; no gameplay file or preference changed.
- EditMode **317/317**, PlayMode **69/69**; host tests **29/29**. Repository check: only the 24 known TMP sample GUIDs.
- Scripted Magpie pilot from the perched lookout: **Completed in 215.85 simulated s**, seed collected, one contact (final garden landing, 3.36 m/s), garden restored after reload (air-spawn baseline 202.07 s).
- Development APK: BuildReport Succeeded, 168.529 s, 0 errors / 8 warnings, 69,181,451 bytes, SHA-256 `b050111474ae4cb3cfb3417d28582d13db45211e2ce075a5aaf5adbe7fec154d`; the previous controller APK is preserved as `builds/quest/VoarVR-controller-4067556e.apk`. The manifest requires hand tracking and declares the hand/body permissions, body tracking, `handtracking.frequency=LOW` and the VR launcher. The embedded fingerprint `ff21bfc3…` equals the tree with only the later editor-only `VerifyApk` change reverted.

## What failed

- The first `VerifyApk()` failed on `boot.config`: the only match was Unity's Development-build `player-connection-ip` (this Mac's LAN address). A full scan found the SDK token in none of the 797 entries. The check now exempts only that exact line and reports it.
- The Development APK also contains Meta Core's dev-only XR Operator AAR (MediaProjection service, `FOREGROUND_SERVICE_MEDIA_PROJECTION`, non-required `com.oculus.experimental.enabled`); the SDK excludes it from non-Development builds. Meta's setup checks warn about minimum API 32 and a single GameActivity entry.
- MCP `BuildQuest` RPC returned no result while the editor blocked; the PlayMode runner flips Enter Play Mode options and was restored after each run. `git diff --check` reports trailing spaces only in Unity-serialized empty strings of `OpenXR Package Settings.asset` (engine-generated, left untouched).

## Remaining questions

- Device behaviour of `GetHandPoseSourceInferred` with and without WMM or body permission (see open questions); physical pinch/gaze and two-pinch recognition; whether an immediate lift discovery from the lookout helps a first-time wearer; stereo comfort of always-on-top cards; course overlay TextMesh cards may still be depth-clipped near terrain.
- No Quest install, launch, recording, commit or push in this session.

## Suggested next prompt

"Quest is awake with wireless ADB. Install the current hands Development APK after rerunning `VerifyApk()`, then run the controller-free cold start: deny and then grant body tracking, calibrate, take off from the ridge, rest/settings/recalibrate/results. Pull telemetry and report source fractions, recovery events and any false-propulsion guard activity. Do not commit."

## Close-out (September 29)

After the round-3 critique revisions: EditMode **318/318**, PlayMode **70/70**, host **29/29**. Final Development APK `builds/quest/VoarVR.apk`: BuildReport Succeeded in 45.1 s, 0 errors / 6 warnings, 74,756,207 bytes, SHA-256 `54c68b00587426c5fd19f306b8650bbd51638dd9ef3740bc1324443f5475020f`. Manifest still requires hand tracking with hand/body permissions; `VerifyApk()` PASS (only Unity's Development profiler address present). This supersedes the earlier `b0501114…` APK.

Every Android build replaces the two XR preload entries in `ProjectSettings.asset` with Meta's `OculusRuntimeSettings`; restore them afterwards with `PlayerSettings.SetPreloadedAssets` (XRGeneralSettings Android `7fef7708…:-7519165060713318301`, OpenXR Android settings `769f6f73…:-6908929336186598598`) and `cmp` against the committed file. The MCP PlayMode runner also flips Enter Play Mode options; reset them to None. Both were restored at close-out. The technical critic was cut off by a usage limit; rerun it against this tree if a desktop technical review is wanted before the Quest session.

Quest test checklist: controller-free cold start; deny then grant body tracking (hands must still calibrate); READY on the ridge with the bird visible; first flap takeoff; two-pinch rest (palms down) → settings → recalibrate → Continue; Quest menu out and back (rest card should reappear); Return to safe perch; Finish session → results → return; Resume at saved perch. Watch for horizon tilt when turning your head in third person (P8) and for the always-on-top cards' depth comfort.

## Quest launch fix (September 29)

First install of `54c68b00…` crashed in `il2cpp_init` (SIGSEGV, before any game code): Gradle's incremental merge wrongly kept the Sep 28 `libil2cpp.so` (build ID `1211fb29…`) beside new metadata. Deleting only `*/build` then failed Gradle's input validation; deleting the whole exported `unity/Library/Bee/Android/Prj/IL2CPP/Gradle` project fixed it. Working APK: 69,185,827 bytes, SHA-256 `734d5217939ac94e9188fdb9fe5270489cc599ca77251d9935a93fab5d717903`, packaged IL2CPP build ID `20d79709…` matching Unity's output, `VerifyApk()` PASS. Installed and launched on Quest 3: process stays alive, hand tracking active, no Unity errors or exceptions in logcat (`artifacts/quest-logs/`). `quest.py install` now refuses an APK whose IL2CPP build ID differs from Unity's compiled output.

`make quest-connect` with USB attached now reads the headset's `wlan0` address, runs `adb tcpip 5555` only when Wi-Fi debugging is closed, connects over Wi-Fi and caches the IP, so later commands keep working after unplugging. Verified live (192.168.68.52). Host tests 33/33.

