# Route Home: visible route and seed possession

The latest Quest recording showed a concrete navigation failure: the seed objective started126–148° behind the player, and the seed was never collected. In the first attempt, its physical mesh stayed beyond the850m visibility cutoff for the entire objective. Closest approach across both attempts was369.76m, versus an8m pickup radius. [Recorded diagnosis](2026-09-10-route-guidance-diagnosis.md).

The user explicitly chose **hidden flight text, stronger world markers and pickup cues**. This implementation adds no ongoing text and preserves the existing HUD preference and flight physics.

## Resulting behavior

- A short gold/charcoal chevron trail leads toward the current objective. It sits below the destination sightline, gets smaller during approach, and yields within16m so the actual target remains open.
- Different destination silhouettes identify rising air, the seed aperture, the arch and the landing terrace. The seed destination marker remains available beyond the physical seed's850m range.
- If the target is outside the current viewing cone, a small non-text turn symbol and optional spatial call help reacquire it. The symbol follows the final camera pose, including tracked head movement, without rotating the camera.
- Lift guidance samples genuinely rising air inside the departure column, including when the player has flown above it. StillAir does not advertise nonexistent lift.
- A real observed pickup moves the seed onto the creature over0.9s, shrinking early so it leaves the view open. A stronger existing motif and pulse confirm collection; possession remains visible beside the creature's head. Resume shows an already-held seed without replaying collection.
- The carry position works across Duck, Magpie and Dragon. Third-person seed size compensates the more distant species-aware chase camera, clamped1–3×; first-person size remains small. The seed stays attached to the creature when the player looks away.
- Guidance, audio and haptic preferences remain independent. Menu, pause, tracking loss, streaming blocks, focus changes, component removal and coordinate rebasing retain their lifecycle behavior.

## Independent review and revisions

Three rounds used separate visual, player and Quest technical critics. Round1 found a large cross-shaped path marker covering the destination, a pickup that stayed too large during transfer, and final-camera/disable lifecycle gaps. Round2 fixed these and exposed Dragon carry size/cropping. Round3 corrected the carry socket and third-person scale. Fresh evidence was captured after each visual revision.

Final [visual](2026-09-10-route-guidance-round-03-visual.md), [player](2026-09-10-route-guidance-round-03-player.md), and [technical](2026-09-10-route-guidance-round-03-quest.md) reports find no confirmed blocker/high defect in this scope. Assessed static navigation/possession dimensions are8/10; quiet-view compliance10/10. Overall material finish remains6/10 and belongs to the broader art roadmap. Stop at the three-round cap.

`artifacts/reviews/route-guidance/round-03/` contains46 fresh labeled states. The explicit `RouteHomeReview.CaptureGuidanceFixtures()` uses actual `JourneyPresentation.Tick`, wind probes, `RouteBearing` and `FlightCamera.ApplyPose`, with staged checkpoints/positions and injected tracked-head samples. The pickup uses a swept challenge observation. All46 states retain hidden flight text; storage isolation reports PlayerPrefs unchanged. These are runtime presentation fixtures, not human route-completion or Quest comfort evidence.

## Validation and delivery

- Unity6000.6.0f1:240 EditMode and37 PlayMode tests passed after the main revision. The final carry change reran all8 affected world/bearing PlayMode tests successfully. Final capture Console:0 errors.
- 21 Python tests pass; `git diff --check` passes. `check_repo.py` still reports exactly24 pre-existing unresolved TMP GUID findings.
- New tests cover the actual far-behind seed position, near-target visibility, runtime lift sampling, pickup versus resume, head-turn reacquisition, final-camera pose, menu/tracking/focus/pause/preferences and owned resource cleanup.
- Quest development build succeeded in40.761s,0 errors/6 warnings. APK: `builds/quest/VoarVR.apk`,100,182,490bytes, SHA256`3797aa32ab72690e015785a71bbe98e1d44f69f13be41a7d941743c41c7f517f`. Embedded source SHA256`81247fa3a4151c0badc135ad4aa1140262ce724976b31a901b804dbf9f8c3744`.
- Installation was attempted with `adb install -r` but failed because the Quest went offline. Reconnection remained offline; the user chose **"I'll test later"**. The APK is ready; no successful installation or startup of this revision is claimed. Existing app data was not cleared.

## Next headset check

Install the APK with app data preserved. Play Route Home with HUD hidden, turn away from the current objective, collect the seed, look away/back, switch view once and resume a checkpoint. Confirm that the wearer can identify the next destination and whether they possess the seed without prompting. Measure continuous stereo readability, pickup timing/audio/haptics, comfort and Quest frame delivery separately. No screenshot score establishes fun, exercise effectiveness or sustained hardware performance.
