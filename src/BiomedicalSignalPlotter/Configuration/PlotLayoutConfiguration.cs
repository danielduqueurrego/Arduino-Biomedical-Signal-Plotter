using BiomedicalSignalPlotter.Models;

namespace BiomedicalSignalPlotter.Configuration;

public sealed record PlotLayoutConfiguration
{
    public PlotLayoutConfiguration(int plotCount, IReadOnlyList<ChannelPlotAssignment> channelAssignments)
    {
        PlotCount = PlotLayoutConfigurationService.ValidatePlotCount(plotCount);
        ChannelAssignments = NormalizeAssignments(channelAssignments, PlotCount);
    }

    public int PlotCount { get; }

    public ChannelPlotAssignment[] ChannelAssignments { get; }

    private static ChannelPlotAssignment[] NormalizeAssignments(
        IReadOnlyList<ChannelPlotAssignment> channelAssignments,
        int plotCount)
    {
        ChannelPlotAssignment[] assignments = new ChannelPlotAssignment[AnalogChannelLimits.Maximum];

        for (int i = 0; i < assignments.Length; i++)
        {
            ChannelPlotAssignment assignment = i < channelAssignments.Count
                ? channelAssignments[i]
                : ChannelPlotAssignment.Plot1;

            if (assignment != ChannelPlotAssignment.Hidden &&
                ((int)assignment < PlotLayoutConfigurationService.MinimumPlotCount || (int)assignment > plotCount))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(channelAssignments),
                    $"Assignment must be Hidden or Plot 1 through Plot {plotCount}.");
            }

            assignments[i] = assignment;
        }

        return assignments;
    }
}
