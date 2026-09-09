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

`FlightInputFrame` is a value snapshot: head and wing position/orientation/tracking, wing velocity, look direction, semantic bank/tuck/flare, and reset/pause edges. XR coordinates are tracking-local meters (Unity +Y up, +Z forward), independent of virtual bird movement. Gamepad/synthetic poses are emulated. Input adapters own sampling and button edges. `BirdState` owns world position/rotation/velocity and phase. Presentation never modifies the simulation's private state directly.

The duck prototype uses a deterministic point mass with damped attitude. It integrates persistent velocity from gravity, airspeed-squared lift and drag, banked lift, flare incidence/drag and controller-driven per-wing stroke reaction. Tuck reduces wing area; lift degrades at low speed and beyond the tuned stall angle. Runtime frames subdivide to at most 1/120 s. Perch capture, simple ground contact and flap takeoff are present. Wind, angular momentum, ground effect, gusts, per-feather flow and general collision remain outside this model. Profile values are gameplay assumptions, not measured duck biomechanics.

`BirdFlightDriver` connects runtime input, calibration, simulation, scene transform and the imported duck rig. Auto selects XR in an Android player, gamepad if connected before Editor Play, otherwise synthetic. XR wing input and presentation remain neutral until a deliberate valid arm-span capture; raw head/hands remain available to calibration. Missing tracking contributes no stroke and the rig eases toward rest; reacquisition suppresses one derivative sample.

`BirdRigDriver` solves two-link analytic IK for each four-joint wing chain. It smooths calibrated local offsets before applying current root translation/yaw, clamps reach without stretching and bounds controller twist. `FlightCamera` applies a third-person desktop view or a yaw-only bird/calibration basis plus 1:1 tracked headset offset, including a before-render update. It retains the last valid HMD pose, hides face/neck geometry from the eye anchor and renders the XR calibration instruction. It does not inherit artificial body bank/roll. This remains an initial comfort solution pending headset validation. `FlightDiagnostics` exposes calibration, force/energy state and a switchable desktop overlay.

`Bootstrap` loads the prototype by build-scene name. BirdFlight contains the imported rigged duck, measured ground references, route posts and a directional light. `ProjectSetup` configures URP and Android OpenXR through an explicit command; it does not replace scenes. `DuckSetup` is the explicit editor operation that imports/wires the duck and scene. Runtime, editor, EditMode tests and PlayMode tests have separate assemblies.

Blender production is independent: `.blend` source -> validate -> FBX export -> curated runtime art. The exporter supports static meshes and one armature parent/modifier per skinned mesh, validates normalized one-to-four-bone weights and keeps authored sources unchanged. No Blender, Python, MCP, Quest SDK or platform account is required by the flight simulation. OpenXR's built-in Meta Quest support is sufficient for this prototype.
