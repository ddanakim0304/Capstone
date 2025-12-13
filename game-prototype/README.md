## Web-playable Demo
https://ddanakim0304.itch.io/when-we-found-us

## Project Structure

### C# Scripts (`Assets/Scripts/`)
All game logic is contained within this folder.

#### Core Systems
*   `MainGameFlowManager.cs`: Manages the overall game progression and scene loading.
*   `MiniGameManager.cs`: A base class that all mini-game managers inherit from, providing a shared `WinGame()` function.
*   `HardwareManager.cs`: Manages the connection to physical ESP32 controllers via serial ports.
*   `ControllerInput.cs`: Reads data from a single hardware controller (or keyboard) and makes the input available to other scripts.

#### Mini-Game Managers
Specific logic for each level (e.g., `BumpingGameManager.cs`, `WavelengthGameManager.cs`, `CheeseCuttingManager.cs`). Each script controls the unique mechanics and rules for that specific mini-game or comic sequence.

#### Game Components
*   `PlayerMover.cs`: Handles horizontal character movement using keyboard input.
*   `PlayerWaveController.cs`: Controls a player's wave, adjusting frequency based on input and rendering it with shaders.
*   `CollisionDetection.cs`: A component that detects collisions with other objects and triggers game events.
*   `BumpReaction.cs`: Controls visual reactions to bumping, such as sprite swaps and animations.
*   `GeneralComicManager.cs`: Handles the interactive comic book cutscenes and panel transitions.

### Game Levels (`Assets/Scenes/`)
*   **`Chap 1/`** - **`Chap 5/`**: Scenes organized by story chapters (e.g., `IntroComics.unity`, `WavelengthComics.unity`).
*   **`Others/`**: Utility scenes like `OpeningScene.unity` and `Credit.unity`.

### Graphics & Shaders (`Assets/`)
*   **`*.mat`**: Shared Materials (e.g., `P1WaveMaterial`, `Pencil_Material`) defining the visual style.
*   **`WaveShader.shader`**: The custom shader used to render the frequency matching mechanic.
*   **`Shader Graphs_*.mat`**: Materials using Shader Graphs for the "doodle" visual effects.

### Hardware Code (`ESP32/`)
*   `ESP32.ino`: The Arduino sketch for the custom ESP32 hardware controllers.