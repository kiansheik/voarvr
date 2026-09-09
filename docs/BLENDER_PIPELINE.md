# Blender pipeline

Use the tested Blender 5.2.1 LTS baseline and its bundled Python. The scripts support static meshes and skinned meshes with a single exported armature; animation, LODGroup creation and material/texture baking remain future workflows. Never use `pip install bpy` as a substitute for the chosen Blender build.

```text
blender/source/*.blend (authored, LFS)
    -> validate_asset.py
    -> export_asset.py
blender/exports/*.fbx (ignored staging output)
    -> inspect and deliberately promote
unity/Assets/Art/Models/*.fbx + .meta (curated runtime source, LFS for FBX)
```

## Asset contract

- Scene units: Metric, Unit Scale 1.0. One Blender unit = one meter; Unity should import at scale 1 with unit conversion enabled.
- Put only the meshes to publish in an `EXPORT` collection. Other collections may hold cameras/reference/work in progress.
- Names: descriptive ASCII PascalCase with optional underscores, such as `OakPerch`, `BirdBody_LOD0`, `BirdBody_LOD1`. Blender's automatic `.001` names and spaces fail validation.
- Static export objects are unparented meshes. A skinned mesh may have exactly one exported armature parent and one matching Armature modifier. Set object/armature transforms to identity; use XYZ rotation mode. Place geometry in Edit Mode to establish the pivot.
- Blender +Z is up and authored forward is -Y; FBX exports -Z forward / +Y up. Verify orientation in Unity with a visibly asymmetric model and a meter reference; a cube alone cannot prove heading.
- Optional LOD suffixes must start at LOD0 and be contiguous within each named group. The scripts don't decimate or choose distance thresholds; Unity LODGroup wiring is separate future work.
- Every skinned vertex must have finite normalized weight across one to four valid exported deform bones. The current guardrail is 64 bones per exported armature. Rest transforms must be finite and the source must not be saved in a posed state.
- Export duplicates the selected meshes and required armature, remaps parents/modifiers, applies eligible modifiers, disables animation and leaf bones, and strips texture paths. Materials/textures need deliberate Unity authoring; FBX is not a complete material portability solution.

## Commands

From the root, set `BLENDER` to the executable in your actual Blender installation:

```sh
export BLENDER="/Applications/Blender.app/Contents/MacOS/Blender"
"$BLENDER" --version
"$BLENDER" --background blender/source/OakPerch.blend --python-exit-code 1 --python blender/scripts/validate_asset.py
"$BLENDER" --background blender/source/OakPerch.blend --python-exit-code 1 --python blender/scripts/export_asset.py -- --output blender/exports/OakPerch.fbx
```

Create `OakPerch.blend` first; it's an example source, not a bundled asset. Validation reports names, transforms, units, empty geometry, non-finite vertices and LOD sequence failures. Fix authored transforms in Blender (`Object > Apply > Rotation & Scale`, then position vertices around the desired origin), or use a task-specific checked-in `bpy` script. No source file is silently saved by validation/export.

The duck is a reproducible rigged asset. Its current authored source is `blender/source/DuckV1.blend`; `create_duck.py` builds the stylized mallard, nine-bone rig and vertex-color meshes. Export it to ignored staging, then promote the reviewed FBX:

```sh
"$BLENDER" --background --factory-startup --python-exit-code 1 --python blender/scripts/create_duck.py -- --output blender/source/DuckV1.blend
"$BLENDER" --background blender/source/DuckV1.blend --python-exit-code 1 --python blender/scripts/export_asset.py -- --output blender/exports/Duck.fbx --overwrite
cp blender/exports/Duck.fbx unity/Assets/Art/Models/Duck.fbx
```

The exporter validates first, duplicates source meshes temporarily, applies rotation/scale to the copies and exports FBX. It restores source names/selection and removes temporary objects. Existing output is refused unless `--overwrite` is supplied. `--python-exit-code 1` makes script exceptions fail the shell command rather than merely print a Blender traceback.

Inspect the export, then promote deliberately:

```sh
cp blender/exports/OakPerch.fbx unity/Assets/Art/Models/OakPerch.fbx
```

Check destination changes first when replacing an existing asset. Preserve its `.meta`; Unity should update its importer while keeping prefab/scene references stable. Check dimensions, orientation, normals and pivot. Save importer settings with the asset. Avoid direct `.blend` import into Unity because that introduces an implicit local Blender executable dependency.

## Repeatable validation

This creates a disposable cube, verifies rejection of unapplied scale, checks source preservation and performs a one-meter FBX export/import roundtrip. It does not save a `.blend` or require external assets:

```sh
"$BLENDER" --background --factory-startup --python-exit-code 1 --python blender/scripts/smoke_test.py
"$BLENDER" --background blender/source/DuckV1.blend --python-exit-code 1 --python blender/scripts/rig_smoke_test.py
```

The rig smoke rejects invalid weights, posed source and dangling rigs, then roundtrips the nine-bone meter-scale duck and proves a hand-bone pose deforms its mesh. Run both smoke tests after pipeline changes. Generated studies go in ignored `blender/generated/`; curated authored sources belong in `blender/source/`. Keep script inputs/seed explicit for future procedural assets. MCP execution should invoke these same contracts and leave repeatable scripts behind.
