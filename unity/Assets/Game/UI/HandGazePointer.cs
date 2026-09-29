using UnityEngine;
using UnityEngine.UI;
using VoarVR.Input;

namespace VoarVR.UI
{
    // An explicit release on the current target is required before each press. A
    // tracking recovery, opening panel or gaze change during a pinch cannot click.
    public sealed class HandPinchGate
    {
        private int releasedTarget;
        public void RequireRelease() => releasedTarget = 0;
        public bool Sample(bool tracked, bool pinched, int target)
        {
            if (!tracked || target == 0) { RequireRelease(); return false; }
            if (!pinched) { releasedTarget = target; return false; }
            bool activate = releasedTarget == target;
            RequireRelease();
            return activate;
        }
    }

    // Direct gaze-plane hit testing keeps the UI independent of controller rays
    // and EventSystem focus. The same real Buttons serve desktop/controller input.
    public sealed class HandGazePointer : MonoBehaviour
    {
        private static readonly Color Ivory = new Color(1f, .95f, .82f);
        private static readonly Color Outline = new Color(.02f, .07f, .07f);
        private readonly HandPinchGate gate = new HandPinchGate();
        private Canvas panel;
        private Camera eye;
        private Button[] buttons;
        private Image reticle;
        private RectTransform hoverBorder;
        public Button Hovered { get; private set; }
        public bool ReticleVisible => reticle != null && reticle.enabled;
        public bool HoverVisible => hoverBorder != null && hoverBorder.gameObject.activeSelf;

        public void Configure(Canvas canvas, Camera camera)
        {
            panel = canvas; eye = camera;
            buttons = canvas.GetComponentsInChildren<Button>(true);
            if (reticle == null)
            {
                // Two tones keep gaze feedback visible on gold primary buttons as well as
                // dark rows: an ivory core in a dark diamond, and an ivory line in a dark band.
                reticle = Graphic("Hand gaze reticle", canvas.transform, new Vector2(28, 28), Outline);
                reticle.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                Graphic("Core", reticle.transform, new Vector2(16, 16), Ivory);
                hoverBorder = new GameObject("Hand gaze target", typeof(RectTransform)).GetComponent<RectTransform>();
                hoverBorder.SetParent(canvas.transform, false);
                Frame(hoverBorder, 9f, 10f, Outline);
                Frame(hoverBorder, 3f, 4f, Ivory);
            }
            RequireRelease();
        }

        // An outline centred `outset` units outside its parent, `thickness` units wide.
        private static void Frame(RectTransform parent, float outset, float thickness, Color color)
        {
            var frame = new GameObject("Focus frame", typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(parent, false);
            frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
            frame.offsetMin = new Vector2(-outset, -outset); frame.offsetMax = new Vector2(outset, outset);
            Edge(frame, new Vector2(0, 0), new Vector2(1, 0), new Vector2(thickness, thickness), color);
            Edge(frame, new Vector2(0, 1), new Vector2(1, 1), new Vector2(thickness, thickness), color);
            Edge(frame, new Vector2(0, 0), new Vector2(0, 1), new Vector2(thickness, 0), color);
            Edge(frame, new Vector2(1, 0), new Vector2(1, 1), new Vector2(thickness, 0), color);
        }

        private static void Edge(RectTransform frame, Vector2 minimum, Vector2 maximum, Vector2 size, Color color)
        {
            var edge = Graphic("Focus edge", frame, size, color);
            edge.rectTransform.anchorMin = minimum; edge.rectTransform.anchorMax = maximum;
            edge.rectTransform.anchoredPosition = Vector2.zero;
        }

        private static Image Graphic(string label, Transform parent, Vector2 size, Color color)
        {
            var image = new GameObject(label, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false); image.rectTransform.sizeDelta = size;
            image.color = color; image.raycastTarget = false;
            return image;
        }

        public void RequireRelease()
        {
            gate.RequireRelease(); Hovered = null;
            if (reticle != null) reticle.enabled = false;
            if (hoverBorder != null) hoverBorder.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!HandInputSettings.UseHands || eye == null || !Application.isFocused)
            { RequireRelease(); return; }
            HandInteraction.Read(out bool headTracked, out bool handTracked, out bool pinched);
            ProcessSample(new Ray(eye.transform.position, eye.transform.forward), headTracked, handTracked, pinched);
        }

        public bool ProcessSample(Ray gaze, bool tracked, bool pinched) => ProcessSample(gaze, tracked, tracked, pinched);

        // Public deterministic boundary for tests and review fixtures; production
        // obtains confidence-filtered input exclusively from HandInteraction. Gaze
        // feedback needs only the head; activation needs a direct hand and a fresh pinch.
        public bool ProcessSample(Ray gaze, bool headTracked, bool handTracked, bool pinched)
        {
            if (panel == null || !panel.isActiveAndEnabled || !headTracked)
            { RequireRelease(); return false; }
            var plane = new Plane(panel.transform.forward, panel.transform.position);
            if (!plane.Raycast(gaze, out float distance) || distance < 0)
            { RequireRelease(); return false; }
            var point = gaze.GetPoint(distance);
            var panelRect = (RectTransform)panel.transform;
            if (!panelRect.rect.Contains(panelRect.InverseTransformPoint(point)))
            { RequireRelease(); return false; }
            reticle.rectTransform.position = point - panel.transform.forward * .002f;
            reticle.enabled = true;
            Button target = null;
            int targetId = 0;
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                var button = buttons[i];
                if (button == null || !button.isActiveAndEnabled || !button.IsInteractable()) continue;
                var rect = (RectTransform)button.transform;
                if (rect.rect.Contains(rect.InverseTransformPoint(point))) { target = button; targetId = i + 1; break; }
            }
            Hovered = target;
            hoverBorder.gameObject.SetActive(target != null);
            if (target != null)
            {
                var targetRect = (RectTransform)target.transform;
                hoverBorder.SetParent(targetRect, false);
                hoverBorder.anchorMin = Vector2.zero; hoverBorder.anchorMax = Vector2.one;
                hoverBorder.offsetMin = hoverBorder.offsetMax = Vector2.zero;
                var position = hoverBorder.localPosition; position.z = -1f; hoverBorder.localPosition = position;
            }
            reticle.transform.SetAsLastSibling();
            bool activate = gate.Sample(handTracked, pinched, targetId);
            if (activate) target.onClick.Invoke();
            return activate;
        }

        private void OnDisable() => RequireRelease();
    }
}
