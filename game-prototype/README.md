## Web-playable Demo
https://ddanakim0304.itch.io/when-we-found-us

## Project Structure

### Assets/Scripts
This folder contains all the C# code.

#### Core Systems
*   `MainGameFlowManager.cs`: Manages the overall game progression and scene loading.
*   `MiniGameManager.cs`: A base class that all mini-game managers inherit from, providing a shared `WinGame()` function to advance the story.
*   `HardwareManager.cs`: Manages the connection to physical ESP32 controllers via serial ports.
*   `ControllerInput.cs`: Reads data from a single hardware controller (or keyboard) and makes the input available to other scripts.

#### Mini-Game Managers
Contains specific logic for each level (e.g., `BumpingGameManager`, `WavelengthGameManager`, `CheeseCuttingManager`). Each script controls the unique mechanics and rules for that specific mini-game or comic sequence.

#### Game Components
*   `PlayerMover.cs`: Handles horizontal character movement using keyboard input.
*   `PlayerWaveController.cs`: Controls a player's wave, adjusting frequency based on input and rendering it with shaders.
*   `CollisionDetection.cs`: A component that detects collisions with other objects and triggers game events.
*   `BumpReaction.cs`: Controls visual reactions to bumping, such as sprite swaps and animations.
*   `GeneralComicManager.cs`: Handles the interactive comic book cutscenes and panel transitions.

### Other Asset Folders
*   **`Assets/Scenes/`**: Game levels organized by story chapters (`Chap 1` through `Chap 5`).
*   **`Assets/`** (Root): Contains shared Materials and Shader Graphs (e.g., `WaveShader`, `DoodleEffect`) for the visual style.
*   **`ESP32/`**: Contains the Arduino (`.ino`) sketch for the ESP32 hardware controllers.