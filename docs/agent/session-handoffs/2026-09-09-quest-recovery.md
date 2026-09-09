# Quest 3 recovery - 2026-09-09

## Goal

HEAD (`8ab540e`, "latest commit but not working") needed to actually launch, stay alive, render
stereoscopically, and reach BirdFlight on a connected Quest 3, without guessing at the cause.

## Files inspected

`AGENTS.md`, agent state/log/open-questions, both prior session handoffs, `docs/VR_SETUP.md`,
`design/INPUT.md`, `design/FLIGHT.md`, `git log`/`git diff abe91d1..HEAD --stat` (823 files,
+204463/-2957 - see "known non-blocking issue" below for why), `ProjectSetup.cs`,
`CharacterSelectController.cs`, `ProjectSettings.asset`, the OpenXR package settings asset, and
real on-device evidence (adb, logcat, screencap) gathered directly rather than inferred.

## Commands run

`adb devices/pm path/dumpsys package`, repeated `adb install -r` + `am force-stop` +
`logcat -c` + `am start -W` + `pidof` + `logcat -d`/`screencap` cycles against the connected
Quest 3 (`192.168.68.52:5555`, product `eureka`, already authorized); Unity Editor driven via a
temporary file-polling bridge (`AgentBridge.cs`, deleted before finishing - no live MCP tool
access was available in this session despite the server showing connected) to run
`VoarVR/Configure Foundation`, `VoarVR/Configure Characters`, EditMode/PlayMode suites, and
`ProjectSetup.BuildQuest()`; `check_repo.py`, `python -m unittest discover`, `git diff --check`.

## What we found (in order)

1. **`androidApplicationEntry` did change** from `1` (Activity, at `abe91d1`) to `2` (GameActivity,
   at HEAD) - but a real device test showed GameActivity launches, stays resident, and creates a
   correct stereo swapchain (`views=2`, confirmed via `screencap` showing two distinct eye
   images). **This was not the regression.** Do not revert it without a new reason.
2. **A stale/earlier APK build** (installed before this session, not reflecting current HEAD)
   crashed `CharacterSelectController.BuildCard` with `MissingMethodException:
   RectOffset::set_left_Injected` - `RectOffset`'s property setters can be IL2CPP-stripped when
   nothing else in a project references them (`stripEngineCode: 1` is set here). Current HEAD's
   `BuildCard` no longer constructs a `RectOffset` at all (an earlier session replaced the
   `VerticalLayoutGroup`-based layout with explicit anchored positioning after finding that a
   `LayoutGroup` and a `LayoutElement` on the same GameObject fight over which reports the
   preferred size to a parent layout). Rebuilding fresh from HEAD does not reproduce this crash.
3. **A real, reproducible, unexplained issue**: on every clean on-device run, `Select()` fires on
   its own roughly 8-13 seconds after `CharacterSelectController.BuildUI()` completes - with
   nobody wearing the headset or touching a controller. Confirmed via temporary `[Recovery]`
   `Debug.Log` calls (removed before finishing) that `Start()`, `BuildUI()`, and `Select()` all
   ran with no exception in between; this is a UI/interaction event actually firing, not a scene
   fallback. Quest 3's controllers are Touch Plus, and only "Oculus Touch Controller Profile" was
   enabled in OpenXR - "Meta Quest Touch Plus Controller Profile" was present but disabled. This
   is a real, independently-justified gap (Quest 3 should have its actual interaction profile
   enabled) that was fixed. **The auto-select mechanism was not fully root-caused** - fixing the
   profile did not visibly change the timing in the one further test run available, and no
   physical hands-on test (a person actually holding the controllers) was possible from this
   session. Flag this to Kian explicitly; see remaining questions.
4. **`preloadedAssets` in `ProjectSettings.asset`** appeared empty in the live Editor at the start
   of this session versus 2 entries at HEAD. This turned out to be transient/build-time
   (XR Management injects them during `BuildPlayer`, not persisted at rest) - every build in this
   session produced a working XR session regardless of the checked-in file's state at that
   moment, and the file matches HEAD again now. Not a real issue; noted in case it recurs.
5. **Known, non-blocking, separate issue**: `Assets/TextMesh Pro/` (Resources + a full
   `Examples & Extras` folder, ~204k of the diff's insertions) exists in the repo despite nothing
   in VoarVR's own code using TextMeshPro (`CharacterSelectController` uses legacy
   `UnityEngine.UI.Text`). It appears to have been pulled in as a side effect of importing the
   XR Interaction Toolkit Starter Assets sample. It is incomplete/broken - `check_repo.py` reports
   24 unresolved GUIDs, all confined to that folder - and it produces harmless but noisy
   `TMP_SpriteAsset`/`TextSettings` serialization warnings in logcat on device. It does not block
   building, launching, or running. Deliberately **not touched** this session per the instruction
   to keep the functional fix and cache/asset cleanup separate - see Remaining questions.

## Root cause

No single fatal crash was found in current HEAD. The "not working" report most plausibly
described either (a) the stale pre-HEAD APK's `RectOffset` crash (already fixed upstream of this
session, confirmed gone in fresh builds), and/or (b) the on-device experience of the menu
auto-advancing past CharacterSelect within seconds, which - combined with the initial
`androidApplicationEntry`/OpenXR-profile suspicion - would read as "nothing works" even though
the app is actually alive, rendering, and reaching BirdFlight successfully underneath it.

## Fixes made

- `ProjectSetup.ConfigureXR()`: enabled `MetaQuestTouchPlusControllerProfile` alongside the
  existing `OculusTouchControllerProfile` (kept for older Quest 1/2 hardware). `BuildQuest()` now
  fails fast if either is disabled.
