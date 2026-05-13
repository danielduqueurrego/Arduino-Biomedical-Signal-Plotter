namespace BiomedicalSignalPlotter.Recording;

public sealed record CsvRecordingMetadata(
    string Mode,
    int ChannelCount,
    IReadOnlyList<string> ChannelLabels,
    IReadOnlyList<string> ChannelUnits,
    int AdcBits,
    double ReferenceVoltage,
    string DisplayMode);
