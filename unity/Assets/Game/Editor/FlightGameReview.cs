using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.World;
namespace VoarVR.Editor
{
    public static class FlightGameReview
    {
        public static string Folder="../artifacts/reviews/flight-game-v1/round-01";
        public static string Capture()
        {var d=Object.FindAnyObjectByType<BirdFlightDriver>();d.enabled=false;d.StartCoroutine(Views(d));return "Flight Game world capture running";}
        private static IEnumerator Views(BirdFlightDriver d)
        {
            Directory.CreateDirectory(Folder);var stream=Object.FindAnyObjectByType<WorldStreamer>();var sky=Object.FindAnyObjectByType<SkyArchipelago>();
            var positions=new[]{new Vector3(0,25,0),new Vector3(100,145,140),new Vector3(350,315,430),new Vector3(420,280,590),new Vector3(420,271,615),new Vector3(720,280,530)};
            var targets=new[]{new Vector3(100,140,150),new Vector3(420,270,620),new Vector3(420,270,620),new Vector3(420,285,620),new Vector3(410,274,630),new Vector3(420,270,620)};
            var names=new[]{"lowland-departure","cloud-ascent","island-approach","split-arch","garden-terrace","rain-front"};
            var log=new StringBuilder();
            for(int i=0;i<positions.Length;i++)
            {
                d.Controller.SetSpawn(positions[i]);d.Controller.Reset();d.transform.position=positions[i];
                for(int j=0;j<180;j++)stream.TickStreaming(positions[i]);sky.Tick(positions[i]);
                for(int j=0;j<90;j++){d.Tick(1f/120);yield return null;}
                Shot(positions[i],targets[i],names[i]);
                log.AppendLine(names[i]+" position="+d.transform.position+" phase="+d.Controller.State.Phase+" wind="+d.Controller.WindVelocity+" chunks="+stream.ActiveCount+" vertices="+stream.VertexCount+" islands="+sky.ActiveIslands);
            }
            File.WriteAllText(Path.Combine(Folder,"world.txt"),log.ToString());
        }
        public static void Shot(Vector3 position,Vector3 target,string name)
        {
            var go=new GameObject("Review camera");var cam=go.AddComponent<Camera>();cam.transform.position=position;cam.transform.LookAt(target);cam.fieldOfView=75;cam.farClipPlane=1800;cam.backgroundColor=Camera.main!=null?Camera.main.backgroundColor:new Color(.42f,.65f,.76f);cam.clearFlags=Camera.main!=null?Camera.main.clearFlags:CameraClearFlags.SolidColor;
            var rt=new RenderTexture(1280,800,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(1280,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();Directory.CreateDirectory(Folder);File.WriteAllBytes(Path.Combine(Folder,name+".png"),tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;Object.Destroy(tex);Object.Destroy(rt);Object.Destroy(go);
        }
    }
}
