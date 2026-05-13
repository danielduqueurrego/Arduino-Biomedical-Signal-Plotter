using BiomedicalSignalPlotter.Configuration;

namespace BiomedicalSignalPlotter.Tests;

public class SignalConfigurationServiceTests
{
    [Fact]
    public void ConvertRawToDisplayValue_ConvertsCountsToVoltage()
    {
        SignalConfiguration configuration = SignalConfigurationService.CreateCustomDefault() with
        {
            AdcBits = 10,
            ReferenceVoltage = 5.0,
            DisplayMode = SignalDisplayMode.Voltage
        };

        double voltage = SignalConfigurationService.ConvertRawToDisplayValue(512, configuration);

        Assert.Equal(512.0 * 5.0 / 1023.0, voltage, precision: 6);
    }

    [Fact]
    public void CreateCustomDefault_UsesEditableArduinoCompatibleDefaults()
    {
        SignalConfiguration configuration = SignalConfigurationService.CreateCustomDefault();

        Assert.Equal(SignalMode.Custom, configuration.Mode);
        Assert.Equal(2, configuration.ChannelCount);
        Assert.Equal("A0", configuration.Channel0.Label);
        Assert.Equal("A1", configuration.Channel1.Label);
        Assert.Equal("ADC counts", configuration.Channel0.Unit);
        Assert.Equal("ADC counts", configuration.Channel1.Unit);
        Assert.Equal(14, configuration.AdcBits);
        Assert.Equal(5.0, configuration.ReferenceVoltage);
        Assert.Equal(SignalDisplayMode.RawAdcCounts, configuration.DisplayMode);
        Assert.Equal(1, configuration.PlotLayout.PlotCount);
    }

    [Fact]
    public void CreateCustomDefault_NamesChannelsAfterArduinoAnalogPins()
    {
        SignalConfiguration configuration = SignalConfigurationService.CreateCustomDefault();

        string[] labels = configuration.Channels
            .Select(channel => channel.Label)
            .ToArray();

        Assert.Equal(["A0", "A1", "A2", "A3", "A4", "A5"], labels);
    }

    [Fact]
    public void ApplyPreset_ReturnsExpectedEcgDefaults()
    {
        SignalConfiguration configuration = SignalConfigurationService.ApplyPreset(SignalMode.Ecg);

        Assert.Equal(SignalMode.Ecg, configuration.Mode);
        Assert.Equal(1, configuration.ChannelCount);
        Assert.Equal("ECG", configuration.Channel0.Label);
        Assert.Equal("Auxiliary", configuration.Channel1.Label);
        Assert.Equal("ADC counts", configuration.Channel0.Unit);
        Assert.Equal("ADC counts", configuration.Channel1.Unit);
        Assert.Equal(6.0, configuration.PlotWindowSeconds);
    }

    [Theory]
    [InlineData(SignalMode.GenericTwoChannel, 2)]
    [InlineData(SignalMode.EmgForcePressure, 2)]
    [InlineData(SignalMode.Ecg, 1)]
    [InlineData(SignalMode.PpgPulseOximetry, 1)]
    [InlineData(SignalMode.BloodPressure, 1)]
    public void ApplyPreset_ReturnsExpectedChannelCount(SignalMode mode, int expectedChannelCount)
    {
        SignalConfiguration configuration = SignalConfigurationService.ApplyPreset(mode);

        Assert.Equal(expectedChannelCount, configuration.ChannelCount);
    }

    [Fact]
    public void ApplyManualChannelTextEdit_SwitchesPresetToCustomAndPreservesEdits()
    {
        SignalConfiguration preset = SignalConfigurationService.ApplyPreset(SignalMode.EmgForcePressure);

        SignalConfiguration edited = SignalConfigurationService.ApplyManualChannelTextEdit(
            preset,
            ["Raw EMG", "Grip force"],
            ["mV", "N"]);

        Assert.Equal(SignalMode.Custom, edited.Mode);
        Assert.Equal("Raw EMG", edited.Channel0.Label);
        Assert.Equal("mV", edited.Channel0.Unit);
        Assert.Equal("Grip force", edited.Channel1.Label);
        Assert.Equal("N", edited.Channel1.Unit);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    public void ApplyChannelCount_AcceptsSupportedCounts(int channelCount)
    {
        SignalConfiguration configuration = SignalConfigurationService.ApplyChannelCount(
            SignalConfigurationService.CreateDefault(),
            channelCount);

        Assert.Equal(SignalMode.Custom, configuration.Mode);
        Assert.Equal(channelCount, configuration.ChannelCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void ApplyChannelCount_RejectsUnsupportedCounts(int channelCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SignalConfigurationService.ApplyChannelCount(SignalConfigurationService.CreateDefault(), channelCount));
    }

    [Fact]
    public void ApplyChannelPlotAssignment_UpdatesRoutingWithoutChangingChannelCount()
    {
        SignalConfiguration configuration = SignalConfigurationService.ApplyPlotCount(
            SignalConfigurationService.CreateDefault(),
            2);

        SignalConfiguration updated = SignalConfigurationService.ApplyChannelPlotAssignment(
            configuration,
            channelIndex: 1,
            ChannelPlotAssignment.Plot2);

        Assert.Equal(2, updated.ChannelCount);
        Assert.Equal(ChannelPlotAssignment.Plot2, updated.PlotLayout.ChannelAssignments[1]);
    }
}
