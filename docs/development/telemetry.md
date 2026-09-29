# Local flight telemetry (development builds)

Development builds and Editor sessions record by default; release players do not. No microphone, camera, passthrough, account identifier or cloud upload. Raw motion belongs in ignored `artifacts/telemetry/`; do not commit recordings or marker windows without deliberate review. No biological parameters are inferred automatically from one wearer's movement.

Each flight scene creates a UUID session at `Application.persistentDataPath/telemetry/<uuid>.voartlm` and prints `VOAR_TELEMETRY_PATH=<resolved file>`. Character return closes the session; the next selection starts another. The installed app and recordings persist until app data is cleared/uninstalled. A reserved Meta button is not intercepted: the existing OpenXR origin-change event triggers in-place recalibration.

Mark an awkward or good moment by **holding both grips and clicking the left stick**; `MARK n` appears briefly. Right-stick click still toggles HUD. KeyboardM marks Editor sessions. Marker events have timestamps and increasing session-local numbers.

## Capture and reliability

Each gameplay input tick copies raw tracking-local positions/quaternions, tracking flags, body-relative finite-difference velocities expressed in tracking axes, native XR velocity/angular-velocity features with independent availability flags, triggers and relevant held/edge controls. `raw` is before gameplay gating/normalization; `mapped` is the controller's consumed, body-normalized frame. Head loss preserves raw device tracking flags while mapped physics remains safely gated. Calibration, targets/reach errors, six core wing rotations, feather/tail independent states, flight forces, motion, phase, wind, logical coordinates and cheap streaming counters accompany each sample.

The frame schema is [`TelemetrySample.Fields`](../../unity/Assets/Game/Telemetry/TelemetrySample.cs). Units: metres, seconds, m/s, radians/s for native angular velocity; quaternions XYZW; force newtons; energy joules; angles explicitly named degrees where applicable. `phase` uses FlightPhase order. Native unsupported values are NaN and have false flags. CPU/GPU timings are optional prior-render measurements. `capture_cpu_ms` measures main-thread capture work before the final ring copy; it is not total telemetry CPU or GPU overhead. Allocation counters are explicitly unavailable: the previously tried GC counter returned zero even for known allocations. Use Unity profiling and optional external Meta OVR Metrics alongside captures; do not infer zero allocations.

A512-slot single-producer/single-consumer ring reserves one empty slot and copies value structs. Full rings drop new records and count loss. A background thread performs buffered binary writes and flushes about once a second. It never calls Unity APIs. Main-thread capture does no file IO, waiting, JSON serialization or joins. Pause/focus loss requests a flush; destroy/quit requests drain and close. Android force-kill can interrupt shutdown: absence of a terminal record means incomplete, not successful flush. Pull after returning to character selection for a closed file; live pulls can have a partial tail, which the decoder reports while preserving completed frames.

## Binary layout

All numeric fields are little endian. Header: eight ASCII bytes `VOARTLM1`, int32 schema version (currently **5**), int32 UTF-8 JSON header byte count, then the one-time JSON header. Header includes field names, UUID/UTC/build revision plus code/model/catalog source fingerprint, Unity/app/device/OS, input mode, character, full effective flight/morphology/articulation values, initial spawn, seed, wind mode and world configuration. Unknown biological wingbeat frequency is explicitly flagged unavailable. Duck's tuned and Dragon's fantasy flight values are not described as measured morphology.

Every record starts with uint8 kind and int32 payload byte count. Kind1: values in header field order; schemas 1–2 use IEEE754 float64 throughout, while schemas 3–5 use float64 only for `wideFields` and float32 for the remaining fields. Kind2: int32 event code, float64 monotonic timestamp, float64 numeric value. Kind3: int32 total dropped records, marking a clean writer close. No per-frame strings or JSON. Exact event codes are [`TelemetryEvent`](../../unity/Assets/Game/Telemetry/TelemetryWriter.cs); unknown length-delimited kinds can be skipped. Never change a shipped schema without versioning its reader contract.

Events cover session start/end, accepted/rejected calibration, recenter, tracking changes, view/weather, effective stall, rising-air entry/exit, impacts, approach/landing, deliberate takeoff, support loss, streaming stalls, character return, marker, pause/resume and reset. Tracking event values are HMD/left/right bit masks. A landed phase transition alone is not counted as an intentional takeoff.

