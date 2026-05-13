using BiomedicalSignalPlotter.Configuration;

namespace BiomedicalSignalPlotter.Tests;

public class PlotLayoutConfigurationServiceTests
{
    [Fact]
    public void CreateDefault_UsesOnePlotWithAllChannelsVisible()
    {
        PlotLayoutConfiguration configuration = PlotLayoutConfigurationService.CreateDefault();

        Assert.Equal(1, configuration.PlotCount);
        Assert.All(configuration.ChannelAssignments, assignment =>
            Assert.Equal(ChannelPlotAssignment.Plot1, assignment));
        Assert.Equal(3, configuration.PlotPanels.Length);
        Assert.All(configuration.PlotPanels, panel =>
        {
            Assert.True(panel.UseAutoYRange);
            Assert.Equal(0.0, panel.ManualYMinimum);
            Assert.Equal(1.0, panel.ManualYMaximum);
            Assert.Equal(5, panel.ReferenceBars.Length);
            Assert.All(panel.ReferenceBars, referenceBar => Assert.False(referenceBar.IsEnabled));
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ApplyPlotCount_AcceptsSupportedCounts(int plotCount)
    {
        PlotLayoutConfiguration configuration = PlotLayoutConfigurationService.ApplyPlotCount(
            PlotLayoutConfigurationService.CreateDefault(),
            plotCount);

        Assert.Equal(plotCount, configuration.PlotCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void ApplyPlotCount_RejectsUnsupportedCounts(int plotCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PlotLayoutConfigurationService.ApplyPlotCount(
                PlotLayoutConfigurationService.CreateDefault(),
                plotCount));
    }

    [Fact]
    public void AssignChannel_RoutesChannelToSelectedPlot()
    {
        PlotLayoutConfiguration configuration = PlotLayoutConfigurationService.ApplyPlotCount(
            PlotLayoutConfigurationService.CreateDefault(),
            2);

        PlotLayoutConfiguration updated = PlotLayoutConfigurationService.AssignChannel(
            configuration,
            channelIndex: 1,
            ChannelPlotAssignment.Plot2);

        Assert.Equal(ChannelPlotAssignment.Plot2, updated.ChannelAssignments[1]);
        Assert.Equal(1, PlotLayoutConfigurationService.GetPlotIndex(updated.ChannelAssignments[1]));
    }

    [Fact]
    public void AssignChannel_CanHideChannel()
    {
        PlotLayoutConfiguration configuration = PlotLayoutConfigurationService.AssignChannel(
            PlotLayoutConfigurationService.CreateDefault(),
            channelIndex: 0,
            ChannelPlotAssignment.Hidden);

        Assert.Equal(ChannelPlotAssignment.Hidden, configuration.ChannelAssignments[0]);
        Assert.Null(PlotLayoutConfigurationService.GetPlotIndex(configuration.ChannelAssignments[0]));
    }

    [Fact]
    public void ApplyPlotCount_MovesUnavailableAssignmentsBackToPlotOne()
    {
        PlotLayoutConfiguration configuration = PlotLayoutConfigurationService.ApplyPlotCount(
            PlotLayoutConfigurationService.CreateDefault(),
            3);
        configuration = PlotLayoutConfigurationService.AssignChannel(
            configuration,
            channelIndex: 2,
            ChannelPlotAssignment.Plot3);

        PlotLayoutConfiguration reduced = PlotLayoutConfigurationService.ApplyPlotCount(configuration, 2);

        Assert.Equal(2, reduced.PlotCount);
        Assert.Equal(ChannelPlotAssignment.Plot1, reduced.ChannelAssignments[2]);
    }

    [Fact]
    public void GetAvailableAssignments_OnlyIncludesSelectedPlotsAndHidden()
    {
        IReadOnlyList<ChannelPlotAssignment> assignments =
            PlotLayoutConfigurationService.GetAvailableAssignments(2);

        Assert.Equal(
            [ChannelPlotAssignment.Hidden, ChannelPlotAssignment.Plot1, ChannelPlotAssignment.Plot2],
            assignments);
    }

    [Fact]
    public void ApplyPanelYRange_StoresManualRangePerPlot()
    {
        PlotLayoutConfiguration configuration = PlotLayoutConfigurationService.CreateDefault();

        PlotLayoutConfiguration updated = PlotLayoutConfigurationService.ApplyPanelYRange(
            configuration,
            plotIndex: 0,
            useAutoYRange: false,
            manualYMinimum: -2.0,
            manualYMaximum: 2.0);

        Assert.False(updated.PlotPanels[0].UseAutoYRange);
        Assert.Equal(-2.0, updated.PlotPanels[0].ManualYMinimum);
        Assert.Equal(2.0, updated.PlotPanels[0].ManualYMaximum);
        Assert.True(updated.PlotPanels[1].UseAutoYRange);
    }

    [Theory]
    [InlineData(1.0, 1.0)]
    [InlineData(2.0, 1.0)]
    public void ApplyPanelYRange_RejectsInvalidManualRange(double yMinimum, double yMaximum)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PlotLayoutConfigurationService.ApplyPanelYRange(
                PlotLayoutConfigurationService.CreateDefault(),
                plotIndex: 0,
                useAutoYRange: false,
                manualYMinimum: yMinimum,
                manualYMaximum: yMaximum));
    }

    [Fact]
    public void ApplyReferenceBar_EnablesReferenceBar()
    {
        PlotLayoutConfiguration configuration = PlotLayoutConfigurationService.ApplyReferenceBar(
            PlotLayoutConfigurationService.CreateDefault(),
            plotIndex: 0,
            referenceBarIndex: 0,
            isEnabled: true,
            value: 512.0,
            label: "Target ADC");

        ReferenceBarConfiguration referenceBar = configuration.PlotPanels[0].ReferenceBars[0];
        Assert.True(referenceBar.IsEnabled);
        Assert.Equal(512.0, referenceBar.Value);
        Assert.Equal("Target ADC", referenceBar.Label);
    }

    [Fact]
    public void ApplyReferenceBar_DisablesReferenceBar()
    {
        PlotLayoutConfiguration configuration = PlotLayoutConfigurationService.ApplyReferenceBar(
            PlotLayoutConfigurationService.CreateDefault(),
            plotIndex: 0,
            referenceBarIndex: 0,
            isEnabled: true,
            value: 512.0,
            label: "Target ADC");

        PlotLayoutConfiguration disabled = PlotLayoutConfigurationService.ApplyReferenceBar(
            configuration,
            plotIndex: 0,
            referenceBarIndex: 0,
            isEnabled: false,
            value: 512.0,
            label: "Target ADC");

        Assert.False(disabled.PlotPanels[0].ReferenceBars[0].IsEnabled);
        Assert.Equal(512.0, disabled.PlotPanels[0].ReferenceBars[0].Value);
    }

    [Fact]
    public void ApplyReferenceBar_RejectsInvalidValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PlotLayoutConfigurationService.ApplyReferenceBar(
                PlotLayoutConfigurationService.CreateDefault(),
                plotIndex: 0,
                referenceBarIndex: 0,
                isEnabled: true,
                value: double.PositiveInfinity,
                label: "Invalid"));
    }

    [Fact]
    public void ApplyReferenceBar_StoresBarsPerSubplotIndependently()
    {
        PlotLayoutConfiguration configuration = PlotLayoutConfigurationService.ApplyPlotCount(
            PlotLayoutConfigurationService.CreateDefault(),
            2);

        PlotLayoutConfiguration updated = PlotLayoutConfigurationService.ApplyReferenceBar(
            configuration,
            plotIndex: 1,
            referenceBarIndex: 0,
            isEnabled: true,
            value: 2.5,
            label: "Voltage target");

        Assert.False(updated.PlotPanels[0].ReferenceBars[0].IsEnabled);
        Assert.True(updated.PlotPanels[1].ReferenceBars[0].IsEnabled);
        Assert.Equal(2.5, updated.PlotPanels[1].ReferenceBars[0].Value);
    }
}
