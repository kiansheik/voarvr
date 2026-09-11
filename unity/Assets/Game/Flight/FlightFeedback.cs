using UnityEngine;
using UnityEngine.XR;
using VoarVR.UI;
using VoarVR.World;

namespace VoarVR.Flight
{
    public enum AirFeedbackCue { None, LiftAvailable, Climbing }

    // Available rising air and a successful sustained climb are different observations.
    // Dwell and cooldown use active simulation time; gated intervals never bank a cue.
    public sealed class AirFeedbackState
    {
        private float clock, climbSeconds, nextLift, nextClimb;
        private bool inLift;
        public AirFeedbackCue Sample(float dt, bool eligible, float verticalAir, float actualClimb)
        {
            if (!eligible || dt <= 0) { Reset(); return AirFeedbackCue.None; }
            clock += dt;
            bool wasInLift = inLift;
            inLift = inLift ? verticalAir > 1.4f : verticalAir > 2f;
            climbSeconds = actualClimb > .5f ? climbSeconds + dt : 0;
            if (!wasInLift && inLift && clock >= nextLift)
            {
                nextLift = clock + 4f;
                return AirFeedbackCue.LiftAvailable;
            }
            if (climbSeconds >= .85f && clock >= nextClimb)
            {
                nextClimb = clock + 8f;
                climbSeconds = 0;
                return AirFeedbackCue.Climbing;
            }
            return AirFeedbackCue.None;
        }
        public void Reset() { clock = climbSeconds = nextLift = nextClimb = 0; inLift = false; }
    }

    // Bounded procedural motifs and independently sampled wing airflow. Guidance has a
    // separate source so muting it does not silence a catch/contact already being played.
    public sealed class FlightFeedback : MonoBehaviour
    {
        private BirdFlightDriver bird;
        private WindField wind;
        private Transform leftTip, rightTip;
        private AudioSource events, breeze, guidance, restoration;
        private AudioClip impact, touchdown, air, thermalTone, climbTone, catchTone, trickTone, modeTone, viewTone, resumeTone, wingbeat, seedTone, homeTone;
        private readonly AirFeedbackState airState = new AirFeedbackState();
        private float wingAfter, snackAfter, trickAfter;
        private int landings;
        private float clock, contactAfter, leftAfter, rightAfter;
        private bool active, focused = true, applicationPaused;
        private bool audioEnabled = true, hapticsEnabled = true, guidanceEnabled = true;
        private FlightControlMode lastMode;
        private FlightViewMode lastView;
        private bool wasPaused;
        public int ContactCueCount { get; private set; }

        public static float ImpactAmplitude(float speed) => speed < .8f ? 0f : Mathf.Lerp(.15f, .65f, Mathf.InverseLerp(.8f, 14f, speed));
        public static float WindAmplitude(Vector3 flow) => Mathf.Clamp((flow.magnitude - 3f) * .022f, 0f, .16f);

        public void Configure(BirdFlightDriver driver, WindField field, BirdRigDriver rig)
        {
            bird = driver; wind = field;
            leftTip = rig != null ? rig.leftTip : null; rightTip = rig != null ? rig.rightTip : null;
            if (driver == null || driver.Controller == null) return;
            landings = driver.Controller.LandingCount; lastMode = driver.Controller.ControlMode; lastView = driver.ViewMode;
            wasPaused = driver.Controller.State.Phase == FlightPhase.Paused;
            if (events != null) return;
            events = Source(false); breeze = Source(true); guidance = Source(false);
            impact = MakeClip("Soft collision", .16f, 85f, false);
            touchdown = MakeClip("Touchdown rustle", .22f, 180f, false);
            wingbeat = MakeClip("Feather sweep", .18f, 45f, false);
            thermalTone = MakeMotif("Rising air available", .36f, 440f, 0, 0);
            climbTone = MakeMotif("Sustained climb", .38f, 392f, 587f, 0);
            catchTone = MakeMotif("Seed bell catch", .17f, 1047f, 1568f, 0);
            trickTone = MakeMotif("Flight trick", .42f, 523f, 659f, 784f);
            modeTone = MakeMotif("Flight mode changed", .13f, 330f, 440f, 0);
            viewTone = MakeMotif("View changed", .11f, 660f, 550f, 0);
            resumeTone = MakeMotif("Flight resumed", .14f, 392f, 523f, 0);
            seedTone = MakeMotif("Garden seed received", .7f, 659f, 880f, 1047f);
            homeTone = MakeMotif("Garden restored", 1.1f, 392f, 523f, 784f);
            air = MakeClip("Air over wings", 2f, 0f, true); breeze.clip = air; breeze.volume = 0;
            PreferencesChanged();
        }

        private AudioSource Source(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 0; source.loop = loop; return source;
        }

