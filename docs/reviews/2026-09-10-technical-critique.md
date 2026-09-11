# VoarVR: independent standalone Quest technical critique

Date: 2026-09-10. Scope: current first-party source/settings, authored asset pipeline, existing screenshots and recorded playtest summaries. Documentation-only review; no editor mutation, build, device session or fresh Unity test run. This review inspected current artifacts before other critics' conclusions.

**Verdict:** the engineering foundation is unusually useful for a young flight prototype: simulation is separable from input, the world has bounded pools, motion has a versioned recorder, and important flight/contact behaviors have deterministic tests. It is not yet a measured platform for a beautiful, content-rich, interruption-safe 30–45 minute game. The next investment should turn those foundations into reliable production constraints before multiplying assets or adding a campaign.

Scores below grade the reviewed implementation against that longer-session ambition, not against an established commercial release. They are critical judgment, not benchmark results.

| Dimension | Score / 10 | Basis and limitation |
| --- | ---: | --- |
| Simulation/input separation and regression coverage | 8 | Pure flight/controller inputs, synthetic trajectories, collision and rebase tests; no fresh suite executed here. |
| Telemetry usefulness and evidence honesty | 8 | Raw/mapped input, calibration, build identity, event transitions, missing-value flags and clean-close/drop reporting. Metrics still need presentation-time correlation and longer capture workflows. |
| Standalone rendering architecture | 6 | Mostly opaque palette shaders, shared meshes/materials and bounded effects are sensible. Ground detail has no production distance LOD; total render cost is not in the evidence. |
| Asset pipeline readiness for richer content | 6 | Reproducible Blender validation/FBX staging and identity preservation; authored animation, material baking and automatic runtime LOD wiring are explicitly future work. |
| Content/progression extensibility | 4 | Four activities, hardcoded objective branches and local best scores are a usable slice, not a campaign format. |
| Long-session persistence and operational readiness | 4 | Scene-lifetime progress, no resumable expedition checkpoint, no dev-recording size ceiling, and short bounded traversal tests. |
| Overall technical readiness for the requested next milestone | 6 | Strong prototype architecture; needs measured hardware budgets and interruption-safe content before a large expansion. |
| Sustained Quest frame delivery, thermal behavior, memory plateau | **Unverified** | Existing brief timing summaries cannot establish a complete 30–45 minute device result. |
| Worn comfort, physical fatigue, tracking-to-display latency | **Unverified** | Cannot be graded from code or still captures. |

## Evidence used and its limits

- Visual artifacts personally inspected: `artifacts/reviews/sky-garden-comfort/round-03/{island-approach,lowland-departure}.png` and `artifacts/reviews/quiet-cockpit/round-01/hud-enabled.png`. These are desktop evidence of composition/geometry, not through-lens stereo or standalone performance evidence. The inspected world uses repeated faceted trees, towers, floating islands and opaque cloud sculptures with substantial distance haze.
- `artifacts/reviews/sky-garden-comfort/round-03/world.txt`: six samples contain 25 active chunks, nine islands and **511,813–551,527 chunk vertices**. `WorldStreamer.VertexCount` sums only active ground chunks. It omits island/cloud/creature/effect/UI meshes and is neither a visible-triangle count nor total GPU vertex workload.
- `artifacts/telemetry/quest-quiet-cockpit-2026-09-10/be892784e6bb4e749831cef23cb386e6.summary.md`: Magpie, 15,930 frames, 221.24 simulated seconds, clean close, zero recorder drops; render/unscaled delta p50/p95/p99 0.01389 seconds; CPU p95 14.69 ms, GPU p95 3.503 ms; three streaming-block transitions, three recoveries. This recording precedes the latest neutral-torque patch; do not present it as post-patch acceptance. Recorder drops are not dropped display frames.
- `artifacts/telemetry/quest-neutral-trim-2026-09-10/grip-analysis.json`: newer Duck/Magpie recordings are complete, have zero recorder drops and identify SourceSHA256 `b8bb65ee…`. Their input/torque evidence supports the neutral-equilibrium diagnosis, not a long-session performance certification.
- Current source includes uncommitted advanced-neutral work. Preserve it. `docs/agent/current-state.md` identifies the latest installed patch as wearer-validation pending. Prior test counts in that document were not rerun for this critique.
- Current rendering: `Assets/Settings/VoarVRPipeline.asset` has HDR/depth/opaque textures off, 4× MSAA, render scale 1 and SRP Batcher enabled. `SkywardVertex.shader`, `DuckVertex.shader` and `SpiritSurface.shader` use custom opaque unlit passes with authored directional shading; enabling real lights/shadows alone will not make those surfaces respond to them. `WindRibbon.shader` is additive transparent with no depth writes.

