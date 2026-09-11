# VoarVR visual critique and art production brief

Reviewed 2026-09-10. Independent visual/art critic; documentation only. This report informs future implementation. It does not authorize changing controls, installing packages, acquiring assets or overwriting authored Blender files.

## Verdict

**Visual presentation: 5/10, provisionally.** VoarVR has the beginnings of a pleasing jade, ivory and warm-gold world, readable bird silhouettes and an unusually promising relationship between physical wing movement and visible feather motion. The scenery currently communicates a competent flight prototype more strongly than a place worth repeatedly visiting. The principal shortfall is authored composition and meaningful variation, followed by material/lighting depth and close-range creature finish. Adding undirected detail or more randomly distributed objects will not resolve those problems.

The highest-value art investment is **one memorable, navigable, beautiful flight circuit**: a recognisable home perch, an enticing ascent, a distinct landmark to play around, and a restful destination that visibly changes after the journey. Its assets should teach flight, support tricks and make progress tangible. Long-term exercise motivation needs those rewards to remain readable while the player is moving and becoming tired.

This is an assessment of supplied images and source, not a claim to have worn the headset or personally played the game. Actual visual comfort, binocular scale, display readability, sustained frame rate and enjoyment remain ungraded.

## Evidence and boundaries

Images were inspected before reading any other critic's report. Required agent documentation was read for context; its historical aggregate scores were not used to calculate these scores. No existing individual critic reports were needed for this assessment.

| Evidence | What it establishes | Limit |
| --- | --- | --- |
| [Sky Garden round 03](../../artifacts/reviews/sky-garden-comfort/round-03/): `lowland-departure.png`, `cloud-ascent.png`, `island-approach.png`, `split-arch.png`, `garden-terrace.png`, `rain-front.png`, `moth-closeup.png` | Current environment family, approaches, closer scenery, weather representation and collectible appearance | Editor captures, not stereo headset output; associated build is `2d063c80…`, preceding quiet-cockpit and advanced-neutral changes |
| [Quiet cockpit round 01](../../artifacts/reviews/quiet-cockpit/round-01/): `quiet-eyes.png`, `hud-enabled.png`, `inverted-eyes.png` | Uncluttered default view, optional HUD composition, body-following inverted view | Associated build `2cab1e99…`; no body-in-view pose sweep or wearer comfort evidence |
| [Magpie round 03](../../artifacts/reviews/magpie-telemetry/round-03/): spread, fold-side and upstroke-top images | Feather fan, body masses, folded silhouette and material treatment | Older character capture; background is superseded and is not scored as the current environment |
| [Dragon round 03 chase](../../artifacts/reviews/dragon-flight-v2/round-03/chase.png) and [Duck round 03 wing](../../artifacts/reviews/duck-flight-v1/round-03/Glide-left-wing.png) | Membrane versus feather silhouette and older close-range character surface treatment | Historical captures; not evidence of current first-person camera placement or current world |
| [Sky Garden world observations](../../artifacts/reviews/sky-garden-comfort/round-03/world.txt) | Samples contain 25 chunks, 9 islands and 511,813–551,527 reported chunk vertices | These are not visible triangles, GPU measurements or a whole-scene render budget |
| Current first-party shaders, Blender authoring scripts and world/presentation code | Actual palette, module ownership, layout rules, visual LOD switch and effects implementation | A source inspection cannot prove successful rendering on the installed APK |

The later advanced-neutral build record reports APK `e30314ba…` and source stamp `d2bd0906…`. This review did not obtain fresh images from that build. The narrow pitch-equilibrium changes do not provide grounds to improve or reduce the visual scores. The reported accumulating magenta bars are **a wearer-reported issue with a subsequently implemented fix awaiting wearer confirmation**, not an artifact demonstrated by the inspected screenshots.

The images live under ignored `artifacts/`. Future agents must check their availability; recapture missing evidence instead of claiming to have reviewed it. Preserve a small approved contact sheet outside ignored output if this brief must travel independently of this workspace.

## Independent scorecard

These are critical judgments, not measurements or an asserted market ranking. On this rubric, 5 means functional prototype presentation with conspicuous limitations; 8 means cohesive, memorable, consistently polished presentation in the assessed scope. Do not average unverified dimensions into the overall score.

