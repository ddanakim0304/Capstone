#include <ESP32Encoder.h>

// --- IMPORTANT: SET A UNIQUE ID FOR EACH CONTROLLER ---
#define CONTROLLER_ID 1   // For your second ESP32, change this to 2

#define ENCODER_CLK 32
#define ENCODER_DT  33
#define ENCODER_SW  25

ESP32Encoder encoder;

long oldEncoderCount = 0;
bool oldButtonState = true; // true = not pressed

void setup() {
  Serial.begin(115200);
  encoder.attachFullQuad(ENCODER_CLK, ENCODER_DT);
  pinMode(ENCODER_SW, INPUT_PULLUP);
  encoder.clearCount();
}

void loop() {
  long newEncoderCount = encoder.getCount();
  bool newButtonState = digitalRead(ENCODER_SW);

  if (newEncoderCount != oldEncoderCount || newButtonState != oldButtonState) {
    
    // New data format: "ID,count,button_state"
    String dataString = String(CONTROLLER_ID) + "," + 
                        String(newEncoderCount) + "," + 
                        String(newButtonState == LOW ? 1 : 0);
    
    Serial.println(dataString);

    oldEncoderCount = newEncoderCount;
    oldButtonState = newButtonState;
  }
  
  delay(10); 
}