# Input contract and current mappings

All devices supply `FlightInputFrame` via `IFlightInput`. Device APIs remain in Input.

| Action | Quest | Gamepad |
| --- | --- | --- |
| Wing pose | Tracked controllers, scaled to selected species after calibration | Neutral pose; South held supplies a downward stroke |
| Calibration coach / recalibrate | Right primary (A) opens the coach; press A again when its relaxed T pose is ready | Select/back resets (tracked calibration is not needed) |
| Calibrate after platform recenter | Meta/Oculus recenter notification, then hold the coached spread still | Not applicable |
| Switch first/third person | Right secondary | North |
| Cycle Assisted/Touring/Wild/StillAir | Left secondary | West |
| Bank | Lower the wing toward the turn | Left stick X |
| Turn physically | Spread-hand axis estimates torso yaw | Not applicable |
| Tuck/dive | Right trigger | Right trigger |
| Landing brake | Raise and hold spread wings; left trigger assists | Left trigger |
| Pause | Left primary (X) | Start |
| Walk while supported | Left stick translates relative to the bird's heading | Left stick |
| Turn while supported | Right stick X rotates the bird | Right stick X |
| Session menu / finish | Left Menu; right trigger confirms a menu row | East/B; Escape opens, Enter confirms on keyboard |

On a first XR flight, a diagram-led coach opens while flight is paused. The player faces a comfortable direction, holds a relaxed T pose with both controllers and looks slightly below the horizon; A captures only after the pose is ready. During ordinary flight A returns to this coach without ending the session or discarding journey state; entering it during a timed course safely ends/resets that attempt. Capture requires a level 0.7–2.2 m comfortable spread. Invalid capture leaves neutral wings and an explicit retry prompt. Each species supplies its rest reach and motion scale.

A platform recenter pauses safely, invalidates/rebuilds calibration in place, preserves simulation position, resumable velocity and view mode, suppresses wing derivatives, waits for 0.35 seconds of fresh stable comfortable tracking, and reopens the session menu after capture. Meta's reserved system button is not directly bound by the app: the app receives `XRInputSubsystem.trackingOriginUpdated`. This event may also originate from other runtime tracking-origin changes.

Left Menu pauses and opens the session menu. From there the player can continue, recalibrate here, return to a safe perch/course start, restart the route, change comfort/guidance/audio/haptics options, or choose **Finish session + see results**. Finishing saves a local movement summary before showing active/rest time, estimated wingbeats, flight and walking distance, gross climb/descent, top ground/air speed, catches/value, tricks, landings and contacts. Returning from that result goes to player selection. X remains a quick pause/resume control; while paused, a released then squeezed right trigger still opens the same menu.

`TrackedBodyFrame` estimates heading from the horizontal line between spread controllers. Independent head yaw does not steer; calibrated head pitch intentionally controls climb/descent. With tucked/missing hands it holds the last heading; head yaw is only an initial fallback. This is an approximation without a torso tracker: unusual asymmetric or crossed-hand poses remain a physical testing concern. Torso-relative wing derivatives prevent a whole-body turn from acting like a flap. Camera math removes the physical yaw already applied to the bird, preserving head look without doubling it; head translation stays 1:1.

Auto uses XR on Android, a connected gamepad on desktop, otherwise synthetic input. Missing/reacquired tracking suppresses velocity spikes. First and third person both retain head tracking; camera bank/roll is not inherited. Quest recenter behavior, physical effort and comfort need an on-headset session for each release.

BirdFlight starts in third person for XR, desktop and synthetic paths. B toggles without resetting flight; Meta recenter preserves the selected view. Head pitch uses the comfortable elevation captured during calibration, with a4° neutral deadzone, full downward response at23° and full upward response at32°. Visual head look remains independent. Loss of previously calibrated HMD tracking neutralizes pitch until tracking returns.

The physical landing brake initiates only while descending faster than0.2m/s within18m of a surface. It requires both hands at least18 cm above their calibrated neutral, horizontal spread>85% of neutral, speed<0.4 m/s, held0.3 s. Once engaged it stays active until the raised pose is released; ordinary open-air recovery strokes do not latch braking. Trigger braking remains available at any altitude. Approach coaching shares the contact solver speed limits; see [flight](FLIGHT.md).

## Flight Game mode gesture

Hold left-stick click0.7s without either grip to toggle Beginner/Acrobatic; release before another toggle. Either grip consumes the chord until release, preserving both-grips+left-click telemetry markers. Desktop C is equivalent. A/calibration coach, Meta recenter, B/view, X/pause, Y/weather, Left Menu/session menu, right-stick click/instruments and both ground sticks remain available. Acrobatic uses wrist pitch, fore/aft hand difference for yaw and bank for roll; Beginner retains comfortable calibrated head pitch.

## Quiet cockpit refinement (2026-09-10)

Right-stick click/H toggles the optional flight instruments. It does **not** hide the persistent gameplay ribbon: the current goal/course step and typed moth/value totals remain visible, and world markers follow the independent guidance preference. Required calibration prompts also remain available with instruments off; moths, wind visuals, sound and haptics remain. Advanced pitch measures relative controller rotation in body coordinates against each captured comfortable grip, with6° deadzone and32° full command. No absolute level-controller or90° grip requirement. Roll and yaw mapping unchanged. B selects full-rotation authored-eye first-person while Acrobatic is active; chase view remains stabilized. Beginner retains its comfortable first-person basis. A opens the calibration coach; Meta origin recenter calibrates in place and returns to the session menu.

Advanced neutral refinement: the captured comfortable grip is still zero. Passive aerodynamic pitch scales with deliberate pitch input and opposing airflow torque is bounded to half the actual pitch-control torque. Returning to neutral lets existing angular damping settle rotation; this is not altitude hold. Yaw weathercock, roll authority and calibrated wrist sensitivity retain their previous settings.

## Journey controls (2026-09-10)

Select a local player, flyer and activity before choosing Begin, Resume or Restart. A Route Home is the default untimed adventure. Resume restores the saved objective and prepares a physically supported perch; Quest resumes paused and asks for a fresh comfortable calibration. Player selection also exposes cumulative flight, distance, moth and top-speed history. New players use isolated local session history and course personal bests.

Left Menu opens the session menu directly and pauses safely. While already paused with X, releasing and squeezing the right trigger is an alternate way to open it. Left stick selects a row; a separate right-trigger squeeze confirms. Desktop equivalents include Escape or F1, arrows and Enter. Opening or navigating cannot carry a held tuck/walking/turning command into resumed flight. Lost tracking or focus closes help and requires a fresh release. Pausing alone leaves the cockpit quiet; explicitly opened help temporarily hides the bird and flight overlays without changing instrument preference.

The menu offers recalibration **here** (position and journey preserved), return to a verified safe perch (journey preserved), explicit route restart, session completion/results, and local preferences. Recovery waits for loaded collision and validates support; it does not award a landing, moth, arch crossing, course task or technique. App interruption pauses and saves; returning does not automatically resume flight.

First-person view comfort is stored separately from Beginner/Acrobatic flight authority: embodied preserves advanced body rotation, steady horizon uses the heading basis. World guidance, audio and haptics are independently stored options. B still switches first/third person. The chase camera checks the body collision environment for obstructions; cosmetic wings remain outside that collision proxy.
