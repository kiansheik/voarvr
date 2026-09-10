"""Original vertex-colour environment kit. Run headless; never overwrites open art."""
import bpy, math, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.name='SkywardWorkshop';scene.unit_settings.system='METRIC'
collection=bpy.data.collections.new('EXPORT');scene.collection.children.link(collection)
mat=bpy.data.materials.new('SkywardPalette');mat.diffuse_color=(.32,.48,.43,1)
STONE=(.22,.32,.35,1);MOSS=(.30,.49,.34,1);JADE=(.16,.53,.43,1);BARK=(.29,.25,.24,1);LEAF=(.22,.40,.31,1);GOLD=(.95,.64,.20,1);IVORY=(.83,.78,.61,1);BLOSSOM=(.87,.43,.29,1)
class Shape:
 def __init__(self):self.v=[];self.f=[];self.c=[]
 def face(self,points,color):
  n=len(self.v);self.v.extend(points);self.f.append(tuple(range(n,n+len(points))));self.c.append(color)
 def rings(self,rings,n,color,top=None):
  pts=[]
  for cx,cy,z,rx,ry,phase in rings:
   pts.append([(cx+math.cos(i*2*math.pi/n+phase)*rx,cy+math.sin(i*2*math.pi/n+phase)*ry,z) for i in range(n)])
  self.face(list(reversed(pts[0])),color)
  for k in range(len(pts)-1):
   for i in range(n):
    j=(i+1)%n;c=tuple(x*(.88+.12*((i*7+k)%5)/4) for x in color[:3])+(1,)
    self.face([pts[k][i],pts[k][j],pts[k+1][j],pts[k+1][i]],c)
  self.face(pts[-1],top or color)
 def box(self,c,s,color):
  x,y,z=c;a,b,d=[v/2 for v in s]
  p=[(x-a,y-b,z-d),(x+a,y-b,z-d),(x+a,y+b,z-d),(x-a,y+b,z-d),(x-a,y-b,z+d),(x+a,y-b,z+d),(x+a,y+b,z+d),(x-a,y+b,z+d)]
  for f in [(3,2,1,0),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]:self.face([p[i] for i in f],color)
 def branch(self,a,b,r1,r2,color,n=7):
  a,b=Vector(a),Vector(b);axis=(b-a).normalized();u=axis.cross(Vector((0,1,0)))
  if u.length<.1:u=axis.cross(Vector((1,0,0)))
  u.normalize();v=axis.cross(u)
  ends=[[tuple(p+(u*math.cos(i*2*math.pi/n)+v*math.sin(i*2*math.pi/n))*r) for i in range(n)] for p,r in [(a,r1),(b,r2)]]
  self.face(list(reversed(ends[0])),color);self.face(ends[1],color)
  for i in range(n):j=(i+1)%n;self.face([ends[0][i],ends[0][j],ends[1][j],ends[1][i]],color)
 def save(self,name):
  mesh=bpy.data.meshes.new(name);mesh.from_pydata(self.v,[],self.f);mesh.update()
  col=mesh.color_attributes.new(name='Col',type='FLOAT_COLOR',domain='CORNER')
  for poly,c in zip(mesh.polygons,self.c):
   for li in poly.loop_indices:col.data[li].color=c
  obj=bpy.data.objects.new(name,mesh);collection.objects.link(obj);mesh.materials.append(mat)
  assert len(mesh.vertices)<10000 and all(math.isfinite(x) for v in mesh.vertices for x in v.co)
  return obj