## Priority findings

### T01 — Measure the complete frame before spending the graphics budget

**High · unverified risk.** A 72 Hz delta distribution is encouraging but does not identify CPU work, presentation waits, stale compositor frames, thermal throttling or spare capacity. `FlightTelemetry.cs:128–133` records `FrameTiming.cpuFrameTime` and GPU time without frame-timestamp correlation or main/render/present-wait breakdown. Current `ProjectSettings.asset` has `enableFrameTimingStats: 0`, although historical capture reports contain values; the enabled provider/build configuration and meaning of those values should be recorded explicitly.

Unity defines `cpuFrameTime` to include waiting and overhead, so **14.69 ms cannot by itself prove a missed 72 Hz deadline**. Store wait time separately and correlate valid samples with device metrics. [Unity FrameTiming reference](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FrameTiming.html). Meta's OVR Metrics Tool can record frame rate, stale frames, utilization, heat and throttling; use it alongside application markers. [Meta OVR Metrics Tool](https://developers.meta.com/horizon/documentation/unity/ts-ovrmetricstool/).

**Task:** add an explicit review capture that reports build/source hash, Quest model/OS, refresh/resolution, thermal state, app CPU/GPU work, stale/missed frames, GC allocations, managed/native memory, visible geometry, draw calls/SetPass calls, active colliders and generation phase timings. Maintain metric availability and clock domains. Do not silently substitute zero for unsupported data or label elapsed CPU time as execution cost.

**Acceptance:** a repeatable cold-start and 30–45 minute scripted route, with a separate voluntary worn session; cover lowland canopy, island approach, rain, thermal/effect overlap, low-altitude fast traversal, reset, menu return and resume. Identify worst intervals rather than only whole-session averages. Agree refresh and actual per-feature CPU/GPU/memory ceilings from that run. At 72 Hz, the nominal display interval is 13.89 ms; it is not an allowance for each subsystem. Do not invent universal triangle/draw-call limits or claim an unmeasured 90/120 Hz target is free.

### T02 — Spend detail where the player can perceive it

**High · unverified scaling risk; objective implementation limitation.** `WorldChunk.cs:41–57` builds five renderers per ground chunk; authored details are merged into the fifth. All active chunks retain that detail irrespective of viewing distance. Haze in that renderer reduces contrast, not triangle processing. `SkyArchipelago.cs` already has a useful two-representation switch at 520 m, but switches abruptly and uses the same threshold in both directions.

**Task:** introduce explicit near/mid/far representations for ground features, separate hero/perch geometry from inexpensive distant massing, and add hysteresis to representation changes. Reuse island variants and shared meshes; do not solve every cost by merging an entire region into one mesh, which also removes fine culling opportunities. Give cliffs, arches, waterfalls, ruins and perch entrances stronger silhouette variation before adding density everywhere. Match runtime LOD data to authored Blender LOD sources.

**Acceptance:** one representative finished region has near detail, midrange silhouette and distant massing captures at fixed flight paths; the same collision surface IDs and physical routes remain valid at every LOD. Demonstrate a lower measured distant render cost, no visible oscillation at a threshold and no missing islands during fast turns. Keep absolute budgets dependent on T01.

### T03 — Make streaming reliable during fast, tired or distracted play

**High · unverified risk with recorded events.** `WorldStreamer.cs:94–101` checks the 1.5 ms budget before a `GenerateStep`, so it cannot preempt an expensive upload/normal recalculation/collider cook. `Configure` and `ResetOrigin` finish the center synchronously. `WorldChunk.cs:101–116` still contains atomic mesh work. The Magpie capture has three streaming block/recovery pairs; these are simulation readiness events, not proof of a visible freeze, but they deserve duration/context analysis.

