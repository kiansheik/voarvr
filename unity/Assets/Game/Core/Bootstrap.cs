using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoarVR.Core
{
    public sealed class Bootstrap : MonoBehaviour
    {
        private void Start() => SceneManager.LoadScene("BirdFlight");
    }
}