| Dimension | Score | Rationale |
| --- | --- | --- |
| Art-direction coherence | 6 | Jade/ivory architecture, coral insects and blue atmosphere belong together. Rounded creature pieces, faceted terrain and opaque cloud clumps still have visibly different degrees of finish. `ART_DIRECTION.md` remains an M0 undecided brief. |
| Composition and landmark hierarchy | 4 | The close garden has a legible path and tower. Wider travel repeats similarly pointed islands against a largely empty blue field, with few distinct destination silhouettes. |
| Shape and material quality | 5 | Shapes identify their categories. Stone, roofs, paths, foliage and insects mostly differ through colour and coarse normal shading; close views expose box/sphere construction. |
| Light, depth and atmosphere | 5 | Haze establishes distance and the sky is coherent. Foreground and middle-distance values often compress together; opaque cloud undersides read as solid sculptures. Contact and sheltered-space shading are weak. |
| Creature silhouette and species identity | 6 | Duck, long-tailed Magpie and membrane-winged Dragon are distinct. Magpie's individually articulated feather fan is a useful foundation. Primitive coverts/body intersections and limited surface response reduce believability. Older character evidence makes this provisional. |
| Visual embodiment | 5, provisional | Quiet cockpit and authored eye anchors are valuable. Older wing detail and feather articulation support bodily identity, but the supplied current forward-facing images do not show whether hands feel continuously like wings throughout natural looking/reaching/folding. |
| Navigation and interaction affordances | 5 | Ivory approach path, an actual arch and gold lift cues provide a language. Repeated silhouettes, sparse thin air cues and a colour family shared by food, lift and landing decorations make its hierarchy underdeveloped. |
| Visual storytelling and environmental life | 3 | Buildings and ruins imply a setting, but the reviewed route has little visible evidence of inhabitants, specific history, changing purpose or consequences. Moths provide an initial moving life layer. |
| Presentation of optional information | 6 | Hidden HUD restores a clear view. Enabled panels are restrained but still numerous, text-heavy and spatially scattered; in-headset legibility is unverified. |
| Visual distinctiveness / ambition | 5 | Being a responsive bird among airborne gardens is appealing. The current execution is familiar prototype fantasy imagery. Technical novelty or being “cutting edge” cannot be established by these screenshots. |
| Stereo quality, wearer comfort, motion stability, sustained Quest performance | Unverified | No personal headset review, matched latest stereo capture or sustained render profiling was performed. |

## Findings

| ID | Severity and classification | Evidence and player consequence | Specific response |
| --- | --- | --- | --- |
| V01 | **High — subjective preference**, relative to the requested lasting appeal | `cloud-ascent`/`island-approach`/`rain-front`: similar pointed island bodies and repeated clumped clouds dominate multiple destinations. `SkyArchipelago.BuildIsland` uses three composites sharing arch, tower, ruin, bridge and tree placement logic. The next island does not promise a sufficiently different experience. | Author three distinct landmark compositions for one route; use unique primary silhouettes, approach angles and gameplay spaces before increasing world size. |
| V02 | **High — unverified risk** | `SkyArchipelago.Tick` swaps the full garden for the base island at 520 m. The far mesh omits the separately combined tower, arch and trees. Identity cues may appear late or pop on approach. | Preserve each hero landmark in a simplified distant silhouette. Capture continuous approaches across the boundary in both eyes before choosing thresholds. |
| V03 | **Medium — subjective preference** | `garden-terrace`/`split-arch`: architecture has broad flat slabs, block windows and little grounding. `SkywardVertex.shader` uses fixed directional/sky colouring and haze; reviewed world renderers disable shadow casting. | Improve major bevels, roof overhangs, underside values and baked vertex cavity/contact colour. Evaluate one inexpensive bird/contact cue. Profile before adding dynamic shadows. |
| V04 | **Medium — subjective preference** | `lowland-departure` and quiet cockpit: nearby clouds have dark, hard, overlapping lobes; the same four-puff mesh is reused in 16 slots. | Author a small cloud silhouette family with flatter bases, distinct edges and clear traversable gaps. Design cloud tops, side banks and wisps as different roles. Keep the first pass opaque and bounded. |
| V05 | **Medium — objective presentation limitation** | `rain-front` is conveyed by a darker sky and a handful of thin dark streaks. Source uses 12 narrow lines around the camera and a binary region tint. It does not visually express a broad, approaching weather front. | Add a distant front silhouette and transition band; give the boundary a readable approach/exit. Test bright and dark backgrounds for streak visibility. Match changes to the real weather region. |
| V06 | **Medium — unverified readability risk** | Gold is used for thermal seeds, edible moths and landing-garden decorations. At distance, moths collapse into small gold flecks. Colour cannot fully distinguish these actions. | Retain palette but assign shape/motion semantics: upward coherent streams for lift, fluttering paired wings for food, stationary segmented arcs for landing. Validate with HUD hidden and colour reduced. |
| V07 | **Medium — subjective preference** | Older Duck wing close-up and Magpie spread/upstroke show inflated oval coverts and visibly stacked primitive masses. Fine feather articulation is more developed than the surrounding silhouette. | Polish one species first: coherent shoulder/forearm transitions, layered feather groups, clean folded outlines, small eye/bill details and restrained sheen. Reuse validated bones and proportions. |
| V08 | **Medium — unverified risk** | `DuckVertex.shader` and `SkywardVertex.shader` handle vertex colour differently: the latter explicitly applies `SRGBToLinear`, the former does not. Magpie borrows Duck's body material. | Investigate authoring/import colour spaces with known colour patches in the actual Unity project and target build; standardise a documented pipeline only after comparison. This is not proof that either shader is wrong. |
| V09 | **Medium — unverified embodiment risk** | Current quiet first-person capture shows no visible body in the forward frame; older character captures cannot establish natural current peripheral wing visibility. | Capture all species looking naturally left/right/down, extending, relaxing and folding. Fix clipping/vanishing/shoulder detachment if reproduced; do not force a beak into the central view simply to make a screenshot look embodied. |
| V10 | **Medium — subjective preference** | Garden architecture and moths provide decoration but no distinct inhabitant, ownership or visible consequence. The ground pose is procedural and the world has little authored creature behaviour. | Give the home garden a resident, a nest/perch and a small persistent restoration change tied to one mission. Build recognition and anticipation through repeated visual response. |
| V11 | **Low — subjective preference** | `hud-enabled`: five upper instrument cards and lower air/food cards compete for attention. Quiet mode has improved this substantially. | Consolidate priority feedback, give optional momentary confirmation through sound/wing/world cues, and offer a calm perch view for detail. Preserve the user's ability to hide all ongoing flight text. |

