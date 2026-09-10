# Quest 3 / OpenXR on macOS

Setup baseline reviewed 2026-09-08; dashboard requirements, UI labels, supported package versions and store submission rules can change. This is local development setup, not a store-release configuration.

## Toolchain

Install Unity 6000.3.21f1 through Hub with Android Build Support, Android SDK & NDK Tools and OpenJDK. In Unity Settings/Preferences > External Tools use the tools installed with Unity. Open `unity/`, let packages resolve and run **VoarVR > Configure Foundation**. Restart for input backend changes if requested.

The pinned OpenXR 1.16.1 package contains **Meta Quest Support** and Touch interaction profiles. No Meta All-in-One SDK, Oculus provider or Unity OpenXR Meta extension package is necessary for basic opaque VR/controller tracking. Add optional extensions only when their functionality is needed. The architecture does not require XR Interaction Toolkit.

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
| Build scene order | Bootstrap, BirdFlight |

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

On device Auto selects XR. Physical controller position/orientation is read through Input System/OpenXR; torso-relative derivatives suppress body-turn and tracking-recovery spikes. A restarts and calibrates together; B toggles view; X pauses; Y cycles weather; left Menu returns to character selection. Meta platform recenter recalibrates in place after stable tracking. The reserved system button is not directly bound. See [input contract](../design/INPUT.md).

Installed APKs persist locally after disconnect/reboot. Find **VoarVR** in **Library → Unknown Sources**, not the main app grid. If the tab is absent after installation, reopen Library. [Meta documents this location](https://developers.meta.com/horizon/documentation/android-apps/enable-developer-mode/).

Confirm head rotation/translation, left/right controller assignment, reasonable wing velocity signs, pause/reset and consistent stereo rendering before developing the workout loop. Smooth yaw/forward motion and direct physical wing mappings are provisional and not comfort-tested. The simulation uses swept scenery contact and safe contact landing; start in third person, use B to toggle, and raise/hold spread wings to brake before gentle contact. The desktop HUD is deliberately disabled in XR; inspect values through Unity tooling/logging until an in-world HUD is designed.

## Mac testing boundary

Desktop Play mode supports synthetic/gamepad testing. A connected USB Quest does not turn macOS Unity Play mode into a supported Quest Link preview. Use standalone Android builds for this workflow; Meta's Windows Link tooling is a separate path. Hardware validation and GPU/frame-time profiling remain necessary even when EditMode and PlayMode tests pass.

## Official references

- [Unity Meta Quest Support and profiles](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/features/metaquest.html)
- [Unity XR Plug-in Management](https://docs.unity3d.com/6000.3/Documentation/Manual/xr-plugin-management.html)
- [Meta headset developer setup](https://developers.meta.com/horizon/documentation/unity/unity-env-device-setup/)
- [Meta Horizon Link requirements](https://developers.meta.com/horizon/documentation/unity/unity-link/)

## Infinite-world iteration validation

Validate third-person default/A recovery and view-preserving Meta recenter, modest calibrated head-down descent, unchanged Duck effort and longer Dragon glide. Fly across several chunk boundaries, glance a wall/tree, brake onto ground and an elevated canopy/roof, and flap off again. Follow upward-moving traces through a drifting lift feature; cyan horizontal wind alone does not promise lift. Watch for hitches and camera clipping. Automated scene/physics tests and adb launch do not establish worn comfort or sustained Quest frame rate. See the [current handoff](agent/session-handoffs/2026-09-09-infinite-world-landing-v1.md).

## Flight Game playtest

Install `builds/quest/VoarVR.apk` using `python3 tools/scripts/quest.py run` with Quest awake on Wi-Fi. The sideload remains installed locally under Apps → Unknown Sources, titled VoarVR; it is not a store-library listing. Choose activity before species. First verify Beginner comfort/rigging with Duck, then Dragon/Magpie. Use A to return and calibrate, Meta recenter for in-place calibration, left Menu to return to selection. Hold left-stick0.7s without grips for optional Acrobatic; use B third-person for body awareness. Quiet wings plus intentional circling in golden rising air should gain height. Test arch approach and physical terrace landing; check saved best after returning to menu. Record a telemetry marker with both grips+left click. Current desktop validation does not establish headset72Hz or comfort.
