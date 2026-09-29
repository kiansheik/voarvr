using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoarVR.Input
{
    public struct HandTrackingFrame
    {
        public WingInput Left, Right;
        public Vector3 HeadPosition;
        public Quaternion HeadOrientation;
        public bool HeadTracked, LeftPinched, RightPinched;
    }

    // Shared once-per-render sample for UI and flight; Meta types stay in the adapter.
    public static class HandInteraction
    {
        private static InputActionMap headActions;
        private static InputAction headPosition, headRotation, headTracked;
        private static OVRPlugin.HandState left, right, leftMeasured, rightMeasured;
        private static HandTrackingFrame frame;
        private static int sampledFrame = -1;
        private static bool skeletonReady, permissionRequested, wideMotionRequested;
        public static bool WideMotionEnabled { get; private set; }
        public static string Status { get; private set; } = "Waiting for hand tracking";
#if UNITY_EDITOR
        public static Func<HandTrackingFrame> EditorProvider;
#endif
        public static void Read(out bool headTracked, out bool handTracked, out bool pinched)
        {
            var sample = Sample();
            headTracked = sample.HeadTracked;
            handTracked = headTracked && (Direct(sample.Left) || Direct(sample.Right));
            pinched = handTracked && (sample.LeftPinched || sample.RightPinched);
        }

        public static HandTrackingFrame Sample()
        {
#if UNITY_EDITOR
            if (EditorProvider != null) return EditorProvider();
            return default;
#else
            if (sampledFrame == Time.frameCount) return frame;
            sampledFrame = Time.frameCount;
            if (headActions == null)
            {
                headActions = new InputActionMap("HandsHead");
                headPosition = headActions.AddAction("Position", InputActionType.Value, "<XRHMD>/centerEyePosition");
                headRotation = headActions.AddAction("Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation");
                headTracked = headActions.AddAction("Tracked", InputActionType.Value, "<XRHMD>/isTracked");
                headActions.Enable();
            }
            frame = new HandTrackingFrame { HeadPosition = headPosition.ReadValue<Vector3>(),
                HeadOrientation = headRotation.ReadValue<Quaternion>(), HeadTracked = headTracked.ReadValue<float>() > 0 };
            if (frame.HeadOrientation == default) frame.HeadOrientation = Quaternion.identity;
            if (!OVRPlugin.initialized)
            { WideMotionEnabled = false; Status = "Starting hand tracking"; return frame; }
            if (!skeletonReady) skeletonReady = OVRPlugin.SetHandSkeletonVersion(OVRHandSkeletonVersion.OpenXR);
            if (!skeletonReady) { Status = "OpenXR hand skeleton unavailable"; return frame; }
            ConfigureWideMotion();
            frame.Left = ReadHand(OVRPlugin.Hand.HandLeft, ref left, ref leftMeasured, out frame.LeftPinched);
            frame.Right = ReadHand(OVRPlugin.Hand.HandRight, ref right, ref rightMeasured, out frame.RightPinched);
            Status = $"Hands: {frame.Left.Source} / {frame.Right.Source}";
            return frame;
#endif
        }

        private static void ConfigureWideMotion()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            const string permission = "com.oculus.permission.BODY_TRACKING";
            if (!permissionRequested)
            {
                permissionRequested = true;
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
                    UnityEngine.Android.Permission.RequestUserPermission(permission);
            }
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            { WideMotionEnabled = false; return; }
            if (!OVRPlugin.IsWideMotionMode2HandPosesEnabled())
                wideMotionRequested |= OVRPlugin.SetWideMotionMode2HandPosesEnabled(true);
            WideMotionEnabled = OVRPlugin.IsWideMotionMode2HandPosesEnabled();
            wideMotionRequested |= WideMotionEnabled;
#endif
        }

        private static WingInput ReadHand(OVRPlugin.Hand hand, ref OVRPlugin.HandState display,
            ref OVRPlugin.HandState measured, out bool pinched)
        {
            pinched = false;
            var result = new WingInput { Source = HandPoseSource.Lost, MotionEstimated = true,
                Orientation = Quaternion.identity };
            if (!OVRPlugin.GetHandState(OVRPlugin.Step.Render, hand, ref display)
                || (display.Status & OVRPlugin.HandStatus.HandTracked) == 0) return result;
            bool inferred;
            bool sourceKnown = OVRPlugin.GetHandPoseSourceInferred(-1, hand, out inferred) == OVRPlugin.Result.Success;
            bool high = display.HandConfidence == OVRPlugin.TrackingConfidence.High;
            result.Source = ClassifySource(sourceKnown, inferred, high, WideMotionEnabled || wideMotionRequested);
            result.Tracked = true;
            result.Position = Position(display.RootPose);
            result.Orientation = Orientation(display.RootPose);
            result.SampleTimestamp = display.SampleTimeStamp;
            result.MotionEstimated = result.Source != HandPoseSource.DirectHigh;
            if (OVRPlugin.GetUnextrapolatedHandState(hand, ref measured)
                && (measured.Status & OVRPlugin.HandStatus.HandTracked) != 0)
            {
                result.HasUnextrapolatedPose = true;
                result.UnextrapolatedPosition = Position(measured.RootPose);
                result.UnextrapolatedOrientation = Orientation(measured.RootPose);
                result.UnextrapolatedTimestamp = measured.SampleTimeStamp;
                if (result.Source == HandPoseSource.DirectHigh
                    && measured.HandConfidence != OVRPlugin.TrackingConfidence.High)
                { result.Source = HandPoseSource.DirectLow; result.MotionEstimated = true; }
            }
            int index = (int)OVRPlugin.HandFinger.Index;
            pinched = Direct(result) && (display.Status & OVRPlugin.HandStatus.InputStateValid) != 0
                && (display.Status & OVRPlugin.HandStatus.SystemGestureInProgress) == 0
                && display.FingerConfidences != null && display.FingerConfidences.Length > index
                && display.FingerConfidences[index] == OVRPlugin.TrackingConfidence.High
                && (display.Pinches & OVRPlugin.HandFingerPinch.Index) != 0;
            return result;
        }

        // Only Wide Motion Mode infers hand poses. Without it, a failed or unsupported
        // source query cannot be hiding inference, so tracking confidence decides.
        public static HandPoseSource ClassifySource(bool sourceKnown, bool inferred, bool highConfidence, bool wideMotionPossible)
        {
            if (inferred) return HandPoseSource.Inferred;
            return highConfidence && (sourceKnown || !wideMotionPossible) ? HandPoseSource.DirectHigh : HandPoseSource.DirectLow;
        }

        private static Vector3 Position(OVRPlugin.Posef pose) => new Vector3(pose.Position.x, pose.Position.y, -pose.Position.z);
        private static Quaternion Orientation(OVRPlugin.Posef pose) =>
            new Quaternion(-pose.Orientation.x, -pose.Orientation.y, pose.Orientation.z, pose.Orientation.w);
        private static bool Direct(WingInput hand) => hand.Tracked && hand.Source == HandPoseSource.DirectHigh;
    }

    public sealed class HandMenuGesture
    {
        private float held;
        private bool armed;
        public bool Sample(bool tracked, bool leftPinched, bool rightPinched, bool forward, float dt)
        {
            if (!tracked) { armed = false; held = 0; return false; }
            if (!leftPinched && !rightPinched) { armed = true; held = 0; return false; }
            if (!armed || !leftPinched || !rightPinched || !forward) { held = 0; return false; }
            held += Mathf.Max(0, dt);
            if (held < .6f) return false;
            armed = false; held = 0; return true;
        }
        public void Reset() { held = 0; armed = false; }
    }

    public sealed class MetaHandFlightInput : ITrackedFlightInput
    {
        private readonly Func<HandTrackingFrame> read;
        private readonly TrackedBodyFrame body = new TrackedBodyFrame();
        private readonly HandPoseContinuity left = new HandPoseContinuity(), right = new HandPoseContinuity();
        private readonly HandMenuGesture menu = new HandMenuGesture();
        private readonly HandDragGesture leftDrag = new HandDragGesture(), rightDrag = new HandDragGesture();
        private readonly BackstrokeFlare backstroke = new BackstrokeFlare();
        private HandPoseSource previousLeftSource = HandPoseSource.Lost, previousRightSource = HandPoseSource.Lost;
        public MetaHandFlightInput(Func<HandTrackingFrame> source = null) { read = source ?? HandInteraction.Sample; }
        public FlightInputFrame LastDeviceFrame { get; private set; }
        public FlightInputFrame LastRawFrame { get; private set; }
        public bool LastWingsEnabled { get; private set; }
        public bool WingsEnabled { get; set; }
        public bool ResetEnabled { get; set; }
        public string Mode => "Hands / Meta OpenXR";

        public FlightInputFrame Sample(float deltaTime)
        {
            var sample = read();
            var frame = FlightInputFrame.Neutral;
            frame.HeadPosition = sample.HeadPosition; frame.HeadOrientation = sample.HeadOrientation;
            frame.HeadTracked = sample.HeadTracked && HandPoseContinuity.Valid(sample.HeadOrientation)
                && HandPoseContinuity.Finite(sample.HeadPosition);
            frame.LookDirection = frame.HeadTracked ? frame.HeadOrientation * Vector3.forward : Vector3.forward;
            frame.LeftWing = sample.Left; frame.RightWing = sample.Right;
            if (!frame.HeadTracked) { left.Reset(); right.Reset(); body.ResetDerivatives(); }
            else if (sample.Left.Source != previousLeftSource || sample.Right.Source != previousRightSource)
            { left.ResetMotion(); right.ResetMotion(); body.ResetDerivatives(); }
            previousLeftSource=sample.Left.Source;previousRightSource=sample.Right.Source;
            var bodyFrame=frame;
            bodyFrame.LeftWing.Tracked &= sample.Left.Source==HandPoseSource.DirectHigh && HandPoseContinuity.Finite(sample.Left.Position);
            bodyFrame.RightWing.Tracked &= sample.Right.Source==HandPoseSource.DirectHigh && HandPoseContinuity.Finite(sample.Right.Position);
            var priorHeading=body.Heading;bool hadHeading=body.HasHeading;
            body.Sample(ref bodyFrame,deltaTime);
            if(hadHeading && Quaternion.Angle(priorHeading,body.Heading)>15f) { left.ResetMotion();right.ResetMotion(); }
            frame.BodyOrientation=bodyFrame.BodyOrientation;frame.BodyTracked=bodyFrame.BodyTracked;
            if(!frame.HeadTracked)frame.LeftWing.Tracked=frame.RightWing.Tracked=false;
            var raw = frame;
            frame.LeftWing = left.Sample(frame.LeftWing, frame.HeadPosition, body.Heading, deltaTime);
            frame.RightWing = right.Sample(frame.RightWing, frame.HeadPosition, body.Heading, deltaTime);
            frame.Bank = frame.LeftWing.Tracked && frame.RightWing.Tracked
                ? Mathf.Clamp(frame.LeftWing.Position.y - frame.RightWing.Position.y, -1f, 1f) : 0;
            bool reliable = frame.HeadTracked && !frame.LeftWing.MotionEstimated && !frame.RightWing.MotionEstimated;
            var inverse = Quaternion.Inverse(frame.HeadOrientation);
            var l = inverse * (sample.Left.Position - frame.HeadPosition);
            var r = inverse * (sample.Right.Position - frame.HeadPosition);
            bool forward = l.z > .08f && r.z > .08f && Mathf.Abs(l.x) < .65f && Mathf.Abs(r.x) < .65f;
            frame.CharacterSelectPressed = menu.Sample(reliable, sample.LeftPinched, sample.RightPinched, forward, deltaTime);
            frame.ButtonsHeld = (sample.LeftPinched ? 512u : 0u) | (sample.RightPinched ? 1024u : 0u);
            // Each drag needs only its own measured hand; the idle hand may rest out of view.
            var leftMove = leftDrag.Sample(frame.HeadTracked && !frame.LeftWing.MotionEstimated, sample.LeftPinched,
                Quaternion.Inverse(body.Heading) * (sample.Left.Position-frame.HeadPosition));
            var rightMove = rightDrag.Sample(frame.HeadTracked && !frame.RightWing.MotionEstimated, sample.RightPinched,
                Quaternion.Inverse(body.Heading) * (sample.Right.Position-frame.HeadPosition));
            frame.GroundMove = new Vector2(leftMove.x,leftMove.z);
            frame.GroundTurn = rightMove.x;
            // Sweeping both hands forward pushes air forward: brake until a normal downstroke.
            var inverseHeading = Quaternion.Inverse(body.Heading);
            frame.Flare = backstroke.Sample(frame.HeadTracked && !frame.LeftWing.MotionEstimated && !frame.RightWing.MotionEstimated,
                inverseHeading * frame.LeftWing.Velocity, inverseHeading * frame.RightWing.Velocity, deltaTime);
            // Pinch is never an airborne tuck/flare. Span and raised-wing physics retain
            // those existing physical verbs; menus consume pinch separately.
            raw.LeftWing.Velocity = frame.LeftWing.Velocity; raw.RightWing.Velocity = frame.RightWing.Velocity;
            raw.LeftWing.MotionEstimated = frame.LeftWing.MotionEstimated; raw.RightWing.MotionEstimated = frame.RightWing.MotionEstimated;
            raw.ButtonsHeld = frame.ButtonsHeld; raw.CharacterSelectPressed = frame.CharacterSelectPressed;
            raw.GroundMove=frame.GroundMove;raw.GroundTurn=frame.GroundTurn;raw.Flare=frame.Flare;
            LastDeviceFrame = raw; LastRawFrame = frame; LastWingsEnabled = WingsEnabled;
            if (!WingsEnabled)
            {
                frame.LeftWing = FlightInputFrame.Neutral.LeftWing; frame.RightWing = FlightInputFrame.Neutral.RightWing;
                frame.Bank = frame.Tuck = frame.Flare = 0; frame.BodyTracked = false;
            }
            return frame;
        }
        // Calibration's measured real-minus-inferred offsets, in the body heading frame.
        public void SetInferredBias(bool leftKnown, Vector3 leftBias, bool rightKnown, Vector3 rightBias)
        {
            if (leftKnown) left.SetInferredBias(leftBias);
            if (rightKnown) right.SetInferredBias(rightBias);
        }
        public void ResetDerivatives() { body.ResetDerivatives(); left.Reset(); right.Reset(); menu.Reset(); leftDrag.Reset();rightDrag.Reset(); backstroke.Reset(); }
        public void ResetTrackingOrigin() { body.Reset(); ResetDerivatives(); }
        public void Dispose() { }
    }

    // A deliberate forward sweep of both measured hands latches the flare brake; a normal
    // downstroke releases it. Recorded downstrokes stay below +0.75 m/s forward at p99.
    public sealed class BackstrokeFlare
    {
        public const float ForwardSpeed = 1f, HoldSeconds = .1f, RampSeconds = .25f;
        private float charge, output;
        private bool latched;
        public bool Latched => latched;
        public float Sample(bool measured, Vector3 leftBody, Vector3 rightBody, float dt)
        {
            dt = Mathf.Max(0, dt);
            if (measured)
            {
                bool sweep = Mathf.Min(leftBody.z, rightBody.z) > ForwardSpeed;
                charge = sweep ? charge + dt : 0f;
                if (charge >= HoldSeconds) latched = true;
                bool powerStroke = (leftBody.y + rightBody.y) * .5f < -.8f && Mathf.Max(leftBody.z, rightBody.z) < .3f;
                if (powerStroke) { latched = false; charge = 0f; }
            }
            else charge = 0f;
            output = Mathf.MoveTowards(output, latched ? 1f : 0f, dt / RampSeconds);
            return output;
        }
        public void Reset() { charge = output = 0f; latched = false; }
    }

    // A small relative hand displacement replaces each ground stick. The solver
    // only consumes these axes while supported; a pinch alone never moves the bird.
    public sealed class HandDragGesture
    {
        private Vector3 origin;
        private bool armed, held;
        public Vector3 Sample(bool tracked, bool pinched, Vector3 position)
        {
            if (!tracked) { Reset(); return Vector3.zero; }
            if (!pinched) { armed=true;held=false;return Vector3.zero; }
            if (!armed) return Vector3.zero;
            if (!held) { origin=position;held=true;return Vector3.zero; }
            var delta=position-origin;
            return delta.magnitude<.025f?Vector3.zero:Vector3.ClampMagnitude(delta/.16f,1f);
        }
        public void Reset() { armed=held=false; }
    }
}
