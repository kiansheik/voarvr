"""Reproducible stylized dragon, meters, -Y forward. Run in a fresh Blender scene.
Build creates its own scene; existing open scenes are preserved. Eye anchor (0,-.66,.62).
Nine bones, same rig contract as the duck (Root + Left/Right Upper/Forearm/Hand/Tip);
continuous thick membranes blend across the three wing joints.
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
    colors = {'plum': (.46, .085, .19, 1), 'belly': (.70, .58, .42, 1), 'hide': (.27, .035, .085, 1),
              'horn': (.16, .12, .10, 1), 'claw': (.12, .09, .08, 1), 'membrane': (.72, .075, .17, 1),
              'strut': (.105, .018, .045, 1), 'eye': (.86, .58, .05, 1), 'spike': (.82, .55, .29, 1)}

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

    def mesh_part(name, vertices, faces, color, group, weights=None):
        import bmesh
        mesh = bpy.data.meshes.new(name + 'Mesh')
        mesh.from_pydata(vertices, [], faces); mesh.update()
        bm = bmesh.new(); bm.from_mesh(mesh)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(mesh); bm.free()
        obj = bpy.data.objects.new(name, mesh); collection.objects.link(obj)
        mesh.materials.append(mat)
        attr = mesh.color_attributes.new(name='Color', type='FLOAT_COLOR', domain='CORNER')
        for entry in attr.data: entry.color = colors[color]
        for face in mesh.polygons: face.use_smooth = True
        groups = {}
        for index, vertex in enumerate(vertices):
            for bone, weight in (weights(vertex) if weights else {'Root': 1.0}).items():
                if weight <= 0: continue
                if bone not in groups: groups[bone] = obj.vertex_groups.new(name=bone)
                groups[bone].add([index], weight, 'REPLACE')
        parts[group].append(obj)
        return obj

    def tube(name, centers, radii, color, group='DragonBody', weights=None, sides=10):
        # Shared rings form a connected tapered skin instead of overlapping beads.
        points = [Vector(p) for p in centers]
        vertices, faces = [], []
        for i, point in enumerate(points):
            tangent = (points[min(i+1,len(points)-1)] - points[max(0,i-1)]).normalized()
            reference = Vector((0,0,1)) if abs(tangent.z) < .9 else Vector((0,1,0))
            u = tangent.cross(reference).normalized(); v = tangent.cross(u).normalized()
            for j in range(sides):
                angle = j * math.tau / sides
                vertices.append(tuple(point + radii[i] * (u*math.cos(angle)+v*math.sin(angle))))
        for i in range(len(points)-1):
            for j in range(sides):
                n=(j+1)%sides; a=i*sides; b=(i+1)*sides
                faces.append((a+j,a+n,b+n,b+j))
        faces.extend([tuple(reversed(range(sides))), tuple((len(points)-1)*sides+j for j in range(sides))])
        return mesh_part(name, vertices, faces, color, group, weights)

    def wing_weights(label, vertex):
        x = abs(vertex[0])
        def blend(center, width):
            t = max(0.0,min(1.0,(x-center+width)/(2*width)))
            return t*t*(3-2*t)
        root = 1-blend(.31,.16)
        lower = blend(1.35,.38); hand = blend(2.45,.36)
        return {'Root': root, label+'Upper': (1-root)*(1-lower),
                label+'Forearm': (1-root)*lower*(1-hand), label+'Hand': (1-root)*hand}

    def interpolate(keys, x):
        for a,b in zip(keys,keys[1:]):
            if x <= b[0]:
                t=max(0.0,(x-a[0])/(b[0]-a[0]))
                return a[1]+(b[1]-a[1])*t
        return keys[-1][1]

    def wing_surface(x, t):
        lead = interpolate([(.24,-.05),(1.35,0),(2.45,.08),(3.43,.24)],x)
        z = interpolate([(.24,.14),(1.35,.22),(2.45,.10),(3.43,-.06)],x)
        trailing = [(.24,.62),(.95,1.30),(1.70,1.65),(2.45,1.55),(3.10,1.0),(3.43,.25)]
        rear = interpolate(trailing,x)
        for a,b in zip(trailing,trailing[1:]):
            if a[0] <= x <= b[0]:
                # Inward curves between supported finger tips form the bat scallops.
                rear -= .24 * math.sin(math.pi*(x-a[0])/(b[0]-a[0]))
                break
        return (x,lead+(rear-lead)*t,z-.16*t+.075*math.sin(math.pi*t))

    # Long serpentine torso and tail, big enough to read as "huge" next to the duck.
    ellipsoid('Torso', (0, .10, .0), (.33, .62, .30), 'plum')
    ellipsoid('Belly', (0, .05, -.12), (.28, .55, .16), 'belly')
    for i in range(5):
        t = i / 4
        ellipsoid('BackSpike', (0, .45 - t * .95, .28 + t * .02), (.03, .07, .11 - t * .05), 'spike')
    tube('Neck', [(0,-.40,.10),(0,-.55,.18),(0,-.72,.28),
                  (0,-.88,.40),(0,-1.05,.53),(0,-1.22,.64)],
         [.20,.175,.15,.135,.12,.12], 'plum', 'DragonNeck')
    ellipsoid('Head', (0, -1.28, .68), (.155, .27, .155), 'plum', 'DragonFace')
    ellipsoid('Snout', (0, -1.58, .655), (.095, .17, .095), 'hide', 'DragonFace')
    ellipsoid('JawTip', (0, -1.715, .63), (.06, .085, .05), 'claw', 'DragonFace')
    for side in [-1, 1]:
        ellipsoid('Horn', (side * .085, -1.19, .84), (.026, .026, .16), 'horn', 'DragonFace',
                  direction=(side * .3, -.55, 1))
        ellipsoid('BrowRidge', (side * .11, -1.30, .77), (.05, .09, .025), 'hide', 'DragonFace')
        ellipsoid('Eye', (side * .105, -1.32, .74), (.024, .022, .024), 'eye', 'DragonFace')
        ellipsoid('EyeGlint', (side * .115, -1.335, .752), (.007, .007, .007), 'belly', 'DragonFace')
    tube('Tail', [(0,.43+i*.105,-.015-.07*(i/16)**2) for i in range(17)],
         [.20*(1-i/17)**1.15+.012 for i in range(17)], 'hide')
    ellipsoid('TailFin', (0, 2.05, -.06), (.02, .10, .13), 'spike', direction=(0, 1, 0))
    for side in [-1, 1]:
        ellipsoid('TuckedLeg', (side * .21, .28, -.28), (.075, .17, .075), 'hide')
        ellipsoid('Claw', (side * .21, .16, -.36), (.035, .06, .03), 'claw')

    # Broad, load-bearing wings: 6.7 m tip-to-tip against a roughly 4 m dragon length.
    # Each wing is one closed subdivided surface; joint weights are continuous across its skin.
    arm = bpy.data.armatures.new('DragonSkeleton')
    rig = bpy.data.objects.new('DragonArmature', arm); collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig; rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    root = arm.edit_bones.new('Root'); root.head = (0, 0, 0); root.tail = (0, 0, .2)
    for side, label in [(1, 'Left'), (-1, 'Right')]:
        points = [(side * .24, -.05, .14), (side * 1.35, .00, .22), (side * 2.45, .08, .10),
                  (side * 3.35, .22, -.05), (side * 3.43, .24, -.06)]
        parent = root
        for index, segment in enumerate(['Upper', 'Forearm', 'Hand', 'Tip']):
            b = arm.edit_bones.new(label + segment); b.head = points[index]; b.tail = points[index + 1]
            b.parent = parent; parent = b
    bpy.ops.object.mode_set(mode='OBJECT')
    for side, label in [(1, 'Left'), (-1, 'Right')]:
        weights = lambda vertex, label=label: wing_weights(label, vertex)
        # One watertight membrane per side, with shared vertices at every joint.
        spans, chords = 48, 8
        vertices, faces = [], []
        for layer in [-1,1]:
            for i in range(spans+1):
                x=.24+(3.43-.24)*i/spans
                for j in range(chords+1):
                    px,y,z=wing_surface(x,j/chords)
                    vertices.append((side*px,y,z+layer*.012))
        layer_count=(spans+1)*(chords+1)
        for layer in range(2):
            for i in range(spans):
                for j in range(chords):
                    a=layer*layer_count+i*(chords+1)+j; b=a+chords+1
                    faces.append((a,a+1,b+1,b))
        boundary = ([i*(chords+1) for i in range(spans+1)]
                    +[spans*(chords+1)+j for j in range(1,chords+1)]
                    +[i*(chords+1)+chords for i in range(spans-1,-1,-1)]
                    +[j for j in range(chords-1,0,-1)])
        for i,a in enumerate(boundary):
            b=boundary[(i+1)%len(boundary)]
            faces.append((a,b,b+layer_count,a+layer_count))
        mesh_part('ContinuousBatMembrane',vertices,faces,'membrane','DragonWings',weights)
        leading=[wing_surface(.24+(3.43-.24)*i/32,0) for i in range(33)]
        tube('LeadingSpar',[(side*x,y,z+.018) for x,y,z in leading],
             [.055-.038*i/32 for i in range(33)],'strut','DragonWings',weights,8)
        for finger,x in enumerate([.95,1.70,2.45,3.10]):
            points=[wing_surface(x,j/12) for j in range(13)]
            tube('MembraneFinger',[(side*px,y,z+.015) for px,y,z in points],
                 [.032-.025*j/12 for j in range(13)],'strut','DragonWings',weights,6)
        ellipsoid('WingClaw',(side*3.43,.24,-.06),(.065,.035,.025),'belly',
                  'DragonWings',label+'Hand',(side,.1,-.05))
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
