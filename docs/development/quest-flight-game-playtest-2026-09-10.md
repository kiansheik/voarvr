# Actual Quest gameplay diagnosis — 2026-09-10

User reports advanced controls too sensitive and thermals difficult to use. Downloaded the two latest persisted sessions from Quest3 (`192.168.68.52:5555`) after recovering an intermittent offline ADB transport. No install, launch, reset, deletion, physics tuning or commit was performed. Source fingerprint matches the FlightGame APK: `ea5906a9fff94256282168fe23fda1f6680feffc58aff03fba2dcb2694978d4b` over d62ad94. Package last updated16:01:51 local time. This closes the previous uncertainty about whether this build reached hardware; it does not establish user acceptance.

## Downloads

Local originals, manifests, decoder summaries and reproducible analysis scripts: `artifacts/telemetry/quest-latest-2026-09-10/`.

| Session | Character | Frames | Recorded duration | Bytes |
|---|---|---:|---:|---:|
| `9667308d5d014fe4a6a274f30643f2db` | Dragon |25,328|351.66s|55,862,508|
| `ac551b9c4b6548549688ea6de8e54224` | Magpie |50,933|707.37s|112,349,870|

Both schema2, complete footer, zero dropped records, no truncation. Raw files remain on Quest. Older14:25 session was listed but not needed for this latest-playtest comparison. Filtered Unity/AndroidRuntime logcat snapshot was empty; startup messages had rotated out or were otherwise unavailable. Persisted frame telemetry supplies the evidence below; absence of logcat is not proof of no runtime errors.

## Advanced mode is too reactive

For calibrated, wings-enabled airborne frames, Magpie advanced absolute angular-rate95th percentiles: pitch143.9deg/s, yaw90.8, roll162.2. Dragon: pitch72.7, yaw43.9, roll51.4. These are actual integrated body rates, not just configured limits. They do not prove every movement was unintended, but support the user's report of excessive sensitivity.

Magpie's first mode switch at433.17s was followed by pitch164/roll214deg/s95th-percentile rates over the next6.83s. Dragon switched at80.37s after a strong climb; during92.52–94.38s it lost23.7m while air rose2.58m/s, descending12.9m/s on average. This is not a wind strength failure in isolation: trajectory/orientation can overwhelm the updraft.

Source maps calibrated wrist pitch directly to torque (`wrist/22 - headSignal*.2`) without a wrist deadzone, plus un-deadzoned fore/aft sweep. Existing flow torque increases with speed squared. The recordings establish rapid response; separating intentional commands from flow torque needs deterministic playback/instrumentation, since telemetry records control torque but not the separate aerodynamic torque.

## Thermals are inconsistently useful and poorly communicated

Magpie, Beginner/Assisted,38.98–50.53s:11.55s in mean3.41m/s rising air, altitude58.72→42.23m (loss16.5m).10.71s had total active stroke force below0.5N. It stalled for roughly54% of this interval. There was no commanded tuck or flare. This is a concrete regression fixture for easier soaring, not a reason to ask the user to flap harder.

Across calibrated airborne Beginner Magpie samples with wind_y>2 and active stroke<0.5N:40.08s, mean air+3.04m/s but mean climb only+0.28m/s;15.44s descending and16.75s stalled. This is a force threshold, not a video-confirmed motionless-wing measurement. Banking/relative-flow incidence remain possible contributors.

Dragon did find useful lift in Beginner:54.77–80.37s gained142.1m in25.6s, mean air+6.71m/s;14.73s below0.5N active stroke. Across its qualifying quiet Beginner lift samples, mean climb+3.46m/s. The fact that this felt unclear to the wearer means visual/sonic/progress feedback is insufficient even when physics works.

Most lift visits lasted only a few seconds. Assisted centering was active for only1.72s of the Magpie's failed11.55s interval; its intentional-turn gate/cap can provide little help to someone still trying to discover the technique. Do not infer that stronger updraft alone solves retention, stall behavior and feedback.

## Mission feedback obscures success

Both sessions remained on objective stage1 (soar), eventually reporting progress1.0 without advancing. Source also requires current altitude>=230m, while progress only displays accumulated quiet-wing gain. Dragon's big ascent occurred before its quiet-gain total was complete; later progress reached100% at lower altitude. Thus the user can see completed progress yet be stuck. Expose both conditions or redesign the initial soaring objective. The strong earlier climb should receive clear feedback immediately.

## Performance and calibration signals

Both recordings report about72Hz, unscaled-frame95th/99th percentiles13.89ms. Only2 Dragon and3 Magpie captured frames exceeded20.83ms. Streaming blocked time was zero. GPU95th: Dragon3.13ms, Magpie2.95ms. CPU95th14.73/14.69ms includes pacing/wait and delayed FrameTiming data; do not interpret it alone as simulation cost or compositor dropped frames. Capture cost95th0.086/0.085ms. These samples do not show broad frame-time collapse as the main explanation for poor handling, nor guarantee all headset frames were presented correctly.

Normalized upward head command frequently saturated in airborne Beginner frames: Dragon70.3/123.1s, Magpie309.6/461.2s. This is relative calibrated input saturation, not a direct neck-angle measurement. It warrants checking calibration and whether the user feels forced to look up to gain height. The user report supersedes earlier provisional positive comfort assumptions.

## Focused next changes

1. Preserve Beginner baseline. Make advanced opt-in with a substantially softer center, explicit neutral deadzones, lower rotational authority/rate limits and stronger damping; test entry/exit and actual captured input before re-enabling it as a promoted feature.
2. Reproduce the Magpie38.98–50.53s failed climb with matching wind/environment. Fix Assisted stall/turn/sink interaction so ordinary open-wing flight in useful rising air delivers predictable gain without exhausting technique. Evaluate wider retention/support and readable core/edge/net-climb feedback together.
3. Correct the soaring objective's hidden altitude condition and celebrate actual height gained. Validate with a short wearer attempt, not the known-coordinate synthetic pilot.

No runtime changes in this diagnostic pass. Analysis describes observed correlation and source mechanisms, not a fully reconstructed visual replay or a proven single cause.
