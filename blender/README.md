# Blender workspace

- `source/`: authored Blender 4.5 LTS `.blend` files, tracked with Git LFS.
- `scripts/`: shared static-mesh validator, FBX exporter and disposable roundtrip test.
- `generated/`: ignored procedural studies/intermediate data.
- `exports/`: ignored FBX staging output; promote approved assets into Unity.

See [Blender pipeline](../docs/BLENDER_PIPELINE.md) for units, naming, pivots, LODs and exact headless commands. No binary art is bundled. Blender MCP is optional; scripts work with a normal Blender installation.
