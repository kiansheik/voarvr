"""Validated FBX export without saving or modifying the source .blend on disk."""
import argparse
from pathlib import Path
import sys
import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from asset_common import export_objects, validate


def export(output, overwrite=False):
    output = Path(output).resolve()
    if output.suffix.lower() != ".fbx":
        raise ValueError("Output must have an .fbx extension.")
    if output.exists() and not overwrite:
        raise FileExistsError(f"Refusing to overwrite {output}; pass --overwrite intentionally.")
    objects = validate(export_objects())
    output.parent.mkdir(parents=True, exist_ok=True)
    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    original_selected = list(bpy.context.selected_objects)
    original_active = bpy.context.view_layer.objects.active
    copies = []
    temporary = bpy.data.collections.new("VoarVRExportTemporary")
    bpy.context.scene.collection.children.link(temporary)
    try:
        bpy.ops.object.select_all(action="DESELECT")
        # Duplicate data so transform application never mutates authored source meshes.
        for obj in objects:
            clone = obj.copy()
            clone.data = obj.data.copy()
            temporary.objects.link(clone)
            clone.hide_set(False)
            clone.hide_viewport = False
            clone.hide_select = False
            clone.select_set(True)
            copies.append((clone, obj, obj.name))
        remap = {original: clone for clone, original, name in copies}
        for clone, original, name in copies:
            clone.parent = remap.get(original.parent)
            for modifier in clone.modifiers:
                if modifier.type == "ARMATURE":
                    modifier.object = remap[modifier.object]
        bpy.context.view_layer.objects.active = copies[0][0]
        bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
        # Blender adds .001 to duplicated object names; restore export names temporarily.
        for clone, original, name in copies:
            original.name = name + "__SourceDuringExport"
            clone.name = name
        result = bpy.ops.export_scene.fbx(
            filepath=str(output), use_selection=True, object_types={"MESH", "ARMATURE"},
            global_scale=1.0, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
            axis_forward="-Z", axis_up="Y", use_mesh_modifiers=not any(o.type == "ARMATURE" for o in objects),
            add_leaf_bones=False, bake_anim=False, path_mode="STRIP",
            use_custom_props=False,
        )
        if result != {"FINISHED"} or not output.is_file() or output.stat().st_size == 0:
            raise RuntimeError("FBX export did not produce a nonempty file.")
    finally:
        for clone, original, name in copies:
            mesh = clone.data
            bpy.data.objects.remove(clone, do_unlink=True)
            if isinstance(mesh, bpy.types.Armature):
                bpy.data.armatures.remove(mesh)
            else:
                bpy.data.meshes.remove(mesh)
        for clone, original, name in copies:
            original.name = name
        bpy.data.collections.remove(temporary)
        for obj in original_selected:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = original_active
    print(f"EXPORTED: {output}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    parser.add_argument("--overwrite", action="store_true")
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else [])
    export(args.output, args.overwrite)
