# Biomedical Instrumentation Signal Plotter

Biomedical Instrumentation Signal Plotter is a lightweight cross-platform desktop app for plotting Arduino-based biomedical instrumentation signals in real time.

## Purpose

This app is designed for educational use in biomedical instrumentation laboratories. It supports real-time visualization of signals such as EMG, ECG, pulse oximetry/PPG, and blood pressure sensor outputs from Arduino-compatible boards.

## Target users

- BMEG-420L students
- Teaching assistants
- Instructors
- Open-source biomedical instrumentation users

## Target platforms

- Windows
- macOS

## Planned technology stack

- C# / .NET
- Avalonia UI
- ScottPlot
- Serial communication over USB
- Arduino UNO R4 WiFi as the initial target board

## Development

Version 0.1 focuses on Arduino UNO R4 WiFi acquisition using numeric CSV serial data. The app supports 1 to 6 analog channels, with 2 channels as the default.

Prerequisite:

- .NET 10 SDK

Restore and build:

```powershell
dotnet restore
dotnet build
```

Run the app:

```powershell
dotnet run --project src/BiomedicalSignalPlotter/BiomedicalSignalPlotter.csproj
```

Run tests:

```powershell
dotnet test
```

## Release Documentation

- [Student installation guide](docs/student-installation.md)
- [Instructor release checklist](docs/release-checklist.md)

## Windows Release Packaging

Create a repeatable Windows v0.1.0 package with:

```powershell
.\scripts\package-windows.ps1
```

The script cleans the previous v0.1.0 `win-x64` release output, runs restore/build/test, publishes the Avalonia app in `Release` mode for `win-x64`, and creates:

```text
artifacts/Biomedical-Instrumentation-Signal-Plotter-v0.1.0-win-x64/
artifacts/Biomedical-Instrumentation-Signal-Plotter-v0.1.0-win-x64.zip
```

The package includes the published app, app assets such as `Assets/app-icon-source.png` when present, `firmware/`, `docs/`, `README.md`, `scripts/upload-uno-r4-wifi.ps1`, and any repository license file if present. The app publish is self-contained, so students should not need to install .NET to run the packaged Windows app.

Arduino CLI is not bundled in the release package. Students only need Arduino CLI if they will upload or update firmware; it is not required for plotting from an already programmed Arduino.

## macOS Release Packaging

Create a repeatable macOS v0.1.0 package on macOS with:

```bash
chmod +x scripts/package-macos.sh
./scripts/package-macos.sh
```

The default package targets Apple Silicon (`osx-arm64`). To build for Intel Macs, run:

```bash
./scripts/package-macos.sh osx-x64
```

To build both macOS packages, run:

```bash
./scripts/package-macos.sh all
```

The script cleans the selected previous macOS release output, runs restore/build/test, publishes the Avalonia app in `Release` mode as a self-contained app, creates a `.app` bundle, and writes ZIP files such as:

```text
artifacts/Biomedical-Instrumentation-Signal-Plotter-v0.1.0-macos-arm64.zip
artifacts/Biomedical-Instrumentation-Signal-Plotter-v0.1.0-macos-x64.zip
```

Each macOS ZIP includes `Biomedical Instrumentation Signal Plotter.app`, app assets when present, `firmware/`, `docs/`, `README.md`, `scripts/upload-uno-r4-wifi.ps1`, and any repository license file if present. The app publish is self-contained, so students should not need to install .NET to run the packaged macOS app.

The macOS app is unsigned in this workflow. Students may need to approve opening it in macOS security settings or use Control-click, then Open. Arduino CLI is not bundled; students only need Arduino CLI for firmware upload/setup, not for plotting from an already programmed Arduino.

If you do not have a Mac, build the macOS package with GitHub Actions:

1. Open the repository on GitHub.
2. Go to `Actions`.
3. Select `Package macOS Release`.
4. Click `Run workflow`.
5. Choose `osx-arm64`, `osx-x64`, or `all`.
6. Download the uploaded macOS release artifact from the completed workflow run.

The workflow also runs automatically for tags matching `v*`. macOS artifacts are built on GitHub-hosted macOS runners. The app is still unsigned unless a future signing/notarization step is added, and a real Mac should still be used for final launch and hardware validation before a public release.

