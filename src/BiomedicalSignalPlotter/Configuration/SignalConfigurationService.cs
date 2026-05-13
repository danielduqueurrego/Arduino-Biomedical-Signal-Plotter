using System.Globalization;
using BiomedicalSignalPlotter.Models;

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
            AnalogChannelLimits.Default,
            "Channel 0",
            "Channel 1",
            "ADC counts",
            "ADC counts",
            DefaultPlotWindowSeconds),
        CreatePreset(
            SignalMode.GenericTwoChannel,
            "Generic two-channel",
            2,
            "Channel 0",
            "Channel 1",
            "ADC counts",
            "ADC counts",
            DefaultPlotWindowSeconds),
        CreatePreset(
            SignalMode.EmgForcePressure,
            "EMG + Force/Pressure",
            2,
            "EMG",
            "Force/Pressure",
            "ADC counts",
            "ADC counts",
            5.0),
        CreatePreset(
            SignalMode.Ecg,
            "ECG",
            1,
            "ECG",
            "Auxiliary",
            "ADC counts",
            "ADC counts",
            6.0),
        CreatePreset(
            SignalMode.PpgPulseOximetry,
            "PPG / Pulse Oximetry",
            1,
            "PPG",
            "Auxiliary PPG",
            "ADC counts",
            "ADC counts",
            10.0),
        CreatePreset(
            SignalMode.BloodPressure,
            "Blood Pressure",
            1,
            "Pressure",
            "Pulse/Reference",
            "ADC counts",
            "ADC counts",
            15.0)
    ];

    public static IReadOnlyList<SignalModePreset> AllPresets => Presets;

    public static SignalConfiguration CreateDefault()
    {
        return CopyConfiguration(GetPreset(SignalMode.GenericTwoChannel).Configuration);
    }

    public static SignalConfiguration CreateCustomDefault()
    {
        return CopyConfiguration(GetPreset(SignalMode.Custom).Configuration);
    }

    public static SignalModePreset GetPreset(SignalMode mode)
    {
        return Presets.Single(preset => preset.Mode == mode);
    }

    public static SignalConfiguration ApplyPreset(SignalMode mode)
    {
        return CopyConfiguration(GetPreset(mode).Configuration);
    }

    public static SignalConfiguration SwitchToCustomPreservingSettings(SignalConfiguration configuration)
    {
        return CopyConfiguration(configuration, SignalMode.Custom);
    }

    public static SignalConfiguration ApplyChannelCount(SignalConfiguration configuration, int channelCount)
    {
        AnalogChannelLimits.Validate(channelCount);

        return CopyConfiguration(configuration, SignalMode.Custom, channelCount);
    }

    public static SignalConfiguration ApplyManualChannelTextEdit(
        SignalConfiguration configuration,
        IReadOnlyList<string> channelLabels,
        IReadOnlyList<string> channelUnits)
    {
        ChannelConfiguration[] channels = CopyChannels(configuration.Channels);

        for (int i = 0; i < configuration.ChannelCount; i++)
        {
            string label = i < channelLabels.Count ? channelLabels[i] : channels[i].Label;
            string unit = i < channelUnits.Count ? channelUnits[i] : channels[i].Unit;
            channels[i] = new ChannelConfiguration(NormalizeText(label), NormalizeText(unit));
        }

        return configuration with
        {
            Mode = SignalMode.Custom,
            Channels = channels
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

        return configuration.Channels[channelIndex].Unit;
    }

    public static string FormatReferenceVoltage(double referenceVoltage)
    {
        return referenceVoltage.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static SignalModePreset CreatePreset(
        SignalMode mode,
        string displayName,
        int channelCount,
        string channel0Label,
        string channel1Label,
        string channel0Unit,
        string channel1Unit,
        double plotWindowSeconds)
    {
        SignalConfiguration configuration = new(
            mode,
            CreateChannels(channel0Label, channel1Label, channel0Unit, channel1Unit),
            AnalogChannelLimits.Validate(channelCount),
            DefaultAdcBits,
            DefaultReferenceVoltage,
            SignalDisplayMode.RawAdcCounts,
            plotWindowSeconds);

        return new SignalModePreset(mode, displayName, configuration);
    }

    private static ChannelConfiguration[] CreateChannels(
        string channel0Label,
        string channel1Label,
        string channel0Unit,
        string channel1Unit)
    {
        ChannelConfiguration[] channels = new ChannelConfiguration[AnalogChannelLimits.Maximum];
        channels[0] = new ChannelConfiguration(channel0Label, channel0Unit);
        channels[1] = new ChannelConfiguration(channel1Label, channel1Unit);

        for (int i = 2; i < channels.Length; i++)
        {
            channels[i] = new ChannelConfiguration($"Channel {i}", "ADC counts");
        }

        return channels;
    }

    private static SignalConfiguration CopyConfiguration(
        SignalConfiguration configuration,
        SignalMode? mode = null,
        int? channelCount = null)
    {
        return configuration with
        {
            Mode = mode ?? configuration.Mode,
            ChannelCount = channelCount ?? configuration.ChannelCount,
            Channels = CopyChannels(configuration.Channels)
        };
    }

    private static ChannelConfiguration[] CopyChannels(IReadOnlyList<ChannelConfiguration> channels)
    {
        ChannelConfiguration[] copy = new ChannelConfiguration[AnalogChannelLimits.Maximum];

        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = i < channels.Count
                ? channels[i]
                : new ChannelConfiguration($"Channel {i}", "ADC counts");
        }

        return copy;
    }

    private static string NormalizeText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Untitled" : value.Trim();
    }
}