for variant in range(3):
 s=Shape();rng=random.Random(401+variant)
 rx,ry=[(57,44),(78,29),(43,57)][variant]
 rings=[(7,-8,-125-variant*15,.05),(-8,4,-88,.30),(5,-3,-47,.64),(0,0,-12,1.04),(0,0,0,1)]
 pts=[]
 for layer,(cx,cy,z,scale) in enumerate(rings):
  ring=[]
  for i in range(19):
   a=i*2*math.pi/19
   r=1+.15*math.sin(a*3+variant)+.12*math.sin(a*5+.6*variant)
   ring.append((cx+math.cos(a)*rx*r*scale,cy+math.sin(a)*ry*r*scale,z+(math.sin(a*3)*7 if 0<layer<4 else 0)))
  pts.append(ring)
 s.face(list(reversed(pts[0])),STONE)
 for layer in range(4):
  for i in range(19):
   j=(i+1)%19;c=tuple(v*(.65+.35*((i*7+layer*3)%9)/8) for v in STONE[:3])+(1,)
   s.face([pts[layer][i],pts[layer][j],pts[layer+1][j],pts[layer+1][i]],c)
 s.face(pts[-1],MOSS)
 for i in range(9):
  a=i*2*math.pi/9;x=math.cos(a)*rx*.85;y=math.sin(a)*ry*.85
  mid=(x*.9+5,y*.88,-30-rng.random()*20);end=(x*.65+9,y*.6,-90-rng.random()*25)
  s.branch((x,y,-3),mid,1.6,.75,BARK);s.branch(mid,end,.75,.04,BARK)
 # Rim cliffs and a stepped pale approach path leave the central terrace clear.
 for i in range(6):
  a=i*1.13+variant;x=math.cos(a)*rx*.75;y=math.sin(a)*ry*.7
  s.rings([(x,y,-2,9,6,0),(x+3,y,12+rng.random()*12,5,4,.1),(x+5,y+2,22+rng.random()*15,.4,.3,0)],7,STONE,MOSS)
 for i in range(9):s.box((0,-38+i*5,.10),(8+(i%3),4.7,.2),IVORY)
 # Repeated concentric bands make the touchdown garden visible from above.
 for radius in (17,20):
  for i in range(24):
   a=i*2*math.pi/24;s.box((math.cos(a)*radius,math.sin(a)*radius*.65,.15),(1.5,1.5,.3),GOLD)
 # Small warm garden details share the same palette as collectible moths.
 for i in range(18):
  a=i*2.399+variant;x=math.cos(a)*(25+i%3*5);y=math.sin(a)*(20+i%4*3)
  s.branch((x,y,0),(x+.3,y,1.4),.12,.04,LEAF)
  s.rings([(x+.3,y,1,0.2,.2,0),(x+.3,y,1.5,.65,.65,0),(x+.3,y,1.8,.15,.15,0)],6,GOLD if i%3 else BLOSSOM)
 s.save('Island'+str(variant))
# Asymmetric segmented stone arch; actual opening x+-14,z3..31, depth6.
s=Shape()
for side in [-1,1]:
 s.rings([(side*19,0,0,6,4,0),(side*18,0,18,5,3,.1),(side*14,0,29,4,3,.15)],7,STONE,MOSS)
for i in range(6):
 a=math.pi*i/6;b=math.pi*(i+1)/6
 x=math.cos((a+b)/2)*14;z=29+math.sin((a+b)/2)*8
 s.rings([(x,0,z-3,5,3,0),(x,0,z+2,4.6,3,.08)],6,STONE,MOSS)
s.save('Arch')
for variant in range(3):
 s=Shape();height=9+variant*3
 s.branch((0,0,0),(.8,-.3,height),.55,.18,BARK)
 for i in range(5):
  a=i*2.4;z=height*.5+i*.8;end=(math.cos(a)*3.8,math.sin(a)*3,z+2)
  s.branch((.4,0,z),end,.22,.08,BARK)
  if variant==1:s.rings([(end[0],end[1],end[2]-2,3.5,2.7,0),(end[0],end[1],end[2]+1,1.8,1.5,.1),(end[0],end[1],end[2]+5,.1,.1,0)],8,LEAF)
  else:s.rings([(end[0],end[1],end[2]-1.5,.6,.5,0),(end[0],end[1],end[2]-.2,3.7,2.9,.1),(end[0],end[1],end[2]+1.8,4.1,3.2,.2),(end[0],end[1],end[2]+3.4,2,1.6,.15),(end[0],end[1],end[2]+3.9,.3,.25,0)],9,JADE if variant==2 else LEAF)
 s.save('Tree'+str(variant))
