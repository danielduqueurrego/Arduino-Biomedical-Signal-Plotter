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

## Display Settings

Click `Signal Settings` to open the detailed signal/channel configuration. The main window keeps only connection, simulation, recording, device, and firmware actions visible so more space is available for plotting.

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

Channels are mapped to Arduino analog pins `A0` through `A5`. The default editable labels are `A0`, `A1`, `A2`, `A3`, `A4`, and `A5`; presets may replace the visible label with lab-oriented text such as `ECG` or `EMG`, while CSV metadata still records the underlying Arduino pin mapping.

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

The app can record samples from either simulated mode or a connected serial device.

1. Click `Start Recording` while simulated data or serial data is running.
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

The Arduino sketch is in `firmware/arduino/TwoChannelCsvStreamer/`. It targets the Arduino UNO R4 WiFi, reads a runtime configured contiguous channel range from `A0` through `A5`, and streams raw ADC values at 250 Hz by default. The default firmware channel count is 2 (`A0,A1`) to preserve current behavior. The default ADC resolution is 14 bits.

Arduino CLI is required for students who need to upload or update firmware through this workflow. Arduino CLI is not required for simulation mode, CSV export, or plotting from an already programmed board.

In the app, click `Check Arduino CLI` to run:

```powershell
arduino-cli version
```

You can install Arduino CLI from the official Arduino CLI installation instructions, then confirm it is on your `PATH` with the command above.

To upload firmware from the app:

1. Connect one Arduino UNO R4 WiFi by USB.
2. Click `Check Arduino CLI` and confirm the CLI is available.
3. Click `Upload Firmware`.

The app uses Arduino CLI to detect connected boards, compile `firmware/arduino/TwoChannelCsvStreamer`, and upload it to the single detected UNO R4 WiFi. If a serial connection is open in the app, the app disconnects before uploading so Arduino CLI can use the port. After a successful upload, the app attempts to reconnect to the uploaded board automatically; if reconnect fails, it leaves the app disconnected and shows a status message.

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