## App Icon Assets

The recommended editable source icon path is:

```text
src/BiomedicalSignalPlotter/Assets/app-icon-source.png
```

Use a 1024x1024 PNG for the source image. When this file exists, the development build copies it to the app output and uses it as the Avalonia window icon where practical. Release packaging may later generate platform-specific icons from this source, such as Windows `.ico` and macOS `.icns` files, without adding icon conversion dependencies to the app project.

## Quick Start for Students

1. Install Arduino CLI if firmware upload is needed.
2. Connect an Arduino UNO R4 WiFi by USB.
3. Click `Check Arduino CLI`.
4. Click `Upload Firmware`.
5. Click `Refresh Ports`.
6. Select the Arduino serial port.
7. Click `Connect`.
8. Open `Signal Settings`.
9. Choose channel count, ADC bits, reference voltage, display mode, signal labels, and units.
10. Click `Apply Device Settings` so channel count, ADC bits, and sample rate are sent to the Arduino firmware.
11. Configure plot layout if needed: choose 1 to 3 vertical plots, assign `A0` through `A5` signals to plots, hide signals, set plot time window, set Y-axis ranges, and add horizontal reference bars.
12. Click `Start Recording`.
13. Click `Stop Recording`.
14. Click `Save CSV`.

Arduino CLI is required for firmware upload and setup, but it is not required for plotting from an already programmed Arduino. The supported board is Arduino UNO R4 WiFi. The supported channels are `A0` through `A5`, channel count is 1 to 6, ADC bits are 8 to 14, sample rate is 1 to 1000 Hz, plot count is 1 to 3, and each subplot supports up to 5 horizontal reference bars.

Signal and device settings should not be changed during recording. Plot display settings and reference bars affect visualization only, not recorded raw data. Reference voltage is used for app-side voltage conversion, not hardware ADC reference configuration.

## What Each Control Does

- `Check Arduino CLI`: verifies that `arduino-cli` is installed and available on `PATH`.
- `Upload Firmware`: detects one connected UNO R4 WiFi, compiles the firmware, uploads it, and reconnects if the app was connected before upload.
- `View Firmware`: opens a read-only firmware details window showing the Arduino sketch file path, the sketch source that will be compiled/uploaded, an Open Firmware Folder button, and the most recent upload commands and stdout/stderr log.
- `Refresh Ports`: refreshes the serial port list. When Arduino CLI is available, the dropdown may include detected board names, such as `COM5 — Arduino UNO R4 WiFi`; the app still connects using the raw port name.
- `Connect`: opens or closes the selected serial connection.
- `Signal Settings`: configures signal mode, active channel count, channel labels and units, ADC bits, reference voltage, display mode, and channel routing.
- `Apply Device Settings`: sends channel count, ADC bits, and sample rate to the connected Arduino firmware.
- Plot layout/routing: chooses 1 to 3 vertical plots and assigns each active `A0` through `A5` signal to a plot or `Hidden`.
- Time window: sets how many seconds of recent data are visible.
- Y range: sets automatic or manual Y-axis limits per subplot.
- Reference bars: adds display-only horizontal target lines such as a target ADC count or voltage.
- `Start Recording` / `Stop Recording`: starts and stops sample capture.
- `Save CSV`: exports recorded active-channel samples.

## Display Settings

Click `Signal Settings` to open the detailed signal/channel configuration. The main window keeps only connection, recording, device, and firmware actions visible so more space is available for plotting.

The app includes lightweight display presets so the same Arduino stream can be used in different lab activities:

- Custom
- Generic two-channel
- EMG + Force/Pressure
- ECG
- PPG / Pulse Oximetry
- Blood Pressure

These presets only set display labels, units, and the default plot time window. They are not diagnostic algorithms and do not calculate heart rate, SpO2, blood pressure, EMG RMS, or other biomedical measurements.

Custom mode lets users edit:

- active channel labels and units
- reference voltage
- display mode
- plot layout and channel-to-plot routing

Channels are mapped to Arduino analog pins `A0` through `A5`. The default editable labels are `A0`, `A1`, `A2`, `A3`, `A4`, and `A5`; presets may replace the visible label with lab-oriented text such as `ECG` or `EMG`, while CSV metadata still records the underlying Arduino pin mapping.

