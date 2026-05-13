namespace BiomedicalSignalPlotter.Models;

public static class AnalogChannelLimits
{
    public const int Minimum = 1;
    public const int Maximum = 6;
    public const int Default = 2;

    public static int Validate(int channelCount)
    {
        if (channelCount is < Minimum or > Maximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(channelCount),
                $"Channel count must be between {Minimum} and {Maximum}.");
        }

        return channelCount;
    }

    public static int Clamp(int channelCount)
    {
        return Math.Clamp(channelCount, Minimum, Maximum);
    }
}
