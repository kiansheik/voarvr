"""Shared static and armature mesh conventions. Import inside Blender's Python interpreter."""
import math
import re
import bpy

NAME = re.compile(r"^[A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)*(?:_LOD[0-9]+)?$")


def export_objects():
    collection = bpy.data.collections.get("EXPORT")
    if collection is None:
        raise ValueError("Create an EXPORT collection containing the meshes and optional armature to publish.")
    objects = sorted(collection.all_objects, key=lambda obj: obj.name)
    if not objects:
        raise ValueError("EXPORT collection is empty.")
    return objects


def validate(objects):
    errors = []
    units = bpy.context.scene.unit_settings
    if units.system != "METRIC" or not math.isfinite(units.scale_length) or abs(units.scale_length - 1.0) > 1e-6:
        errors.append("Scene units must be Metric with Unit Scale 1.0 (one unit = one meter).")
    lods = {}
    for obj in objects:
        if not NAME.fullmatch(obj.name):
            errors.append(f"{obj.name}: use names such as BirdBody or OakPerch_LOD0; no spaces/dots.")
        if obj.type not in {"MESH", "ARMATURE"}:
            errors.append(f"{obj.name}: only mesh and armature objects are supported.")
            continue
        if obj.parent is not None and (obj.parent not in objects or obj.parent.type != "ARMATURE"):
            errors.append(f"{obj.name}: parent must be an exported armature.")
        if any(not math.isfinite(value) for values in (obj.location, obj.rotation_euler, obj.scale) for value in values):
            errors.append(f"{obj.name}: transforms must contain only finite values.")
        if any(abs(v) > 1e-6 for v in obj.location):
            errors.append(f"{obj.name}: place object origin at the world origin; edit mesh geometry for offsets.")
        if obj.rotation_mode != "XYZ" or any(abs(v) > 1e-6 for v in obj.rotation_euler):
            errors.append(f"{obj.name}: apply rotation and use XYZ rotation mode.")
        if any(abs(v - 1.0) > 1e-6 for v in obj.scale):
            errors.append(f"{obj.name}: apply scale before publishing.")
        if obj.type == "ARMATURE":
            if not obj.data.bones or len(obj.data.bones) > 64:
                errors.append(f"{obj.name}: expected 1..64 bones.")
            for bone in obj.data.bones:
                if not NAME.fullmatch(bone.name) or bone.length < 1e-5:
                    errors.append(f"{obj.name}: invalid bone {bone.name}.")
                if any(not math.isfinite(v) for p in (bone.head_local, bone.tail_local) for v in p):
                    errors.append(f"{bone.name}: non-finite bone coordinates.")
            if any(abs(b.matrix_basis[i][j] - (1 if i == j else 0)) > 1e-5
                   for b in obj.pose.bones for i in range(4) for j in range(4)):
                errors.append(f"{obj.name}: export in rest pose.")
            continue
        armatures = [m for m in obj.modifiers if m.type == "ARMATURE"]
        if armatures:
            rig = armatures[0].object
            if len(armatures) != 1 or rig not in objects or rig.type != "ARMATURE" or obj.parent != rig:
                errors.append(f"{obj.name}: skin must reference its exported parent armature.")
            else:
                for vertex in obj.data.vertices:
                    weights = [g.weight for g in vertex.groups
                               if obj.vertex_groups[g.group].name in rig.data.bones]
                    if not weights or len(weights) > 4 or any(not math.isfinite(w) or w < 0 for w in weights) or abs(sum(weights)-1) > .001:
                        errors.append(f"{obj.name}: vertex {vertex.index} needs normalized 1..4 bone weights.")
                        break
        elif obj.parent is not None:
            errors.append(f"{obj.name}: armature parenting requires an armature modifier.")
        if not obj.data.polygons:
            errors.append(f"{obj.name}: mesh has no faces.")
        if any(not math.isfinite(v) for vertex in obj.data.vertices for v in vertex.co):
            errors.append(f"{obj.name}: mesh has non-finite coordinates.")
        match = re.fullmatch(r"(.+)_LOD([0-9]+)", obj.name)
        if match:
            lods.setdefault(match.group(1), []).append(int(match.group(2)))
    for name, levels in lods.items():
        if sorted(levels) != list(range(max(levels) + 1)):
            errors.append(f"{name}: LOD levels must be contiguous starting at LOD0.")
    if errors:
        raise ValueError("Asset validation failed:\n" + "\n".join(errors))
    return objects
