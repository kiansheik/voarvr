# Asset guidelines

Standalone Quest 3 VR is the first performance target. **No strict triangle, draw-call, memory or texture budget has been established yet.** The following are starting conventions; profile representative scenes on hardware before turning them into numeric budgets.

- Use only the geometry needed for silhouette at its viewing distance. Avoid dense subdivisions on small/distant birds, insects or foliage.
- Start with modest texture dimensions, share atlases where practical and justify higher resolution with in-headset inspection. Use Android-appropriate compression (evaluate ASTC) and mipmaps for 3D textures; configure per-platform import settings.
- Favor simple opaque URP materials. Transparent foliage, layered materials, screen-space effects and many unique materials can be costly in stereo. The prototype uses a single small unlit shader and three colored materials.
- Add LODs for assets whose projected size changes substantially; use `_LOD0`, `_LOD1`, etc. Distances and reduction ratios remain undecided. Validate popping in headset.
- Reduce draw calls through shared materials, suitable mesh grouping and GPU instancing for repeated objects. Consider static batching only for truly static geometry and account for memory tradeoffs. A material's instancing checkbox alone is not proof of fewer draw calls.
- Keep pivots, scale and forward axes consistent. Author collision meshes separately when landing/collision is implemented; do not assume render geometry is an appropriate collider.
- `.blend` and layered/HDR art sources use LFS. Ordinary small PNG/JPEG textures stay in normal Git unless size justifies a targeted LFS rule. FBX and WAV are LFS. Do not add downloaded asset packs, paid assets or third-party source libraries to populate empty directories.
- Record source/license/provenance for each future third-party asset and put it under `Assets/ThirdParty/` only when deliberately approved for the task.

Use Unity Profiler and rendering diagnostics on Quest to measure CPU/GPU frame time, memory and draw calls before optimizing. Desktop frame rate is not a standalone VR acceptance test. Artistic style, refresh-rate target and final budgets remain open in [open questions](agent/open-questions.md).