## Schema 5: hand source and capture provenance

The writer emits **325 signals / 1336 bytes per frame**, preserving the complete v4 prefix (301 signals / 1224 bytes). Each raw hand appends:

- `raw_{left,right}_pose_source`: 0 Unknown/legacy controller, 1 DirectHigh, 2 DirectLow, 3 Inferred, 4 Lost. Direct states encode the adapter's confidence classification; inferred/lost states do not assert a separate confidence value.
- `raw_{left,right}_sample_timestamp`: the standard hand-state sample timestamp, float64. Controller samples write NaN. The existing raw position/orientation capture the native display pose before the continuity filter; mapped fields capture the pose consumed by flight. Raw velocity and estimated-motion flags retain the continuity filter's motion authority, so a visible native pose does not imply measured movement.
- `raw_{left,right}_unextrapolated_available`, `..._position_{x,y,z}`, `..._rotation_{x,y,z,w}` and `..._timestamp`: native unextrapolated pose and capture timestamp when available. Unavailable payload values are NaN. Availability alone does not authorize flap energy; source and `MotionEstimated` still govern motion authority.

Four per-hand timestamps join the five original `wideFields`. Their clock is the tracking runtime's clock; do not assume it equals Unity's `timestamp` epoch. Positions/orientations use the adapter's tracking-local Unity coordinates. `hand_wmm2_enabled` records the SDK's reported enabled state (false can also mean unavailable); it does not prove accurate inferred motion. `hand_fmm_requested` is the app request, currently always 0. There is no getter for actual FMM activation, so an OS frequency override cannot be inferred from this signal. Header `input` identifies the adapter and `handConvention` describes the provenance contract.

C# and Python retain v1–v4 reader behavior and reject future unsupported versions. Old records gain no invented hand evidence: C# replay leaves source Unknown and raw-pose availability false; Python retains original fields. Replay restores v5 source, sample times, raw-pose availability and pose alongside v4 estimated flags. It does not rerun the live continuity filter or enable device tracking modes. Marker windows preserve the raw hand signals and WMM/FMM fields.

Runtime effort accounting and Python cadence/speed analysis exclude estimated, DirectLow, Inferred and Lost motion. Invalid samples clear stroke arming, so a recovery cannot complete an inferred upstroke. Legacy Unknown input retains historical controller meaning when tracked and not estimated. `tracking_fraction` remains pose availability; `measured_motion_fraction` and v5 source fractions expose the distinction. These are movement proxies, not physical exertion or Quest tracking-quality validation.

## Schema 4: supported turning and estimated motion

Schema 4 contains **301 signals / 1224 bytes per frame**. It preserves the entire schema-3 prefix and appends six float32 fields: `raw_ground_turn`, `mapped_ground_turn`, `raw_left_motion_estimated`, `raw_right_motion_estimated`, `mapped_left_motion_estimated` and `mapped_right_motion_estimated`. Turn is the normalized -1..1 supported-yaw input; estimated-motion flags are 0 or 1. Raw and mapped values are retained separately because gameplay gates can change what the solver consumes.

Version 4 used the same version in both the binary prefix and JSON header. Earlier recordings have no evidence for these inputs: C# replay uses zero ground turn and false estimated flags, preserving historical controller behavior; Python retains the original fields without inventing new measurements. Compact v3 remains exactly 295 fields / 1200 bytes. Do not describe a pre-v4 recording as containing supported-turn or estimated-motion evidence.

V4 repaired recording of existing turn and estimated-motion inputs. It contains no hand source, capture timestamp, unextrapolated pose or WMM/FMM evidence; those fields begin with v5. A historical v4 effort summary should not be relabeled as provenance-aware hand activity.

Editor reviews and tests may temporarily set `FlightTelemetry.EditorEnabledOverride=false` before scene startup, restoring its prior nullable value afterward. Null retains ordinary Editor capture behavior; this override is absent from player builds.

## Schema 3 baseline and enjoyable effort