There is no screenshot-proven blocker in this review. V01 is an art priority, not a crash. V02 and the other unverified risks need reproduction and measurements before implementation decisions are treated as defects.

## Proposed art direction

Working direction for approval through one finished slice: **a handcrafted sky sanctuary, seen from a bird's body**. Soft atmospheric distance; crisp nearby silhouettes; tactile, gently weathered ivory stone; deep jade foliage; restrained coral life and warm golden wind. Let the atmosphere be spacious while destinations contain layered, discoverable detail.

Use three scales of visual information. At far range, a landmark's silhouette and light/value grouping answer “where am I going?” At medium range, paths, openings, trees and air movement answer “how do I approach?” At wing/landing range, feet, feather layers, flowers, nesting materials and worn ledges reward being small and physically present. Detail belongs where players can perceive it at their speed.

Proposed motifs are decisions to test, not established story canon. An old wind garden could have sheltered migrating creatures, with the player reopening routes between its refuges. Evidence of this story should appear in landing wear, nest repairs, drifting seeds, residents returning and changed garden colour. A new name on a card alone does not create a new location.

## Ordered production backlog

Each ticket should be a bounded agent assignment with before/after evidence, explicit asset ownership and a handoff. Implement only the top three priorities in the first revision round, following the repository's independent critique loop. The rest is a staged backlog, not a request to build everything concurrently.

### A01 — Establish an actual visual baseline and one art target

**Priority:** next; small documentation/capture task. **Dependencies:** none. **Owners:** `design/ART_DIRECTION.md`, `docs/ASSET_GUIDELINES.md`, `Game/Editor/FlightGameReview.cs`, `Game/Editor/MagpieReview.cs`.

Replace the stale M0 art brief with an explicit proposed style sheet: palette roles, hero silhouette sheet, close/mid/far detail hierarchy, creature/material examples and a single route composition. Capture latest installed-build provenance and repeatable normal flight, approach, landing and wing views. Record stereo output separately from editor images.

**Accept when:** all review images identify build/source, species, mode, camera and location; missing hardware evidence is labelled; the style target specifies which assets are retained/refined/replaced; there is one reproducible circuit for comparison. Do not invent a fixed geometry budget from desktop counts.

### A02 — Make one route memorable through composition

**Priority:** first major art task; medium. **Dependencies:** A01. **Owners:** `blender/scripts/create_skyward_kit.py`, `World/SkyArchipelago.cs`, `World/FlightRegions.cs`, `Editor/SkywardSetup.cs`, authored source/curated FBX through the documented pipeline.

Compose a departure garden with a broad sheltering tree, a middle landmark with a conspicuous split opening and circling space, and a destination with a distinct stepped tower or crescent rim. Retain generous flight corridors, visible touchdown surfaces and quiet perches. Place rest locations where the player naturally enjoys a vista after an effort segment. Make the landmark's far mesh preserve its unique outline.

