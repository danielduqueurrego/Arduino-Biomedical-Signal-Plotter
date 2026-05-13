namespace BiomedicalSignalPlotter.Arduino;

public static class ArduinoCommandBuilder
{
    public const string StartCommand = "#START";
    public const string StopCommand = "#STOP";
    public const string StatusCommand = "#STATUS";

    public static string BuildSetChannelCount(int channelCount)
    {
        return $"#SET CHANNEL_COUNT {ArduinoDeviceSettingsLimits.ValidateChannelCount(channelCount)}";
    }

    public static string BuildSetAdcBits(int adcBits)
    {
        return $"#SET ADC_BITS {ArduinoDeviceSettingsLimits.ValidateAdcBits(adcBits)}";
    }

    public static string BuildSetSampleRateHz(int sampleRateHz)
    {
        return $"#SET SAMPLE_RATE_HZ {ArduinoDeviceSettingsLimits.ValidateSampleRateHz(sampleRateHz)}";
    }

    public static IReadOnlyList<string> BuildApplySettingsSequence(ArduinoDeviceSettings settings)
    {
        return
        [
            StopCommand,
            BuildSetChannelCount(settings.ChannelCount),
            BuildSetAdcBits(settings.AdcBits),
            BuildSetSampleRateHz(settings.SampleRateHz),
            StatusCommand,
            StartCommand
        ];
    }
}