The app can show 1 to 3 vertically stacked plots. In `Signal Settings`, choose the number of plots and assign each active analog channel to `Plot 1`, `Plot 2`, `Plot 3`, or `Hidden`. Only plot choices available for the selected plot count are shown. Hidden channels are not displayed, but they are still received and recorded if they are active in the selected channel count.

Click `Plot Display` to adjust display-only plot controls. The global time window controls how many seconds of recent data are visible; common choices include 2, 5, 10, and 30 seconds, and custom positive values are supported. Each visible subplot can use automatic Y scaling or a manual Y minimum and maximum. Manual Y limits are per subplot, and invalid ranges where the minimum is not less than the maximum are rejected with a status message.

Each subplot can also show up to five horizontal reference bars. A reference bar has an enabled checkbox, a numeric value, and an optional label, such as a target `512` ADC count or `2.5` V level. The entered value uses the current displayed Y-axis units for that subplot. If a subplot contains multiple signals with different units, the bar is still allowed, but students should interpret it in the plot's displayed Y-axis units.

Plot display settings and reference bars affect live visualization only. They do not change serial parsing, firmware settings, recording, or exported CSV data, and they are not included in CSV metadata, so they can be changed while recording.

If a preset is selected and a channel label or unit is edited manually, the app switches to Custom mode while preserving the edited text.

Display mode can show raw ADC counts or voltage. Voltage display uses:

```text
voltage = raw_count * reference_voltage / (2^adc_bits - 1)
```

Reference voltage is currently used only for app-side voltage conversion. It does not configure an Arduino hardware reference voltage.

## Device Settings

Device settings are sent to the Arduino only when a serial connection is active and `Apply Device Settings` is clicked. The app sends `#STOP`, applies the settings, asks for `#STATUS`, then sends `#START`. The returned `#STATUS` line is checked to verify that `CHANNEL_COUNT`, `ADC_BITS`, and `SAMPLE_RATE_HZ` match the requested values.

Device settings include:

- channel count: 1 to 6
- ADC bits: 8 to 14
- sample rate: 1 to 1000 Hz
- streaming state, controlled by `#START` and `#STOP`

The channel count control selects how many values the app expects per serial row and how many channels are plotted and recorded. The default is 2. Single-channel presets such as ECG, PPG / Pulse Oximetry, and Blood Pressure default to 1 channel; Generic two-channel and EMG + Force/Pressure default to 2 channels.

ADC bits configure Arduino `analogReadResolution(adcBits)` after `Apply Device Settings`. The app also uses the selected ADC bits when displaying raw counts as voltage.

Sample Hz is sent to firmware as `#SET SAMPLE_RATE_HZ <value>` after `Apply Device Settings`.

Choose the channel count in `Signal Settings` before starting a recording. If recorded samples are present, clear the recording before changing signal mode or channel count so the exported CSV keeps one consistent shape.

## Recording Data

The app records samples from a connected Arduino serial device.

1. Click `Start Recording` after the Arduino is connected and streaming data.
2. Click `Stop Recording` to pause capture.
3. Click `Save CSV` to export the recorded samples.
4. Click `Clear Recording` to discard the current recording.

Recording is separate from plotting, so the plot continues to refresh on its timer while samples are captured.

While recording is active, controls that would make the recording metadata or device state inconsistent are disabled. Stop recording before changing signal/display/device settings, clearing data, saving CSV, or uploading firmware.

CSV exports use invariant-culture numeric formatting and include timestamps in seconds relative to the start of recording:

```text
# mode=Generic two-channel
# channel_count=2
# channel_0_pin=A0
# channel_0_label=A0
# channel_0_unit=ADC counts
# channel_1_pin=A1
# channel_1_label=A1
# channel_1_unit=ADC counts
# adc_bits=14
# reference_voltage=5
# display_mode=Raw ADC counts
time_s,channel_0,channel_1,source
0.000,512,310,serial
0.004,514,311,serial
```

Recorded channel values remain the captured source values even if the plot is displayed as voltage. The display configuration is included as `#` metadata above the CSV header.

For one active channel, the header is `time_s,channel_0,source`. For six active channels, the header is `time_s,channel_0,channel_1,channel_2,channel_3,channel_4,channel_5,source`.

