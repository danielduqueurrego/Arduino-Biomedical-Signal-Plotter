namespace BiomedicalSignalPlotter.Configuration;

public sealed record SignalConfiguration(
    SignalMode Mode,
    ChannelConfiguration Channel0,
    ChannelConfiguration Channel1,
    int AdcBits,
    double ReferenceVoltage,
    SignalDisplayMode DisplayMode,
    double PlotWindowSeconds);
