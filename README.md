# Stardew Claude Companion

A SMAPI mod for Stardew Valley that reads your save data (fish caught, etc.)
and will eventually let you ask Claude AI questions about your farm progress
directly in-game.

## Status: Work in Progress

Currently implemented:

- Reads caught fish data from the player's save (`Game1.player.fishCaught`)
- Maps internal fish IDs to bilingual (Chinese/English) display names
- Press F5 in-game to print your full fish collection to the SMAPI console

Planned:

- Compare against full fish list to show missing fish for full collection
- In-game input UI to ask questions
- Claude API integration for natural language Q&A about save data

## Tech Stack

- C# / .NET 6
- SMAPI (Stardew Modding API)
- Anthropic Claude API (planned)

## Setup

1. Install [SMAPI](https://smapi.io)
2. Build this project with `dotnet build` — the mod will auto-deploy to your
   Stardew Valley `Mods` folder
3. Launch the game via SMAPI, load a save, press F5
