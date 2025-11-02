#include <ESP32Encoder.h>

// 2 controllers can be differentiated by changing this ID
#define CONTROLLER_ID 1   

#define ENCODER_CLK 32
#define ENCODER_DT  33
#define ENCODER_SW  25

ESP32Encoder encoder;

long oldEncoderCount = 0;
bool oldButtonState = true;

void setup() {
  // Start serial communication
  Serial.begin(115200);
  encoder.attachFullQuad(ENCODER_CLK, ENCODER_DT);
  pinMode(ENCODER_SW, INPUT_PULLUP);
  encoder.clearCount();
}

void loop() {
  // Read current states
  long newEncoderCount = encoder.getCount();
  bool newButtonState = digitalRead(ENCODER_SW);

  if (newEncoderCount != oldEncoderCount || newButtonState != oldButtonState) {
    
    // Format: CONTROLLER_ID,ENCODER_COUNT,BUTTON_STATE
    String dataString = String(CONTROLLER_ID) + "," + 
                        String(newEncoderCount) + "," + 
                        String(newButtonState == LOW ? 1 : 0);
    
    Serial.println(dataString);

    // Update old states
    oldEncoderCount = newEncoderCount;
    oldButtonState = newButtonState;
  }
  
  delay(10); 
}