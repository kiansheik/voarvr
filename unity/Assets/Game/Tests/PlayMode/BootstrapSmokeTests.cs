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
            cards.First().onClick.Invoke();
            for (int i = 0; i < 120 && SceneManager.GetActiveScene().name != "BirdFlight"; i++)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BirdFlight"));
            yield return null;
            var driver = Object.FindAnyObjectByType<BirdFlightDriver>();
            Assert.That(driver, Is.Not.Null);
            Assert.That(driver.Controller, Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(GameObject.Find("Ground"), Is.Not.Null);
            Assert.That(GameObject.Find("Perch_03"), Is.Not.Null);
            foreach (var renderer in Object.FindObjectsByType<Renderer>())
            {
                Assert.That(renderer.sharedMaterial, Is.Not.Null, renderer.name + " has a missing material");
                Assert.That(renderer.sharedMaterial.shader.isSupported, Is.True, renderer.name + " uses an unsupported shader");
            }
            Assert.That(driver.Controller.State.Speed, Is.GreaterThan(0f));
        }
    }
}
