using System.Globalization;
using System.Text;
using BiomedicalSignalPlotter.Recording;

namespace BiomedicalSignalPlotter.Tests;

public class CsvRecordingExporterTests
{
    [Fact]
    public void FormatCsv_WritesHeaderRowsAndSources()
    {
        RecordedSample[] samples =
        [
            new(0.0, 512.0, 310.0, RecordedSampleSource.Serial),
            new(0.004, 0.52, 0.31, RecordedSampleSource.Simulated)
        ];

        string[] lines = SplitLines(CsvRecordingExporter.FormatCsv(samples));

        Assert.Equal("time_s,channel_0,channel_1,source", lines[0]);
        Assert.Equal("0.000,512,310,serial", lines[1]);
        Assert.Equal("0.004,0.52,0.31,simulated", lines[2]);
    }

    [Fact]
    public void FormatCsv_UsesInvariantCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            string csv = CsvRecordingExporter.FormatCsv(
            [
                new RecordedSample(1.25, 0.52, 0.31, RecordedSampleSource.Simulated)
            ]);

            Assert.Contains("1.250,0.52,0.31,simulated", csv);
            Assert.DoesNotContain("1,250", csv);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task WriteAsync_WritesCsvToStream()
    {
        RecordedSample[] samples =
        [
            new(0.0, 512.0, 310.0, RecordedSampleSource.Serial)
        ];

        await using MemoryStream stream = new();
        await CsvRecordingExporter.WriteAsync(stream, samples);

        string csv = Encoding.UTF8.GetString(stream.ToArray());
        string[] lines = SplitLines(csv);

        Assert.Equal("time_s,channel_0,channel_1,source", lines[0]);
        Assert.Equal("0.000,512,310,serial", lines[1]);
    }

    private static string[] SplitLines(string text)
    {
        return text.Split(
            ["\r\n", "\n"],
            StringSplitOptions.RemoveEmptyEntries);
    }
}
