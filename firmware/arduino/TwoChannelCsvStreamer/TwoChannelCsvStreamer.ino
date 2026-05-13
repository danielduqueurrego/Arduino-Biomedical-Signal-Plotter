// AnalogCsvStreamer
// Target board: Arduino UNO R4 WiFi
//
// Streams raw ADC readings from a contiguous set of analog pins starting at A0.
// The default is two channels for compatibility with the Version 0.1 app:
//   512,310
//
// Change CHANNEL_COUNT to 1 through 6 to stream A0 through A5:
//   1 channel: 512
//   6 channels: 512,310,203,102,850,914
//
// The app parser expects numeric-only rows by default. If metadata is added
// later, prefix it with '#' so the app can ignore it.

// UNO R4 WiFi exposes six analog input pins, A0 through A5. Keep the value
// between 1 and 6. The app currently selects its expected channel count
// separately; dynamic device reconfiguration will be added later.
#define CHANNEL_COUNT 2

#if CHANNEL_COUNT < 1 || CHANNEL_COUNT > 6
#error "CHANNEL_COUNT must be between 1 and 6."
#endif

const unsigned long SerialBaudRate = 115200;
const unsigned long SampleRateHz = 250;
const unsigned long SampleIntervalMicros = 1000000UL / SampleRateHz;
const byte ChannelCount = CHANNEL_COUNT;
const byte AnalogPins[] = {A0, A1, A2, A3, A4, A5};

unsigned long nextSampleMicros = 0;

void setup() {
  Serial.begin(SerialBaudRate);

  // UNO R4 WiFi supports analogReadResolution(). Ten bits keeps values
  // compatible with classic Arduino UNO-style 0-1023 lab examples.
  analogReadResolution(10);

  nextSampleMicros = micros();
}

void loop() {
  unsigned long now = micros();

  if ((long)(now - nextSampleMicros) < 0) {
    return;
  }

  nextSampleMicros += SampleIntervalMicros;

  for (byte channelIndex = 0; channelIndex < ChannelCount; channelIndex++) {
    if (channelIndex > 0) {
      Serial.print(',');
    }

    Serial.print(analogRead(AnalogPins[channelIndex]));
  }

  Serial.println();

  // If serial printing ever takes longer than one interval, resynchronize
  // instead of trying to emit a burst of stale samples.
  if ((long)(micros() - nextSampleMicros) >= 0) {
    nextSampleMicros = micros() + SampleIntervalMicros;
  }
}
