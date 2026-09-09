# Quality and critique loop

Use this for substantial player-facing/visual work. For trivial documentation edits or small deterministic fixes, use the narrow relevant checks instead.

1. **Builder:** restate the brief and implemented scope; run deterministic checks; capture current Game/viewport screenshots plus runtime observations. Put disposable evidence in `artifacts/reviews/<task>/round-01/`.
2. **Independent critics:** spawn separate agents for the three roles below. Give them the brief, design/source paths, evidence and measured limits. Each must inspect the artifact/evidence before seeing another critic's conclusions. Critics do not mutate the live editors while a builder/test run is using them.
3. **Synthesis:** collect all reports, deduplicate defects, resolve contradictions against the brief and measured evidence, distinguish objective defects from preferences, and choose the smallest high-value revision set. Explain deferred findings. Do not blindly chase scores.
4. **Revision:** implement the justified set, rerun affected deterministic tests and capture fresh evidence in `round-02/`. Never rescore old screenshots after code/scene changes.
5. **Repeat when useful:** reuse critics for an independent review of the fresh evidence. Normal maximum is three critique/revision rounds (`round-01/`, `round-02/`, `round-03/`). If the cap is reached with defects, report them instead of calling the work approved.

## Critic roles

| Role | Relevant rubric dimensions |
| --- | --- |
| Visual / art direction | Composition, silhouette/readability, scale, lighting, material coherence, scene hierarchy, naturalism/stylization consistency, visible artifacts |
| VR / player experience | Motion, bird-scale perception, spatial readability, camera, comfort, input/action feedback, interaction readability, likely play experience |
| Technical / standalone Quest | Geometry, materials, shaders, transparency, textures, LOD, physics cost, draw-call concerns, runtime errors, architecture/performance risk |
| Optional brief compliance | Requested outcomes, missed requirements, invented scope, regressions against design docs |

All critics identify concrete issues with severity **blocker / high / medium / low**, classify **objective defect / subjective preference / unverified risk**, score relevant dimensions **1–10** and propose specific actionable fixes. State what cannot be judged from available evidence; mark it unverified instead of inventing a score. Technical reviewers use source and measurements as well as images. Player reviewers should inspect recorded motion/telemetry when available; still images cannot establish flight feel.

## Stop conditions and safeguards

Stop early when no blocker/high defects remain, deterministic validation passes, assessable rubric dimensions are at least 8/10, and another round would mainly create subjective churn. Keep unverified hardware gates visible; an 8/10 desktop/readability score is not an 8/10 Quest comfort score. At the round cap, finish with explicit remaining issues/risks and the next useful validation rather than cosmetic churn.

Screenshots never replace an APK running on actual Quest hardware. Scores cannot override performance, accessibility, comfort, maintainability or the user's brief. A prototype can pass a desktop review while remaining unvalidated on Quest. Do not add unrelated systems, polish or asset packs to inflate scores.

## Reusable critic prompt

> Review this artifact independently as **[role]**. Read the task brief, relevant design/source files and evidence under **[round path]**. Inspect the images and available runtime observations before reading other reviews; do not access other critics' conclusions yet. Do not edit the repository or mutate Unity/Blender. Separate objective defects, subjective preferences and unverified risks. For every finding provide severity (blocker/high/medium/low), concrete evidence/location, player or technical impact and a specific fix. Score relevant rubric dimensions 1–10 with a short rationale; mark dimensions that require unavailable hardware/motion evidence unverified. Propose no more than three priority revisions and explain whether another round would be useful. Honor the current prototype scope.

## Suggested evidence/report format

- `brief.md`: task scope, controls, known limits and evidence inventory.
- Game View / Blender viewport screenshots, measured state snapshots and test result JSON/logs.
- `visual.md`, `player.md`, `technical.md` (optional `compliance.md`): independent reports.
- `synthesis.md`: deduplicated decisions, prioritized revisions, deferred issues and stop rationale.
- Fresh evidence and reassessment in the next round directory.

Link the evidence and final outcome from the dated session handoff. `artifacts/` remains ignored; durable behavioral decisions belong in design/architecture/current-state docs.
