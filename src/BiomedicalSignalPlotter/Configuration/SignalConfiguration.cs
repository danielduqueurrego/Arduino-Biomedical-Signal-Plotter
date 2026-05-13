namespace BiomedicalSignalPlotter.Configuration;

public sealed record SignalConfiguration(
    SignalMode Mode,
    ChannelConfiguration[] Channels,
    int ChannelCount,
    int AdcBits,
    double ReferenceVoltage,
    SignalDisplayMode DisplayMode,
    double PlotWindowSeconds,
    PlotLayoutConfiguration PlotLayout)
{
    public ChannelConfiguration Channel0 => Channels[0];

    public ChannelConfiguration Channel1 => Channels[1];
}
