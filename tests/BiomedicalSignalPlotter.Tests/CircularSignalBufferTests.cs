using BiomedicalSignalPlotter.Models;
using BiomedicalSignalPlotter.Services;

namespace BiomedicalSignalPlotter.Tests;

public class CircularSignalBufferTests
{
    [Fact]
    public void Add_IncreasesCountAndStoresVariableChannelSamples()
    {
        CircularSignalBuffer buffer = new(capacity: 3);

        buffer.Add(new SignalSample(0.0, [10.0, 20.0, 30.0]));
        buffer.Add(new SignalSample(0.1, [11.0, 21.0, 31.0]));

        SignalSnapshot snapshot = buffer.Snapshot(channelCount: 3);

        Assert.Equal(2, buffer.Count);
        Assert.Equal(3, snapshot.ChannelCount);
        Assert.Equal([0.0, 0.1], snapshot.TimeSeconds);
        Assert.Equal([10.0, 11.0], snapshot.Channels[0]);
        Assert.Equal([20.0, 21.0], snapshot.Channels[1]);
        Assert.Equal([30.0, 31.0], snapshot.Channels[2]);
    }

    [Fact]
    public void Add_WrapsAtCapacity()
    {
        CircularSignalBuffer buffer = new(capacity: 3);

        buffer.Add(new SignalSample(0.0, [10.0, 20.0]));
        buffer.Add(new SignalSample(0.1, [11.0, 21.0]));
        buffer.Add(new SignalSample(0.2, [12.0, 22.0]));
        buffer.Add(new SignalSample(0.3, [13.0, 23.0]));

        SignalSnapshot snapshot = buffer.Snapshot(channelCount: 2);

        Assert.Equal(3, snapshot.Count);
        Assert.Equal([0.1, 0.2, 0.3], snapshot.TimeSeconds);
        Assert.Equal([11.0, 12.0, 13.0], snapshot.Channels[0]);
        Assert.Equal([21.0, 22.0, 23.0], snapshot.Channels[1]);
    }

    [Fact]
    public void Snapshot_ReturnsSamplesInChronologicalOrder()
    {
        CircularSignalBuffer buffer = new(capacity: 5);

        buffer.Add(new SignalSample(0.0, [10.0, 20.0]));
        buffer.Add(new SignalSample(0.1, [11.0, 21.0]));
        buffer.Add(new SignalSample(0.2, [12.0, 22.0]));

        SignalSnapshot snapshot = buffer.Snapshot(channelCount: 2);

        Assert.Equal([0.0, 0.1, 0.2], snapshot.TimeSeconds);
        Assert.Equal([10.0, 11.0, 12.0], snapshot.Channels[0]);
        Assert.Equal([20.0, 21.0, 22.0], snapshot.Channels[1]);
    }

    [Fact]
    public void Snapshot_UsesRequestedChannelCount()
    {
        CircularSignalBuffer buffer = new(capacity: 3);
        buffer.Add(new SignalSample(0.0, [10.0, 20.0, 30.0, 40.0, 50.0, 60.0]));

        SignalSnapshot snapshot = buffer.Snapshot(channelCount: 1);

        Assert.Equal(1, snapshot.ChannelCount);
        Assert.Equal([10.0], snapshot.Channels[0]);
    }

    [Fact]
    public void Clear_RemovesBufferedSamples()
    {
        CircularSignalBuffer buffer = new(capacity: 3);
        buffer.Add(new SignalSample(0.0, [10.0, 20.0]));

        buffer.Clear();

        Assert.Equal(0, buffer.Count);
        Assert.Empty(buffer.Snapshot(channelCount: 2).TimeSeconds);
    }
}
