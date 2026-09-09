"""Create a disposable metric branch study; preserves existing scenes and objects.

Run through Blender MCP with runpy.run_path, or with --background --factory-startup.
Output goes to ignored blender/generated/PrototypePerch.blend.
"""
from pathlib import Path
import math
import bpy
from mathutils import Vector, Quaternion

ROOT = Path(__file__).resolve().parents[2]


def build():
    if bpy.data.scenes.get("VoarVRPerchStudy") or bpy.data.collections.get("EXPORT"):
        raise ValueError("A perch study or EXPORT collection already exists; inspect it before replacing anything.")
    scene = bpy.data.scenes.new("VoarVRPerchStudy")
    bpy.context.window.scene = scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    collection = bpy.data.collections.new("EXPORT")
    scene.collection.children.link(collection)
    vertices, faces = [], []

    def branch(start, end, start_radius, end_radius, sides=8):
        start, end = Vector(start), Vector(end)
        direction = (end - start).normalized()
        tangent = direction.cross(Vector((0, 1, 0))).normalized()
        bitangent = direction.cross(tangent).normalized()
        offset = len(vertices)
        for center, radius in [(start, start_radius), (end, end_radius)]:
            for index in range(sides):
                angle = index * math.tau / sides
                vertices.append(tuple(center + radius * (math.cos(angle) * tangent + math.sin(angle) * bitangent)))
        faces.append(tuple(offset + i for i in reversed(range(sides))))
        faces.append(tuple(offset + sides + i for i in range(sides)))
        for index in range(sides):
            nxt = (index + 1) % sides
            faces.append((offset + index, offset + nxt, offset + sides + nxt, offset + sides + index))

    branch((0, 0, 0), (0.04, 0, 0.6), 0.10, 0.085)
    branch((-0.8, 0, 0.63), (0.75, 0.05, 0.72), 0.08, 0.05)
    branch((0.35, 0.03, 0.69), (0.55, 0.33, 0.9), 0.045, 0.02, 6)
    mesh = bpy.data.meshes.new("PrototypePerchMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("PrototypePerch_LOD0", mesh)
    collection.objects.link(obj)
    mat = bpy.data.materials.new("PrototypeBark")
    mat.diffuse_color = (0.28, 0.13, 0.045, 1)
    obj.data.materials.append(mat)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    for window in bpy.context.window_manager.windows:
        for area in window.screen.areas:
            if area.type == "VIEW_3D":
                area.spaces.active.shading.type = "SOLID"
                area.spaces.active.shading.color_type = "MATERIAL"
                area.spaces.active.region_3d.view_distance = 2.7
                area.spaces.active.region_3d.view_location = Vector((0, 0, 0.45))
                area.spaces.active.region_3d.view_rotation = Quaternion((0.88, 0.40, 0.12, 0.20)).normalized()
    output = ROOT / "blender/generated/PrototypePerch.blend"
    if output.exists():
        raise FileExistsError(f"Refusing to overwrite {output}")
    output.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(output))
    mesh.calc_loop_triangles()
    print({"source": str(output), "vertices": len(mesh.vertices), "triangles": len(mesh.loop_triangles),
           "dimensions_m": tuple(obj.dimensions), "pivot": "world-zero base center", "scene": scene.name})


if __name__ == "__main__":
    build()
