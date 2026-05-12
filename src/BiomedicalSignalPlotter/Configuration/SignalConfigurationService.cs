using System.Globalization;

namespace BiomedicalSignalPlotter.Configuration;

public static class SignalConfigurationService
{
    public const int DefaultAdcBits = 10;
    public const double DefaultReferenceVoltage = 5.0;
    public const double DefaultPlotWindowSeconds = 10.0;

    private static readonly SignalModePreset[] Presets =
    [
        CreatePreset(
            SignalMode.Custom,
            "Custom",
            "Channel 0",
            "Channel 1",
            "ADC counts",
            "ADC counts",
            DefaultPlotWindowSeconds),
        CreatePreset(
            SignalMode.GenericTwoChannel,
            "Generic two-channel",
            "Channel 0",
            "Channel 1",
            "ADC counts",
            "ADC counts",
            DefaultPlotWindowSeconds),
        CreatePreset(
            SignalMode.EmgForcePressure,
            "EMG + Force/Pressure",
            "EMG",
            "Force/Pressure",
            "ADC counts",
            "ADC counts",
            5.0),
        CreatePreset(
            SignalMode.Ecg,
            "ECG",
            "ECG",
            "Auxiliary",
            "ADC counts",
            "ADC counts",
            6.0),
        CreatePreset(
            SignalMode.PpgPulseOximetry,
            "PPG / Pulse Oximetry",
            "PPG",
            "Auxiliary PPG",
            "ADC counts",
            "ADC counts",
            10.0),
        CreatePreset(
            SignalMode.BloodPressure,
            "Blood Pressure",
            "Pressure",
            "Pulse/Reference",
            "ADC counts",
            "ADC counts",
            15.0)
    ];

    public static IReadOnlyList<SignalModePreset> AllPresets => Presets;

    public static SignalConfiguration CreateDefault()
    {
        return GetPreset(SignalMode.GenericTwoChannel).Configuration;
    }

    public static SignalConfiguration CreateCustomDefault()
    {
        return GetPreset(SignalMode.Custom).Configuration;
    }

    public static SignalModePreset GetPreset(SignalMode mode)
    {
        return Presets.Single(preset => preset.Mode == mode);
    }

    public static SignalConfiguration ApplyPreset(SignalMode mode)
    {
        return GetPreset(mode).Configuration;
    }

    public static SignalConfiguration ApplyManualChannelTextEdit(
        SignalConfiguration configuration,
        string channel0Label,
        string channel0Unit,
        string channel1Label,
        string channel1Unit)
    {
        return configuration with
        {
            Mode = SignalMode.Custom,
            Channel0 = new ChannelConfiguration(NormalizeText(channel0Label), NormalizeText(channel0Unit)),
            Channel1 = new ChannelConfiguration(NormalizeText(channel1Label), NormalizeText(channel1Unit))
        };
    }

    public static SignalConfiguration ApplyAdcSettings(
        SignalConfiguration configuration,
        int adcBits,
        double referenceVoltage)
    {
        return configuration with
        {
            AdcBits = Math.Clamp(adcBits, 1, 32),
            ReferenceVoltage = referenceVoltage > 0 ? referenceVoltage : configuration.ReferenceVoltage
        };
    }

    public static SignalConfiguration ApplyDisplayMode(
        SignalConfiguration configuration,
        SignalDisplayMode displayMode)
    {
        return configuration with { DisplayMode = displayMode };
    }

    public static double ConvertRawToDisplayValue(double rawValue, SignalConfiguration configuration)
    {
        if (configuration.DisplayMode == SignalDisplayMode.RawAdcCounts)
        {
            return rawValue;
        }

        double maxRawValue = Math.Pow(2, configuration.AdcBits) - 1;
        return rawValue * configuration.ReferenceVoltage / maxRawValue;
    }

    public static string GetModeDisplayName(SignalMode mode)
    {
        return GetPreset(mode).DisplayName;
    }

    public static string GetDisplayModeText(SignalDisplayMode displayMode)
    {
        return displayMode switch
        {
            SignalDisplayMode.RawAdcCounts => "Raw ADC counts",
            SignalDisplayMode.Voltage => "Voltage",
            _ => displayMode.ToString()
        };
    }

    public static string GetDisplayUnit(SignalConfiguration configuration, int channelIndex)
    {
        if (configuration.DisplayMode == SignalDisplayMode.Voltage)
        {
            return "V";
        }

        return channelIndex == 0 ? configuration.Channel0.Unit : configuration.Channel1.Unit;
    }

    public static string FormatReferenceVoltage(double referenceVoltage)
    {
        return referenceVoltage.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static SignalModePreset CreatePreset(
        SignalMode mode,
        string displayName,
        string channel0Label,
        string channel1Label,
        string channel0Unit,
        string channel1Unit,
        double plotWindowSeconds)
    {
        SignalConfiguration configuration = new(
            mode,
            new ChannelConfiguration(channel0Label, channel0Unit),
            new ChannelConfiguration(channel1Label, channel1Unit),
            DefaultAdcBits,
            DefaultReferenceVoltage,
            SignalDisplayMode.RawAdcCounts,
            plotWindowSeconds);

        return new SignalModePreset(mode, displayName, configuration);
    }

    private static string NormalizeText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Untitled" : value.Trim();
    }
}
