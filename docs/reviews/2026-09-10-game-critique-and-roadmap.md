# VoarVR: professional critique and next production roadmap

Date: 2026-09-10. **Status: recommendations for future implementation; no gameplay changes in this review.**

Implementation follow-up: [A Route Home milestone and remaining acceptance](2026-09-10-route-home-implementation.md). The original findings/grades below describe the reviewed baseline; consult that follow-up before assigning an already implemented work item.

Brief: make embodied bird/dragon flight beautiful, expressive and rewarding enough to sustain enjoyable physical activity over many sessions. Review the actual game, recorded playtests and first-party repository, then provide work that future agents can implement and verify.

Reading guide: [grades](#3-grading-the-current-game), [story treatment](#6-proposed-creative-direction-the-returning-sky), [session design](#7-a-session-worth-repeating), [graphics/assets/audio](#9-graphics-assets-embodiment-and-sound), [24 work orders](#11-work-orders-for-future-agents), [playtest gates](#12-playtest-protocol-and-acceptance-gates), [agent brief](#13-ready-to-use-agent-brief).

## 1. Editorial verdict

**VoarVR's strongest asset is the relationship between your arms, an articulated creature and a moving atmosphere. Its largest product weakness is the scarcity of meaningful reasons to use that relationship.** There is a promising sensation to develop: earn height, feel the wings settle, read the air, choose a line, then land somewhere worth reaching. Recent Quest evidence shows useful progress. The next investment should turn that sensation into an authored adventure with recognizable places, achievable mastery and persistent consequences.

The current content is a flight prototype with a small playable activity layer. It has three differentiated flyers, optional acrobatics, thermals, actual landings, short expeditions and moth catches. It does not yet provide the narrative, encounter variety, authored pacing or persistence needed to substantiate long-term replayability. Bigger procedural terrain alone will not supply those qualities.

My recommended creative direction is **a living sky sanctuary restored through flight**. Build one excellent journey from a lowland roost, through useful rising air, to a garden above the clouds. The player returns with a meaningful discovery; the garden visibly changes; a new route becomes interesting. Short movement bursts, quiet soaring and genuine arms-down perches should all have a purpose. Optional trick lines give enthusiasts a high skill ceiling while story completion remains available in comfortable flight.

The most important immediate changes are:

1. Confirm the latest handling and visual fixes on the installed build; preserve the improvements already made.
2. Separate soaring efficiency, expressive flying and voluntary movement goals. The current expedition score penalizes active flapping and time spent beyond ten minutes.
3. Make one route understandable with the HUD hidden, and make its destination emotionally and visually worthwhile.
4. Add interruption-safe progress and satisfying places to rest before asking players to invest in longer journeys.
5. Spend the next art budget on landmark composition, close creature presence, atmospheric depth and sound, then measure the result on Quest.

## 2. Evidence and confidence

This is a **repository and recorded-playtest critique**, not a claim that the reviewers wore the headset. Sources include direct code inspection, actual saved Quest telemetry, current and historical editor captures, test/build reports, and wearer feedback recorded in handoffs. No fresh human playtest, gameplay video with narrated intent, listening test, or physiological measurement was available in this review.

The inspected worktree starts at `54562d17e0e566c0f712a7d1763c1a08bd50e057` with the user's uncommitted advanced-neutral fix and tests already present. Treat that patch as current source. The build/install handoff records APK `e30314ba…`, embedded source `d2bd0906…`, installed and launched; wearer acceptance of that neutral fix remains pending. Earlier captures must not be represented as recordings of it.

| Evidence | What it establishes | What it does not establish |
| --- | --- | --- |
| [Initial Quest diagnosis](../development/quest-flight-game-playtest-2026-09-10.md), Dragon 351.66 s and Magpie 707.37 s, source `ea5906a9…` | Historically excessive advanced response; a Magpie thermal interval lost 16.5 m; progress could appear complete while an altitude gate blocked it | That these failures remain unchanged in current source |
| [Sky-garden validation](../development/sky-garden-comfort-validation.md) and latest world captures under `artifacts/reviews/sky-garden-comfort/round-03/` | Subsequent handling/objective work, current kit composition, controlled replay and desktop checks | Human pacing, stereo readability, sustained current-build device performance |
| [Quiet-cockpit handoff](../agent/session-handoffs/2026-09-10-quiet-cockpit.md) and `artifacts/reviews/quiet-cockpit/round-01/` | HUD-off presentation, authored-eye full-rotation advanced first-person, targeted UI lifecycle changes | Proven cause or elimination of reported magenta bars; worn comfort of inversion |
| Latest saved Duck `caf1f4dc…` 518.72 s and Magpie `a7484ed4…` 352.12 s, source `b8bb65ee…` | Both clean with zero dropped telemetry records; both complete Training; Magpie records a BackLoop award | Completion of Skyward/Ridge by a wearer, deliberate successful trick technique, post-neutral handling acceptance |
| [Neutral-fix handoff](../agent/session-handoffs/2026-09-10-advanced-neutral.md) and current `AcrobaticFlight.cs` | Evidence-based correction to passive pitch at calibrated neutral; historical full EditMode suite reports 190 passing tests | A new human comfort or relaxed-grip result |
| Current Unity MCP read-only inspection | Correct project `/Users/kian/code/voarvr/unity`, Unity 6000.6.0f1, Android target, BirdFlight scene idle in Edit mode | A new Unity compilation, rendered playthrough or headset test |

The independent player reviewer decoded the latest raw files: Duck Training completion at session 119.78 s, score 554; Magpie Training completion at 21.52 s, score 988; Magpie BackLoop event at 161.99 s. This is welcome evidence of usable progression. These are session-relative event times, not controlled first-time-user completion benchmarks. Both recordings precede the new neutral patch. The earlier blanket description “stuck in soaring” is therefore obsolete for these latest Training sessions.

The neutral analysis is also specific: a quiet Magpie interval held approximately +30.23° bilateral calibrated wrist input while control and passive pitch moments nearly cancelled. Earlier compensation changed sign. Current code addresses that equilibrium rather than applying an arbitrary fixed wrist offset. Repeatedly retuning all flight controls would discard valuable work.

Local images and raw recordings remain in ignored `artifacts/`; a fresh clone may not contain them. The durable findings here and in the three companion reports summarize their relevant evidence. Obtain fresh captures before implementing visual changes. Historical test counts are reported evidence, not tests rerun by this review.

### Repository coverage

This was a cross-repository first-party audit, using a file inventory and targeted reads rather than treating every vendor or generated byte as gameplay evidence.

| Domain | Main inspected owners | Review focus |
| --- | --- | --- |
| Vision and operating constraints | `design/`, `docs/ROADMAP.md`, architecture, asset guidelines, agent wiki/handoffs | Intended experience versus current behavior; production constraints |
| Input and embodiment | `Game/Input/`, `BirdFlightController`, `BirdFlightDriver`, `BirdRigDriver`, `WingRigSolver`, `FlightCamera`, character definitions | Calibration, agency, physical effort, views, species differences |
| Gameplay and rewards | `FlightChallenge`, `ExpeditionDirector`, `ForagingScore`, `SkyForaging`, `TrickDetector`, `CharacterSelectController` | Goal clarity, score incentives, progression, content ceiling |
| World and presentation | `Game/World/`, first-party shaders, `FlightFeedback`, HUD/cards, scene/asset wiring | Air readability, composition, travel, interaction and sound |
| Art production | Blender generation/validation/export scripts, curated models and resource catalogs | Silhouette, articulation, reusable assets and export constraints |
| Engineering evidence | EditMode/PlayMode tests, telemetry capture/reader, Quest/Unity tools, checked-in render/XR settings | Regressions, persistence, long-session risks and performance evidence |

Unity caches, vendored packages and raw binary assets were not exhaustively reverse engineered. No undocumented narrative, NPC system or fitness validation is assumed to exist. `design/GAME.md`, `design/ART_DIRECTION.md`, and portions of architecture/roadmap still contain foundation-era descriptions; use current source and dated handoffs for implementation state.

## 3. Grading the current game

Scale: **1–2** absent or barely established, **3–4** thin prototype, **5–6** promising playable foundation, **7–8** polished and convincing in reviewed scope, **9–10** exceptional execution supported by relevant player evidence. These are editorial judgments, not scientific measurements. Scores assess what exists today, not future potential or development effort.

| Dimension | Current grade | Reason and limit |
| --- | --- | --- |
| Core loop design / potential for moment-to-moment fun | **5/10** | Flap, soar, bank, dive, catch and land form a useful verb set. Too few interesting decisions connect them. Felt fun is unverified. |
| Graphics and art direction | **5/10** | Coherent jade/ivory sky-garden palette and original creatures; repetitive island/cloud silhouettes, flat close surfaces and weak visual hierarchy still read as prototype art. Desktop captures only. |
| Embodiment affordances | **6/10** | Calibrated wings, articulated Magpie, authored eye anchors and real bank/loop dynamics provide meaningful foundations. Peripheral self-presence, contact and tracking behavior need worn evaluation. |
| Comfort and ergonomic success | **Unverified** | Wearer feedback drove real improvements; neutral and full-rotation first-person acceptance remain open. No numerical comfort grade can be justified. |
| Skill expression / tricks | **6/10** | Actual trajectory loops, rolls and inverted holds are more substantial than an animation button. Discovery, coaching, scoring feedback and a varied mastery ladder are underdeveloped. |
| World exploration and level design | **4/10** | Vertical regions and real lift support journeys. Repeated procedural arrangements and sparse destination-specific interactions weaken discovery. |
| Story and emotional investment | **2/10** | Expedition titles and destinations supply a premise; there is no developed cast, dramatic progression or visible persistent consequence in current gameplay. |
| Mission variety | **4/10** | Discover lift, soar, traverse an arch, migrate and land; Ridge largely recombines existing verbs. Moth collection adds another activity, but little encounter authorship. |
| Progression and replayability | **3/10** | Local bests, medals and one route unlock exist. No lasting sanctuary, collection journal, mission checkpoint or substantial route library. |
| Support for enjoyable exercise | **4/10** | Physical flapping and movement telemetry exist. The main score rewards less flapping; arm stillness is not arms-down recovery; no playtested session structure yet. Exercise effectiveness is unverified. |
| Audio and haptic design breadth | **4/10** | Air, wingbeat, contact, lift and catch cues exist. Procedural clips and limited sound identities cannot yet supply a living world. Actual listening quality is ungraded. |
| Distinctiveness / innovation | **6/10** | Creature embodiment interacting with readable air and effort has a credible identity. There is no evidence for a market-leading or biologically exact simulation claim. |
| Engineering foundation | **7/10** | Pure simulation interfaces, regression tests, bounded world systems and versioned telemetry make iteration tractable. Product persistence and measured content budgets need work. |
| Standalone long-session delivery | **Unverified** | Short recorded timing and successful builds are useful evidence; sustained representative Quest performance, thermal behavior and comfort remain gates. |

**Overall editorial assessment: approximately 4–5/10 for the present content and design package.** This is a qualitative synthesis, not an average that hides unverified comfort/performance. It describes an early game with a stronger flight foundation than narrative or replay structure. A credible next milestone is a polished, replayable small journey; there is no reason to promise a 9/10 game by adding more objects.

Independent assessments, with their own evidence and scopes: [visual critique](2026-09-10-visual-critique.md), [player critique](2026-09-10-player-critique.md), [technical critique](2026-09-10-technical-critique.md). Their component scores should not be averaged mechanically; a clear quiet cockpit and a visually repetitive landscape can both be true.

## 4. What players in this genre need

The design opportunity sits between embodied flight adventure, movement skill game and light exergame. The following are **audience hypotheses to test**, not claimed market research:

| Player motivation | Desired experience | Design response |
| --- | --- | --- |
| “Let me be a bird” | Agency, convincing wings, bird-scale places, readable air | Prioritize sensorimotor congruence: visible wing response and motion should agree with input; develop embodied contact and a quiet view |
| “Show me somewhere wonderful” | Discovery, scale, changing vistas, memorable destinations | Landmark-based navigation, anticipation/reveal sequences, a few authored routes |
| “Let me get good” | Learnable cause/effect, repeatable challenges, useful feedback | A low skill floor and high skill ceiling; maneuver coaching, personal ghosts, fair comparable records |
| “I want to move without staring at a workout timer” | Playful goals, varied demand, natural recovery and visible achievement | Route-based movement bursts interspersed with soaring and actual rest; separate movement recognition from efficiency medals |
| “Give me a reason to come back” | Personal history, meaningful progress, optional novelty | Sanctuary restoration, discoveries, route variants and a mastery journal |
| “Respect my body and time” | Clear options, easy help, short sessions that count, no lost effort | Accessible calibration/settings, save-at-perch, immediate pause, voluntary extensions and no penalty for stopping |

Useful terminology: **presence** is the sense of being in the world; **body ownership** is accepting the creature as your body; **agency** is believing your actions caused its actions. **Optic flow** is visual motion that conveys travel and can also contribute to discomfort. **Affordance** is a cue that suggests an action. **Diegetic feedback** belongs to the world, such as leaves showing lift. **Flow** requires understandable goals, responsive feedback and a suitable challenge; it does not mean maximum intensity. **Horizontal progression** expands choices or expression without making old birds numerically obsolete.

### Reference lessons, researched 2026-09-10

Ubisoft describes *Eagle Flight* through free flight, a chaptered story, landmark activities and competitive play. The transferable lesson is to build territory and memorable routes around flight. Its older platform/features are historical references, not evidence of current service availability or a template to copy wholesale. [Ubisoft developer interview](https://news.ubisoft.com/en-us/article/1dzrc9I0i3FpspECqpc0dD/eagle-flight-everything-you-need-to-know).

*Synth Riders* combines expressive movement with repeatable activities; its March 2026 progression announcement explicitly adds visible personal performance and a clearer path for newcomers. My inference: VoarVR should make improvement visible after a short flight, while keeping open exploration available. This is a design analogy, not proof that a similar feature will retain VoarVR players. [Official progression announcement](https://synthridersvr.com/level-up/).

*Supernatural* documents calibration to individual reach and accessibility settings. The relevant lesson is that the movement challenge must fit the person. VoarVR should expose comfortable range and guidance preferences instead of inferring that every player wants the same reach, workload or feedback. No exercise-effectiveness or calorie claims are borrowed here. [Official calibration/accessibility FAQ](https://www.getsupernatural.com/faq?clean=true).

Meta recommends accessible, changeable locomotion preferences because artificial motion suits people differently. For this game, retain the user's requested full-rotation advanced view and add clearly named optional comfort choices; do not silently flatten advanced flight. Exact implementation requires this project's XR path, not wholesale adoption of another locomotion package. [Meta locomotion preferences](https://developers.meta.com/horizon/design/locomotion-user-preferences/).

## 5. Findings that should govern the next decisions

Classification: **D** = observed/source-confirmed defect; **G** = confirmed gap against this brief; **P** = editorial preference; **R** = unverified risk. Severity is relative to the requested next product, not an assertion that every prototype omission is a runtime bug.

| ID | Severity / type | Finding, evidence and player impact | Response |
| --- | --- | --- | --- |
| F01 | High / G | `FlightChallenge.Complete` subtracts two points per active-stroke second and duration beyond 600 s. `Step` advances elapsed time while perched. The central medal economy conflicts with movement and rest if used as the whole game's reward. | Preserve a soaring-efficiency category; add distinct movement/mastery goals and untimed adventure progress. See W02/W06. |
| F02 | High / R | The latest neutral correction has strong regression evidence but no post-fix wearer acceptance. Full-rotation advanced first-person is intentional and intense. | Verify the installed build before further control changes. W01/W04. |
| F03 | High / G | `ShowFlightText` gates coach, objective, destination and food text; HUD starts hidden. This respects the user's preference but leaves sparse alternative guidance. | Develop optional world/audio guidance and explicit paused help without returning clutter. W03/W08. |
| F04 | High / G | `FlightProgress` persists bests and an inferred Ridge unlock, not journey checkpoints or world change. Longer content would amplify lost progress. | Interruption-safe checkpoint and sanctuary state first. W05/W09. |
| F05 | High / G | Story consists of activity titles/instructions and a route. No meaningful character relationship or persistent outcome accompanies landing. | Give the first route one request, one discovery and one visible consequence. W09/W13. |
| F06 | High / G | Latest landscapes repeat inverted island cones, similarly shaped clouds and angular props. Distinctive places and lighting hierarchy are limited. | Art-direct one route and three destination silhouettes before expanding density. W07/W10. |
| F07 | Medium / G | Four trick awards exist, expedition bonuses cap at three; feedback shows cumulative trick score after a plus sign. Species/mode/weather are not separate best-score categories. | Clear per-event rewards, mastery ladder and comparable records. W14/W15/W21. |
| F08 | Medium / G | `SkyForaging` replenishes 24 moths around travel and uses a six-second combo window. Its best saves at `OnDestroy`. It supplies collection, not a developed ecology or purpose. | Author readable catch lines, separate habitats and checkpoint meaningful rewards. W05/W12/W17. |
| F09 | High / R | Camera obstruction is not resolved by current chase positioning; world collision uses a body sphere, not visible wing/foot contact. New narrow corridors could feel unfair. | Fix/test camera obstruction; keep route clearance generous before precision content. W11/W19. |
| F10 | Medium / G | Audio is procedural and mostly nonspatial through `FlightFeedback`; there is little habitat, creature or narrative sound identity. | A restrained audio hierarchy is a high-value presence investment. W16. |
| F11 | High / R | Chunk vertex counts are partial scene counts; bounded pooling and 72 Hz-looking recorded cadence do not prove sustained rendering quality. | Establish representative Quest measurements before richer content. W01/W18/W22. |
| F12 | Medium / G | Foundation-era design/wiki statements can contradict current objectives, camera behavior and device status. | Date the evidence, keep source ownership explicit and update handoffs with every accepted change. W24. |

Preserve what already works: quiet HUD default, deliberate calibration and in-place platform recenter, original Beginner trajectories, physical lift/trajectory tricks, automatic contact landings, free flight, distinct species and local ownership of recordings. High-priority work should not casually replace these contracts.

## 6. Proposed creative direction: The Returning Sky

This is a working story treatment, not existing lore or a committed title. Treat floating gardens and the Dragon as an intentional fable, with recognizable bird behavior anchoring it.

**Premise:** a seasonal storm has scattered the routes that connect a valley roost to the gardens above the clouds. The player helps a small migrating flock reopen those routes. A persistent sanctuary fills with plants, calls and visitors as discoveries return. The mystery is what draws the flock to the highest garden; the emotional payoff is a place that changes because of the player's journeys.

Use one recurring companion, a small magpie scout provisionally called **Pip**, built from the existing rig. Pip is expressive through pose, short calls and flight demonstrations. Avoid constant narration, dialogue wheels and a large cast in the first slice. A few written journal fragments at rest can supply optional depth. The player should understand the first mission even with all speech disabled.

| Chapter | Dramatic beat | Flight activity and choice | Persistent payoff | Production scope |
| --- | --- | --- | --- | --- |
| 1 — A Route Home | A quiet roost has lost contact with a garden. Pip shows a rising-air column. | Learn useful lift, choose an open scenic ascent or an optional catch detour, pass the split arch, land with a discovered seed | One dormant plant blooms; a companion settles; the next landmark becomes visible | **Build first:** existing departure/arch/terrace, one companion, one seed, three sanctuary states |
| 2 — Along the Rain | A small flock waits for a route around an unsettled front | Scout two readable alternatives: longer shelter or shorter gust exposure; lead the flock through generous waypoints | A new visitor and wind chimes; Ridge route and journal entry | After Chapter 1 playtest; reuse actual wind, bounded flock and route data |
| 3 — The First Migration | The restored garden becomes a departure point rather than an endpoint | Link familiar thermals, choose an optional mastery line, arrive at a shared aerial gathering | Garden celebration, final view and replay variants; free flight continues | Only after replay and Quest gates; no full ecosystem simulation required |

Every chapter needs an **anticipation → action → relief → consequence** rhythm. Show a distant destination early, conceal/reveal it deliberately, let the player accomplish something legible, then let them enjoy it with their arms down. A short line such as “That path will bring them home” earns meaning when birds actually use it later.

Keep the story primarily in actions and scenery. An abandoned perch becomes occupied; a plant changes silhouette; the same route sounds different after restoration. Persistent authored state provides meaning without a sprawling branching narrative. Storms supply environmental stakes. Predator combat, death loops and hunger depletion would change the game's tone and should wait for evidence that players want them.

## 7. A session worth repeating

Offer time intentions such as **short visit, journey, open flight**, with approximate durations refined by playtest. Candidate prototypes are 5, 12 and 20 minutes. These are content pacing hypotheses, not exercise prescriptions or promises of calorie expenditure.

| Approximate beat in a 12-minute prototype | Player experience | Movement / recovery opportunity |
| --- | --- | --- |
| 0–1 min | Start at an inviting roost; optional calibration refresher; choose one destination | Comfortable preparation and easy takeoff |
| 1–3 min | A short foraging line leads toward a visible rising-air feature | Brief active flying, no demand for maximal effort |
| 3–5 min | Follow leaf movement, settle into lift, see the destination emerge | Quiet soaring; explicitly not assumed to be physical rest |
| 5–6 min | Land at a sheltered overlook; Pip reveals the seed or route clue | Genuine arms-down rest with progress already saved |
| 6–9 min | Choose broad scenic arch or optional precision/trick line | Agency over challenge and exertion; same story access |
| 9–11 min | Reach the garden; complete a contextual delivery | Controlled approach, landing and visible world change |
| 11–12 min | Enjoy the sanctuary and a concise personal result | Stop satisfied, remain perched, or explicitly choose another short flight |

Prototype variable intervals rather than a mandatory rhythm-game cadence. A bird game benefits from irregular air and route choice. Movement intensity and technical difficulty should be separate settings: larger comfortable strokes should not automatically mean tighter obstacles, and opting for a gentler session should not lock away the story.

The desired retention pattern is **“I want to try that line again”**, supported by good rest opportunities. Do not make fatigue itself the obstacle to a reward. No progress decay, daily streak loss, escalating grind, surprise extra objectives, timer penalties during sanctuary rest, or automatic difficulty increases based on slowing hands. The player should leave with saved progress and something specific to anticipate next time.

Arm motion is the principal measured activity. Quiet arms held out can still be tiring; shoulder workload, cardiovascular benefit and physical recovery cannot be inferred from stroke counts. Ask for perceived effort and comfort separately from motion telemetry. Avoid turning the game into an unverified full-body fitness claim.

## 8. Missions and tricks that create real choices

Each new mission should change at least two of: route shape, information available, wind problem, precision demand, companion behavior, consequence. Renaming a waypoint is not enough variety.

| Activity | Concrete design | Why it may be fun | Implementation boundary |
| --- | --- | --- | --- |
| Thermal detective | Follow drifting leaves and a companion circle; find the useful core, then choose a departure heading | Reading an invisible system becomes a learnable discovery | Reuse real sampled wind; do not draw a false lift column |
| Orchard breakfast | Catch a short curved sequence of food across a clearing, then choose another line or perch | Flow and pursuit with a finite finish | Reuse swept catches; author heights/spacing with each bird; avoid dangerous physical reaching |
| Seed courier | Fly through an airborne seed, carry it visually near the breast, land to deliver | Immediate purpose and a visible garden reward | Abstract pickup; no need to hold a trigger throughout flight, add mass simulation or change hand ownership |
| Ribbon line | A broad slalom followed by one clean arch and a landing | Combines anticipation, precision and a satisfying finish | Generous clearance; sphere sweep success and visible geometry must agree |
| Flock guide | Stay within a wide moving corridor while companions follow a predictable route | Social presence and gentle responsibility | First version follows bounded waypoints; companions wait safely and never punish rest |
| Rain reader | Choose sheltered contour travel or exposed stronger lift | A meaningful risk/reward decision involving the existing atmosphere | Both routes viable in the selected mode; no hidden wind switch to force an outcome |
| Perch explorer | Discover three distinct roosts by landmark clues; each offers a different vista and sound | Low-pressure exploration, environmental storytelling and recovery | Stable IDs, checkpointed discoveries; no compulsory narrow branch balance |
| Sky dance | Link an optional roll, clean arc and recovery in a large open area | Expressive mastery with room for personal style | Opt-in advanced course; actual motion detection, no canned trajectory or mandatory inversion |

### The trick ladder

Begin with **carving and recovery**: steady bank, coordinated turn, clean arch and controlled landing. Progress to **soaring craft**: sustained useful thermal turn, choosing a departure line and converting height to a smooth traverse. Then **aerobatic craft**: a full roll, loop, inverted hold and clean recovery, all optional. This gives players achievements before they can tolerate or execute inversion.

Existing `FullRoll` is an orientation roll. Do not label it a barrel roll unless a helical trajectory is detected. Existing loop detection requires body rotation and path winding; retain that distinction. A future wingover, split-S or Immelmann needs its own entry, trajectory and recovery definition, not reuse of a rotation counter under a new name. Do not require these advanced maneuvers for story progression.

For each trick, show a short demonstration at rest, entry prerequisites in ordinary language, the player's last failed condition, and a retry location with adequate room. Thresholds for speed and clearance must come from species-specific fixtures and wearer trials; do not invent one universal safe altitude. Award **per-event points** clearly; `+750` should not mean “this event was worth 250, but your total is 750.”

A style run can later reward different maneuver families, clean exits and deliberate route use. Bank progress at safe checkpoints. Repetition should receive diminishing style credit within a judged run, while free-flight practice remains welcome. Do not reward near-collision farming or force long combo chains that punish breaks.

## 9. Graphics, assets, embodiment and sound

### Art direction

Choose **painterly natural forms with a restrained sky-fable palette**: grounded feather/rock/leaf structure, broad readable color masses, soft atmospheric depth and carefully chosen warm accents. Keep the existing jade/ivory/coral identity. Improve shape design and material separation before increasing polygon density.

The latest quiet first-person images succeed at removing clutter, but forward self-presence is sparse. Preserve the authored eye anchors; test optional peripheral shoulder/wing cues during small glances and natural strokes. Do not enlarge the wings in front of the face just to score better in a still image. A successful avatar looks coherent in motion, at the player's actual eye position, at partial folds and on the ground.

Build composition at **three scales**: region silhouette visible during travel; destination geometry that suggests approach and landing; close detail that rewards perching. Each major route should have a single dominant destination in the forward view, one supporting landmark and quiet space between them. Direction can be communicated through asymmetric shapes, occupied perches and foliage motion rather than floating text alone.

### First production asset list

Quantities below describe a bounded first slice, not a download shopping list. Create original assets through the checked-in Blender/FBX pipeline and establish target-device budgets before approving them.

| Asset set | Initial deliverable | Purpose / acceptance direction |
| --- | --- | --- |
| Hero destinations | 3 distinct silhouettes: rooted garden mesa, split arch with recognizable profile, sheltered roost tree | Recognizable from approach without labels; clear landing surface and unobstructed exit |
| Cloud family | 3 sculpted families, each with limited variants: bank, wisp-like shelf, distant tower | Reduce repeated “boulder” appearance; strong depth ordering; modest screen coverage and measured cost |
| Terrain/vegetation kit | 2 tree crowns, 2 trunk/branch forms, 2 rock clusters, 1 grass/flower clump, 1 seed pod | Break obvious repetition while retaining shared materials and predictable collision |
| Sanctuary states | Dormant, first bloom, flock arrival for one compact garden | Immediate before/after story payoff; deterministic saved state and reload behavior |
| Companion | Reuse Magpie rig; author perch idle, attention, hop, call and takeoff gestures | Character through timing and gaze; no lip-sync, dialogue engine or new species required |
| Existing playable birds | Focused feather/membrane material refinement and grounded motion polish | Readable species traits; believable partial folds/contact; no tuning changes disguised as art |
| Food/readable collectibles | Refined moth silhouette plus visually distinct seed | Differentiate catch, story item and wind cue through shape/motion, not only hue |
| Audio kit | Distinct wing strokes per flyer, landing surfaces, 2 habitat ambiences, lift/route/success motifs, companion calls | Cues remain distinguishable with HUD hidden and at user-selected volumes |

Material improvement can start with baked vertex shading, carefully authored gradients, a small shared palette/texture atlas and a stronger light/shadow value design. Consider a cheap contact treatment at perches. Test any new texture, normal, shadow or transparency path in stereo on Quest. “More realistic” does not automatically mean more attractive or more legible at flight speed.

Preserve calm areas. Dense particles and nearby fine patterns increase visual motion and clutter. Meta's optic-flow guidance supports evaluating detail placement as part of comfort, especially around close geometry; it does not justify removing all depth cues. [Meta optic-flow design guidance](https://developers.meta.com/horizon/resources/locomotion-design-reduce-optic-flow/).

### Audio direction

Use a layered soundscape: quiet habitat beds; spatial birds or chimes at destinations; player-local wind/feather cues; short success motifs; sparse music that opens up at a reveal and recedes on a perch. Lift and successful climb should be distinguishable: rising air can coexist with a descending bird. Separate world sound, guidance, music and haptic intensity options. Caption essential information at rest and offer equivalent visual guidance.

The existing per-wing wind haptics and focus/pause silence are useful. Refine the vocabulary rather than amplifying everything. A thermal entry, a moth catch and a collision should not share an ambiguous dominant cue. Evaluate the actual headset speakers and controllers; source inspection cannot grade the mix.

## 10. Progression that supports months of interest

Use three independent layers:

1. **Journey progress:** restored sanctuary states, opened routes and encounters. Short completed chapters count, and checkpoints survive interruption.
2. **Mastery:** personal bests for comparable challenges and badges for new techniques. Record species, control mode, assistance, route revision and scoring version so unlike flights are not silently compared.
3. **Expression/discovery:** local journal, feather or trail cosmetics, companion observations and optional roost decorations. Favor unlocks that celebrate accomplishments over repeated resource farming.

Species are horizontal choices. Explain Duck as a balanced flyer, Dragon as broad and weighty, Magpie as light and responsive using current profile evidence; confirm player-facing language against worn tests. Do not sell “more power” through stat bars unsupported by actual feel. Keep all currently selectable creatures available. Each can receive a mastery route after the core route works across all three.

Replay should combine a recognizable place with a different decision: reverse a permitted route, vary catch placement, choose another wind condition, follow a ghost, attempt a new precision line or look for a newly arrived visitor. Keep wind/seed/difficulty visible before a scored attempt. Authored variations should be tested for reachability; random placement is not automatic level design.

A later local route generator can assemble validated encounter segments with a known beginning, recovery perch and finish. Publish no endless-content promise until multiple sessions show that these combinations feel different. Multiplayer, online leaderboards and live events are deferred; a local ghost and a responsive companion offer cheaper social presence and mastery value first.

## 11. Work orders for future agents

**Backlog ownership:** these W IDs are the implementation queue. F IDs describe findings; IDs in companion reviews are supporting detail. Future agents should pick one bounded W item, verify current source, preserve unrelated work and report evidence. Recommendations are not instructions to implement all items in one pass.

Source names below sometimes identify classes rather than separate files: `TrickDetector` is inside `Flight/AcrobaticFlight.cs`, and `FlightProgress` is inside `Gameplay/FlightChallenge.cs`. Add new abstractions only when a concrete item requires them.

Sizes: **S** = one narrow subsystem/content change; **M** = several connected files plus player validation; **L** = a feature crossing content/runtime/save boundaries. These are relative scope indicators, not delivery estimates. P0 establishes trustworthy foundations; P1 builds the playable slice; P2 deepens replay only after that slice succeeds.

### P0 — make the next session trustworthy

**W01 — Current-build wearer and performance baseline · M · Quest QA + gameplay · dependencies: none.**
Read `tools/scripts/quest.py`, `telemetry.py`, telemetry contract, latest neutral handoff and current settings. Record build fingerprint, species, mode, view, wind, assistance and test route. Ask for a short relaxed-neutral/lift/HUD/menu test, then a separate representative longer run when the wearer wants one. Begin an automated streaming/lifetime soak now, independent of wearer exercise; rerun after new content in W22. Capture compositor/app timings, tracking gaps, streaming stalls and the wearer's account. **Baseline complete when:** neutral and magenta-bar results are explicitly passed or logged as reproducible open issues; data is tied to the installed build; no unmeasured comfort or frame-rate claim. **Feature acceptance additionally requires** resolving reproducible high comfort/rendering problems in the affected experience. A completed diagnosis does not authorize promoting a failing feature. Do not retune flight preemptively.

**W02 — Align reward categories with the brief · M · gameplay · dependencies: none.**
Targets: `FlightChallenge.cs`, `ExpeditionDirector.cs`, `ForagingScore.cs`, `FlightChallengeTests.cs`. Keep efficiency medals for soaring trials. Add a distinct untimed adventure completion reward and explicit movement/technique summaries; count rest independently. Specify whether a run is timed before it begins. **Done when:** identical task completion receives the same story progress regardless of optional perching; pause/rest cannot erase earned progress; an efficiency trial still rewards clean soaring; no movement proxy is labelled calories/recovery. Version changed scores and preserve legacy bests.

**W03 — Discoverable help with the cockpit quiet · M · UX + gameplay · dependencies: none.**
Targets: `CharacterSelectController`, `BirdFlightDriver`, `FlightCamera`, HUD/cards and expedition UI. Offer a clear preflight task preview and explicitly invoked help while paused/perched, independently of the instruments toggle. Do not automatically restore ongoing text on landing or pause. Introduce optional brief world/audio guidance and distinct mode/view/pause acknowledgements. **Done when:** a new tester can identify current objective, HUD binding, pause, recovery, view and mode without an external operator; HUD-off flight stays text-free except required calibration; guidance can be muted/disabled. Preserve existing button/chord behavior.

**W04 — Separate view comfort from flight authority · M · input/camera · dependencies: W01.**
Targets: `FlightCamera`, driver, selection/settings UI, `QuietCockpitTests`, `FlightRecoveryTests`. Keep current advanced full-body first-person and stabilized third-person. Add an explicitly chosen stabilized first-person option and optional comfort vignette only if wearer testing supports it; remember preferences. **Done when:** flight dynamics and trick detection are identical across view settings; HMD motion remains tracked; switching and recovery work from bank/inversion; no silent change to the user's preferred advanced view. Record individual comfort outcomes without a universal “safe” claim.

In the same settings design, specify separate comfortable-reach, seated/reduced-range, guidance and movement-intensity capabilities. Implement each input capability as its own reviewed increment with calibration/tracking tests and equivalent broad-route acceptance. Never claim one-hand support by bypassing tracking gates or lower the current calibration span threshold without testing. These capabilities do not change merely because a different camera was selected.

**W05 — Reliable checkpoint/save contract · M · gameplay/persistence · dependencies: none; coordinate schema with W02.**
Targets: `FlightProgress`, `ExpeditionDirector`, `SkyForaging`; add a small versioned local save owner. Save completed objectives, discoveries and restoration state at explicit checkpoints; flush on lifecycle events as best effort. Preserve `VoarVR.FlightProgress.v1` and foraging best during migration. **Done when:** menu return, app interruption, old-save migration, corrupt-save recovery and duplicate completion cannot lose or double-award previously checkpointed progress. Resume at a valid loaded perch, preserving story stage; never promise exact in-air restore without collision/calibration handling.

Add explicit on-demand pause actions for **recalibrate here**, **return to last safe perch**, **restart route** and **save and leave**, keeping A's existing restart/calibrate contract intact. Non-restarting recovery must preserve earned stages and exclude repositioning from altitude, quiet gain, catch and trick credit. Unknown schema or missing content IDs must preserve recoverable data and present a clear fallback rather than silently overwriting it.

**W06 — Rest that counts as success · M · gameplay/UX · dependencies: W02/W05.**
Targets: driver pause/ground phases, expedition timing, new session policy using existing observations. Add an intentional sanctuary/perch rest state with optional continue/finish choices and saved rewards. **Done when:** arms can rest without steering, combo or story penalties; stop and resume work; quiet airborne hands remain distinct from actual resting; no timer-based pressure is introduced into adventure mode.

### P1 — produce one excellent small adventure

**W07 — One-route art direction proof · M · art/level design · dependencies: W01.**
Targets: `design/ART_DIRECTION.md`, `create_skyward_kit.py`, `SkywardKit`, `SkyArchipelago`, existing shaders and explicit setup tools. Compose departure → lift → arch → garden, with a single dominant landmark per approach. Build hero silhouettes and material/value studies before detail. **Done when:** same-view before/after captures show stronger depth, material separation and destination identity; three independent visual/player/technical critics review the fresh artifact; all new geometry stays within the measured provisional budget. Do not expand world radius.

**W08 — Read the air without instruments · M · world/feedback · dependencies: W03/W07.**
Targets: `WindVisualizer`, `ThermalSeeds`, `FlightFeedback`, `WindField`, `FlightRegions`. Distinguish ambient flow, rising-air entry and actual altitude gain using shape/motion/sound as well as hue. Start with leaf/audio demonstrations at the first thermal; optional companion demonstration follows in W13. **Done when:** in HUD-off tests the player can find, enter, circle and leave useful lift; false visual lift is absent in Still Air; quiet successful ascent and rising-but-sinking are distinguishable. Do not increase wind strength to hide poor communication.

**W09 — Chapter 1 with a persistent consequence · L · gameplay/content · dependencies: W02/W03/W05/W06/W07/W08.**
Targets: `FlightChallenge`, director, route data, garden placement. Implement “A Route Home” with one request, one seed pickup, existing arch/landing and one visible restoration. **Done when:** the player can describe what they helped and see the result after restart; all three species can complete the broad route in Beginner; optional detours do not block the story; objective completion is driven by real world observations.

**W10 — Hero assets and cloud composition · M · Blender/art · dependencies: W07, budget from W01.**
Targets: Blender kit scripts, curated FBX, `SkyArchipelago`, sky/shaders. Produce the bounded asset set in section 9 with near/mid/far versions as needed. **Done when:** destinations are distinguishable in HUD-off approaches, visible cloud repetition is reduced, imported scales/collision align, and a worst-view Quest capture meets W18 criteria. Validate FBX and preserve `.meta` GUIDs; do not add paid packs or rewrite authored `.blend` sources without task scope.

**W11 — Camera and route clearance · M · camera/world · dependencies: W01.**
Targets: `FlightCamera`, contact/environment interfaces, `SkyArchipelago`, camera/ground tests. First fix chase obstruction against actual loaded geometry with minimal comfortable adjustment; author routes wide enough for the visible species, including wings. **Done when:** approaches near terrain/trees/arch do not place the chase camera through solid scenery, tracking offsets still work, and collision success agrees with what the player sees. Leave full wing physics for a separate justified task.

**W12 — Authored foraging lines · M · gameplay/level design · dependencies: W02/W05/W07.**
Targets: `SkyForaging`, `ForagingScore`, `FlightRegions`, foraging tests. Add three finite arrangements: easy sweeping arc, alternating broad turns, optional climb/diving line. Define start/finish and fair respawn rules. **Done when:** catches are readable at flight speed, each line works across intended species/modes, pause/reset/rebase cannot phantom-catch, and session interruption preserves banked rewards. Avoid making all food appear as a player-following reward cloud.

**W13 — One expressive companion · M · animation/gameplay/audio · dependencies: W07/W09.**
Targets: existing Magpie asset/articulation, a bounded companion presenter and route-following state machine. Start with perch/attention/call/takeoff and one demonstration loop. **Done when:** the companion communicates the next action without constant text, waits through rest/tracking interruptions, does not obstruct the view or collide unpredictably, and never controls the player's flight. No general-purpose NPC framework or language-model dependency.

**W14 — Maneuver school · M · flight/gameplay/UX · dependencies: W01/W03/W11.**
Targets: `TrickDetector`, driver feedback, training definitions, `AcrobaticFlightTests`. Teach carving/recovery before rolls/loops, then provide per-species entry guidance and retries. **Done when:** a wearer can repeat a maneuver intentionally and explain a failure; event points are distinct from cumulative total; no canned motion, false rotation awards or required inversion for story completion. Use actual trajectory regression fixtures.

**W15 — Comparable local records and mastery journal · M · gameplay/UI · dependencies: W02/W05/W14.**
Targets: `FlightProgress`, challenge results, menu and a journal presented at rest. Separate records by route/scoring revision and relevant species/control/assistance/weather settings. Add a small set of accomplishments across soaring, precision, exploration and optional acrobatics. **Done when:** old records remain readable, unlike attempts are not ranked as equivalent, and the player sees one concrete improvement and one next skill without grind requirements.

**W16 — Soundscape and feedback hierarchy · M · audio/gameplay · dependencies: W03/W08/W09.**
Targets: `FlightFeedback`, curated Audio assets and an audio settings/mixer path. Add habitat beds, spatial destination cues, species strokes and separate lift/catch/impact/success identities. **Done when:** listening on Quest distinguishes key events with HUD hidden; focus/pause/tracking gates remain correct; volumes/haptics can be adjusted independently; essential cues have alternatives; layering remains bounded under repeated events.

**W17 — Habitat identity and discovery · M · world/content · dependencies: W05/W09/W12.**
Targets: `WorldTerrain`, `FlightRegions`, pooled content placement and journal. Make lowland food, cloud travel and garden rewards distinct. Place one discoverable roost and one story clue per route segment. **Done when:** encounters are deterministic, reachable and checkpointed; returning after rebase/reload preserves discovered identity; no unlimited per-chunk spawned objects or ecology simulation is required.

**W18 — Rendering budget and LOD implementation · M · rendering/world · dependencies: W01; before accepting W10/W13 density.**
Targets: `WorldChunk`, `WorldStreamer`, `SkyArchipelago`, `SkywardKit`, URP settings, shaders and measurements tooling. Measure full-scene draws/vertices/materials, overdraw, CPU/GPU, collision cooking and memory; reduce the measured bottleneck using LOD/culling/batching, not arbitrary global quality cuts. **Done when:** representative lowland, sky, garden, food and character views sustain the chosen refresh target on Quest with documented margins, no repeatable traversal stalls and acceptable visual transitions. A chunk vertex count alone cannot pass this item.

**W19 — Creature presence and perching polish · M · rig/animation/art · dependencies: W01/W07/W11.**
Targets: `BirdRigDriver`, `AvianWingPresentation`, `BirdGroundPresentation`, character anchors/materials and Blender rig scripts. Refine partial folds, ground idle, foot placement and species-specific feather/membrane response. Evaluate peripheral wing ownership at actual eye anchors. **Done when:** first/third-person, all three species, walking/perching/half-fold and tracking reacquisition are captured; no visible popping or common surface penetration remains in the reviewed cases; physics/calibration profiles are preserved.

### P2 — deepen replay after the slice passes

**W20 — Three validated route variants · M · level design/gameplay · dependencies: W09/W15/W18.**
Introduce scenic, foraging and mastery variants using the same polished geography. Show wind/effort intent before launch. **Done when:** returning testers can identify a different decision in each route; authored segment validation rejects unreachable combinations; stopping after one variant counts as a complete session.

**W21 — Local ghost and style lines · M · gameplay/telemetry · dependencies: W05/W14/W15/W18.**
Create a compact visual ghost format with route/build compatibility; do not make full personal raw telemetry the product replay format. Add optional route-based style scoring for varied maneuvers and clean recovery. **Done when:** ghost view can be disabled, cannot collide, survives rebasing and does not obscure landmarks; incompatible runs are labelled; banking/rest behavior is clear.

**W22 — Long-session reliability and telemetry cost · M · technical QA · dependencies: W05/W18.**
Targets: streamer, logical coordinates, gameplay lifecycle, telemetry writer/history, device tools. Exercise 20–30 minutes of representative travel plus repeat menu/resume/rest cycles, with longer engineering soak if needed. Measure memory trend, frame delivery, warm-device behavior, save integrity and recording growth. **Done when:** no unexplained progressive degradation, lost checkpoint or recurring streaming stall; captures close cleanly or report partial state honestly. Keep dev recording retention explicit and user-controlled; no silent deletion policy.

**W23 — Chapter 2 / flock route · L · gameplay/content · dependencies: W09/W13/W17/W20/W22.**
Build the sheltered-versus-exposed Ridge choice with a small bounded flock and one new restoration consequence. **Done when:** both routes are usable in intended settings, companions wait safely during rest, and players recall the route choice and outcome after the session. Chapter 3 remains an outline until this adds demonstrated variety.

**W24 — Maintain a source-aligned design contract · S, recurring · documentation · dependencies: every accepted item.**
Update design/architecture only for accepted implemented decisions; append evidence to current-state/log/handoff and link relevant W IDs. **Done when:** current controls, mission conditions, saves and build acceptance can be found without trusting stale historical prose. Never mark a source scan as Unity compilation or a screenshot as Quest validation.

### Sequence and parallel ownership

Start **W01, W02, W03 and W05** as separately owned work. W01 protects current handling; W02 and W05 must agree on versioned rewards before overlapping edits. Then combine W06/W08 with W07's route design; build W09's single chapter. Art/audio can prototype while gameplay is written, but merge content only against W18's measured budgets. W11/W19 may address the same camera/rig area and need explicit file ownership. W24 is part of each task's handoff.

Do not begin with multiplayer, another playable species, a massive map, a large quest engine, paid asset packs, photorealistic cloud rendering, automated physiological adaptation or a Switch port. They have high cost and do not resolve the present reasons to stop playing. Revisit them only after the core slice earns repeat sessions.

## 12. Playtest protocol and acceptance gates

The next playtests must separate **input success, understanding, enjoyment, exertion and discomfort**. They are different outcomes. A detector award is input evidence; a high score is not proof of fun; long playtime can reflect confusion or inefficient control.

Use a small formative group, initially about six willing testers spanning new/experienced VR users and varied comfortable reach. This is a defect-discovery sample, not a statistically representative retention study. The existing wearer is an essential longitudinal case; do not treat them as the whole audience. Every participant should demonstrate pause/exit before beginning the moving route, and preflight should check comfortable reach without claiming unimplemented seated/reduced-range support. Let people stop immediately; record the reason without treating a shorter session as failure.

| Test | Evidence to collect | Proposed formative pass gate |
| --- | --- | --- |
| First five minutes | Build/config, calibration attempts, first useful lift, first landing; observer interventions | At least 5 of 6 can state the goal and reach the first meaningful success without operator intervention; investigate each exception |
| HUD-off navigation | Ask for destination, current progress and how to get help; route mistakes | At least 5 of 6 can navigate the chapter and access help; no unavoidable hidden gate |
| Relaxed control | Wearer marks corrective effort; compare matched calm/lift windows and small inputs | No repeated report of forced wrist/neck compensation in ordinary flight; any recurrence becomes a precise regression case |
| Embodiment | Ask whether wings feel like their body, whether actions match, and whether they can judge clearance | Record 1–7 ratings and concrete moments separately; target median at least 5 as a prototype hypothesis, with no severe recurring mismatch |
| Fun and variety | Favorite/least favorite moment, why a retry is appealing, perceived repetition | Most testers identify a specific enjoyable action and choose a second different activity when comfortable; do not substitute session duration |
| Story clarity | Ask whom/what they helped, what changed, and what they expect next | At least 5 of 6 can answer from play without an explanation; consequence survives reload |
| Exercise/rest rhythm | Self-reported effort 0–10, comfort separately, active/quiet/perched durations | Tester can choose challenge and obtain arms-down rest without losing rewards; no claims about calorie accuracy |
| Return visit | A second voluntary session on another day; record choice of activity and reason | Demonstrate interest in a different route or improving a skill; report actual return counts rather than promising retention |
| Delivery on Quest | App/compositor metrics, CPU/GPU work, memory trend, warm-device sample, near/far views | Sustain the selected target through representative content; identify and resolve repeatable stalls and visible rendering defects |

Numbers above are proposed go/no-go criteria for a small prototype, not industry standards or evidence of current success. Revise thresholds openly if playtests show the wrong thing is being measured. Negative comfort feedback takes priority over an average fun score.

At 72 Hz a frame interval is approximately 13.89 ms; at 90 Hz it is 11.11 ms. Choose the target and measure sustained delivery before increasing it. Total Unity `FrameTiming` CPU values may include waits; do not infer missed headset frames solely from a paced CPU percentile or infer zero misses from telemetry capture completeness. [Meta display-rate guidance](https://developers.meta.com/horizon/documentation/unity/unity-set-disp-freq/), [Unity FrameTiming semantics](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/FrameTiming.html).

For visual implementation, follow the repository's [quality loop](../agent/quality-loop.md): builder evidence → independent visual/player/technical critics → synthesis → small revision → fresh evidence, at most three rounds. Assessable dimensions should meet its documented gate; unavailable hardware dimensions remain unverified. No implementation/revision cycle occurred in this documentation-only review.

## 13. Ready-to-use agent brief

> Implement **W[ID]** from `docs/reviews/2026-09-10-game-critique-and-roadmap.md`. Read the required agent wiki and inspect the current worktree first. The roadmap is a recommendation; current code and latest user choices are authoritative. Restate this item's player outcome, affected files, dependencies and acceptance criteria. Preserve unrelated work, the quiet HUD default, deliberate calibration, current Beginner behavior and the user's chosen advanced embodiment unless this item explicitly changes an agreed contract. Do not expand to adjacent backlog items. Use Unity Editor APIs for scene/prefab edits and the checked-in Blender pipeline for authored assets. Run the narrow relevant checks, collect fresh evidence and use independent critics for substantial player-facing work. Report what is implemented, what was actually tested and what still needs a wearer. Update current-state, log and a dated handoff. Do not commit or push unless asked.

The first creative milestone should be easy to describe after playing: **“I learned to find the air, brought something home, saw my garden change, and wanted to fly a different route.”** That is the experience against which new assets, tricks and systems should be judged.
