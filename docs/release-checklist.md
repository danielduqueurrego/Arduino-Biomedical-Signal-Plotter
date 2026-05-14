# Release Checklist

Use this checklist before publishing a Biomedical Instrumentation Signal Plotter release.

## Build And Test

- [ ] Run `dotnet restore`.
- [ ] Run `dotnet build` and confirm 0 warnings.
- [ ] Run `dotnet test`.
- [ ] Compile firmware:

```text
arduino-cli compile --fqbn arduino:renesas_uno:unor4wifi firmware/arduino/TwoChannelCsvStreamer
```

- [ ] Upload firmware to a real Arduino UNO R4 WiFi.
- [ ] Confirm the app launches with the title `Biomedical Instrumentation Signal Plotter`.
- [ ] Confirm the app icon appears where supported by the package/platform.
- [ ] Confirm the educational-use disclaimer is visible in Help/About.

## Hardware Workflow

- [ ] Test firmware upload button from the GUI.
- [ ] Test serial plotting from a real Arduino UNO R4 WiFi.
- [ ] Test 1-channel mode.
- [ ] Test 2-channel mode.
- [ ] Test 6-channel mode.
- [ ] Test ADC bits set to 10.
- [ ] Test ADC bits set to 14.
- [ ] Test sample rate set to 100 Hz.
- [ ] Test sample rate set to 250 Hz.
- [ ] Test sample rate set to 1000 Hz.
- [ ] Confirm `Apply Device Settings` reports matching `#STATUS` values.
- [ ] Confirm malformed or mismatched serial rows do not crash the app.

## Recording And Export

- [ ] Test CSV recording/export for 1 channel.
- [ ] Test CSV recording/export for 2 channels.
- [ ] Test CSV recording/export for 6 channels.
- [ ] Confirm CSV metadata maps active channels to Arduino pins `A0` through `A5`.
- [ ] Confirm hidden plotted channels are still recorded when active.
- [ ] Confirm saving is blocked or disabled while recording.

## Windows Package

- [ ] Run `.\scripts\package-windows.ps1`.
- [ ] Confirm `artifacts/Biomedical-Instrumentation-Signal-Plotter-v0.1.0-win-x64.zip` exists.
- [ ] Test the Windows ZIP on a clean Windows machine.
- [ ] Confirm the app starts without installing .NET.
- [ ] Confirm the package includes `firmware/`, `docs/`, `README.md`, and `scripts/upload-uno-r4-wifi.ps1`.
- [ ] Confirm Arduino CLI is not bundled and the README says students install it separately for firmware upload/setup.

## macOS Package

- [ ] If no local Mac is available, open GitHub `Actions` and run the `Package macOS Release` workflow manually.
- [ ] For tag releases, confirm the `Package macOS Release` workflow ran automatically for the `v*` tag.
- [ ] Download the macOS ZIP artifact from the completed GitHub Actions workflow.
- [ ] Confirm macOS artifacts are built on GitHub-hosted macOS runners.
- [ ] On a Mac, run `chmod +x scripts/package-macos.sh`.
- [ ] On a Mac, run `./scripts/package-macos.sh`.
- [ ] Optionally run `./scripts/package-macos.sh osx-x64` or `./scripts/package-macos.sh all`.
- [ ] Confirm the expected macOS ZIP exists under `artifacts/`.
- [ ] Test the macOS package on a Mac.
- [ ] Confirm the `.app` starts without installing .NET.
- [ ] Confirm the unsigned-app opening instructions are documented.
- [ ] Confirm the app is unsigned unless signing/notarization is added later.
- [ ] Use a real Mac for final launch and hardware validation before public release.
- [ ] Confirm the package includes `firmware/`, `docs/`, `README.md`, and `scripts/upload-uno-r4-wifi.ps1`.

## GitHub Release

- [ ] Review `docs/student-installation.md`.
- [ ] Review `docs/serial-protocol.md`.
- [ ] Review `README.md`.
- [ ] Create a GitHub release draft.
- [ ] Attach Windows and macOS artifacts.
- [ ] Include release notes with supported board, supported channels, Arduino CLI requirement, and educational-use disclaimer.
- [ ] Publish the release.
