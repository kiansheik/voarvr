# Input contract and current mappings

All devices supply `FlightInputFrame` via `IFlightInput`. Device APIs remain in Input.

| Action | Quest | Gamepad |
| --- | --- | --- |
| Wing pose | Tracked controllers, scaled to selected species after calibration | Neutral pose; South held supplies a downward stroke |
| Restart and calibrate | Right primary (A), in a comfortable level spread | Select/back resets |
| Calibrate here | Platform Meta/Oculus recenter notification, then hold spread still | Not needed for emulated poses |
| Switch first/third person | Right secondary | North |
| Cycle Assisted/Touring/Wild/StillAir | Left secondary | West |
| Bank | Lower the wing toward the turn | Left stick X |
| Turn physically | Spread-hand axis estimates torso yaw | Not applicable |
| Tuck/dive | Right trigger | Right trigger |
| Flare | Left trigger | Left trigger |
| Pause | Left primary (X) | Start |
| Character selection | Left Menu button | East/B; Escape on keyboard |

Right primary (A) resets the flight to spawn/first person and captures the current tracked head/hands in one action, requiring a level 0.7–2.2 m comfortable spread. Invalid capture leaves neutral wings and an explicit retry prompt. Each species supplies its rest reach and motion scale. A platform recenter only invalidates/rebuilds calibration in place: it leaves simulation position, velocity and view mode intact, suppresses wing derivatives, and waits for 0.35 seconds of fresh stable comfortable tracking before automatically capturing. Meta's reserved system button is not directly bound by the app: the app receives `XRInputSubsystem.trackingOriginUpdated`. This event may also originate from other runtime tracking-origin changes.

Left Menu returns to CharacterSelect, clearing the consumed character pick and restoring scene time scale. X still pauses. The calibration and pause prompts show character-selection help.

`TrackedBodyFrame` estimates heading from the horizontal line between spread controllers. Independent head look does not steer. With tucked/missing hands it holds the last heading; head yaw is only an initial fallback. This is an approximation without a torso tracker: unusual asymmetric or crossed-hand poses remain a physical testing concern. Torso-relative wing derivatives prevent a whole-body turn from acting like a flap. Camera math removes the physical yaw already applied to the bird, preserving head look without doubling it; head translation stays 1:1.

Auto uses XR on Android, a connected gamepad on desktop, otherwise synthetic input. Missing/reacquired tracking suppresses velocity spikes. First and third person both retain head tracking; camera bank/roll is not inherited. Quest recenter behavior, physical effort and comfort need an on-headset session for each release.
