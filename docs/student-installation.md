# Student Installation Guide

Biomedical Instrumentation Signal Plotter is for educational signal visualization with an Arduino UNO R4 WiFi. It is not a medical device and must not be used for diagnosis, treatment, monitoring, or clinical decision-making.

## What You Need

- Biomedical Instrumentation Signal Plotter release package for your operating system.
- Arduino UNO R4 WiFi.
- USB data cable.
- Arduino CLI only if you need to upload or update firmware.

Arduino CLI is not required to plot from an Arduino that has already been programmed with the course firmware.

## Windows Installation

1. Download `Biomedical-Instrumentation-Signal-Plotter-v0.1.0-win-x64.zip` from the release.
2. Right-click the ZIP file, choose `Extract All`, and extract it to a normal folder such as `Documents`.
3. Open the extracted folder.
4. Open the `app` folder.
5. Run `BiomedicalSignalPlotter.exe`.
6. If Windows shows a security prompt, only continue if the app came from your instructor or the official course release.

The Windows package is self-contained, so you should not need to install .NET.

## macOS Installation

1. Download the package for your Mac:
   - Apple Silicon Macs: `Biomedical-Instrumentation-Signal-Plotter-v0.1.0-macos-arm64.zip`
   - Intel Macs: `Biomedical-Instrumentation-Signal-Plotter-v0.1.0-macos-x64.zip`
2. Open the ZIP file.
3. Open `Biomedical Instrumentation Signal Plotter.app`.
4. If macOS blocks the unsigned app, Control-click the app, choose `Open`, then confirm. You may also need to approve it in `System Settings` > `Privacy & Security`.

The macOS package is self-contained, so you should not need to install .NET.

## Arduino CLI For Firmware Upload

Install Arduino CLI only if you need to upload or update firmware from your computer.

After installation, restart the app or terminal so `arduino-cli` is available on your `PATH`. In the app, click `Check Arduino CLI`. A success message means the app can find Arduino CLI.

If `Check Arduino CLI` fails, install Arduino CLI from the official Arduino documentation and confirm this command works in a terminal:

```text
arduino-cli version
```

## Connect The Arduino

1. Plug the Arduino UNO R4 WiFi into your computer with a USB data cable.
2. Wait a few seconds for the operating system to detect the board.
3. If you need to upload firmware, click `Check Arduino CLI`, then click `Upload Firmware`.
4. Click `Refresh Ports`.
5. Select the Arduino serial port:
   - Windows ports usually look like `COM5`.
   - macOS ports usually look like `/dev/cu.usbmodem...`.
   - When Arduino CLI is available, the dropdown may show the board name after the port.
6. Click `Connect`.

## Apply Device Settings

1. Click `Signal Settings`.
2. Choose the channel count, ADC bits, reference voltage, display mode, labels, and units for your lab.
3. Close or return from the settings panel.
4. Click `Apply Device Settings`.

`Apply Device Settings` sends channel count, ADC bits, and sample rate to the Arduino firmware. Reference voltage is used by the app for voltage display only; it does not configure Arduino hardware reference voltage.

## Record And Save CSV

1. Connect to the Arduino and confirm live data are appearing.
2. Click `Start Recording`.
3. Perform the lab activity.
4. Click `Stop Recording`.
5. Click `Save CSV`.
6. Choose a file name and save location.

The exported CSV is for educational and lab analysis only.

## Troubleshooting

### Arduino CLI Not Found

- Install Arduino CLI if you need firmware upload/setup.
- Restart the app after installing Arduino CLI.
- Confirm `arduino-cli version` works in a terminal.
- You can still plot from an already programmed board without Arduino CLI.

### Board Not Detected

- Make sure the board is an Arduino UNO R4 WiFi.
- Use a USB data cable, not a charge-only cable.
- Try another USB port.
- Click `Refresh Ports`.
- Run `arduino-cli board list` if Arduino CLI is installed.

### Wrong COM Or Serial Port Selected

- Click `Refresh Ports`.
- Look for the port with `Arduino UNO R4 WiFi` in the dropdown if board names are available.
- Unplug and replug the board to see which port disappears and reappears.
- Disconnect before changing ports.

### Upload Fails Because A Serial Monitor Or App Is Connected

- Click `Disconnect` in the app.
- Close Arduino IDE Serial Monitor, Arduino Serial Plotter, or any other serial tool.
- Try `Upload Firmware` again.

The app disconnects its own serial connection before upload, but it cannot close other programs that are using the port.

### No Data Appear

- Confirm the firmware was uploaded successfully.
- Confirm the app is connected to the Arduino port.
- Click `Apply Device Settings`.
- Make sure the app channel count matches the firmware setting.
- Check that your sensor or signal source is wired to the expected analog pins, starting at `A0`.
- Check that the signal shares ground with the Arduino.

### CSV File Is Empty

- Start recording only after the Arduino is connected and data are arriving.
- Watch the recorded sample count before saving.
- Click `Stop Recording` before `Save CSV`.
- If no samples were recorded, check the serial connection and channel count.
