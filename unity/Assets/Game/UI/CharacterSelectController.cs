using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VoarVR.Flight;
using VoarVR.Gameplay;

namespace VoarVR.UI
{
    // Species are discovered from the existing Resources catalog. Selecting a species now
    // previews it; the explicit journey button starts flight after the task/control briefing.
    public sealed class CharacterSelectController : MonoBehaviour
    {
        [SerializeField] private string flightSceneName = "BirdFlight";
        [SerializeField] private GameObject xrRigPrefab;
        private static readonly Color Background = new Color(.025f, .068f, .075f);
        private static readonly Color Panel = new Color(.065f, .13f, .14f);
        private static readonly Color Ink = new Color(.91f, .97f, .93f);
        private static readonly Color Muted = new Color(.66f, .78f, .75f);
        private static readonly Color Gold = new Color(.98f, .78f, .42f);
        private static readonly FlightActivity[] Activities =
        {
            FlightActivity.RouteHome, FlightActivity.Training, FlightActivity.FreeFlight,
            FlightActivity.SkywardExpedition, FlightActivity.RidgeJourney
        };
        private static readonly string[] ActivityLabels = { "A ROUTE HOME", "LEARN THE AIR", "FREE FLIGHT", "SKYWARD", "RIDGE JOURNEY" };
        private static readonly (string Label, Func<BirdCharacterDefinition, float> Read)[] Stats =
        {
            ("SIZE", d => Mathf.InverseLerp(.3f, 3f, d.Size)), ("SPEED", d => d.Speed),
            ("POWER", d => d.Power), ("AGILITY", d => d.Agility), ("WEIGHT", d => d.Weight)
        };
        private readonly List<GameObject> ownedObjects = new List<GameObject>();
        private readonly List<Button> activityButtons = new List<Button>();
        private readonly List<Button> speciesButtons = new List<Button>();
        private readonly List<Image> statFills = new List<Image>();
        private BirdCharacterDefinition[] characters;
        private BirdCharacterDefinition selectedCharacter;
        private Text routeTitle, routeDescription, routeSteps, saveStatus, speciesDescription;
        private Text beginLabel, newLabel, comfortLabel, calibrationBriefing;
        private Button beginButton, newButton;
        private Canvas canvas;
        public Canvas PreflightPanel => canvas;
        public BirdCharacterDefinition SelectedCharacter => selectedCharacter;
        public string PreviewText => routeDescription != null ? routeDescription.text : string.Empty;

