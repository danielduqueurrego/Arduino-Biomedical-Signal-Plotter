# Arduino-Biomedical-Signal-Plotter

A lightweight cross-platform desktop app for plotting Arduino-based biomedical instrumentation signals in real time.

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

Version 0.1 proves the desktop app architecture with simulated data and the first Arduino numeric CSV serial pathway. The app supports 1 to 6 analog channels, with 2 channels as the default.

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

## Signal Modes And Display

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
- channel count from 1 to 6
- ADC bits
- reference voltage
- display mode

If a preset is selected and a channel label or unit is edited manually, the app switches to Custom mode while preserving the edited text.

The channel count control selects how many values the app expects per serial row and how many channels are plotted and recorded. The default is 2. Single-channel presets such as ECG, PPG / Pulse Oximetry, and Blood Pressure default to 1 channel; Generic two-channel and EMG + Force/Pressure default to 2 channels.

Choose the channel count before starting a recording. If recorded samples are present, clear the recording before changing signal mode or channel count so the exported CSV keeps one consistent shape.

Display mode can show raw ADC counts or voltage. Voltage display uses:

```text
voltage = raw_count * reference_voltage / (2^adc_bits - 1)
```

The included UNO R4 WiFi firmware configures `analogReadResolution(10)`, so the app default is 10 ADC bits. Change ADC bits and reference voltage if your firmware or lab hardware uses different settings.

## Recording Data

The app can record samples from either simulated mode or a connected serial device.

1. Click `Start Recording` while simulated data or serial data is running.
2. Click `Stop Recording` to pause capture.
3. Click `Save CSV` to export the recorded samples.
4. Click `Clear Recording` to discard the current recording.

Recording is separate from plotting, so the plot continues to refresh on its timer while samples are captured.

CSV exports use invariant-culture numeric formatting and include timestamps in seconds relative to the start of recording:

```text
# mode=Generic two-channel
# channel_count=2
# channel_0_label=Channel 0
# channel_0_unit=ADC counts
# channel_1_label=Channel 1
# channel_1_unit=ADC counts
# adc_bits=10
# reference_voltage=5
# display_mode=Raw ADC counts
time_s,channel_0,channel_1,source
0.000,512,310,serial
0.004,514,311,serial
```

Recorded channel values remain the captured source values even if the plot is displayed as voltage. The display configuration is included as `#` metadata above the CSV header.

For simulated samples, the `source` column is `simulated`:

```text
time_s,channel_0,channel_1,source
0.000,0.52,0.31,simulated
```

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

## Arduino Firmware

The Arduino sketch is in `firmware/arduino/TwoChannelCsvStreamer/`. It targets the Arduino UNO R4 WiFi, reads a compile-time configured contiguous channel range from `A0` through `A5`, and streams raw ADC values at 250 Hz. The default firmware channel count is 2 (`A0,A1`) to preserve current behavior.

Channel count is currently selected independently in the app and compile-time configured in firmware by changing `CHANNEL_COUNT` in the sketch. Dynamic Arduino reconfiguration will be added later.

Arduino CLI is only needed by developers or instructors uploading firmware. Students running a packaged app should not need Arduino CLI.

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
