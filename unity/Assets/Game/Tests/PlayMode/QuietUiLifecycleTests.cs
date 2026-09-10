using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VoarVR.Flight;
using VoarVR.UI;
namespace VoarVR.Tests
{
    public class QuietUiLifecycleTests
    {
        [UnityTest] public IEnumerator HiddenByDefaultAndNoCardsAccumulateAcrossFlightSessions()
        {
            for(int session=0;session<2;session++)
            {
                yield return SceneManager.LoadSceneAsync("BirdFlight");yield return null;
                var bird=Object.FindAnyObjectByType<BirdFlightDriver>();var hud=bird.GetComponent<FlightHud>();
                Assert.That(bird.ShowFlightText,Is.False);
                for(int i=0;i<25;i++)yield return null;
                Assert.That(Object.FindObjectsByType<FlightCard>(FindObjectsSortMode.None),Is.Empty);
                hud.SetVisible(true);
                yield return new WaitForSecondsRealtime(.3f);
                var cards=Object.FindObjectsByType<FlightCard>(FindObjectsSortMode.None);Assert.That(cards,Is.Not.Empty);
                foreach(var card in cards){var plate=card.transform.Find("Ink backing");Assert.That(plate,Is.Not.Null);Assert.That(plate.GetComponent<MeshRenderer>(),Is.Null);Assert.That(plate.GetComponent<UnityEngine.UI.Image>().material.shader.isSupported,Is.True);}
                hud.SetVisible(false);yield return new WaitForSecondsRealtime(.3f);
                Assert.That(Object.FindObjectsByType<FlightCard>(FindObjectsSortMode.None),Is.Empty);
                yield return SceneManager.LoadSceneAsync("CharacterSelect");yield return null;
                Assert.That(Object.FindObjectsByType<FlightCard>(FindObjectsInactive.Include,FindObjectsSortMode.None),Is.Empty);
            }
        }
        [UnityTest] public IEnumerator RemovingCardDestroysItsBacking()
        {
            var go=new GameObject("Card cleanup");go.AddComponent<TextMesh>().text="Test";var card=go.AddComponent<FlightCard>();yield return null;
            var backing=go.transform.Find("Ink backing");Assert.That(backing,Is.Not.Null);
            Object.Destroy(card);yield return null;yield return null;Assert.That(backing==null,Is.True);
            Object.Destroy(go);yield return null;
        }
    }
}