s=Shape();s.branch((0,0,0),(1,0,10),.65,.25,BARK)
for i in range(4):s.branch((.5,0,4+i),(math.sin(i)*4,math.cos(i)*3,7+i),.25,.02,BARK)
s.save('DeadTree')
s=Shape();s.branch((-4,0,.5),(4,1,1),.6,.45,BARK);s.branch((1,.5,.6),(2,3,1.5),.25,.03,BARK);s.save('Log')
s=Shape();s.rings([(0,0,0,4,3,0),(1,0,2,4.5,3.1,.1),(1.5,0,4,2.2,1.8,.3)],7,STONE,MOSS);s.save('Rock')
s=Shape();s.rings([(0,0,0,7,5,0),(2,0,12,5,3,.1),(-1,0,27,3,2,.1),(2,0,39,.2,.2,.2)],7,STONE,MOSS);s.save('Spire')
for kind in ['Tower','House','Ruin']:
 s=Shape();levels=4 if kind=='Tower' else 2 if kind=='House' else 1
 for i in range(levels):
  z=i*5;w=4-i*.45
  s.box((0,0,z+2),(w*2,w*1.5,4),IVORY)
  s.box((0,0,z+.2),(w*2+.6,w*1.5+.6,.4),STONE)
  for x in [-w,w]:
   for y in [-w*.75,w*.75]:s.branch((x,y,z+.4),(x,y,z+3.9),.18,.18,GOLD)
  s.box((0,-w*.75-.06,z+1.2),(1.2,.15,2.4),BARK)
  for side in [-1,1]:
   s.box((side*(w+.02),0,z+2),(.1,w*.8,1.6),JADE)
   s.box((0,side*(w*.75+.02),z+2),(w*.8,.1,1.6),JADE)
  s.rings([(0,0,z+4,w+1.6,w*.75+1.6,math.pi/4),(0,0,z+5.7,w*.7,w*.55,math.pi/4)],4,JADE,GOLD)
 if kind=='Tower':s.branch((0,0,20),(0,0,25),.4,.05,GOLD)
 if kind=='Ruin':
  for x,y in [(-8,0),(8,0),(0,8)]:s.rings([(x,y,0,1.5,1.5,0),(x,y,7 if x<0 else 11,1.2,1.2,0)],6,IVORY,GOLD)
 s.save(kind)
s=Shape()
for i in range(14):
 x=(i-6.5)*1.5;z=3+3*math.cos(x/12)
 s.box((x,0,z),(1.6,3,.5),IVORY)
 for y in [-1.5,1.5]:s.branch((x,y,z),(x,y,z+1.3),.12,.1,JADE)
s.save('Bridge')
# A recognizably edible, original sun moth. One small mesh shared by the flock.
s=Shape();s.branch((0,-.45,0),(0,.5,0),.13,.20,GOLD,8)
for side in [-1,1]:
 s.face([(0,-.15,0),(side*.8,-.5,.05),(side*1.05,.1,.18),(side*.55,.6,.14),(0,.2,.02)],BLOSSOM)
 s.face(list(reversed([(0,-.15,0),(side*.8,-.5,.05),(side*1.05,.1,.18),(side*.55,.6,.14),(0,.2,.02)])),IVORY)
 s.branch((side*.07,.4,.04),(side*.3,.7,.1),.025,.015,BARK,5)
s.save('SunMoth')
source=ROOT/'blender/source/SkywardKitV1.blend' ;source.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(source))
bpy.ops.object.select_all(action='DESELECT')
for obj in collection.objects:obj.select_set(True)
out=ROOT/'unity/Assets/Art/Models/SkywardKit.fbx'
bpy.ops.export_scene.fbx(filepath=str(out),use_selection=True,object_types={'MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False)
print('SKYWARD_KIT',len(collection.objects),'meshes',sum(len(o.data.vertices) for o in collection.objects),'vertices',out)
