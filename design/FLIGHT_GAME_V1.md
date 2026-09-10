# Flight Game v1

Implementation over `d62ad94`, 2026-09-10. Beginner remains the default; no character profile assets were retuned. This is a testable development slice, with acceptance evidence in `docs/development/flight-game-v1-validation.md`.

## Player journey

Choose Free Flight, Training, Skyward Expedition or the unlocked Ridge Journey before choosing a bird. Free Flight has no mandatory objectives. Training teaches discovery and 30m of quiet-wing ascent. Skyward asks the player to find the departure convection, gain140m with quiet wings and reach230m, cross the physical split arch, then contact the garden terrace. Completion saves a local best and unlocks Ridge Journey. The latter adds a migration waypoint around the rain front before its different island arch and landing.

A world label locates the next destination. The small mission card reports the task, distance and quiet-wing gain. Actual tricks add75 points each, up to three. Gold starts850, silver600; flapping time, collisions and elapsed time beyond ten minutes cost score. A collision never permanently invalidates a mission. Results persist locally with PlayerPrefs; no bird stat inflation, paid rewards or network service. Menu displays best scores. More mission templates/start unlocks/cosmetic rewards are future work.

## Controls and motion

Hold left-stick click for0.7s, without either grip, to toggle Beginner/Acrobatic. Release before another toggle. Both grips plus left-stick click remain the telemetry marker chord. A returns to start and calibrates; Meta recenter calibrates in place; B changes camera; X pauses; Y changes wind; right-stick click toggles HUD; left Menu returns to character selection; left stick walks on supported surfaces. Desktop C toggles Acrobatic.

Beginner retains existing calibrated head pitch, flap forces and bank flight. Acrobatic integrates body angular velocity, body inertia, control torque, gyroscopic coupling, aerodynamic flow alignment and damping into a quaternion. Wrist pitch, differential fore/aft sweep and bank drive pitch/yaw/roll. Orientation redirects the existing aerodynamic forces. There is no loop boost or world-up spring. Tucking changes angular damping; all rates are bounded. Values are gameplay tuning, not measured biological inertia.

Duck is medium response(155deg/s cap); Dragon broad/heavy(80); Magpie light/fast(230). Full rolls, forward/back loops and two-second inverted holds use actual body orientation; loops also require300degrees of trajectory winding. Twelve-second episodes, reversals, idle gaps, tracking/mode resets limit disconnected accumulation. Successful loops need entry energy. Comfort camera retains a horizon; Acrobatic and its exit limit heading following to45deg/s. Worn comfort remains to be evaluated.

## Air and world

Existing finite drifting plumes remain. Persistent terrain-fed convection supports the expedition route; it is explicit game geography. Both the controller and visuals sample the same field. Assisted widths use wing loading and comfortable turn radius, bounded1–1.8; Touring1.15, Wild1. Still Air removes all added air. Assisted centering requires an intentional sustained turn, useful lift and open wings; adds at most0.10bank and one quarter of player bank, never reverses the intended turn and adds no upward force.

Lowland below140m, cloud sea140–215m, archipelago215–480m, high sky above480m. Rain front applies actual gusts/sink. Nine logical island slots retain identity through travel and rebasing. Near geometry appears within520m; solid collision within260m. Lowland chunks combine original authored tree/rock/city modules. Clouds are bounded opaque sculpted meshes;192 thermal seeds and24 actual-flow ribbons expose rising air. Palette shader is single-pass with vertex colors and distance haze. These bounds are design choices, not proof of Quest frame rate.

## Validation contract

Preserve original Beginner trajectories; test actual energy/trajectory loops, input chord separation, mission progression, stable logical coordinates, collision placement and legacy/current recording reads. Review live Unity and authored Blender assets through MCP. Five independent critics per round; maximum three rounds. Scores use a strict commercial Quest comparison; screenshots cannot establish comfort or72Hz. A completed desktop input-only expedition proves mechanical connectivity, not fun or wearer usability. Report unmet gates and hardware gaps explicitly.
