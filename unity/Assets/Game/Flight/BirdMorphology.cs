using System;
using UnityEngine;

namespace VoarVR.Flight
{
    public enum WingArchitecture { LegacyAvian, ArticulatedAvian, Membrane }
    // Biological/reference geometry, never personalized by calibration. Optional so old
    // fantasy/gameplay profiles retain their exact values. Provenance is part of the data.
    [Serializable]
    public sealed class BirdMorphology
    {
        public string Species;
        public string Provenance;
        public float MassKg=.2175f;
        public float WingspanM=.56f;
        public float BothWingAreaM2=.06171f;
        public float TailLengthM=.2421f;
        public float TailAreaM2=.012f;
        public float HumerusM=.05582f, ForearmM=.06773f, ManusM=.04232f;
        public int PrimariesPerWing=10, SecondariesPerWing=9, Rectrices=12;
        public float ComparativeCruiseMps=9f, ComparativeWingbeatHz=0f; // Frequency unavailable, not a measured zero.
        public bool WingbeatFrequencyAvailable=false;
        public float AspectRatio => WingspanM*WingspanM/BothWingAreaM2;
        public float WingLoading => MassKg*9.81f/BothWingAreaM2;
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public bool IsValid => Finite(MassKg)&&Finite(WingspanM)&&Finite(BothWingAreaM2)&&Finite(TailAreaM2) && MassKg>0 && WingspanM>0 && BothWingAreaM2>0 && TailAreaM2>=0
            && PrimariesPerWing>0 && PrimariesPerWing<=32 && SecondariesPerWing>0 && SecondariesPerWing<=40 && Rectrices>0 && Rectrices<=32;
    }

    [Serializable]
    public sealed class AvianArticulationSettings
    {
        // INFERRED linkage geometry and GAMEPLAY-TUNED human leverage; degrees.
        public float FanExponent=1.35f, FoldReachFraction=.55f, ShoulderPronationGain=.55f;
        public float PrimaryFanDeg=38f, SecondaryFanDeg=12f, ManusFoldDeg=150f;
        public float ShoulderSweepDeg=25f, PronationDeg=45f, AlulaDeg=32f;
        public float TailFanDeg=32f, TailBrakePitchDeg=25f, TailYawDeg=16f;
    }

    public struct AvianPoseState
    {
        public float LeftFan, RightFan, LeftFold, RightFold, Alula, TailSpread, TailPitch, TailYaw;
    }

    // Presentation capabilities differ between feathered wings, membranes and future
    // insect wings. This layer adds articulation without changing legacy two-link IK.
    public interface IWingArticulation
    {
        void Present(VoarVR.Input.FlightInputFrame input,BirdTrackingCalibration calibration,
            BirdFlightController controller,float groundBlend,float dt);
    }
}
