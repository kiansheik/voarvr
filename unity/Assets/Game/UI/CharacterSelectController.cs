using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VoarVR.Flight;

namespace VoarVR.UI
{
    // Builds its own Canvas at runtime from every BirdCharacterDefinition under
    // Resources/Characters, so adding a new species to the select screen is a one-asset change
    // with no scene-authored UI to keep in sync. World-space + XR Interaction Toolkit rather
    // than screen-space overlay: overlay canvases do not render at all in Quest's stereo output.
    public sealed class CharacterSelectController : MonoBehaviour
    {
        [SerializeField] private string flightSceneName = "BirdFlight";
        // "XR Origin (XR Rig)" from the XRI Starter Assets sample - camera, both controllers
        // with Near-Far Interactors already wired to the default XRI input actions.
        [SerializeField] private GameObject xrRigPrefab;
        private static readonly (string Label, Func<BirdCharacterDefinition, float> Read)[] StatRows =
        {
            ("SIZE", d => Mathf.InverseLerp(0.3f, 3f, d.Size)),
            ("SPEED", d => d.Speed),
            ("POWER", d => d.Power),
            ("AGILITY", d => d.Agility),
            ("WEIGHT", d => d.Weight),
        };

        private void Start()
        {
            if (xrRigPrefab != null)
            {
                var rig = Instantiate(xrRigPrefab);
                // The Starter Assets rig's CharacterController-driven gravity/teleport/move
                // system expects a floor to stand on; this menu has none, so left alone the rig
                // free-falls out from under the camera every time. A static menu needs none of it.
                var locomotion = rig.transform.Find("Locomotion");
                if (locomotion != null) locomotion.gameObject.SetActive(false);
            }
            // Interactors register with this on enable; without one in the scene they just sit
            // on an internal waitlist and nothing responds to controller rays.
            if (FindAnyObjectByType<XRInteractionManager>() == null)
                new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
            var characters = Resources.LoadAll<BirdCharacterDefinition>(CharacterSelection.ResourcesFolder)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.DisplayName).ToArray();
            if (characters.Length == 0)
                throw new InvalidOperationException("No BirdCharacterDefinition assets under Resources/Characters. Run VoarVR/Configure Characters.");
            BuildUI(characters);
        }

        private void BuildUI(BirdCharacterDefinition[] characters)
        {
            if (FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);

            // Float the panel in front of wherever the XR camera starts, facing back at it -
            // there is no mouse pointer in VR, so screen-space anchoring has nothing to anchor to.
            var eye = Camera.main;
            if (eye != null)
            {
                eye.clearFlags = CameraClearFlags.SolidColor;
                eye.backgroundColor = new Color(.08f, .10f, .16f, 1f);
                var forward = eye.transform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward; else forward.Normalize();
                canvasRect.position = eye.transform.position + forward * 2.2f;
                canvasRect.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
            canvasRect.localScale = Vector3.one * 0.0012f;

            Stretch(CreateImage(canvasObject.transform, "Background", new Color(.08f, .10f, .16f, 1f)).rectTransform);

            var title = CreateText(canvasObject.transform, "Title", "CHOOSE YOUR FLYER", 56, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(0f, 140f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -20f);

            var rowObject = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            rowObject.transform.SetParent(canvasObject.transform, false);
            var rowRect = (RectTransform)rowObject.transform;
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(.5f, .5f);
            // childControlHeight clamps each card to this row's own height, which otherwise
            // stays at a RectTransform's raw default (100) since nothing else ever sets it.
            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, CardHeight);
            var rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 60f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowObject.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var character in characters) BuildCard(rowObject.transform, character);
            var activities=new[]{VoarVR.Gameplay.FlightActivity.FreeFlight,VoarVR.Gameplay.FlightActivity.Training,VoarVR.Gameplay.FlightActivity.SkywardExpedition,VoarVR.Gameplay.FlightActivity.RidgeJourney};
            var labels=new[]{"FREE FLIGHT","TRAINING","SKYWARD EXPEDITION","RIDGE JOURNEY"};
            var choices=new System.Collections.Generic.List<Button>();
            for(int i=0;i<activities.Length;i++)
            {
                var activity=activities[i];var progress=VoarVR.Gameplay.ExpeditionDirector.LoadProgress();
                int best=i==1?progress.TrainingBest:i==2?progress.SkywardBest:progress.RidgeBest;
                string label=labels[i]+(i==3 && !progress.RidgeUnlocked?" · LOCKED":i>0?" · BEST "+best:"");
                var button=CreateButton(canvasObject.transform,label);choices.Add(button);
                button.GetComponentInChildren<Text>().color=Color.white;button.GetComponent<Image>().color=new Color(.16f,.25f,.32f);
                var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(390,75);rect.anchoredPosition=new Vector2((i-1.5f)*420,18);
                button.interactable=activity!=VoarVR.Gameplay.FlightActivity.RidgeJourney || VoarVR.Gameplay.ExpeditionDirector.LoadProgress().RidgeUnlocked;
                button.onClick.AddListener(()=>{VoarVR.Gameplay.ActivitySelection.Chosen=activity;foreach(var b in choices)b.GetComponent<Image>().color=new Color(.16f,.25f,.32f);button.GetComponent<Image>().color=new Color(.16f,.48f,.42f);});
                if(VoarVR.Gameplay.ActivitySelection.Chosen==activity)button.GetComponent<Image>().color=new Color(.16f,.48f,.42f);
            }

        }

