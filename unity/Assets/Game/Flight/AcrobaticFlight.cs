using System;
using UnityEngine;

namespace VoarVR.Flight
{
    public enum FlightControlMode { Beginner, Acrobatic }
    public enum FlightTrick { None, FullRoll, FrontLoop, BackLoop, InvertedHold }

    // All envelopes are GAMEPLAY-TUNED. These are not measured animal inertias.
    [Serializable]
    public sealed class AcrobaticProfile
    {
        public Vector3 Inertia = new Vector3(.18f,.25f,.12f);
        public Vector3 Torque = new Vector3(.65f,.35f,.9f);
        public float Damping = 3.2f;
        public float MaxRateDeg = 80f;
        public static AcrobaticProfile ForMass(float mass)
        {
            if(mass>3f) return new AcrobaticProfile { Inertia=new Vector3(9,13,7),Torque=new Vector3(12,9,17),Damping=1.8f,MaxRateDeg=60 };
            if(mass<.5f) return new AcrobaticProfile { Inertia=new Vector3(.035f,.05f,.022f),Torque=new Vector3(.19f,.10f,.25f),Damping=3.5f,MaxRateDeg=110 };
            return new AcrobaticProfile();
        }
    }

    public sealed class AcrobaticDynamics
    {
        public Vector3 AngularVelocity { get; private set; } // body-local rad/s
        public Vector3 AerodynamicTorque {get;private set;}
        public Vector3 ControlTorque { get; private set; } // body-local Nm
        public static float SoftInput(float value,float deadzone,float full)
        {float amount=Mathf.InverseLerp(deadzone,full,Mathf.Abs(value));return Mathf.Sign(value)*amount*amount;}
        // Relative rotation in body axes: a comfortable sideways/upright controller
        // grip must not rotate the pitch-control axis along with the controller.
        public static float CalibratedPitch(Quaternion current,Quaternion neutral)
        {
            var delta=current*Quaternion.Inverse(neutral);
            if(delta.x*delta.x+delta.w*delta.w<1e-8f)return 0;
            return Mathf.DeltaAngle(0,2*Mathf.Atan2(delta.x,delta.w)*Mathf.Rad2Deg);
        }
        // At a calibrated relaxed grip, wind must not demand a sustained wrist
        // correction just to stop pitching. Scale passive pitch with deliberate input;
        // opposing airflow may soften that command but must never reverse it.
        public static float PitchFlowTorque(float flow,float command,float control)
        {
            flow*=Mathf.Clamp01(Mathf.Abs(command));
            return flow*control<0 ? Mathf.Sign(flow)*Mathf.Min(Mathf.Abs(flow),Mathf.Abs(control)*.5f) : flow;
        }
        public void Reset() { AngularVelocity=ControlTorque=AerodynamicTorque=Vector3.zero; }
        public Quaternion Step(Quaternion orientation,Vector3 command,float speed,float tuck,float dt,AcrobaticProfile p,Vector3 localAir=default)
        {
            // Bounded pressure authority, damping and body inertia; no world-upright spring.
            float pressure=Mathf.Lerp(.25f,1f,Mathf.Clamp01(speed/8f));
            ControlTorque=Vector3.Scale(Vector3.ClampMagnitude(command,1f),p.Torque)*pressure;
            var omega=AngularVelocity;
            var momentum=Vector3.Scale(p.Inertia,omega);
            // Aerodynamic weathercock stability follows relative flow in body axes,
            // never world up. It permits inversion and does not level a rolling animal.
            var flowTorque=localAir.sqrMagnitude>.01f?Vector3.Cross(Vector3.forward,localAir.normalized)*p.Torque.x*Mathf.Clamp(speed*speed/64,0,9)*2.5f:Vector3.zero;
            flowTorque.x=PitchFlowTorque(flowTorque.x,command.x,ControlTorque.x);
            AerodynamicTorque=flowTorque;
            var torque=ControlTorque+flowTorque-Vector3.Cross(omega,momentum);
            omega+=new Vector3(torque.x/p.Inertia.x,torque.y/p.Inertia.y,torque.z/p.Inertia.z)*dt;
            omega*=Mathf.Exp(-p.Damping*Mathf.Lerp(1f,.65f,tuck)*dt);
            omega=Vector3.ClampMagnitude(omega,p.MaxRateDeg*Mathf.Deg2Rad);
            AngularVelocity=omega;
            return (orientation*Quaternion.AngleAxis(omega.magnitude*dt*Mathf.Rad2Deg,omega.sqrMagnitude>1e-8f?omega.normalized:Vector3.up)).normalized;
        }
    }

