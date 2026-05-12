using System.Globalization;
using System.Text;

namespace BiomedicalSignalPlotter.Recording;

public static class CsvRecordingExporter
{
    public const string Header = "time_s,channel_0,channel_1,source";

    public static string FormatCsv(IEnumerable<RecordedSample> samples)
    {
        StringBuilder builder = new();
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
        CancellationToken cancellationToken = default)
    {
        await using StreamWriter writer = new(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            bufferSize: 16 * 1024,
            leaveOpen: true);

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
}
