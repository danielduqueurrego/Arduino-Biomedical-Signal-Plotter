# AGENTS.md

## Project identity

This repository contains a lightweight biomedical signal plotting app for Arduino-based laboratory teaching.

The app is intended for BMEG-420L-style biomedical instrumentation labs and should support signals such as EMG, ECG, PPG/pulse oximetry, and blood pressure sensor outputs.

## Technology stack

Use:

- C#
- .NET
- Avalonia UI
- ScottPlot.Avalonia
- Serial communication over USB
- xUnit or NUnit for tests

Do not use:

- Python
- MATLAB
- Electron
- Web-only UI frameworks
- Heavy dependencies unless justified

## Target platforms

The app must eventually be packaged as:

- Windows executable
- macOS application bundle

Students should not need to install .NET, Python, Arduino tools, or development dependencies.

## Real-time plotting requirements

- Do not redraw the plot for every serial sample.
- Use fixed-size circular buffers.
- Use a GUI timer to refresh plots at approximately 30 Hz.
- Keep serial reading off the UI thread.
- The UI must remain responsive if malformed serial lines are received.
- The app should support at least two channels initially.

## Initial serial protocol

Start with simple CSV lines:

```text
A0,A1
512,310
513,311
514,312