        private bool CanEmitNow()
        {
            if (!isActiveAndEnabled || bird == null || bird.Controller == null || events == null) return false;
            var c = bird.Controller;
            if (!focused || applicationPaused || c.State.Phase == FlightPhase.Paused || c.StreamingBlocked) return false;
            return !bird.UsesXR || (bird.Calibration.Captured && c.LastInput.HeadTracked
                && c.LastInput.LeftWing.Tracked && c.LastInput.RightWing.Tracked);
        }

        public void Tick(float dt)
        {
            if (bird == null || bird.Controller == null || events == null) return;
            clock += Mathf.Max(0, dt);
            var c = bird.Controller;
            bool paused = c.State.Phase == FlightPhase.Paused;
            bool resumed = wasPaused && !paused;
            bool modeChanged = lastMode != c.ControlMode;
            bool viewChanged = lastView != bird.ViewMode;
            wasPaused = paused; lastMode = c.ControlMode; lastView = bird.ViewMode;
            if (!CanEmitNow()) { Silence(); landings = c.LandingCount; return; }
            active = true;
            if (guidanceEnabled && (resumed || modeChanged || viewChanged))
                PlayGuidance(resumed ? resumeTone : modeChanged ? modeTone : viewTone, .1f);
            bool landed = c.LandingCount != landings; landings = c.LandingCount;
            float hit = ImpactAmplitude(c.LastImpactSpeed);
            if (clock >= contactAfter && (landed || hit > 0f))
            {
                ContactCueCount++;
                float strength = landed ? Mathf.Clamp(hit, .14f, .35f) : hit;
                Pulse(XRNode.LeftHand, strength, .1f); Pulse(XRNode.RightHand, strength, .1f);
                PlayEvent(landed ? touchdown : impact, landed ? .18f : Mathf.Lerp(.12f, .4f, hit / .65f));
                contactAfter = clock + .3f; leftAfter = rightAfter = contactAfter;
            }
            bool airborne = c.State.Phase != FlightPhase.Perched;
            Vector3 left = airborne && c.LastInput.LeftWing.Tracked && wind != null && leftTip != null
                ? wind.Sample(leftTip.position, c.SimulationTime) : Vector3.zero;
            Vector3 right = airborne && c.LastInput.RightWing.Tracked && wind != null && rightTip != null
                ? wind.Sample(rightTip.position, c.SimulationTime) : Vector3.zero;
            WindPulse(XRNode.LeftHand, left, ref leftAfter); WindPulse(XRNode.RightHand, right, ref rightAfter);
            var cue = airState.Sample(dt, airborne && guidanceEnabled, c.WindVelocity.y, c.State.Velocity.y);
            if (cue == AirFeedbackCue.LiftAvailable) PlayGuidance(thermalTone, .08f);
            else if (cue == AirFeedbackCue.Climbing) PlayGuidance(climbTone, .095f);
            if (airborne && c.StrokeForce.magnitude > .5f && clock > wingAfter)
            { PlayEvent(wingbeat, .07f); wingAfter = clock + .5f; }
            float airspeed = (c.State.Velocity - c.WindVelocity).magnitude;
            breeze.pitch = Mathf.Lerp(.7f, 1.2f, Mathf.Clamp01(airspeed / 18));
            float volume = airborne && audioEnabled ? Mathf.Clamp((airspeed - 3f) * .003f + (left.magnitude + right.magnitude) * .001f, 0, .065f) : 0;
            breeze.volume = Mathf.Lerp(breeze.volume, volume, 1 - Mathf.Exp(-dt * 4));
            breeze.panStereo = Mathf.Clamp((right.magnitude - left.magnitude) * .08f, -.6f, .6f);
            if (volume > 0 && !breeze.isPlaying) breeze.Play();
            if (volume == 0 && breeze.volume < .001f) breeze.Stop();
        }