**Task:** instrument separate generation phases and collision cooking, then choose the smallest fix demonstrated by the trace: predictive prioritization in the velocity/landing direction, smaller geometry upload units, reusable prebuilt feature meshes, or prewarmed collision data. Preserve fail-closed contact readiness. Avoid a speculative job/ECS rewrite.

**Acceptance:** deterministic routes at the supported speed envelope traverse boundaries/diagonals in both directions, reverse rapidly, reset and land on recently loaded terrain without tunneling or stale colliders. Report every blocked interval and the worst atomic operation. No operation repeatedly exceeds its agreed T01 budget on the target device; any unavoidable loading is presented at a deliberate safe transition rather than during an approach.

### T04 — Save the journey before making the journey longer

**High · objective capability gap; interruption-loss risk.** `ExpeditionDirector.cs:18–33` reads a JSON `PlayerPrefs` record and persists only on completion. `FlightProgress` stores four best values and a version field; the loader has no version migration behavior. `SkyForaging.cs:72` saves its best only in `OnDestroy`. A longer mission cannot currently be resumed after process termination; an unclean termination can also lose a newly achieved foraging best.

**Task:** first add interruption-safe progress boundaries: milestone/landing checkpoints, a versioned save model, idempotent rewards, migration from existing local scores, and recoverable corrupt-save handling. Persist foraging best at a bounded safe checkpoint instead of relying solely on destruction. Keep physical calibration separate: resume at a known safe perch and require valid tracking before flight. Do not resume an airborne body immediately into a wall or automatically restore a prior user's arm calibration.

**Acceptance:** existing bests survive migration; completed rewards are awarded once; close/reopen after every stage resumes the documented safe state; controlled background/resume and development process-termination tests preserve the last acknowledged checkpoint. Bad/unknown schema does not silently overwrite recoverable data. Tests cover missing character/content IDs. Scope this to local saves; cloud services are not required.

### T05 — Build content definitions only when the next mission needs them

**Medium · objective extensibility limitation.** `FlightChallenge.cs:49–100` embeds stage ordering, destination coordinates, instructions, thresholds and scores in conditional branches. That is understandable for one slice; a future agent adding each story chapter directly to these branches will make content validation and tuning fragile.

**Task:** extract the next two genuinely different authored missions into data definitions, with stable IDs, world seeds/locations, objective requirements, localized text keys, reward/checkpoint IDs and permitted control/accessibility modes. Preserve a pure observation-based evaluator. Add authoring validation for unreachable prerequisites, invalid destination references and unavailable landmarks; avoid constructing a general visual scripting system.

**Acceptance:** the current activities retain behavior under replay/regression tests; designers can change a route/stage threshold without touching movement code; two different missions share evaluators without copying branches; invalid content fails validation with an asset-specific message. Depend on T04 before offering multi-session chapters.

### T06 — Make better materials and lighting a deliberate art/rendering contract

**Medium · objective limitation plus aesthetic proposal.** The current opaque vertex palette is efficient and coherent, but surfaces cannot express much local material identity. Current shader shading is independent of Unity scene-light shadows; a heavy lighting setup could increase cost without delivering the expected art change. In the inspected images, ground features fade strongly into the same blue haze and cloud/island shapes repeat.

**Task:** keep a strong stylized direction and upgrade a small representative asset set: moss/stone differentiation, weathered ruin edges, readable vegetation clusters, wing/feather highlights and deliberate perch contact shading. Start with vertex color/AO baking, carefully bounded palette/trim textures and controllable sun/sky palette parameters. A selected close-contact shadow solution is a test candidate, not a global demand. Extend the Blender contract for UV/baking and import compression only when that candidate uses textures.

**Acceptance:** same camera/path and headset captures compare the finished representative set against current art; material identity improves without sacrificing thermal cues or bird silhouette. Shader paths render correctly in both eyes on the built Android player. Record shader variants, texture memory and GPU delta. Decide whether to expand that treatment only after T01 measures it. Avoid starting with fullscreen volumetrics, many dynamic lights, mass transparency or a large purchased asset pack.

