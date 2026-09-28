# Meta VR Start Developer Competition 2026

This document is the competition-facing product brief for VoarVR. It is intentionally narrower than the long-term game roadmap.

## Submission path

**Track:** Gaming  
**Division:** Adapted / Significantly Updated Experience  
**Primary target:** Best Adapted / Significantly Updated Gaming Experience  
**Secondary positioning:** Best First Five Minutes, Boldest Original Concept, and Best Reason to Come Back.

Do **not** enter this repository as a New Experience. VoarVR had a working prototype and commits before the competition window opened on September 24, 2026. The competition explicitly lists implementing hand interactions as an example of a significant update, which makes the controller-to-hands conversion both required for the 2026 rules and an unusually clean adapted-experience story.

The entry deadline is November 18, 2026 at 12:00 PM PT. The project must remain available through judging. The current implementation uses Unity and therefore the competition build should be uploaded to a Meta VR Developer Dashboard release channel named `Competition`, with an invite URL supplied to judges. The demo video must be public on YouTube or Vimeo and under three minutes.

Official sources:

- https://start-developer-competition-26.devpost.com/
- https://start-developer-competition-26.devpost.com/rules
- https://start-developer-competition-26.devpost.com/details/judging
- https://start-developer-competition-26.devpost.com/details/special-awards
- https://start-developer-competition-26.devpost.com/details/faqs
- https://developers.meta.com/blog/meta-connect-2026-vr-start-developer-competition/

## Competition version of the product

The competition pitch is not “a large bird simulator.” It is:

> **Spread your hands. Become a bird. Fly home.**

The judge should understand the fantasy in seconds and experience a complete physical story within roughly five minutes.

The preferred competition slice is a tightened version of **A Route Home**:

1. **Perch / 0:00–0:20.** The player sees their bird and hands/wings responding immediately. A compact comfortable hand pose establishes neutral flight. No controller is paired.
2. **First launch / 0:20–0:50.** One natural downstroke launches the bird. The game teaches by response rather than a text wall.
3. **First mastery / 0:50–1:45.** Glide, bank and make one useful flap. The player learns that hand height/orientation and motion have physical consequences.
4. **Thermal payoff / 1:45–2:45.** A highly legible rising-air feature lets the player climb by spreading and circling instead of repeatedly flapping.
5. **Purpose / 2:45–4:00.** Reach and collect the Route Home seed, then pass through the arch. Existing route guidance should keep the objective in view without requiring HUD text.
6. **Return / 4:00–5:00.** Flare physically, land in the garden, and visibly restore it.
7. **Afterglow.** Show the persistent consequence and one obvious reason to fly again: another route, species, restored sanctuary element, or mastery target.

This slice directly supports the special-award language without sacrificing the main division: fast controller-free learning, an early payoff, an XR-native premise, and persistent progression.

## What to cut from the judging path

The competition branch should aggressively protect the first five minutes.

Do not make any of these prerequisites for submission:

- multiplayer;
- passthrough/MR merely to demonstrate another Meta API;
- a larger procedural world;
- additional species beyond what already exists;
- more acrobatic depth before Beginner/competition flight is excellent;
- a large campaign;
- new economy systems;
- generalized UGC;
- controller parity work beyond preserving the existing fallback.

Passthrough should be added only if it materially changes the experience. The current full-immersion “become a bird” fantasy is already spatially native.

## Rubric translation

The official judging criteria are equally weighted.

### Innovation & Creativity

Evidence we should put in front of judges:

- bare hands become biological control surfaces rather than generic cursors;
- hand pose, wing orientation, motion, air mass and species parameters feed a real force-integrated flight model;
- soaring/thermals create a spatial mechanic that is hard to reproduce on a flat screen;
- the player embodies an articulated bird rather than piloting a vehicle.

The submission text should explain the pre-competition controller baseline and the competition-window hand implementation as a meaningful new platform integration.

### Experience Design

The build must pass three simple tests:

- **Hands-first:** every required action from cold start to completion works without pairing a controller.
- **Airplane-seat:** the competition mode works within an approximately two-foot radius around the seated player.
- **Bus-stop:** there is a complete satisfying arc in ten minutes or less; our internal target is about five.

The competition default should use a **compact wing envelope**. Calibrate a comfortable half-span well inside the two-foot radius and scale that to the selected bird's authored span. Full-wing/fitness mode may remain available, but cannot be the only viable interaction model.

### Technical Implementation

The floor is a stable Quest build at at least 60 fps. Our technical story should be:

- OpenXR runtime retained;
- Meta XR Core / Interaction SDK v207+ added deliberately for hand/gaze capabilities;
- OpenXR hand skeleton used for wrist/palm tracking;
- Wide Motion Mode evaluated for side-of-headset wing motion;
- Fast Motion Mode evaluated from measured tracking loss rather than enabled blindly;
- confidence/source-aware continuity prevents tracking loss or reacquisition from producing impossible aerodynamic impulses;
- gaze + pinch handles menus and settings;
- local telemetry and deterministic replay drive tuning.

