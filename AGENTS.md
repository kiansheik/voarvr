# Agent operating manual

## Before editing

1. Read [index](docs/agent/index.md), [current state](docs/agent/current-state.md), [repo map](docs/agent/repo-map.md) and [open questions](docs/agent/open-questions.md).
2. Inspect `git status --short`. Preserve existing files and unrelated work.
3. Use `rg --files`, `rg`, `git grep` and targeted reads before opening large files. Exclude Library and other generated output from searches.
4. Code, tests and checked-in settings are authoritative. `docs/agent/` is compiled memory; correct it when it disagrees with source. State uncertainty rather than inventing behavior.

## Layout and permitted work

- `unity/Assets/Game/`: first-party C# under `VoarVR.*`. Flight simulation consumes `IFlightInput`; device adapters stay in Input. Editor tooling has a separate editor-only assembly.
- `unity/Assets/Art/`, `Audio/`, `Scenes/`: curated runtime assets and their metadata.
- `unity/Packages/`, `ProjectSettings/`: dependencies and shared editor/build settings. Review upgrades explicitly.
- `blender/source/`: authored `.blend` sources (LFS); `blender/scripts/`: reproducible `bpy` operations.
- `blender/generated/`, `blender/exports/`, `builds/`, `artifacts/`: ignored output, regenerated as needed.
- `tools/`, `docs/`, `design/`: development tooling, operational docs, intended experience.

Agents may modify first-party sources appropriate to the task. Do not edit Unity Library, Temp, Logs, obj, PackageCache, UserSettings or other engine-generated caches. Do not alter `Assets/ThirdParty/` unnecessarily, vendor packages, add paid/large downloaded assets, configure Switch SDKs or add speculative systems. Do not commit or push unless explicitly requested. Do not run destructive commands without explicit user approval. Keep diffs small and reviewable.

## Unity scenes and prefabs

Use the Unity editor API or an available MCP integration for ongoing scene/prefab edits. Preserve `.meta` GUIDs when moving assets; never recreate GUIDs to fix a broken reference. Inspect components and existing connections first. Avoid bulk YAML rewrites. The bootstrap scenes are already checked in; Configure Foundation never regenerates them.

When automating recurring editor operations, add explicit menu/CLI methods under `Game/Editor/`. Do not run mutations on every import. Save scenes/assets, inspect Console output, run the narrow relevant tests and visually check scene changes. Do not call a static source scan a successful Unity compilation. The initial project settings are expanded by Unity on first import; preserve the real lockfile and project settings, not Library.

## Unity MCP

We intend to use an interchangeable Unity MCP integration for scene inspection, GameObject/component edits, editor tests, Console inspection and screenshots where supported. Check which tools are actually available and which Unity instance they target before use. Keep server configuration local and use documented commands when MCP is absent. No runtime code should depend on MCP. Record editor version, scene path, action, saved files and test/Console result. Screenshots complement tests; they do not prove Quest performance.

## Blender MCP / bpy

Prefer checked-in `bpy` scripts and headless commands over repetitive hand edits. Use Blender MCP for inspection/iteration if installed; transfer repeatable operations into scripts. Validate before export. The initial pipeline handles static meshes only, leaves source files intact during export and fails on invalid names/transforms. Do not save over an authored .blend without task authorization. FBX is the deliberate bridge into Unity; Unity does not import .blend sources directly.

## Validate and hand off

From the root:

```sh
python3 tools/scripts/check_repo.py
python3 -m unittest discover -s tools/scripts -p 'test_*.py'
# Close this project's Unity editor before batch commands; set UNITY_EDITOR.
python3 tools/scripts/unity.py test-edit
python3 tools/scripts/unity.py test-play
# For Blender pipeline edits, set BLENDER:
"$BLENDER" --background --factory-startup --python-exit-code 1 --python blender/scripts/smoke_test.py
git diff --check
git status --short
```

Run the narrowest useful checks. Use Unity Test Runner when the editor is open. Run a Quest development build for XR/build configuration changes when tools are installed, and report separately whether it was tested on hardware. Do not leave known compilation errors. If editor/license/modules/hardware are absent, record the exact validation gap and reproducible command; never claim those checks passed.

After significant work update `docs/agent/current-state.md`, `docs/agent/log.md` and a new dated file in `docs/agent/session-handoffs/`. Each handoff includes Goal, Files inspected, Files changed, Commands run, What worked, What failed, Remaining questions and Suggested next prompt. Keep notes concise and linked; summarize broad exploration in the repo map or a domain page.

## Player-facing quality loop

For substantial visual/player-facing work, follow [quality-loop.md](docs/agent/quality-loop.md): builder -> independent visual, VR/player and Quest technical critics -> synthesis -> a small revision -> fresh evidence. Use separate agents; each inspects the current artifact before seeing other conclusions. Findings need severity, concrete evidence, objective-versus-preference labels, 1–10 rubric scores and actionable fixes. Use at most three rounds, stop when the documented gates are met, and never treat screenshot scores as Quest hardware validation. Trivial docs and small deterministic changes need only their relevant checks.
