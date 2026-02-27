#include <WiFi.h>
#include <WiFiUdp.h>
#include <ESP32Encoder.h>

#define PLAYER_ID  1

const char* SSID     = "KT_GiGA_2G_Wave2_CE0C";
const char* PASSWORD = "5af5hcf861";
const char* PC_IP    = "172.30.1.3";

#if PLAYER_ID == 0
  const int UDP_PORT = 5000;
#else
  const int UDP_PORT = 5001;
#endif

#define ENCODER_CLK 32
#define ENCODER_DT  33
#define ENCODER_SW  25

WiFiUDP udp;
ESP32Encoder encoder;

long oldEncoderCount = 0;
bool oldButtonState  = true;

void setup() {
  Serial.begin(115200);

  encoder.attachFullQuad(ENCODER_CLK, ENCODER_DT);
  pinMode(ENCODER_SW, INPUT_PULLUP);
  encoder.clearCount();

  // Connect to WiFi
  WiFi.begin(SSID, PASSWORD);
  Serial.print("Connecting to WiFi");
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println();
  Serial.println("WiFi connected. IP: " + WiFi.localIP().toString());
  Serial.printf("Sending UDP to %s:%d  (Player %d)\n", PC_IP, UDP_PORT, PLAYER_ID);
}

void loop() {
  long newEncoderCount = encoder.getCount();
  bool newButtonState  = digitalRead(ENCODER_SW);

  if (newEncoderCount != oldEncoderCount || newButtonState != oldButtonState) {
    // Format: "playerID,encoderCount,buttonState"  e.g. "0,5,1"
    String msg = String(PLAYER_ID) + "," +
                 String(newEncoderCount) + "," +
                 String(newButtonState == LOW ? 1 : 0);

    // Send UDP packet to Unity on the Mac
    udp.beginPacket(PC_IP, UDP_PORT);
    udp.print(msg);
    udp.endPacket();

    // Debug via USB Serial Monitor
    Serial.println(msg);

    oldEncoderCount = newEncoderCount;
    oldButtonState  = newButtonState;
  }

  delay(10);
}
