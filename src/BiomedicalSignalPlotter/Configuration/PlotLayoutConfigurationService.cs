using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Configuration;

public static class PlotLayoutConfigurationService
{
    public const int MinimumPlotCount = 1;
    public const int MaximumPlotCount = 3;
    public const int DefaultPlotCount = 1;
    public const int MaximumReferenceBarsPerPlot = 5;
    public const double DefaultManualYMinimum = 0.0;
    public const double DefaultManualYMaximum = 1.0;

    public static PlotLayoutConfiguration CreateDefault()
    {
        return new PlotLayoutConfiguration(
            DefaultPlotCount,
            Enumerable.Repeat(ChannelPlotAssignment.Plot1, AnalogChannelLimits.Maximum).ToArray());
    }

    public static PlotPanelConfiguration CreateDefaultPanel()
    {
        return new PlotPanelConfiguration(
            useAutoYRange: true,
            DefaultManualYMinimum,
            DefaultManualYMaximum);
    }

    public static ReferenceBarConfiguration CreateDefaultReferenceBar()
    {
        return new ReferenceBarConfiguration(
            IsEnabled: false,
            Value: 0.0,
            Label: string.Empty);
    }

    public static int ValidatePlotCount(int plotCount)
    {
        if (plotCount is < MinimumPlotCount or > MaximumPlotCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(plotCount),
                $"Plot count must be between {MinimumPlotCount} and {MaximumPlotCount}.");
        }

