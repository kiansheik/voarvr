using System;
using UnityEngine;
using UnityEngine.UI;
using VoarVR.Input;

namespace VoarVR.UI
{
    public enum FlightMenuAction
    {
        Resume, RecalibrateHere, ReturnSafePerch, RestartRoute, FinishSession,
        ToggleFirstPersonComfort, ToggleGuidance, ToggleAudio, ToggleHaptics, ReturnAfterResults
    }

    public readonly struct FlightMenuInputChange
    {
        public readonly bool Toggle, Select;
        public readonly int Move;
        public FlightMenuInputChange(bool toggle, bool select, int move)
        { Toggle = toggle; Select = select; Move = move; }
    }

    // A held flight trigger cannot open help on pause or activate its first row. Both
    // trigger and stick require a release; opening is separate from selecting an action.
    public sealed class FlightMenuInputRouter
    {
        private bool wasPaused, triggerHeld, waitingForRelease;
        private int stickHeld;
        public void RequireRelease() => waitingForRelease = true;
        public FlightMenuInputChange Sample(FlightInputFrame frame, bool paused, bool visible,
            bool desktopToggle = false, int desktopNavigation = 0, bool desktopSelect = false)
        {
            bool trigger = triggerHeld ? frame.Tuck > .25f : frame.Tuck >= .65f;
            int stick = frame.GroundMove.y > .55f ? -1 : frame.GroundMove.y < -.55f ? 1 : 0;
            bool triggerEdge = trigger && !triggerHeld;
            bool justPaused = paused && !wasPaused;
            int move = stick != 0 && stickHeld == 0 ? stick : 0;
            triggerHeld = trigger;
            if (Mathf.Abs(frame.GroundMove.y) < .25f) stickHeld = 0;
            else if (stick != 0) stickHeld = stick;
            wasPaused = paused;
            if (waitingForRelease)
            {
                if (frame.Tuck <= .25f && frame.GroundMove.magnitude <= .25f) waitingForRelease = false;
                return default;
            }
            if (!paused) return default;
            if (justPaused) { triggerEdge = false; move = 0; }
            bool toggle = desktopToggle || (!visible && triggerEdge);
            if (toggle) return new FlightMenuInputChange(true, false, 0);
            if (!visible) return default;
            return new FlightMenuInputChange(false, triggerEdge || desktopSelect,
                desktopNavigation != 0 ? Math.Sign(desktopNavigation) : move);
        }
    }

    // Explicitly opened at rest; no ray prefab or automatic pause/landing overlay. The
    // Canvas is positioned once in front of the viewer and remains still while reading.
    public sealed class FlightMenu : MonoBehaviour
    {
        public const string ResultColumnSeparator="\n<RESULT_COLUMN>\n";
        private static readonly Color Ink = new Color(.9f, .97f, .94f);
        private static readonly Color Muted = new Color(.64f, .76f, .73f);
        private static readonly Color Gold = new Color(.97f, .79f, .43f);
        private static readonly FlightMenuAction[] Actions =
        {
            FlightMenuAction.Resume, FlightMenuAction.RecalibrateHere, FlightMenuAction.ReturnSafePerch,
            FlightMenuAction.RestartRoute, FlightMenuAction.FinishSession, FlightMenuAction.ToggleFirstPersonComfort,
            FlightMenuAction.ToggleGuidance, FlightMenuAction.ToggleAudio, FlightMenuAction.ToggleHaptics
        };
        private readonly FlightMenuInputRouter input = new FlightMenuInputRouter();
        private Canvas canvas;
        private Camera eye;
        private Action<FlightMenuAction> onAction;
        private Func<string> statusProvider;
        private Text title, subtitle, status, navigation, resultText,resultDetails, comfortNote;
        private Image resultPanel;
        private bool resultsMode;
        private bool courseContext;
        private readonly Text[] labels = new Text[Actions.Length];
        private readonly Image[] rows = new Image[Actions.Length];
        private int selected;
        public bool Visible => canvas != null && canvas.enabled;
        public Canvas Panel => canvas;
        public int SelectedIndex => selected;
        public string StatusText => status != null ? status.text : string.Empty;
        public bool ResultsVisible => Visible && resultsMode;

