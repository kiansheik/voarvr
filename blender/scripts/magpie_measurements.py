"""Read-only authored-rest dimensions and overlap-aware projected wing area."""
import json
import bpy


def projected_union_area_xy(obj, strip_width=.00025):
    vertices=[obj.matrix_world @ v.co for v in obj.data.vertices]
    polygons=[[vertices[i] for i in p.vertices] for p in obj.data.polygons]
    y=min(v.y for v in vertices)+strip_width/2; ymax=max(v.y for v in vertices); area=0
    while y<ymax:
        intervals=[]
        for polygon in polygons:
            xs=[]
            for i,a in enumerate(polygon):
                b=polygon[(i+1)%len(polygon)]
                if min(a.y,b.y)<=y<max(a.y,b.y):xs.append(a.x+(y-a.y)*(b.x-a.x)/(b.y-a.y))
            xs.sort();intervals.extend(zip(xs[::2],xs[1::2]))
        end=float('-inf');width=0
        for start,stop in sorted(intervals):
            width+=max(0,stop-max(start,end));end=max(end,stop)
        area+=width*strip_width;y+=strip_width
    return area


def measure():
    wing=bpy.data.objects['MagpieWings'];rig=bpy.data.objects['MagpieArmature']
    span=max(v.co.x for v in wing.data.vertices)-min(v.co.x for v in wing.data.vertices)
    area=projected_union_area_xy(wing)
    assert abs(span-.56)<.0001,(span,'span mismatch')
    assert abs(area-.06171)<.00005,(area,'area mismatch')
    lengths={n:rig.data.bones[n].length for n in ['LeftUpper','LeftForearm','LeftHand','Rectrix06']}
    assert abs(lengths['Rectrix06']-.2421)<.0001
    report=dict(span_m=span,both_wing_area_m2=area,aspect_ratio=span*span/area,bone_lengths_m=lengths)
    print(json.dumps(report,indent=2));return report

if __name__=='__main__':measure()
