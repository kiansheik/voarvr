# VoarVR

Bird flight simulation / fitness game for Meta Quest 3, with a future non-VR gamepad mode. Unity is the runtime engine; Blender is the source DCC tool. **Current status: foundation / prototype stage.**

The prototype lets you choose a rigged duck or a broad-winged dragon, calibrate your arm span, flap and glide through an endless streamed spirit-city/forest world, and follow visible moving air. It includes controller-to-wing IK, third-person by default with a first-person toggle, swept scenery collision, contact landings and deterministic flight/world tests. Fitness scoring, wildlife and production environment art are future work.

**Validation status:** Unity 6000.6.0f1 and Blender 5.2.1 LTS are the tested local baselines. Editor compilation, deterministic simulation/rig tests, Blender static and rigged FBX roundtrips and desktop visual review pass. Android tooling is installed; the final APK build result and Quest hardware boundary are recorded in [current state](docs/agent/current-state.md). Nothing was committed or pushed.

## Prerequisites on a new Mac

| Tool / access | Purpose |
| --- | --- |
| macOS MacBook | Development host; use the Unity/Blender installer matching Apple Silicon or Intel. |
| Git and Git LFS | Source control and binary source assets. |
| Python 3.10+ | Dependency-free repository tools. Blender uses its own bundled Python. |
| Unity Hub and a licensed Unity Editor | Install the repository-pinned **Unity 6000.6.0f1** and activate an appropriate Unity license. |
| Android Build Support | Install as a module of that exact Unity Editor. |
| Android SDK & NDK Tools, OpenJDK | Select both nested Hub module options; prefer Unity's bundled versions. |
| Blender **5.2.1 LTS** | Tested authoring/export baseline for this prototype. |
| Meta Quest 3, controllers, USB data cable | Real standalone VR testing. |
| Meta developer access, Developer Mode, USB authorization | Permit installing and debugging your own APKs. |
| Optional adb / Meta Quest Developer Hub | Check the device and install APKs. Unity includes adb in its SDK. |
| Optional Unity/Blender MCP integration | Editor inspection and agent automation; never required to build. |

The checked-in `ProjectVersion.txt` is authoritative for Unity. Blender 5.2.1 LTS is the validated local authoring baseline. Review upgrades deliberately with package and test changes together.

## Install and configure

