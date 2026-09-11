using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VoarVR.Input;
using VoarVR.UI;

namespace VoarVR.Tests
{
    public sealed class JourneyUiLifecycleTests
    {
        [UnityTest]
        public IEnumerator ExplicitHelpIsQuietAndOwnedCanvasDoesNotAccumulate()
        {
            var cameraObject = new GameObject("Help test eye"); var camera = cameraObject.AddComponent<Camera>();
            var owner = new GameObject("Help owner");
            for (int cycle = 0; cycle < 2; cycle++)
            {
                int actions = 0;
                var menu = owner.AddComponent<FlightMenu>();
                menu.Configure(camera, _ => actions++, () => "Bring the seed to the garden.");
                var panel = menu.Panel;
                menu.Configure(camera, _ => actions++, () => "Bring the seed to the garden.");
                Assert.That(menu.Panel, Is.SameAs(panel), "Repeated configuration must reuse the owned canvas");
                var frame = FlightInputFrame.Neutral;
                menu.HandleInput(frame, true);
                Assert.That(menu.Visible, Is.False, "Pausing alone must not restore text");
                frame.Tuck = 1; menu.HandleInput(frame, true);
                Assert.That(menu.Visible, Is.True); Assert.That(actions, Is.Zero);
                Assert.That(menu.StatusText, Does.Contain("seed"));
                foreach (var image in panel.GetComponentsInChildren<Image>())
                    Assert.That(image.material.shader.isSupported, Is.True, "Help uses the existing supported Canvas/Image path");
                frame.Tuck = 0; menu.HandleInput(frame, true);
                frame.Tuck = 1; menu.HandleInput(frame, true);
                menu.HandleInput(frame, true); Assert.That(actions, Is.EqualTo(1));
                menu.HandleInput(frame, false); Assert.That(menu.Visible, Is.False);
                Object.Destroy(menu); yield return null; yield return null;
                Assert.That(panel == null, Is.True, "Removing the component must destroy the independently positioned canvas");
            }
            Object.Destroy(owner); Object.Destroy(cameraObject); yield return null;
        }

        [UnityTest]
        public IEnumerator PreflightExplainsStoryAndSelectionWaitsForDeliberateLaunch()
        {
            yield return SceneManager.LoadSceneAsync("CharacterSelect"); yield return null;
            var menu = Object.FindAnyObjectByType<CharacterSelectController>();
            menu.ChooseActivity(VoarVR.Gameplay.FlightActivity.RouteHome);
            Assert.That(menu.PreviewText, Does.Contain("Untimed"));
            var characters = Resources.LoadAll<VoarVR.Flight.BirdCharacterDefinition>(VoarVR.Flight.CharacterSelection.ResourcesFolder);
            foreach (var character in characters)
            {
                menu.ChooseCharacter(character);
                Assert.That(menu.SelectedCharacter, Is.EqualTo(character));
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("CharacterSelect"));
            }
            var text = string.Join("\n", menu.PreflightPanel.GetComponentsInChildren<Text>().Select(t => t.text));
            Assert.That(text, Does.Contain("RIGHT TRIGGER"));
            Assert.That(text, Does.Contain("calibrate"));
            Assert.That(text, Does.Contain("Instruments start hidden"));
            var panel = menu.PreflightPanel;
            Object.Destroy(menu); yield return null; yield return null;
            Assert.That(panel == null, Is.True);
        }
    }
}