        public void SnackCue(int combo)
        {
            if (!active || !CanEmitNow() || clock < snackAfter) return;
            PlayEvent(catchTone, Mathf.Lerp(.12f, .18f, Mathf.Clamp01(combo / 5f)));
            Pulse(XRNode.LeftHand, .12f, .045f); Pulse(XRNode.RightHand, .12f, .045f);
            snackAfter = clock + .09f;
        }
        public void TrickCue()
        {
            if (!active || !CanEmitNow() || clock < trickAfter) return;
            PlayEvent(trickTone, .14f);
            Pulse(XRNode.LeftHand, .16f, .08f); Pulse(XRNode.RightHand, .16f, .08f);
            trickAfter = clock + .6f;
        }
        public void StoryCue(bool restored)
        {
            if (!CanEmitNow()) return;
            var crown = restored ? bird.GetComponent<JourneyPresentation>()?.CrownAnchor : null;
            if (restored && crown != null && audioEnabled)
            {
                if (restoration == null)
                {
                    restoration = crown.gameObject.AddComponent<AudioSource>();
                    restoration.playOnAwake = false; restoration.spatialBlend = 1; restoration.dopplerLevel = 0;
                    restoration.rolloffMode = AudioRolloffMode.Linear; restoration.minDistance = 2; restoration.maxDistance = 90;
                }
                restoration.PlayOneShot(homeTone, .4f);
            }
            else PlayEvent(restored ? homeTone : seedTone, restored ? .18f : .28f);
            float strength=restored?.18f:.24f, duration=restored?.12f:.18f;
            Pulse(XRNode.LeftHand, strength, duration); Pulse(XRNode.RightHand, strength, duration);
        }
        public void PreferencesChanged()
        {
            audioEnabled = FlightPreferences.AudioEnabled;
            hapticsEnabled = FlightPreferences.HapticsEnabled;
            guidanceEnabled = FlightPreferences.GuidanceEnabled;
            if (!audioEnabled)
            {
                if (events != null) events.Stop();
                if (restoration != null) restoration.Stop();
                if (breeze != null) { breeze.Stop(); breeze.volume = 0; }
            }
            if (!audioEnabled || !guidanceEnabled) { if (guidance != null) guidance.Stop(); airState.Reset(); }
            if (!hapticsEnabled) StopHaptics();
        }
        private void PlayEvent(AudioClip clip, float volume) { if (audioEnabled && events != null) events.PlayOneShot(clip, volume); }
        private void PlayGuidance(AudioClip clip, float volume)
        { if (audioEnabled && guidanceEnabled && guidance != null) guidance.PlayOneShot(clip, volume); }
        private void WindPulse(XRNode hand, Vector3 flow, ref float next)
        {
            float amount = WindAmplitude(flow);
            if (amount <= .015f || clock < next) return;
            Pulse(hand, amount, .07f); next = clock + Mathf.Lerp(.85f, .42f, amount / .16f);
        }
        private void Pulse(XRNode hand, float amplitude, float duration)
        {
            if (!hapticsEnabled || bird == null || !bird.UsesXR) return;
            var device = InputDevices.GetDeviceAtXRNode(hand);
            if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                device.SendHapticImpulse(0, amplitude, duration);
        }
        private void StopHaptics()
        {
            if (bird == null || !bird.UsesXR) return;
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).StopHaptics();
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).StopHaptics();
        }
        private void Silence()
        {
            airState.Reset();
            if (!active) return;
            active = false;
            if (events != null) events.Stop();
            if (guidance != null) guidance.Stop();
            if (restoration != null) restoration.Stop();
            if (breeze != null) { breeze.Stop(); breeze.volume = 0; }
            StopHaptics();
        }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) Silence(); }
        private void OnApplicationPause(bool value) { applicationPaused = value; if (value) Silence(); }
        private void OnDisable() => Silence();
        private void OnDestroy()
        {
            if (events != null) Destroy(events); if (breeze != null) Destroy(breeze); if (guidance != null) Destroy(guidance);
            if (restoration != null) Destroy(restoration);
            if (impact != null) Destroy(impact); if (touchdown != null) Destroy(touchdown); if (air != null) Destroy(air);
            if (thermalTone != null) Destroy(thermalTone); if (climbTone != null) Destroy(climbTone); if (catchTone != null) Destroy(catchTone);
            if (trickTone != null) Destroy(trickTone); if (modeTone != null) Destroy(modeTone); if (viewTone != null) Destroy(viewTone);
            if (resumeTone != null) Destroy(resumeTone);
            if (seedTone != null) Destroy(seedTone); if (homeTone != null) Destroy(homeTone);
            if (wingbeat != null) Destroy(wingbeat);
        }
        private static AudioClip MakeMotif(string name, float seconds, float first, float second, float third)
        {
            const int rate = 22050;
            var samples = new float[Mathf.RoundToInt(seconds * rate)];
            int notes = third > 0 ? 3 : second > 0 ? 2 : 1;
            float noteLength = seconds / notes;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                int note = Mathf.Min(notes - 1, Mathf.FloorToInt(t / noteLength));
                float local = t - note * noteLength;
                float frequency = note == 0 ? first : note == 1 ? second : third;
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(local / noteLength)) * Mathf.Exp(-local * 7f);
                samples[i] = (Mathf.Sin(local * 2 * Mathf.PI * frequency) * .5f
                    + Mathf.Sin(local * 2 * Mathf.PI * frequency * 2) * .08f) * envelope;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private static AudioClip MakeClip(string name, float seconds, float tone, bool loop)
        {
            const int rate = 22050;
            var samples = new float[Mathf.RoundToInt(seconds * rate)];
            var random = new System.Random(713); float filtered = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2 - 1, .23f);
                float envelope = loop ? 1f : Mathf.Sin(Mathf.PI * i / samples.Length) * Mathf.Exp(-t * 13);
                samples[i] = (filtered * .65f + (tone > 0 ? Mathf.Sin(t * 2 * Mathf.PI * tone) * .18f : 0)) * envelope;
            }
            if (loop) for (int i = 0; i < 220; i++) { float gain = i / 219f; samples[i] *= gain; samples[samples.Length - 1 - i] *= gain; }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
    }
}
