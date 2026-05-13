using BiomedicalSignalPlotter.Arduino;

namespace BiomedicalSignalPlotter.Tests;

public class ArduinoCommandBuilderTests
{
    [Fact]
    public void BuildApplySettingsSequence_FormatsExpectedCommands()
    {
        ArduinoDeviceSettings settings = new(channelCount: 6, adcBits: 14, sampleRateHz: 250);

        IReadOnlyList<string> commands = ArduinoCommandBuilder.BuildApplySettingsSequence(settings);

        Assert.Equal(
            [
                "#STOP",
                "#SET CHANNEL_COUNT 6",
                "#SET ADC_BITS 14",
                "#SET SAMPLE_RATE_HZ 250",
                "#STATUS",
                "#START"
            ],
            commands);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void ArduinoDeviceSettings_AcceptsSupportedChannelCounts(int channelCount)
    {
        ArduinoDeviceSettings settings = new(channelCount, adcBits: 14, sampleRateHz: 250);

        Assert.Equal(channelCount, settings.ChannelCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void ArduinoDeviceSettings_RejectsUnsupportedChannelCounts(int channelCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArduinoDeviceSettings(channelCount, adcBits: 14, sampleRateHz: 250));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    public void ArduinoDeviceSettings_AcceptsSupportedAdcBits(int adcBits)
    {
        ArduinoDeviceSettings settings = new(channelCount: 2, adcBits, sampleRateHz: 250);

        Assert.Equal(adcBits, settings.AdcBits);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(15)]
    public void ArduinoDeviceSettings_RejectsUnsupportedAdcBits(int adcBits)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArduinoDeviceSettings(channelCount: 2, adcBits, sampleRateHz: 250));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void ArduinoDeviceSettings_AcceptsSupportedSampleRates(int sampleRateHz)
    {
        ArduinoDeviceSettings settings = new(channelCount: 2, adcBits: 14, sampleRateHz);

        Assert.Equal(sampleRateHz, settings.SampleRateHz);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void ArduinoDeviceSettings_RejectsUnsupportedSampleRates(int sampleRateHz)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArduinoDeviceSettings(channelCount: 2, adcBits: 14, sampleRateHz));
    }
}
