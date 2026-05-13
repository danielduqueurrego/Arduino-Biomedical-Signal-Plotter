using System.Globalization;
using System.Text;
using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Recording;

public static class CsvRecordingExporter
{
    public static string FormatCsv(IEnumerable<RecordedSample> samples, CsvRecordingMetadata? metadata = null)
    {
        RecordedSample[] sampleArray = samples.ToArray();
        int channelCount = ResolveChannelCount(sampleArray, metadata);

        StringBuilder builder = new();
        AppendMetadata(builder, metadata);
        builder.AppendLine(BuildHeader(channelCount));

        foreach (RecordedSample sample in sampleArray)
        {
            builder.AppendLine(FormatRow(sample, channelCount));
        }

        return builder.ToString();
    }

    public static async Task WriteAsync(
        Stream stream,
        IReadOnlyList<RecordedSample> samples,
        CsvRecordingMetadata? metadata = null,
        CancellationToken cancellationToken = default)
    {
        int channelCount = ResolveChannelCount(samples, metadata);

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

        await writer.WriteLineAsync(BuildHeader(channelCount)).ConfigureAwait(false);

        foreach (RecordedSample sample in samples)
        {
            await writer.WriteLineAsync(FormatRow(sample, channelCount)).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static string BuildHeader(int channelCount)
    {
        AnalogChannelLimits.Validate(channelCount);

        string[] channelHeaders = Enumerable.Range(0, channelCount)
            .Select(channelIndex => $"channel_{channelIndex}")
            .ToArray();

        return string.Join(',', ["time_s", .. channelHeaders, "source"]);
    }

    private static string FormatRow(RecordedSample sample, int channelCount)
    {
        if (sample.ChannelCount != channelCount)
        {
            throw new InvalidOperationException("Recorded sample channel count does not match the CSV channel count.");
        }

        string[] channelValues = sample.Values
            .Select(FormatChannel)
            .ToArray();

        return string.Join(',', [FormatTime(sample.TimeSeconds), .. channelValues, FormatSource(sample.Source)]);
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
        AnalogChannelLimits.Validate(metadata.ChannelCount);

        yield return $"# mode={SanitizeMetadataValue(metadata.Mode)}";
        yield return $"# channel_count={metadata.ChannelCount.ToString(CultureInfo.InvariantCulture)}";

        for (int channelIndex = 0; channelIndex < metadata.ChannelCount; channelIndex++)
        {
            yield return $"# channel_{channelIndex}_pin=A{channelIndex.ToString(CultureInfo.InvariantCulture)}";
            yield return $"# channel_{channelIndex}_label={SanitizeMetadataValue(GetMetadataValue(metadata.ChannelLabels, channelIndex, $"A{channelIndex}"))}";
            yield return $"# channel_{channelIndex}_unit={SanitizeMetadataValue(GetMetadataValue(metadata.ChannelUnits, channelIndex, "ADC counts"))}";
        }

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

    private static int ResolveChannelCount(IReadOnlyList<RecordedSample> samples, CsvRecordingMetadata? metadata)
    {
        int channelCount = metadata?.ChannelCount
            ?? samples.FirstOrDefault()?.ChannelCount
            ?? AnalogChannelLimits.Default;

        AnalogChannelLimits.Validate(channelCount);

        if (samples.Any(sample => sample.ChannelCount != channelCount))
        {
            throw new InvalidOperationException("All recorded samples must have the same channel count for CSV export.");
        }

        return channelCount;
    }

    private static string GetMetadataValue(IReadOnlyList<string> values, int index, string fallback)
    {
        return index < values.Count ? values[index] : fallback;
    }
}
