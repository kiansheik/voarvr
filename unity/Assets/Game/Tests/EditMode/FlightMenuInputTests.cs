using NUnit.Framework;
using VoarVR.Input;
using VoarVR.UI;
using UnityEngine;

namespace VoarVR.Tests
{
    public sealed class FlightMenuInputTests
    {
        [Test]
        public void ReturningTrackingRequiresReleaseBeforeOpening()
        {
            var input = new FlightMenuInputRouter();
            var frame = FlightInputFrame.Neutral;
            input.Sample(frame, true, false);
            input.RequireRelease();
            frame.Tuck = 1;
            Assert.That(input.Sample(frame, true, false).Toggle, Is.False);
            Assert.That(input.Sample(frame, true, false).Toggle, Is.False);
            frame.Tuck = 0; input.Sample(frame, true, false);
            frame.Tuck = 1;
            Assert.That(input.Sample(frame, true, false).Toggle, Is.True);
        }

        [Test]
        public void HeldFlightTriggerCannotOpenHelpWhenPausing()
        {
            var input = new FlightMenuInputRouter();
            var frame = FlightInputFrame.Neutral; frame.Tuck = 1;
            Assert.That(input.Sample(frame, false, false).Toggle, Is.False);
            Assert.That(input.Sample(frame, true, false).Toggle, Is.False);
            Assert.That(input.Sample(frame, true, false).Toggle, Is.False);
            frame.Tuck = .3f;
            Assert.That(input.Sample(frame, true, false).Toggle, Is.False, "Trigger hysteresis avoids chatter");
            frame.Tuck = 0; input.Sample(frame, true, false);
            frame.Tuck = 1;
            var opened = input.Sample(frame, true, false);
            Assert.That(opened.Toggle, Is.True);
            Assert.That(opened.Select, Is.False, "Opening must never select Continue on the same squeeze");
            Assert.That(input.Sample(frame, true, true).Select, Is.False);
            frame.Tuck = 0; input.Sample(frame, true, true);
            frame.Tuck = 1; Assert.That(input.Sample(frame, true, true).Select, Is.True);
        }

        [Test]
        public void NavigationRequiresStickReleaseAndNeverRepeatsWhileHeld()
        {
            var input = new FlightMenuInputRouter();
            var frame = FlightInputFrame.Neutral; input.Sample(frame, true, true);
            frame.GroundMove = Vector2.down;
            Assert.That(input.Sample(frame, true, true).Move, Is.EqualTo(1));
            Assert.That(input.Sample(frame, true, true).Move, Is.Zero);
            frame.GroundMove = Vector2.down * .4f; Assert.That(input.Sample(frame, true, true).Move, Is.Zero);
            frame.GroundMove = Vector2.up; Assert.That(input.Sample(frame, true, true).Move, Is.Zero);
            frame.GroundMove = Vector2.zero; input.Sample(frame, true, true);
            frame.GroundMove = Vector2.up; Assert.That(input.Sample(frame, true, true).Move, Is.EqualTo(-1));
        }

        [Test]
        public void FlightNeverConsumesMenuShortcutsAndKeyboardOpeningCannotSelect()
        {
            var input = new FlightMenuInputRouter();
            var frame = FlightInputFrame.Neutral;
            var flying = input.Sample(frame, false, false, true, 1, true);
            Assert.That(flying.Toggle || flying.Select || flying.Move != 0, Is.False);
            var paused = input.Sample(frame, true, false, true, 1, true);
            Assert.That(paused.Toggle, Is.True);
            Assert.That(paused.Select, Is.False);
            Assert.That(paused.Move, Is.Zero);
            Assert.That(input.Sample(frame, true, true, false, 1).Move, Is.EqualTo(1));
        }
    }
}
