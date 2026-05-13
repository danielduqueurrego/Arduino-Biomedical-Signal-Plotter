// AnalogCsvStreamer
// Target board: Arduino UNO R4 WiFi
//
// Streams raw ADC readings from a contiguous set of analog pins starting at A0.
// Runtime commands configure channel count, ADC resolution, sample rate, and
// streaming state without recompiling firmware.
//
// Numeric sample rows stay CSV-only:
//   512
//   512,310
//   512,310,203,102,850,914
//
// Commands and responses are comment/metadata lines beginning with '#':
//   #SET CHANNEL_COUNT 6
//   #SET ADC_BITS 14
//   #SET SAMPLE_RATE_HZ 250
//   #STOP
//   #START
//   #STATUS

#include <ctype.h>
#include <stdlib.h>
#include <string.h>

const unsigned long SerialBaudRate = 115200;

const byte MinimumChannelCount = 1;
const byte MaximumChannelCount = 6;
const byte DefaultChannelCount = 2;

const byte MinimumAdcBits = 8;
const byte MaximumAdcBits = 14;
const byte DefaultAdcBits = 14;

const unsigned long MinimumSampleRateHz = 1;
const unsigned long MaximumSampleRateHz = 1000;
const unsigned long DefaultSampleRateHz = 250;

const byte AnalogPins[] = {A0, A1, A2, A3, A4, A5};
const byte CommandBufferSize = 80;

byte channelCount = DefaultChannelCount;
byte adcBits = DefaultAdcBits;
unsigned long sampleRateHz = DefaultSampleRateHz;
unsigned long sampleIntervalMicros = 1000000UL / DefaultSampleRateHz;
unsigned long nextSampleMicros = 0;
bool streamingEnabled = true;

char commandBuffer[CommandBufferSize];
byte commandLength = 0;
bool commandOverflowed = false;

void setup() {
  Serial.begin(SerialBaudRate);
  analogReadResolution(adcBits);
  RescheduleSampling();
}

void loop() {
  ReadSerialCommands();

  if (streamingEnabled) {
    StreamSampleIfDue();
  }
}

void StreamSampleIfDue() {
  unsigned long now = micros();

  if ((long)(now - nextSampleMicros) < 0) {
    return;
  }

  nextSampleMicros += sampleIntervalMicros;

  for (byte channelIndex = 0; channelIndex < channelCount; channelIndex++) {
    if (channelIndex > 0) {
      Serial.print(',');
    }

    Serial.print(analogRead(AnalogPins[channelIndex]));
  }

  Serial.println();

  // If serial printing ever takes longer than one interval, resynchronize
  // instead of trying to emit a burst of stale samples.
  if ((long)(micros() - nextSampleMicros) >= 0) {
    RescheduleSampling();
  }
}

void ReadSerialCommands() {
  while (Serial.available() > 0) {
    char incoming = (char)Serial.read();

    if (incoming == '\r') {
      continue;
    }

    if (incoming == '\n') {
      if (!commandOverflowed) {
        commandBuffer[commandLength] = '\0';
        HandleCommand(commandBuffer);
      }

      commandLength = 0;
      commandOverflowed = false;
      continue;
    }

    if (commandOverflowed) {
      continue;
    }

    if (commandLength >= CommandBufferSize - 1) {
      commandLength = 0;
      commandOverflowed = true;
      PrintError("COMMAND_TOO_LONG");
      continue;
    }

    commandBuffer[commandLength++] = incoming;
  }
}

void HandleCommand(char* rawLine) {
  char* command = Trim(rawLine);

  if (command[0] == '\0') {
    return;
  }

  if (command[0] != '#') {
    PrintError("EXPECTED_HASH");
    return;
  }

  if (strcmp(command, "#START") == 0) {
    streamingEnabled = true;
    RescheduleSampling();
    Serial.println("#OK START");
    return;
  }

  if (strcmp(command, "#STOP") == 0) {
    streamingEnabled = false;
    Serial.println("#OK STOP");
    return;
  }

  if (strcmp(command, "#STATUS") == 0) {
    PrintStatus();
    return;
  }

  if (strncmp(command, "#SET ", 5) == 0) {
    HandleSetCommand(command + 5);
    return;
  }

  PrintError("UNKNOWN_COMMAND");
}

void HandleSetCommand(char* body) {
  char* settingName = Trim(body);
  char* valueText = strchr(settingName, ' ');

  if (valueText == NULL) {
    PrintError("MISSING_VALUE");
    return;
  }

  *valueText = '\0';
  valueText = Trim(valueText + 1);

  long value = 0;
  if (!TryParseLong(valueText, &value)) {
    PrintError("BAD_VALUE");
    return;
  }

  if (strcmp(settingName, "CHANNEL_COUNT") == 0) {
    if (value < MinimumChannelCount || value > MaximumChannelCount) {
      PrintError("CHANNEL_COUNT_RANGE");
      return;
    }

    channelCount = (byte)value;
    Serial.print("#OK CHANNEL_COUNT ");
    Serial.println(channelCount);
    return;
  }

  if (strcmp(settingName, "ADC_BITS") == 0) {
    if (value < MinimumAdcBits || value > MaximumAdcBits) {
      PrintError("ADC_BITS_RANGE");
      return;
    }

    adcBits = (byte)value;
    analogReadResolution(adcBits);
    Serial.print("#OK ADC_BITS ");
    Serial.println(adcBits);
    return;
  }

  if (strcmp(settingName, "SAMPLE_RATE_HZ") == 0) {
    if (value < (long)MinimumSampleRateHz || value > (long)MaximumSampleRateHz) {
      PrintError("SAMPLE_RATE_RANGE");
      return;
    }

    sampleRateHz = (unsigned long)value;
    sampleIntervalMicros = 1000000UL / sampleRateHz;
    RescheduleSampling();
    Serial.print("#OK SAMPLE_RATE_HZ ");
    Serial.println(sampleRateHz);
    return;
  }

  PrintError("UNKNOWN_SETTING");
}

bool TryParseLong(char* text, long* value) {
  char* trimmed = Trim(text);

  if (trimmed[0] == '\0') {
    return false;
  }

  char* endPointer = NULL;
  long parsed = strtol(trimmed, &endPointer, 10);
  endPointer = Trim(endPointer);

  if (endPointer[0] != '\0') {
    return false;
  }

  *value = parsed;
  return true;
}

char* Trim(char* text) {
  while (isspace((unsigned char)*text)) {
    text++;
  }

  if (*text == '\0') {
    return text;
  }

  char* end = text + strlen(text) - 1;
  while (end > text && isspace((unsigned char)*end)) {
    *end = '\0';
    end--;
  }

  return text;
}

void RescheduleSampling() {
  nextSampleMicros = micros() + sampleIntervalMicros;
}

void PrintStatus() {
  Serial.print("#STATUS CHANNEL_COUNT=");
  Serial.print(channelCount);
  Serial.print(" ADC_BITS=");
  Serial.print(adcBits);
  Serial.print(" SAMPLE_RATE_HZ=");
  Serial.print(sampleRateHz);
  Serial.print(" STREAMING=");
  Serial.println(streamingEnabled ? 1 : 0);
}

void PrintError(const char* reason) {
  Serial.print("#ERR ");
  Serial.println(reason);
}
