using System;
using UnityEngine;
using UnityEngine.UI;
using VoarVR.Flight;
using VoarVR.Input;

namespace VoarVR.UI
{
    // A diagram-led, camera-relative calibration step. It is deliberately independent
    // of the optional flight HUD so a first-time player can always understand the pose.
    [DefaultExecutionOrder(1000)]
    public sealed class CalibrationCoach : MonoBehaviour
    {
        private static readonly Color Ink = new Color(.92f, .98f, .95f);
        private static readonly Color Muted = new Color(.62f, .76f, .73f);
        private static readonly Color Gold = new Color(.98f, .78f, .34f);
        private static readonly Color Ready = new Color(.42f, .92f, .63f);
        private static readonly Color Missing = new Color(.95f, .42f, .35f);
        private static readonly Color Waiting = new Color(.09f, .18f, .17f);
        private static readonly Color ButtonInk = new Color(.04f, .12f, .105f);
        // Hands cards sit about 15 degrees above the view axis, clear of the perched bird.
        public static readonly Vector3 HandCardOffset = new Vector3(0f, .55f, 2.05f);
        public const float ReadyHoldSeconds = .25f;

        private Canvas canvas;
        private Camera eye;
        private Text title;
        private Text explanation;
        private Text readiness;
        private Image leftController;
        private Image rightController;
        private Image headset;
        private bool redo;
        private Button confirm;
        private Image confirmRow;
        private Text confirmLabel;
        private HandGazePointer handPointer;
        private bool anchorAfterCameraPose;
        private float readyUntil;

        // Hands: fired when the hands-free countdown completes; capture that latest frame.
        public event Action ConfirmRequested;
        public readonly HandCalibrationFlow Flow = new HandCalibrationFlow();

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
            Box(root.transform, "Backdrop", Vector2.zero, rect.sizeDelta, new Color(.025f, .075f, .076f, 1f));
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
            if (HandInputSettings.UseHands)
            {
                // Bent elbows keep the illustrated hands within comfortable forward
                // tracking space, rather than requiring a literal full T pose.
                var leftArm = figure.transform.Find("Left arm").GetComponent<RectTransform>();
                var rightArm = figure.transform.Find("Right arm").GetComponent<RectTransform>();
                leftArm.anchoredPosition = new Vector2(-64, 61); leftArm.sizeDelta = new Vector2(126, 26);
                rightArm.anchoredPosition = new Vector2(64, 61); rightArm.sizeDelta = new Vector2(126, 26);
                leftArm.localRotation = Quaternion.Euler(0, 0, 35);
                rightArm.localRotation = Quaternion.Euler(0, 0, -35);
                Box(figure.transform, "Left bent forearm", new Vector2(-119, 72), new Vector2(80, 26), Ink)
                    .rectTransform.localRotation = Quaternion.Euler(0, 0, -60);
                Box(figure.transform, "Right bent forearm", new Vector2(119, 72), new Vector2(80, 26), Ink)
                    .rectTransform.localRotation = Quaternion.Euler(0, 0, 60);
                leftController.name = "Left hand"; rightController.name = "Right hand";
                leftController.rectTransform.anchoredPosition = new Vector2(-139, 115);
                rightController.rectTransform.anchoredPosition = new Vector2(139, 115);
                leftController.rectTransform.sizeDelta = rightController.rectTransform.sizeDelta = new Vector2(42, 60);
            }
            Box(figure.transform, "Left leg", new Vector2(-42, -232), new Vector2(28, 145), Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, -8);
            Box(figure.transform, "Right leg", new Vector2(42, -232), new Vector2(28, 145), Ink).rectTransform.localRotation = Quaternion.Euler(0, 0, 8);
            var gaze = Box(figure.transform, "Comfortable gaze", new Vector2(118, 170), new Vector2(150, 10), Gold);
            gaze.rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
            var gazeLabel=Label(figure.transform, "Gaze label", "LOOK SLIGHTLY DOWN",
                new Vector2(0, -326), new Vector2(350, 48), 25, Gold);
            gazeLabel.alignment=TextAnchor.MiddleCenter;
            Label(root.transform, "Why", "This becomes your neutral flying posture. It is for comfort, not a test of reach.",
                new Vector2(0, -390), new Vector2(1400, 68), 29, Muted).alignment = TextAnchor.MiddleCenter;
            if (HandInputSettings.UseHands)
            {
                readiness.rectTransform.anchoredPosition = new Vector2(235, -225);
                readiness.rectTransform.sizeDelta = new Vector2(940, 82);
                readiness.fontSize = 30;
                confirmRow = Box(root.transform, "Confirm comfortable hand pose", new Vector2(235, -319), new Vector2(940, 64), Gold);
                confirm = confirmRow.gameObject.AddComponent<Button>(); confirm.targetGraphic = confirmRow;
                // Explicit states: a tint cross-fade made READY look disabled as it unlocked.
                confirm.transition = Selectable.Transition.None;
                confirmLabel = Label(confirmRow.transform, "Label", "START  ·  LOOK HERE + PINCH", Vector2.zero, new Vector2(900, 58), 30, ButtonInk);
                confirmLabel.alignment = TextAnchor.MiddleCenter;
                SetConfirm(false);
                // START only advances the flow; the pose itself is captured hands-free later.
                confirm.onClick.AddListener(() => { Flow.PressStart(); handPointer?.RequireRelease(); });
                handPointer = root.AddComponent<HandGazePointer>(); handPointer.Configure(canvas, camera);
            }
            WorldCardRendering.DrawOverWorld(canvas);
            canvas.enabled = false;
        }

