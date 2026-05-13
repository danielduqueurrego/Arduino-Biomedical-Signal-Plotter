\# Initial Requirements



\## Version 0.1



The first version should prove that the app can:



1\. Launch on Windows.

2\. Show a clean main window.

3\. Plot 1 to 6 channels of simulated real-time data.

4\. Read 1 to 6 channels of numeric CSV data from a serial port.

5\. Store data in a fixed-size buffer.

6\. Refresh plots at a fixed UI rate, not per sample.

7\. Export captured data to CSV.

8\. Avoid crashing on malformed serial input.

9\. Configure Arduino channel count, ADC bits, sample rate, and streaming state over serial commands.

10\. Upload Arduino firmware from the GUI using Arduino CLI when available.

11\. Keep detailed signal/channel configuration in a Signal Settings dialog or panel so the main window has more room for plotting.

12\. Identify app channels by Arduino analog pin names A0 through A5 while preserving editable channel labels and units.

13\. Show 1 to 3 vertically stacked plots and allow active channels to be routed to Plot 1, Plot 2, Plot 3, or Hidden.



\## Not included in Version 0.1



\- Heart rate detection

\- EMG RMS/envelope processing

\- Pulse oximetry SpO2 calculation

\- Blood pressure estimation

\- Installer creation

\- Medical validation

