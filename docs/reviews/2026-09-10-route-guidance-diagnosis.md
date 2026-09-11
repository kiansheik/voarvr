# Route Home guidance diagnosis

Independent diagnostic review of the user report: “The objective of the route home still wasn't clear, I couldn't see what I was supposed to follow. I couldn't tell whether I had the seed or not.” Inspected current source and round-03 images directly; no live Editor mutation or other critics' reports. This reviews the implementation before the guidance revision, not its eventual fix.

**User constraint confirmed during diagnosis: keep flight text hidden.** Use world affordances, distinct shapes, motion and an inspectable physical inventory state; the fixes below do not authorize re-enabling objective prose or instruments.

## Actual Quest session

Decoded `artifacts/telemetry/route-guidance-2026-09-10/a5697a0c1b8e43429ca4532eb178ae63.voartlm`: Magpie, RouteHome, Quest3,37,899frames,526.55s recorded simulation delta, clean close, zero drops. Header UTC is2026-09-11T02:13:43.9383090Z (September10 local); source SHA256 is`7eda92d41bd4d0000e637a8d842d82aa3c1eb06628cd0d30fcc39d6c6f43ed81`, matching the RouteHome package. This is actual installed-build evidence.

**The seed was never collected in this recording.** Both attempts reached CollectSeed(stage2); neither reached Precision(stage3), Land(stage4) or completion. No direct seed flag exists in schema3, so this conclusion follows the authoritative stage transition, which only advances2→3 after pickup. Four reset events include the initial calibration reset and three later resets.

| CollectSeed attempt | Entry, relative to session start | Seed direction at entry relative to flight velocity | Closest approach while pickup was active | Consequence |
| --- | --- | --- | --- | --- |
| First |125.17s; player`(1047.36,317.82,-483.29)` |147.56° behind;1,138.20m away |1,102.80m over99.86s | The story-seed mesh was outside its850m visibility cutoff for the entire attempt. The moving guide jumped behind the player. |
| Second |334.41s; player`(384.03,278.14,475.82)` |126.33° behind;369.76m away |369.76m over192.40s | The player never came close to the8m pickup radius; this was not a near-miss pickup failure. |

Soar can complete in other rising air long after the bird has flown beyond the departure seed. This is valid progression, but it creates a reversal that the current guidance does not communicate. A regression fixture must cover this case.

Actual physical head-forward pitch relative to body had median+26.15° and95th percentile+70.61°; yaw had95th percentile+44.96° and99th+79.86°. Tracking translation from calibration had95th percentile0.244m, maximum0.381m. These are tracked head orientation/position, not eye tracking, and show why neutral-head screenshots are insufficient. The session contains both views and both control modes. Guidance preference, marker visibility and final camera pose are not recorded, so exact screen visibility cannot be reconstructed from telemetry alone.

A source-formula reconstruction of the departure column at the old Soar guide target predicts6.85s below its1.5m/s display gate out of181.42s in Soar. This excludes a full Unity total-field replay and is supporting inference, not measured renderer visibility. Machine-readable calculations and concise capture report are in adjacent ignored `baseline-summary.json` and `baseline-report.md`.

## Highest-value findings

