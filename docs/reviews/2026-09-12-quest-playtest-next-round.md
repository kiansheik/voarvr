# Quest playtest diagnosis and next-round brief — 2026-09-12

Status: latest Quest recording downloaded and analyzed; next-round work scoped. No runtime code, scene, prefab, build, install, commit or push in this pass.

## Evidence integrity

The Quest reconnected over its existing wireless ADB authorization. The app log containing `VOAR_TELEMETRY_PATH` had rotated, so the helper was given the previously established app-specific external telemetry directory and validated that it existed. Remote modification times showed exactly one new recording since the September 10 evidence set:

- Session `acb16e8e479045f9a1649180e6172698`
- Remote and local size: 116,227,660 bytes
- Remote and local SHA-256: `d5185550a72594874c6df5227eefd05ead3346c3399fe9758b440a6ce98b02bc` (remote verified on the subsequent read-only reconnect)
- Header start: 2026-09-12 10:39:08.004 UTC / 07:39:08.004 BRT
- Approximate end: 08:01:36.94 BRT
- Oculus Quest 3, Magpie, Skyward Expedition, Assisted start
- Schema 3; 96,393 frames; 1,338.96 simulated seconds
- Clean footer, not truncated, zero writer drops

The embedded code/model/catalog source stamp exactly matches the current `BuildRevision` and HEAD `72a714ac764a0500ba692a015370c4915d8fe7a1`. That stamp is not an APK-byte identity: it excludes some assets/settings, and the installed `base.apk` was not pulled. A subsequent read-only reconnect obtained the remote SHA-256 and it exactly matches the local file. The matching hash, successful full decode and clean footer establish a faithful telemetry copy; they do not prove compositor delivery or the exact installed APK bytes.

Raw motion remains ignored under `artifacts/telemetry/quest-playtest-2026-09-12/`. The reproducible focused analysis and normal decoder summary sit beside it. Nothing was deleted from the Quest.

## What this session says

### A genuinely sticky catch loop

The player's enthusiasm for eating moths agrees with the behavioral trace:

| Signal | Result |
| --- | ---: |
| Moths caught | 50 |
| Session value | 870 points |
| Best combo | x5 |
| Reward distribution | 30×10, 8×20, 8×30, 3×40, 1×50 |
| Median gap between catches | 9.83 s |
| Catch speed, median / maximum | 17.62 / 36.68 m/s |

Today every target is the same Sun Moth; value changes only through the six-second combo. There is no type, rarity, persistent inventory, mission quota or `caught/required` contract. This is therefore the right mechanic to deepen: readable rarity, value and authored placement can add decisions without replacing the enjoyable embodied catch.

### A session summary already has useful raw ingredients

These values are derivable from the clean recording:

| Candidate result | Value | Product wording constraint |
| --- | ---: | --- |
| Estimated wingbeats | 1,136 | A motion-derived estimate, not anatomical truth |
| Active hand movement | 884.77 s / 14m45s | Movement proxy, not calories or exertion |
| Quiet airborne hands | 282.18 s / 4m42s | Not physiological rest |
| Supported perch | 153.71 s / 2m34s | A real rest opportunity |
| Paused | 7.12 s | Separate from perching and quiet flight |
| Horizontal travel | 17.475 km | Integrates velocity; resets excluded by continuity rules |
| Three-dimensional path | 19.222 km | Same continuity requirement |
| Gross positive climb | 2.916 km | Sum of upward travel, not net altitude |
| Top groundspeed | 39.40 m/s / 141.9 km/h | Keep m/s internally; present friendly units |
| Landings / takeoffs | 51 / 25 | Controller transition telemetry, not necessarily deliberate tasks |

Development telemetry cannot be the product history: it is disabled in release players and raw files are large personal motion evidence. Add a small always-on gameplay accumulator and save only finalized summaries.

### Campaign navigation failed in a specific, reproducible way

Skyward progressed from lift discovery to soaring at 11.18 seconds and activated the split-stone-arch objective at 81.73 seconds. It never completed:

- The split-arch stage remained active for 1,163.78 simulated seconds, or 19m 24s.
- The bird traveled 17.08 km during that stage.
- The target started 877 m away; the closest approach was still 617 m.
- All three crossings of the target's Z plane missed by at least 957 m horizontally.
- At 20m45s, A/recalibrate restarted Skyward at stage 0.

The HUD starts hidden and was toggled only once, at 262.64 seconds. Thus the arch goal and destination text were absent for its first 181 seconds. This is causal in source: `ExpeditionDirector` hides both its card and world-space destination with the instrument HUD, while `JourneyPresentation` supplies HUD-independent bearings and chevrons only for Route Home. Training, Skyward and Ridge have no equivalent.