**Minimum new art:** one signature tree form, one specific rest-perch/nest module, one distinct landmark crown; reuse the existing arch, bridge, buildings and vegetation where they serve the composition. Rotate/scale substitutions alone do not count as authored variety.

**Accept when:** a small wearer test can name and distinguish all three places without map labels; each route has an obvious spacious line plus an optional trick line; continuous approach reveals no severe silhouette pop; required openings are inspectable from the approach; collision and logical identity survive traversal/rebase; no new mandatory rapid rotation. Record actual navigation results rather than awarding an 8 solely from a beauty shot.

### A03 — Give air, food and landing distinct visual grammar

**Priority:** first major readability task; medium. **Dependencies:** A01; coordinate with A02. **Owners:** `World/WindVisualizer.cs`, `World/ThermalSeeds.cs`, `World/SkyWeatherPresentation.cs`, `Gameplay/SkyForaging.cs`, `World/LandingGuide.cs`, `Art/Materials/WindRibbon.shader`.

Treat lift as a volume a player can recognise from outside, enter and circle: sparse coherent upward motion, an identifiable lower origin, optional directional audio and a readable exit. Keep geometry tied to the same sampled physics. Give moths a distinctive paired-wing movement and gather them into purposeful routes rather than a universally similar field. Landing cues should belong to a surface and remain still. Avoid large opaque symbols in the flight centre.

**Accept when:** wearers with HUD hidden can explain each cue and choose the requested action on a short capture; repeat against sky/cloud/foliage and with colour-reduced reference images; no persistent UI is required for basic identification; cues survive typical viewing distances and headset motion; GPU cost is measured. A successful art pass must not invent lift that physics does not supply.

### A04 — Ground the world with modest shading and material work

**Priority:** next; medium. **Dependencies:** A02. **Owners:** `Art/Materials/{SkywardVertex,DuckVertex,SkyGardenSky}.shader`, Blender colour authoring, `World/WorldChunk.cs`, `World/SkyArchipelago.cs`.

Create an asset/material test corner with stone, bark, leaf, roof and bird under the same light. Establish correct colour handling. Add authored cavity/underside colour and edge highlights where they communicate construction. Refine a few silhouettes and canopy layers. Try a bounded ground-contact treatment that tracks actual landing height; retain it only if it improves contact perception and fits measured cost. Rebalance haze by layer so near landmarks remain distinct while distant terrain recedes.

**Accept when:** compared normal gameplay views show clear material grouping and better depth without loss of cue contrast; no colour-space regression in birds; no shimmering thin detail; headset approach/landing inspection passes; measured CPU/GPU frame-time deltas and memory/batch changes are recorded. Transparent foliage, postprocessing and realtime shadows are optional techniques, not acceptance requirements.

### A05 — Polish one creature as the embodiment benchmark

**Priority:** after navigation/readability; medium-to-large. **Dependencies:** current controls stable enough for matched captures. **Owners:** `blender/scripts/create_magpie.py` or `create_duck.py` for the selected species; `Flight/{AvianWingPresentation,BirdRigDriver,BirdGroundPresentation}.cs`; `Editor/CharacterEyeSetup.cs`; current validated export pipeline.

Choose a single flagship species, then finish its outline, feather layering, shoulder continuity, underside, eye/bill, folded rest pose and landing preparation. Keep calibrated hand mapping and physics proportions intact. Add low-amplitude breathing/preening only at rest and only where it does not override tracked motion. For Dragon, later use membrane tension, finger silhouettes and coordinated tail motion to express its different body rather than borrowing feather behaviour.

**Accept when:** matched first/third-person captures cover spread, comfortable partial reach, full fold, twist, braking, tracking loss, landing, walking and natural downward/sideward looks; no detached-looking wing roots or new occlusion; no new required wrist posture; rig/export tests pass; wearer describes controller-to-wing ownership positively. Keep per-species limitations visible until each is reviewed.

### A06 — Turn the garden into a place with visible consequences

**Priority:** content milestone; medium. **Dependencies:** A02 plus a gameplay-approved mission/reward contract. **Owners:** `Gameplay/ExpeditionDirector.cs`, `Gameplay/FlightChallenge.cs`, `World/SkyArchipelago.cs`, new bounded presentation components, authored environment/creature assets.

