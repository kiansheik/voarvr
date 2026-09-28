# Open questions

- ~~First real Unity compilation, URP import and package resolution~~ Resolved 2026-09-08 with Unity 6000.6.0f1.
- ~~Android Build Support / Quest install-launch~~ Resolved 2026-09-09: a Quest 3 was connected and the current build installs, launches, stays alive and reaches BirdFlight over adb (see [quest-recovery handoff](session-handoffs/2026-09-09-quest-recovery.md)).
- ~~September 12 gameplay follow-through~~ Implemented locally: typed moth/value ribbon, mission counts, explicit session results and per-player records, supported right-stick yaw, richer contact/foliage response, branch/compound landmark collision, non-overlapping/HUD-independent guidance, Duck/Dragon trigger poses, diagram-led safe calibration and five short data-driven courses. [Original evidence](../reviews/2026-09-12-quest-playtest-next-round.md), [implementation handoff](session-handoffs/2026-09-12-playtest-features-implementation.md). The Quest wearer acceptance items below remain open; earlier unattended CharacterSelect auto-advance is separate.
- `Assets/TextMesh Pro/` is an incomplete, unused import (pulled in as a side effect of an earlier XRI sample import) causing 24 `check_repo.py` unresolved-GUID findings; recommend deleting it as its own change, not blocking anything today.
- ~~Blender FBX scale/orientation and skin roundtrip~~ Resolved with Blender 5.2.1 LTS static and nine-bone rig smoke tests plus Unity import/deformation checks.
- Design: deliberate neutral calibration, physical flaps, Assisted flight and separate advanced controls are implemented. Latest passive-pitch neutral fix is installed; relaxed-grip wearer acceptance, target effort and accessible reach/seated options remain open.
- Content: A Route Home now adds a persistent restored garden, seed carry and optional quiet-view guidance. Broader landmark variety, close material/animation finish, companion/mastery content and human navigation acceptance remain open. A body sphere can allow cosmetic wing/neck overlap.
- Performance: cooperative1.5 ms generation meets measured warm desktop boundaries, but atomic mesh cooking and cold/restart generation can overshoot. Sustained Quest streaming, GPU/batch cost and wearer-perceived hitches remain gates. No production LOD or full simulation-clock/vertical double precision yet.
- Editor rig/catalog verification is complete; no Configure Characters step is pending.
- Tools: Unity MCP is useful for the live editor but remains optional; no credentials/server configuration are part of the repo.
- Portability: Switch access and scope remain future M10 questions; no proprietary tooling assumed.

None of the design questions block the initial toolchain validation. Do not create speculative systems just to close these questions.

## Flight Game v1 acceptance questions

- The 2026-09-12 Magpie/Skyward capture predates the new feature pass. Its remote/local hashes now match, but it does not validate the new ribbon, session flow, collision/animation work or courses. The Quest was offline at the end of the latest check; do not describe current connectivity without a fresh check.
- Input-only Skyward completes in 180.4s, but the actual wearer spent 19m 24s on the arch stage without approaching within 617 m. HUD-independent direction/distance and world guidance are now implemented; a first-time wearer must still prove that they identify and approach the target without coaching.
- Strict art/thermal-readability gates are not yet met; bounded prototype geometry needs composition/lighting/landmark polish.
- Natural-start acrobatic loop technique needs coaching and wearable tests; energetic-entry fixtures are insufficient user-usability proof.
- Ridge Journey and local bests provide initial progression, not a full campaign, cosmetic unlock system or journal.

## Future game production decisions

- [Professional critique and W backlog](../reviews/2026-09-10-game-critique-and-roadmap.md) remains the broader production proposal. [A Route Home implementation](../reviews/2026-09-10-route-home-implementation.md) now covers the first story consequence, durable/rest-friendly progress, quiet help and focused presentation. Broader campaign/mastery/asset direction remains future work.
- Highest-value validation: build/install the current tree, then test HUD-off Skyward comprehension; ribbon/stereo text size; Left Menu session finalization and profile history; ground-turn and calibration comfort; collider openings/contact feel; Duck/Dragon trigger poses; and real-controller completion, ranking and rest/retry for all five 15–30s courses. Record sustained Quest frame pacing separately from screenshot quality.
