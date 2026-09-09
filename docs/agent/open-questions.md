# Open questions

- ~~First real Unity compilation, URP import and package resolution~~ Resolved 2026-09-08 with Unity 6000.6.0f1.
- Android Build Support is installed and the development APK boundary is tracked in current state. Quest install/launch awaits a connected authorized headset.
- Quest: verify tracking origin/recenter behavior, controller binding values, stereo rendering and comfort of the current camera/yaw approach.
- ~~Blender FBX scale/orientation and skin roundtrip~~ Resolved with Blender 5.2.1 LTS static and nine-bone rig smoke tests plus Unity import/deformation checks.
- Design: calibration/neutral arm pose, physical flap interpretation, target effort and accessibility/comfort options are undecided.
- Content: a second species (Dragon) and a stat-card character-select screen now exist untested in-Editor; art style, biome and performance budgets still need playtesting/profiling. Whether more than five stats or non-linear stat curves are worth it is undecided - do not add either speculatively.
- Verify in-Editor: run `VoarVR/Configure Characters`, confirm `Dragon.fbx` imports and looks right next to the duck, then run the EditMode/PlayMode suites (`BootstrapSmokeTests` now clicks through the select screen).
- Tools: Unity MCP is useful for the live editor but remains optional; no credentials/server configuration are part of the repo.
- Portability: Switch access and scope remain future M10 questions; no proprietary tooling assumed.

None of the design questions block the initial toolchain validation. Do not create speculative systems just to close these questions.
