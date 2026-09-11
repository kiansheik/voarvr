using NUnit.Framework;
using VoarVR.Flight;

namespace VoarVR.Tests
{
    public sealed class AirFeedbackTests
    {
        [Test]
        public void AvailableLiftDoesNotClaimSuccessWhileBirdDescends()
        {
            var cues = new AirFeedbackState();
            Assert.That(cues.Sample(.02f, true, 4f, -2f), Is.EqualTo(AirFeedbackCue.LiftAvailable));
            for (int i = 0; i < 500; i++)
                Assert.That(cues.Sample(.02f, true, 4f, -2f), Is.EqualTo(AirFeedbackCue.None));
        }
        [Test]
        public void ClimbingRequiresContinuousActualClimbAndHasBoundedRepeats()
        {
            var cues = new AirFeedbackState();
            for (int i = 0; i < 30; i++) Assert.That(cues.Sample(.02f, true, 0f, 2f), Is.EqualTo(AirFeedbackCue.None));
            Assert.That(cues.Sample(.02f, true, 0f, -.1f), Is.EqualTo(AirFeedbackCue.None));
            for (int i = 0; i < 42; i++) Assert.That(cues.Sample(.02f, true, 0f, 2f), Is.EqualTo(AirFeedbackCue.None));
            Assert.That(cues.Sample(.02f, true, 0f, 2f), Is.EqualTo(AirFeedbackCue.Climbing));
            for (int i = 0; i < 390; i++) Assert.That(cues.Sample(.02f, true, 0f, 2f), Is.EqualTo(AirFeedbackCue.None));
        }
        [Test]
        public void PausePerchOrLostTrackingCannotBankClimbingTime()
        {
            var cues = new AirFeedbackState();
            for (int i = 0; i < 40; i++) cues.Sample(.02f, true, 0f, 2f);
            Assert.That(cues.Sample(20f, false, 4f, 10f), Is.EqualTo(AirFeedbackCue.None));
            Assert.That(cues.Sample(.02f, true, 0f, 2f), Is.EqualTo(AirFeedbackCue.None));
            for (int i = 0; i < 40; i++) Assert.That(cues.Sample(.02f, true, 0f, 2f), Is.EqualTo(AirFeedbackCue.None));
            Assert.That(cues.Sample(.02f, true, 4f, -2f), Is.EqualTo(AirFeedbackCue.LiftAvailable));
        }
        [Test]
        public void LiftThresholdHysteresisAvoidsRepeatedEntryChatter()
        {
            var cues = new AirFeedbackState();
            Assert.That(cues.Sample(.02f, true, 2.1f, -1f), Is.EqualTo(AirFeedbackCue.LiftAvailable));
            for (int i = 0; i < 500; i++)
                Assert.That(cues.Sample(.02f, true, i % 2 == 0 ? 1.9f : 2.1f, -1f), Is.EqualTo(AirFeedbackCue.None));
            cues.Sample(.02f, true, 1.3f, -1f);
            Assert.That(cues.Sample(.02f, true, 2.1f, -1f), Is.EqualTo(AirFeedbackCue.LiftAvailable));
        }
    }
}
