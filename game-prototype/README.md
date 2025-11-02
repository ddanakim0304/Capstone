## Web-playable Demo
https://ddanakim0304.itch.io/when-we-found-us

## Project Structure

### Assets/Scripts
This folder contains all the C# code

#### Core Systems
*   `MainGameFlowManager.cs`: Manages the overall game progression, loading the next scene when a mini-game is won.
*   `MiniGameManager.cs`: A base class that all mini-game managers inherit from, providing a shared `WinGame()` function to advance the story.
*   `HardwareManager.cs`: Manages the connection to physical ESP32 controllers via serial ports and creates virtual controllers for keyboard fallback.
*   `ControllerInput.cs`: Reads data from a single hardware controller (or keyboard) and makes the input (endoer value, button state) available to other scripts.

#### Mini-Game Managers
*   `BumpingGameManager.cs`: Manages the rules for the 'bumping' mini-game, waiting for a collision event to trigger the win condition.
*   `WavelengthGameManager.cs`: Manages the 'wavelength' mini-game, checking if the two players' wave frequencies are matched for a specific duration.
*   `CheeseCuttingManager.cs`: Manages the cheese-cutting timing mini-game, where a player must trigger a cut while the knife is in the 'good zone'.
*   `CheeseComicGameManager.cs`: Controls the animated comic book cutscene, playing a sequence of fades and animations before automatically winning.

#### Game Components
*   `PlayerMover.cs`: Handles horizontal character movement using keyboard input.
*   `PlayerWaveController.cs`: Controls a single player's wave, adjusting its frequency based on hardware/keyboard input and drawing it with a Line Renderer.
*   `CollisionDetection.cs`: A component attached to players that detects collisions with other objects and fires an event.
*   `BumpReaction.cs`: Controls the character's visual reaction to bumping, including swapping sprites and playing a bounce/shudder animation.
*   `startButton.cs`: A script for the main menu button to start the game by loading the first level

### Other Asset Folders
*   **`Assets/Scenes/`**: All game levels
*   **`Assets/Sprites/`**: Art assets
*   **`Assets/Audio/`**: Sound assets
*   **`ESP32/`**: Contains the Arduino (`.ino`) sketch for the ESP32 hardware controllers.