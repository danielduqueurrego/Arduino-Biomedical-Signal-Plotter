using System.Globalization;
using System.Text;

namespace BiomedicalSignalPlotter.Recording;

public static class CsvRecordingExporter
{
    public const string Header = "time_s,channel_0,channel_1,source";

    public static string FormatCsv(IEnumerable<RecordedSample> samples, CsvRecordingMetadata? metadata = null)
    {
        StringBuilder builder = new();
        AppendMetadata(builder, metadata);
        builder.AppendLine(Header);

        foreach (RecordedSample sample in samples)
        {
            builder.AppendLine(FormatRow(sample));
        }

        return builder.ToString();
    }

    public static async Task WriteAsync(
        Stream stream,
        IReadOnlyList<RecordedSample> samples,
        CsvRecordingMetadata? metadata = null,
        CancellationToken cancellationToken = default)
    {
        await using StreamWriter writer = new(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 16 * 1024,
            leaveOpen: true);

        if (metadata is not null)
        {
            foreach (string line in FormatMetadataLines(metadata))
            {
                await writer.WriteLineAsync(line).ConfigureAwait(false);
            }
        }

        await writer.WriteLineAsync(Header).ConfigureAwait(false);

        foreach (RecordedSample sample in samples)
        {
            await writer.WriteLineAsync(FormatRow(sample)).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string FormatRow(RecordedSample sample)
    {
        return string.Join(
            ',',
            FormatTime(sample.TimeSeconds),
            FormatChannel(sample.Channel0),
            FormatChannel(sample.Channel1),
            FormatSource(sample.Source));
    }

    private static void AppendMetadata(StringBuilder builder, CsvRecordingMetadata? metadata)
    {
        if (metadata is null)
        {
            return;
        }

        foreach (string line in FormatMetadataLines(metadata))
        {
            builder.AppendLine(line);
        }
    }

    private static IEnumerable<string> FormatMetadataLines(CsvRecordingMetadata metadata)
    {
        yield return $"# mode={SanitizeMetadataValue(metadata.Mode)}";
        yield return $"# channel_0_label={SanitizeMetadataValue(metadata.Channel0Label)}";
        yield return $"# channel_0_unit={SanitizeMetadataValue(metadata.Channel0Unit)}";
        yield return $"# channel_1_label={SanitizeMetadataValue(metadata.Channel1Label)}";
        yield return $"# channel_1_unit={SanitizeMetadataValue(metadata.Channel1Unit)}";
        yield return $"# adc_bits={metadata.AdcBits.ToString(CultureInfo.InvariantCulture)}";
        yield return $"# reference_voltage={metadata.ReferenceVoltage.ToString("0.##########", CultureInfo.InvariantCulture)}";
        yield return $"# display_mode={SanitizeMetadataValue(metadata.DisplayMode)}";
    }

    private static string FormatTime(double value)
    {
        return value.ToString("0.000######", CultureInfo.InvariantCulture);
    }

    private static string FormatChannel(double value)
    {
        return value.ToString("0.##########", CultureInfo.InvariantCulture);
    }

    private static string FormatSource(RecordedSampleSource source)
    {
        return source switch
        {
            RecordedSampleSource.Serial => "serial",
            RecordedSampleSource.Simulated => "simulated",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, "Unknown recorded sample source.")
        };
    }

    private static string SanitizeMetadataValue(string value)
    {
        return value
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
    }
}
