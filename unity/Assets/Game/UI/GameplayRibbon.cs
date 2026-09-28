using System;
using UnityEngine;
using VoarVR.Flight;
using VoarVR.Gameplay;

namespace VoarVR.UI
{
    // The single owner for persistent gameplay text. Flight instruments remain a
    // separate optional layer below it; directors publish state instead of placing cards.
    public sealed class GameplayRibbon : MonoBehaviour
    {
        private BirdFlightDriver bird;
        private TextMesh text;
        private float nextUpdate;
        private Func<string> courseStatus;

        public TextMesh Label => text;
        public bool Visible => text != null && text.gameObject.activeSelf;

        public void Configure(BirdFlightDriver driver, Camera camera, Func<string> courseStatusProvider = null)
        {
            bird = driver;
            courseStatus = courseStatusProvider;
            if (text != null || camera == null) return;
            var root = new GameObject("Gameplay ribbon");
            root.transform.SetParent(camera.transform, false);
            // Keep the always-on objective/collection strip above the forward route so
            // it reads like a game status bar instead of masking the next gate.
            root.transform.localPosition = new Vector3(0f, .9f, 2f);
            text = root.AddComponent<TextMesh>();
            text.anchor = TextAnchor.UpperCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 48;
            text.characterSize = .0084f;
            text.color = new Color(.94f, .98f, .94f);
            root.AddComponent<FlightCard>();
            root.SetActive(false);
        }

        public void SetCourseStatusProvider(Func<string> provider) => courseStatus = provider;

        private void LateUpdate() => TickRibbon();

        public void TickRibbon()
        {
            if (text == null || bird == null) return;
            // Goals and catch totals remain visible when optional flight gauges are hidden.
            bool show = bird.ShowGameplayRibbon;
            text.gameObject.SetActive(show);
            if (!show || Time.unscaledTime < nextUpdate) return;
            nextUpdate = Time.unscaledTime + .15f;

            var challenge = bird.Expedition?.Challenge;
            var foraging = bird.GetComponent<SkyForaging>();
            string objective = challenge == null || challenge.Activity == FlightActivity.FreeFlight
                ? "FREE FLIGHT"
                : challenge.Activity == FlightActivity.ObstacleCourse
                    ? courseStatus?.Invoke() ?? "COURSE  •  READY"
                    : challenge.Status == ChallengeStatus.Completed
                        ? challenge.Title + "  •  COMPLETE"
                        : challenge.Title + "  " + (challenge.Stage + 1) + "/" + challenge.StageCount + "  •  " + ShortGoal(challenge);
            string collection = foraging == null
                ? string.Empty
                : CollectionLine(foraging);
            text.text = objective + (string.IsNullOrEmpty(collection) ? string.Empty : "\n" + collection);
        }

        private static string CollectionLine(SkyForaging foraging)
        {
            if(Time.unscaledTime<foraging.LastCatchNoticeUntil && foraging.LastCatch.IsValid)
            {
                var caught=foraging.LastCatch;
                var definition=CollectibleCatalog.Find(caught.CollectibleId);
                return "+"+caught.AwardedValue+"  "+caught.Rarity.ToString().ToUpperInvariant()+" "+(definition?.DisplayName??"MOTH").ToUpperInvariant()
                    +"  x"+caught.Combo+"   ◆ "+caught.TotalValue;
            }
            var sun=foraging.Score.Tally(CollectibleCatalog.SunMoth.StableId);
            var moon=foraging.Score.Tally(CollectibleCatalog.MoonMoth.StableId);
            var ember=foraging.Score.Tally(CollectibleCatalog.EmberMoth.StableId);
            var crown=foraging.Score.Tally(CollectibleCatalog.CrownMoth.StableId);
            return "☀ "+sun.Count+"   ◐ "+moon.Count+"   ✦ "+ember.Count+"   ♛ "+crown.Count
                +"   ◆ "+foraging.Score.Points+"   x"+Mathf.Max(1,foraging.Score.Combo);
        }

        private static string ShortGoal(FlightChallenge challenge)
        {
            if (challenge.Kind == ObjectiveKind.Soar)
                return Mathf.RoundToInt(challenge.SoaringGain) + "/" + Mathf.RoundToInt(challenge.RequiredGain) + " m climb";
            switch (challenge.Kind)
            {
                case ObjectiveKind.DiscoverLift: return "FIND GOLD LIFT";
                case ObjectiveKind.CollectSeed: return "CATCH THE SEED";
                case ObjectiveKind.Migration: return "FOLLOW THE RIDGE";
                case ObjectiveKind.Precision: return "CROSS THE ARCH";
                case ObjectiveKind.Land: return "LAND AT THE GARDEN";
                default: return challenge.Instruction.ToUpperInvariant();
            }
        }

        private void OnDestroy()
        {
            if (text != null) Destroy(text.gameObject);
        }
    }
}
