# Architecture

```text
Input Device (OpenXR controllers / gamepad / synthetic sequence)
     |
IFlightInput -> FlightInputFrame
     |
BirdTrackingCalibration -> BirdRigDriver (articulated presentation)
     |
BirdFlightController (point-mass flight simulation)
     |
BirdState -> BirdFlightDriver -> scene transform / camera / diagnostics
```

The controller is a plain C# object using Unity math types. It owns state, samples an injected provider once per explicit Step and has no scene, physics engine, Meta or device API dependencies. Tests construct it directly with synthetic/supplied input. A future force model belongs here or in a small pure helper when real requirements emerge; don't introduce ECS, services or a global event bus preemptively.

`FlightInputFrame` is a value snapshot: head and wing position/orientation/tracking, wing velocity, look direction, estimated torso yaw, semantic bank/tuck/flare, and separate reset/recalibrate/pause/view/wind/character-selection edges. XR coordinates are tracking-local meters (Unity +Y up, +Z forward), independent of virtual bird movement. Gamepad/synthetic poses are emulated. Input adapters own sampling and button edges. `BirdState` owns world position/rotation/velocity and phase. Presentation never modifies the simulation's private state directly.

The duck prototype uses a deterministic point mass with damped attitude. It integrates persistent velocity from gravity, airspeed-squared lift and drag, banked lift, flare incidence/drag and controller-driven per-wing stroke reaction. Tuck reduces wing area; lift degrades at low speed and beyond the tuned stall angle. Runtime frames subdivide to at most 1/120 s. An injected IFlightEnvironment supplies swept contact, dynamic landing queries and support checks; FlightContactSolver handles dissipative response and safe contact. `IWindField` supplies moving air; forces use velocity relative to that air. Angular momentum, ground effect and per-feather flow remain outside this model. Profile values are gameplay assumptions, not measured duck biomechanics.

`BirdFlightDriver` connects runtime input, calibration, simulation, scene transform and the selected species rig/profile. `CharacterSelection` is consumed once from a Resources character catalog. Each definition supplies rest reach, wing area and species glide drag efficiency. Duck aerodynamic values remain unchanged; Dragon applies0.7 to profile/induced wing drag. Auto selects XR in an Android player, gamepad if connected before Editor Play, otherwise synthetic. XR wing input and presentation remain neutral until a deliberate valid arm-span capture; raw head/hands remain available to calibration. Missing tracking contributes no stroke and the rig eases toward rest; reacquisition suppresses one derivative sample.

`BirdRigDriver` solves two-link analytic IK for each four-joint wing chain. It smooths calibrated local offsets before applying current root translation/yaw, clamps reach without stretching and bounds controller twist. `FlightCamera` applies a third-person desktop view or a yaw-only bird/calibration basis plus 1:1 tracked headset offset, including a before-render update. It retains the last valid HMD pose, hides face/neck geometry from the eye anchor and renders the XR calibration instruction. It does not inherit artificial body bank/roll. This remains an initial comfort solution pending headset validation. `FlightDiagnostics` exposes calibration, force/energy state and a switchable desktop overlay.

`Bootstrap` loads CharacterSelect, which routes the chosen species to BirdFlight. BirdFlight contains the species driver, camera, directional light and serialized world/material bridge. Legacy ground/perches/route references are disabled; their GUIDs remain intact. `ProjectSetup` configures URP and Android OpenXR through an explicit command; it does not replace scenes. `DuckSetup` is the explicit editor operation that imports/wires the duck and scene. Runtime, editor, EditMode tests and PlayMode tests have separate assemblies.

Blender production is independent: `.blend` source -> validate -> FBX export -> curated runtime art. The exporter supports static meshes and one armature parent/modifier per skinned mesh, validates normalized one-to-four-bone weights and keeps authored sources unchanged. No Blender, Python, MCP, Quest SDK or platform account is required by the flight simulation. OpenXR's built-in Meta Quest support is sufficient for this prototype.

`TrackedBodyFrame` estimates torso yaw from spread controllers, retains it through tuck, and computes wing derivatives relative to the torso. The controller normalizes these frames before interpreting wing forces and applies bounded physical-yaw changes to heading/velocity. The camera subtracts yaw already applied by the controller so free head look is not doubled. Missing HMD tracking suppresses wing/control input. Right primary resets to spawn and captures neutral inputs; platform tracking-origin changes preserve flight state and queue an automatic stable recapture.

## Endless world and coordinate ownership

`ProceduralFlightWorld` is now only the scene/material bootstrap. `WorldTerrain` supplies pure continuous elevation, river and biome fields from logical world position and seed7319. `WorldChunk` combines terrain, city/water, trunks/rocks and canopy meshes into four renderers, with simplified collider proxies. Features use seed plus signed integer coordinates; sequential hash mixing prevents reflected-coordinate aliases. Generation has no traversal-order random state. Low-frequency continuous trigonometric fields give broad valleys/ridges/biomes; production terrain noise and rich assets are outside this prototype.

