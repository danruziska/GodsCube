# Cube of the Gods

A turn-based roguelite where every battle is fought on a 3D cube of elemental pieces. Pick a team of three heroes, climb six floors, and build your run around combos that start rare and grow as you get stronger.

A Windows game made with Unity 6, with a pixel art UI. It plays with the mouse in a tall 9:16 window. The internal codename is **GodsCube** (and `Cubo` in the code, since it started as a Portuguese project).

<!--
  Screenshot / GIF goes here, e.g.:
  <p align="center"><img src="Docs/screenshot.png" width="320" alt="A battle on the cube"></p>
-->

> **Status:** early prototype. It is playable from start to finish, but the balance, art and content are still changing a lot. Expect rough edges.

## Table of contents

- [How it plays](#how-it-plays)
- [Features](#features)
- [Getting started](#getting-started)
- [Building for Windows](#building-for-windows)
- [Running the tests](#running-the-tests)
- [Project layout](#project-layout)
- [Game data](#game-data)
- [Contributing](#contributing)
- [Third-party assets](#third-party-assets)
- [License](#license)

## How it plays

You and a rival share one cube. Each side has an altar: yours is the golden face at the front, theirs is the red face at the back.

1. **Fire a group.** Click a patch of two or more connected pieces of the same color on your face. They break, the pieces above fall into place and new ones come in. If falling pieces line up three or more of a color, they fire too, and the chain keeps going.
2. **Use your powers.** Broken pieces fill your heroes' mana. Each hero has two powers tied to two of the six elements: damage, healing, critical hits, purging colors from the board and so on.
3. **Hand over a face.** At the end of your turn you pick one of the neighbouring faces and give it to the rival. That choice is the heart of the game: you want to leave them as little as possible while setting up the next face that comes back to you.

Groups of three or more deal damage. Pairs only give mana. The six elements form a cycle where each one is strong against the next, so the color you break matters as much as how many pieces you break.

Between battles you visit a shop, receive blessings, run into events and choose which door to take on the next floor. Equipment, runes and ability upgrades push your build further, and the idea is that by the last floors your board is chaining far more often than it did at the start.

## Features

- **3D cube battles** on a 6x6 board per face, with gravity and cascades
- **Seven heroes** inspired by chess pieces and cards: King, Knight, Bishop and Joker on your side, the Black Queen, Black Rook and Black Pawn on the other
- **Six elements** (Sun, Sea, War, Death, Harvest and Storm) with strengths and weaknesses
- **Runs of six floors** with elite rivals, a boss, a shop, blessings and random events
- **Armory**: pick equipment for each hero before the run, including poison and burn items that let you leave a hazard area on the face you hand over
- **Ability upgrades** (+, ++, +++, ★) earned during the run
- **Rival AI** with different levels of attention
- **Save and resume** a run in progress
- **English, Portuguese and Spanish** text

## Getting started

### Requirements

- [Unity **6000.6.4f1**](https://unity.com/releases/editor/archive) (other Unity 6 versions may work but are not tested)
- **Windows Build Support (Mono)**, added in Unity Hub under **Installs ▸ 6000.6.4f1 ▸ Add modules**
- [.NET SDK 8](https://dotnet.microsoft.com/download) or newer, only for running the logic tests outside Unity

Everything else the project needs is already in the repo or comes from the Unity Package Manager.

### Open the project

1. Clone this repository.
2. Add the cloned folder in Unity Hub and open it with Unity 6000.6.4f1. The first import takes a while.
3. Go to **File ▸ Build Profiles**, select **Windows** and press **Switch Platform** if it is not the active platform yet.
4. Open `Assets/Game/Content/Materials/Scenes/Main.unity`.
5. Set the Game view to a portrait resolution such as 1080x1920 (the UI is laid out for a tall 9:16 window) and press Play.

### Shortcuts for testing

While in Play mode there are a few shortcuts to move faster:

| Key / control | What it does |
| --- | --- |
| **Screens** button (top right, development builds only) | Jump straight to any screen (shop, blessing, events...) |
| `F9` | Win the current battle |
| `F10` | Leave the rival on 1 HP |
| `F11` | Force an event on the next floor |

The function keys only work inside the editor. You can also find them, along with the screen picker, under the **Cubo ▸ Teste** menu.

## Building for Windows

1. Go to **File ▸ Build Profiles** and select **Windows**.
2. Make sure `Main` is in the scene list.
3. Under **Player Settings ▸ Other Settings**, keep **Scripting Backend** on **Mono**. IL2CPP builds for Windows can only be made on a Windows computer; Mono works from Windows and from macOS.
4. Press **Build** and pick an empty folder.
5. To share the game, zip the whole folder. The `.exe` needs the `_Data` folder and the other files next to it.

When it starts, the game opens a 9:16 window as tall as the screen allows, leaving the taskbar visible. The window cannot be resized or switched to full screen, because the UI is laid out for that shape. Close the window to quit.

### Controls

| Action | Mouse |
| --- | --- |
| Pick and fire a group | Click it |
| Use a power | Click it when its bar is full |
| See what a power, rune or item does | Hover over it, or hold the mouse button on it |
| Aim poison or burn | Hold the mouse button on the cube, let go to place the area |
| Hand over a face | Click a neighbouring face, then **Hand over** |
| Scroll a list | Drag it or use the mouse wheel |

## Running the tests

The game rules do not depend on Unity, so most of the logic is tested with plain .NET and xUnit. The test project compiles the code straight from `Assets/Game/Domain`, `Assets/Game/Data` and `Assets/Game/App`:

```bash
cd domain-tests
dotnet test
```

These run in a few seconds and cover the battle engine, cascades, AI, the run flow, the shop, saving and more.

There are also a few Edit Mode tests for the cube rig and HUD prefabs in `Assets/Tests/EditMode`. Run them from **Window ▸ General ▸ Test Runner** inside Unity.

## Project layout

The code is split into assemblies so that the game rules never touch Unity:

```
Assets/
├── Game/
│   ├── Domain/     Rules of a battle: board, groups, cascades, powers, AI (pure C#)
│   ├── Data/       Loads the JSON content into domain definitions
│   ├── App/        Run flow, screen models, shop, events, saving (pure C#)
│   ├── View/       Everything Unity: cube rendering, input, UI screens, effects
│   └── Content/    JSON data, scenes, materials and 3D models
├── Editor/         Builder scripts that generate and update the UI prefabs
├── Tests/          Unity Edit Mode tests
└── Plugins/        Third-party code (DOTween, free version)
domain-tests/       .NET test project for Domain, Data and App
tools/blender/      Python scripts used to generate the cube models in Blender
```

`Domain` only knows about numbers and rules. `App` sits on top of it and describes what each screen should show. `View` reads those models and draws them. If you want to change how the game plays, you will almost always be working in `Domain` or in the JSON files, not in Unity.

## Game data

Most of the balance lives in JSON under `Assets/Game/Content/Json/data`, so you can tweak it without touching code:

| File | What is in it |
| --- | --- |
| `rules.json` | Board rules: minimum group size, cascades, damage, mana, element cycle, hazards |
| `heroes.json` | Heroes, their colors, passives and powers |
| `run.json` | Floors, gold, shop prices, enemy scaling |
| `upgrades.json` | Ability and item upgrade levels |
| `equipment.json` | Armory items |
| `runes.json` | Runes sold in the shop |
| `events.json` | Random events between floors |
| `domains.json` | The six elements |

Text for every language is in `Assets/Game/Content/Json/texts`. Adding a language means adding a new `ui.<code>.json` file and the matching entries in `names.json`.

Some notes inside the JSON files and some code comments are still in Portuguese. Translating them is a welcome contribution.

## Contributing

Bug reports, ideas and pull requests are all welcome. A few things that help:

- **Open an issue first** for anything larger than a small fix, so we can agree on the direction before you spend time on it. The design is still moving and some mechanics (like cube rotation and card decks) were removed on purpose.
- **Keep the rules out of Unity.** New mechanics should go in `Domain` or `App` with tests in `domain-tests`.
- **Run `dotnet test`** before opening a pull request.
- **Prefer prefabs over scene edits.** Change the prefab, not the copy in `Main.unity`, so merges stay small.

## Third-party assets

This repository includes or depends on work by other people, each under its own license:

- [DOTween](http://dotween.demigiant.com/) (free version) by Daniele Giardini (Demigiant), for tweens and animations
- [TextMesh Pro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest) by Unity Technologies
- Fonts: [Cinzel](https://fonts.google.com/specimen/Cinzel) by Natanael Gama and [Alegreya](https://fonts.google.com/specimen/Alegreya) by Huerta Tipográfica (SIL Open Font License 1.1), [Alagard](https://www.dafont.com/alagard.font) and [Romulus](https://www.dafont.com/romulus.font) by Hewett Tsoi. Full notices are in [`Assets/Game/View/Fonts/LICENSES.md`](Assets/Game/View/Fonts/LICENSES.md).
- Pixel art made with the help of [PixelLab](https://www.pixellab.ai/)

The MIT license below applies to the original code and content of this project. It does not change the license of the assets listed above.

## License

Released under the [MIT License](LICENSE.md).
