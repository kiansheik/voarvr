using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VoarVR.Flight;

namespace VoarVR.Tests
{
    public sealed class BootstrapSmokeTests
    {
        [UnityTest]
        public IEnumerator BootstrapLoadsConnectedPrototype()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "CharacterSelect"; i++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("CharacterSelect"));
            yield return null;
            var cards = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            Assert.That(cards, Is.Not.Empty, "Character select screen built no selectable cards");
            cards.First(b => b.GetComponentInChildren<Text>().text.StartsWith("SELECT ")).onClick.Invoke();
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "BirdFlight"; i++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BirdFlight"));
            yield return null;
            var driver = Object.FindAnyObjectByType<BirdFlightDriver>();
            Assert.That(driver, Is.Not.Null);
            Assert.That(driver.Controller, Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<VoarVR.World.WorldStreamer>(), Is.Not.Null);
            Assert.That(driver.ViewMode, Is.EqualTo(FlightViewMode.ThirdPerson));
            Assert.That(Object.FindAnyObjectByType<VoarVR.World.UnityFlightEnvironment>(), Is.Not.Null);
            var bridge = Object.FindAnyObjectByType<VoarVR.World.ProceduralFlightWorld>();
            Assert.That(bridge.LandingMaterial, Is.Not.Null, "Landing shader needs a serialized build dependency");
            var guide = driver.GetComponent<VoarVR.World.LandingGuide>().GetComponentInChildren<LineRenderer>();
            Assert.That(guide.sharedMaterial.shader, Is.EqualTo(bridge.LandingMaterial.shader));
            foreach (var renderer in Object.FindObjectsByType<Renderer>())
            {
                Assert.That(renderer.sharedMaterial, Is.Not.Null, renderer.name + " has a missing material");
                Assert.That(renderer.sharedMaterial.shader.isSupported, Is.True, renderer.name + " uses an unsupported shader");
            }
            Assert.That(driver.Controller.State.Speed, Is.GreaterThan(0f));
        }
    }
}
