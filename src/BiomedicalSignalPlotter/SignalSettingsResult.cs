using BiomedicalSignalPlotter.Configuration;

namespace BiomedicalSignalPlotter;

public sealed record SignalSettingsResult(SignalConfiguration Configuration, int SampleRateHz);
