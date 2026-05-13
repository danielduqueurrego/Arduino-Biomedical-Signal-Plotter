namespace BiomedicalSignalPlotter.Configuration;

public sealed record ReferenceBarConfiguration(
    bool IsEnabled,
    double Value,
    string Label);