        public void Show(bool isRedo)
        {
            if (canvas == null) return;
            redo = isRedo;
            title.text = isRedo ? "RECALIBRATE YOUR FLIGHT" : "MAKE THE FLIGHT FIT YOU";
            string closing = isRedo ? "Your current flight and progress are kept."
                : HandInputSettings.UseHands ? "Flap down to fly  ·  sweep both hands forward to slow"
                : "You can return here at any time from the pause menu.";
            explanation.text = (HandInputSettings.UseHands
                ? "1   Sit or stand comfortably, shoulders relaxed\n\n2   Open your arms, hands just ahead of your shoulders\n\n3   Look comfortably a little below the horizon\n\n4   Look at READY and pinch thumb + index"
                : "1   Face a comfortable direction\n\n2   Hold a relaxed T pose with both controllers\n\n3   Look comfortably a little below the horizon\n\n4   Press A when the pose is ready") + "\n\n" + closing;
            canvas.enabled = true;
            if (HandInputSettings.UseHands)
            {
                // Start can run before FlightCamera's first pose. Follow for this
                // frame, then anchor after that camera's LateUpdate has settled.
                canvas.transform.SetParent(eye.transform, false);
                canvas.transform.localPosition = HandCardOffset;
                canvas.transform.localRotation = Quaternion.LookRotation(HandCardOffset);
                canvas.transform.localScale = Vector3.one * .00105f;
                anchorAfterCameraPose = true;
                readyUntil = 0f; SetConfirm(false);
                handPointer?.RequireRelease();
                Flow.Begin(!isRedo);
                RefreshHands(default);
            }
        }

        // Hands flow step. `frame` is the filtered pose used for flight, `device` the native
        // pose with its source. Returns true (and raises ConfirmRequested) when the countdown ends.
        public bool UpdateHands(FlightInputFrame frame, FlightInputFrame device, float dt)
        {
            if (!Visible || !HandInputSettings.UseHands) return false;
            var before = Flow.Step;
            bool done = Flow.Sample(frame, device, dt);
            // START is the only gaze target: hold the card still for it, follow the head otherwise.
            if (Flow.Step != before && Flow.Step == HandCalibrationStep.Start) { anchorAfterCameraPose = true; handPointer?.RequireRelease(); }
            RefreshHands(frame);
            if (done) ConfirmRequested?.Invoke();
            return done;
        }

        private void RefreshHands(FlightInputFrame frame)
        {
            var step = Flow.Step;
            headset.color = Flow.HeadReady ? Ready : Missing;
            leftController.color = Element(frame.LeftWing, Flow.LeftReady);
            rightController.color = Element(frame.RightWing, Flow.RightReady);
            bool start = step == HandCalibrationStep.Start;
            confirmRow.gameObject.SetActive(start); SetConfirm(start);
            string heading = redo ? "RECALIBRATE" : "MAKE THE FLIGHT FIT YOU";
            switch (step)
            {
                case HandCalibrationStep.Pose:
                    title.text = heading;
                    explanation.text = "1   Hold your natural gliding pose, arms open and relaxed\n\n2   Hands a little below your shoulders, wherever feels natural\n\n3   Look ahead and slightly down, as if driving\n\nHold still: the next step starts by itself.";
                    break;
                case HandCalibrationStep.LookLeft:
                    title.text = "◄◄   LOOK LEFT";
                    explanation.text = "Keep your arms still and slowly turn your head left.\n\nThis teaches the game where your hands are when the cameras lose sight of them.";
                    break;
                case HandCalibrationStep.LookRight:
                    title.text = "LOOK RIGHT   ►►";
                    explanation.text = "Keep your arms still and slowly turn your head right.\n\nThis teaches the game where your hands are when the cameras lose sight of them.";
                    break;
                case HandCalibrationStep.Start:
                    title.text = "READY TO FLY";
                    explanation.text = "You can move your arms now.\n\nLook at START and pinch thumb + index.\n\nThen go back into your gliding pose; flight begins after a 3 · 2 · 1 countdown.";
                    break;
                case HandCalibrationStep.Return:
                    title.text = "BACK INTO YOUR GLIDE POSE";
                    explanation.text = "Open your arms like before and hold still.\n\nThe countdown starts when head and both hands are green.\n\nFlap down to fly  ·  sweep both hands forward to slow";
                    break;
                case HandCalibrationStep.Countdown:
                    title.text = Mathf.CeilToInt(Flow.CountdownRemaining).ToString();
                    explanation.text = "HOLD YOUR GLIDE POSE…\n\nThis becomes your neutral: you start gliding in it.";
                    break;
            }
            bool tracking = Flow.HeadReady && Flow.LeftReady && Flow.RightReady;
            readiness.color = Flow.PoseReady ? Ready : Muted;
            readiness.text = start ? "ARMS FREE  •  LOOK AT START + PINCH"
                : !tracking ? "TRACKING: " + (Flow.HeadReady ? "HEAD ✓" : "HEAD …") + "   " + (Flow.LeftReady ? "LEFT ✓" : "LEFT …") + "   " + (Flow.RightReady ? "RIGHT ✓" : "RIGHT …")
                : step == HandCalibrationStep.LookLeft || step == HandCalibrationStep.LookRight ? "KEEP YOUR ARMS STILL WHILE YOU LOOK"
                : !Flow.PoseSane ? "OPEN YOUR ARMS TO YOUR SIDES, LEVEL AND RELAXED"
                : !Flow.Steady ? "HOLD STILL" : "HOLDING…";
        }