All repository commands below run from the repository root. Install [Homebrew](https://brew.sh/) if you want to use the brew commands; direct vendor installers also work.

```sh
# Git may already be provided by Apple's Command Line Tools.
git --version
# If Git is missing, install Apple's tools:
xcode-select --install

brew install git-lfs python
git lfs install
git lfs version
python3 --version
# After cloning, retrieve any binary sources (none are included initially).
git lfs pull
```

1. Install [Unity Hub](https://unity.com/download), sign in and activate your license.
2. Install Unity 6000.6.0f1 through Hub. Select the matching Mac architecture and **Android Build Support**, including **Android SDK & NDK Tools** and **OpenJDK**.
3. Install Blender 5.2.1 LTS. Keep authored `.blend` files in `blender/source/`, outside Unity's Assets tree.
4. Set executable paths for this shell. These are **examples**, not universal installation paths. Use Hub's installed-editor location and your actual Blender app location.

```sh
export UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity"
export BLENDER="/Applications/Blender.app/Contents/MacOS/Blender"
test -x "$UNITY_EDITOR"
"$BLENDER" --version
python3 tools/scripts/doctor.py
python3 tools/scripts/check_repo.py
python3 -m unittest discover -s tools/scripts -p 'test_*.py'
```

## First Unity launch

1. Hub > Projects > Add > select this repository's **`unity/`** directory. Do not create another Unity template inside it.
2. Wait for package resolution and import. Package Manager > In Project should show Input System, Universal RP, XR Plug-in Management, OpenXR Plugin and Test Framework. Versions are in `unity/Packages/manifest.json`; URP 17.3 and Test Framework 1.6 are editor-core packages. No Asset Store download, Meta All-in-One SDK or XR Interaction Toolkit is needed.
3. Run **VoarVR > Configure Foundation**. This creates URP renderer/pipeline and Android XR settings assets, sets Input System-only input, ARM64/IL2CPP, Vulkan, Linear color, 4x MSAA without HDR, and the two build scenes. Restart the editor if requested for the input backend. The command preserves the authored scenes; rerunning it reapplies foundation player/XR defaults.
4. Verify Graphics and Quality settings reference `VoarVRPipeline`. Confirm the Console has no compiler/import errors.
5. Run **VoarVR > Configure Characters** after regenerating a character FBX. Open `Assets/Scenes/Bootstrap/Bootstrap.unity` and press Play to select Duck or Dragon. Without a gamepad, Auto uses deterministic synthetic glide. On the Bird object, choose Synthetic and change Gesture to inspect flap, bank, tuck/dive, flare, stall recovery and tracking loss.
6. On Quest, spread both tracked arms comfortably and press **A** to restart and calibrate together (level 0.7–2.2 m spread). **B** switches viewpoint (third-person is the default and A restores it); **Y** cycles Assisted, Touring, Wild and Still Air; **X** pauses. Raise and hold spread wings to brake for a gentle landing; left trigger assists braking; right trigger tucks. Holding Meta to recenter recalibrates **in place** after you hold the spread still. **Left Menu** returns to character selection. Dragon is tuned for slow moderate wingbeats and glide breaks; extra-fast flapping reaches a force ceiling. The HUD shows wind-relative knots, ground clearance, climb/sink and bird attitude. Follow gold rising-air streams; when CLIMB turns positive, spread your wings and circle gently to stay in the lift. Wingtip wakes shift cyan to violet as airspeed increases.

To configure from the command line instead, **close the editor for this project**:

```sh
python3 tools/scripts/unity.py configure
```

The first import generates `Packages/packages-lock.json`, additional `ProjectSettings/` files and settings assets under `Assets/Settings/` and `Assets/XR/`. These are reviewable project sources: preserve them and their `.meta` files for a future authorized commit. The lockfile is intentionally not fabricated before Unity resolves dependencies. Library/Temp/logs remain ignored. See [development](docs/DEVELOPMENT.md).

## Quest 3 setup and deployment

1. Register and verify a Meta developer account. Meta's current onboarding may also ask you to create/join a developer organization or team; complete the requirements shown in its dashboard. Account requirements and mobile UI change over time. Follow [Meta's current device setup](https://developers.meta.com/horizon/documentation/unity/unity-env-device-setup/).
2. Pair Quest 3 in the Meta Horizon phone app using that account. Open the paired headset's settings and enable **Developer Mode**.
3. Connect a USB **data** cable, wear/unlock the headset, and accept **Allow USB debugging** for this Mac. If the prompt is absent, check the headset's developer settings / MTP notification option and reconnect.
4. Optionally check adb. Set `ADB` to the binary inside your Hub-installed Android SDK, or use adb already on PATH. Locate the actual SDK in Unity > Settings/Preferences > External Tools; do not assume every SDK is at the example path.

```sh
export ADB="/actual/Unity/AndroidPlayer/SDK/platform-tools/adb"
"$ADB" version
"$ADB" devices -l
```

The headset should report `device`, not `unauthorized` or `offline`. For multiple devices, use `-s SERIAL` for install/log commands.

5. In Unity, File > Build Profiles > Android (or Meta Quest if exposed by that editor) > Switch Platform. In XR Plug-in Management's **Android** tab, verify OpenXR and Initialize XR on Startup. Under OpenXR, verify Meta Quest Support, Quest 3 in its device list, and Oculus Touch Controller Profile. Run Android **Project Validation** and resolve errors. Detailed checks and manual fallback are in [VR setup](docs/VR_SETUP.md).
6. Enable Development Build, verify Bootstrap then BirdFlight in the scene list, select the authorized Quest as Run Device, then **Build And Run**. Save to `builds/quest/VoarVR.apk` under the repository, outside `unity/Assets/`.

Alternatively, close the Unity editor and build/install explicitly:

```sh
python3 tools/scripts/unity.py build-quest
"$ADB" install -r builds/quest/VoarVR.apk
# Launch VoarVR from the headset's developer / unknown-sources app library.
"$ADB" logcat -s Unity
```

macOS Editor Play mode tests the desktop/synthetic path. Meta Quest Link's Windows PC VR workflow is not the Mac headset preview route; use Android builds for Quest checks. Verify head tracking, both controller poses, reset/pause, frame timing and comfort on hardware before adding physical exertion mechanics.

## Blender setup and use

Create metric meshes and an optional armature in an `EXPORT` collection, one Blender unit per meter, with applied object transforms. Static objects remain unparented; skinned meshes use one exported armature parent/modifier and normalized one-to-four-bone weights. See [pipeline conventions](docs/BLENDER_PIPELINE.md).

```sh
"$BLENDER" --background blender/source/OakPerch.blend --python-exit-code 1 --python blender/scripts/validate_asset.py
"$BLENDER" --background blender/source/OakPerch.blend --python-exit-code 1 --python blender/scripts/export_asset.py -- --output blender/exports/OakPerch.fbx
# Explicitly replace a previous export by appending --overwrite.
# Disposable generated fixture; does not require an existing .blend file:
"$BLENDER" --background --factory-startup --python-exit-code 1 --python blender/scripts/smoke_test.py
```

`blender/generated/` and `blender/exports/` are ignored working output. Promote approved FBX exports into `unity/Assets/Art/Models/`, import at scale 1, check a one-meter reference and preserve the generated `.meta`. Approved runtime models are trackable with LFS; never copy `.blend` into Assets or require Unity to run Blender implicitly.

## Optional agent-assisted Unity and Blender setup

MCP lets an agent talk to a running editor through an installed integration. A Unity integration may inspect scenes/components, edit GameObjects, run tests, inspect the Console and capture screenshots. A Blender integration may inspect the scene and execute `bpy` operations. Capabilities vary by server/version.

No MCP package, server or port is pinned here. Select a maintained integration compatible with your editor and agent client; use that integration's installation instructions to install its editor side, start its local server and connect your client. Confirm read-only scene inspection before edits, then use the checked-in workflows and validate the result. Keep connection settings/credentials local. The operational contract is in [AGENTS.md](AGENTS.md); all work remains possible through scripts and normal editors.

## Running tests

In Unity: Window > General > Test Runner. Choose **EditMode > Run All** for deterministic simulation tests, then **PlayMode > Run All** for Bootstrap/scene wiring. No headset is needed. Run Configure Foundation once before PlayMode tests.

From a terminal, close this project's editor first:

```sh
python3 tools/scripts/unity.py test-edit
python3 tools/scripts/unity.py test-play
```

Each run writes a fresh log and test XML under ignored `artifacts/unity/`. The wrapper fails on a nonzero editor exit, missing/empty results, failed or skipped tests. Headless tests do not validate rendering quality or headset behavior. Offline checks work before editor installation:

```sh
python3 tools/scripts/check_repo.py
python3 -m unittest discover -s tools/scripts -p 'test_*.py'
git diff --check
git status --short
```

## Repository hygiene and navigation

Track first-party code, scene/prefab/material text, Unity `.meta`, package manifest/real resolved lockfile, ProjectSettings, docs, Blender scripts and curated source assets. Use LFS for `.blend`, FBX and heavyweight layered/HDR source images/audio; tiny source text and ordinary PNG/JPEG files are not blanket-LFS. Review size before adding large textures. Never add paid/downloaded asset packs or generated Unity folders.

- [Architecture](docs/ARCHITECTURE.md) · [Development](docs/DEVELOPMENT.md) · [VR setup](docs/VR_SETUP.md)
- [Blender pipeline](docs/BLENDER_PIPELINE.md) · [Asset guidelines](docs/ASSET_GUIDELINES.md)
- [Roadmap](docs/ROADMAP.md) · [Future Switch notes](docs/SWITCH_NOTES.md)
- [Game design](design/GAME.md) · [Agent index](docs/agent/index.md) · [Tools](tools/README.md)

The installed **VoarVR** app stays on Quest after disconnect/reboot. Open **Library → Unknown Sources → VoarVR**; sideloaded apps do not appear in the main app grid. If the tab is missing, reopen Library. See [Meta’s current instructions](https://developers.meta.com/horizon/documentation/android-apps/enable-developer-mode/).

## Flight Game v1 development slice

Free Flight remains the default. Character select now offers Training and Skyward Expedition; completing Skyward saves a local best and unlocks Ridge Journey. Hold left-stick click without grips for0.7s to toggle optional Acrobatic flight. [Design and controls](design/FLIGHT_GAME_V1.md); [validation and known gaps](docs/development/flight-game-v1-validation.md). No current headset-performance acceptance claim.
