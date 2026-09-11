using UnityEngine;
using UnityEngine.Rendering;
using VoarVR.Flight;
using VoarVR.Gameplay;
using VoarVR.UI;

namespace VoarVR.World
{
    // A non-text wayfinder only when the real destination is outside the view.
    // It indicates a bearing, not a collectible or a promise of unobstructed flight.
    [DefaultExecutionOrder(200)]
    public sealed class RouteBearing : MonoBehaviour
    {
        private BirdFlightDriver bird;
        private JourneyPresentation journey;
        private Camera eye;
        private Transform cue;
        private Mesh mesh;
        private AudioSource call;
        private AudioClip clip;
        private float nextCall;
        private bool focused=true,applicationPaused;
        public bool Visible => cue != null && cue.gameObject.activeSelf;
        public Vector3 CuePosition => cue != null ? cue.position : Vector3.zero;

        public void Configure(BirdFlightDriver driver, JourneyPresentation presentation, Camera camera)
        {
            bird=driver; journey=presentation; eye=camera;
            if(cue!=null || eye==null)return;
            var kit=Resources.Load<SkywardKit>("SkywardKit"); if(kit==null)return;
            cue=new GameObject("Offscreen route bearing").transform;
            mesh=BuildArrow(); cue.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=cue.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=kit.Material;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            call=cue.gameObject.AddComponent<AudioSource>(); call.playOnAwake=false;
            call.spatialBlend=1; call.dopplerLevel=0; call.rolloffMode=AudioRolloffMode.Linear;
            call.minDistance=4; call.maxDistance=16;
            clip=BuildCall(); cue.gameObject.SetActive(false);
        }

        private void OnEnable()=>Application.onBeforeRender+=ApplyFinalPose;
        [BeforeRenderOrder(100)]
        private void ApplyFinalPose()=>UpdateBearing(false);
        private void LateUpdate()=>TickBearing();
        public void TickBearing()=>UpdateBearing(true);
        private void UpdateBearing(bool updateAudio)
        {
            var c=bird!=null?bird.Controller:null;
            bool eligible=isActiveAndEnabled && focused && !applicationPaused && eye!=null && c!=null && journey!=null && journey.NavigationAvailable
                && FlightPreferences.GuidanceEnabled && !bird.FlightMenuVisible && !c.StreamingBlocked
                && c.State.Phase!=FlightPhase.Paused
                && (!bird.UsesXR || bird.Calibration.Captured && c.LastInput.HeadTracked
                    && c.LastInput.LeftWing.Tracked && c.LastInput.RightWing.Tracked);
            if(!eligible || cue==null){Hide();return;}
            var local=eye.transform.InverseTransformPoint(journey.TargetMarkerWorldPosition);
            if(!TryProject(local,eye.fieldOfView,eye.aspect,out var position,out float angle)) {Hide();return;}
            cue.gameObject.SetActive(true);
            cue.SetPositionAndRotation(eye.transform.TransformPoint(position),eye.transform.rotation*Quaternion.Euler(0,0,angle));
            if(!updateAudio)return;
            if(!FlightPreferences.AudioEnabled)call.Stop();
            else if(Time.unscaledTime>=nextCall)
            {
                call.PlayOneShot(clip,.11f); nextCall=Time.unscaledTime+6;
            }
        }

        private void Hide()
        {
            if(cue==null)return;
            if(call!=null)call.Stop();
            cue.gameObject.SetActive(false);
        }

        // Project into a conservative viewing cone: valid even directly behind or
        // overhead, finite at z=0, and never pretend the offscreen goal is straight ahead.
        public static bool TryProject(Vector3 local,float verticalFov,float aspect,out Vector3 position,out float angle)
        {
            position=Vector3.zero; angle=0;
            if(!float.IsFinite(local.x)||!float.IsFinite(local.y)||!float.IsFinite(local.z)||local.sqrMagnitude<64)return false;
            float tangent=Mathf.Tan(Mathf.Clamp(verticalFov,30,120)*.5f*Mathf.Deg2Rad);
            float vertical=Mathf.Min(tangent*.72f,.48f),horizontal=Mathf.Min(tangent*Mathf.Clamp(aspect,.6f,2)*.72f,.65f);
            if(local.z>0 && Mathf.Abs(local.x/local.z)<horizontal && Mathf.Abs(local.y/local.z)<vertical)return false;
            var direction=new Vector2(local.x,local.y);
            if(local.z<0 && Mathf.Abs(direction.x)<.05f)direction.x=1; // A consistent turn cue for directly behind.
            if(direction.sqrMagnitude<.0001f)direction=Vector2.up;
            float scale=1/Mathf.Max(Mathf.Abs(direction.x)/horizontal,Mathf.Abs(direction.y)/vertical);
            direction*=scale;
            position=new Vector3(direction.x*4,direction.y*4,4);
            angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
            return true;
        }

        private static Mesh BuildArrow()
        {
            var points=new[]{new Vector3(-.20f,-.12f,0),new Vector3(.04f,0,0),new Vector3(-.20f,.12f,0),
                new Vector3(-.28f,.12f,0),new Vector3(-.04f,0,0),new Vector3(-.28f,-.12f,0)};
            var vertices=new Vector3[12];var colors=new Color[12];
            for(int i=0;i<6;i++)
            {
                vertices[i]=points[i]*1.22f;colors[i]=new Color(.055f,.11f,.12f);
                vertices[i+6]=points[i]+Vector3.back*.006f;colors[i+6]=new Color(1,.83f,.25f);
            }
            var result=new Mesh{name="Outlined route bearing"};result.vertices=vertices;result.colors=colors;
            result.triangles=new[]{0,4,1,1,4,2,2,4,3,0,5,4,6,10,7,7,10,8,8,10,9,6,11,10};
            result.RecalculateNormals();result.RecalculateBounds();return result;
        }
        private static AudioClip BuildCall()
        {
            const int rate=22050;var samples=new float[rate/3];
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate,phase=t*3;
                samples[i]=Mathf.Sin(2*Mathf.PI*(620*t+240*t*t))*.2f*Mathf.Pow(Mathf.Sin(Mathf.PI*phase),2);
            }
            var result=AudioClip.Create("Route turn call",samples.Length,1,rate,false);result.SetData(samples,0);return result;
        }
        private void OnApplicationFocus(bool value){focused=value;if(!value)Hide();}
        private void OnApplicationPause(bool value){applicationPaused=value;if(value)Hide();}
        private void OnDisable(){Application.onBeforeRender-=ApplyFinalPose;Hide();}
        private void OnDestroy()
        {
            if(cue!=null)Destroy(cue.gameObject);
            if(mesh!=null)Destroy(mesh);if(clip!=null)Destroy(clip);
        }
    }
}
