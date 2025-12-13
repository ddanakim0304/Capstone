#include <ESP32Encoder.h>
#include "BluetoothSerial.h"

// ---------------- CONFIGURATION ---------------- //
#define PLAYER_ID 0
// ----------------------------------------------- //

#if PLAYER_ID == 0
  #define DEVICE_NAME "Controller_1" 
#else
  #define DEVICE_NAME "Controller_2"
#endif

BluetoothSerial SerialBT;

#define ENCODER_CLK 32
#define ENCODER_DT  33
#define ENCODER_SW  25

ESP32Encoder encoder;

long oldEncoderCount = 0;
bool oldButtonState = true;

void setup() {
  Serial.begin(115200);
  
  // Start Bluetooth
  bool success = SerialBT.begin(DEVICE_NAME);
  
  if(success) {
    Serial.println("Bluetooth Started! Name: " + String(DEVICE_NAME));
  } else {
    Serial.println("Bluetooth FAILED to start!");
    // If this prints, your board might be low on power
  }

  encoder.attachFullQuad(ENCODER_CLK, ENCODER_DT);
  pinMode(ENCODER_SW, INPUT_PULLUP);
  encoder.clearCount();
}

void loop() {
  long newEncoderCount = encoder.getCount();
  bool newButtonState = digitalRead(ENCODER_SW);

  if (newEncoderCount != oldEncoderCount || newButtonState != oldButtonState) {
    
    // Format: 0,5,1
    String dataString = String(PLAYER_ID) + "," + 
                        String(newEncoderCount) + "," + 
                        String(newButtonState == LOW ? 1 : 0);
    
    // Send via Bluetooth
    if (SerialBT.hasClient()) {
      SerialBT.println(dataString);
    }
    // Debug via USB
    Serial.println(dataString);

    oldEncoderCount = newEncoderCount;
    oldButtonState = newButtonState;
  }
  
  delay(10); 
}