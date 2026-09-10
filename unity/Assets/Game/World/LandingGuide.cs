using UnityEngine;
using VoarVR.Flight;

namespace VoarVR.World
{
    // One reusable surface marker; no invisible capture volume or permanent instructions.
    public sealed class LandingGuide : MonoBehaviour
    {
        private BirdFlightDriver bird;
        private LineRenderer ring;
        private Material material;
        public void Configure(BirdFlightDriver driver, Material sourceMaterial)
        {
            bird=driver;
            if(sourceMaterial==null) return;
            var go=new GameObject("Contact landing guide");go.transform.SetParent(transform,false);
            ring=go.AddComponent<LineRenderer>();ring.useWorldSpace=true;ring.positionCount=33;
            ring.widthMultiplier=.035f; ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            // Scene reference keeps the shader in Android builds; Shader.Find-only
            // materials can work in Editor while their shader is stripped from IL2CPP.
            material=new Material(sourceMaterial);
            material.SetColor("_BaseColor",new Color(.25f,.85f,.65f,1));ring.sharedMaterial=material;
        }
        private void LateUpdate()
        {
            if(bird==null||bird.Controller==null||ring==null)return;
            var c=bird.Controller;
            ring.enabled=c.NearestPerchDistance<18f && c.State.Phase!=FlightPhase.Perched;
            if(!ring.enabled)return;
            var center=c.NearestPerchPosition+Vector3.up*.045f;
            for(int i=0;i<33;i++){float a=i*Mathf.PI*2/32;ring.SetPosition(i,center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.75f);}
        }
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
