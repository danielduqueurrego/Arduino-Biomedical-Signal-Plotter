using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Recording;

public sealed class RecordedSample
{
    public RecordedSample(double timeSeconds, IReadOnlyList<double> values, RecordedSampleSource source)
    {
        ArgumentNullException.ThrowIfNull(values);
        AnalogChannelLimits.Validate(values.Count);

        TimeSeconds = timeSeconds;
        Values = values.ToArray();
        Source = source;
    }

    public double TimeSeconds { get; }

    public double[] Values { get; }

    public int ChannelCount => Values.Length;

    public RecordedSampleSource Source { get; }
}
