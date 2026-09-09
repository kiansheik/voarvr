"""Reproducible stylized dragon, meters, -Y forward. Run in a fresh Blender scene.
Build creates its own scene; existing open scenes are preserved. Eye anchor (0,-.66,.62).
Nine bones, same rig contract as the duck (Root + Left/Right Upper/Forearm/Hand/Tip);
overlapping volumetric wing sections are rigid weighted to three joints.
"""
from pathlib import Path
import math
import sys
import bpy
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
from asset_common import validate, export_objects
from export_asset import export
ROOT = Path(__file__).resolve().parents[2]


def build(overwrite=False):
    scene = bpy.data.scenes.new('DragonAuthoring')
    bpy.context.window.scene = scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    # EXPORT is the pipeline contract; refuse to steal another asset's collection.
    if bpy.data.collections.get('EXPORT'):
        raise ValueError('Use a fresh instance or rename the previous EXPORT collection before construction')
    collection = bpy.data.collections.new('EXPORT')
    scene.collection.children.link(collection)
    mat = bpy.data.materials.new('DragonPalette')
    mat.diffuse_color = (.32, .13, .3, 1)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    attr = nodes.new('ShaderNodeVertexColor'); attr.layer_name = 'Color'
    mat.node_tree.links.new(attr.outputs['Color'], nodes.get('Principled BSDF').inputs['Base Color'])
    nodes.get('Principled BSDF').inputs['Roughness'].default_value = .75
    parts = {'DragonBody': [], 'DragonNeck': [], 'DragonFace': [], 'DragonWings': []}
    colors = {'plum': (.30, .11, .27, 1), 'belly': (.70, .58, .42, 1), 'hide': (.22, .08, .20, 1),
              'horn': (.16, .12, .10, 1), 'claw': (.12, .09, .08, 1), 'membrane': (.26, .06, .17, 1),
              'strut': (.20, .07, .19, 1), 'eye': (.86, .58, .05, 1), 'spike': (.42, .16, .34, 1)}

    def ellipsoid(name, center, scale, color, group='DragonBody', bone='Root', direction=None):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, location=center)
        obj = bpy.context.object; obj.name = name
        obj.scale = scale
        if direction is not None:
            obj.rotation_euler = Vector(direction).to_track_quat('X', 'Z').to_euler()
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        for owner in list(obj.users_collection): owner.objects.unlink(obj)
        collection.objects.link(obj)
        obj.data.materials.append(mat)
        attr = obj.data.color_attributes.new(name='Color', type='FLOAT_COLOR', domain='CORNER')
        for entry in attr.data: entry.color = colors[color]
        vg = obj.vertex_groups.new(name=bone); vg.add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
        for p in obj.data.polygons: p.use_smooth = True
        parts[group].append(obj)
        return obj

    # Long serpentine torso and tail, big enough to read as "huge" next to the duck.
    ellipsoid('Torso', (0, .10, .0), (.33, .62, .30), 'plum')
    ellipsoid('Belly', (0, .05, -.12), (.28, .55, .16), 'belly')
    for i in range(5):
        t = i / 4
        ellipsoid('BackSpike', (0, .45 - t * .95, .28 + t * .02), (.03, .07, .11 - t * .05), 'spike')
    ellipsoid('NeckBase', (0, -.55, .18), (.16, .22, .19), 'hide', 'DragonNeck')
    ellipsoid('NeckMid', (0, -.82, .34), (.135, .20, .155), 'plum', 'DragonNeck')
    ellipsoid('NeckUpper', (0, -1.05, .52), (.115, .17, .135), 'hide', 'DragonNeck')
    ellipsoid('Head', (0, -1.28, .68), (.155, .27, .155), 'plum', 'DragonFace')
    ellipsoid('Snout', (0, -1.58, .655), (.095, .17, .095), 'hide', 'DragonFace')
    ellipsoid('JawTip', (0, -1.76, .62), (.05, .06, .045), 'claw', 'DragonFace')
    for side in [-1, 1]:
        ellipsoid('Horn', (side * .085, -1.19, .84), (.026, .026, .16), 'horn', 'DragonFace',
                  direction=(side * .3, -.55, 1))
        ellipsoid('BrowRidge', (side * .11, -1.30, .77), (.05, .09, .025), 'hide', 'DragonFace')
        ellipsoid('Eye', (side * .105, -1.32, .74), (.024, .022, .024), 'eye', 'DragonFace')
        ellipsoid('EyeGlint', (side * .115, -1.335, .752), (.007, .007, .007), 'belly', 'DragonFace')
    for i in range(9):
        t = (i + 1) / 9
        radius = .155 * (1 - t) + .02
        ellipsoid('Tail', (0, .55 + t * 1.55, -.02 - t * .05), (radius, .155, radius), 'hide')
    ellipsoid('TailFin', (0, 2.05, -.06), (.02, .10, .13), 'spike', direction=(0, 1, 0))
    for side in [-1, 1]:
        ellipsoid('TuckedLeg', (side * .21, .28, -.28), (.075, .17, .075), 'hide')
        ellipsoid('Claw', (side * .21, .16, -.36), (.035, .06, .03), 'claw')

    # Anatomical chain in extended rest pose; each wing is 1.14 m from shoulder to tip -
    # roughly double the duck's, matching the "huge" size stat this species is meant to carry.
    arm = bpy.data.armatures.new('DragonSkeleton')
    rig = bpy.data.objects.new('DragonArmature', arm); collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig; rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    root = arm.edit_bones.new('Root'); root.head = (0, 0, 0); root.tail = (0, 0, .2)
    for side, label in [(1, 'Left'), (-1, 'Right')]:
        points = [(side * .22, -.05, .12), (side * .68, .04, .13), (side * 1.10, -.01, .09),
                  (side * 1.34, -.03, .03), (side * 1.38, -.03, .03)]
        parent = root
        for index, segment in enumerate(['Upper', 'Forearm', 'Hand', 'Tip']):
            b = arm.edit_bones.new(label + segment); b.head = points[index]; b.tail = points[index + 1]
            b.parent = parent; parent = b
    bpy.ops.object.mode_set(mode='OBJECT')
    for side, label in [(1, 'Left'), (-1, 'Right')]:
        ellipsoid('WingStrut', (side * .45, .045, .125), (.30, .21, .05), 'strut', 'DragonWings', label + 'Upper')
        ellipsoid('WingStrut', (side * .89, .015, .11), (.26, .17, .04), 'strut', 'DragonWings', label + 'Forearm')
        for i in range(6):
            x = side * (1.04 + i * .058); y = .05 + i * .03
            ellipsoid('Membrane', (x, y, .07 - i * .012), (.22 - i * .012, .06, .028), 'membrane',
                      'DragonWings', label + 'Hand', (side, .55 + i * .10, -.1))
        ellipsoid('WingClaw', (side * 1.40, -.05, .05), (.03, .05, .025), 'claw', 'DragonWings', label + 'Tip')
    for name, objects in parts.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects: obj.select_set(True)
        bpy.context.view_layer.objects.active = objects[0]
        bpy.ops.object.join(); obj = bpy.context.object; obj.name = name
        modifier = obj.modifiers.new('WingSkeleton', 'ARMATURE'); modifier.object = rig
        obj.parent = rig
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    validate(export_objects())
    source = ROOT / 'blender/source/DragonV1.blend'
    if source.exists() and not overwrite: raise FileExistsError('Refusing to overwrite authored DragonV1.blend')
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    export(ROOT / 'blender/exports/Dragon.fbx', overwrite=True)
    print('DRAGON', sum(len(o.data.polygons) for o in export_objects() if o.type == 'MESH'), 'faces', len(arm.bones), 'bones')


if __name__ == '__main__': build(overwrite='--overwrite' in sys.argv)
