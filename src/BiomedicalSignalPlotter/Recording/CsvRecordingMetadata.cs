namespace BiomedicalSignalPlotter.Recording;

public sealed record CsvRecordingMetadata(
    string Mode,
    string Channel0Label,
    string Channel0Unit,
    string Channel1Label,
    string Channel1Unit,
    int AdcBits,
    double ReferenceVoltage,
    string DisplayMode);
