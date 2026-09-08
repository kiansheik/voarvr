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
| Reset/recenter | Right primary button; requests XR recenter and resets bird | Select/back resets bird | Not sequenced yet; tests can supply frame flags |
| Pause | Left primary button edge | Start button edge | Not sequenced yet; tests can supply frame flags |

Gamepad poses/velocity and bank-from-height are provisional mappings, not finished biomechanics. The left XR primary button is used for pause to avoid assuming the system menu is app-owned. Button edges are detected per sample, so a held button doesn't toggle pause repeatedly. Reset takes precedence over pause.

Auto input chooses XR for an Android player, gamepad if present at startup in the Editor/desktop, otherwise Synthetic/Glide. Mode is fixed for a Play session; select explicit Gamepad to support connecting one later, or restart Auto after connection. Gesture can change during Play. Missing gamepad returns neutral input; missing controller tracking returns untracked wing poses and zero motion. There is no calibration UI, dominant-hand setting, replay file format, haptics or locomotion comfort option yet.

Future physical input needs arm-span calibration, neutral pose, sample filtering, tracking-loss handling over app focus changes and comfortable effort thresholds. Test coordinate spaces and signs with synthetic fixtures before interpreting real flaps as forces.
