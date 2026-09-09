# Flight verbs

| Verb | Current behavior |
| --- | --- |
| Flap | Tracked wing velocity against the oriented pressure normal adds energy. |
| Glide | Air-relative lift and drag integrate persistent velocity; still-air glide spends height. |
| Bank | Wing-height/orientation asymmetry tilts lift and curves the trajectory. |
| Turn physically | Spread-hand torso-yaw changes rotate bird heading and velocity responsively; free looking stays independent. |
| Ride a thermal | Spread wings and circle in the cyan rising core. Moving updrafts can support prolonged no-flap height gain; leaving the core or excessive bank loses the benefit. |
| Dive | Tuck reduces wing area, lowering drag/lift and accelerating descent. |
| Flare | Left trigger increases incidence/drag to slow an approach. Long flares stall rather than hover. |
| Land/perch | Approach a marked landing surface slowly and flare; capture settles the bird, then a strong flap launches. |

The deterministic point mass uses bounded substeps (at most 1/120 s), gravity, air-relative lift/drag, stall degradation and active strokes. Character stat cards bake mass/area/response profiles. Unusually broad authored wings have an explicit `WingAreaMultiplier`; the dragon uses 6.16 m² at about 10.9 kg, rather than the former 1.12 m². These are game tuning, not measured biomechanics.

Assisted has strong helpful rising air; Touring reduces help; Wild increases gusts and includes a red descending column; StillAir disables wind. Thermal centers meander on bounded deterministic paths. Physics and visible streamlines use the same simulation time, including reset/pause; draft trails are advected through the sampled wind. The city/forest course includes arched routes at different heights, exposed perches, a river and distant ridges.

The course is bounded, generated at load and deliberately simple. Obstacles are visual practice references: general building/tree/terrain collision, angular momentum, ground effect, endless terrain streaming and detailed weather are not implemented. Actual Quest performance and flight comfort require hardware validation.

Dragon effort tuning: active stroke force scales with species mass so controller effort does not become cubically harder with visual size. Dragon converts neutral downstroke work to forward thrust (0.8 relative to upward force), caps force above 1.6 m/s hand speed, and launches at 8.4 m/s after a 0.72 m/s downward stroke. Its mass/lift, broad membrane, slower response and wider turns retain a distinct feel. Duck retains its existing profile. No motion/upstrokes generate no active thrust. The regression uses 36 cm peak-to-peak controller arcs at 0.8 Hz, including torso-relative derivatives and natural span shortening, in still air; it tests takeoff, 30s sustained flight and a 3s glide break. These are ergonomic gameplay targets, not claims of biological accuracy or a substitute for worn feedback.