The implementation plan lives in [hand flight](../../design/HAND_FLIGHT.md).

### Polish & Presentation

The judge should not be asked to imagine the finished experience. Before content expansion, prioritize:

- readable bird silhouette and feather response;
- strong launch, thermal, seed, arch and landing feedback;
- spatial audio and wind;
- clean hand-first menus;
- stable pause/resume;
- zero obvious tracking pops;
- no unexplained HUD clutter;
- a three-minute trailer made from actual Quest gameplay.

## Hero species

Use **Magpie** as the initial competition hero unless the first hand-tracked wearer tests show a clear readability problem. It is the most developed avian rig, gives the strongest “I am a bird” silhouette, and avoids making the competition pitch depend on a fantasy creature. Duck and Dragon can remain available as retention/replay rewards without entering the critical path.

## Milestones

### Sep 27–Oct 4 — hand foundation

- Import/validate Meta XR Core + Interaction SDK v207+ without replacing the OpenXR backend.
- Build `MetaHandFlightInput : IFlightInput`.
- Enable end-to-end hands in the existing BirdFlight scene.
- Add gaze/pinch UI path for all required menu actions.
- Establish direct/inferred/lost pose telemetry.
- Get first Quest recordings of side wing motion.

Exit: launch, glide, flap, bank, pause/recalibrate and land without a controller.

### Oct 5–12 — compact competition flight

- Add compact seated calibration and scale it into existing bird-space control.
- Evaluate Wide Motion Mode on actual side sweeps.
- Compare default 30 Hz vs Fast Motion Mode 60 Hz using telemetry.
- Tune tracking continuity and reacquisition.
- Make Route Home completable hand-only.

Exit: no physics spikes through direct → inferred → lost → recovered transitions; complete Route Home inside the competition movement envelope.

### Oct 13–25 — first-five-minute loop

- Tighten route spacing/pacing for a roughly five-minute first run.
- Improve thermal readability and early payoff.
- Make launch, seed pickup and garden restoration self-explanatory with HUD off.
- Run repeated fresh-user wearer tests.
- Preserve a visible persistent change/replay hook.

Exit: new player can finish without verbal coaching from the developer.

### Oct 26–Nov 8 — polish and Quest performance

- Quality-loop passes for visual, player and Quest technical review.
- Profile representative route on hardware; maintain >=60 fps and remove obvious hitches.
- Polish audio, animation, UI, hand transitions and restart/resume.
- Freeze feature scope.

Exit: release candidate survives repeated cold-start-to-completion runs.

### Nov 9–16 — submission candidate

- Build to the `Competition` release channel and verify invite from a separate account/device path if possible.
- Produce final screenshots and under-three-minute video from real gameplay.
- Write <=500-word submission description and adapted-experience changelog.
- Verify English text/subtitles, policies, third-party licenses and credits.
- Run a judge-path checklist from a clean install.

### Nov 17 — internal freeze

Only submission blockers after this point. Submit before the November 18 deadline rather than treating noon PT as the working target.

## Competition definition of done

A release candidate is not ready until all of the following are true:

- no controller is paired during the complete judge route;
- all required UI is hands or gaze+pinch;
- the seated compact mode works inside the intended two-foot-radius envelope;
- first meaningful flight occurs in under one minute;
- a complete Route Home payoff is reachable in roughly five minutes and comfortably under ten;
- a tracking dropout/reacquisition cannot add active flap energy or snap the bird;
- WMM inferred pose state is observable in telemetry;
- FMM decision is backed by measured Quest data;
- current representative Quest run stays at or above the competition's 60 fps floor;
- pause, focus loss, resume and recalibration are clean;
- cold install and Competition-channel access have been tested;
- the final video leads with actual hand flight, not menus or architecture slides.

## Submission narrative skeleton

**Inspiration:** people know what spreading their arms like wings means before a tutorial explains it.

**What changed during the competition:** VoarVR's pre-existing controller-tracked flight prototype became an end-to-end hands-first experience. Hand pose now directly controls biological wings, with confidence-aware continuity for the difficult side-of-headset motion inherent to flapping flight. Menus/onboarding are controller-free, and the existing Route Home journey was rebuilt around a compact seated interaction envelope.

**How it works:** the game calibrates the player's comfortable hand pose into bird-specific wing geometry. Relative hand motion and orientation feed the existing force-integrated lift/drag/stroke model. Direct camera tracking, Wide Motion Mode inference, tracking confidence and short-gap continuity are treated differently so tracking artifacts cannot create free energy.

**Why return:** each short flight restores the sanctuary and exposes new routes/species/mastery goals rather than ending as a one-off tech demo.
