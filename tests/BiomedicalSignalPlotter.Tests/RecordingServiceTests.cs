using BiomedicalSignalPlotter.Models;
using BiomedicalSignalPlotter.Recording;

namespace BiomedicalSignalPlotter.Tests;

public class RecordingServiceTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    public void Record_StoresRequestedChannelCount(int channelCount)
    {
        RecordingService service = new();
        SignalSample sample = new(0.0, Enumerable.Range(0, channelCount).Select(value => (double)value).ToArray());

        service.Start();
        service.Record(sample, RecordedSampleSource.Serial, channelCount);

        RecordedSample recordedSample = Assert.Single(service.Snapshot());
        Assert.Equal(channelCount, recordedSample.ChannelCount);
        Assert.Equal(sample.Values, recordedSample.Values);
    }

    [Fact]
    public void Record_DoesNotMixChannelCountsInOneRecording()
    {
        RecordingService service = new();

        service.Start();
        service.Record(new SignalSample(0.0, [1.0, 2.0]), RecordedSampleSource.Serial, channelCount: 2);
        service.Record(new SignalSample(0.1, [3.0]), RecordedSampleSource.Serial, channelCount: 1);

        RecordedSample recordedSample = Assert.Single(service.Snapshot());
        Assert.Equal(2, recordedSample.ChannelCount);
        Assert.Equal([1.0, 2.0], recordedSample.Values);
    }
}
