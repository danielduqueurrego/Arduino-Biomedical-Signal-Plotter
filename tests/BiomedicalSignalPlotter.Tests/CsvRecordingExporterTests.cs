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
            new(0.0, [512.0, 310.0], RecordedSampleSource.Serial),
            new(0.004, [0.52, 0.31], RecordedSampleSource.Simulated)
        ];

        string[] lines = SplitLines(CsvRecordingExporter.FormatCsv(samples));

        Assert.Equal("time_s,channel_0,channel_1,source", lines[0]);
        Assert.Equal("0.000,512,310,serial", lines[1]);
        Assert.Equal("0.004,0.52,0.31,simulated", lines[2]);
    }

    [Theory]
    [InlineData(1, "time_s,channel_0,source")]
    [InlineData(2, "time_s,channel_0,channel_1,source")]
    [InlineData(6, "time_s,channel_0,channel_1,channel_2,channel_3,channel_4,channel_5,source")]
    public void BuildHeader_WritesVariableChannelHeaders(int channelCount, string expectedHeader)
    {
        Assert.Equal(expectedHeader, CsvRecordingExporter.BuildHeader(channelCount));
    }

    [Fact]
    public void FormatCsv_WritesSixChannelRows()
    {
        string[] lines = SplitLines(CsvRecordingExporter.FormatCsv(
        [
            new RecordedSample(0.004, [512.0, 310.0, 203.0, 102.0, 850.0, 914.0], RecordedSampleSource.Serial)
        ]));

        Assert.Equal("time_s,channel_0,channel_1,channel_2,channel_3,channel_4,channel_5,source", lines[0]);
        Assert.Equal("0.004,512,310,203,102,850,914,serial", lines[1]);
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
                new RecordedSample(1.25, [0.52, 0.31], RecordedSampleSource.Simulated)
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
            new(0.0, [512.0, 310.0], RecordedSampleSource.Serial)
        ];

        await using MemoryStream stream = new();
        await CsvRecordingExporter.WriteAsync(stream, samples);

        string csv = Encoding.UTF8.GetString(stream.ToArray());
        string[] lines = SplitLines(csv);

        Assert.Equal("time_s,channel_0,channel_1,source", lines[0]);
        Assert.Equal("0.000,512,310,serial", lines[1]);
    }

    [Fact]
    public void FormatCsv_WritesMetadataCommentsAboveHeader()
    {
        CsvRecordingMetadata metadata = new(
            "Custom",
            2,
            ["EMG", "Force"],
            ["ADC counts", "ADC counts"],
            10,
            5.0,
            "Raw ADC counts");

        string[] lines = SplitLines(CsvRecordingExporter.FormatCsv([], metadata));

        Assert.Equal("# mode=Custom", lines[0]);
        Assert.Equal("# channel_count=2", lines[1]);
        Assert.Equal("# channel_0_label=EMG", lines[2]);
        Assert.Equal("# channel_0_unit=ADC counts", lines[3]);
        Assert.Equal("# channel_1_label=Force", lines[4]);
        Assert.Equal("# channel_1_unit=ADC counts", lines[5]);
        Assert.Equal("# adc_bits=10", lines[6]);
        Assert.Equal("# reference_voltage=5", lines[7]);
        Assert.Equal("# display_mode=Raw ADC counts", lines[8]);
        Assert.Equal("time_s,channel_0,channel_1,source", lines[9]);
    }

    private static string[] SplitLines(string text)
    {
        return text.Split(
            ["\r\n", "\n"],
            StringSplitOptions.RemoveEmptyEntries);
    }
}