### T07 — Separate render complexity from contact complexity

**Medium · unverified scaling and interaction risk.** `SkyArchipelago.cs:29,57` assigns the combined garden/decor visual mesh to a `MeshCollider`; `WorldChunk` already uses terrain plus simple building proxies. The flight body remains a sphere; the camera has no obstruction sweep in `FlightCamera.cs`. Richer branches/bridges can therefore increase collision cost while still permitting cosmetic wing or camera penetration.

**Task:** author named simple collision meshes/landing surfaces alongside visual assets, keep decorative leaves/feathers nonblocking, and add camera obstruction handling at perches and in chase view. Define which contact is gameplay-relevant before introducing articulated wing colliders. If a narrow-gap trick needs wing clearance, use an explicit course clearance rule and tested swept query, not arbitrary decoration collisions.

**Acceptance:** every curated landing location has a safe camera volume and valid support normal; approaches work from intended directions at supported speeds; graceful failed approaches cannot trap the player. Visual LOD changes do not modify collision identity. Report collider count/cooking cost separately from rendered geometry.

### T08 — Turn procedural rig potential into production animation

**Medium · objective pipeline gap; subjective quality opportunity.** Current curated Duck/Dragon/Magpie importers disable animation. Blender documentation explicitly leaves animation export and runtime LOD wiring for future work. Magpie has 63 bones against the current 64-bone pipeline guardrail; adding more independent feather bones is not the default route to higher fidelity.

**Task:** define a small animation set—perch settle, idle breathing, look/attention, feeding, launch and landing anticipation—and a clear blend policy between authored body/legs and tracked wings. Preserve the player's 1:1 head response and input-owned wing intention. Bake less visible secondary motion or simplify distant rigs; treat bone-count increases as reviewed pipeline changes.

**Acceptance:** each species is inspected through downstroke, upstroke, full fold, braking, tracking loss/recovery and landing, from first and third person; no snapped feet, disconnected feathers, skin inversion or rest-pose reset on transition. In-headset body ownership remains a wearer gate. Costs are measured for the player plus the maximum proposed NPC flock, not one idle bird alone.

### T09 — Longer exercise play needs honest movement metrics and kinder reward accounting

**High · objective goal mismatch if the current score becomes the exercise score.** `FlightChallenge.cs:100` subtracts two points per second of active stroke force, intentionally rewarding efficient soaring. `FlightEffort.cs` reports hand-motion thresholds; three quiet seconds set `Resting` even though holding wings out can still be tiring. Paused/perched periods are excluded from that metric. Neither value measures physical recovery or calorie expenditure.

**Task:** maintain a distinct soaring-technique score and an opt-in active-flight objective model. Expose movement time/coverage honestly, add voluntary effort ratings and explicit break/arms-down events, and ensure rest/perch exploration can still reveal story or collectibles. A difficulty/effort profile should change achievable movement amplitude and assistance with visible player consent, not silently make a tired player flap harder.

**Acceptance:** rest never removes already earned rewards; ending after a short completed leg is a valid success; seated/reduced-reach profiles can complete the intended equivalent routes; reports never rename quiet hands as physiological recovery. Wearer tests ask why the player continued or stopped, separating fun, exertion, discomfort and confusion. Do not pursue compulsive duration as a success metric.

### T10 — Keep development recordings useful during repeated long sessions

**Medium · objective development scaling limitation.** `TelemetryWriter` has a bounded 512-record ring and asynchronous I/O, but files continue growing. At 72 samples/s, 1,200-byte payloads plus five bytes of per-frame envelope are approximately **312 MB per hour**, before headers/events. `docs/development/telemetry.md` explicitly states no recording is automatically deleted; initial Python analysis loads the whole recording. Development-only default limits the shipping impact, but repeated test sessions will accumulate storage and analysis memory.

**Task:** implement streaming/incremental analysis and explicit recording size/retention controls, with export/pin support for important playtests. Never silently delete existing evidence. Separate lightweight release session summaries from optional developer motion traces; keep raw motion local by default. Expose stopped recording/storage errors to the developer without interrupting flight.

