"""Validate authored Duck skin and export/import roundtrip. Run with DuckV1.blend open."""
from pathlib import Path
import sys
import tempfile
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
from asset_common import validate, export_objects
from export_asset import export
objects=validate(export_objects())
rig=next(o for o in objects if o.type=='ARMATURE')
mesh=next(o for o in objects if o.name=='DuckWings')
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
    path=Path(tmp)/'Duck.fbx';export(path)
    assert original==(mesh.name,mesh.parent,mesh.modifiers[0].object,len(mesh.data.vertices),tuple(rig.data.bones.keys()))
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.import_scene.fbx(filepath=str(path))
    imported=list(bpy.context.selected_objects)
    imported_rig=next(o for o in imported if o.type=='ARMATURE')
    assert len(imported_rig.data.bones)==9
    wings=next(o for o in imported if o.type=='MESH' and o.name.startswith('DuckWings'))
    assert len(wings.data.vertices)==original[3]
    assert .9 < wings.dimensions.x < 1.6, wings.dimensions
    assert wings.modifiers[0].object==imported_rig
    deps=bpy.context.evaluated_depsgraph_get()
    before=[v.co.copy() for v in wings.evaluated_get(deps).data.vertices]
    hand=imported_rig.pose.bones['LeftHand'];hand.rotation_mode='XYZ';hand.rotation_euler.x=.6
    bpy.context.view_layer.update();deps.update()
    after=[v.co.copy() for v in wings.evaluated_get(deps).data.vertices]
    assert max((a-b).length for a,b in zip(before,after))>.02, 'Imported hand does not deform wing'
print('PASS: skin validation negative cases, source preservation, nine-bone meter-scale FBX roundtrip and hand deformation')
