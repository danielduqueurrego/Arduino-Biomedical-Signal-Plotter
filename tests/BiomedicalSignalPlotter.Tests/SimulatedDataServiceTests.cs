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
        Assert.True(double.IsFinite(sample.Channel1));
        Assert.True(double.IsFinite(sample.Channel2));
    }

    [Fact]
    public void CreateSample_RejectsInvalidSampleRate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SimulatedDataService.CreateSample(sampleIndex: 0, sampleRateHz: 0.0));
    }
}
