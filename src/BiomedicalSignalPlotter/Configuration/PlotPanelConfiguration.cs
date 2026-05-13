namespace BiomedicalSignalPlotter.Configuration;

public sealed record PlotPanelConfiguration(
    bool UseAutoYRange,
    double ManualYMinimum,
    double ManualYMaximum);
