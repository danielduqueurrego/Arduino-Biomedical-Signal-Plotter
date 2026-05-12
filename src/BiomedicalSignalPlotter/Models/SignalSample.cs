namespace BiomedicalSignalPlotter.Models;

public readonly record struct SignalSample(
    double TimeSeconds,
    double Channel1,
    double Channel2);
