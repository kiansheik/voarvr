# Quest 3 / OpenXR on macOS

Setup baseline reviewed 2026-09-08; dashboard requirements, UI labels, supported package versions and store submission rules can change. This is local development setup, not a store-release configuration.

## Toolchain

Install the checked-in editor version, Unity 6000.6.0f1, through Hub with Android Build Support, Android SDK & NDK Tools and OpenJDK. In Unity Settings/Preferences > External Tools use the tools installed with Unity. Open `unity/`, let packages resolve and run **VoarVR > Configure Foundation**. Restart for input backend changes if requested.

The pinned OpenXR 1.18.0 package contains **Meta Quest Support** and Touch interaction profiles. No Meta All-in-One SDK, Oculus provider or Unity OpenXR Meta extension package is necessary for basic opaque VR/controller tracking. Add optional extensions only when their functionality is needed. The architecture does not require XR Interaction Toolkit for flight input.

## Android configuration and manual fallback

File > Build Profiles > Android > Switch Platform. Some Hub/editor UI versions expose a Meta Quest entry over Android. Inspect the Android target tab regardless.

| Setting | Foundation value / action |
| --- | --- |
| Player identifier | `com.voarvr.prototype` (provisional local identifier) |
| Scripting backend / architecture | IL2CPP / ARM64 |
| Input | Input System Package (New) only |
| Color space | Linear |
| Android graphics API | Vulkan; automatic API selection off |
| Minimum / target SDK | API 29 / highest installed (Automatic), for local development |
| Render pipeline | `Assets/Settings/VoarVRPipeline.asset` in Graphics and all Quality levels |
| URP settings | 4x MSAA, HDR off, scale 1; provisional, profile on device |
| XR Plug-in Management, Android | OpenXR enabled; Initialize XR on Startup enabled |
| OpenXR interaction profile | Oculus Touch Controller Profile |
| OpenXR feature | Meta Quest Support enabled; Quest 3 selected in its gear/settings device list |
| OpenXR rendering | Single Pass Instanced (the provider maps to its supported stereo path) |
| Build scene order | Bootstrap, CharacterSelect, BirdFlight |

These values are applied by `ProjectSetup.Configure`. If automated configuration fails, inspect the Console, then use the settings above as the manual fallback. Review **XR Plug-in Management > Project Validation**, Android tab; resolve errors before building and evaluate performance warnings. Do not blindly apply unrelated optional feature suggestions. Store API requirements can exceed local minimum settings and must be rechecked before submission.

## Headset authorization

Use a verified Meta developer account; complete any organization/team onboarding currently requested in the developer dashboard. Pair the headset in the Meta Horizon mobile app. Enable Developer Mode in the paired device's settings. Connect a USB data cable, unlock/wear Quest, then accept the USB debugging trust prompt for this Mac. The headset's developer/MTP notification settings may be needed to expose the prompt.

Use the adb from Unity's configured SDK or an existing platform-tools installation:

```sh
export ADB="/actual/Android/SDK/platform-tools/adb"
"$ADB" version
"$ADB" devices -l
```

`device` means authorized. `unauthorized` requires accepting the headset prompt; `offline` merits reconnect/restart troubleshooting. An empty list merits checking the cable, Developer Mode and USB connection. USB charging alone does not prove data access. Meta Quest Developer Hub is an optional device-management GUI.

## Build, run, observe

From Unity: Development Build, Run Device = your Quest, then Build And Run to `builds/quest/VoarVR.apk`. Both scenes must be enabled. From a shell with the editor closed:

```sh
python3 tools/scripts/unity.py build-quest
"$ADB" install -r builds/quest/VoarVR.apk
"$ADB" logcat -s Unity
```

For multiple devices use `"$ADB" -s SERIAL install -r builds/quest/VoarVR.apk`. Launch from the headset's developer/unknown-sources app library after adb installation. Build output is ignored. No production signing key is stored; Unity uses development signing for this prototype.

Current acceptance artifact (2026-09-12): the live Unity Editor produced `builds/quest/VoarVR.apk` in 91.360 seconds with BuildReport `Succeeded`, zero errors and five warnings. It is 82,339,095 bytes with SHA-256 `61a4b5ee5e4aca70ab32b82b7c7052dea4aabcfa0078e9b0a5c09cd82d8d36b7`; ZIP integrity and the Android signature pass. Manifest inspection confirms `com.voarvr.prototype`, debuggable, minimum SDK 29, target/compile SDK 36, `arm64-v8a`, OpenXR permissions and the VR headtracking feature. Its embedded source SHA-256 `7764cae054cd89b5b93a609cd983bdd309c2dd62c28ede5835daa8efc573fb10` matches current source. Exact XR preload subassets were restored after the build transient. Quest 3 remains `adb offline` and reconnect reports `Host is down`, so this artifact has not been installed, launched or worn-tested.