**Acceptance:** analysis of a full long-session recording uses bounded memory; size rollover keeps all event/build/calibration context needed for interpretation; decode detects loss/truncation across segments; a selected recording survives retention; disk-full stops recording cleanly while gameplay remains responsive. A zero recorder-drop result stays distinct from zero display-frame misses.

### T11 — Grow regression coverage around player journeys

**Medium · objective evidence gap.** `WorldStreamingTests.cs` verifies bounded reuse across eight locations and terrain identity; `OriginSafetyTests.cs` verifies one rebase. Those are useful invariants, not proof of an hour without leaks, state drift or lost progression. Latest UI lifecycle tests cover recent leaks, but richer UI/audio/NPC content will introduce more lifetime owners.

**Task:** add one reproducible continuous journey harness with prescribed input, repeated boundaries, landings, menu transitions, calibrated restarts, HUD toggles and supported tracking loss. Collect object/mesh/material/audio-source counts and writer completion after scene disposal. Keep real wearer playtests separate; do not call synthetic completion human usability.

**Acceptance:** after warmup, live object/native memory counts reach a repeatable plateau rather than rising per restart; twenty menu-flight cycles leave no orphan cards, meshes, audio or writer threads; repeated rebases preserve logical positions and objective identity. The same trace can be replayed after future art/content changes. Unity suites must actually compile/run; a source scan is not compilation.

### T12 — Repair the validation signal without mixing it into gameplay work

**Low · known repository hygiene defect.** The agent docs consistently record 24 unresolved TMP GUID findings from an incomplete unused import. This makes a general repository check noisy and makes future new failures easier to miss.

**Task:** separately inspect actual dependencies, then remove or repair that incomplete import using Unity asset workflows. Do not delete it merely because old docs call it unused; recheck references. Preserve first-party work and GUIDs.

**Acceptance:** repository validation produces a clean baseline or a narrow documented exception list; Unity import/build still resolves all needed UI assets. No runtime feature changes in this cleanup.

## The next three work packages

1. **Measure and stabilize the representative journey (T01, T03, T11).** Deliver a route, matching device trace and specific bottleneck fixes. This sets the graphics budget and establishes that extended play does not degrade.
2. **Make every completed leg durable (T04, the reward separation in T09).** Deliver local checkpoints, migration and interruption tests. This makes rest and return compatible with progression.
3. **Finish one region as the art/content standard (T02, T06, T07, then T08/T05 as needed).** Produce a small hero asset/animation kit, perceptual LOD, safe perches and two distinct tasks in that region. Measure it before ordering many more biomes or NPCs.

T10 is worthwhile while collecting those sessions; T12 is a separate cleanup. Multiplayer, sophisticated fluid simulation, broad engine replacement and a large downloaded environment are deferred until one excellent region proves the intended fun and performance. The distinguishing technical ambition should be responsive embodied flight through a world whose wind, animation, landmarks and tasks agree with one another.

## Reproducibility and follow-up

Inspected: required `docs/agent` pages and quality loop; architecture/Blender/telemetry docs; current render pipeline/renderer/manifest/import settings; WorldStreamer/WorldChunk/SkyArchipelago/SkyWeatherPresentation/ThermalSeeds; FlightTelemetry/TelemetryWriter/TelemetrySample/FlightEffort; ExpeditionDirector/FlightChallenge/SkyForaging; selected controller/driver/camera source; shader source; WorldStreaming/OriginSafety/FlightChallenge tests; the artifact paths above.

Commands: `git status --short`; targeted `rg --files`, `rg -n`, `cat`, `sed`, `nl`; read-only artifact inventory; local image viewing; official Unity/Meta documentation lookup. No code/assets/settings changed, no existing raw captures deleted, no Unity/Blender automation performed by this critic, and no device or build validation claimed.

Suggested next agent prompt: “Implement T01's repeatable performance/evidence route and T11's lifetime measurements, preserving the current advanced-neutral controls. First inspect existing profiling/review helpers, capture a baseline and identify the actual worst operation. Make only trace-supported fixes from T03. Keep all changes local, save evidence with source/build identity, and distinguish automated, device and wearer results.”
