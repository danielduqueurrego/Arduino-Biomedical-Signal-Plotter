using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Serial;

public sealed class SerialChannelValues
{
    public SerialChannelValues(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        AnalogChannelLimits.Validate(values.Count);
        Values = values.ToArray();
    }

    public double[] Values { get; }

    public int ChannelCount => Values.Length;
}
