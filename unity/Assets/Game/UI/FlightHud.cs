using UnityEngine;
using UnityEngine.UI;
using VoarVR.Flight;
using VoarVR.World;

namespace VoarVR.UI
{
    // Sparse world-space instruments at a two-metre focal distance, visible in both eyes.
    // Air movement and actual bird climb are deliberately separate measurements.
    public sealed class FlightHud : MonoBehaviour
    {
        public static readonly Color LiftColor = new Color(1f, .76f, .28f);
        public static readonly Color SinkColor = new Color(1f, .38f, .5f);
        public static readonly Color BreezeColor = new Color(.35f, .9f, 1f);
        public static float AirspeedKnots(Vector3 velocity, Vector3 wind) => (velocity-wind).magnitude * 1.9438445f;
        public static Color SpeedColor(float metersPerSecond) => Color.Lerp(new Color(.25f,.85f,1),new Color(.8f,.45f,1),Mathf.InverseLerp(4,22,metersPerSecond));
        public static Color AirColor(float vertical) => vertical > .7f ? LiftColor : vertical < -.7f ? SinkColor : BreezeColor;
        public static string LiftCue(float airRise, float climb, float brake=0)
        {
            if (airRise < -.7f) return "SINKING AIR - SEEK CLEAR AIR";
            if (airRise <= .7f) return "GOLD STREAMS: RISING AIR";
            return climb > .2f ? "LIFT FOUND - GLIDE + CIRCLE" : brake > .1f
                ? "RISING AIR - EASE BRAKE" : "RISING AIR - HOLD WINGS LEVEL";
        }
        private BirdFlightDriver bird;
        private WorldStreamer world;
        private Canvas canvas;
        private Text speed, altitude, climb, air, heading;
        private RectTransform attitude;
        private Image pitchDot;
        private float nextText, lastTime=-1, smoothSpeed, smoothClimb, smoothAir;
        public Canvas Instruments => canvas;
        public string LiftReadout => air != null ? air.text : "";
        public void Configure(BirdFlightDriver driver, Camera camera, WorldStreamer streamer)
        {
            if (canvas != null || camera == null) return;
            bird=driver; world=streamer;
            var root=new GameObject("Flight instruments",typeof(RectTransform),typeof(Canvas));
            root.transform.SetParent(camera.transform,false);
            root.transform.localPosition=new Vector3(0,0,2);
            root.transform.localScale=Vector3.one*.001f;
            canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=camera;
            root.GetComponent<RectTransform>().sizeDelta=new Vector2(1600,1000);
            speed=Readout(root.transform,"Airspeed",new Vector2(-620,410),new Vector2(310,125));
            altitude=Readout(root.transform,"Ground clearance",new Vector2(620,410),new Vector2(310,125));
            climb=Readout(root.transform,"Climb rate",new Vector2(620,255),new Vector2(310,125));
            air=Readout(root.transform,"Air guidance",new Vector2(0,-440),new Vector2(840,105));
            air.fontSize=30;
            heading=Label(root.transform,"Heading",new Vector2(0,455),new Vector2(210,44),27);
            var backdrop=Panel(root.transform,"Attitude",new Vector2(0,360),new Vector2(235,165));
            var fixedLine=Panel(backdrop,"Level reference",Vector2.zero,new Vector2(180,3));
            fixedLine.GetComponent<Image>().color=new Color(1,1,1,.4f);
            attitude=Panel(backdrop,"Bird bank",Vector2.zero,new Vector2(145,7));
            attitude.GetComponent<Image>().color=BreezeColor;
            pitchDot=Panel(backdrop,"Bird pitch",Vector2.zero,new Vector2(12,12)).GetComponent<Image>();
            pitchDot.color=Color.white;
            var legend=Label(backdrop,"Attitude label",new Vector2(0,-70),new Vector2(220,30),23);legend.text="BANK / PITCH";
        }
        private static RectTransform Panel(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.sizeDelta=size;r.anchoredPosition=position;
            var image=go.GetComponent<Image>();image.color=new Color(.025f,.065f,.08f,.55f);image.raycastTarget=false;
            return r;
        }
        private static Text Label(Transform parent,string name,Vector2 position,Vector2 size,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();r.sizeDelta=size;r.anchoredPosition=position;
            var t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize=fontSize;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.9f,.97f,1);t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;
            return t;
        }
        private static Text Readout(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var panel=Panel(parent,name,position,size);
            return Label(panel,"Value",Vector2.zero,size-new Vector2(12,8),40);
        }
        private void LateUpdate() => TickInstruments();
        public void TickInstruments()
        {
            if(bird==null||bird.Controller==null||canvas==null)return;
            var c=bird.Controller;float time=c.SimulationTime;
            bool reset=lastTime<0||time<lastTime;
            float blend=reset?1:1-Mathf.Exp(-Mathf.Max(0,time-lastTime)/.4f);
            smoothSpeed=Mathf.Lerp(smoothSpeed,AirspeedKnots(c.State.Velocity,c.WindVelocity),blend);
            smoothClimb=Mathf.Lerp(smoothClimb,c.State.Velocity.y,blend);
            smoothAir=Mathf.Lerp(smoothAir,c.WindVelocity.y,blend);
            lastTime=time;
            var e=c.State.Rotation.eulerAngles;
            attitude.localRotation=Quaternion.Euler(0,0,Mathf.DeltaAngle(0,e.z));
            pitchDot.rectTransform.anchoredPosition=new Vector2(0,Mathf.Clamp(-Mathf.DeltaAngle(0,e.x)*1.4f,-34,34));
            if(!reset&&time<nextText)return;
            nextText=time+.2f;
            speed.text=$"<size=24>AIRSPEED</size>\n{smoothSpeed:0} kt";
            speed.color=SpeedColor(smoothSpeed/1.9438445f);
            float agl=0;
            bool groundKnown=false;
            if(world!=null&&world.TryGetReadyChunk(c.State.Position,out var chunk)
                &&chunk.TryGetGround(c.State.Position,out var p,out _,out _))
            { groundKnown=true;agl=Mathf.Max(0,c.State.Position.y-p.y); }
            altitude.text=groundKnown?$"<size=24>ABOVE GROUND</size>\n{agl:0} m":"<size=24>ABOVE GROUND</size>\n-- m";
            climb.text=$"<size=24>CLIMB / SINK</size>\n{smoothClimb:+0.0;-0.0;0.0} m/s";
            climb.color=smoothClimb>.2f?LiftColor:smoothClimb<-.7f?SinkColor:Color.white;
            air.text=c.State.Phase==FlightPhase.Perched?"LANDED - FLAP TO TAKE OFF":
                bird.WindModeName.StartsWith("Still")?"STILL AIR - WINGBEATS + GLIDE":
                $"VERTICAL AIR {smoothAir:+0.0;-0.0;0.0} m/s\n{LiftCue(smoothAir,smoothClimb,c.LandingBrake)}";
            air.color=AirColor(smoothAir);
            heading.text=$"HDG {Mathf.RoundToInt(e.y)%360:000}";
        }
        private void OnDestroy(){if(canvas!=null)Destroy(canvas.gameObject);}
    }
}