        public void Configure(Camera camera, Action<FlightMenuAction> action, Func<string> getStatus)
        {
            eye = camera; onAction = action; statusProvider = getStatus;
            if (canvas != null || eye == null) return;
            var root = new GameObject("Flight pause help", typeof(RectTransform), typeof(Canvas));
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = eye; canvas.enabled = false;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1420, 1010); rect.localScale = Vector3.one * .0012f;
            var background = Box(root.transform, "Backdrop", Vector2.zero, rect.sizeDelta, new Color(.025f, .075f, .076f, .98f));
            Box(background.transform, "Top accent", new Vector2(0, 497), new Vector2(1420, 6), Gold);
            title = Label(root.transform, "Title", "A MOMENT TO REST", new Vector2(-35, 422), new Vector2(1220, 75), 47, Ink);
            subtitle = Label(root.transform, "Subtitle", "Your journey can wait. Take the time you need.", new Vector2(-35, 354), new Vector2(1220, 55), 29, Muted);
            Box(root.transform, "Divider", new Vector2(-25, -25), new Vector2(2, 660), new Color(.25f, .4f, .37f));
            Label(root.transform, "Objective heading", "YOUR JOURNEY", new Vector2(-367, 253), new Vector2(570, 48), 27, Gold);
            status = Label(root.transform, "Objective", "", new Vector2(-367, 116), new Vector2(570, 205), 30, Ink);
            Label(root.transform, "Controls heading", "CONTROLS", new Vector2(-367, -49), new Vector2(570, 46), 27, Gold);
            Label(root.transform, "Controls", "X   Pause / resume\nA   Open calibration coach\nB   First / third-person view\nLeft stick   Walk · right stick   Turn on ground\nRight stick click   Instruments\nLeft Menu   Open this session menu",
                new Vector2(-367, -197), new Vector2(570, 242), 29, Ink);
            comfortNote = Label(root.transform, "Comfort note", "View comfort changes the horizon only.\nFlight authority stays in your chosen mode.",
                new Vector2(-367, -370), new Vector2(570, 90), 27, Muted);
            for (int i = 0; i < Actions.Length; i++)
            {
                float y = 265 - i * 76;
                rows[i] = Box(root.transform, Actions[i].ToString(), new Vector2(345, y), new Vector2(616, 64), Color.clear);
                labels[i] = Label(rows[i].transform, "Label", "", Vector2.zero, new Vector2(572, 58), 28, Ink);
            }
            resultPanel = Box(root.transform, "Session result backdrop", new Vector2(0, -28), new Vector2(1320, 700), new Color(.025f, .075f, .076f, 1f));
            resultText = Label(resultPanel.transform, "Primary session results", "", new Vector2(-318,0), new Vector2(570, 630), 34, Ink, TextAnchor.MiddleLeft);
            resultDetails = Label(resultPanel.transform, "Detailed session results", "", new Vector2(318,0), new Vector2(570, 630), 32, Ink, TextAnchor.MiddleLeft);
            resultPanel.gameObject.SetActive(false);
            navigation = Label(root.transform, "Navigation", "LEFT STICK  Choose     RIGHT TRIGGER  Confirm     X  Resume\nDesktop: arrows / Enter · F1 closes help",
                new Vector2(0, -455), new Vector2(1330, 74), 27, Muted, TextAnchor.MiddleCenter);
            RefreshRows();
        }

        public void SetCourseContext(bool value)
        {
            courseContext = value;
            RefreshRows();
        }

        public bool HandleInput(FlightInputFrame rawFrame, bool paused, bool desktopToggle = false,
            int desktopNavigation = 0, bool desktopSelect = false)
        {
            var change = input.Sample(rawFrame, paused, Visible, desktopToggle, desktopNavigation, desktopSelect);
            if (!paused) { Close(); return false; }
            if (resultsMode)
            {
                if (change.Select || desktopSelect) onAction?.Invoke(FlightMenuAction.ReturnAfterResults);
                return true;
            }
            if (change.Toggle)
            {
                if (Visible) Close(); else Open();
                return true;
            }
            if (!Visible) return false;
            if (change.Move != 0) { selected = (selected + change.Move + Actions.Length) % Actions.Length; RefreshRows(); }
            if (change.Select) ActivateSelected();
            return true;
        }

