# Architecture

```text
Input Device (OpenXR controllers / gamepad / synthetic sequence)
     |
IFlightInput -> FlightInputFrame
     |
BirdFlightController (bird flight simulation)
     |
BirdState -> BirdFlightDriver -> scene transform / camera / diagnostics
```

The controller is a plain C# object using Unity math types. It owns state, samples an injected provider once per explicit Step and has no scene, physics engine, Meta or device API dependencies. Tests construct it directly with synthetic/supplied input. A future force model belongs here or in a small pure helper when real requirements emerge; don't introduce ECS, services or a global event bus preemptively.

`FlightInputFrame` is a value snapshot: wing position, orientation, velocity and tracking status; look direction; semantic bank/tuck/flare; reset and pause edges. XR coordinates are tracking-local meters (Unity +Y up, +Z forward), independent of virtual bird movement. Gamepad/synthetic wing poses are emulated. Input adapters own sampling and button edges. `BirdState` owns world position/rotation/velocity and phase. Presentation never modifies the simulation's private state directly.

M0 motion is kinematic: yaw from bank, forward cruise, upward motion from downward wing velocity, dive descent and flare ascent/braking. There is no gravity integration, aerodynamic fidelity, collider response, landing or energy model. Perches are visual markers. Frame-size-independent physics is not claimed. Deterministic tests use identical explicit timestep sequences.

`BirdFlightDriver` connects runtime input to the controller and scene transform. Auto selects XR in an Android player, gamepad if connected before Editor Play, otherwise synthetic. Input mode changes take effect on the next Play session; synthetic gesture changes are live. XR and gamepad providers share the controller. Missing XR tracking returns untracked poses/zero wing velocity; tracking reacquisition suppresses one derivative sample.

`FlightCamera` applies a simple third-person desktop view or tracked headset pose relative to the bird, including a before-render update. It keeps physical head rotation and adds no artificial roll. This is an initial rig, not a validated comfort solution. `FlightDiagnostics` exposes inspector values and a switchable desktop IMGUI overlay.

`Bootstrap` loads the prototype by build-scene name. Both scenes and primitive meshes/materials are authored repository assets. `ProjectSetup` configures URP and Android OpenXR through an explicit command; it does not replace scenes. Runtime, editor, EditMode tests and PlayMode tests have separate assemblies. Empty domain folders reserve layout only; they contain no invented components.

Blender production is independent: `.blend` source -> validate -> FBX export -> curated runtime art. No Blender, Python, MCP, Quest SDK or platform account is required by the flight simulation. OpenXR's built-in Meta Quest support is sufficient for this prototype; XRI and Meta extensions can be introduced only when an actual interaction requires them.
