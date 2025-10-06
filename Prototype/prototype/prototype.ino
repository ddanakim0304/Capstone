#include <Servo.h> // Include the Servo library

// --- Configuration ---
const int potPin = A0;      // Pin for the potentiometer
const int servoPin = 9;     // Pin for the servo (must be PWM ~)

const int unlockedAngle = 90; // The angle when the door is "open" (90 degrees)
const int lockedAngle = 0;    // The angle when the door is "closed" (0 degrees)

const int lowerBound = 500; // The lower boundary of the "sweet spot"
const int upperBound = 600; // The upper boundary of the "sweet spot"
// --- End of Configuration ---

Servo myServo; // Create a servo object

bool isUnlocked = false; // A state variable to track if the door is open or not

void setup() {
  // Start the Serial Monitor to see the potentiometer values (for debugging)
  Serial.begin(9600); 

  // Attach the servo to its pin
  myServo.attach(servoPin);

  // Start with the door closed
  myServo.write(lockedAngle);
  Serial.println("Safe is locked. Turn the dial to find the sweet spot.");
}

void loop() {
  // 1. Read the value from the potentiometer (will be 0-1023)
  int potValue = analogRead(potPin);

  // 2. Print the value to the Serial Monitor so you can see it
  Serial.print("Dial Value: ");
  Serial.print(potValue);

  // 3. Check if the value is inside our target range
  if (potValue >= lowerBound && potValue <= upperBound) {
    // We are in the sweet spot!
    
    // Check if the door is currently locked. If so, open it.
    if (!isUnlocked) {
      Serial.println(" --> Sweet spot found! Opening the door.");
      myServo.write(unlockedAngle);
      isUnlocked = true; // Update the state to "unlocked"
    } else {
      Serial.println(" --> Door is already open.");
    }

  } else {
    // We are NOT in the sweet spot.
    
    // Check if the door is currently unlocked. If so, close it.
    if (isUnlocked) {
      Serial.println(" --> Dial moved! Closing the door.");
      myServo.write(lockedAngle);
      isUnlocked = false; // Update the state to "locked"
    } else {
      // If the door is already locked, we do nothing.
      Serial.println(""); // Print an empty line for cleaner output
    }
  }

  // A small delay to make the Serial Monitor easier to read
  delay(100); 
}