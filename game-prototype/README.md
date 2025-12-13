## Web-playable Demo
https://ddanakim0304.itch.io/when-we-found-us

## Project Structure

### C# Scripts (`Assets/Scripts/`)
All game logic is contained within this folder.

#### Core Systems (`Core`)
*   `Game Manager/MainGameFlowManager.cs`: Manages the overall game progression and scene loading.
*   `Game Manager/MiniGameManager.cs`: A base class that all mini-game managers inherit from, providing a shared `WinGame()` function.
*   `Hardware/HardwareManager.cs`: Manages the connection to physical ESP32 controllers via serial ports.
*   `Hardware/ControllerInput.cs`: Reads data from a single hardware controller (or keyboard) and makes the input available to other scripts.

#### Mini-Game Managers
Specific logic for each level (e.g., `BumpingGameManager.cs`, `WavelengthGameManager.cs`, `CheeseCuttingManager.cs`). Each script controls the unique mechanics and rules for that specific mini-game or comic sequence.

#### General Game Components (`Core/General`)
Contains shared scripts attached to GameObjects across various scenes. This includes player movement logic (`PlayerMover.cs`), collision handling (`CollisionDetection.cs`), etc.

### Game Levels (`Assets/Scenes/`)
*   **`Chap 1/`** - **`Chap 5/`**: Scenes organized by story chapters
*   **`Others/`**: Utility scenes

### Other Asset Folders
*   **`Assets/Sprites/`**: Art assets
*   **`Assets/Audio/`**: Sound assets
*   **`ESP32/`**: Contains the Arduino (`.ino`) sketch for the ESP32 hardware controllers.