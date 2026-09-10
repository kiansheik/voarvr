"""Add leg/foot skin joints to an authored flyer without overwriting its .blend.
Run: Blender --background source/DuckV1.blend --python add_ground_rig.py
Writes generated/<Species>Ground.blend and exports/<Species>.fbx. Wing geometry/weights stay intact.
"""
from pathlib import Path
import sys
import bpy
from mathutils import Vector
sys.path.insert(0, str(Path(__file__).resolve().parent))
from asset_common import validate, export_objects
from export_asset import export
ROOT=Path(__file__).resolve().parents[2]
objects=export_objects()
rig=next(o for o in objects if o.type=='ARMATURE')
body=next(o for o in objects if o.type=='MESH' and o.name.endswith('Body'))
species='Dragon' if body.name.startswith('Dragon') else 'Duck'
# Connected authored ellipsoids are discrete islands; identify only the existing feet/legs.
adj=[set() for _ in body.data.vertices]
for e in body.data.edges:
 a,b=e.vertices;adj[a].add(b);adj[b].add(a)
seen=set();feet={-1:[],1:[]}
for v in body.data.vertices:
 if v.index in seen:continue
 stack=[v.index];seen.add(v.index);island=[]
 while stack:
  i=stack.pop();island.append(i)
  for j in adj[i]:
   if j not in seen:seen.add(j);stack.append(j)
 center=sum((body.data.vertices[i].co for i in island),Vector())/len(island)
 match=(.035<abs(center.x)<.13 and .08<center.y<.25 and center.z<-.09) if species=='Duck' else (.15<abs(center.x)<.3 and .08<center.y<.4 and center.z<-.22)
 if match:feet[1 if center.x>0 else -1]+=island
assert all(feet.values()), 'Authored leg islands not found'
bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
for side,label in [(1,'Left'),(-1,'Right')]:
 assert label+'Foot' not in rig.data.edit_bones, 'Ground rig already added'
 hip=(side*.075,.12,-.075) if species=='Duck' else (side*.21,.28,-.18)
 ankle=(side*.075,.15,-.123) if species=='Duck' else (side*.21,.16,-.36)
 leg=rig.data.edit_bones.new(label+'Leg');leg.head=hip;leg.tail=ankle;leg.parent=rig.data.edit_bones['Root']
 foot=rig.data.edit_bones.new(label+'Foot');foot.head=ankle;foot.tail=Vector(ankle)+Vector((0,-.08,0));foot.parent=leg
bpy.ops.object.mode_set(mode='OBJECT')
for side,label in [(1,'Left'),(-1,'Right')]:
 ids=feet[side]
 for group in body.vertex_groups:group.remove(ids)
 leg=body.vertex_groups.new(name=label+'Leg');foot=body.vertex_groups.new(name=label+'Foot')
 for i in ids:
  z=body.data.vertices[i].co.z
  weight=1 if species=='Duck' else min(1,max(0,(-z-.21)/.13))
  if weight>0:foot.add([i],weight,'REPLACE')
  if weight<1:leg.add([i],1-weight,'REPLACE')
validate(export_objects())
(ROOT/'blender/generated').mkdir(exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'blender/generated'/f'{species}Ground.blend'))
export(ROOT/'blender/exports'/f'{species}.fbx',overwrite=True)
print('GROUND RIG',species,len(rig.data.bones),'bones',sum(map(len,feet.values())),'leg vertices')
