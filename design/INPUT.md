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
| Landing brake | Raise and hold spread wings; left trigger assists | Left trigger |
| Pause | Left primary (X) | Start |
| Character selection | Left Menu button | East/B; Escape on keyboard |

Right primary (A) resets the flight to spawn/third person and captures the current tracked head/hands in one action, requiring a level 0.7–2.2 m comfortable spread. Invalid capture leaves neutral wings and an explicit retry prompt. Each species supplies its rest reach and motion scale. A platform recenter only invalidates/rebuilds calibration in place: it leaves simulation position, velocity and view mode intact, suppresses wing derivatives, and waits for 0.35 seconds of fresh stable comfortable tracking before automatically capturing. Meta's reserved system button is not directly bound by the app: the app receives `XRInputSubsystem.trackingOriginUpdated`. This event may also originate from other runtime tracking-origin changes.

Left Menu returns to CharacterSelect, clearing the consumed character pick and restoring scene time scale. X still pauses. The calibration and pause prompts show character-selection help.

`TrackedBodyFrame` estimates heading from the horizontal line between spread controllers. Independent head yaw does not steer; calibrated head pitch intentionally controls climb/descent. With tucked/missing hands it holds the last heading; head yaw is only an initial fallback. This is an approximation without a torso tracker: unusual asymmetric or crossed-hand poses remain a physical testing concern. Torso-relative wing derivatives prevent a whole-body turn from acting like a flap. Camera math removes the physical yaw already applied to the bird, preserving head look without doubling it; head translation stays 1:1.

Auto uses XR on Android, a connected gamepad on desktop, otherwise synthetic input. Missing/reacquired tracking suppresses velocity spikes. First and third person both retain head tracking; camera bank/roll is not inherited. Quest recenter behavior, physical effort and comfort need an on-headset session for each release.

BirdFlight starts in third person for XR, desktop and synthetic paths. B toggles without resetting flight; Meta recenter preserves the selected view. Head pitch uses the comfortable elevation captured during calibration, with a4° neutral deadzone, full downward response at23° and full upward response at32°. Visual head look remains independent. Loss of previously calibrated HMD tracking neutralizes pitch until tracking returns.

The physical landing brake initiates only while descending faster than0.2m/s within18m of a surface. It requires both hands at least18 cm above their calibrated neutral, horizontal spread>85% of neutral, speed<0.4 m/s, held0.3 s. Once engaged it stays active until the raised pose is released; ordinary open-air recovery strokes do not latch braking. Trigger braking remains available at any altitude. Approach coaching shares the contact solver speed limits; see [flight](FLIGHT.md).

## Flight Game mode gesture

Hold left-stick click0.7s without either grip to toggle Beginner/Acrobatic; release before another toggle. Either grip consumes the chord until release, preserving both-grips+left-click telemetry markers. Desktop C is equivalent. A/reset, Meta recenter, B/view, X/pause, Y/weather, left Menu/character select, right-stick click/HUD and ground stick walking remain available. Acrobatic uses wrist pitch, fore/aft hand difference for yaw and bank for roll; Beginner retains comfortable calibrated head pitch.

## Quiet cockpit refinement (2026-09-10)

Right-stick click/H toggles instruments **and all ongoing textual coaching, objective captions/beacons and food score**. Required calibration prompts remain available with HUD off; moths, wind visuals, sound and haptics remain. Advanced pitch measures relative controller rotation in body coordinates against each captured comfortable grip, with6° deadzone and32° full command. No absolute level-controller or90° grip requirement. Roll and yaw mapping unchanged. B selects full-rotation authored-eye first-person while Acrobatic is active; chase view remains stabilized. Beginner retains its comfortable first-person basis. A still recovers/resets/calibrates into third-person; Meta origin recenter calibrates in place.