        // Vertical stacking here is done by hand with explicit anchored offsets rather than a
        // VerticalLayoutGroup: a LayoutGroup placed on the same GameObject as a LayoutElement
        // ends up self-reporting its own (wrong, content-derived) preferred height instead of
        // honoring the LayoutElement's value, and every child's height resolves to 0 as a
        // result. Horizontal-axis layout groups (see BuildStatBar) don't hit this - only the
        // vertical pass does - so this sidesteps it rather than chasing it further.
        private const float CardWidth = 420f, CardHeight = 720f, CardPadding = 28f;

        private void BuildCard(Transform parent, BirdCharacterDefinition character)
        {
            var card = new GameObject(character.DisplayName + "Card", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = new Color(1f, 1f, 1f, .07f);
            var element = card.GetComponent<LayoutElement>();
            element.preferredWidth = CardWidth;
            element.preferredHeight = CardHeight;

            float y = -CardPadding;
            const float spacing = 14f;

            PlaceRow(CreateText(card.transform, "Name", character.DisplayName.ToUpperInvariant(), 40, TextAnchor.MiddleCenter).rectTransform, y, 56f);
            y -= 56f + spacing;

            var flavor = CreateText(card.transform, "Flavor", character.FlavorText, 22, TextAnchor.UpperCenter);
            flavor.color = new Color(1f, 1f, 1f, .75f);
            PlaceRow(flavor.rectTransform, y, 90f);
            y -= 90f + spacing;

            foreach (var stat in StatRows)
            {
                BuildStatBar(card.transform, stat.Label, stat.Read(character), y);
                y -= 26f + spacing;
            }

            var button = CreateButton(card.transform, "SELECT " + character.DisplayName.ToUpperInvariant());
            PlaceRow(button.GetComponent<RectTransform>(), -(CardHeight - CardPadding - 84f), 84f);
            button.onClick.AddListener(() => Select(character));
        }

        // Stretches full card width (minus CardPadding on each side), fixed height, anchored to
        // the top-left corner with y measured downward from there - independent of any layout
        // group pass.
        private static void PlaceRow(RectTransform rect, float y, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-2f * CardPadding, height);
        }

        private void BuildStatBar(Transform parent, string label, float value, float y)
        {
            var row = new GameObject(label + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            PlaceRow((RectTransform)row.transform, y, 26f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var labelElement = CreateText(row.transform, "Label", label, 18, TextAnchor.MiddleLeft).gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 100f;

            var track = CreateImage(row.transform, "Track", new Color(1f, 1f, 1f, .15f));
            track.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var fill = CreateImage(track.transform, "Fill", new Color(.35f, .85f, .65f, 1f));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
        }

        private void Select(BirdCharacterDefinition character)
        {
            CharacterSelection.Chosen = character;
            SceneManager.LoadScene(flightSceneName);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            var image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.text = value ?? string.Empty;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            var image = CreateImage(parent, "SelectButton", new Color(.35f, .85f, .65f, 1f));
            var button = image.gameObject.AddComponent<Button>();
            var text = CreateText(image.transform, "Label", label, 24, TextAnchor.MiddleCenter);
            text.color = Color.black;
            Stretch(text.rectTransform);
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
