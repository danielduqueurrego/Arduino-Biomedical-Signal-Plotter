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

Version 0.1 currently proves the desktop app architecture with simulated two-channel data. Real serial reading is intentionally not implemented yet.

Prerequisite:

- .NET 8 SDK or newer

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

## Safety disclaimer

This software is for educational signal visualization only. It is not a medical device and must not be used for diagnosis, treatment, monitoring, or clinical decision-making.
