"""Reproducible stylized mallard, meters, -Y forward. Run in a fresh Blender scene.
Build creates its own scene; existing open scenes are preserved. Eye anchor (0,-.31,.27).
Nine bones; overlapping volumetric wing sections are rigid weighted to three joints.
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
    scene = bpy.data.scenes.new('DuckAuthoring')
    bpy.context.window.scene = scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0
    # EXPORT is the pipeline contract; refuse to steal another asset's collection.
    if bpy.data.collections.get('EXPORT'):
        raise ValueError('Use a fresh instance or rename the previous EXPORT collection before construction')
    collection = bpy.data.collections.new('EXPORT')
    scene.collection.children.link(collection)
    mat = bpy.data.materials.new('DuckPalette')
    mat.diffuse_color = (.52, .48, .38, 1)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    attr = nodes.new('ShaderNodeVertexColor'); attr.layer_name = 'Color'
    mat.node_tree.links.new(attr.outputs['Color'], nodes.get('Principled BSDF').inputs['Base Color'])
    nodes.get('Principled BSDF').inputs['Roughness'].default_value = .8
    parts = {'DuckBody': [], 'DuckNeck': [], 'DuckFace': [], 'DuckWings': []}
    colors = {'gray': (.44,.43,.37,1), 'breast': (.24,.095,.045,1), 'green': (.025,.19,.10,1),
              'bill': (.73,.48,.065,1), 'dark': (.035,.043,.035,1), 'cream': (.78,.76,.61,1),
              'blue': (.035,.09,.3,1)}
    def ellipsoid(name, center, scale, color, group='DuckBody', bone='Root', direction=None):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12, ring_count=6, location=center)
        obj = bpy.context.object; obj.name = name
        obj.scale = scale
        if direction is not None:
            obj.rotation_euler = Vector(direction).to_track_quat('X','Z').to_euler()
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
    ellipsoid('Torso',(0,.07,.0),(.145,.29,.15),'gray')
    ellipsoid('Breast',(0,-.11,.035),(.14,.15,.145),'breast')
    ellipsoid('Neck',(0,-.20,.16),(.073,.078,.13),'green','DuckNeck')
    ellipsoid('Collar',(0,-.20,.093),(.080,.079,.025),'cream','DuckNeck')
    ellipsoid('Head',(0,-.285,.265),(.092,.10,.091),'green','DuckFace')
    ellipsoid('Bill',(0,-.40,.241),(.076,.10,.022),'bill','DuckFace')
    ellipsoid('BillTip',(0,-.486,.242),(.025,.012,.012),'dark','DuckFace')
    for side in [-1,1]:
        ellipsoid('Eye',(side*.082,-.32,.288),(.013,.012,.013),'dark','DuckFace')
        ellipsoid('EyeGlint',(side*.091,-.325,.292),(.004,.004,.004),'cream','DuckFace')
        ellipsoid('TuckedFoot',(side*.075,.15,-.123),(.040,.075,.013),'bill')
    for i in range(5):
        ellipsoid('Tail',( (i-2)*.033,.32,-.005),(.031,.13,.020),'dark')
    # Anatomical chain in extended rest pose; each wing is .58 m from shoulder to tip.
    arm = bpy.data.armatures.new('DuckSkeleton')
    rig = bpy.data.objects.new('DuckArmature',arm); collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig; rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    root = arm.edit_bones.new('Root'); root.head=(0,0,0); root.tail=(0,0,.1)
    for side, label in [(1,'Left'),(-1,'Right')]:
        points = [(side*.11,-.025,.055),(side*.34,.015,.055),(side*.56,-.005,.04),(side*.69,-.015,.015),(side*.71,-.015,.015)]
        parent=root
        for index, segment in enumerate(['Upper','Forearm','Hand','Tip']):
            b=arm.edit_bones.new(label+segment); b.head=points[index]; b.tail=points[index+1]; b.parent=parent; parent=b
    bpy.ops.object.mode_set(mode='OBJECT')
    for side,label in [(1,'Left'),(-1,'Right')]:
        ellipsoid('UpperWing',(side*.23,.025,.055),(.15,.11,.047),'gray','DuckWings',label+'Upper')
        ellipsoid('ForeWing',(side*.445,.025,.04),(.145,.10,.036),'gray','DuckWings',label+'Forearm')
        ellipsoid('Speculum',(side*.445,.06,.063),(.11,.04,.012),'blue','DuckWings',label+'Forearm')
        ellipsoid('SpeculumEdge',(side*.445,.10,.055),(.105,.012,.011),'cream','DuckWings',label+'Forearm')
        for i in range(7):
            x=side*(.50+i*.028); y=.07+i*.006
            ellipsoid('Primary',(x,y,.025-i*.003),(.12-i*.006,.029,.014),'dark','DuckWings',label+'Hand', (side,.40+i*.13,0))
        for i in range(5):
            ellipsoid('Secondary',(side*(.26+i*.043),.12,.028),(.029,.093,.016),'gray','DuckWings',label+'Forearm' if i>1 else label+'Upper')
    for name, objects in parts.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in objects: obj.select_set(True)
        bpy.context.view_layer.objects.active=objects[0]
        bpy.ops.object.join(); obj=bpy.context.object; obj.name=name
        modifier=obj.modifiers.new('WingSkeleton','ARMATURE'); modifier.object=rig
        obj.parent=rig
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
    bpy.context.view_layer.objects.active=rig
    validate(export_objects())
    source=ROOT/'blender/source/DuckV1.blend'
    if source.exists() and not overwrite: raise FileExistsError('Refusing to overwrite authored DuckV1.blend')
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    export(ROOT/'blender/exports/Duck.fbx', overwrite=True)
    print('DUCK', sum(len(o.data.polygons) for o in export_objects() if o.type=='MESH'), 'faces',len(arm.bones),'bones')

if __name__ == '__main__': build(overwrite='--overwrite' in sys.argv)
