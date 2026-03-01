#include <WiFi.h>
#include <WiFiUdp.h>
#include <ESPmDNS.h>
#include <ESP32Encoder.h>
#include "arduino_secrets.h"

#define PLAYER_ID  1

const char* SSID     = SECRET_SSID;
const char* PASSWORD = SECRET_PASSWORD;
const char* PC_IP    = SECRET_PC_IP;

const int UDP_PORT = 5001;

#define ENCODER_CLK 32
#define ENCODER_DT  33
#define ENCODER_SW  25

WiFiUDP udp;
ESP32Encoder encoder;

IPAddress pcAddr;

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

  // Resolve mDNS hostname to IP
  if (!WiFi.hostByName(PC_IP, pcAddr)) {
    Serial.println("ERROR: Failed to resolve hostname: " + String(PC_IP));
  } else {
    Serial.println("Resolved " + String(PC_IP) + " -> " + pcAddr.toString());
  }

  Serial.printf("Sending UDP to %s:%d  (Player %d)\n", pcAddr.toString().c_str(), UDP_PORT, PLAYER_ID);
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
    udp.beginPacket(pcAddr, UDP_PORT);
    udp.print(msg);
    udp.endPacket();

    // Debug via USB Serial Monitor
    Serial.println(msg);

    oldEncoderCount = newEncoderCount;
    oldButtonState  = newButtonState;
  }

  delay(10);
}
