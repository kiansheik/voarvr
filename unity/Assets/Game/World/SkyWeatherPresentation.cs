using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace VoarVR.World
{
    // Opaque sculpted cloud banks leave navigable gaps. No fullscreen transparency.
    public sealed class SkyWeatherPresentation:MonoBehaviour
    {
        private readonly List<GameObject> banks=new List<GameObject>();
        private WorldSpace space;private Mesh cloud;private Material material;
        private readonly LineRenderer[] rain=new LineRenderer[12];
        private float lastClock;private Material previousSky,sky;
        public void Configure(WorldSpace coordinates)
        {
            space=coordinates;previousSky=RenderSettings.skybox;var skyAsset=Resources.Load<Material>("SkyGardenSky");if(skyAsset!=null){sky=new Material(skyAsset);RenderSettings.skybox=sky;}var kit=Resources.Load<SkywardKit>("SkywardKit");if(kit==null)return;material=kit.Material;
            cloud=CloudMesh();
            for(int i=0;i<16;i++)
            {var go=new GameObject("Cloud bank "+i);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=cloud;go.AddComponent<MeshRenderer>().sharedMaterial=material;banks.Add(go);}
            for(int i=0;i<rain.Length;i++)
            {var go=new GameObject("Rain streak "+i);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.positionCount=2;line.widthMultiplier=.012f;line.sharedMaterial=kit.Material;line.startColor=line.endColor=new Color(.45f,.65f,.75f);line.shadowCastingMode=ShadowCastingMode.Off;line.enabled=false;rain[i]=line;}
        }
        private Mesh CloudMesh()
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            for(int puff=0;puff<4;puff++)
            {
                int start=v.Count;float px=(puff-1.5f)*19,pz=Mathf.Sin(puff*2)*8,scale=23+puff%3*5;
                for(int lat=0;lat<=6;lat++)for(int lon=0;lon<=12;lon++)
                {float a=lat*Mathf.PI/6,b=lon*Mathf.PI/6;v.Add(new Vector3(px+Mathf.Sin(a)*Mathf.Cos(b)*scale,Mathf.Cos(a)*scale*.75f+Mathf.Sin(puff*2)*4,pz+Mathf.Sin(a)*Mathf.Sin(b)*scale));c.Add(Color.Lerp(new Color(.40f,.52f,.61f),new Color(.82f,.87f,.84f),(Mathf.Cos(a)+1)*.5f));}
                for(int lat=0;lat<6;lat++)for(int lon=0;lon<12;lon++)
                {int a=start+lat*13+lon;t.Add(a);t.Add(a+1);t.Add(a+13);t.Add(a+1);t.Add(a+14);t.Add(a+13);}
            }
            var mesh=new Mesh{name="Cloud sculpture"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetColors(c);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private void LateUpdate()
        {
            var eye=Camera.main;if(eye==null || cloud==null)return;
            eye.farClipPlane=1800;eye.clearFlags=sky!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;
            var p=space.ToLogical(eye.transform.position);long x=(long)System.Math.Floor(p.X/256),z=(long)System.Math.Floor(p.Z/256);
            for(int i=0;i<banks.Count;i++)
            {
                long bx=x+i%4-1,bz=z+i/4-1;uint h=AtmosphereModel.Hash(space.Seed,bx,bz,99);
                // Keep the departure lift and garden approach clear enough to navigate.
                double wx=(bx+.3)*256,wz=(bz+.7)*256;
                if((wx-100)*(wx-100)+(wz-140)*(wz-140)<150*150)wx-=180;
                banks[i].transform.position=space.ToLocal(wx,155+(h%4)*10,wz);banks[i].transform.rotation=Quaternion.Euler(0,h%360,0);banks[i].transform.localScale=new Vector3(1+(h%3)*.25f,1,1);
            }
            bool wet=FlightRegions.Weather(p.X,p.Y,p.Z)==WeatherRegion.RainFront;
            float clock=Time.time;
            for(int i=0;i<rain.Length;i++)
            {var line=rain[i];line.enabled=wet;if(!wet)continue;float a=i*2.399f;var start=eye.transform.position+new Vector3(Mathf.Cos(a)*4,6-Mathf.Repeat(clock*9+i*.7f,12),Mathf.Sin(a)*4);line.SetPosition(0,start);line.SetPosition(1,start+new Vector3(-.25f,1.6f,0));}
            float cloudAmount=1-Mathf.Clamp01(Mathf.Abs((float)p.Y-175)/40);
            eye.backgroundColor=Color.Lerp(new Color(.42f,.65f,.76f),wet?new Color(.22f,.30f,.39f):new Color(.57f,.68f,.73f),wet?.75f:cloudAmount*.6f);
            if(sky!=null){sky.SetFloat("_Exposure",wet?.68f:1f);sky.SetColor("_SkyTint",wet?new Color(.65f,.75f,.80f):Color.white);}
            lastClock=clock;
        }
        private void OnDestroy(){if(cloud!=null)Destroy(cloud);if(sky!=null){RenderSettings.skybox=previousSky;Destroy(sky);}}
    }
}
