# Advanced neutral equilibrium — 2026-09-10

## Goal
Fix the last sustained forward-wrist correction in advanced mode using newest user recordings, preserve the otherwise improved controls, build/install for another try.

## Files inspected
Required agent wiki, current git54562d1 (clean baseline); Flight/AcrobaticFlight,controller,calibration; existing QuietCockpit/Acrobatic/ComfortSoaring tests; telemetry mapped/calibration semantics; actual Duck caf1f4dc… and Magpie a7484ed4… recordings; independent physics and data-analysis findings.

## Files changed
Flight/AcrobaticFlight.cs; new Tests/EditMode/AdvancedNeutralTests.cs/meta; design/INPUT.md; current-state/log and this handoff. No scene, art, camera, roll/yaw or Beginner changes. No commit/push.

## Commands run
Bounded ADB reconnect/list/pull; telemetry decoding and artifacts/telemetry/quest-neutral-trim-2026-09-10/analyze_grip.py; Unity MCP resources/Console/refresh; initial188 and revised final190EditMode tests; git diff --check; restore XR preloaded assets via EditorAPI; Quest development build. No Play/Python rerun because this isolated flight-moment change is covered by the full EditMode dynamics suite; previous22Play/21Python remain historical.

## What worked
Both new recordings clean0drops, Duck518.72s and Magpie352.12s, installed sourceb8bb65ee. Reconstructed wrist commands agree with telemetry torque within tiny numeric residuals. Magpie302.72–308.91s holds+30.23° (left39.30/right21.31) to balance+.1651/-.1646Nm; earlier window uses-13.23° and opposite torque. This supports correcting physics equilibrium rather than guessing a permanent anatomical offset. Final190tests passed, including all species' actual loops and energy checks.

## What failed / revisions
Initial smooth-step return of passive pitch had a partial-input sign crossing under opposing air. Independent technical review caught it; replaced before build with linear input scaling and opposing torque bounded to half actual command torque. Strong opposing full-input behavior intentionally changes so requested direction wins; helpful full input remains unchanged. Calibration zero itself was already correct and was preserved.

## Remaining questions
Wearer confirmation of relaxed neutral and small pitch movements remains pending. Zero passive pitch is not zero translational acceleration or altitude hold; inertia/gyroscopic coupling can still affect combined maneuvers. No automatic calibration from stable grip because quiet/stable intervals can be intentional corrective input. Data and reports remain ignored under artifacts; aggregate evidence summarized here.

## Suggested next prompt
Try the new advanced mode after comfortable calibration; hold relaxed neutral in calm/rising air, make small pitch inputs and a loop, then pull the recording and compare sustained wrist correction against the prior Magpie window.

## Build/install
Built successfully25.925s,82,057,947bytes. APK SHA256 `e30314baae432e513b5e84f58211c1b58ed07aeb3b142b650aa115800a679e56`; embedded source stamp `d2bd0906e733a31c1a0965908e00064c85f8c17411f872db8154a381e69bbc22` verified. XR preloads restored before build, incidental .utmp fingerprint restored after. MCP returned empty false but Unity PlayerBuildInfo confirms successful build. First installation attempt hit offline Quest; after wake request it reconnected. ADB install -r succeeded with app data preserved. Launched PID15102; captured PID-filtered startup log has no matched exceptions/Unity errors. Wearer validation of the new neutral remains pending.
