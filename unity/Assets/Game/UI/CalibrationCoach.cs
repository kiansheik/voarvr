using UnityEngine;
using UnityEngine.UI;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.UI
{
    // A diagram-led, camera-relative calibration step. It is deliberately independent
    // of the optional flight HUD so a first-time player can always understand the pose.
    public sealed class CalibrationCoach : MonoBehaviour
    {
        private static readonly Color Ink = new Color(.92f, .98f, .95f);
        private static readonly Color Muted = new Color(.62f, .76f, .73f);
        private static readonly Color Gold = new Color(.98f, .78f, .34f);
        private static readonly Color Ready = new Color(.42f, .92f, .63f);
        private static readonly Color Missing = new Color(.95f, .42f, .35f);

        private Canvas canvas;
        private Camera eye;
        private Text title;
        private Text explanation;
        private Text readiness;
        private Image leftController;
        private Image rightController;
        private Image headset;
        private bool redo;

        public Canvas Panel => canvas;
        public bool Visible => canvas != null && canvas.enabled;
        public string ReadinessText => readiness != null ? readiness.text : string.Empty;

        public void Configure(Camera camera)
        {
            if (canvas != null || camera == null) return;
            eye = camera;
            var root = new GameObject("Comfort calibration coach", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(camera.transform, false);
            root.transform.localPosition = new Vector3(0f, 0f, 2.05f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * .00105f;
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1540, 900);
            Box(root.transform, "Backdrop", Vector2.zero, rect.sizeDelta, new Color(.025f, .075f, .076f, .985f));
            Box(root.transform, "Top accent", new Vector2(0, 444), new Vector2(1540, 7), Gold);

            title = Label(root.transform, "Title", "MAKE THE FLIGHT FIT YOU", new Vector2(235, 360), new Vector2(940, 80), 46, Ink);
            explanation = Label(root.transform, "Explanation", string.Empty, new Vector2(235, 105), new Vector2(940, 390), 34, Ink);
            readiness = Label(root.transform, "Readiness", string.Empty, new Vector2(235, -252), new Vector2(940, 100), 34, Muted);

            var figure = Box(root.transform, "Pose diagram", new Vector2(-500, -15), new Vector2(390, 720), new Color(.05f, .13f, .13f, 1f));
            Circle(figure.transform, "Head", new Vector2(0, 205), 102, Ink);
            headset = Box(figure.transform, "Quest headset", new Vector2(7, 218), new Vector2(132, 48), Gold);
            Box(figure.transform, "Body", new Vector2(0, -20), new Vector2(48, 320), Ink);
            Box(figure.transform, "Left arm", new Vector2(-112, 92), new Vector2(224, 28), Ink);
            Box(figure.transform, "Right arm", new Vector2(112, 92), new Vector2(224, 28), Ink);
            leftController = Box(figure.transform, "Left controller", new Vector2(-242, 92), new Vector2(52, 78), Gold);
            rightController = Box(figure.transform, "Right controller", new Vector2(242, 92), new Vector2(52, 78), Gold);
            Box(figure.transform, "Left leg", new Vector2(-42, -250), new Vector2(28, 210), Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, -8);
            Box(figure.transform, "Right leg", new Vector2(42, -250), new Vector2(28, 210), Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, 8);
            var gaze = Box(figure.transform, "Comfortable gaze", new Vector2(118, 170), new Vector2(150, 10), Gold);
            gaze.rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
            var gazeLabel=Label(figure.transform, "Gaze label", "LOOK SLIGHTLY DOWN",
                new Vector2(0, -326), new Vector2(350, 48), 25, Gold);
            gazeLabel.alignment=TextAnchor.MiddleCenter;
            Label(root.transform, "Why", "This becomes your neutral flying posture. It is for comfort, not a test of reach.",
                new Vector2(0, -390), new Vector2(1400, 68), 29, Muted).alignment = TextAnchor.MiddleCenter;
            canvas.enabled = false;
        }

        public void Show(bool isRedo)
        {
            if (canvas == null) return;
            redo = isRedo;
            title.text = isRedo ? "RECALIBRATE WITHOUT LOSING YOUR FLIGHT" : "MAKE THE FLIGHT FIT YOU";
            explanation.text = "1   Face a comfortable direction\n\n2   Hold a relaxed T pose with both controllers\n\n3   Look comfortably a little below the horizon\n\n4   Press A when the pose is ready\n\nYou can return here at any time from the pause menu.";
            canvas.enabled = true;
        }

        public void Hide()
        {
            if (canvas != null) canvas.enabled = false;
        }

        public void UpdateReadiness(FlightInputFrame frame, BirdTrackingCalibration calibration)
        {
            if (!Visible || readiness == null) return;
            bool head = frame.HeadTracked;
            bool left = frame.LeftWing.Tracked;
            bool right = frame.RightWing.Tracked;
            bool pose = head && left && right && calibration != null && calibration.IsComfortableGlidePose(frame);
            headset.color = head ? Ready : Missing;
            leftController.color = left ? Ready : Missing;
            rightController.color = right ? Ready : Missing;
            readiness.color = pose ? Ready : Muted;
            readiness.text = pose
                ? "POSE READY  •  PRESS A"
                : !head || !left || !right
                    ? "TRACKING: " + (head ? "HEAD ✓" : "HEAD …") + "   " + (left ? "LEFT ✓" : "LEFT …") + "   " + (right ? "RIGHT ✓" : "RIGHT …")
                    : "RELAX YOUR SHOULDERS AND OPEN BOTH ARMS A LITTLE MORE";
        }

        private static Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }

        private static Image Circle(Transform parent, string name, Vector2 position, float diameter, Color color)
        {
            // The circular built-in sprite is unavailable in stripped players, so an
            // octagonal silhouette is represented by a square rotated over a square.
            var back = Box(parent, name, position, new Vector2(diameter, diameter), color);
            var diamond = Box(back.transform, "Diamond", Vector2.zero, new Vector2(diameter * .72f, diameter * .72f), color);
            diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            return back;
        }

        private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value; text.fontSize = fontSize; text.color = color; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
        }
    }
}
