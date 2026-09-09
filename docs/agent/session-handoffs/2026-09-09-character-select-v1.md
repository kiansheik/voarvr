# Character select v1 - 2026-09-09

## Goal

Turn the single hardcoded duck profile into a data-driven stat card any species can use, add a
second species (a huge, Shrek's-Dragon-style dragon) built with the same Blender pipeline as the
duck, and add a character-select screen in front of the flight scene so a player picks between
them before spawning. Groundwork for choosing among more species later (e.g. a hummingbird).

## Files changed

- New runtime: `Assets/Game/Flight/BirdCharacterDefinition.cs` (ScriptableObject stat card:
  Size/Speed/Power/Agility/Weight, `BuildProfile()` bakes into `BirdFlightProfile`),
  `Assets/Game/Flight/CharacterSelection.cs` (static cross-scene pick, consume-once),
  `Assets/Game/UI/CharacterSelectController.cs` (builds its own uGUI Canvas at runtime from
  every `BirdCharacterDefinition` under `Resources/Characters`).
- Edited runtime: `BirdFlightDriver.cs` (spawns the selected character's rig and profile at
  `Start()`, replacing anything editor-baked into the scene), `BirdRigDriver.cs` (rest-pose
  wing-target span is now a field, `restArmSpan`, instead of a hardcoded `.56f`, and rebuilding
  the wing IK state is a public `Configure()` instead of only happening in `Awake()`), `Bootstrap.cs`
  (routes through `CharacterSelect` before `BirdFlight`).
- New editor tool: `Assets/Game/Editor/CharacterCatalogSetup.cs` (`VoarVR/Configure Characters` -
  fixes FBX import settings and wires each definition's `RigModel` reference from
  `ModelAssetPath`, the same importer overrides `DuckSetup` already applied inline).
  `ProjectSetup.Scenes` and `EditorBuildSettings.asset` gained `Assets/Scenes/Menu/CharacterSelect.unity`
  between Bootstrap and BirdFlight; `DuckSetup`'s scene-index check was bumped to match.
- New art: `blender/scripts/create_dragon.py` (same EXPORT/bone-naming contract as
  `create_duck.py`: Root + Left/RightUpper/Forearm/Hand/Tip), `blender/source/DragonV1.blend`,
  `unity/Assets/Art/Models/Dragon.fbx`, `Assets/Art/Materials/DragonPalette.mat` (reuses the
  existing generic `VoarVR/DuckVertex` vertex-color shader).
- New data: `Assets/Resources/Characters/{Duck,Dragon}.asset`. Duck's five stats are all at
  their midpoint by construction, so `BuildProfile()` reproduces `BirdFlightProfile.Duck()`
  exactly; Dragon is Size 2 / Speed .55 / Power .85 / Agility .3 / Weight .7.
- Tests: `BootstrapSmokeTests` now waits for `CharacterSelect` to load, clicks the first
  generated card's button, then asserts the existing BirdFlight checks.
- Docs: this handoff, `current-state.md`, `open-questions.md`.

## Design notes

- Stat mapping is `Size` (wing/drag area scale with the square, mass with the cube - so a small
  Size naturally needs more frequent flapping without that being its own stat), `Speed`
  (`MaxStrokeSpeed`/`InitialSpeedMps`), `Power` (`StrokeForcePerSpeedSquared`), `Agility`
  (roll/pitch limits and attitude response time), `Weight` (mass multiplier and stall speed).
  Every stat's formula is centered so 0.5 (or Size 1) reproduces the exact Duck constant it
  replaces - verify this stays true before changing any Lerp bound.
- Bone-name and `"*Face"`-suffix renderer contracts are unchanged from the duck; a new species
  only needs a Blender script producing that shape plus one `BirdCharacterDefinition.asset`. No
  prefab asset was introduced - `RigModel` references the FBX's imported root GameObject
  directly (build-safe; no `AssetDatabase` at runtime), following how `DuckSetup` already treated
  `Duck.fbx` as instantiable.
- `CharacterSelection.Chosen` is deliberately a bare static field, cleared the instant
  `BirdFlightDriver.Start()` reads it, so a PlayMode test (or an editor Play button press) that
  loads `BirdFlight` directly without visiting the select screen always falls back to Duck rather
  than leaking a previous test's pick.
- The select screen has no scene-authored UI at all; `CharacterSelectController` builds Canvas,
  cards and stat bars from `Resources.LoadAll<BirdCharacterDefinition>` in code. Adding a third
  species to the screen is only ever a new `.asset`.

## Commands run

- Blender 5.2.1 LTS headless: ran `create_dragon.py` (9 bones, 3,816 faces, exported
  `Dragon.fbx`), then a one-off roundtrip check mirroring `rig_smoke_test.py` - nine-bone import,
  vertex count preserved, `LeftHand` rotation deforms `DragonWings` (max 0.20 m, well above the
  0.02 m threshold the duck check uses), wingspan 2.89 m. Rendered two EEVEE preview stills to
  confirm the silhouette reads as a dragon (horns, back spikes, segmented tail with a fin,
  membrane wings, tucked legs) before wiring it into Unity.

## What failed / was unavailable

- No live Unity MCP connection this session (the Editor's MCP-for-Unity server was running
  locally, but this CLI session had no MCP client configured), so none of the C#/scene/asset
  changes above were verified inside the actual Editor - no compile check, no Play mode run, no
  test run. Everything was hand-authored as ForceText YAML/C# following the existing file
  conventions exactly (reused GUIDs are documented in this handoff's diff), but that is a real
  gap versus the duck session's live-editor verification.

## Known gap

`BirdTrackingCalibration` (`WingRigSolver.cs`) - the XR/tracked-input calibration that maps a
real human arm span onto the rig - still hardcodes a duck-scale `.56f` neutral-target baseline
and a `DuckSpanMeters = 1.12f` motion-scale constant, independent of the `restArmSpan`
parameterization this session added to `BirdRigDriver`. That field only governs the *untracked*
fallback pose (gamepad/synthetic input, and the idle pose before the first `Present()` call).
With tracked (XR) input, a selected Dragon's wings are still driven toward the duck-sized
baseline plus the calibrated delta, so they will not extend to the dragon's actual ~1.1 m bone
length even at full arm spread - the IK solver clamps reach so this cannot break anything, it
just means the dragon's wings read as more folded than they could be in XR. Fixing this means
deciding whether a giant character's tracked wing motion should scale 1:1 with real arms or
exaggerate to match its wingspan - a design call, not one to make unilaterally here.

## Remaining questions

- Run `VoarVR/Configure Characters` in the open Editor, confirm no compile errors, then run the
  EditMode and PlayMode suites (`BootstrapSmokeTests`, `DuckRigTests`, `DuckFlightTests`,
  `FlightControllerTests`) to catch anything the lack of live verification missed.
- Look at the dragon in the Editor/Play mode next to the duck at actual scale and decide whether
  the proportions (long neck, ~2.9 m wingspan) read well before treating it as final art.
- Decide whether the character-select screen needs a "Back" affordance once there is anywhere to
  go back to, and whether XR input (not just mouse/gamepad via the new Input System UI module)
  needs to drive card selection for the Quest build.

## Suggested next prompt

"Open the BirdFlight project, run `VoarVR/Configure Characters`, fix anything that doesn't
compile or import cleanly, then run the full EditMode/PlayMode suite and report pass/fail."
