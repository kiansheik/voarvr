# Flight verbs

| Verb | Current behavior |
| --- | --- |
| Flap | Torso-relative controller velocity against each wing pressure normal adds energy. |
| Glide | Air-relative lift/drag spend height in still air; Duck tuning is preserved. |
| Bank / turn | Wing asymmetry tilts lift; spread-hand torso yaw rotates heading and velocity independently of free head look. |
| Look down | Pitch is relative to calibrated comfortable head elevation: 4° deadzone, full down at23°, full up at32°. |
| Ride rising air | Spread wings, find upward-moving cyan traces, then bank gently to remain inside moving lift. Ambient horizontal traces alone do not imply lift. |
| Dive | Right trigger/tuck reduces wing area and accelerates descent. |
| Brake | While descending within18m of a surface, raise both spread hands at least18 cm above neutral and hold0.3s with low hand speed; keep holding to retain braking. Open-air recovery strokes do not engage it; left trigger also supplies braking incidence/drag. Sustained braking can stall. |
| Land | Align over a marked surface, slow forward motion and descent, brake, then make actual gentle contact. |
| Take off | A strong downstroke launches from stable contact. |

The deterministic point mass integrates gravity, air-relative lift/drag, stall and active strokes at substeps no larger than1/120 s. These are gameplay assumptions, not measured biomechanics. There is no angular-momentum or ground-effect solver.

Dragon retains v3 mass, wing area (6.16m²), stroke normalization/forward thrust, force ceiling and slower response. `GlideDragMultiplier=.7` reduces profile and induced wing drag; Duck remains1. No further wing-area increase or art rewrite. From100m to90m, neutral no-flap input at120Hz, no wind/environment:

| Species | Distance | Actual altitude loss | Glide ratio | Mean sink | Final airspeed | Elapsed |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Duck |43.12234m|10.01102m|4.30749|1.47221m/s|6.38784m/s|6.79998s|
| Dragon |81.80056m|10.00363m|8.17709|0.61593m/s|4.79271m/s|16.24150s|

Dragon covers1.897× Duck distance. Neither run gains mechanical energy on any step. This measures a reproducible trimmed gameplay glide including initial transients, not a biological lift/drag polar. Duck results match the pre-change baseline. Moderate v3 stroke/takeoff regressions remain in the suite.

## Contact landing

A species-sized sphere sweeps every flight substep against nearby terrain and simplified building/trunk/canopy/rock geometry. Glancing contact removes inward velocity and damps the tangent; it cannot add energy or tunnel through a thin obstacle. Four contacts per substep are allowed; exhausted/full query buffers stop conservatively. Wings and protruding necks are not individually collidable.

Safe contact requires an upward surface within35° of level, non-departing motion, downward speed<=2.5 m/s and horizontal speed<=6 m/s. Active braking widens these to3.2 m/s and8 m/s. Unsafe impacts slide and cannot silently become a landing on the next substep: clear the contacted surface by about0.3m and try again. Coaching asks the player to flap up and retry. Safe contact enters Perched immediately; no invisible radius or magnetic settle. Loaded support disappearing releases the bird. Flap takeoff has0.5 s recapture protection.

The reusable ring and transient coaching identify nearby actual collider tops. The messages use braking-dependent limits: TOO FAST, SLOW YOUR DESCENT, RAISE + HOLD WINGS SPREAD, GOOD APPROACH, LANDED. The ring is guidance only. There is no progression/adaptive-coaching system or dedicated perched wing animation yet.

## World and air

`WorldStreamer` maintains25 reusable128 m chunks in every horizontal direction. Logical double offsets and integer chunk keys survive local-origin shifts. Continuous elevation/river/biome fields create city clusters, forest, open valleys and ridges across boundaries. Geometry is a deliberately simple combined-mesh prototype; visibility fades into sky before the loaded edge.

Atmosphere combines altitude-varying ambient wind, finite drifting/tilted thermal plumes, intermittent convergence streets, terrain upslope lift and lee sink. Plumes use192 m cells, births every80 s,160 s smooth lifetimes and finite vertical envelopes. Assisted provides stronger help/readability; Touring is subtler; Wild includes stronger variability and red dangerous sink; StillAir has no wind or ribbons. Up to24 pooled ribbons sample and advect through the exact physics field, fade over their10–14s lifetime and occur at several altitudes. Cyan horizontal flow is ambient wind; rising motion indicates lift. Prototype channel-following and comfort still require worn feedback.

Dragon wrist control uses `WingPitchSensitivity=.3` for effective wing incidence/body pitch only. Raw controller orientations still drive visual twist, and explicit calibrated head-down steering still descends. This prevents natural wrist tilt during ordinary flaps from producing a steep unintended dive; Duck sensitivity stays1 and neutral glide is identical.

Generated terrain enables its native MeshCollider before binding the completed mesh. Exact retained triangle data provides upward recovery when already embedded below a loaded surface; swept native geometry handles normal approaches. Tests require non-null meshes, real top-ray hits and species sphere clearance through regeneration and rebasing.

## Instruments and Assisted air

Airspeed is wind-relative knots; altitude is loaded-ground clearance in metres; climb/sink is actual world vertical speed. Heading and a compact bank/pitch display describe the bird independently of HMD gaze. Gold streams indicate positive vertical air, cyan ambient and rose sinking. Actual climbing plus rising air shows GLIDE + CIRCLE; rising air alone never claims height gain. Wing wakes and speed numbers share cyan→violet airspeed color.

Assisted thermals have48m base radius,9.4m/s peak strength,30–95m center altitude and5.2m/s convergence packets. To avoid an unrecovering crosswind stall, Assisted nonzero air enables bounded automatic aerodynamic feathering: reduce excess positive incidence above StallAngle−4 by at most45°. No additional force is injected, negative incidence remains unchanged, and explicit flare/tuck bypass it. Other weather modes and zero-wind trajectories are unchanged. Visible controller pose is independent. In20s unpowered circles both species gain height; actual entry and control regressions protect this behavior.

## Optional Acrobatic mode (2026-09-10)

Beginner retains the d62ad94 flight profile and measured trajectories. Acrobatic adds persistent quaternion rotation with body inertia, control/flow torque and damping; the existing aerodynamic forces follow body orientation. Duck/Dragon/Magpie have distinct rotational envelopes, not changes to Beginner assets. Energetic-entry loop tests require actual trajectory winding and nonincreasing passive energy. See [Flight Game design](FLIGHT_GAME_V1.md) for tuned values and limits.
