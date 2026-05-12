using BiomedicalSignalPlotter.Models;
using BiomedicalSignalPlotter.Services;

namespace BiomedicalSignalPlotter.Tests;

public class CircularSignalBufferTests
{
    [Fact]
    public void Snapshot_ReturnsSamplesInChronologicalOrder()
    {
        CircularSignalBuffer buffer = new(capacity: 3);

        buffer.Add(new SignalSample(0.0, 10.0, 20.0));
        buffer.Add(new SignalSample(0.1, 11.0, 21.0));
        buffer.Add(new SignalSample(0.2, 12.0, 22.0));
        buffer.Add(new SignalSample(0.3, 13.0, 23.0));

        SignalSnapshot snapshot = buffer.Snapshot();

        Assert.Equal(3, snapshot.Count);
        Assert.Equal([0.1, 0.2, 0.3], snapshot.TimeSeconds);
        Assert.Equal([11.0, 12.0, 13.0], snapshot.Channel1);
        Assert.Equal([21.0, 22.0, 23.0], snapshot.Channel2);
    }

    [Fact]
    public void Clear_RemovesBufferedSamples()
    {
        CircularSignalBuffer buffer = new(capacity: 3);
        buffer.Add(new SignalSample(0.0, 10.0, 20.0));

        buffer.Clear();

        Assert.Equal(0, buffer.Count);
        Assert.Empty(buffer.Snapshot().TimeSeconds);
    }
}