| Severity / classification | Evidence and player impact | Bounded fix |
| --- | --- | --- |
| **High — objective defect:** the quiet view has insufficient world affordances to replace hidden mission text. | `ExpeditionDirector.LateUpdate` gates both the instruction card and named world beacon on `driver.ShowFlightText`; the HUD starts hidden. Remaining leaves have no explicit “go here / circle here” distinction or offscreen locator. `JourneyPresentation.Present` places one formation along the direction to the target, recalculated from the player every frame, at up to28m. It can be behind, above, terrain-occluded or retreat ahead of the bird. During Soar the target is always at least20m above the player, so following it never provides an arrival event or teaches the required circling behavior. Actual pickup targets were126–148° behind the player's motion. | Keep text hidden. Use a distinct world target silhouette and a brief visible reacquisition motion when the destination changes behind the player; give circling-in-lift its own moving demonstration rather than a continuously retreating upward target. Guide the player to a reachable world location and acknowledge arrival. |
| **High — objective defect:** guidance visibility is tested at an artificial climb target rather than the air being indicated. | `JourneyPresentation.Tick` samples wind at `GuideTarget`, whose Soar height is `max(RequiredAltitude+15, playerHeight+20)`, then hides the entire guide below1.5m/s. `FlightRegions.HighAir` makes departure convection finite around altitude165 with half-height185. Thus an uncompleted ascent can lose its navigation as its aim rises out of the column, even when useful air exists lower down. The same hide decision leaves no explanation in Still Air. This defect is established by the source contract; frequency in the user's session requires telemetry. | Keep navigation available independently of the lift signal. Sample the true thermal center at the player's relevant flight height, and use actual local wind/climb to distinguish “find lift”, “circle in lift”, and “return to lift”. If current weather prevents this objective, explain it and give a deliberate way to return to Assisted thermals rather than pointing into empty air. |
| **High — objective defect:** collection has an event but no dependable inventory confirmation. | The seed appears only during CollectSeed, disappears on swept pickup within8m, then becomes a `.035`-scale copy attached ahead of the bird's authored eyes. `StoryCue` is a one-time procedural chime/haptic; no durable explicit inventory affordance accompanies it. The carry mesh has no visible attachment and can read as another floating object. `FlightCamera` independently applies tracked head rotation/translation while the carried mesh follows body pose; looking aside or moving the head can remove it. The existing Dragon offset fix only addressed neutral eye placement. | Animate unmistakable acquisition into a recognizable carried location and preserve an inspectable empty/filled/delivered physical state derived from `Challenge.SeedCollected` and completion. Restore it immediately on resume, without adding flight text. Distinguish the acquisition motion/sound and carried silhouette from optional SunMoth catches. |

## Three acceptance tests that would have caught this

1. **Runtime wayfinding continuity:** exercise actual `JourneyPresentation.Tick` with the driver/director/wind, for every real stage, at departure, thermal entry, ascent inside/outside the column, and above its usable ceiling. Include Still Air, paused/tracking-invalid states, origin shift and a target behind the camera. Verify navigation is recoverable, lift assertions match sampled wind, the Soar destination does not keep receding upward, and guidance-off is intentional. Capture the actual player camera at these states.
2. **Inventory lifecycle and tracked view:** collect by a real swept observation, miss just outside the radius, resume a collected checkpoint, restart and complete. Assert the visible physical inventory state matches authoritative progression in every case, with audio/haptics disabled and all flight text/instruments hidden. Capture all species in first/third person, Beginner and advanced/stabilized views, with representative tracked head yaw, pitch and translation. It is acceptable for the embodied seed to leave view; it is not acceptable for the player to lose all way to check whether it is carried.
3. **Guidance-driven route comprehension:** run a pilot that consumes the same public guidance target/state as the player display rather than private mission coordinates and prewritten stage-specific route knowledge. Then ask a wearer, without coaching, to identify the next action and seed state at thermal entry, immediately after pickup and after resume. Log stage dwell, time without an available cue, time target is offscreen, reacquisition and collection. Scripted completion proves reachability; this final wearer test establishes comprehension.

## Why the prior screenshots missed this

`RouteHomeReview.Visuals` advances its challenge to CollectSeed before **all** of the lowland/ascent/arch/garden fixtures and calls `Present(..., guidanceEnabled:false, ...)`. Their filenames describe scenery, not the running objective state. The viewed lowland fixture contains a tiny orange seed high in the sky; the ascent fixture has no usable direction. `CarriedSeedViews` enables guidance but calls `Present` directly, bypassing the runtime wind gate, and explicitly records **no tracked head offset**. These images cannot validate real-stage wayfinding or physical-head inventory awareness. `JourneyWorldTests` likewise tests direct `Present` calls and state/rebase budgets; it does not exercise `Tick`'s visibility gate.

The input-only route pilot also has extra route knowledge: it circles the departure thermal, climbs again near the island and sets up an arch approach. Its completion is useful physics evidence, but does not show that the actual displayed leaves communicate those maneuvers.

## Evidence-scoped scores

| Dimension | Score | Basis |
| --- | --- | --- |
| Objective comprehension | **3/10** | User reports failure; necessary semantics are hidden by default. |
| Navigation continuity | **3/10** | One ambiguous moving formation, no offscreen reacquisition, wind gate can remove it. |
| Seed state feedback | **4/10** | Physical carried object and acquisition sound exist; persistent semantic confirmation is missing. |
| Static seed silhouette | **7/10** | Seed is distinct in the close staged image; this does not establish discoverability in flight. |
| Wearer comfort and sustained Quest performance | **Unverified** | Still images and source cannot establish these. |

Priority is the three fixes above. New art packs, extra quests and higher particle density would not resolve the reported failure. Fresh runtime evidence and a short wearer comprehension check are required before calling navigation accepted.
