"""Shared static-mesh conventions. Import inside Blender's Python interpreter."""
import math
import re
import bpy

NAME = re.compile(r"^[A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)*(?:_LOD[0-9]+)?$")


def export_objects():
    collection = bpy.data.collections.get("EXPORT")
    if collection is None:
        raise ValueError("Create an EXPORT collection containing the static meshes to publish.")
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
        if obj.type != "MESH":
            errors.append(f"{obj.name}: only static meshes are supported by the initial exporter.")
            continue
        if obj.parent is not None:
            errors.append(f"{obj.name}: export meshes must be unparented; rigged export is future work.")
        if any(not math.isfinite(value) for values in (obj.location, obj.rotation_euler, obj.scale) for value in values):
            errors.append(f"{obj.name}: transforms must contain only finite values.")
        if any(abs(v) > 1e-6 for v in obj.location):
            errors.append(f"{obj.name}: place object origin at the world origin; edit mesh geometry for offsets.")
        if obj.rotation_mode != "XYZ" or any(abs(v) > 1e-6 for v in obj.rotation_euler):
            errors.append(f"{obj.name}: apply rotation and use XYZ rotation mode.")
        if any(abs(v - 1.0) > 1e-6 for v in obj.scale):
            errors.append(f"{obj.name}: apply scale before publishing.")
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
