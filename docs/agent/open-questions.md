# Open questions

- ~~First real Unity compilation, URP import and package resolution~~ Resolved 2026-09-08 with Unity 6000.6.0f1.
- ~~Android Build Support / Quest install-launch~~ Resolved 2026-09-09: a Quest 3 was connected and the current build installs, launches, stays alive and reaches BirdFlight over adb (see [quest-recovery handoff](session-handoffs/2026-09-09-quest-recovery.md)).
- Latest wearer requests HUD and more useful thermals. Flight instruments/Assisted feathering now implemented; worn-test readability/distraction, wing wakes and lift entries for both birds. Also retest: calibrated modest downward look, third-person default, Dragon glide, physical braking/ground+elevated contact, moving lift navigation and sustained chunk-streaming performance. Worn comfort is not established by editor or adb checks. Earlier unattended CharacterSelect auto-advance remains a separate unresolved observation.
- `Assets/TextMesh Pro/` is an incomplete, unused import (pulled in as a side effect of an earlier XRI sample import) causing 24 `check_repo.py` unresolved-GUID findings; recommend deleting it as its own change, not blocking anything today.
- ~~Blender FBX scale/orientation and skin roundtrip~~ Resolved with Blender 5.2.1 LTS static and nine-bone rig smoke tests plus Unity import/deformation checks.
- Design: calibration/neutral arm pose, physical flap interpretation, target effort and accessibility/comfort options are undecided.
- Content: deterministic endless horizontal chunks, finite 3D air and contact landings are implemented/tested. Repeated cuboid art, short cyan trail interpretation and a gliding pose while perched remain prototype limitations. A body sphere can allow cosmetic wing/neck overlap.
- Performance: cooperative1.5 ms generation meets measured warm desktop boundaries, but atomic mesh cooking and cold/restart generation can overshoot. Sustained Quest streaming, GPU/batch cost and wearer-perceived hitches remain gates. No production LOD or full simulation-clock/vertical double precision yet.
- Editor rig/catalog verification is complete; no Configure Characters step is pending.
- Tools: Unity MCP is useful for the live editor but remains optional; no credentials/server configuration are part of the repo.
- Portability: Switch access and scope remain future M10 questions; no proprietary tooling assumed.

None of the design questions block the initial toolchain validation. Do not create speculative systems just to close these questions.

## Flight Game v1 acceptance questions

- Quest is offline: wearer comfort,72Hz/thermal overhead, APK launch and new telemetry pull remain unverified.
- Input-only Skyward pilot completes in180.4s; target10–20minute human journey pacing is unverified.
- Strict art/thermal-readability gates are not yet met; bounded prototype geometry needs composition/lighting/landmark polish.
- Natural-start acrobatic loop technique needs coaching and wearable tests; energetic-entry fixtures are insufficient user-usability proof.
- Ridge Journey and local bests provide initial progression, not a full campaign, cosmetic unlock system or journal.
