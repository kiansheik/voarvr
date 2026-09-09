# Flight verbs

| Verb | Intended experience | Implemented in duck prototype |
| --- | --- | --- |
| Flap | Rhythmic wing strokes create lift/thrust and effort. | Each tracked wing projects stroke velocity against its oriented pressure normal; summed forces add energy. |
| Glide | Extended wings trade height/energy for travel. | Airspeed-squared lift and drag integrate persistent velocity; neutral glide slowly spends height/energy. |
| Bank | Wing posture guides a readable, comfortable turn. | Wing-height/orientation asymmetry tilts lift, curves velocity and then aligns heading to the trajectory. |
| Dive | Tuck to descend and gain speed. | Reduced wing area lowers drag/lift so gravity accelerates a descent. |
| Flare | Spread/reorient wings to slow an approach. | Area, incidence and drag increase; a prolonged flare slows into a stall rather than hovering. |
| Land/perch | Settle into a stable rest state on a surface. | Existing perch capture and a simple ground-plane contact settle to Perched; a strong flap launches with inertia. |

The model is a deterministic point mass with damped attitude. State persists position, velocity and orientation; forces include gravity, lift, profile/induced/flare drag and active per-wing stroke. Lift degrades at low speed and beyond the configured stall angle. Integration subdivides runtime frames to at most 1/120 s. Values use SI units. The duck profile is gameplay tuning, not a claim of measured biomechanics. Angular momentum, wind, ground effect, gusts, per-feather flow and general surface collision remain simplified or absent.