    // Detect actual orientation and trajectory. Reset on mode/reset/contact/tracking gaps.
    public sealed class TrickDetector
    {
        private Quaternion previous;
        private Vector3 previousPath, loopAxis;
        private float roll,pitch,pathPitch,invertedTime,episodeTime,idleTime;
        private bool initialized,invertedAwarded;
        public int Count { get; private set; }
        public int Score { get; private set; }
        public FlightTrick Last { get; private set; }
        public void Reset(bool clearScore=false)
        { initialized=false;roll=pitch=pathPitch=invertedTime=episodeTime=idleTime=0;invertedAwarded=false;Last=FlightTrick.None;if(clearScore){Count=Score=0;} }
        public FlightTrick Step(Quaternion orientation,Vector3 velocity,float dt,bool eligible)
        {
            if(!eligible || dt<=0 || dt>.1f) {Reset();return FlightTrick.None;}
            var path=velocity.sqrMagnitude>.25f?velocity.normalized:Vector3.zero;
            if(!initialized) { previous=orientation;previousPath=path;loopAxis=orientation*Vector3.right;initialized=true;return FlightTrick.None; }
            var delta=Quaternion.Inverse(previous)*orientation;
            delta.ToAngleAxis(out float angle,out var axis); if(angle>180)angle-=360;
            if(float.IsNaN(axis.x) || Mathf.Abs(angle)>60) {Reset();return FlightTrick.None;}
            float dr=axis.z*angle,dp=axis.x*angle;
            episodeTime+=dt;idleTime=Mathf.Abs(dr)+Mathf.Abs(dp)<.05f?idleTime+dt:0;
            if(episodeTime>12 || idleTime>1.5f){roll=pitch=pathPitch=0;episodeTime=idleTime=0;loopAxis=orientation*Vector3.right;}
            if(Mathf.Abs(dp)>.05f && pitch*dp<0){pitch=pathPitch=0;loopAxis=orientation*Vector3.right;episodeTime=0;}
            roll=Accumulate(roll,dr);pitch=Accumulate(pitch,dp);
            if(path!=Vector3.zero && previousPath!=Vector3.zero)
                pathPitch+=Vector3.SignedAngle(Vector3.ProjectOnPlane(previousPath,loopAxis),Vector3.ProjectOnPlane(path,loopAxis),loopAxis);
            float upright=Vector3.Dot(orientation*Vector3.up,Vector3.up);
            FlightTrick result=FlightTrick.None;
            if(upright<-.65f) {invertedTime+=dt;if(invertedTime>=2 && !invertedAwarded){result=FlightTrick.InvertedHold;invertedAwarded=true;}}
            else {invertedTime=0;invertedAwarded=false;}
            if(Mathf.Abs(roll)>=345 && upright>.75f) {result=FlightTrick.FullRoll;roll=0;}
            if(Mathf.Abs(pitch)>=345 && Mathf.Abs(pathPitch)>=300 && upright>.75f)
            {result=pitch>0?FlightTrick.FrontLoop:FlightTrick.BackLoop;pitch=pathPitch=0;}
            previous=orientation;previousPath=path;
            if(result!=FlightTrick.None){Last=result;Count++;Score+=result==FlightTrick.InvertedHold?100:250;}
            return result;
        }
        private static float Accumulate(float total,float delta)
        {return Mathf.Abs(delta)>.05f && total*delta<0?delta:total+delta;}
    }
}
