using UnityEngine;
using UnityEngine.XR;
using VoarVR.World;

namespace VoarVR.Flight
{
    // Event-driven contact cues and independently sampled airflow at each wingtip.
    public sealed class FlightFeedback : MonoBehaviour
    {
        private BirdFlightDriver bird;
        private WindField wind;
        private Transform leftTip, rightTip;
        private AudioSource events, breeze;
        private AudioClip impact, touchdown, air;
        private int landings;
        private float clock, contactAfter, leftAfter, rightAfter;
        private bool active, focused=true, applicationPaused;
        public int ContactCueCount { get; private set; }

        public static float ImpactAmplitude(float speed) => speed < .8f ? 0f : Mathf.Lerp(.15f,.65f,Mathf.InverseLerp(.8f,14f,speed));
        public static float WindAmplitude(Vector3 flow) => Mathf.Clamp((flow.magnitude-3f)*.022f,0f,.16f);

        public void Configure(BirdFlightDriver driver, WindField field, BirdRigDriver rig)
        {
            bird=driver;wind=field;leftTip=rig.leftTip;rightTip=rig.rightTip;
            landings=driver.Controller.LandingCount;
            events=gameObject.AddComponent<AudioSource>();events.playOnAwake=false;events.spatialBlend=0;
            breeze=gameObject.AddComponent<AudioSource>();breeze.playOnAwake=false;breeze.loop=true;breeze.spatialBlend=0;
            impact=MakeClip("Soft collision",.16f,85f,false);
            touchdown=MakeClip("Touchdown rustle",.22f,180f,false);
            air=MakeClip("Air over wings",2f,0f,true);breeze.clip=air;breeze.volume=0;
        }

        public void Tick(float dt)
        {
            if(bird==null)return;
            clock+=dt;
            var c=bird.Controller;
            bool enabledFeedback=focused && !applicationPaused && c.State.Phase!=FlightPhase.Paused && !c.StreamingBlocked
                && (!bird.UsesXR || bird.Calibration.Captured);
            if(!enabledFeedback){Silence();landings=c.LandingCount;return;}
            active=true;
            bool landed=c.LandingCount!=landings;landings=c.LandingCount;
            float hit=ImpactAmplitude(c.LastImpactSpeed);
            if(clock>=contactAfter && (landed || hit>0f))
            {
                ContactCueCount++;
                float strength=landed?Mathf.Clamp(hit,.14f,.35f):hit;
                Pulse(XRNode.LeftHand,strength,.1f);Pulse(XRNode.RightHand,strength,.1f);
                events.PlayOneShot(landed?touchdown:impact,landed?.18f:Mathf.Lerp(.12f,.4f,hit/.65f));
                contactAfter=clock+.3f;leftAfter=rightAfter=contactAfter;
            }
            bool airborne=c.State.Phase!=FlightPhase.Perched;
            Vector3 left=airborne && c.LastInput.LeftWing.Tracked && wind!=null?wind.Sample(leftTip.position,c.SimulationTime):Vector3.zero;
            Vector3 right=airborne && c.LastInput.RightWing.Tracked && wind!=null?wind.Sample(rightTip.position,c.SimulationTime):Vector3.zero;
            WindPulse(XRNode.LeftHand,left,ref leftAfter);
            WindPulse(XRNode.RightHand,right,ref rightAfter);
            float airspeed=(c.State.Velocity-c.WindVelocity).magnitude;
            float volume=airborne?Mathf.Clamp((airspeed-3f)*.003f+(left.magnitude+right.magnitude)*.001f,0,.065f):0;
            breeze.volume=Mathf.Lerp(breeze.volume,volume,1-Mathf.Exp(-dt*4));
            breeze.panStereo=Mathf.Clamp((right.magnitude-left.magnitude)*.08f,-.6f,.6f);
            if(volume>0 && !breeze.isPlaying)breeze.Play();
            if(volume==0 && breeze.volume<.001f)breeze.Stop();
        }

        private void WindPulse(XRNode hand,Vector3 flow,ref float next)
        {
            float amount=WindAmplitude(flow);
            if(amount<=.015f || clock<next)return;
            Pulse(hand,amount,.07f);
            next=clock+Mathf.Lerp(.85f,.42f,amount/.16f);
        }
        private void Pulse(XRNode hand,float amplitude,float duration)
        {
            if(!bird.UsesXR)return;
            var device=InputDevices.GetDeviceAtXRNode(hand);
            if(device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                device.SendHapticImpulse(0,amplitude,duration);
        }
        private void Silence()
        {
            if(!active)return;
            active=false;events.Stop();breeze.Stop();breeze.volume=0;
            if(bird.UsesXR)
            {
                InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).StopHaptics();
                InputDevices.GetDeviceAtXRNode(XRNode.RightHand).StopHaptics();
            }
        }
        private void OnApplicationFocus(bool value){focused=value;if(!value)Silence();}
        private void OnApplicationPause(bool value){applicationPaused=value;if(value)Silence();}
        private void OnDisable()=>Silence();
        private void OnDestroy()
        {
            if(impact!=null)Destroy(impact);
            if(touchdown!=null)Destroy(touchdown);
            if(air!=null)Destroy(air);
        }
        private static AudioClip MakeClip(string name,float seconds,float tone,bool loop)
        {
            const int rate=22050;
            var samples=new float[Mathf.RoundToInt(seconds*rate)];
            var random=new System.Random(713);float filtered=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate;
                filtered=Mathf.Lerp(filtered,(float)random.NextDouble()*2-1,.23f);
                float envelope=loop?1f:Mathf.Sin(Mathf.PI*i/samples.Length)*Mathf.Exp(-t*13);
                samples[i]=(filtered*.65f+(tone>0?Mathf.Sin(t*2*Mathf.PI*tone)*.18f:0))*envelope;
            }
            // Ten-millisecond fades make both ends zero and prevent loop clicks.
            if(loop)for(int i=0;i<220;i++){float gain=i/219f;samples[i]*=gain;samples[samples.Length-1-i]*=gain;}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
    }
}