Build a small visual state sequence: empty nesting place → delivered materials → occupied refuge → renewed route marker. Add one resident with a few readable behaviours (notice arrival, settle, depart along the route). Use distinct garden objects to commemorate skill accomplishments. Put the detailed journal/trophy display at a perch so reading is voluntary and does not interrupt flight. Treat species ecology and the meaning of “eating” collectibles as an explicit narrative choice.

**Accept when:** a returning player recognises their prior consequence without reading a popup; state survives a session restart and region streaming; progression does not vanish on a break; idle ecology has a documented bounded update/render cost; resident behaviour never supplies misleading collision/landing affordances. Story work must alter the place or relationship, not only add text.

### A07 — Build trick playgrounds that look inviting from the easy route

**Priority:** skill/replay milestone; medium. **Dependencies:** A02, reliable trick recognition and wearer-tested controls. **Owners:** `World/SkyArchipelago.cs`, `World/FlightRegions.cs`, `Gameplay/FlightChallenge.cs`, `Flight/AcrobaticFlight.cs` only if a separately justified detection change is needed.

Use art/layout for three types of optional play: a wide slalom between asymmetric pillars, a banked orbit around a landmark, and a roomy vertical opening for players who deliberately select advanced rotation. Show the entry, clearance, recovery space and next safe perch. Give successful routes a brief recognisable sound and restrained world response. Keep ordinary travel rewarding without demanding acrobatics.

**Accept when:** first-time wearers can identify the easy path; expert captures demonstrate actual trajectory completion and safe virtual recovery space; scoring cannot be farmed by stationary camera rotation; opening clearances account for each species' visible wings; rewards persist through pauses. Difficulty belongs in route shape/precision, not mandatory neck strain or visually obscured hazards.

### A08 — Extend the finished vocabulary to a second biome

**Priority:** later; large. **Dependencies:** first route passes wearers' navigation, appeal and sustained-device gates. **Owners:** existing world-region/streaming pipeline and authored asset scripts.

Use a contrasting shape language and activity: sheltered rain-fed canopy with broad leaves, hanging nests and slower precision routes, or sunlit ridges with long sightlines and sustained gliding. Carry shared palette/cue semantics across it. Produce unique hero assets plus a modest set of useful supporting modules; avoid a library of props without playable placement.

**Accept when:** the second area is recognisable from silhouette, sound and route structure; it supports a distinct flight task and recovery rhythm; persistent world/story state remains stable; captured device cost stays within measured headroom. An endless repetition of a larger kit is not proof of long-term variety.

## Production controls and validation

- Preserve `.meta` GUIDs and the validated rig hierarchy. Author scene/prefab changes through Unity Editor APIs. Transfer repeatable visual operations into explicit editor/Blender scripts; never run them on every import.
- Keep visual support and aerodynamic truth connected. New leaves, wind wisps or weather walls must not advertise lift, collision or an objective where none exists.
- Review normal-speed motion at the actual player's viewpoint. Include ascent, descent, low-level passing, quiet rest, arch approach, looking at wings, rain entry and region/LOD transitions. An external fly-through can hide the problems that matter in the headset.
- Separate visual polish from performance evidence. Record Quest CPU/GPU timing, batches, visible geometry, memory and thermal behaviour for the representative circuit. A fixed new polygon target without measurements would be arbitrary.
- Use the documented builder → independent visual/player/technical critics → synthesis loop, at most three rounds for each substantial milestone. Keep confirmed defects, subjective preferences and unverified risks distinct. Do not raise scores by repeatedly reviewing unchanged images.
- Maintain restful viewpoints and optional detail inspection as part of the art brief. Reward coming back to a loved place and improving a skill; do not make visual rewards depend on continuing after pain, dizziness or loss of tracking.

## Source owners inspected

Required `docs/agent/{index,current-state,repo-map,open-questions,quality-loop}.md`; `design/{ART_DIRECTION,GAME,FLIGHT_GAME_V1}.md`; `docs/ASSET_GUIDELINES.md`; current environment/character asset inventory; full `blender/scripts/create_skyward_kit.py`; targeted creature authoring, ground/avian presentation and eye setup reads; `Art/Materials/{SkywardVertex,SkyGardenSky,DuckVertex}.shader`; `World/{SkyArchipelago,SkyWeatherPresentation,SkywardKit,WorldChunk}.cs`; `Gameplay/SkyForaging.cs`; `UI/FlightCard.cs`; supplied image/build/world evidence described above. Paths beginning `Game/` or domain folders in this report refer to `unity/Assets/Game/`; `Art/` refers to `unity/Assets/Art/`.

No runtime or authored asset was edited. No Unity compilation, new playtest, performance benchmark or hardware visual acceptance was claimed.
