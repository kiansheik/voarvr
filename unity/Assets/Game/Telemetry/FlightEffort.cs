using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;
namespace VoarVR.Telemetry
{
    // Movement proxies, never calories, medical fatigue or fitness estimates.
    public sealed class FlightEffort
    {
        public float ActiveSeconds,RestSeconds,HandTravelMeters,SmoothedSpeed,ContinuousActiveSeconds;
        public int Strokes;
        public bool Resting {get;private set;}
        private float quietSeconds,strokeCooldown;private bool armed;
        public void Step(FlightInputFrame input,float dt,bool valid)
        {
            if(!valid || dt<=0 || dt>.1f){quietSeconds=ContinuousActiveSeconds=0;Resting=false;armed=false;return;}
            float speed=(input.LeftWing.Velocity.magnitude+input.RightWing.Velocity.magnitude)*.5f;
            HandTravelMeters+=speed*dt;
            SmoothedSpeed=Mathf.Lerp(SmoothedSpeed,speed,1-Mathf.Exp(-dt/2));
            bool working=speed>.35f;
            if(working){ActiveSeconds+=dt;ContinuousActiveSeconds+=dt;quietSeconds=0;Resting=false;}
            else{RestSeconds+=dt;quietSeconds+=dt;if(quietSeconds>3){Resting=true;ContinuousActiveSeconds=0;}}
            strokeCooldown=Mathf.Max(0,strokeCooldown-dt);
            float vertical=(input.LeftWing.Velocity.y+input.RightWing.Velocity.y)*.5f;
            if(vertical>.25f)armed=true;
            if(armed && vertical<-.25f && strokeCooldown==0){Strokes++;armed=false;strokeCooldown=.25f;}
        }
    }
}
