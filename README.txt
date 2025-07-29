# GambaPlugin - Blackjack Game for Dalamud

A gambling plugin for Dalamud (FFXIV plugin framework) that lets players join sessions and play Blackjack with persistent chips and gil balances. Features betting, chip purchases, multiplayer support, and a simple ImGui interface.

---

## Features

- Multiplayer Blackjack game with turn-based play  
- Place bets before each round  
- Buy chips using gil at a 1:1 rate anytime  
- Persistent chip and gil balances saved between sessions  
- ImGui-based GUI for easy interaction  
- Easily extensible to add more gambling games  

---

## Installation

1. Download the latest release ZIP from [Releases](https://github.com/yourusername/gambaplugin/releases) (coming soon)  
2. Extract the ZIP into your Dalamud `plugins` folder (usually `%AppData%\XIVLauncher\plugins`)  
3. Enable the plugin in your plugin manager  
4. Use `/gamba` (or your chosen command) to open the GUI  

---

## Usage

- Create or join a game session  
- Players can buy chips at any time with gil using the GUI  
- Place bets before starting each Blackjack round  
- Take turns to hit or stand during the round  
- Winners receive double their bet in chips  
- Chips and gil persist even if you restart the game  

---

## Development

To build from source:

```bash
dotnet build -c Release
