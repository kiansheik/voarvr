using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace VoarVR.World
{
    // Opaque sculpted cloud banks leave navigable gaps. No fullscreen transparency.
    public sealed class SkyWeatherPresentation:MonoBehaviour
    {
        private readonly List<GameObject> banks=new List<GameObject>();
        private WorldSpace space;private readonly Mesh[] clouds=new Mesh[3];private Material material;
        private readonly MeshFilter[] bankMeshes=new MeshFilter[16];
        private readonly LineRenderer[] rain=new LineRenderer[12];
        private Material previousSky,sky;
        public void Configure(WorldSpace coordinates)
        {
            space=coordinates;previousSky=RenderSettings.skybox;var skyAsset=Resources.Load<Material>("SkyGardenSky");if(skyAsset!=null){sky=new Material(skyAsset);RenderSettings.skybox=sky;}var kit=Resources.Load<SkywardKit>("SkywardKit");if(kit==null)return;material=kit.Material;
            for(int i=0;i<clouds.Length;i++)clouds[i]=CloudMesh(i);
            for(int i=0;i<16;i++)
            {var go=new GameObject("Cloud bank "+i);go.transform.SetParent(transform,false);bankMeshes[i]=go.AddComponent<MeshFilter>();bankMeshes[i].sharedMesh=clouds[i%3];var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;banks.Add(go);}
            for(int i=0;i<rain.Length;i++)
            {var go=new GameObject("Rain streak "+i);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.positionCount=2;line.widthMultiplier=.012f;line.sharedMaterial=kit.Material;line.startColor=line.endColor=new Color(.45f,.65f,.75f);line.shadowCastingMode=ShadowCastingMode.Off;line.enabled=false;rain[i]=line;}
        }
        private Mesh CloudMesh(int family)
        {
            var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
            // Broad decks, tall cumulus and sparse wind-sheared wisps share the same
            // opaque material. Weld longitude and poles so no open seams or split
            // normals remain. Each family stays below the original 364 vertices.
            int count=family==2?3:4;
            for(int puff=0;puff<count;puff++)
            {
                int start=v.Count;float px=(puff-(count-1)*.5f)*(family==0?25:family==1?16:31),pz=Mathf.Sin(puff*2+family)*9;
                float scale=family==0?27+puff%2*7:family==1?19+puff%3*8:20+puff%2*5;
                float vertical=family==0?.29f:family==1?.90f:.18f;
                float height=family==1?(puff==1?12:puff==2?22:0):Mathf.Sin(puff*1.9f)*3;
                v.Add(new Vector3(px,height+scale*vertical,pz));c.Add(new Color(.89f,.91f,.85f));
                for(int lat=1;lat<6;lat++)for(int lon=0;lon<12;lon++)
                {
                    float a=lat*Mathf.PI/6,b=lon*Mathf.PI/6;
                    float r=1+.07f*Mathf.Sin(b*4+puff)*Mathf.Sin(a);
                    v.Add(new Vector3(px+Mathf.Sin(a)*Mathf.Cos(b)*scale*r,Mathf.Cos(a)*scale*vertical+height,pz+Mathf.Sin(a)*Mathf.Sin(b)*scale*(family==2?.55f:1)*r));
                    c.Add(Color.Lerp(new Color(.43f,.56f,.65f),new Color(.89f,.91f,.85f),(Mathf.Cos(a)+1)*.5f));
                }
                int bottom=v.Count;v.Add(new Vector3(px,height-scale*vertical,pz));c.Add(new Color(.43f,.56f,.65f));
                for(int lon=0;lon<12;lon++)
                {
                    int next=(lon+1)%12;
                    t.Add(start);t.Add(start+1+next);t.Add(start+1+lon);
                    for(int lat=0;lat<4;lat++)
                    {int a=start+1+lat*12+lon,b=start+1+lat*12+next;t.Add(a);t.Add(b);t.Add(a+12);t.Add(b);t.Add(b+12);t.Add(a+12);}
                    t.Add(start+49+lon);t.Add(start+49+next);t.Add(bottom);
                }
            }
            var mesh=new Mesh{name="Cloud sculpture "+family};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetColors(c);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static bool ClearsFirstRoute(double x,double z)
        {
            // Reserve a scenic gap through the cloud sea between the first lift and
            // the garden. Account for the sculpture's whole width, not only its center.
            double t=System.Math.Max(0,System.Math.Min(1,((x-100)*320+(z-140)*480)/(320d*320+480d*480)));
            double dx=x-(100+t*320),dz=z-(140+t*480);
            return dx*dx+dz*dz>160*160;
        }
        public void Tick(Vector3 player)
        {
            if(space==null || clouds[0]==null)return;
            var p=space.ToLogical(player);long x=(long)System.Math.Floor(p.X/256),z=(long)System.Math.Floor(p.Z/256);
            for(int i=0;i<banks.Count;i++)
            {
                long bx=x+i%4-1,bz=z+i/4-1;uint h=AtmosphereModel.Hash(space.Seed,bx,bz,99);
                double wx=(bx+.3)*256,wz=(bz+.7)*256;
                banks[i].SetActive(ClearsFirstRoute(wx,wz));bankMeshes[i].sharedMesh=clouds[(int)((h>>8)%3)];
                banks[i].transform.position=space.ToLocal(wx,151+((h>>4)%28),wz);banks[i].transform.rotation=Quaternion.Euler(0,h%360,0);banks[i].transform.localScale=new Vector3(.85f+(h%4)*.12f,1,.85f+((h>>6)%3)*.13f);
            }
        }
        private void LateUpdate()
        {
            var eye=Camera.main;if(eye==null || clouds[0]==null)return;
            eye.farClipPlane=1800;eye.clearFlags=sky!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;
            Tick(eye.transform.position);var p=space.ToLogical(eye.transform.position);
            bool wet=FlightRegions.Weather(p.X,p.Y,p.Z)==WeatherRegion.RainFront;
            float clock=Time.time;
            for(int i=0;i<rain.Length;i++)
            {var line=rain[i];line.enabled=wet;if(!wet)continue;float a=i*2.399f;var start=eye.transform.position+new Vector3(Mathf.Cos(a)*4,6-Mathf.Repeat(clock*9+i*.7f,12),Mathf.Sin(a)*4);line.SetPosition(0,start);line.SetPosition(1,start+new Vector3(-.25f,1.6f,0));}
            float cloudAmount=1-Mathf.Clamp01(Mathf.Abs((float)p.Y-175)/40);
            eye.backgroundColor=Color.Lerp(new Color(.42f,.65f,.76f),wet?new Color(.22f,.30f,.39f):new Color(.57f,.68f,.73f),wet?.75f:cloudAmount*.6f);
            if(sky!=null){sky.SetFloat("_Exposure",wet?.68f:1f);sky.SetColor("_SkyTint",wet?new Color(.65f,.75f,.80f):Color.white);}
        }
        private void OnDestroy(){foreach(var cloud in clouds)if(cloud!=null)Destroy(cloud);if(sky!=null){RenderSettings.skybox=previousSky;Destroy(sky);}}
    }
}