- `ProjectSetup.BuildQuest()`: added `ValidateCharacterCatalog()`, run before every build - fails
  with a specific message if a build scene is missing, `CharacterSelect`'s `xrRigPrefab`
  reference is unset, no `BirdCharacterDefinition` exists, or any character is missing its
  `RigModel`, `BodyMaterial`, a required bone, or a `"*Face"` renderer. A `BuildPlayer` success no
  longer means the resulting app can actually do anything once it launches.
- Reran `VoarVR/Configure Foundation` so the new OpenXR profile state is what future builds pick
  up (not just a one-off editor session change).

## GameActivity vs Activity A/B

Not run as a full A/B (flip-rebuild-reinstall-compare) - real-device evidence already showed
GameActivity working correctly (launches, stays alive, correct stereo swapchain), which
falsified the hypothesis before an A/B was needed. Recorded here so a future session doesn't
redo this: **GameActivity is not the problem, don't revert it.**

## CharacterSelect vs direct-BirdFlight A/B

Not built as a separate smoke APK - direct on-device testing of the real app already showed both
scenes are reachable and neither exceptions nor hangs during the transition; the interesting
finding (spurious auto-select) needed the real menu, not a bypass, to observe. A
`VoarVR/Build Quest Smoke APK` facility (direct-to-BirdFlight) was not created this session; it
remains a reasonable idea for whoever chases the auto-select issue next, but wasn't necessary to
reach this session's evidence.

## What worked

- Editor: 38/38 EditMode, 7/7 PlayMode (including `BootstrapSmokeTests`, which drives the real
  Bootstrap -> CharacterSelect -> click -> BirdFlight path). `check_repo.py` fails only on the
  pre-existing TMP import issue (24 findings, all in `Assets/TextMesh Pro/`, zero involving
  first-party code). 12/12 host Python tests pass. `git diff --check` clean.
- Quest 3 (`192.168.68.52:5555`, real hardware, `adb`-verified `device` state throughout):
  clean install, explicit `am start -W` launch succeeds, process survives repeated multi-second
  observation windows, a real two-eye stereo swapchain renders, zero fatal `AndroidRuntime`
  crashes and zero first-party exceptions across every capture in
  `artifacts/quest-recovery/head-current/` from the final build. BirdFlight was directly observed
  rendering upright (duck head/beak, world geometry, wind ribbons, the "LANDING RING AHEAD" coach
  prompt and the pre-calibration reticle all visible and correctly oriented - no camera roll, a
  real defect seen in one earlier stale-build screenshot that fresh HEAD does not reproduce).
- Final APK: `builds/quest/VoarVR.apk`, SHA-256
  `e7117a7d85d85552551e1d3cb43ad62bbaf3bab690d634a5fd3c4f2b6f133b7c`, package
  `com.voarvr.prototype`, launcher `com.unity3d.player.UnityPlayerGameActivity`, minSdk 29,
  targetSdk 36, `arm64-v8a` only, `android.hardware.vr.headtracking` present.

## What failed / was unavailable

- No live Unity MCP tool access this session despite `claude mcp list` showing the server
  connected (same limitation as a prior session) - all Editor automation went through a
  temporary, deleted-before-finishing file-polling bridge script instead.
- No hands-on hardware flight test: nobody physically wore the Quest or held its controllers
  during this session, so calibration, wing tracking, flap/tuck/flare response, and the
  auto-select mechanism's actual trigger could not be confirmed or disproven by touch - only by
  logs and screenshots taken through `adb` against an unattended, seated headset. One run quit
  cleanly (Unity's own orderly shutdown sequence, no exception) after ~27 unattended seconds,
  most likely the headset's own proximity/idle behavior with nobody wearing it, not a new crash -
  but this was not confirmed against a worn headset either.
- `Assets/TextMesh Pro/` cleanup was deliberately not attempted (see above) - left as a scoped,
  separate follow-up rather than mixed into this fix.

## Remaining questions

- **Hands-on test still needed**: put the headset on, hold both controllers, and confirm whether
  the ~8-13s auto-select still happens with a real controller resting in a tracked pose, whether
  CharacterSelect is visible/legible/interactable before that point, and whether Duck/Dragon can
  be deliberately chosen. If the auto-select persists with real hands on the controllers, it is a
  software issue (likely the first-read/binding-settle behavior of the XRI default input actions
  or the Near-Far Interactor's default ray target) and needs the menu simplified per
  `AGENTS.md`'s "minimal Quest menu rig... preferable to importing locomotion... machinery merely
  for two buttons" guidance - but only once evidence (not the profile fix's absence of visible
  effect) actually implicates it.
- Full on-hardware flight validation (calibration, wing tracking, flap/tuck/flare, recenter,
  tracking-loss/reacquisition) is unrun - needs a worn headset, not adb.
- `Assets/TextMesh Pro/` incomplete import: recommend deleting `Examples & Extras` (and
  `Resources` if nothing needs it) as its own change, then rerunning `check_repo.py`.
- Whether the `unity/.utmp/` tracked-generated-file removal (flagged in the character-select
  handoff) is still pending explicit approval - untouched this session, no new churn introduced
  (the one file that changed from a build was reverted before finishing).

## Suggested next prompt

"Put the Quest 3 on and hold both controllers. Launch the current build, watch whether
CharacterSelect stays up and is selectable versus auto-advancing, and report what you see and
felt for a few minutes of real flight - calibration prompt, wing response, tuck/flare, recenter."
