using NUnit.Framework;
using UnityEngine;
using VoarVR.Core;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class JourneyRecoveryTests
    {
        private sealed class Controls : IFlightInput
        {
            public FlightInputFrame Frame = FlightInputFrame.Neutral;
            public string Mode => "Recovery test";
            public FlightInputFrame Sample(float dt) => Frame;
        }

        [Test]
        public void RecoveryRequiresRealSupportAndPreservesPausedProgress()
        {
            var input = new Controls();
            var profile = BirdFlightProfile.Duck();
            var controller = new BirdFlightController(input, Vector3.up * 20, profile: profile,
                environment: new PlaneFlightEnvironment(0));
            controller.Step(.1f);
            controller.SetControlMode(FlightControlMode.Acrobatic);
            controller.SetPaused(true);
            float clock = controller.SimulationTime;
            var before = controller.State.Position;
            Assert.That(controller.TryRecoverToPerch(new Vector3(10, 200, 10), 0), Is.False);
            Assert.That(controller.TryRecoverToPerch(new Vector3(float.NaN, 0, 0), 0), Is.False);
            Assert.That(controller.State.Position, Is.EqualTo(before));
            var perch = new Vector3(10, profile.CollisionRadius + FlightContactSolver.Skin, 10);
            Assert.That(controller.TryRecoverToPerch(perch, 35), Is.True);
            Assert.That(controller.HasSupportedPerch, Is.True);
            Assert.That(controller.IsPaused, Is.True);
            Assert.That(controller.LandingCount, Is.Zero, "Relocation cannot award a physical landing");
            Assert.That(controller.TakeoffCount, Is.Zero);
            Assert.That(controller.SimulationTime, Is.EqualTo(clock));
            Assert.That(controller.ControlMode, Is.EqualTo(FlightControlMode.Acrobatic));
            controller.SetPaused(false);
            Assert.That(controller.State.Phase, Is.EqualTo(FlightPhase.Perched));
            Assert.That(controller.State.Velocity, Is.EqualTo(Vector3.zero));
            input.Frame.LeftWing.Velocity = input.Frame.RightWing.Velocity = Vector3.down * 2;
            controller.Step(1f / 120);
            Assert.That(controller.State.Phase, Is.Not.EqualTo(FlightPhase.Perched));
            Assert.That(controller.State.Velocity.y, Is.GreaterThan(0));
        }

        [Test]
        public void PauseRestoresVelocityWithoutAdvancingFlightTime()
        {
            var controller = new BirdFlightController(new Controls(), Vector3.up * 20);
            controller.Step(.1f);
            var state = controller.State;
            float clock = controller.SimulationTime;
            controller.SetPaused(true);
            for (int i = 0; i < 50; i++) controller.Step(.02f);
            Assert.That(controller.State.Position, Is.EqualTo(state.Position));
            Assert.That(controller.SimulationTime, Is.EqualTo(clock));
            controller.SetPaused(false);
            Assert.That(controller.State.Velocity, Is.EqualTo(state.Velocity));
            Assert.That(controller.State.Phase, Is.EqualTo(state.Phase));
        }

        [Test]
        public void MenuConfirmationAndNavigationMustReleaseBeforeFlight()
        {
            var input = new Controls();
            var gate = new FlightActionGate(input);
            input.Frame.Tuck = 1; input.Frame.GroundMove = Vector2.up;
            gate.RequireRelease();
            for (int i = 0; i < 10; i++)
            {
                var blocked = gate.Sample(.02f);
                Assert.That(blocked.Tuck, Is.Zero);
                Assert.That(blocked.GroundMove, Is.EqualTo(Vector2.zero));
            }
            input.Frame.Tuck = 0;
            Assert.That(gate.Sample(.02f).GroundMove, Is.EqualTo(Vector2.zero));
            input.Frame.Tuck = 1;
            Assert.That(gate.Sample(.02f).Tuck, Is.EqualTo(1));
            input.Frame.GroundMove = Vector2.zero;
            gate.Sample(.02f);
            input.Frame.GroundMove = Vector2.up;
            Assert.That(gate.Sample(.02f).GroundMove, Is.EqualTo(Vector2.up));
        }

        [Test]
        public void StabilizedCameraIsIndependentOfAcrobaticBodyAttitude()
        {
            var body = Quaternion.Euler(80, 35, 130);
            var heading = Quaternion.Euler(0, 35, 0);
            Assert.That(Quaternion.Angle(FlightCamera.FirstPersonBasis(FlightControlMode.Acrobatic, body, heading, true), heading), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(FlightCamera.FirstPersonBasis(FlightControlMode.Acrobatic, body, heading, false), body), Is.LessThan(.001f));
        }

        [Test]
        public void ChaseCameraSweepStopsAtSurfaceAndLeavesClearViewAlone()
        {
            var environment = new PlaneFlightEnvironment(0);
            var origin = new Vector3(0, 2, 0);
            var clear = new Vector3(0, 3, -2);
            Assert.That(FlightCamera.ConstrainChasePosition(origin, clear, environment), Is.EqualTo(clear));
            var constrained = FlightCamera.ConstrainChasePosition(origin, new Vector3(0, -1, -2), environment);
            Assert.That(constrained.y, Is.GreaterThanOrEqualTo(.14f));
            Assert.That(constrained.z, Is.InRange(-2f, 0f));
        }
    }
}
