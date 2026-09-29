using UnityEngine;

namespace VoarVR.Input
{
    public enum HandPoseSource { Unknown, DirectHigh, DirectLow, Inferred, Lost }

    public struct WingInput
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Quaternion Orientation;
        public bool Tracked;
        // True when the pose is usable for continuity/presentation but its motion is
        // inferred or predicted. Estimated motion must not create active flap energy.
        public bool MotionEstimated;
        // Fraction of active stroke force an estimated pose may still earn; 0 by default, so
        // estimated motion stays forceless unless an adapter measured it is trustworthy.
        public float EstimatedStrokeAuthority;
        public HandPoseSource Source;
        public double SampleTimestamp;
        public bool HasUnextrapolatedPose;
        public Vector3 UnextrapolatedPosition;
        public Quaternion UnextrapolatedOrientation;
        public double UnextrapolatedTimestamp;

        public static WingInput Rest(Vector3 position) => new WingInput
        {
            Position = position, Orientation = Quaternion.identity, Tracked = true
        };
    }

    public struct FlightInputFrame
    {
        public WingInput LeftWing;
        public WingInput RightWing;
        public Vector3 LookDirection;
        public Vector3 HeadPosition;
        public Quaternion HeadOrientation;
        public bool HeadTracked;
        public Quaternion BodyOrientation;
        public bool BodyTracked;
        public float Bank; // -1 left, +1 right: provisional semantic mapping.
        public float Tuck; // 0..1
        public float Flare; // 0..1
        public bool ResetPressed;
        public bool RecalibratePressed;
        public bool CharacterSelectPressed;
        public bool PausePressed;
        public bool ViewTogglePressed;
        public bool WindModePressed;
        public bool HudTogglePressed;
        public bool MarkerPressed;
        public bool ControlModePressed;
        public uint ButtonsHeld; // A,B,X,Y,Menu,HUD,left stick,left grip,right grip; telemetry raw state.
        public Vector2 GroundMove;
        public float GroundTurn; // -1 left, +1 right; consumed only while supported.

        public static FlightInputFrame Neutral => new FlightInputFrame
        {
            LeftWing = WingInput.Rest(new Vector3(-0.6f, 0f, 0f)),
            RightWing = WingInput.Rest(new Vector3(0.6f, 0f, 0f)),
            HeadOrientation = Quaternion.identity,
            LookDirection = Vector3.forward
        };
    }
    // A spread controller axis estimates torso yaw without turning a glance into steering.
    // Once acquired, retain the last torso heading while hands are tucked or unavailable.
    public sealed class TrackedBodyFrame
    {
        public Quaternion Heading { get; private set; } = Quaternion.identity;
        public bool HasHeading { get; private set; }
        private Vector3 previousLeft, previousRight;
        private bool leftValid, rightValid;

        public void Sample(ref FlightInputFrame frame, float dt)
        {
            var axis = frame.RightWing.Position - frame.LeftWing.Position;
            axis.y = 0f;
            if (frame.LeftWing.Tracked && frame.RightWing.Tracked && axis.magnitude >= .65f)
            {
                Heading = Quaternion.LookRotation(Vector3.Cross(axis.normalized, Vector3.up), Vector3.up);
                HasHeading = true;
            }
            else if (!HasHeading && frame.HeadTracked)
            {
                Heading = Quaternion.Euler(0f, frame.HeadOrientation.eulerAngles.y, 0f);
                HasHeading = true;
            }
            frame.BodyOrientation = Heading;
            frame.BodyTracked = HasHeading && frame.HeadTracked;
            frame.LeftWing = RelativeVelocity(frame.LeftWing, frame, dt, ref previousLeft, ref leftValid);
            frame.RightWing = RelativeVelocity(frame.RightWing, frame, dt, ref previousRight, ref rightValid);
            if (!frame.HeadTracked)
            {
                // No torso origin: raw tracking-space hands must not enter calibrated
                // body-space physics or IK. Camera independently retains its last head pose.
                frame.LeftWing.Tracked = frame.RightWing.Tracked = false;
                frame.Bank = frame.Tuck = frame.Flare = 0f;
                frame.LookDirection = Vector3.forward;
            }
        }

        private WingInput RelativeVelocity(WingInput wing, FlightInputFrame frame, float dt,
            ref Vector3 previous, ref bool valid)
        {
            var local = Quaternion.Inverse(Heading) * (wing.Position - frame.HeadPosition);
            bool tracked = wing.Tracked && frame.BodyTracked;
            wing.Velocity = tracked && valid && dt > 0f ? Heading * ((local - previous) / dt) : Vector3.zero;
            previous = local;
            valid = tracked;
            return wing;
        }

        public void ResetDerivatives() { leftValid = rightValid = false; }
        public void Reset() { HasHeading = false; Heading = Quaternion.identity; ResetDerivatives(); }
    }

}
