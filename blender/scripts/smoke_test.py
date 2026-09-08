"""Run with --background --factory-startup --python-exit-code 1 --python."""
from pathlib import Path
import sys
import tempfile
import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
from asset_common import export_objects, validate
from export_asset import export

# This script creates a disposable in-memory scene. Never saves a source asset.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system = "METRIC"
bpy.context.scene.unit_settings.scale_length = 1.0
collection = bpy.data.collections.new("EXPORT")
bpy.context.scene.collection.children.link(collection)
bpy.ops.mesh.primitive_cube_add(size=1.0)
obj = bpy.context.object
obj.name = "MeterCube_LOD0"
for owner in list(obj.users_collection):
    owner.objects.unlink(obj)
collection.objects.link(obj)
validate(export_objects())
obj.scale.x = 2.0
try:
    validate(export_objects())
except ValueError:
    pass
else:
    raise AssertionError("Validation accepted unapplied scale")
obj.scale.x = 1.0
with tempfile.TemporaryDirectory(prefix="voarvr-blender-") as temporary:
    output = Path(temporary) / "MeterCube.fbx"
    export(output)
    assert obj.name == "MeterCube_LOD0" and tuple(obj.scale) == (1.0, 1.0, 1.0)
    try:
        export(output)
    except FileExistsError:
        pass
    else:
        raise AssertionError("Exporter overwrote an existing output without permission")
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(output))
    meshes = [item for item in bpy.context.selected_objects if item.type == "MESH"]
    assert len(meshes) == 1
    assert all(abs(value - 1.0) < 1e-4 for value in meshes[0].dimensions), meshes[0].dimensions
print("PASS: validation, source preservation and one-meter FBX roundtrip")
