// TwoChannelCsvStreamer
// Target board: Arduino UNO R4 WiFi
//
// Streams raw ADC readings from A0 and A1 as numeric CSV rows:
//   512,310
//   514,311
//
// The app's Version 0.1 parser expects numeric-only rows by default.
// If metadata is added later, prefix it with '#' so the app can ignore it.

const unsigned long SerialBaudRate = 115200;
const unsigned long SampleRateHz = 250;
const unsigned long SampleIntervalMicros = 1000000UL / SampleRateHz;

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

  int channel0 = analogRead(A0);
  int channel1 = analogRead(A1);

  Serial.print(channel0);
  Serial.print(',');
  Serial.println(channel1);

  // If serial printing ever takes longer than one interval, resynchronize
  // instead of trying to emit a burst of stale samples.
  if ((long)(micros() - nextSampleMicros) >= 0) {
    nextSampleMicros = micros() + SampleIntervalMicros;
  }
}
