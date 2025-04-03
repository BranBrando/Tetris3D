# 3D Tetris Game for Unity

This project implements a 3D version of the classic Tetris game using Unity's engine. Players can control tetromino pieces in a 3D grid, aiming to clear layers by completely filling them with blocks.

## Game Features

- Full 3D gameplay with tetromino rotation on all three axes
- Score system with combo bonuses and level progression
- Three-dimensional line clearing (horizontal, vertical, and depth planes)
- Customizable grid size and difficulty settings
- Camera controls for orbiting and zooming around the play field
- Visual grid helpers and ghost piece preview

## Setup Instructions

1. **Scene Setup**:
   - Open the TetrisGame scene from `Assets/Scenes/TetrisGame.unity`
   - If starting from scratch, create a new scene and add an empty GameObject named "GameController"

2. **Component Setup**:
   - Add the following scripts to the GameController object:
     - `GameManager.cs`
     - `UIManager.cs`
     - `GridVisualizer.cs` (optional for grid visualization)
     - `LineRendererMaterialFixer.cs` (if using grid visualization)
   
   - The GameManager will automatically create:
     - GridSystem
     - PieceSpawner
     - ScoreManager

3. **Camera Setup**:
   - Add the `CameraController.cs` script to your Main Camera
   - Position the camera to have a good view of the game area
   - The camera will automatically target the grid system

4. **UI Setup**:
   - Create a Canvas with UI elements for Score, Level, and Lines
   - Add Text components for each and reference them in the UIManager
   - Add UI elements for game over and controls panels

## Controls

Default controls (can be customized in the InputController):

- **Movement**: 
  - A/D: Move left/right
  - W/S: Move forward/backward
  - Down Arrow: Move down

- **Rotation**:
  - J: Rotate around X-axis
  - K: Rotate around Y-axis
  - L: Rotate around Z-axis

- **Quick Fall**:
  - Space: Speed up falling

- **Camera**:
  - Right Mouse Button + Mouse Movement: Orbit camera
  - Mouse Wheel: Zoom in/out
  - F1: Toggle controls panel

## Code Organization

All game scripts are in the `TetrisGame` namespace and located in the `Assets/Scripts/Tetris` folder:

- **Core Game Logic**:
  - `GameManager.cs`: Central game controller
  - `GridSystem.cs`: Manages the 3D grid state
  - `PieceSpawner.cs`: Creates and spawns tetromino pieces
  - `Tetromino.cs`: Controls individual tetromino behavior
  - `ScoreManager.cs`: Handles scoring and level progression

- **Input & UI**:
  - `InputController.cs`: Processes player input
  - `UIManager.cs`: Manages game interface
  - `CameraController.cs`: Controls camera movement

- **Visualization**:
  - `GridVisualizer.cs`: Renders the game grid
  - `LineRendererMaterialFixer.cs`: Fixes line rendering issues
  - `GridVisualizationUtility.cs`: Helper functions for visualization

- **Editor Tools**:
  - `Editor/GridVisualizerEditor.cs`: Custom inspector for GridVisualizer

## Customization

The game has numerous customizable parameters in each component:

1. **Grid Size**: Adjust width, height, and depth in GameManager
2. **Difficulty**: Modify fall speed and level progression in GameManager
3. **Controls**: Change key bindings in InputController
4. **Visuals**: Customize grid appearance in GridVisualizer
5. **Scoring**: Adjust point values and combo settings in ScoreManager

## Troubleshooting

- **Grid not visible**: Check if LineRendererMaterialFixer is attached and a compatible shader is being used
- **Lines not clearing**: Ensure GridSystem is configured with correct dimensions
- **Performance issues**: Reduce grid size or simplify visualization
- **Unity version**: Designed for Unity 2021.3 LTS or newer
