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

Version 0.1 proves the desktop app architecture with simulated two-channel data and the first Arduino two-channel CSV serial pathway.

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

## Arduino Serial Format

The app expects numeric-only two-channel CSV rows:

```text
512,310
514,311
513,312
```

Blank lines, malformed lines, and metadata lines beginning with `#` are ignored by the parser. The firmware does not print a plain text header by default.

## Arduino Firmware

The initial Arduino sketch is in `firmware/arduino/TwoChannelCsvStreamer/`. It targets the Arduino UNO R4 WiFi, reads `A0` and `A1`, and streams raw ADC values at 250 Hz.

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