        private void Start()
        {
            if (xrRigPrefab != null)
            {
                var rig = Own(Instantiate(xrRigPrefab));
                var locomotion = rig.transform.Find("Locomotion");
                if (locomotion != null) locomotion.gameObject.SetActive(false);
            }
            if (FindAnyObjectByType<XRInteractionManager>() == null)
                Own(new GameObject("XR Interaction Manager", typeof(XRInteractionManager)));
            if (FindAnyObjectByType<EventSystem>() == null)
                Own(new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule)));
            characters = Resources.LoadAll<BirdCharacterDefinition>(CharacterSelection.ResourcesFolder)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.DisplayName).ToArray();
            if (characters.Length == 0)
                throw new InvalidOperationException("No BirdCharacterDefinition assets under Resources/Characters. Run VoarVR/Configure Characters.");
            BuildUI();
            ChooseCharacter(CharacterSelection.Chosen != null && characters.Contains(CharacterSelection.Chosen)
                ? CharacterSelection.Chosen : characters[0]);
            ChooseActivity(ActivitySelection.Chosen);
        }

        private GameObject Own(GameObject obj) { ownedObjects.Add(obj); return obj; }

        private void BuildUI()
        {
            var root = Own(new GameObject("Journey preflight", typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster)));
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            var rect = root.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1920, 1320); rect.localScale = Vector3.one * .00115f;
            var eye = Camera.main; canvas.worldCamera = eye;
            if (eye != null)
            {
                eye.clearFlags = CameraClearFlags.SolidColor; eye.backgroundColor = Background;
                var forward = Vector3.ProjectOnPlane(eye.transform.forward, Vector3.up);
                if (forward.sqrMagnitude < .001f) forward = Vector3.forward; else forward.Normalize();
                rect.position = eye.transform.position + forward * 2.5f;
                rect.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            Box(root.transform, "Backdrop", Vector2.zero, rect.sizeDelta, Background);
            Box(root.transform, "Accent", new Vector2(0, 651), new Vector2(1920, 5), Gold);
            Label(root.transform, "Eyebrow", "V O A R   /   SKY SANCTUARY", new Vector2(0, 577), new Vector2(1740, 55), 30, Gold);
            Label(root.transform, "Title", "Every flight can bring something home.", new Vector2(0, 501), new Vector2(1740, 82), 48, Ink);
            for (int i = 0; i < Activities.Length; i++)
            {
                var activity = Activities[i];
                var button = Button(root.transform, ActivityLabels[i], new Vector2((i - 2) * 356, 397), new Vector2(340, 72), 27);
                button.onClick.AddListener(() => ChooseActivity(activity)); activityButtons.Add(button);
            }
            var route = Box(root.transform, "Journey preview", new Vector2(-350, 28), new Vector2(1090, 620), Panel);
            routeTitle = Label(route.transform, "Route title", "", new Vector2(0, 241), new Vector2(982, 70), 43, Ink);
            routeDescription = Label(route.transform, "Route description", "", new Vector2(0, 134), new Vector2(982, 128), 30, Muted);
            Label(route.transform, "Route heading", "THE FLIGHT AHEAD", new Vector2(0, 20), new Vector2(982, 45), 26, Gold);
            routeSteps = Label(route.transform, "Route steps", "", new Vector2(0, -78), new Vector2(982, 150), 31, Ink);
            saveStatus = Label(route.transform, "Save status", "", new Vector2(0, -233), new Vector2(982, 113), 25, Muted);

            var species = Box(root.transform, "Flyer preview", new Vector2(566, 28), new Vector2(668, 620), Panel);
            Label(species.transform, "Flyer heading", "CHOOSE YOUR FLYER", new Vector2(0, 266), new Vector2(576, 53), 28, Gold);
            // The catalog remains data-driven: additional species share this bounded area.
            float rowHeight = Mathf.Min(76, 235f / characters.Length);
            for (int i = 0; i < characters.Length; i++)
            {
                var character = characters[i];
                var button = Button(species.transform, character.DisplayName.ToUpperInvariant(), new Vector2(0, 198 - i * rowHeight), new Vector2(576, rowHeight - 8), 28);
                button.onClick.AddListener(() => ChooseCharacter(character)); speciesButtons.Add(button);
            }
            speciesDescription = Label(species.transform, "Species description", "", new Vector2(0, -43), new Vector2(576, 118), 26, Muted);
            for (int i = 0; i < Stats.Length; i++)
            {
                float y = -139 - i * 31;
                Label(species.transform, Stats[i].Label, Stats[i].Label, new Vector2(-199, y), new Vector2(180, 30), 22, Ink);
                var track = Box(species.transform, Stats[i].Label + " track", new Vector2(90, y), new Vector2(386, 10), new Color(.2f, .3f, .29f));
                var fill = Box(track.transform, "Fill", Vector2.zero, Vector2.zero, new Color(.47f, .78f, .64f));
                fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero; statFills.Add(fill);
            }
            beginButton = Button(root.transform, "BEGIN JOURNEY", new Vector2(-542, -360), new Vector2(705, 94), 33);
            beginLabel = beginButton.GetComponentInChildren<Text>(); beginButton.onClick.AddListener(() => Launch(true));
            newButton = Button(root.transform, "NEW JOURNEY", new Vector2(16, -360), new Vector2(376, 94), 27);
            newLabel = newButton.GetComponentInChildren<Text>(); newButton.onClick.AddListener(() => Launch(false));
            var comfort = Button(root.transform, "", new Vector2(566, -360), new Vector2(668, 94), 27);
            comfortLabel = comfort.GetComponentInChildren<Text>();
            comfort.onClick.AddListener(() => { FlightPreferences.FirstPersonStabilized = !FlightPreferences.FirstPersonStabilized; RefreshComfort(); });
            RefreshComfort();
            calibrationBriefing = Label(root.transform, "Calibration briefing", "Begin in third-person. Spread your arms comfortably, then press A to calibrate.",
                new Vector2(0, -468), new Vector2(1770, 62), 29, Ink);
            Label(root.transform, "Control briefing", "X  Pause  >  RIGHT TRIGGER  Help + recovery     B  View     RIGHT STICK CLICK  Instruments\nHOLD LEFT STICK CLICK  Flight mode     LEFT MENU  Save + leave\nInstruments start hidden. Pause and open help whenever you need your objective or a rest.",
                new Vector2(0, -564), new Vector2(1770, 120), 26, Muted);
        }

        public void ChooseCharacter(BirdCharacterDefinition character)
        {
            int index = Array.IndexOf(characters, character); if (index < 0) return;
            selectedCharacter = character;
            for (int i = 0; i < speciesButtons.Count; i++) SetSelected(speciesButtons[i], i == index);
            speciesDescription.text = character.FlavorText;
            for (int i = 0; i < Stats.Length; i++) statFills[i].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(Stats[i].Read(character)), 1);
        }

        public void ChooseActivity(FlightActivity activity)
        {
            var legacy = ExpeditionDirector.LoadProgress();
            var journey = ExpeditionDirector.LoadJourney();
            if (!Activities.Contains(activity) || (activity == FlightActivity.RidgeJourney && !legacy.RidgeUnlocked)) activity = FlightActivity.RouteHome;
            ActivitySelection.Chosen = activity;
            for (int i = 0; i < activityButtons.Count; i++)
            {
                activityButtons[i].interactable = Activities[i] != FlightActivity.RidgeJourney || legacy.RidgeUnlocked;
                activityButtons[i].GetComponentInChildren<Text>().text = ActivityLabels[i] + (Activities[i] == FlightActivity.RidgeJourney && !legacy.RidgeUnlocked ? " / LOCKED" : "");
                SetSelected(activityButtons[i], Activities[i] == activity);
            }
            switch (activity)
            {
                case FlightActivity.RouteHome:
                    routeTitle.text = "A ROUTE HOME";
                    routeDescription.text = "Carry a living seed to a quiet garden above the valley.\nUntimed adventure / flap, glide and rest at your pace.";
                    routeSteps.text = "01   Rise in the gold air / collect the living seed\n02   Cross the broad stone arch toward the garden\n03   Land and bring the sanctuary to life";
                    break;
                case FlightActivity.Training:
                    routeTitle.text = "LEARNING THE AIR";
                    routeDescription.text = "Learn to find lift and turn height into possibility.\nEfficiency trial / medals reward quiet soaring and time.";
                    routeSteps.text = "01   Find the gold streams above the valley\n02   Circle upward with relaxed, open wings\n03   Gain 30 m quietly and reach 50 m altitude";
                    break;
                case FlightActivity.FreeFlight:
                    routeTitle.text = "THE SKY IS YOURS";
                    routeDescription.text = "Choose a horizon and see where the air takes you.\nNo required tasks / no timer / no completion target.";
                    routeSteps.text = "Follow rising gold streams, or explore the lowlands.\nCatch moths, find a perch, or practise a gentle turn.\nOptional rolls and loops await in advanced flight.";
                    break;
                default:
                    routeTitle.text = activity == FlightActivity.RidgeJourney ? "RIDGE JOURNEY" : "SKYWARD EXPEDITION";
                    routeDescription.text = "A longer route through lift, open sky and stone arches.\nUntimed adventure / quiet soaring is part of the route.";
                    routeSteps.text = activity == FlightActivity.RidgeJourney
                        ? "01   Find lift and build height\n02   Follow the ridge and cross its stone arch\n03   Set down on the distant garden terrace"
                        : "01   Find lift and gain 100 m with quiet wings\n02   Reach 230 m and fly through the stone arch\n03   Land on the garden terrace to finish";
                    break;
            }
            bool resume = journey.HasResume(activity);
            if (calibrationBriefing != null) calibrationBriefing.text = resume
                ? "Resume stays paused. Right trigger: flight menu → Recalibrate here. A restarts the route."
                : "Begin in third-person. Spread your arms comfortably, then press A to calibrate.";
            var checkpoint = journey.GetCheckpoint(activity);
            string progress = resume && checkpoint != null ? "Checkpoint saved / step " + (checkpoint.Stage + 1) + ". Resume from a safe perch."
                : activity == FlightActivity.RouteHome && journey.GardenRestored ? "The garden remembers your gift. Fly the journey again whenever you like."
                : activity == FlightActivity.Training && journey.TrainingEfficiencyBest > 0 ? "Your efficiency best: " + journey.TrainingEfficiencyBest
                : activity == FlightActivity.FreeFlight ? "Free flight has no route checkpoint. Your banked journey rewards remain."
                : "Progress saves at completed steps. Stopping does not cost an earned reward.";
            if (!string.IsNullOrEmpty(journey.SaveNotice)) progress = journey.SaveNotice;
            else if (resume) progress += " Restart begins this route again; banked restoration stays.";
            saveStatus.text = progress;
            beginLabel.text = resume ? "RESUME FLIGHT" : activity == FlightActivity.FreeFlight ? "GO EXPLORING" : "BEGIN FLIGHT";
            newButton.gameObject.SetActive(resume);
            newLabel.text = "RESTART ROUTE";
            SetSelected(beginButton, true);
        }

        private void RefreshComfort() => comfortLabel.text = "FIRST-PERSON VIEW\n" + (FlightPreferences.FirstPersonStabilized ? "Steady horizon" : "Embodied horizon");
        private void Launch(bool resume)
        {
            if (selectedCharacter == null) return;
            CharacterSelection.Chosen = selectedCharacter;
            ActivitySelection.ResumeRequested = resume && ExpeditionDirector.LoadJourney().HasResume(ActivitySelection.Chosen);
            SceneManager.LoadScene(flightSceneName);
        }
        private static void SetSelected(Button button, bool selected)
        {
            button.GetComponent<Image>().color = selected ? Gold : new Color(.12f, .23f, .22f);
            button.GetComponentInChildren<Text>().color = selected ? new Color(.06f, .13f, .11f) : Ink;
        }
        private static Image Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        private static Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchoredPosition = position; rect.sizeDelta = size;
            var text = go.GetComponent<Text>(); text.text = value; text.fontSize = fontSize; text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.color = color; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        private static Button Button(Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
        {
            var image = Box(parent, value + " button", position, size, new Color(.12f, .23f, .22f)); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = Label(image.transform, "Label", value, Vector2.zero, size - new Vector2(30, 6), fontSize, Ink); label.alignment = TextAnchor.MiddleCenter;
            var colors = button.colors; colors.highlightedColor = new Color(1f, 1f, .85f); colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(.55f, .55f, .55f); button.colors = colors; return button;
        }
        private void OnDestroy() { foreach (var obj in ownedObjects) if (obj != null) Destroy(obj); }
    }
}
