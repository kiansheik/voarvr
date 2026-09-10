using UnityEngine;
using UnityEngine.UI;
namespace VoarVR.UI
{
    // Use the same build-referenced UI material as the instruments, not a shader
    // found only by name at runtime (which can be stripped from Android builds).
    public sealed class FlightCard:MonoBehaviour
    {
        private TextMesh text;private GameObject plate;private RectTransform rect;private float next;
        private void Start()
        {
            text=GetComponent<TextMesh>();
            plate=new GameObject("Ink backing",typeof(RectTransform),typeof(Canvas),typeof(Image));
            plate.transform.SetParent(transform,false);rect=plate.GetComponent<RectTransform>();
            var canvas=plate.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            var image=plate.GetComponent<Image>();image.color=new Color(.025f,.075f,.075f,.95f);image.raycastTarget=false;
        }
        private void LateUpdate()
        {
            if(text==null || plate==null)return;
            bool visible=text.GetComponent<MeshRenderer>().enabled && !string.IsNullOrEmpty(text.text);
            plate.SetActive(visible);if(!visible || Time.unscaledTime<next)return;next=Time.unscaledTime+.2f;
            var bounds=text.GetComponent<MeshRenderer>().localBounds;
            rect.localPosition=new Vector3(bounds.center.x,bounds.center.y,.012f);
            rect.sizeDelta=new Vector2(Mathf.Max(.15f,bounds.size.x+.045f),Mathf.Max(.06f,bounds.size.y+.035f));
        }
        private void OnDisable(){if(plate!=null)plate.SetActive(false);}
        private void OnDestroy(){if(plate!=null)Destroy(plate);}
    }
}
