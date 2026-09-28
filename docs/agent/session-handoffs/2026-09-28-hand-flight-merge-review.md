# Hand-flight merge and competition readiness review

> Historical merge-only phase. Later on September28 the user confirmed US/Start membership and Unity was reconnected. Executed tests, v4 repairs and current readiness supersede the pending items below: [preflight handoff](2026-09-28-competition-preflight.md).

## Goal

Resolve the current feature-branch conflicts, read the latest implementation and competition plan, and leave an accurate, actionable path to a strong hackathon candidate.

## Files inspected

- Required agent docs, September 12 and September 27 handoffs; competition brief, hand design, VR setup and package manifest.
- Merge base `72a714a`, feature head `7798d3a`, incoming main `183557f`, index conflict stages and remote refs.
- `FlightInputFrame`, `BirdFlightController`, `BirdFlightDriver`, `FlightActionGate`, `HandTrackingSafetyTests`, `FlightSessionTracker`, telemetry capture/replay/reader paths and Unity tooling/reconnect helper.
- Unity instance resources and project-relative Editor log. Official Devpost rules/overview/judging and Meta Wide Motion Mode documentation, checked September 28.

## Files changed

- `docs/agent/log.md`: combine both sides of the sole conflict in date order, then record this pass.
- `docs/competition/meta-vr-start-2026.md`: readiness table, eligibility question, access/freeze requirements, video wording, description target and draft-only narrative qualification.
- `design/HAND_FLIGHT.md`: explicit OVRPlugin/API compatibility, session/replay motion provenance, takeoff/statistics acceptance and removal of an unrequested commit instruction.
- Agent current state, index, repo map and open questions; `docs/VR_SETUP.md` historical artifact wording; this handoff.
- No new runtime, SDK, scene, prefab or asset edits. Existing staged merge changes, including Duck source and `.utmp` fingerprint, are preserved.

## Commands run

- Targeted `rg`, file reads, `git status --short`, `git status --branch`, `git ls-files -u`, merge-parent/base and targeted `git show`/`git diff` inspection.
- `git ls-remote origin refs/heads/meta-vr-start-2026-hand-flight refs/heads/main`: remote refs match the merge's feature/main heads.
- `python3 -m unittest discover -s tools/scripts -p 'test_*.py'`: 21/21 pass.
- `python3 tools/scripts/check_repo.py`: exits 1 with 24 unresolved GUID findings, all under the documented unused TMP import.
- Unity MCP resources: instances, editor state, project info and custom tools; process/project lock inspection confirms the editor is open.
- Conflict-marker/local-link inspection, `git diff --check`, `git diff --cached --check`; `git add -- docs/agent/log.md` to mark resolution.

## What worked

- Both histories survive the log resolution; no unmerged index entries remain after staging. The merge remains uncommitted; no push was made.
- Working-tree `git diff --check` and local Markdown-link checks pass. Final Git status says all conflicts are fixed; only the resolved log was staged by this pass, leaving the other documentation edits reviewable in the worktree.
- Independent source audit found no merge-specific runtime defect: `MotionEstimated`, both controller guards and the unchanged hand safety regression coexist with incoming ground-turn/session/course work. Struct normalization/action gating retain the flag.
- The competition plan focuses on compact Magpie Route Home. Existing extras can support replay, but every option exposed in the competition build must work with hands.
- Readiness distinguishes implemented source, executed tests, historical controller evidence and future hand/wearer evidence. Submission copy requires final-build verification.

## What failed

- Sandboxed Git status initially failed when LFS attempted to create temporary metadata; permitted escalation succeeded.
- Unity 6000.6.0f1 is open for `voarvr/unity`, but MCP reports zero instances and `no_unity_session`. No fresh Unity Test Runner run was possible through the available connection. Batch tests were not launched against the open project. This pass does not certify Unity compilation or the new regression.
- The 24 existing TMP reference findings remain. No APK was built/installed, and no current Quest connectivity, hand tracking, wearer experience or performance was measured.
- `git diff --cached --check` reports 21 trailing-whitespace lines in seven incoming Unity-generated `.meta` files. Each file is byte-identical to incoming main; they were preserved under the instruction not to edit generated files. This is separate from the clean documentation diff and resolved conflict state.

## Remaining questions

- Entrant eligibility and Start membership are unconfirmed; see the [competition brief](../../competition/meta-vr-start-2026.md#entrant-eligibility--user-confirmed). User clarification was requested; environment timezone is not residency evidence.
- SDK/hand adapter, source transitions, compact calibration, hands-first UI and telemetry v4 remain unimplemented. Verify actual imported SDK/API support before integration.
- Session activity/wingbeat accounting and v3 replay do not carry estimated-motion provenance. Address these before enabling hand input; no production provider currently sets the flag, so these are prospective integration gaps.
- A supported-perch estimated-downstroke test should cover takeoff in addition to the existing active-force comparison. Current branch Unity tests remain pending.

## Suggested next prompt

Reconnect the existing editor through **VoarVR → Tools → Reconnect Installed MCP Session**, then verify this merged branch with HandTrackingSafetyTests and the flight, ground and telemetry replay suites. If the editor is closed, the reproducible full commands are `python3 tools/scripts/unity.py test-edit` and `python3 tools/scripts/unity.py test-play`. Preserve XR preload settings. Next, integrate the minimum compatible Meta Core/Interaction packages as a separate reviewable change; retain OpenXR. Implement the hand adapter, confidence/source continuity and provenance-aware metrics/v4 replay with deterministic dropout/recovery tests, then complete compact hands-first UI before changing Route Home pacing. Record exact SDK versions, tests and actual Quest evidence; do not commit or publish unless requested.