Making the same text permanently visible is insufficient. Even after the toggle, the player did not turn toward the arch. All non-FreeFlight activities need world-readable markers, offscreen bearing, approach waypoints, occlusion handling and a strong stage-change cue.

### Contact and ground play matter, but the feedback is under-described

The session contains 422 collision event frames, grouped into 34 contact bouts using a 0.75-second gap, with 51 landings, 25 takeoffs and 22 support-loss events. Collision values have p95 3.77 m/s and maximum 30.83 m/s. One bout contains 268 contact records, consistent with sustained scraping/contact rather than 268 intentional crashes. Telemetry does not record the collider identity, contact point/normal or presentation output, so it cannot say which events were trees versus buildings.

Source inspection confirms the visual mismatch. Streamed Tower, House, Ruin, Rock and Spire meshes use one rotated full-bounds `BoxCollider` each. Openings and irregular silhouettes therefore collide as solid air and can create invisible walkable ledges. Trees instead use a narrow trunk proxy so foliage is passable, though branches outside that shaft are not represented. Hero sky islands already use mesh collision and need a separate audit.

Physics currently sweeps the bird sphere, removes inward velocity and applies a dissipative slide. Feedback supplies a generic nonspatial sound and two-hand haptics based on impact speed. It does not retain surface/contact direction, animate foliage, add directional recoil, vary material sound or provide a camera-safe impact channel.

The player spent 1,096.74 seconds, about 82% of the session, in first person. Directly shaking the tracked first-person camera is therefore a comfort risk. The better contract is directional bird/world response, spatial sound, side-specific haptics and local foliage disturbance in every view, plus a bounded chase-camera spring in third person. Any optional first-person artificial impulse needs a wearer comfort gate.

Ground walking was used: 48.89 seconds of perched stick input produced about 58 m of movement. The input model has only left-stick translation; no right-stick yaw exists. The left stick's X component is strafe, even when the player expects it to steer. Physical torso turning already changes yaw. Add a Perched-only, comfort-bounded right-stick turn and test its interaction with physical rotation and the right-stick HUD click.

### The remaining reported defects are present in source

- **HUD overlap:** the instrument airspeed panel is centered near camera-local `(-0.62, 0.41, 2)`, while the expedition card is independently placed at `(-0.7, 0.42, 2)`. This is an exact layout collision, not a subjective spacing issue. Foraging adds a third independent card.
- **Trigger animation parity:** trigger physics works for every bird. Only the Magpie's `ArticulatedAvian` presenter visibly folds/fans feathers and tail from tuck/flare. Duck (`LegacyAvian`) and Dragon (`Membrane`) lack trigger-only gross-pose presentation. There is no checked-in Swallow character; this brief treats “swallow” as the played Magpie pending visual confirmation.
- **Calibration:** arbitrary comfortable head pitch, including below the horizon, is already captured correctly. Existing UI is text-only and does not explain why the pose matters. A rest-screen `Recalibrate Here` is nondestructive, but A currently resets the world and route before calibrating. The requested diagram, live pose readiness and clearly safe redo flow are missing.
- **Finish flow:** a pause-menu `Save + leave` action exists, but reaching it is obscure: X pauses, a separate right-trigger squeeze opens the menu, then the player must navigate to the fifth row and confirm. Left Menu bypasses that UI, and `Save + leave` returns immediately without results. This recording ended paused with `SessionEnd` but no `CharacterReturn`, so the in-game leave path was not used.

### Additional signals to check, not blindly retune

The user reports that flight is fun, so telemetry anomalies should become targeted observations rather than automatic coefficient changes:

- The bird was classified as Diving for 1,032.7 seconds, about 77% of simulation time, although the right trigger exceeded 0.15 for only about 7.5% of airborne frames. Inferred tuck from arm span was active much more often.
- Normalized head-pitch input was at its positive limit for about 57% of calibrated airborne time.
- The first calibration recorded a 1.439 m span; the late A reset accepted 0.934 m.
- There were 207 stall-entry events and 561 protection-loss events.

These may reflect ordinary folded/resting arm posture, calibration quality, or intentional flying. The next worn pass should mark a few moments and compare posture with phase before changing the flight model.

### Quest delivery was not the main problem in this trace

Render delta p99 was 13.89 ms at the recorded 72 Hz cadence. Twenty-four valid frames exceeded 20.83 ms; generation exceeded 5 ms on two frames. Plausible GPU p99 was 3.875 ms. One impossible raw GPU value was excluded. This does not prove compositor-level zero drops, but it does not show broad frame collapse. The immediate failures are interaction, guidance, collision fidelity and presentation.

## Recommended next-round order

### 1. Make the current session legible and finishable