Exported data are for educational and laboratory analysis only.

## Arduino Serial Format

The app expects numeric-only CSV rows with exactly the selected channel count. Supported Arduino analog inputs are `A0` through `A5`, read contiguously from `A0`.

```text
512
512,310
512,310,203,102,850,914
```

Blank lines, malformed lines, and metadata lines beginning with `#` are ignored by the parser. The firmware does not print a plain text header by default.

The firmware also accepts runtime text commands:

```text
#SET CHANNEL_COUNT 6
#SET ADC_BITS 14
#SET SAMPLE_RATE_HZ 250
#STOP
#STATUS
#START
```

Firmware responses also begin with `#`, so they are ignored as data:

```text
#OK CHANNEL_COUNT 6
#OK ADC_BITS 14
#OK SAMPLE_RATE_HZ 250
#STATUS CHANNEL_COUNT=6 ADC_BITS=14 SAMPLE_RATE_HZ=250 STREAMING=1
#ERR BAD_VALUE
```

## Arduino Firmware

The Arduino sketch is in `firmware/arduino/TwoChannelCsvStreamer/`, and the uploaded source file is `firmware/arduino/TwoChannelCsvStreamer/TwoChannelCsvStreamer.ino`. It targets the Arduino UNO R4 WiFi, reads a runtime configured contiguous channel range from `A0` through `A5`, and streams raw ADC values at 250 Hz by default. The default firmware channel count is 2 (`A0,A1`) to preserve current behavior. The default ADC resolution is 14 bits.

Click `View Firmware` in the app to inspect the exact read-only sketch source the app compiles/uploads. The same window shows the firmware file path, can open the firmware folder in the operating system file explorer, and displays the commands, stdout, and stderr from the most recent upload attempt.

Arduino CLI is required for students who need to upload or update firmware through this workflow. Arduino CLI is not required for CSV export or plotting from an already programmed board.

In the app, click `Check Arduino CLI` to run:

```powershell
arduino-cli version
```

You can install Arduino CLI from the official Arduino CLI installation instructions, then confirm it is on your `PATH` with the command above.

To upload firmware from the app:

1. Connect one Arduino UNO R4 WiFi by USB.
2. Click `Check Arduino CLI` and confirm the CLI is available.
3. Click `Upload Firmware`.

The app uses Arduino CLI to detect connected boards, compile `firmware/arduino/TwoChannelCsvStreamer`, and upload it to the single detected UNO R4 WiFi. The compile command is:

```powershell
arduino-cli compile --fqbn arduino:renesas_uno:unor4wifi firmware/arduino/TwoChannelCsvStreamer
```

The upload command uses the detected port:

```powershell
arduino-cli upload -p <PORT> --fqbn arduino:renesas_uno:unor4wifi firmware/arduino/TwoChannelCsvStreamer
```

If a serial connection is open in the app, the app disconnects before uploading so Arduino CLI can use the port. After a successful upload, the app attempts to reconnect to the uploaded board automatically; if reconnect fails, it leaves the app disconnected and shows a status message. Open `View Firmware` after an upload attempt to inspect the exact commands and CLI output.

If no board is detected, check the USB cable, board power, drivers, and the output of `arduino-cli board list`. If multiple UNO R4 WiFi boards are detected, disconnect extras and try again. If upload fails, read the status message, close any other serial monitor using the board, then retry.

List connected boards:

```powershell
arduino-cli board list
```

Compile the sketch:

```powershell
arduino-cli compile --fqbn arduino:renesas_uno:unor4wifi firmware/arduino/TwoChannelCsvStreamer
```

Upload the sketch, replacing `COM5` with the port reported by `arduino-cli board list`:

```powershell
arduino-cli upload -p COM5 --fqbn arduino:renesas_uno:unor4wifi firmware/arduino/TwoChannelCsvStreamer
```

On Windows PowerShell, the helper script detects a single connected UNO R4 WiFi, compiles, and uploads:

```powershell
.\scripts\upload-uno-r4-wifi.ps1
```

## Safety disclaimer

This software is for educational signal visualization only. It is not a medical device and must not be used for diagnosis, treatment, monitoring, or clinical decision-making.
