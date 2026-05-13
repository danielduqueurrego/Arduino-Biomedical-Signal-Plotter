using BiomedicalSignalPlotter.Models;
using BiomedicalSignalPlotter.Services;

namespace BiomedicalSignalPlotter.Tests;

public class SimulatedDataServiceTests
{
    [Fact]
    public void CreateSample_ReturnsFiniteTwoChannelSample()
    {
        SignalSample sample = SimulatedDataService.CreateSample(sampleIndex: 100, sampleRateHz: 200.0);

        Assert.Equal(0.5, sample.TimeSeconds, precision: 6);
        Assert.Equal(2, sample.ChannelCount);
        Assert.All(sample.Values, value => Assert.True(double.IsFinite(value)));
    }

    [Fact]
    public void CreateSample_ReturnsRequestedChannelCount()
    {
        SignalSample sample = SimulatedDataService.CreateSample(
            sampleIndex: 100,
            sampleRateHz: 200.0,
            channelCount: 6);

        Assert.Equal(6, sample.ChannelCount);
        Assert.All(sample.Values, value => Assert.True(double.IsFinite(value)));
    }

    [Fact]
    public void CreateSample_RejectsInvalidSampleRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SimulatedDataService.CreateSample(sampleIndex: 0, sampleRateHz: 0.0));
    }
}