Schema 3 introduced 295 signals in 1200 bytes per frame (formerly 275 signals / 2200 bytes). Timestamps, simulation time and logical coordinates retain float64; other fields use float32. These encodings remain the prefix of schema 4. The bounded writer and loss footer remain unchanged. Ground-clearance queries run at 10 Hz with a recorded age rather than every frame.

New signals expose span ratio, inferred tuck, automatic feather support/angle, aerodynamic torque, quiet soaring gain, altitude objective history, food/combos, hand travel, movement speed, estimated stroke count and active/quiet intervals. Events mark protection loss/recovery, quiet bouts, periodic movement summaries and catches. Multiple catches in one tick share one event; cumulative counts preserve totals.

Effort is a **movement proxy**, not calories, medical fatigue or physical recovery. Active/quiet counters integrate valid airborne capped simulation time, excluding pauses, perching, streaming blocks and missing head/hand tracking. Holding arms still can be tiring. Reports include excluded-phase coverage; categories overlap. Use mode, species, calibration, tracking quality and build together when comparing sessions. We do not automatically increase difficulty or penalize breaks from these estimates.

The intended tuning loop is: compare active movement with successful climbs/catches, inspect rising-air sinking and protection loss, identify long repetitive flapping without reward, and preserve useful quiet soaring between playful bursts. Combine logs with the player's report of comfort; logs alone cannot establish exertion or neck pain.

`python3 tools/scripts/telemetry.py history artifacts/telemetry --output artifacts/telemetry/history.json` creates a chronological JSON/Markdown report, deduplicated by session. Per-recording caches invalidate on source size/mtime or analysis-script changes. Initial analysis still loads a whole recording; subsequent history runs reuse summaries. Files stay local and no recording is automatically deleted.

## Pull and inspect

The helper reuses existing Unity SDK/ADB discovery. It finds the app-reported directory in device logs and verifies it exists. If logs rotated, supply the actual printed `--remote-path`; it does not blindly assume an Android directory. Use `--serial` if multiple devices are connected.

```sh
python3 tools/scripts/quest.py connect
python3 tools/scripts/telemetry.py list
python3 tools/scripts/telemetry.py pull <uuid>.voartlm
python3 tools/scripts/telemetry.py summarize artifacts/telemetry/<uuid>/<uuid>.voartlm
python3 tools/scripts/telemetry.py export-csv artifacts/telemetry/<uuid>/<uuid>.voartlm
python3 tools/scripts/telemetry.py markers artifacts/telemetry/<uuid>/<uuid>.voartlm
python3 tools/scripts/telemetry.py markers artifacts/telemetry/<uuid>/<uuid>.voartlm --marker 2 --before 5 --after 10
```

Summarize writes JSON and Markdown: body-relative travel percentiles, human cadence estimate, hand-speed/native-angular distributions, neutral-relative wrist excursion, asymmetry, tracking fraction, torso yaw, phase fractions, speeds/climb/AoA/forces, glide segments/energy, stall and thermal durations, collision/landing events, streaming stalls, frame-time percentiles, optional CPU/GPU timing and loss count. Cadence is a thresholded estimate; inspect CSV before deriving tuning conclusions. Marker extraction creates a compact raw-input/calibration window; it is a candidate regression fixture, not an automatically committed personal dataset.

## Deterministic replay boundary

`TelemetryReplayInput` exposes raw recorded frames and original simulation dt, including supported turn and estimated-motion flags from v4 and hand provenance from v5. `TelemetryReplay` additionally restores accepted calibration after its original step, including subsequent recalibrations, and reproduces pre-calibration XR gating. Supply an alternate `BirdFlightProfile` for a controlled comparison. Regressions cover nonzero head pitch, asymmetric neutral, later calibration, varying dt, supported-turn trajectory equivalence and estimated downstrokes remaining unable to create active force after binary replay. Separate fixtures exercise each legacy binary schema and precise hand-clock round trips.

The runner targets simple deterministic environments. It does not reconstruct streamed geometry, world rebases, collision caches or arbitrary external origin changes. Supply fixed wind/environment deliberately; do not call a still-air replay an exact reconstruction of a thermal/collision flight. Dropped input frames cannot be faithfully replayed. Record quality and these limits must accompany comparisons.
