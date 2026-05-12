# Serial Protocol

## Version 0.1: Two-Channel Numeric CSV

The Arduino sends one numeric CSV line per sample. Version 0.1 expects exactly two values per row:

```text
512,310
514,311
513,312
```

The first value is channel 1, initially `A0`. The second value is channel 2, initially `A1`.

Valid rows may include whitespace or decimal values:

```text
512, 310
512.0,310.0
```

## Arduino Output

The initial `TwoChannelCsvStreamer` sketch streams raw ADC values from an Arduino UNO R4 WiFi at 250 Hz:

```text
<A0 raw ADC value>,<A1 raw ADC value>
```

It does not print a plain text header such as `A0,A1` by default.

## Malformed Lines

The app ignores blank or malformed lines without crashing. Ignored lines include:

- blank lines
- nonnumeric rows such as `A0,A1`
- rows with too few values such as `512`
- rows with too many values such as `512,310,99`

## Comment And Metadata Lines

Lines beginning with `#` are treated as comments or metadata and ignored:

```text
# A0,A1
# sample_rate_hz=250
```

Future firmware may use `#` lines for optional metadata while preserving numeric CSV sample rows.

## Future Notes

Future protocol versions may add:

- channel names
- sample-rate metadata
- timestamps
- checksums or framing

Any future extension should keep malformed input safe and avoid blocking the UI thread.
