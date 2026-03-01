#include <WiFi.h>
#include <WiFiUdp.h>
#include <ESPmDNS.h>
#include <ESP32Encoder.h>
#include <ESP32Servo.h>
#include "arduino_secrets.h"

#define PLAYER_ID  1

const char* SSID     = SECRET_SSID;
const char* PASSWORD = SECRET_PASSWORD;
const char* PC_IP    = SECRET_PC_IP;

// Port Unity listens on for P1 encoder data
const int UDP_SEND_PORT = 5001;

// Port this ESP32 listens on for commands from Unity for servo trigger
const int CMD_RECV_PORT = 5002;

#define ENCODER_CLK 32
#define ENCODER_DT  33
#define ENCODER_SW  25
#define SERVO_PIN   13

WiFiUDP udpSend;
WiFiUDP udpRecv;
ESP32Encoder encoder;
Servo myServo;

IPAddress pcAddr;

long oldEncoderCount = 0;
bool oldButtonState  = true;

void setup() {
  Serial.begin(115200);

  // Rotary encoder
  encoder.attachFullQuad(ENCODER_CLK, ENCODER_DT);
  pinMode(ENCODER_SW, INPUT_PULLUP);
  encoder.clearCount();

  // Servo — start at 90 degrees
  myServo.attach(SERVO_PIN);
  myServo.write(90);

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

  Serial.printf("Sending encoder UDP to %s:%d  (Player %d)\n", pcAddr.toString().c_str(), UDP_SEND_PORT, PLAYER_ID);
  Serial.printf("Listening for commands on port %d\n", CMD_RECV_PORT);

  // Open receive socket for Unity → ESP32 commands
  udpRecv.begin(CMD_RECV_PORT);
}

void loop() {
  // ── Check for commands from Unity ────────────────────────────────────────
  int packetSize = udpRecv.parsePacket();
  if (packetSize > 0) {
    char buf[32];
    int len = udpRecv.read(buf, sizeof(buf) - 1);
    buf[len] = '\0';
    String cmd = String(buf);
    cmd.trim();
    Serial.println("Received command: " + cmd);

    if (cmd == "SERVO90") {
      myServo.write(0);
      Serial.println("Servo rotated to 0 degrees");
    }
  }

  // ── Send encoder + button state to Unity ─────────────────────────────────
  long newEncoderCount = encoder.getCount();
  bool newButtonState  = digitalRead(ENCODER_SW);

  if (newEncoderCount != oldEncoderCount || newButtonState != oldButtonState) {
    // Format: "playerID,encoderCount,buttonState"  e.g. "0,5,1"
    String msg = String(PLAYER_ID) + "," +
                 String(newEncoderCount) + "," +
                 String(newButtonState == LOW ? 1 : 0);

    udpSend.beginPacket(pcAddr, UDP_SEND_PORT);
    udpSend.print(msg);
    udpSend.endPacket();

    Serial.println(msg);

    oldEncoderCount = newEncoderCount;
    oldButtonState  = newButtonState;
  }

  delay(10);
}