Create one layout owner rather than three camera-space islands:

- A compact gameplay ribbon for collectible value/type tally, `current/required` objective count and a course timer when relevant.
- Flight instruments toggled independently, preserving quiet Free Flight.
- HUD-independent world markers/bearing/waypoints for every active non-FreeFlight objective.
- Projected-bounds tests that prove long objective copy, counters and instruments do not overlap in either view.

Change left Menu to open the paused context directly. Replace or augment `Save + leave` with `Finish session`, then show a result screen before returning to character selection. Save failures must keep the current flight available for retry.

Add a standalone `FlightSessionTracker` and versioned compact history. It should distinguish:

- Active hand movement
- Quiet airborne movement
- Supported-perch rest opportunity
- Paused time
- Tracking/streaming exclusions
- Walking versus flight distance

Use local player profiles with stable IDs and names so records can be compared on one headset. Preserve the existing user's exact Journey v2 and Foraging v1 keys through a default-profile migration. Raw development telemetry stays profile-free and local.

### 2. Complete embodiment and remove unfair collisions

- Add Perched-only right-stick turning, with a comfort choice and no airborne effect.
- Add a first/redo `CalibrationCoach`: original diagram, headset/controllers, relaxed T pose, comfortable slightly-below-horizon gaze, live tracking/readiness and a nondestructive in-place redo.
- Add architecture-specific Duck and Dragon tuck/flare presentation after core wing IK, without fighting landing folds or Magpie fine-feather animation.
- Import authored low-poly collision profiles for city/temple/rock pieces. Preserve real openings and walk-off edges; do not simply add full visual MeshColliders to every streamed prop.
- Extend contact results with point, normal, side and surface kind; then drive local leaves, spatial material sounds, side-specific haptics and bounded bird/chase recoil.

Collider fidelity is a dependency for timed precision courses. An invisible rectangle in open exploration is ugly; in a leaderboard run it is unfair.

### 3. Turn catching into a reusable game system

Add stable `CollectibleDefinition` assets with type, rarity, base value, presentation and respawn rules. Preserve the current Sun Moth combo behavior as legacy Foraging v1; introduce a versioned v2 result instead of comparing new values with old bests.

An example readable family—not final art or balance—could be common Sun Moth 10, swift Azure Moth 25, sheltered Ember Moth 50 and rare Crown Moth 100. A catch result should expose base value, combo multiplier, awarded value, session total and per-type count. The gameplay ribbon can then show `4/6` or a coin-like total without making the flight instruments mandatory.

There is no enemy or moving-target framework today. Make encounter behavior independent of reward identity so the same catchable can flee, dart, circle, swarm or guard a route without duplicating scoring code. Early “enemies” should extend the physical chase loop rather than introduce combat or exhaustion: evasive insects, moving rivals and course hazards can cost time, break a combo or bump the bird while remaining reusable in open-world missions and obstacle courses.

### 4. Build the course engine, then author levels 1–5

Use one appended high-level `ObstacleCourse` activity and stable string course IDs; do not add one activity enum/checkpoint per level. Data-driven `CourseDefinition` assets should contain content/scoring revisions, supported rules, start/recovery positions and ordered tasks. Reusable tasks: swept gates, typed collectible quotas, altitude bands, corridors, detected tricks and supported landings.

Runtime states:

`Calibration → Ready → 3, 2, 1 → Running → Result → Rest/Retry`

Ranked time should use an injectable monotonic real-time clock. Pause, tracking loss, reset, recovery or streaming blockage must remain available for safety but mark that attempt practice/non-ranked rather than creating a timing exploit. Rest happens after a 15–30 second effort, before the next countdown, and is never penalized.

Suggested first five definitions:

| Level | Target | Reusable tasks |
| --- | --- | --- |
| 1. Moth Line | 15–20 s | Broad gate path, catch 6 common moths, finish perch |
| 2. Canopy Weave | 20–25 s | Flappy-style alternating gaps, optional value targets |
| 3. Ruin Windows | 20–30 s | Faithful openings, ordered gates, rare moth, precise landing |
| 4. Thermal Ladder | 25–30 s | Follow rising-air gates, altitude band, aerial catch |
| 5. Trick and Perch | 25–30 s | Safe open trick volume, crown catch, controlled landing |

Build the runtime and one representative graybox first. Once timing, invalidation, collision fairness and Quest readability pass, levels 2–5 should be content assets rather than new systems.

Store local personal bests and per-profile headset leaderboards keyed by course revision, scoring revision, species, control mode, wind and assistance. A global leaderboard is a later account/backend/privacy/anti-cheat project; none exists today.

## Acceptance gates for the next wearer build

