namespace BiomedicalSignalPlotter.Models;

public sealed class SignalSample
{
    public SignalSample(double timeSeconds, IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        AnalogChannelLimits.Validate(values.Count);

        TimeSeconds = timeSeconds;
        Values = values.ToArray();
    }

    public double TimeSeconds { get; }

    public double[] Values { get; }

    public int ChannelCount => Values.Length;
}
