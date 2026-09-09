using NUnit.Framework;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class FlightControllerTests
    {
        private sealed class SuppliedInput : IFlightInput
        {
            public FlightInputFrame Frame = FlightInputFrame.Neutral;
            public string Mode => "Test";
            public FlightInputFrame Sample(float deltaTime) => Frame;
        }

        [Test]
        public void InitialStateHasConfiguredGlideVelocityAtSpawn()
        {
            var spawn = new Vector3(2f, 4f, 6f);
            var controller = new BirdFlightController(new SuppliedInput(), spawn);
            Assert.That(controller.State.Position, Is.EqualTo(spawn));
            Assert.That(controller.State.Velocity, Is.EqualTo(Vector3.forward * BirdFlightProfile.Default.InitialSpeedMps));
            Assert.That(controller.State.Rotation, Is.EqualTo(Quaternion.identity));
            Assert.That(controller.State.Phase, Is.EqualTo(FlightPhase.Gliding));
        }

        [Test]
        public void SuppliedWingMotionDrivesMovement()
        {
            var input = new SuppliedInput();
            input.Frame.LeftWing.Velocity = input.Frame.RightWing.Velocity = Vector3.down * 2f;
            var controller = new BirdFlightController(input, Vector3.zero);
            controller.Step(0.25f);
            Assert.That(controller.State.Position.y, Is.GreaterThan(0f));
            Assert.That(controller.State.Position.z, Is.GreaterThan(0f));
            Assert.That(controller.State.Phase, Is.EqualTo(FlightPhase.Flapping));
        }

        [TestCase(SyntheticGesture.Glide)]
        [TestCase(SyntheticGesture.Flap)]
        [TestCase(SyntheticGesture.BankLeft)]
        [TestCase(SyntheticGesture.BankRight)]
        [TestCase(SyntheticGesture.Dive)]
        [TestCase(SyntheticGesture.Flare)]
        public void SyntheticRunsRepeatWithoutHardware(SyntheticGesture gesture)
        {
            var first = new BirdFlightController(new SyntheticFlightInput(gesture), Vector3.zero);
            var second = new BirdFlightController(new SyntheticFlightInput(gesture), Vector3.zero);
            for (int i = 0; i < 120; i++) { first.Step(1f / 60f); second.Step(1f / 60f); }
            Assert.That(first.State.Position, Is.EqualTo(second.State.Position));
            Assert.That(first.State.Rotation, Is.EqualTo(second.State.Rotation));
            Assert.That(first.State.Speed, Is.GreaterThan(0f));
            if (gesture == SyntheticGesture.BankLeft) Assert.That(first.State.Position.x, Is.LessThan(0f));
            if (gesture == SyntheticGesture.BankRight) Assert.That(first.State.Position.x, Is.GreaterThan(0f));
            if (gesture == SyntheticGesture.Dive) Assert.That(first.State.Position.y, Is.LessThan(0f));
            // A prolonged flare stalls and descends; it cannot supply free ascent.
        }

        [Test]
        public void PauseFreezesPositionAndResetRestoresSpawn()
        {
            var input = new SuppliedInput();
            var controller = new BirdFlightController(input, Vector3.up);
            controller.Step(0.1f);
            var moved = controller.State.Position;
            input.Frame.PausePressed = true;
            controller.Step(0.1f);
            input.Frame.PausePressed = false;
            controller.Step(0.1f);
            Assert.That(controller.State.Position, Is.EqualTo(moved));
            Assert.That(controller.State.Phase, Is.EqualTo(FlightPhase.Paused));
            input.Frame.ResetPressed = true;
            controller.Step(0.1f);
            Assert.That(controller.State.Position, Is.EqualTo(Vector3.up));
            Assert.That(controller.State.Speed, Is.EqualTo(BirdFlightProfile.Default.InitialSpeedMps));
        }

        [Test]
        public void WingHeightAsymmetryBanksTowardLowerWing()
        {
            var input = new SuppliedInput();
            input.Frame.LeftWing.Position += Vector3.up * 0.3f;
            input.Frame.RightWing.Position += Vector3.down * 0.3f;
            var controller = new BirdFlightController(input, Vector3.zero);
            for (int i = 0; i < 60; i++) controller.Step(1f / 60f);
            Assert.That(controller.State.Position.x, Is.GreaterThan(0f));
        }

        [Test]
        public void ApproachingPerchAtLowSpeedLandsAndFlapLaunches()
        {
            var input = new SuppliedInput();
            var perches = new[] { new PerchInfo(new Vector3(0f, 0f, 3f), Quaternion.identity) };
            var controller = new BirdFlightController(input, Vector3.zero, perches);
            bool perched = false;
            for (int i = 0; i < 180 && !perched; i++)
            {
                controller.Step(1f / 60f);
                if (controller.State.Phase == FlightPhase.Perched) perched = true;
            }
            Assert.That(perched, Is.True);
            Assert.That(controller.State.Velocity, Is.EqualTo(Vector3.zero));

            input.Frame.LeftWing.Velocity = input.Frame.RightWing.Velocity = Vector3.down * 3f;
            controller.Step(1f / 60f);
            Assert.That(controller.State.Phase, Is.EqualTo(FlightPhase.Flapping));
            Assert.That(controller.State.Velocity.y, Is.GreaterThan(0f));
            Assert.That(controller.State.Velocity.z, Is.GreaterThan(0f));
        }
    }
}
