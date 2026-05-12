namespace BiomedicalSignalPlotter.Services;

public sealed record SignalSnapshot(
    double[] TimeSeconds,
    double[] Channel1,
    double[] Channel2)
{
    public int Count => TimeSeconds.Length;
}
