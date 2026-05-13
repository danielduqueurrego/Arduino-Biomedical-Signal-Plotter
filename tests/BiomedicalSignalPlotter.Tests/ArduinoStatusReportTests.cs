using BiomedicalSignalPlotter.Arduino;

namespace BiomedicalSignalPlotter.Tests;

public class ArduinoStatusReportTests
{
    [Fact]
    public void TryParse_ParsesStatusLine()
    {
        bool parsed = ArduinoStatusReport.TryParse(
            "#STATUS CHANNEL_COUNT=6 ADC_BITS=14 SAMPLE_RATE_HZ=500 STREAMING=1",
            out ArduinoStatusReport? status);

        Assert.True(parsed);
        Assert.NotNull(status);
        Assert.Equal(6, status.ChannelCount);
        Assert.Equal(14, status.AdcBits);
        Assert.Equal(500, status.SampleRateHz);
        Assert.True(status.Streaming);
    }

    [Fact]
    public void Matches_ReturnsTrueForExpectedSettings()
    {
        ArduinoStatusReport status = new(ChannelCount: 2, AdcBits: 14, SampleRateHz: 250, Streaming: true);
        ArduinoDeviceSettings settings = new(channelCount: 2, adcBits: 14, sampleRateHz: 250);

        Assert.True(status.Matches(settings));
    }

    [Fact]
    public void Matches_ReturnsFalseForMismatchedSampleRate()
    {
        ArduinoStatusReport status = new(ChannelCount: 2, AdcBits: 14, SampleRateHz: 100, Streaming: true);
        ArduinoDeviceSettings settings = new(channelCount: 2, adcBits: 14, sampleRateHz: 250);

        Assert.False(status.Matches(settings));
    }

    [Theory]
    [InlineData("")]
    [InlineData("#OK SAMPLE_RATE_HZ 250")]
    [InlineData("#STATUS CHANNEL_COUNT=2 ADC_BITS=14 STREAMING=1")]
    public void TryParse_RejectsNonStatusOrIncompleteLines(string line)
    {
        bool parsed = ArduinoStatusReport.TryParse(line, out ArduinoStatusReport? status);

        Assert.False(parsed);
        Assert.Null(status);
    }
}
