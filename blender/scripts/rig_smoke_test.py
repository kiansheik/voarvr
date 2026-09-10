"""Validate authored duck/dragon skin and FBX roundtrip. Run with the source .blend open."""
from pathlib import Path
import sys
import tempfile
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
from asset_common import validate, export_objects
from export_asset import export
objects=validate(export_objects())
rig=next(o for o in objects if o.type=='ARMATURE')
mesh=next(o for o in objects if o.type=='MESH' and o.name.endswith('Wings'))
is_dragon=mesh.name.startswith('Dragon')
is_magpie=mesh.name.startswith('Magpie')
original=(mesh.name,mesh.parent,mesh.modifiers[0].object,len(mesh.data.vertices),tuple(rig.data.bones.keys()))
# Negative cases: missing influence, posed source, omitted rig.
group=mesh.vertex_groups[mesh.data.vertices[0].groups[0].group]
weight=mesh.data.vertices[0].groups[0].weight
group.add([0],.2,'REPLACE')
try: validate(objects)
except ValueError: pass
else: raise AssertionError('Accepted non-normalized weights')
group.add([0],weight,'REPLACE')
rig.pose.bones['LeftHand'].rotation_mode='XYZ';rig.pose.bones['LeftHand'].rotation_euler.x=.2
try: validate(objects)
except ValueError: pass
else: raise AssertionError('Accepted non-rest pose')
rig.pose.bones['LeftHand'].rotation_euler.x=0
try: validate([o for o in objects if o != rig])
except ValueError: pass
else: raise AssertionError('Accepted dangling armature')
with tempfile.TemporaryDirectory(prefix='voarvr-rig-') as tmp:
    path=Path(tmp)/'Rig.fbx';export(path)
    assert original==(mesh.name,mesh.parent,mesh.modifiers[0].object,len(mesh.data.vertices),tuple(rig.data.bones.keys()))
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported=list(bpy.context.selected_objects)
    imported_rig=next(o for o in imported if o.type=='ARMATURE')
    assert len(imported_rig.data.bones)==len(original[4])
    if "LeftFoot" in original[4]:
        assert all(name in imported_rig.data.bones for name in original[4] if name.endswith(("Leg","Foot")))
    wings=next(o for o in imported if o.type=='MESH' and o.name.startswith(original[0]))
    assert len(wings.data.vertices)==original[3]
    minimum, maximum=(6.5,7.5) if is_dragon else ((.5,.7) if is_magpie else (.9,1.6))
    assert minimum < wings.dimensions.x < maximum, wings.dimensions
    if is_magpie:
        assert len(imported_rig.data.bones)==63
        for side in ('Left','Right'):
            assert sum(b.name.startswith(side+'Primary') for b in imported_rig.data.bones)==10
            assert sum(b.name.startswith(side+'Secondary') for b in imported_rig.data.bones)==9
            for b in imported_rig.data.bones:
                if b.name.startswith(side+'Primary'):assert b.parent.name==side+'Hand'
                if b.name.startswith(side+'Secondary'):assert b.parent.name==side+'Forearm'
        assert sum(b.name.startswith('Rectrix') for b in imported_rig.data.bones)==12
    if is_dragon:
        assert any(len(v.groups)>1 for v in wings.data.vertices), 'Dragon membrane must blend across joints'
    assert wings.modifiers[0].object==imported_rig
    deps=bpy.context.evaluated_depsgraph_get()
    before=[v.co.copy() for v in wings.evaluated_get(deps).data.vertices]
    hand=imported_rig.pose.bones['LeftHand'];hand.rotation_mode='XYZ';hand.rotation_euler.x=.6
    bpy.context.view_layer.update();deps.update()
    after=[v.co.copy() for v in wings.evaluated_get(deps).data.vertices]
    assert max((a-b).length for a,b in zip(before,after))>.02, 'Imported hand does not deform wing'
print('PASS: skin validation negative cases, source preservation, bone-preserving meter-scale FBX roundtrip and hand deformation')