        public void Open()
        {
            if (canvas == null || eye == null) return;
            canvas.transform.SetPositionAndRotation(eye.transform.position + eye.transform.forward * 2.2f, eye.transform.rotation);
            resultsMode = false;
            resultPanel.gameObject.SetActive(false);
            title.text = "A MOMENT TO REST";
            comfortNote.gameObject.SetActive(true);
            subtitle.text = "Your journey can wait. Take the time you need.";
            navigation.text = "LEFT STICK  Choose     RIGHT TRIGGER  Confirm     X  Resume\nDesktop: arrows / Enter · F1 closes help";
            selected = 0; status.text = statusProvider?.Invoke() ?? "Explore at your pace. Your instruments are optional.";
            RefreshRows(); canvas.enabled = true;
        }
        public void ShowResults(string summary)
        {
            if (canvas == null || eye == null) return;
            canvas.transform.SetPositionAndRotation(eye.transform.position + eye.transform.forward * 2.2f, eye.transform.rotation);
            resultsMode = true;
            comfortNote.gameObject.SetActive(false);
            title.text = "FLIGHT COMPLETE";
            subtitle.text = "Your movement, rest and discoveries are saved to this player.";
            var columns=(summary??string.Empty).Split(new[]{ResultColumnSeparator},
                StringSplitOptions.None);
            resultText.text=columns.Length>0?columns[0]:string.Empty;
            resultDetails.text=columns.Length>1?columns[1]:string.Empty;
            resultPanel.gameObject.SetActive(true);
            navigation.text = "RIGHT TRIGGER  Return to player select     •     Take all the time you need";
            input.RequireRelease();
            canvas.enabled = true;
        }
        public void Close() { if (canvas != null) canvas.enabled = false; }
        public void RequireInputRelease() => input.RequireRelease();
        public void ShowMessage(string message) { if (status != null) status.text = message ?? string.Empty; }

        private void ActivateSelected()
        {
            var action = Actions[selected];
            switch (action)
            {
                case FlightMenuAction.ToggleFirstPersonComfort: FlightPreferences.FirstPersonStabilized = !FlightPreferences.FirstPersonStabilized; break;
                case FlightMenuAction.ToggleGuidance: FlightPreferences.GuidanceEnabled = !FlightPreferences.GuidanceEnabled; break;
                case FlightMenuAction.ToggleAudio: FlightPreferences.AudioEnabled = !FlightPreferences.AudioEnabled; break;
                case FlightMenuAction.ToggleHaptics: FlightPreferences.HapticsEnabled = !FlightPreferences.HapticsEnabled; break;
            }
            onAction?.Invoke(action);
            RefreshRows();
        }
        private void RefreshRows()
        {
            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null) continue;
                labels[i].text = (i == selected ? ">  " : "    ") + ActionLabel(Actions[i]);
                labels[i].color = i == selected ? new Color(.04f, .12f, .105f) : Ink;
                rows[i].color = i == selected ? Gold : new Color(.09f, .18f, .17f);
            }
        }
        private string ActionLabel(FlightMenuAction action)
        {
            switch (action)
            {
                case FlightMenuAction.Resume: return courseContext ? "Return to course" : "Continue flying";
                case FlightMenuAction.RecalibrateHere: return "Recalibrate here";
                case FlightMenuAction.ReturnSafePerch: return courseContext ? "Return to course start" : "Return to safe perch";
                case FlightMenuAction.RestartRoute: return courseContext ? "End attempt; rest, then retry" : "Restart this route";
                case FlightMenuAction.FinishSession: return "Finish session + see results";
                case FlightMenuAction.ToggleFirstPersonComfort: return "First-person: " + (FlightPreferences.FirstPersonStabilized ? "steady horizon" : "embodied");
                case FlightMenuAction.ToggleGuidance: return "World guidance: " + (FlightPreferences.GuidanceEnabled ? "on" : "off");
                case FlightMenuAction.ToggleAudio: return "Sound: " + (FlightPreferences.AudioEnabled ? "on" : "off");
                default: return "Haptics: " + (FlightPreferences.HapticsEnabled ? "on" : "off");
            }
        }
        private static Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>(); r.anchoredPosition = position; r.sizeDelta = size;
            var t = go.GetComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = value; t.fontSize = fontSize; t.color = color; t.alignment = anchor; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; return t;
        }
        private void OnDisable() => Close();
        private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
    }
}
