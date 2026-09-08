# Flight verbs

| Verb | Intended experience | Implemented in M0 |
| --- | --- | --- |
| Flap | Rhythmic wing strokes create lift/thrust and effort. | Downward wing velocity adds upward kinematic motion; synthetic sinusoid available. |
| Glide | Extended wings trade height/energy for travel. | Constant forward motion; no energy or lift model. |
| Bank | Wing posture guides a readable, comfortable turn. | Semantic bank changes yaw; no aerodynamic roll. |
| Dive | Tuck to descend and gain speed. | Tuck adds forward speed and downward velocity. |
| Flare | Spread/reorient wings to slow an approach. | Flare reduces forward speed and adds upward velocity. |
| Land/perch | Settle into a stable rest state on a surface. | Visual perch cubes only; no collision or landing mechanics. |

Positions are meters, velocities meters/second. Prototype constants are placeholders, not empirical bird biomechanics. Future forces should use explicit timestep/input and be covered by synthetic sequences. Decide calibration, inertia and comfort criteria before replacing the simple controller. Head direction is exposed but does not currently steer the bird.