        return plotCount;
    }

    public static PlotLayoutConfiguration ApplyPlotCount(
        PlotLayoutConfiguration configuration,
        int plotCount)
    {
        ValidatePlotCount(plotCount);

        ChannelPlotAssignment[] assignments = configuration.ChannelAssignments
            .Select(assignment => IsAvailableForPlotCount(assignment, plotCount)
                ? assignment
                : ChannelPlotAssignment.Plot1)
            .ToArray();

        return new PlotLayoutConfiguration(plotCount, assignments, configuration.PlotPanels);
    }

    public static PlotLayoutConfiguration AssignChannel(
        PlotLayoutConfiguration configuration,
        int channelIndex,
        ChannelPlotAssignment assignment)
    {
        ValidateChannelIndex(channelIndex);
        ValidateAssignment(assignment, configuration.PlotCount);

        ChannelPlotAssignment[] assignments = configuration.ChannelAssignments.ToArray();
        assignments[channelIndex] = assignment;
        return new PlotLayoutConfiguration(configuration.PlotCount, assignments, configuration.PlotPanels);
    }

    public static PlotLayoutConfiguration AssignChannels(
        PlotLayoutConfiguration configuration,
        IReadOnlyList<ChannelPlotAssignment> assignments)
    {
        ChannelPlotAssignment[] normalizedAssignments = configuration.ChannelAssignments.ToArray();

        for (int channelIndex = 0; channelIndex < normalizedAssignments.Length && channelIndex < assignments.Count; channelIndex++)
        {
            ValidateAssignment(assignments[channelIndex], configuration.PlotCount);
            normalizedAssignments[channelIndex] = assignments[channelIndex];
        }

        return new PlotLayoutConfiguration(configuration.PlotCount, normalizedAssignments, configuration.PlotPanels);
    }

    public static PlotLayoutConfiguration ApplyPanelYRange(
        PlotLayoutConfiguration configuration,
        int plotIndex,
        bool useAutoYRange,
        double manualYMinimum,
        double manualYMaximum)
    {
        ValidatePlotIndex(plotIndex);

        if (!useAutoYRange)
        {
            ValidateManualYRange(manualYMinimum, manualYMaximum);
        }

        PlotPanelConfiguration[] panels = configuration.PlotPanels.ToArray();
        panels[plotIndex] = new PlotPanelConfiguration(useAutoYRange, manualYMinimum, manualYMaximum);

        return new PlotLayoutConfiguration(
            configuration.PlotCount,
            configuration.ChannelAssignments,
            panels);
    }

    public static PlotLayoutConfiguration ApplyReferenceBar(
        PlotLayoutConfiguration configuration,
        int plotIndex,
        int referenceBarIndex,
        bool isEnabled,
        double value,
        string label)
    {
        ValidatePlotIndex(plotIndex);
        ValidateReferenceBarIndex(referenceBarIndex);
        ValidateReferenceBarValue(value);

        PlotPanelConfiguration[] panels = configuration.PlotPanels.ToArray();
        PlotPanelConfiguration panel = panels[plotIndex];
        ReferenceBarConfiguration[] bars = panel.ReferenceBars.ToArray();
        bars[referenceBarIndex] = new ReferenceBarConfiguration(
            isEnabled,
            value,
            NormalizeReferenceBarLabel(label));

        panels[plotIndex] = new PlotPanelConfiguration(
            panel.UseAutoYRange,
            panel.ManualYMinimum,
            panel.ManualYMaximum,
            bars);

        return new PlotLayoutConfiguration(
            configuration.PlotCount,
            configuration.ChannelAssignments,
            panels);
    }

    public static void ValidateReferenceBarValue(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Reference bar value must be a finite number.");
        }
    }

    public static void ValidateManualYRange(double manualYMinimum, double manualYMaximum)
    {
        if (!double.IsFinite(manualYMinimum) ||
            !double.IsFinite(manualYMaximum) ||
            manualYMinimum >= manualYMaximum)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manualYMinimum),
                "Manual Y minimum must be less than manual Y maximum.");
        }
    }

    public static IReadOnlyList<ChannelPlotAssignment> GetAvailableAssignments(int plotCount)
    {
        ValidatePlotCount(plotCount);

        return Enumerable.Range(1, plotCount)
            .Select(plotNumber => (ChannelPlotAssignment)plotNumber)
            .Prepend(ChannelPlotAssignment.Hidden)
            .ToArray();
    }

    public static string GetAssignmentDisplayName(ChannelPlotAssignment assignment)
    {
        return assignment switch
        {
            ChannelPlotAssignment.Hidden => "Hidden",
            ChannelPlotAssignment.Plot1 => "Plot 1",
            ChannelPlotAssignment.Plot2 => "Plot 2",
            ChannelPlotAssignment.Plot3 => "Plot 3",
            _ => assignment.ToString()
        };
    }

    public static int? GetPlotIndex(ChannelPlotAssignment assignment)
    {
        return assignment == ChannelPlotAssignment.Hidden
            ? null
            : (int)assignment - 1;
    }

    private static void ValidateAssignment(ChannelPlotAssignment assignment, int plotCount)
    {
        if (!IsAvailableForPlotCount(assignment, plotCount))
        {
            throw new ArgumentOutOfRangeException(
                nameof(assignment),
                $"Assignment must be Hidden or Plot 1 through Plot {plotCount}.");
        }
    }

    private static bool IsAvailableForPlotCount(ChannelPlotAssignment assignment, int plotCount)
    {
        return assignment == ChannelPlotAssignment.Hidden ||
            ((int)assignment >= MinimumPlotCount && (int)assignment <= plotCount);
    }

    private static void ValidateChannelIndex(int channelIndex)
    {
        if (channelIndex is < 0 or >= AnalogChannelLimits.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(channelIndex), "Channel index must be between 0 and 5.");
        }
    }

    private static void ValidatePlotIndex(int plotIndex)
    {
        if (plotIndex is < 0 or >= MaximumPlotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(plotIndex), "Plot index must be between 0 and 2.");
        }
    }

    private static void ValidateReferenceBarIndex(int referenceBarIndex)
    {
        if (referenceBarIndex is < 0 or >= MaximumReferenceBarsPerPlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(referenceBarIndex),
                $"Reference bar index must be between 0 and {MaximumReferenceBarsPerPlot - 1}.");
        }
    }

    private static string NormalizeReferenceBarLabel(string label)
    {
        return string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
    }
}
