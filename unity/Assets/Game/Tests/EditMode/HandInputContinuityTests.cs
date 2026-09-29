using NUnit.Framework;
using UnityEngine;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public class HandInputContinuityTests
    {
        private static WingInput Pose(HandPoseSource source, float y, double time) => new WingInput
        {
            Source = source, Tracked = source != HandPoseSource.Lost,
            Position = new Vector3(-.45f, y, .25f), Orientation = Quaternion.identity,
            SampleTimestamp = time, HasUnextrapolatedPose = true,
            UnextrapolatedPosition = new Vector3(-.45f, y, .25f),
            UnextrapolatedOrientation = Quaternion.identity, UnextrapolatedTimestamp = time
        };
        private static WingInput Step(HandPoseContinuity filter, WingInput pose, float dt=.02f) =>
            filter.Sample(pose, Vector3.zero, Quaternion.identity, dt);
        private static void Warm(HandPoseContinuity filter)
        { for (int i=0;i<12;i++) Step(filter,Pose(HandPoseSource.DirectHigh,0,1+i*.02)); }

        [Test]
        public void SourceTransitionsCannotDifferentiateAcrossAnInferredGap()
        {
            var f=new HandPoseContinuity();Warm(f);
            var moving=Step(f,Pose(HandPoseSource.DirectHigh,-.03f,1.24));
            Assert.That(moving.MotionEstimated,Is.False);Assert.That(moving.Velocity.y,Is.EqualTo(-1.5f).Within(.001f));
            var inferred=Step(f,Pose(HandPoseSource.Inferred,-.2f,1.26));
            Assert.That(inferred.MotionEstimated,Is.True);Assert.That(inferred.Velocity,Is.EqualTo(Vector3.zero));
            var recovered=Step(f,Pose(HandPoseSource.DirectHigh,-.5f,1.28));
            Assert.That(recovered.MotionEstimated,Is.True);Assert.That(recovered.Velocity,Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void MissingPoseHasBoundedHoldAndNoEnergy()
        {
            var f=new HandPoseContinuity();Warm(f);
            var grace=Step(f,default,.08f);
            Assert.That(grace.Tracked,Is.True);Assert.That(grace.MotionEstimated,Is.True);
            Assert.That(grace.Velocity,Is.EqualTo(Vector3.zero));
            var lost=Step(f,default,.12f);Assert.That(lost.Tracked,Is.False);
        }

        [Test]
        public void NativeCaptureIntervalControlsVelocityInsteadOfRenderInterval()
        {
            var f=new HandPoseContinuity();Warm(f);
            var moving=Step(f,Pose(HandPoseSource.DirectHigh,-.08f,1.26),.02f);
            Assert.That(moving.Velocity.y,Is.EqualTo(-2f).Within(.001f));
            var duplicate=Step(f,Pose(HandPoseSource.DirectHigh,-.08f,1.26),.01f);
            Assert.That(duplicate.Velocity,Is.EqualTo(moving.Velocity));
            var stale=Step(f,Pose(HandPoseSource.DirectHigh,-.08f,1.26),.11f);
            Assert.That(stale.MotionEstimated,Is.True);Assert.That(stale.Velocity,Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void TeleportAndLowConfidenceCannotBecomeFlaps()
        {
            var f=new HandPoseContinuity();Warm(f);
            var jump=Step(f,Pose(HandPoseSource.DirectHigh,-1,1.24));
            Assert.That(jump.MotionEstimated,Is.True);Assert.That(jump.Velocity,Is.EqualTo(Vector3.zero));
            var low=Step(f,Pose(HandPoseSource.DirectLow,-1.1f,1.26));
            Assert.That(low.MotionEstimated,Is.True);Assert.That(low.Velocity,Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void MenuRequiresBothForwardPinchesHeldAndReleaseAfterLoss()
        {
            var menu=new HandMenuGesture();
            Assert.That(menu.Sample(true,true,true,true,1),Is.False);
            menu.Sample(true,false,false,true,.1f);
            Assert.That(menu.Sample(true,true,false,true,1),Is.False);
            Assert.That(menu.Sample(true,true,true,false,1),Is.False);
            Assert.That(menu.Sample(true,true,true,true,.3f),Is.False);
            Assert.That(menu.Sample(true,true,true,true,.31f),Is.True);
            Assert.That(menu.Sample(true,true,true,true,1),Is.False);
            menu.Sample(false,false,false,true,1);
            Assert.That(menu.Sample(true,true,true,true,1),Is.False);
        }

        [Test]
        public void ChangingMeasuredPoseAvailabilityCannotDifferentiateAcrossStreams()
        {
            var f=new HandPoseContinuity();Warm(f);
            var reading=Pose(HandPoseSource.DirectHigh,-.15f,1.24);
            reading.HasUnextrapolatedPose=false;
            var switched=Step(f,reading);
            Assert.That(switched.MotionEstimated,Is.True);Assert.That(switched.Velocity,Is.EqualTo(Vector3.zero));
            reading.HasUnextrapolatedPose=true;reading.UnextrapolatedTimestamp=1.26;
            var restored=Step(f,reading);
            Assert.That(restored.MotionEstimated,Is.True);Assert.That(restored.Velocity,Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void PinchDragRequiresReleaseAndMovementAndStopsOnLoss()
        {
            var drag=new HandDragGesture();
            Assert.That(drag.Sample(true,true,Vector3.one),Is.EqualTo(Vector3.zero));
            drag.Sample(true,false,Vector3.zero);drag.Sample(true,true,Vector3.zero);
            Assert.That(drag.Sample(true,true,Vector3.forward*.08f).z,Is.EqualTo(.5f).Within(.001f));
            Assert.That(drag.Sample(false,true,Vector3.one),Is.EqualTo(Vector3.zero));
            Assert.That(drag.Sample(true,true,Vector3.one),Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void PinchNeverBecomesAirborneTuckOrRecalibration()
        {
            var sample=new HandTrackingFrame {HeadTracked=true,HeadOrientation=Quaternion.identity,
                Left=Pose(HandPoseSource.DirectHigh,0,1),Right=Pose(HandPoseSource.DirectHigh,0,1),RightPinched=true};
            sample.Right.Position.x=.45f;sample.Right.UnextrapolatedPosition.x=.45f;
            using(var input=new MetaHandFlightInput(()=>sample){WingsEnabled=true})
            {
                var frame=input.Sample(.02f);
                Assert.That(frame.Tuck,Is.Zero);Assert.That(frame.Flare,Is.Zero);
                Assert.That(frame.RecalibratePressed,Is.False);Assert.That(frame.ResetPressed,Is.False);
                Assert.That(frame.ButtonsHeld&1024u,Is.Not.Zero);
            }
        }

        [Test]
        public void SourceQueryFailureBlocksMotionOnlyWhenWideMotionCanInfer()
        {
            Assert.That(HandInteraction.ClassifySource(true, true, true, true), Is.EqualTo(HandPoseSource.Inferred));
            Assert.That(HandInteraction.ClassifySource(true, false, true, true), Is.EqualTo(HandPoseSource.DirectHigh));
            Assert.That(HandInteraction.ClassifySource(false, false, true, true), Is.EqualTo(HandPoseSource.DirectLow),
                "With Wide Motion Mode on, an unknown source stays fail-closed");
            Assert.That(HandInteraction.ClassifySource(false, false, true, false), Is.EqualTo(HandPoseSource.DirectHigh),
                "Denied body tracking still leaves confident camera hands usable");
            Assert.That(HandInteraction.ClassifySource(false, false, false, false), Is.EqualTo(HandPoseSource.DirectLow));
        }

        [Test]
        public void InferredPoseStaysContinuousWithTheLastCameraPose()
        {
            var f=new HandPoseContinuity();Warm(f);
            // Recorded Direct->Inferred edges jumped a median 25-39 cm.
            var first=Step(f,Pose(HandPoseSource.Inferred,.35f,1.26));
            Assert.That(first.Position.y,Is.EqualTo(0f).Within(.02f),"No wing or bank pop at the handoff");
            WingInput later=first;
            for(int i=0;i<10;i++)later=Step(f,Pose(HandPoseSource.Inferred,.45f,1.28+i*.02));
            Assert.That(later.Position.y-first.Position.y,Is.EqualTo(.1f+HandPoseContinuity.InferredOffsetRelax*.2f).Within(.01f),
                "Inferred motion still moves the wing while the handoff offset relaxes slowly");
            Assert.That(later.MotionEstimated,Is.True);Assert.That(later.Velocity,Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ForwardSweepLatchesFlareUntilANormalDownstroke()
        {
            var flare=new BackstrokeFlare();
            var sweep=new Vector3(0,-.3f,1.4f);
            for(int i=0;i<3;i++)flare.Sample(true,sweep,sweep,.02f);
            Assert.That(flare.Latched,Is.False,"A single forward jerk is not a backstroke");
            for(int i=0;i<4;i++)flare.Sample(true,sweep,sweep,.02f);
            Assert.That(flare.Latched,Is.True);
            float held=0;for(int i=0;i<30;i++)held=flare.Sample(true,Vector3.zero,Vector3.zero,.02f);
            Assert.That(held,Is.EqualTo(1f),"Flare holds through a glide");
            flare.Sample(true,new Vector3(0,-1.5f,-.2f),new Vector3(0,-1.5f,-.2f),.02f);
            Assert.That(flare.Latched,Is.False,"A normal downstroke resumes flight");
        }

        [Test]
        public void RecordedDownstrokesAndEstimatedMotionNeverLatchFlare()
        {
            var flare=new BackstrokeFlare();
            // p99 recorded hand downstroke carried +0.73 m/s forward motion.
            var natural=new Vector3(0,-2f,.75f);
            for(int i=0;i<60;i++)flare.Sample(true,natural,natural,.02f);
            Assert.That(flare.Latched,Is.False);
            var sweep=new Vector3(0,0,2f);
            for(int i=0;i<60;i++)flare.Sample(false,sweep,sweep,.02f);
            Assert.That(flare.Latched,Is.False,"Only measured motion can brake");
            for(int i=0;i<60;i++)flare.Sample(true,sweep,new Vector3(0,0,.2f),.02f);
            Assert.That(flare.Latched,Is.False,"Both hands must sweep forward");
        }
    }
}
