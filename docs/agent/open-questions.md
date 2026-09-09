# Open questions

- ~~First real Unity compilation, URP import and package resolution~~ Resolved 2026-09-08 with Unity 6000.6.0f1.
- ~~Android Build Support / Quest install-launch~~ Resolved 2026-09-09: a Quest 3 was connected and the current build installs, launches, stays alive and reaches BirdFlight over adb (see [quest-recovery handoff](session-handoffs/2026-09-09-quest-recovery.md)).
- Quest: worn v2 feedback confirmed good Dragon art but unsustainable effort. Retest v3 normalized effort/forward thrust, A restart+capture, Meta in-place capture and left Menu return. Controller binding values, tracking/recenter, fatigue and whether CharacterSelect auto-advances still need physical evidence.
- `Assets/TextMesh Pro/` is an incomplete, unused import (pulled in as a side effect of an earlier XRI sample import) causing 24 `check_repo.py` unresolved-GUID findings; recommend deleting it as its own change, not blocking anything today.
- ~~Blender FBX scale/orientation and skin roundtrip~~ Resolved with Blender 5.2.1 LTS static and nine-bone rig smoke tests plus Unity import/deformation checks.
- Design: calibration/neutral arm pose, physical flap interpretation, target effort and accessibility/comfort options are undecided.
- Content: Dragon and selection are tested in Editor and built for Quest. Environment richness, altitude wind cues and performance budgets need more playtesting/profiling.
- Editor rig/catalog verification is complete; no Configure Characters step is pending.
- Tools: Unity MCP is useful for the live editor but remains optional; no credentials/server configuration are part of the repo.
- Portability: Switch access and scope remain future M10 questions; no proprietary tooling assumed.

None of the design questions block the initial toolchain validation. Do not create speculative systems just to close these questions.