1. With instruments hidden, a first-time player identifies the active Skyward target and turns toward its safe approach without verbal coaching.
2. Visible objective, collectible and instrument UI has no overlap in both views and all species.
3. Left Menu exposes Finish Session; completion shows estimated wingbeats, distances, gross climb, top speed, movement/quiet/perch/pause time, catches and personal records.
4. Reset/recovery cannot fabricate distance or climb; a world rebase remains continuous. Existing Journey/Foraging saves survive unchanged.
5. Duck and Dragon visibly respond to stationary-hand tuck/flare triggers, restore cleanly and retain current physics.
6. Right-stick turn works only while supported, is comfortable, and coexists with physical yaw and HUD click.
7. Authored solid areas collide and visible holes/walk-off edges do not; representative Quest collision/render cost is recorded.
8. Impact feedback communicates side and surface without artificial first-person camera motion by default.
9. Calibration can be understood without coaching and safely repeated in place without restarting progress.
10. One 15–30 second course completes through real movement, cannot tunnel through gates, cannot exploit pause, records a comparable local result and deliberately offers rest before retry.

## Scope boundary

This pass downloaded and diagnosed evidence only. It does not implement the brief, retune flight, validate Duck/Dragon animation on device, prove rendered HUD overlap from pixels, identify individual collision surfaces, estimate calories/fatigue, create an online leaderboard or claim Quest hardware comfort.

## Implementation follow-through — later on 2026-09-12

The analysis above is the historical input to a subsequent implementation pass. The requested local systems are now present:

- Four stable moth types with rarity, value, typed tallies, combo awards and swarm/circle/dart/flee movement; a persistent Mario-like gameplay ribbon stays visible when optional instruments are hidden.
- Compact profile-scoped session summaries/history with movement, rest-opportunity and gameplay records; Left Menu opens the context menu, Finish Session saves transactionally, and a return-only two-column result screen precedes character selection.
- Supported right-stick yaw, code-native first/redo calibration coaching, and Duck/Dragon tuck/flare presentation.
- Exact compound or mesh-derived collision for buildings, tapered roofs, Ruin hex pillars, Rock, Spire and Log; explicit tree branches; local foliage disturbance; spatial/material sounds; side haptics; bird recoil; and chase-only camera impulse.
- HUD-independent campaign direction/distance guidance, modal-safe world markers and three bounded noncombat aerial rivals outside ranked courses.
- One `ObstacleCourse` activity with five data-driven 30-second definitions: Moth Line, Canopy Weave, Ruin Windows, Thermal Ladder and Trick and Perch. It includes a 3-2-1 start, gates/quotas/bands/corridors/tricks/exact landings, practice invalidation, profile-aware local leaderboards and a mandatory six-second rest before retry.

The implementation deliberately does not add exhaustion, calories or fatigue punishment. Quiet flight, supported perching, pausing and the explicit between-attempt break remain positive recovery opportunities.

The final source audit also closed frame-hitch gate continuity, platform-recenter calibration-context preservation, finalized-result preservation, unsupported future-summary preservation, ambient course-moth interference and the remaining unfair collider profiles. Unity 6000.6.0f1 passes 286/286 EditMode (job `101b9fb371d047d0adca5115b01857bb`) and 54/54 PlayMode tests (job `6234e8eebc3548438b151791e57ea7d2`); focused `SkywardWorldTests` pass 5/5 (job `7bc2d7ede2934fa7b63327d157b85908`). Host tooling passes 21/21 and `git diff --check` passes. A fresh independent seven-screen review found no high objective defect and scored all five screenshot dimensions 8/10; this is not Quest hardware validation. The repo checker still reports the known 24 unused TextMesh Pro sample GUID findings. The Quest copy of `acb16e8e…` was later remote-hashed and exactly matches the local file (`d5185550…b02bc`); no newer recording exists.

A current development APK was built through the live Unity Editor in 91.360 seconds with BuildReport `Succeeded`, zero errors and five warnings. `builds/quest/VoarVR.apk` is 82,339,095 bytes with SHA-256 `61a4b5ee5e4aca70ab32b82b7c7052dea4aabcfa0078e9b0a5c09cd82d8d36b7`; ZIP integrity is clean and its embedded source SHA-256 `7764cae054cd89b5b93a609cd983bdd309c2dd62c28ede5835daa8efc573fb10` matches current source. The Android signature, package/SDK/ABI and OpenXR declarations were also verified, and transient XR preload changes were restored exactly with no settings delta. Quest remains `adb offline` (`Host is down` on reconnect), so install, launch, real-controller completion, stereo readability, comfort, collision feel and sustained Quest performance remain unverified. See the [implementation handoff](../agent/session-handoffs/2026-09-12-playtest-features-implementation.md).
