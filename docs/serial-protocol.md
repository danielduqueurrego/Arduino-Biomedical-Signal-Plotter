# Serial Protocol

## Version 0.1: Variable-Channel Numeric CSV

The Arduino sends one numeric CSV line per sample. Version 0.1 supports an expected channel count from 1 to 6. The app only accepts rows with exactly the selected channel count.

Supported channels are a contiguous range of analog inputs starting at `A0`:

- 1 channel: `A0`
- 2 channels: `A0,A1`
- 3 channels: `A0,A1,A2`
- 4 channels: `A0,A1,A2,A3`
- 5 channels: `A0,A1,A2,A3,A4`
- 6 channels: `A0,A1,A2,A3,A4,A5`

Examples:

```text
512
512,310
512,310,203,102,850,914
```

Valid rows may include whitespace or decimal values:

```text
512, 310
512.0,310.0
```

## Arduino Output

The `TwoChannelCsvStreamer` sketch now behaves as a configurable analog CSV streamer. It streams raw ADC values from an Arduino UNO R4 WiFi at 250 Hz, using the compile-time `CHANNEL_COUNT` setting:

```text
<A0 raw ADC value>,<A1 raw ADC value>,...
```

The default firmware channel count is 2. It does not print a plain text header such as `A0,A1` by default.

## Expected Channel Count

The app channel count controls both parsing and display. If the app expects 2 channels, `512,310` is valid, while `512` and `512,310,203` are malformed. If the app expects 6 channels, rows with fewer or more than 6 values are malformed.

The app and firmware are configured separately in Version 0.1. Dynamic Arduino reconfiguration will be added later.

## Malformed Lines

The app ignores blank or malformed lines without crashing. Ignored lines include:

- blank lines
- nonnumeric rows such as `A0,A1`
- rows with too few values for the selected channel count
- rows with too many values for the selected channel count

Malformed lines are not plotted or recorded.

## Comment And Metadata Lines

Lines beginning with `#` are treated as comments or metadata and ignored:

```text
# A0,A1
# sample_rate_hz=250
# channel_count=2
```

Future firmware may use `#` lines for optional metadata while preserving numeric CSV sample rows.

## Future Notes

Future protocol versions may add:

- channel names
- sample-rate metadata
- timestamps
- checksums or framing
- app-to-device channel-count configuration

Any future extension should keep malformed input safe and avoid blocking the UI thread.
