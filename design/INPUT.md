# Input contract and current mappings

All devices supply `FlightInputFrame` via `IFlightInput`. Keep physical/controller API details outside BirdFlightController.

| Value | XR provider | Gamepad provider | Synthetic provider |
| --- | --- | --- | --- |
| Wing position/orientation | Actual tracked controller pose in local tracking space | Fixed neutral poses | Neutral poses; flap varies height |
| Wing velocity | Finite difference of local poses; zero on tracking reacquisition | South button held emulates a downward stroke | Analytic sinusoidal flap velocity |
| Look direction | Head rotation applied to forward | Forward placeholder | Forward placeholder |
| Bank | Left-minus-right wing height clamped to [-1,1] | Left stick X | Left/right gesture +/-1 |
| Tuck/dive | Right trigger | Right trigger | Dive gesture |
| Flare | Left trigger | Left trigger | Flare gesture |
| Reset/recenter | Spread arms, then right primary captures tracked head/hands and resets bird | Select/back resets bird | Tests can supply frame flags |
| Pause | Left primary button edge | Start button edge | Not sequenced yet; tests can supply frame flags |

XR wing physics and presentation stay at a safe neutral pose until deliberate calibration succeeds. Calibration requires a valid HMD, both tracked hands and 0.7-2.2 m separation. It captures neutral positions/rotations, yaw heading and human span; all three controller-position axes scale to the 1.12 m duck span, while head translation remains physical 1:1. A temporary in-world prompt reports the required action. Reset suppresses derivative spikes.

Auto input chooses XR for an Android player, gamepad if present at startup in the Editor/desktop, otherwise Synthetic/Glide. Missing controller tracking contributes no flap and the rig eases toward rest; reacquisition suppresses the first velocity derivative. The camera retains its last valid HMD pose through loss. There is no dominant-hand setting, replay file format, haptics or accessibility effort control yet.

Calibration, controller bindings, stereo scale and effort still require an on-headset playtest. Synthetic fixtures cover neutral glide, symmetric flap, both banks, tuck/dive, flare, stall recovery, takeoff and tracking loss.
