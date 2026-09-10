# Local flight telemetry (development builds)

Development builds and Editor sessions record by default; release players do not. No microphone, camera, passthrough, account identifier or cloud upload. Raw motion belongs in ignored `artifacts/telemetry/`; do not commit recordings or marker windows without deliberate review. No biological parameters are inferred automatically from one wearer's movement.

Each flight scene creates a UUID session at `Application.persistentDataPath/telemetry/<uuid>.voartlm` and prints `VOAR_TELEMETRY_PATH=<resolved file>`. Character return closes the session; the next selection starts another. The installed app and recordings persist until app data is cleared/uninstalled. A reserved Meta button is not intercepted: the existing OpenXR origin-change event triggers in-place recalibration.

Mark an awkward or good moment by **holding both grips and clicking the left stick**; `MARK n` appears briefly. Right-stick click still toggles HUD. KeyboardM marks Editor sessions. Marker events have timestamps and increasing session-local numbers.

## Capture and reliability

Each gameplay input tick copies raw tracking-local positions/quaternions, tracking flags, body-relative finite-difference velocities expressed in tracking axes, native XR velocity/angular-velocity features with independent availability flags, triggers and relevant held/edge controls. `raw` is before gameplay gating/normalization; `mapped` is the controller's consumed, body-normalized frame. Head loss preserves raw device tracking flags while mapped physics remains safely gated. Calibration, targets/reach errors, six core wing rotations, feather/tail independent states, flight forces, motion, phase, wind, logical coordinates and cheap streaming counters accompany each sample.

The frame schema is [`TelemetrySample.Fields`](../../unity/Assets/Game/Telemetry/TelemetrySample.cs). Units: metres, seconds, m/s, radians/s for native angular velocity; quaternions XYZW; force newtons; energy joules; angles explicitly named degrees where applicable. `phase` uses FlightPhase order. Native unsupported values are NaN and have false flags. CPU/GPU timings are optional prior-render measurements. `capture_cpu_ms` measures main-thread capture work before the final ring copy; it is not total telemetry CPU or GPU overhead. Allocation counters are explicitly unavailable: the previously tried GC counter returned zero even for known allocations. Use Unity profiling and optional external Meta OVR Metrics alongside captures; do not infer zero allocations.

A512-slot single-producer/single-consumer ring reserves one empty slot and copies value structs. Full rings drop new records and count loss. A background thread performs buffered binary writes and flushes about once a second. It never calls Unity APIs. Main-thread capture does no file IO, waiting, JSON serialization or joins. Pause/focus loss requests a flush; destroy/quit requests drain and close. Android force-kill can interrupt shutdown: absence of a terminal record means incomplete, not successful flush. Pull after returning to character selection for a closed file; live pulls can have a partial tail, which the decoder reports while preserving completed frames.

## Binary layout

All numeric fields are little endian. Header: eight ASCII bytes `VOARTLM1`, int32 version1, int32 UTF-8 JSON header byte count, then the one-time JSON header. Header includes field names, UUID/UTC/build revision plus code/model/catalog source fingerprint, Unity/app/device/OS, character, full effective flight/morphology/articulation values, initial spawn, seed, wind mode and world configuration. Unknown biological wingbeat frequency is explicitly flagged unavailable. Duck's tuned and Dragon's fantasy flight values are not described as measured morphology.

Every record starts with uint8 kind and int32 payload byte count. Kind1: one IEEE754 float64 per header field, in schema order. Doubles preserve logical coordinates and timestamps; availability flags/counters also occupy doubles. Kind2: int32 event code, float64 monotonic timestamp, float64 numeric value. Kind3: int32 total dropped records, marking a clean writer close. No per-frame strings or JSON. Exact event codes are [`TelemetryEvent`](../../unity/Assets/Game/Telemetry/TelemetryWriter.cs); unknown length-delimited kinds can be skipped. Never change a shipped schema without versioning its reader contract.

Events cover session start/end, accepted/rejected calibration, recenter, tracking changes, view/weather, effective stall, rising-air entry/exit, impacts, approach/landing, deliberate takeoff, support loss, streaming stalls, character return, marker, pause/resume and reset. Tracking event values are HMD/left/right bit masks. A landed phase transition alone is not counted as an intentional takeoff.

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

`TelemetryReplayInput` exposes raw recorded frames and original simulation dt. `TelemetryReplay` additionally restores accepted calibration after its original step, including subsequent recalibrations, and reproduces pre-calibration XR gating. Supply an alternate `BirdFlightProfile` for a controlled comparison. A regression checks nonzero head pitch, asymmetric arm neutral, a later calibration change and varying original dt against direct simulation.

The runner targets simple deterministic environments. It does not reconstruct streamed geometry, world rebases, collision caches or arbitrary external origin changes. Supply fixed wind/environment deliberately; do not call a still-air replay an exact reconstruction of a thermal/collision flight. Dropped input frames cannot be faithfully replayed. Record quality and these limits must accompany comparisons.