On device Auto selects XR. Physical controller position/orientation is read through Input System/OpenXR; torso-relative derivatives suppress body-turn and tracking-recovery spikes. A opens the diagram-led calibration coach and confirms a ready pose; B toggles view; X pauses; Y cycles weather; Left Menu pauses and opens the session menu. While supported, the left stick translates and the right stick turns the bird. Meta platform recenter pauses, recalibrates in place after stable tracking and returns to the session menu. The reserved system button is not directly bound. See [input contract](../design/INPUT.md).

Installed APKs persist locally after disconnect/reboot. Find **VoarVR** in **Library → Unknown Sources**, not the main app grid. If the tab is absent after installation, reopen Library. [Meta documents this location](https://developers.meta.com/horizon/documentation/android-apps/enable-developer-mode/).

Confirm head rotation/translation, left/right controller assignment, reasonable wing velocity signs, pause/calibration and consistent stereo rendering before tuning physical play. Direct physical wing mappings, supported walking/turning and camera/contact reactions remain provisional until worn comfort testing. The simulation uses swept scenery contact and safe contact landing; start in third person, use B to toggle, and raise/hold spread wings to brake before gentle contact. Right-stick click hides or shows optional flight gauges, while the in-world gameplay ribbon continues to show the current goal/course step plus typed moth totals and value.

The following gameplay is present in the current development APK but still requires installation and on-headset acceptance:

- Four moth types with distinct rarity, movement behavior and values (Sun 10, Moon 20, Ember 35, Crown 60), with counts/value/combo in the gameplay ribbon.
- Duck and Dragon use visible trigger-linked wing poses for left-trigger braking/flare and right-trigger tuck/dive.
- Local player profiles, finalized/interrupted session history and end-of-session movement results; the summary reports observed activity rather than imposing an exhaustion or stamina meter.
- Five 30-second obstacle courses with a 3-2-1 start, ordered tasks, local personal-best/leaderboard records and a required six-second rest before retry.
- Three noncombat aerial rivals outside ranked courses.
- Directional collision bump, camera/rig reaction, spatial contact sound, haptics and foliage disturbance, backed by solid tree branches and exact compound or mesh-derived profiles for tapered roofs, Ruin hex pillars, Rock, Spire and Log rather than invisible landmark rectangles.

## Mac testing boundary

Desktop Play mode supports synthetic/gamepad testing. A connected USB Quest does not turn macOS Unity Play mode into a supported Quest Link preview. Use standalone Android builds for this workflow; Meta's Windows Link tooling is a separate path. Hardware validation and GPU/frame-time profiling remain necessary even when EditMode and PlayMode tests pass.

## Official references

- [Unity Meta Quest Support and profiles](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.18/manual/features/metaquest.html)
- [Unity XR Plug-in Management](https://docs.unity3d.com/6000.6/Documentation/Manual/xr-plugin-management.html)
- [Meta headset developer setup](https://developers.meta.com/horizon/documentation/unity/unity-env-device-setup/)
- [Meta Horizon Link requirements](https://developers.meta.com/horizon/documentation/unity/unity-link/)

## Infinite-world iteration validation

Validate third-person default/A coach flow and view-preserving Meta recenter, modest calibrated head-down descent, unchanged Duck effort and longer Dragon glide. Fly across several chunk boundaries, glance walls, rocks, trunks and branches, and confirm that contact location/direction matches the visible mesh closely enough while foliage responds without becoming solid. Verify directional bump, camera/rig reaction, spatial sound and haptics at tolerable strength. Brake onto ground and an elevated canopy/roof, walk and turn with separate sticks, then flap off again. Follow upward-moving traces through a drifting lift feature; cyan horizontal wind alone does not promise lift. Watch for hitches and camera clipping. Automated scene/physics tests and adb launch do not establish worn comfort or sustained Quest frame rate. See the [current handoff](agent/session-handoffs/2026-09-09-infinite-world-landing-v1.md).

## Flight Game playtest

Install `builds/quest/VoarVR.apk` using `python3 tools/scripts/quest.py run` with Quest awake on Wi-Fi. The sideload remains installed locally under Apps → Unknown Sources, titled VoarVR; it is not a store-library listing. Choose a player, activity and species. First verify the A calibration diagram/pose with Duck, then Dragon/Magpie trigger animation and flight feel. Use Meta recenter for in-place calibration and Left Menu for the session menu. Hold left-stick0.7s without grips for optional Acrobatic; use B third-person for body awareness. Quiet wings plus intentional circling in golden rising air should gain height. Test typed moth catches, persistent ribbon legibility with gauges hidden, supported walking/turning, rival contact and tight tree/building/rock collisions. Run all five courses, confirm countdown/task direction/result/leaderboard behavior and observe the required six-second rest. Finish a session from the menu and verify its movement summary and player history after returning to selection. Record a telemetry marker with both grips+left click. Current desktop validation does not establish headset frame rate, physical comfort, collision intensity, UI readability or fitness-metric usefulness.
