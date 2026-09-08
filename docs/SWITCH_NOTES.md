# Future Nintendo Switch investigation

Switch is a future goal, not a configured platform or compatibility claim. There are no Switch SDKs, proprietary headers, exporters or platform packages in this repository.

Keep flight/gameplay state independent of hardware, route controls through `IFlightInput`, and keep platform build configuration in editor tooling. Basic VR uses OpenXR; avoid unnecessary Meta-specific runtime APIs so a later gamepad/camera presentation can reuse the simulation.

Actual Switch development and export require Nintendo developer approval/access, applicable hardware/SDKs and Unity's restricted platform support/licensing. Investigate those through the official programs at M10; do not infer access from a standard Unity installation. A future port also needs independent performance, input, certification and UX evaluation. No SDK installation/configuration belongs in M0.
