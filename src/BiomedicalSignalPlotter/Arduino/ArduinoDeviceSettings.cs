namespace BiomedicalSignalPlotter.Arduino;

public sealed class ArduinoDeviceSettings
{
    public ArduinoDeviceSettings(int channelCount, int adcBits, int sampleRateHz)
    {
        ChannelCount = ArduinoDeviceSettingsLimits.ValidateChannelCount(channelCount);
        AdcBits = ArduinoDeviceSettingsLimits.ValidateAdcBits(adcBits);
        SampleRateHz = ArduinoDeviceSettingsLimits.ValidateSampleRateHz(sampleRateHz);
    }

    public int ChannelCount { get; }

    public int AdcBits { get; }

    public int SampleRateHz { get; }
}