        // Green when camera-tracked, gold when only inferred, red when missing.
        private static Color Element(WingInput wing, bool ready) => !ready ? Missing
            : wing.Source == HandPoseSource.Inferred ? Gold : Ready;

        // Re-place a visible hands card after the viewpoint moved, e.g. a perch recovery.
        public void Reanchor() { if (Visible && HandInputSettings.UseHands) anchorAfterCameraPose = true; }

        private void SetConfirm(bool ready)
        {
            if (confirm == null) return;
            confirm.interactable = ready;
            confirmRow.color = ready ? Gold : Waiting;
            confirmLabel.color = ready ? ButtonInk : Muted;
            confirmLabel.text = ready ? "READY  ·  LOOK HERE + PINCH" : "HOLD THE POSE TO UNLOCK READY";
        }

        private void LateUpdate()
        {
            // Hands cards follow the head except while START is a gaze target.
            bool follow = HandInputSettings.UseHands && Visible && Flow.Step != HandCalibrationStep.Start
                && Flow.Step != HandCalibrationStep.Done;
            if (!(anchorAfterCameraPose || follow) || !Visible) return;
            // The panel stays still afterward so head gaze can reach READY.
            // Assign the world pose after reparenting: preserving a world-space
            // Canvas transform can carry its UI scale into the parent offset.
            canvas.transform.SetParent(null, false);
            canvas.transform.SetPositionAndRotation(eye.transform.TransformPoint(HandCardOffset),
                eye.transform.rotation * Quaternion.LookRotation(HandCardOffset));
            canvas.transform.localScale = Vector3.one * .00105f;
            anchorAfterCameraPose = false;
        }

        public void Hide()
        {
            if (canvas != null) canvas.enabled = false;
            if (Flow.Step != HandCalibrationStep.Done) Flow.Cancel();
            anchorAfterCameraPose = false;
            handPointer?.RequireRelease();
        }

        public void UpdateReadiness(FlightInputFrame frame, BirdTrackingCalibration calibration)
        {
            if (!Visible || readiness == null) return;
            bool head = frame.HeadTracked;
            bool left = frame.LeftWing.Tracked && (!HandInputSettings.UseHands || !frame.LeftWing.MotionEstimated);
            bool right = frame.RightWing.Tracked && (!HandInputSettings.UseHands || !frame.RightWing.MotionEstimated);
            bool pose = head && left && right && calibration != null && calibration.IsComfortableGlidePose(frame);
            bool hands = HandInputSettings.UseHands && calibration != null;
            headset.color = head ? Ready : Missing;
            // Each hand shows its own reach check, so a tracked but misplaced hand is amber.
            leftController.color = !left ? Missing : !hands || calibration.HandReachOk(frame, true) ? Ready : Gold;
            rightController.color = !right ? Missing : !hands || calibration.HandReachOk(frame, false) ? Ready : Gold;
            readiness.color = pose ? Ready : Muted;
            // A brief hold stops READY flickering off for a single noisy tracking frame.
            if (pose) readyUntil = Time.unscaledTime + ReadyHoldSeconds;
            SetConfirm(pose || Time.unscaledTime < readyUntil);
            readiness.text = pose
                ? HandInputSettings.UseHands ? "POSE READY  •  LOOK AT READY + PINCH" : "POSE READY  •  PRESS A"
                : !head || !left || !right
                    ? "TRACKING: " + (head ? "HEAD ✓" : "HEAD …") + "   " + (left ? "LEFT ✓" : "LEFT …") + "   " + (right ? "RIGHT ✓" : "RIGHT …")
                    : HandInputSettings.UseHands ? (hands ? calibration.PoseHint(frame) : null) ?? "OPEN YOUR ARMS, HANDS JUST IN FRONT OF YOUR SHOULDERS"
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
