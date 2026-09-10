using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.Tests
{
    public sealed class FlightRecoveryTests
    {
        private static FlightInputFrame ComfortableFrame()
        {
            var frame = FlightInputFrame.Neutral;
            frame.HeadTracked = true;
            frame.BodyTracked = true;
            frame.BodyOrientation = Quaternion.identity;
            frame.HeadPosition = new Vector3(0f, 1.65f, 0f);
            frame.LeftWing.Position = new Vector3(-.7f, 1.25f, .1f);
            frame.RightWing.Position = new Vector3(.7f, 1.25f, .1f);
            return frame;
        }

        [UnityTest]
        public IEnumerator PrimaryRestartsAndCalibratesWhilePlatformRecenterKeepsFlightState()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight");
            yield return null;
            var driver = Object.FindAnyObjectByType<BirdFlightDriver>();
            driver.enabled = false;
            driver.Controller.Reset();
            var spawn = driver.Controller.State.Position;
            for (int i = 0; i < 120; i++) driver.Tick(1f / 120f);
            Assert.That(Vector3.Distance(driver.Controller.State.Position, spawn), Is.GreaterThan(1f));
            Assert.That(driver.RestartAndCalibrate(ComfortableFrame()), Is.True);
            Assert.That(driver.Controller.State.Position, Is.EqualTo(spawn));
            Assert.That(driver.Calibration.Captured, Is.True);
            Assert.That(driver.Controller.SimulationTime, Is.Zero);
            Assert.That(driver.ViewMode, Is.EqualTo(FlightViewMode.ThirdPerson));
            for (int i = 0; i < 60; i++) driver.Tick(1f / 120f);
            driver.SetViewMode(FlightViewMode.ThirdPerson);
            var state = driver.Controller.State;
            typeof(BirdFlightDriver).GetProperty(nameof(BirdFlightDriver.UsesXR))
                .GetSetMethod(true).Invoke(driver, new object[] { true });
            driver.NotifyTrackingOriginUpdated();
            Assert.That(driver.Controller.State.Position, Is.EqualTo(state.Position));
            Assert.That(driver.Controller.State.Velocity, Is.EqualTo(state.Velocity));
            Assert.That(driver.ViewMode, Is.EqualTo(FlightViewMode.ThirdPerson));
            var missingTracking = ComfortableFrame();
            missingTracking.HeadTracked = false;
            for (int i = 0; i < 60; i++)
                Assert.That(driver.TryCompletePlatformRecenter(missingTracking, 1f / 60f), Is.False);
            Assert.That(driver.Calibration.Captured, Is.False);
            // Smooth movement under 2.5 cm per frame is not a stable hold window.
            for (int i = 0; i < 90; i++)
            {
                var moving = ComfortableFrame();
                moving.LeftWing.Position.y += i * .002f;
                moving.RightWing.Position.y += i * .002f;
                Assert.That(driver.TryCompletePlatformRecenter(moving, 1f / 90f), Is.False);
            }
            Assert.That(driver.TryCompletePlatformRecenter(ComfortableFrame(), 1f / 60f), Is.False,
                "The first fresh pose cannot calibrate a transient origin sample.");
            bool accepted = false;
            for (int i = 0; i < 30; i++)
                accepted |= driver.TryCompletePlatformRecenter(ComfortableFrame(), 1f / 60f);
            Assert.That(accepted, Is.True);
            Assert.That(driver.Calibration.Captured, Is.True);
            Assert.That(driver.Controller.State.Position, Is.EqualTo(state.Position));
            Assert.That(driver.Controller.State.Velocity, Is.EqualTo(state.Velocity));
        }

        [UnityTest]
        public IEnumerator ReturningToSelectionClearsSelectionAndUnpausesScene()
        {
            yield return SceneManager.LoadSceneAsync("BirdFlight");
            yield return null;
            var driver = Object.FindAnyObjectByType<BirdFlightDriver>();
            driver.enabled = false;
            CharacterSelection.Chosen = Resources.Load<BirdCharacterDefinition>("Characters/Dragon");
            Time.timeScale = 0f;
            driver.ReturnToCharacterSelect();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("CharacterSelect"));
            Assert.That(CharacterSelection.Chosen, Is.Null);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Object.FindAnyObjectByType<BirdFlightDriver>(), Is.Null);
        }
    }
}
