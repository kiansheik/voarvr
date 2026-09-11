using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.UI;

namespace VoarVR.Tests
{
    public sealed class FlightFeedbackLifecycleTests
    {
        [UnityTest]
        public IEnumerator PauseAndAudioPreferenceSilenceEventsAndRemovingFeedbackCleansSources()
        {
            bool oldAudio = FlightPreferences.AudioEnabled;
            try
            {
                yield return SceneManager.LoadSceneAsync("BirdFlight"); yield return null;
                var driver = Object.FindAnyObjectByType<BirdFlightDriver>();
                var feedback = driver.GetComponent<FlightFeedback>();
                var sources = driver.GetComponents<AudioSource>();
                Assert.That(sources.Length, Is.EqualTo(3));
                feedback.Configure(driver, Object.FindAnyObjectByType<VoarVR.World.WindField>(), driver.GetComponent<BirdRigDriver>());
                Assert.That(driver.GetComponents<AudioSource>().Length, Is.EqualTo(3), "Configuration must not allocate another set of voices");
                FlightPreferences.AudioEnabled = true; feedback.PreferencesChanged();
                feedback.StoryCue(true);
                var crown = driver.GetComponent<VoarVR.World.JourneyPresentation>().CrownAnchor;
                var restoration = crown.GetComponent<AudioSource>();
                Assert.That(restoration, Is.Not.Null, "Restoration should direct attention to the actual crown");
                Assert.That(restoration.spatialBlend, Is.EqualTo(1));
                feedback.StoryCue(true);
                Assert.That(crown.GetComponents<AudioSource>().Length, Is.EqualTo(1), "Restoration owns one bounded voice");
                driver.Controller.SetPaused(true); feedback.Tick(.02f);
                feedback.SnackCue(5); feedback.TrickCue(); feedback.StoryCue(false); feedback.StoryCue(true);
                foreach (var source in sources) Assert.That(source.isPlaying, Is.False, "Event entry points must respect pause immediately");
                Assert.That(restoration.isPlaying, Is.False);
                driver.Controller.SetPaused(false);
                FlightPreferences.AudioEnabled = false; feedback.PreferencesChanged(); feedback.Tick(.02f);
                feedback.SnackCue(5); feedback.TrickCue(); feedback.StoryCue(false); feedback.StoryCue(true);
                foreach (var source in sources) Assert.That(source.isPlaying, Is.False, "Audio mute applies to cues and breeze");
                Object.Destroy(feedback); yield return null; yield return null;
                foreach (var source in sources) Assert.That(source == null, Is.True, "Component removal must release its owned voices");
                Assert.That(restoration == null, Is.True, "Component removal must release its world-space voice");
            }
            finally { FlightPreferences.AudioEnabled = oldAudio; }
        }
    }
}
