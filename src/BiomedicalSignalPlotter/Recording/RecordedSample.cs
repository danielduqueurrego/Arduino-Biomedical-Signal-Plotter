namespace BiomedicalSignalPlotter.Recording;

public readonly record struct RecordedSample(
    double TimeSeconds,
    double Channel0,
    double Channel1,
    RecordedSampleSource Source);
