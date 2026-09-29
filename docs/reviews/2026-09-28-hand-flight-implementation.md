# September 28 — hands-first implementation review

Branch `meta-vr-start-2026-hand-flight`, uncommitted tree. Unity 6000.6.0f1, Meta XR Core 207.0.0 / OVRPlugin 1.207.0 on Unity OpenXR 1.18. This review closes the interrupted hands-first milestone: adapter, continuity, compact calibration, gaze + pinch UI, telemetry v5 and a controller-free flow. It records what the desktop evidence shows and what still needs a Quest.

## Scope

Controller-free judge path for the compact Magpie **A Route Home**: cold start → preflight (gaze + pinch) → compact calibration with explicit READY → perched supported departure → rest (two pinches held forward 0.6 s) → flight settings page → in-place recalibration → finish session → results → return. Design contract: [hand flight](../../design/HAND_FLIGHT.md). Competition gates: [brief](../competition/meta-vr-start-2026.md#readiness-at-september-28).

## Defects found by fresh runtime evidence and fixed

| Round | Evidence | Fix |
| --- | --- | --- |
| 1→2 | Perched view rendered below ground: calibration used the synthetic head, the camera read a separate HMD action | Hand-mode camera shares the adapter's head sample; regression asserts the perched camera is above the bird |
| 1→2 | Inferred poses relabeled DirectLow; WMM flag kept after permission/runtime loss | Only DirectHigh downgrades; flag cleared on every early return |
| 2→3 | Perch faced a wall of rising ground: the spawn is the centre of an 85 m flattened bowl (`WorldTerrain.Elevation`) | Authored ridge lookout `FlightRegions.DepartureLookout*` (72, ~23.3, 60; heading 18° to the departure thermal), 10 m scatter clearing, SavedPerch → Departure → SpawnFloor fallback preserving the departure's resume |
| 2→3 | Terrain cut the lower rows of every perched card; the results card's only exit button was hidden | `WorldCardRendering`: built-in canvas material copy with `unity_GUIZTestMode = Always`, queue 4000, sorting order 100, on rest/settings/calibration/result cards |
| 3 | `VerifyApk()` failed every Development build on Unity's own `player-connection-ip` in `boot.config` | Exempt exactly that line; the SDK token is absent from all 797 entries |

## Executed evidence

| Check | Result and limit |
| --- | --- |
| EditMode | **317/317 pass** (adds the lookout terrain test) |
| PlayMode | **69/69 pass** (adds unsupported-lookout fallback and cards-over-world tests) |
| Host tooling | **29/29 pass** |
| Hand runtime captures | Round-03 `HandFlightReview`: 8 frames, synthetic direct hand samples, real Button callbacks, live adapter/calibration/native supported recovery; perch at (72, 23.32, 60), yaw 18°, zero takeoffs; storage isolation PASS |
| Route from the perch | `RouteHomeReview.RunSpeciesFromLookout("Magpie")`: **Completed, 215.85 simulated s**, seed collected, one contact (garden landing, 3.36 m/s), garden restored after reload. Lift found 2.2 s after launch. Air-spawn baseline 202.07 s. Scripted semantic input, not hands |
| Development APK | BuildReport Succeeded, 168.529 s, 0 errors / 8 warnings; 69,181,451 bytes, SHA-256 `b050111474ae4cb3cfb3417d28582d13db45211e2ce075a5aaf5adbe7fec154d`; embedded fingerprint `ff21bfc3…` equals the tree minus one later editor-only change |
| Packaged manifest | `oculus.software.handtracking` required; HAND_TRACKING and BODY_TRACKING permissions; `com.oculus.software.body_tracking` required; `handtracking.frequency=LOW`; VR launcher category; `supportedDevices` quest2/questpro/quest3/quest3s |
| Credential scan | SDK access token in 0 of 797 entries (UTF-8/UTF-16); LAN address only in Unity's Development `player-connection-ip` |
| Settings | Project, Editor, XR general, OpenXR and Meta settings byte-identical to the pre-run snapshot after every Unity run |
| Repository check | Only the 24 known unused TMP sample GUIDs |

Development-only APK contents: Meta Core deliberately adds its XR Operator layer (`com.meta.agenticxr` MediaProjection service, `FOREGROUND_SERVICE_MEDIA_PROJECTION`, non-required `com.oculus.experimental.enabled`) to Development builds only, and Unity writes the profiler address. Meta's setup checks warn about minimum API 32 and a single GameActivity entry. A submission build must be non-Development and re-inspected.

## Round-3 independent critique and disposition

Visual and player critics reported independently (`artifacts/reviews/hand-flight/round-03/{visual,player}.md`). The technical critic was cut off by a usage limit before reporting; its scope (adapter safety, manifest, card rendering) is covered only by the builder's own checks above.

Fixed after critique (then retested):

| Finding | Fix |
| --- | --- |
| V1 high: calibration ran in the spawn bowl with the bird hidden | Fresh hands sessions perch on the lookout before the coach; READY calibrates in place (no world reset or cut); the coach is lifted ~15° and faces the eye; the bird stays visible during calibration |
| V2/P10 high: gold-on-gold reticle and 2 px hover were invisible on primary buttons | Two-tone reticle (ivory core in a dark diamond) and ivory-plus-dark hover frame; gaze feedback needs only head tracking, activation still needs a direct hand and a fresh pinch |
| V3 medium: hands menus showed a controller cursor | Gazed row takes the highlight; nothing pre-selected; pinch-drag cannot move a hidden selection |
| V4 medium: READY looked disabled as it unlocked | Explicit ready/waiting styling, no tint cross-fade, 0.25 s hold against one-frame tracking drops |
| P2 high: both pinches during first calibration exited to the preflight | Ignored in hands mode while uncalibrated |
| P3 high: hands resume showed controller wording and no next step | The recalibration coach opens when the saved perch is ready |
| P4 high: focus/headset return left a frozen scene | A paused non-course hands flight with nothing visible reopens the rest card |
| P5 high: "Return to safe perch" left the card behind | Visible cards re-anchor in front of the camera after any perch recovery; messages name CONTINUE FLYING |
| P6 medium: generic pose rejection | Per-hand reach colours and specific hints (wider, closer, level, …) |
| V7 low: see-through card backdrops | Opaque |
| P13 low: one-hand pinch-drag needed both hands | Each drag needs only its own measured hand |
| Builder: a failed pose-source query blocked all hands | Fail-closed only when Wide Motion Mode could infer; otherwise confident camera hands count as direct |

Deferred, needing a decision or hardware: P1 coaching text is hidden while instruments are off (earlier user decision to keep flight text hidden); V5/P12 always-on moth tally (earlier user request); P8 chase camera applies head yaw inside a fixed 17.7° tilt, which rolls the horizon when the neutral pitch differs (check first on Quest); P7 neutral pitch set by where READY sits; P9 no afterglow card after Route Home; V6 no contact shadow; P11 extras still exposed; U1–U8 device risks (fragile single-frame READY, thermal-circling sickness, stereo depth of always-on-top cards, seat-width reach, palm-up system gesture). Scores (desktop): visual composition 5, gaze affordance 4 before fixes; player failure/recovery 4 before fixes. No re-score after revisions; the next review should be a Quest wearer test.
