namespace BiomedicalSignalPlotter.Configuration;

public sealed record PlotPanelConfiguration
{
    public PlotPanelConfiguration(
        bool useAutoYRange,
        double manualYMinimum,
        double manualYMaximum,
        IReadOnlyList<ReferenceBarConfiguration>? referenceBars = null)
    {
        UseAutoYRange = useAutoYRange;
        ManualYMinimum = manualYMinimum;
        ManualYMaximum = manualYMaximum;
        ReferenceBars = NormalizeReferenceBars(referenceBars);
    }

    public bool UseAutoYRange { get; }

    public double ManualYMinimum { get; }

    public double ManualYMaximum { get; }

    public ReferenceBarConfiguration[] ReferenceBars { get; }

    private static ReferenceBarConfiguration[] NormalizeReferenceBars(IReadOnlyList<ReferenceBarConfiguration>? referenceBars)
    {
        ReferenceBarConfiguration[] bars = new ReferenceBarConfiguration[PlotLayoutConfigurationService.MaximumReferenceBarsPerPlot];

        for (int i = 0; i < bars.Length; i++)
        {
            bars[i] = i < referenceBars?.Count
                ? referenceBars[i]
                : PlotLayoutConfigurationService.CreateDefaultReferenceBar();
        }

        return bars;
    }
}
