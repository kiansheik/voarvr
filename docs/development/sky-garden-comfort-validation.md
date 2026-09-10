# Sky-garden comfort and movement recording — 2026-09-10

## Goal
Make Assisted soaring reliable with relaxed arms, soften optional advanced controls, polish a coherent sky-garden world, add playful edible targets, and record enough local movement/flight context to tune enjoyable exercise across playtests.

## Implementation and evidence
- Actual previous Quest recordings identified a protection discontinuity: approximately 5% relaxed span inferred enough tuck to disable feathering. Assisted mode now fades protection only for larger tucks; deliberate trigger tuck remains authoritative. Other modes retain their existing automatic-feathering rules.
- Recorded-input/prescribed-air Magpie comparison over 11.54 seconds: captured -16.48m versus revised +20.68m, zero stalled seconds. Wind vectors are held to recorded values; this is not a spatial world/collision replay.
- Acrobatic inputs have soft dead zones and squared response. Rate limits Duck80/Dragon60/Magpie110 deg/s; damping3.2/1.8/3.5. Full wrist commands retain tested loops. Beginner baseline physics apart from the targeted Assisted protection is retained.
- Expedition progress now shows both quiet height gain and historical peak-altitude requirements. A restart resets the activity and invalidates food sweeps.
- Sixteen original kit meshes: richer trees, ivory architecture, garden flowers, coral/gold SunMoths. Twenty-four moths share one dynamic mesh; generous swept catches, bounded combos, session points/local best and small audio/haptic rewards. Geometry density reduced after review to about512k–552k chunk vertices in sampled views. This is still not a measured Quest budget.
- Sky shader, cloud opening and backed labels improve continuity/readability. Source .blend and FBX were backed up before final regeneration. Auto-review initially rejected the overwrite, then approved the backed-up reversible operation.
- Telemetry v3:295 signals/1200-byte frame, compared with275/2200 previously. Five time/logical fields retain double precision; remaining float signals use float32. Readers acceptv1/v2/v3. Capture still uses a bounded background writer. Clearance queries reduced to10Hz with age recorded.
- New movement, protection, objective and pickup context plus events; history compares sessions with explicit coverage and cached summaries. Quiet movement is not recovery, and no fitness/calorie diagnosis or automatic difficulty escalation is introduced. See [telemetry operations](telemetry.md).

## Validation
- Unity6000.6.0f1 via MCP, BirdFlight scene:172/172 EditMode;20/20 PlayMode, including paused/perched/reset/origin-jump pickup exclusion and ordinary catch.
- Python21/21 tests; Blender5.2.1LTS static validation16 meshes; git diff --check clean.
- Repo check retains24 pre-existing TMP GUID findings; no new missing metadata after refresh.
- Continuous ordinary-input expedition completed at180.403 simulated seconds, physically perched, stage3 complete and one moth caught for10points.
- Live Editor v3:24,774frames,29,865,180bytes, clean footer,0drops. Editor header reused previous build stamp, so it is not current APK identity evidence.
- Three independent review rounds in ignored artifacts/reviews/sky-garden-comfort. Final no high findings; strict visual score4.5, player6.0 provisional. This is improved prototype work, not completion of the higher visual-quality gates.

## Limits and next playtest
Current hardware performance and wearer comfort remain unverified. Initial whole-session Python decode remains memory intensive; history caches avoid repeating it. Quiet-hand estimates miss isometric arm effort. Perched/paused/tracking coverage is reported separately with overlapping categories. A reset/rebase can relocate food; it cannot award a phantom sweep catch. Simultaneous catches share one event but frame totals retain counts.

Play Assisted with duck and dragon using a comfortable calibrated spread. Try several quiet thermal turns, then catch moths and perch whenever desired. Mark awkward and enjoyable moments with both grips plus left-stick click. Compare height gained per active interval, rising-air sinking, protection loss, tracking quality, perching and rewards against the player's reported comfort. Do not prescribe more exertion from movement data alone.

## Build and installation

Quest development APK built successfully in66.890s:82,057,947bytes, SHA256 `2d063c809187c3f4c4f979b6d49d91497f6dfd14ec8dcf3d7f13523693212666`. Unity build log reports success despite an empty-false MCP wrapper response. XR preloads restored before build. ADB reconnect timed out; asked user to wake Quest. Installation and new device logs pending.
