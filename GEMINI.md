# Potan Minecraft Animation Viewer

Potan Minecraft Animation Viewer is a Unity-based tool designed to load and play Minecraft display animations from BDEngine files.

## Project Overview

*   **Core Technology:** Unity Engine (C#).
*   **Main Functionality:** Loading, playing, and managing Minecraft display entity animations.
*   **Architecture:** Centered around a singleton `GameManager` that manages various `BaseManager` components (e.g., `BdObjectManager`, `FileLoadManager`, `AnimManager`).
*   **Key Libraries:**
    *   **UniTask:** For high-performance asynchronous operations.
    *   **DoTween:** For smooth animations and transitions.
    *   **StandaloneFileBrowser:** For native file open/save dialogs.
    *   **Newtonsoft.Json:** For parsing animation data.
    *   **PoiPoiTooltip:** For UI tooltips.
    *   **RuntimeTransformHandles:** For in-game object manipulation.

## Technical Details

### File Formats
*   **.bdengine / .bdstudio:** The primary input formats. They contain Base64 encoded, GZip compressed JSON data representing an array of `BdObjectData`.
*   **.mcdeanim:** A custom format used for saving and exporting animation project data.

### Manager System
All major systems are implemented as "Managers" that inherit from `BaseManager`. They automatically register themselves with the `GameManager` on `Awake`.
*   `BdObjectManager`: Manages the lifecycle of display objects, including creation, pooling, and destruction.
*   `FileLoadManager`: Handles importing files and sorting animation frames.
*   `AnimManager`: Controls animation playback, including play/pause, seek, and tick management.
*   `SettingManager`: Manages application settings and preferences.

### Animation System
*   Animations are played by `BDObjectAnimator`, which updates the state of `BdObjectContainer` entities based on frame data.
*   Supports interpolation between frames for smooth motion.

## Development Conventions

*   **Asynchronous Programming:** Use `UniTask` and `async/await` for all non-blocking operations (I/O, heavy computation, waiting). Avoid standard C# `Task` where possible to minimize allocations.
*   **Logging:** Use `CustomLog` for consistent logging across the application.
*   **Resource Management:** Objects are often pooled (e.g., `BdObjectPool`) to avoid frequent GC spikes during complex animation loading.
*   **UI:** Built using Unity's UI system (likely UGUI or UI Toolkit based on assets).

## Building and Running

*   **Unity Version:** Check `ProjectSettings/ProjectVersion.txt` for the required Unity version (likely 2022.3.x or 2023.x).
*   **Build Targets:** Primarily Windows (due to `StandaloneFileBrowser` and `win32` environment), but potentially multi-platform.
*   **Running:** Open the project in Unity and play the `Animation.unity` scene.

## Key Files & Directories

*   `Assets/Scripts/`: Contains all source code.
    *   `BDObjectSystem/`: Logic for display entities and their management.
    *   `Animation/`: Animation playback and frame management logic.
    *   `FileSystem/`: File I/O, parsing, and export logic.
    *   `GameSystem/`: Core infrastructure, managers, and settings.
*   `Assets/Prefabs/`: Prefabs for UI elements and display objects.
*   `Assets/Resources/`: Dynamic assets loaded at runtime.
