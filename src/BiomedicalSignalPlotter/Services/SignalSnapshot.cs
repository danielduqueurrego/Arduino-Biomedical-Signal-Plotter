namespace BiomedicalSignalPlotter.Services;

public sealed record SignalSnapshot(
    double[] TimeSeconds,
    double[][] Channels)
{
    public int Count => TimeSeconds.Length;

    public int ChannelCount => Channels.Length;
}
