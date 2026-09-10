"""Original Pica pica model; metre-scale reference geometry, -Y forward.
See docs/research/species/magpie.md. 63 bones, 10+9 remiges per wing,
12 graduated rectrices. Individual feather geometry and rigid feather weights.
Run in fresh background Blender; never overwrites authored sources implicitly.
"""
from pathlib import Path
import math
import sys
import bpy
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
from asset_common import validate, export_objects
from export_asset import export
ROOT=Path(__file__).resolve().parents[2]


def build(overwrite=False):
    source=ROOT/'blender/source/MagpieV1.blend'
    if source.exists() and not overwrite: raise FileExistsError(source)
    if bpy.data.collections.get('EXPORT'): raise ValueError('Use a fresh Blender instance')
    scene=bpy.data.scenes.new('MagpieAuthoring'); bpy.context.window.scene=scene
    scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
    collection=bpy.data.collections.new('EXPORT');scene.collection.children.link(collection)
    palette=bpy.data.materials.new('MagpiePalette');palette.use_nodes=True
    nodes=palette.node_tree.nodes;attr=nodes.new('ShaderNodeVertexColor');attr.layer_name='Color'
    palette.node_tree.links.new(attr.outputs['Color'],nodes.get('Principled BSDF').inputs['Base Color'])
    nodes.get('Principled BSDF').inputs['Roughness'].default_value=.48
    colors={'black':(.018,.025,.032,1),'white':(.88,.9,.87,1),'blue':(.015,.045,.075,1),'green':(.015,.058,.043,1),'feet':(.075,.075,.08,1),'eye':(.002,.002,.003,1)}
    groups={'MagpieBody':[],'MagpieFace':[],'MagpieWings':[],'MagpieTail':[]}
    def finish(obj,name,group,bone,color):
        obj.name=name
        for owner in list(obj.users_collection):owner.objects.unlink(obj)
        collection.objects.link(obj)
        obj.data.materials.append(palette)
        attr=obj.data.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='CORNER')
        for entry in attr.data:entry.color=colors[color]
        obj.vertex_groups.new(name=bone).add(list(range(len(obj.data.vertices))),1,'REPLACE')
        groups[group].append(obj)
        return obj
    def ellipsoid(name,center,scale,color,group='MagpieBody',bone='Root'):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,location=center)
        obj=bpy.context.object;obj.scale=scale
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        for poly in obj.data.polygons:poly.use_smooth=True
        return finish(obj,name,group,bone,color)
    # Compact black corvid head, pointed bill, white belly and scapular patches.
    ellipsoid('Torso',(0,.01,0),(.040,.085,.041),'black')
    ellipsoid('Belly',(0,.018,-.020),(.036,.058,.032),'white')
    ellipsoid('Neck',(0,-.06,.025),(.026,.034,.036),'black')
    ellipsoid('Head',(0,-.089,.049),(.028,.033,.029),'black','MagpieFace')
    # Tapered corvid bill, not a duck bill. Authored culmen approximately .041m.
    mesh=bpy.data.meshes.new('BillGeometry')
    mesh.from_pydata([(-.01,-.111,.044),(.01,-.111,.044),(0,-.111,.057),(0,-.151,.043),(0,-.111,.038)],[],[(0,2,3),(2,1,3),(1,4,3),(4,0,3),(0,4,1,2)])
    obj=bpy.data.objects.new('Bill',mesh);collection.objects.link(obj);finish(obj,'Bill','MagpieFace','Root','black')
    for side in [-1,1]:
        ellipsoid('Eye',(side*.025,-.099,.058),(.005,.005,.005),'eye','MagpieFace')
        ellipsoid('EyeGlint',(side*.028,-.101,.060),(.0015,.0015,.0015),'white','MagpieFace')
        ellipsoid('Scapular',(side*.031,-.002,.022),(.019,.051,.019),'white')
    arm=bpy.data.armatures.new('MagpieSkeleton');rig=bpy.data.objects.new('MagpieArmature',arm);collection.objects.link(rig)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    def bone(name,head,tail,parent):
        b=arm.edit_bones.new(name);b.head=head;b.tail=tail;b.parent=arm.edit_bones.get(parent) if parent else None
        return b
    bone('Root',(0,0,0),(0,0,.03),None)
    feathers=[]
    # Shoulder -> humerus -> ulna -> manus. Bone lengths are inferred, not AVONET chord.
    for side,label in [(1,'Left'),(-1,'Right')]:
        points=[(side*.035,-.016,.018),(side*.095,.010,.018),(side*.175,-.005,.013),(side*.225,-.014,.009),(side*.23,-.014,.009)]
        for i,part in enumerate(['Upper','Forearm','Hand','Tip']):
            bone(label+part,points[i],points[i+1],'Root' if i==0 else label+['Upper','Forearm','Hand'][i-1])
        for i in range(10):
            # Overlapping asymmetrical vanes; longest inner-middle primaries, shorter tip.
            t=i/9;start=Vector((side*(.177+.044*t),-.003-.010*t,.009-i*.0005))
            angle=math.radians(90-71*t)
            length=.171-.045*abs(t-4/9)/(5/9)
            direction=Vector((side*math.cos(angle),math.sin(angle),-.012))
            end=start+direction*length
            name=label+f'Primary{i+1:02}'
            bone(name,start,end,label+'Hand');feathers.append((name,start,end,.021,'blue' if i%3 else 'green','MagpieWings'))
        for i in range(9):
            t=i/8;start=Vector((side*(.098+.071*t),.011-.014*t,.010-i*.0005))
            end=start+Vector((side*.010,1,-.06)).normalized()*(.142-.015*t)
            name=label+f'Secondary{i+1:02}'
            bone(name,start,end,label+'Forearm');feathers.append((name,start,end,.024,'green','MagpieWings'))
        start=Vector((side*.173,-.020,.019));end=start+Vector((side*.032,-.012,.002))
        bone(label+'Alula',start,end,label+'Hand');feathers.append((label+'Alula',start,end,.012,'black','MagpieWings'))
        bone(label+'Foot',(side*.022,.018,-.069),(side*.022,.018,-.059),'Root')
    # Six progressively shorter pairs, longest central rectrices. Narrow resting tail.
    for i in range(12):
        offset=i-5.5;distance=(abs(offset)-.5)/5
        start=Vector((offset*.0017,.071,-.008-i*.0005))
        end=start+Vector((offset*.002,1,-.058)).normalized()*(.2421*(1-.37*distance))
        name=f'Rectrix{i+1:02}';bone(name,start,end,'Root')
        feathers.append((name,start,end,.017,'green' if i%2 else 'blue','MagpieTail'))
    bpy.ops.object.mode_set(mode='OBJECT')
    def feather(name,start,end,width,color,group):
        direction=(end-start).normalized();across=direction.cross(Vector((0,0,1))).normalized()
        length=(end-start).length
        verts=[]
        # A shallow cambered lens with pointed tip and separate vanes, 18 vertices.
        for t,w in [(0,.10),(.18,.76),(.65,1),(.9,.60)]:
            center=start+direction*length*t+Vector((0,0,.00025*math.sin(t*math.pi)))
            verts += [tuple(center-across*width*w*.42),tuple(center+Vector((0,0,.0001))),tuple(center+across*width*w*.58)]
        verts.append(tuple(end));faces=[]
        for j in range(3):
            a=j*3;b=a+3;faces += [(a,b,b+1,a+1),(a+1,b+1,b+2,a+2)]
        faces += [(9,12,10),(10,12,11)]
        # Give feathers thickness so underside is visible with ordinary culled shaders.
        faces=[tuple(reversed(f)) for f in faces] # Upper shell must face +Z, not inward.
        upper=list(verts);verts += [(x,y,z-.00012) for x,y,z in upper]
        n=len(upper);faces += [tuple(v+n for v in reversed(f)) for f in list(faces)]
        outline=[0,3,6,9,12,11,8,5,2]
        for j,a in enumerate(outline):
            b=outline[(j+1)%len(outline)];faces.append((a,b,b+n,a+n))
        mesh=bpy.data.meshes.new(name+'Geometry');mesh.from_pydata(verts,[],faces);mesh.update()
        assert mesh.polygons[0].normal.z>0, 'Feather upper surface must face outward'
        obj=bpy.data.objects.new(name+'Vane',mesh);collection.objects.link(obj);finish(obj,name+'Vane',group,name,color)
        if 'Primary' in name:
            # Diagnostic white INNER vanes, retaining dark outer vanes and tips.
            attr=mesh.color_attributes['Color']
            for loop in mesh.loops:
                index=loop.vertex_index%13
                if index<9 and index%3<2:attr.data[loop.index].color=colors['white']
    for args in feathers:feather(*args)
    for side,label in [(1,'Left'),(-1,'Right')]:
        ellipsoid('UpperCoverts',(side*.066,-.002,.020),(.044,.025,.014),'white','MagpieWings',label+'Upper')
        ellipsoid('InnerCoverts',(side*.073,.037,.014),(.036,.061,.006),'white','MagpieWings',label+'Upper')
        ellipsoid('ForeCoverts',(side*.134,.025,.019),(.049,.045,.010),'black','MagpieWings',label+'Forearm')
        ellipsoid('HandCoverts',(side*.196,-.002,.013),(.035,.023,.009),'black','MagpieWings',label+'Hand')
        ellipsoid('Tarsus',(side*.022,.035,-.048),(.005,.005,.022),'feet','MagpieBody',label+'Foot')
        for toe in [-1,0,1]:
            ellipsoid('Toe',(side*.022+toe*.006,.018,-.069),(.003,.020,.003),'feet','MagpieBody',label+'Foot')
    for name,parts in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for obj in parts:obj.select_set(True)
        bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();obj=bpy.context.object;obj.name=name
        obj.parent=rig;obj.modifiers.new('Skeleton','ARMATURE').object=rig
    # Match the documented spread span after constructing cambered feather vanes.
    # This inferred lateral projection is applied to the complete wing chain and skin,
    # never a runtime stretch. Tail and body retain their independent reference lengths.
    wings=next(o for o in export_objects() if o.name=='MagpieWings')
    xs=[v.co.x for v in wings.data.vertices];factor=.56/(max(xs)-min(xs))
    for vertex in wings.data.vertices:vertex.co.x*=factor
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
    bpy.ops.object.mode_set(mode='EDIT')
    for b in arm.edit_bones:
        if b.name.startswith(('Left','Right')) and not b.name.endswith('Foot'):
            b.head.x*=factor;b.tail.x*=factor
    bpy.ops.object.mode_set(mode='OBJECT')
    validate(export_objects())
    assert len(arm.bones)==63
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    export(ROOT/'blender/exports/Magpie.fbx',overwrite=True)
    print('MAGPIE',len(arm.bones),'bones',sum(len(o.data.vertices) for o in export_objects() if o.type=='MESH'),'vertices')

if __name__=='__main__':build('--overwrite' in sys.argv)
