# Number Merger

A vertical number-merging puzzle prototype built with Unity. Drop `2` and `4` tiles into a 5-column board, merge equal neighbours, and watch the chain reactions cascade.

## Gameplay

- The board is 5 columns wide and 7 visible rows tall.
- Each turn you press and drag across the board to pick a column, then release to drop the current tile into the lowest free cell of that column.
- Tiles are `2` (90%) or `4` (10%). The next tile is always shown in the corner.
- Orthogonally adjacent tiles with the same value merge (2 to 5 participants). The merged result is `value × 2^(participants − 1)`, capped at `8192`.
- Merges can chain: after each merge, gravity is applied and the board is checked again.
- Combos: every merge after the first one in the same move is a combo. Three or more merges in one move show a `NNx` counter in the center of the screen, and each merge in the chain plays its sound a little higher than the previous one.
- Score is the sum of every merge result in a move. The record is kept between sessions.
- The game ends when a column holds more than 7 tiles.

## Requirements

- Unity `6000.3.10f1`
- Universal Render Pipeline (2D)
- Input System package (`Pointer.current` is used for input)
- LeanTween (bundled under `Assets/LeanTween`)
- TextMeshPro

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/michelxrf/Number-Merger.git
   ```
2. Open the project folder in Unity Hub using Unity `6000.3.10f1`.
3. Open `Assets/!/Scenes/Gameplay.unity` and press Play.

The `Gameplay` scene is the only scene in the build settings. `MainMenu` is a placeholder and is not yet connected to the game flow.

## Controls

| Input | Action |
|---|---|
| Press and drag (mouse or touch) | Choose a column |
| Release | Drop the tile |
| F12 | Toggle the debug overlay (restart, clear saved data and restart) |

## Saving

Progress is stored with `PlayerPrefs`:

- **Record:** saved whenever a move ends.
- **Board in progress:** saved after each successful move and removed on game over.

Because `PlayerPrefs` is used, saving works on Windows, Android and WebGL.

## Project Structure

```text
Assets/
  !/                      Game code and assets
    Scripts/
      Gameplay/           Pure game logic (board, merges, cascades, piece queue)
      Managers/           ScoreService
      Save/               SaveSystem and BoardState
      UI/                 Views, gameplay screen, game over and debug overlays
    Prefabs/              Tile prefab and UI prefabs
    Scenes/               Gameplay and MainMenu scenes
    Sprites/, Audio/, Fonts/, Themes/, Icon/
  LeanTween/              Third-party tween library
ProjectSettings/          Unity project settings
Packages/                 Package manifest
```

### Architecture

Game logic and presentation are kept separate:

- **Logic** (`BoardModel`, `BoardController`, `CascadeResolver`, `MergeResolver`, `MergeMath`, `ScoreService`, `PieceGenerator`) has no `MonoBehaviour` dependency and is driven by plain C# objects.
- **Presentation** (`GameplayScreen`, `BoardView`, `PieceQueueView`, `TileView`) reads the results from the logic layer and animates them with LeanTween and coroutines.
- `GameplayScreen` is the composition root for the scene.

For a detailed description of the rules, the move flow and the layout, see [DOCUMENTATION.md](DOCUMENTATION.md) (in Portuguese).

## Status

Working:

- Tile placement, merging, cascades and game over
- Scoring and persisted record
- Save and resume of the board in progress
- Combo counter, combo result screen and chain sound pitch
- Restart from the game over screen and the debug overlay

Not yet done:

- Main menu and menu-to-gameplay transition
- Game over sound
- More in-game challenges
- Google Ads