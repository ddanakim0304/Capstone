const uint8_t PIN_L = A0;
const uint8_t PIN_R = A1;

int readStable(uint8_t pin) {
  analogRead(pin);
  delayMicroseconds(100);
  int a = analogRead(pin);
  int b = analogRead(pin);
  int c = analogRead(pin);
  return (a + b + c) / 3;
}

void setup() {
  Serial.begin(115200);
}

void loop() {
  static uint32_t last = 0;
  uint32_t now = micros();
  if (now - last < 3333) return;
  last = now;

  int v0 = readStable(PIN_L);
  int v1 = readStable(PIN_R);

  // Strict format: "number,number\n" — nothing else
  Serial.print(v0); Serial.print(','); Serial.println(v1);
}