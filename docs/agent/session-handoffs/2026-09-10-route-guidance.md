# Route Home guidance and seed possession

## Goal

Resolve the actual Quest playtest: the player could not identify the route or tell whether they had collected the seed. Explicit preference: keep flight text hidden; strengthen world markers and pickup cues.

## Files inspected

Required agent wiki and quality loop; FlightChallenge/RouteHomeChapter/ExpeditionDirector; JourneyPresentation/WorldSpace/WindField; BirdFlightDriver/FlightCamera/FlightFeedback; FlightPreferences/FlightHud; RouteHomeReview and world/lifecycle tests; latest Quest telemetry a5697a0c1b8e43429ca4532eb178ae63.

## Files changed

JourneyPresentation, new RouteBearing, driver binding, stronger existing seed motif/pulse; JourneyWorldTests, RouteBearingTests and RouteBearingLifecycleTests; RouteHomeReview's explicit runtime capture helper and its Editor assembly's existing Unity.InputSystem reference. Agent current state, log, repo map, this handoff and linked review reports. Earlier first-adventure and advanced-neutral work was preserved; no commit/push. Build-only tracked `.utmp` fingerprint churn was restored to its pre-turn bytes; no engine cache edits.

## Commands run

- Unity MCP, Unity6000.6.0f1, this project's BirdFlight prototype: explicit full asset refresh/compile, EditMode/PlayMode Test Runner; Console inspection.
- `python3 -m unittest discover -s tools/scripts -p 'test_*.py'`: 21 pass.
- `python3 tools/scripts/check_repo.py`: the same24 unresolved TMP GUID findings.
- ADB pull of the latest Quest recording to `artifacts/telemetry/route-guidance-2026-09-10/`; read-only decoding to baseline-summary.json/baseline-report.md.
- Full Unity Test Runner:240 EditMode/37 PlayMode pass. Final carry-only revision reran8 affected world/bearing PlayMode tests, all pass. XML evidence in `artifacts/reviews/route-guidance/round-02/` and `round-03/`.
- `RouteHomeReview.CaptureGuidanceFixtures()` in BirdFlight Play Mode:42/46/46 labeled runtime-camera states across three rounds; final Console0errors and PlayerPrefs unchanged. No scene/prefab assets were saved by this review.
- Restored Android OpenXR/XRGeneralSettings preloads through the Editor API after tests and again after build cleanup. Editor left idle on `Assets/Scenes/Prototypes/BirdFlight.unity`, Unity6000.6.0f1.
- `ProjectSetup.BuildQuest()`: succeeded40.761s,0errors/6warnings. MCP's empty false response was checked against the actual BuildReport and APK before claiming success.
- `adb install -r builds/quest/VoarVR.apk`: failed `adb: device offline`. Existing wireless transport reconnect remained offline; user chose **"I'll test later"**, so installation/startup is deferred.

## What worked

The actual recording matches first-adventure source7eda92d4…: 37,899 frames, clean footer, zero drops. CollectSeed activated twice with the target126–148° behind motion; the first attempt stayed beyond the old850m seed visibility cutoff. Closest recorded seed approach369.76m, versus8m capture radius: the seed was never collected. This confirms that the previous first-adventure build was installed and played, superseding its earlier offline install status. [Diagnosis](../../reviews/2026-09-10-route-guidance-diagnosis.md).

World presentation now exposes an actual sampled lift destination, gold/charcoal chevrons and distinct stage markers. The seed marker remains available beyond850m. A non-text turn cue reacquires offscreen targets without changing the camera. Actual observed pickup transfers the seed onto the bird over0.9s; resumed possession appears directly. Text preference, pause, tracking, guidance/audio/haptic preferences and world-coordinate rebasing are preserved.

## What failed

The previous player-facing review staged seed presentation for unrelated views and bypassed the runtime lift visibility gate. It did not test the behind-player stage transition exposed by telemetry. The current pass adds runtime Tick/camera evidence and regression tests. Repo validation still has the existing24 TMP GUID failures; no unrelated vendor changes were attempted.

The new capture helper initially lacked an Editor reference to the already-installed InputSystem assembly; fixed explicitly. One test run then failed initialization and subsequent runs returned zero tests, which were rejected as validation. Read-only diagnosis found UTF1.8's `PlayerTestAssemblyProvider.ResetStaticsOnLoad()` clears a non-null cached list while `LoadAssemblies()` returns early for non-null; MCP disables domain reload. Resetting private static `m_LoadedAssemblies` to null in memory before the Play run restored real discovery (37 tests). No vendor/cache files were edited. For recurrence, in idle Edit Mode execute:

```csharp
var type = typeof(UnityEngine.TestTools.UnityTestAttribute).Assembly
    .GetType("UnityEngine.TestTools.Utils.PlayerTestAssemblyProvider", true);
type.GetField("m_LoadedAssemblies", System.Reflection.BindingFlags.Static |
    System.Reflection.BindingFlags.NonPublic).SetValue(null, null);
```

Three independent rounds fixed oversized crossing path fins, prolonged pickup occlusion, final-camera synchronization, component disable cleanup and Dragon carry scale/cropping. [Synthesis and final reviews](../../reviews/2026-09-10-route-guidance-implementation.md). Scoped navigation/possession8/10; broader material finish6/10, no headset acceptance claim.

## Build artifact

`builds/quest/VoarVR.apk`:100,182,490bytes; SHA256`3797aa32ab72690e015785a71bbe98e1d44f69f13be41a7d941743c41c7f517f`; embedded source SHA256`81247fa3a4151c0badc135ad4aa1140262ce724976b31a901b804dbf9f8c3744`. Exact metadata in `artifacts/reviews/route-guidance/build-result.json`. Installation and new-build startup remain unverified; previous app data was not cleared.

## Remaining questions

New wearer route comprehension, pickup recognition, binocular comfort and sustained Quest cost require a fresh headset playtest. Desktop fixtures are labeled and cannot establish those results.

## Suggested next prompt

Play Route Home with HUD hidden, deliberately turn away from the current target, then collect the seed and look down at the carried seed. Download the new recording and confirm acquisition, arch passage and garden restoration; report clarity, comfort and performance separately.
