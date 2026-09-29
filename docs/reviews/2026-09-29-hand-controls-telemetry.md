# September 29 — first hands flight vs controller flights

The first Quest hands session (`6a5e0f27`, Magpie, 4.9 min, 3.6 min airborne, schema v5, complete, zero drops) was compared against five unpulled controller sessions from Sep 15–19. Raw files and `comparison.txt` are in `artifacts/telemetry/hands-vs-controllers-2026-09-29/` (ignored). Wearer report: controls feel better than expected, but the natural flying pose (arms less cocked forward) does not register, gaining height is much harder than with controllers, and perched wings feel disembodied.

## What the data shows

| Airborne, calibrated | Controllers: Magpie Sep 15 (`73d350d4`) / Sep 17 (`31cf0ec8`) | Hands (`6a5e0f27`) |
| --- | --- | --- |
| Hand motion counted as measured | 99% / 99% | **76–78%** |
| Hands vs head, median lateral / up / forward | 0.54 / −0.50 / −0.03 m | 0.44 / −0.25 / +0.07 m |
| Measured downstroke speed, median / p95 | 1.2–1.4 / 3.1–3.3 m/s | 1.44–1.51 / 2.8–3.1 m/s |
| Stroke force during measured downstrokes, median | — | 2.8–3.0 (on par with controllers' active 2.4–2.7) |
| Flaring phase / brake > 0.15 | 2% / 4% · 31% / 31% | **16% / 16%** |
| Calibrated span | 1.42 m · 0.93 m | 1.00 m (compact) |

1. **The user's inputs were similar; the counted inputs were not.** Stroke speed and cadence match the controller flights. When the cameras see a downstroke, the solver produces the same force.
2. **Height loss was mostly a hidden brake.** Every braking frame in the hands session had the span wider than calibration: the solver treats span/calibrated span > 1 as flare (drag + 20° pitch-up). The compact 1.0 m calibration made the natural wider flying spread a permanent partial flare. Controller flights show the same trap: Sep 17 calibrated narrow (0.93 m) and flared 31%; Sep 15 calibrated wide (1.42 m) and flared 2%.
3. **The natural pose leaves the cameras.** Direct (camera) tracking is 90–100% while hands are 5–25 cm in front of the eyes, but only 50–66% when hands sit in or behind the shoulder plane at 0.2–0.5 m lateral, which is where the controller posture lived. The rest is Wide Motion Mode's inferred pose.
4. **Inferred poses are not usable for flap force.** Source flips happen about once a second per hand, and the pose jumps a median 25–39 cm (p90 up to 71 cm) at each Direct↔Inferred edge. Crediting inferred motion would turn these jumps into false strokes; the safety gate stays. The jumps also pop the wing and jolt the bank signal.
5. 12–14% of downstroke frames were lost to post-flip recovery windows, not to inferred tracking itself.

## Revision (next Quest test)

- **No span flare for hands** (`BirdFlightController.SpanFlareEnabled = !UsesHands`). Spreading wider is just spread wings; controllers keep their behaviour.
- **Flap backwards to flare** (`BackstrokeFlare`): both measured hands sweeping forward faster than 1 m/s for 0.1 s latch the brake (ramped over 0.25 s) until a normal downstroke. Recorded hand downstrokes stay under +0.73 m/s forward at p99. Held raised spread wings near a perch still brake as before.
- **Natural calibration envelope**: 0.25–0.65 m out per side (was 0.35–0.55), minimum span 0.5 m, coached as "open your arms, hands just ahead of your shoulders" — the controller posture, nudged into camera view.
- **Continuous inferred handoff**: on Direct→Inferred the wing keeps its last camera position plus inferred motion; the offset relaxes at 0.15 m/s (capped 0.6 m). Motion stays estimated.
- **Recovery warmup 0.12 → 0.06 s**; the jump rejection, stable-interval requirement and 8 m/s cap are unchanged.
- **Perched arms mirror**: the ground wing fold follows the tracked span (fully folded at ≤45% of calibrated span, open at ≥85%) instead of always folding.

Tests: EditMode 324/324, PlayMode 70/70. What to look for on Quest: climb rate and flaring fraction versus the table above, direct fraction in the new calibration pose, whether the backstroke is easy to perform deliberately and never fires during normal flapping, and whether the continuous inferred handoff feels better than popping. Fast Motion Mode stays off: downstrokes were 92–97% camera-tracked, so frequency was not the limiting factor.

## Revision 3: hands-free countdown calibration (session `15335227`)

The second session showed the calibration was physically contradictory. The user's natural glide pose was steady and camera-tracked at 0.60–0.66 m lateral, about 0.15 m below the eyes, in the head plane, but READY required aiming the head at a button and bringing a hand forward to pinch, which breaks exactly that pose. After a platform recenter at 114 s the user could not complete the recalibration for about 260 s.

New flow (`Input/HandCalibrationFlow`, driven by `CalibrationCoach.UpdateHands`):

1. **Glide pose**: hold a natural pose (0.18–1.0 m out, level, steady below 0.35 m/s, camera-seen or inferred) for 1 s.
2. **Look left ◄◄ / look right ►►** (30°, 0.4 s each) while holding it. Each hand leaves camera view while physically still, so the flow records camera vs inferred positions and learns each hand's inferred-pose bias.
3. **START** by look + pinch, arms free (the only gaze target; the card follows the head otherwise).
4. **Return to the pose**: when head and both hands are ready, a 3-2-1 countdown runs (brief blips under 0.25 s keep the count) and the last frame becomes neutral (`CaptureNaturalHands`, inferred hands accepted).
5. The first calibration **launches a glide** off the ridge at cruise speed (`LaunchGlide`). Redo, recenter and resume reuse the learned context: pose → countdown → flight resumes directly, with no menu step.

Solver use of that context: an inferred hand's offset now relaxes toward its calibrated bias instead of zero, and once settled (0.15 s), same-source bias-corrected inferred motion earns **50% stroke authority** (capped 4 m/s). It is still `MotionEstimated`, so it never counts as measured effort, a calibration input or a takeoff stroke, and the recorded 25–39 cm edge jumps remain excluded by derivative resets. `WingInput.EstimatedStrokeAuthority` defaults to 0, so every other estimated source stays forceless. Tests: EditMode 329/329, PlayMode 70/70. Telemetry v5 does not yet record the authority value or the learned bias.