`WorldStreamer` maps signed64-bit `(floor(logicalX/128), floor(logicalZ/128))` keys to25 pooled roots: a5×5 neighborhood (radius2). Collision is enabled only in the3×3 inner ring. At Duck~6.4m/s and Dragon~4.8 m/s,128 m provides roughly20–27s of normal glide per chunk, ample advance-generation distance. Four combined meshes per root avoid an object per tree/building. Each root retains up to108 box proxies plus one terrain collider, with a hard theoretical2725 pooled chunk-collider ceiling. Only a bounded neighborhood renders; haze fully matches the sky before its nearest edge. There is no separate distant terrain LOD or persistent modified-world storage.

Rows, feature cells and mesh uploads/cooks are queued central-first with at most32 work items and a cooperative1.5 ms timer per update. Boundary bookkeeping is included in measured time. An indivisible Unity mesh/collider operation can exceed that target; it is not a real-time guarantee. Initial/restart creation completes one central chunk synchronously so contact is safe. Other chunks fill incrementally. The driver pauses progression into an unfinished chunk instead of traversing missing collision. Pools and buffers are reused; components may grow to a per-root maximum during first visits to denser biomes.

`WorldSpace` keeps double X/Z origin offsets and local Unity float transforms. Beyond768 m on either horizontal axis the driver rebases by128 m multiples: bird state/spawn/support, camera, chunks and ribbon histories shift together, while logical position is unchanged. HMD/controller tracking-local samples, calibration, velocity and rotation are untouched. World and atmosphere always sample logical coordinates. A restart empties/reuses chunks, zeros the logical offset and returns to the original departure point. Vertical transforms and the existing simulation clock remain floats; this prototype targets long horizontal flights rather than arbitrary altitude/time precision.

## Atmosphere and presentation

`AtmosphereModel` is deterministic data sampling from logical position, seed and simulation time. Gradually changing ambient altitude layers combine with tilted/drifting finite plumes (192 m cells,80 s births,160 s smooth lifetime;18 neighboring feature probes), intermittent convergence streets in altitude bands, upslope lift/lee sink and Wild gust/sink features. The field has finite lift envelopes rather than infinite vertical columns.

`WindField` adapts local coordinates to this field. `WindVisualizer` owns24 reusable12-point ribbons (16Touring,0StillAir), at most2spawns/frame, historical points every0.35 s,10–14s fade lifetimes and2s fade ramps. Ambient spawn fans favor the view ahead; other ribbons select nearby active plume centers. Every trail advects through the same sampled field used by forces. No visible arrow/tunnel invents lift. Wild sinking flow can use red; cyan also denotes harmless horizontal ambient wind. Shared opaque shading and a transparent additive wind shader avoid texture/light expansion.

## Contact and dynamic landing

The plain controller owns no GameObjects or Unity Physics calls. `IFlightEnvironment` injects Sweep/TryFindLanding/IsSupported; `PlaneFlightEnvironment` and deterministic test rooms validate it. `UnityFlightEnvironment` uses64-entry nonalloc spherecast/overlap buffers against layer30, plus robust penetration recovery. Full sweep buffers stop conservatively; saturation is counted. Bird sphere radius scales from0.22m to0.55m across species. Mesh terrain and box-like trunks/canopies/buildings/rocks provide actual collision; decorative wings/neck do not have separate proxies.

`FlightContactSolver` allows four contacts/substep, removes inward velocity and damps tangent motion. Safe actual contact on a slope<=35° lands below2.5 m/s downward and6 m/s horizontal (3.2/8 with braking). Unsafe contact locks landing until a fresh separated approach; this prevents collision damping from becoming an instant automatic landing on the next substep. A strong flap launches with recapture protection. Loaded support loss releases Perched state.

`LandingSurface` marks loaded collider parents with deterministic surface identities. The runtime adapter queries only nearby active geometry, so streamed surfaces appear/disappear without a global registry. `LandingGuide` reuses one surface ring, and the driver offers transient state-dependent plain-language coaching. Legacy PerchPoint transforms are no longer simulation inputs.

Current recovery routing: right-primary action resets then captures; platform-origin events queue a stable in-place recapture. Driver owns scene return and explicit recovery orchestration; controller calibration itself never teleports. Character profiles normalize active stroke force by mass and optionally convert downstroke work into forward thrust with a bounded hand-speed ceiling.

## Flight instruments and assisted feathering

FlightHud creates one camera-parented world Canvas at2m with noninteractive text/images. It reads controller state and exact loaded triangle height; formats at5Hz with0.4s smoothing. BirdAirflowTrails owns two20-point world histories and two drawing buffers, follows actual tip bones after driver presentation, advects by current bird-local air and resamples at most1.6–4m of path. Rebase/reset/pause/perched behavior is explicit. Existing serialized materials avoid Android shader stripping. WindVisualizer shares HUD vertical-air colors.

IWindAssistance is an optional pure-flight interface; only Assisted WindField enables feathering. Controller bounds excessive positive incidence before the existing lift/drag calculation, with no added force and explicit-brake/tuck exclusions. This prevents measured crosswind-induced deep stalls. StillAir and non-Assisted modes do not use it.

Do not rely on historical GetAllocatedBytesForCurrentThread results: a known1MB allocation also returned0 in this editor. Prior zero-GC evidence is invalid. HUD formatting allocates; rendered-frame profiling and device measurements are separate from synchronous subsystem CPU timings.
