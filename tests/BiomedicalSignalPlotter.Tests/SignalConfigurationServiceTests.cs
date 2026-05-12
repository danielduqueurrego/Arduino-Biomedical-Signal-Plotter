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
        Assert.Equal("Channel 0", configuration.Channel0.Label);
        Assert.Equal("Channel 1", configuration.Channel1.Label);
        Assert.Equal("ADC counts", configuration.Channel0.Unit);
        Assert.Equal("ADC counts", configuration.Channel1.Unit);
        Assert.Equal(10, configuration.AdcBits);
        Assert.Equal(5.0, configuration.ReferenceVoltage);
        Assert.Equal(SignalDisplayMode.RawAdcCounts, configuration.DisplayMode);
    }

    [Fact]
    public void ApplyPreset_ReturnsExpectedEcgDefaults()
    {
        SignalConfiguration configuration = SignalConfigurationService.ApplyPreset(SignalMode.Ecg);

        Assert.Equal(SignalMode.Ecg, configuration.Mode);
        Assert.Equal("ECG", configuration.Channel0.Label);
        Assert.Equal("Auxiliary", configuration.Channel1.Label);
        Assert.Equal("ADC counts", configuration.Channel0.Unit);
        Assert.Equal("ADC counts", configuration.Channel1.Unit);
        Assert.Equal(6.0, configuration.PlotWindowSeconds);
    }

    [Fact]
    public void ApplyManualChannelTextEdit_SwitchesPresetToCustomAndPreservesEdits()
    {
        SignalConfiguration preset = SignalConfigurationService.ApplyPreset(SignalMode.EmgForcePressure);

        SignalConfiguration edited = SignalConfigurationService.ApplyManualChannelTextEdit(
            preset,
            "Raw EMG",
            "mV",
            "Grip force",
            "N");

        Assert.Equal(SignalMode.Custom, edited.Mode);
        Assert.Equal("Raw EMG", edited.Channel0.Label);
        Assert.Equal("mV", edited.Channel0.Unit);
        Assert.Equal("Grip force", edited.Channel1.Label);
        Assert.Equal("N", edited.Channel1.Unit);
    }
}
