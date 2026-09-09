# Open questions

- ~~First real Unity compilation, URP import and package resolution: not yet observed~~ Resolved 2026-09-08: Unity 6000.6.0f1 (project pin updated to match), compiles clean, `packages-lock.json` checked in. Android Build Support module is absent on this machine, so Quest/Android build validation is still open.
- Quest: verify tracking origin/recenter behavior, controller binding values, stereo rendering and comfort of the current camera/yaw approach.
- Blender: verify FBX meter scale/orientation through the actual Blender 4.5 -> Unity import path.
- Design: calibration/neutral arm pose, physical flap interpretation, target effort and accessibility/comfort options are undecided.
- Content: species, bird sizes, art style, biome and performance budgets need playtesting/profiling.
- Tools: Unity/Blender MCP implementation is intentionally unselected. No credentials/server configuration are part of the repo.
- Portability: Switch access and scope remain future M10 questions; no proprietary tooling assumed.

None of the design questions block the initial toolchain validation. Do not create speculative systems just to close these questions.
