#include <Servo.h>

Servo lockServo;
int potPin = A0;

int unlockMin = 500;  // Lower bound of safe-crack range
int unlockMax = 600;  // Upper bound
int lockedPos = 0;    // Servo angle when locked
int unlockedPos = 90; // Servo angle when unlocked

void setup() {
  lockServo.attach(9);
  lockServo.write(lockedPos); 
  Serial.begin(9600);
}

void loop() {
  int potValue = analogRead(potPin);

  Serial.print("Pot Value: ");
  Serial.println(potValue);

  // Check if potentiometer is within the safecracking correct range
  if (potValue >= unlockMin && potValue <= unlockMax) {
    lockServo.write(unlockedPos);   // Unlock by rotating servo 90°
  } else {
    lockServo.write(lockedPos);     // Return to locked position
  }

  delay(20);
}
