# NPCs

**NPCs** is a server-side V Rising mod featuring automated scheduled NPC spawning, manual NPC spawning and despawning, customizable NPC teams, and multiple spawn points. Easily bring your server to life with hourly auto-spawns.

## Features
- **Automated Spawning:** Set the exact minute and second when NPCs spawn automatically every hour.
- **Custom NPC Teams:** Create customizable teams with specific prefabs and spawn quantities.
- **Dynamic Spawn Points:** Add and manage multiple spawn points for each team directly in-game.
- **Clean Despawning:** NPCs are removed cleanly when their lifetime expires or when manually despawned.

## Usage Ideas
- Build your dream city with custom NPCs of your choice.
- Create defensive armies in front of boss bases such as Solarus or Adam.
- Set up automatic faction battles every hour.

## Requirements
1. [BepInEx 1.733.2](https://thunderstore.io/c/v-rising/p/BepInEx/BepInExPack_V_Rising/)
2. [VampireCommandFramework 0.11.0](https://thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/)

## Installation
1. Install the required dependencies.
2. Place `NPCs.dll` into your server's `BepInEx/plugins` folder.
3. Start the server once to generate the config files.
4. Edit the configuration files or use commands to set up your NPC teams.
5. Restart the server or use the reload command.

## Commands

### Team Management
- `.npc addteam <teamName> <prefab> <amount>`
  - Add a new team or add/update a prefab in an existing team.
  - Shortcut: *.npc at <teamName> <prefab> <amount>*
  - Example 1: *.npc at human -1670130821 3*
  - Example 2: *.npc at human CHAR_Militia_BellRinger 3*

- `.npc removeteam <teamName> [prefab]`
  - Remove an entire team or a specific prefab from a team.
  - Shortcut: *.npc rt <teamName> [prefab]*
  - Example 1: *.npc rt human* - Remove the entire team.
  - Example 2: *.npc rt human -1670130821* - Remove a prefab from the team.

- `.npc addpoint <teamName>`
  - Add a spawn point at your current location for a specific team.
  - Shortcut: *.npc ap <teamName>*

- `.npc removepoint <teamName>`
  - Remove all spawn points for a specific team and despawn all active NPCs from that team.
  - Shortcut: *.npc rp <teamName>*

### Spawning & Despawning
- `.npc manualspawn`
  - Manually spawn all configured NPCs immediately.
  - Shortcut: *.npc ms*

- `.npc manualdespawn`
  - Manually despawn all NPCs cleanly.
  - Shortcut: *.npc md*

### Configuration & Utilities
- `.npc team [teamName]`
  - List all configured NPC teams, or view detailed settings and prefabs for a specific team.
  - Shortcut: *.npc t [teamName]*
  - Example 1: *.npc t* - List all configured NPC teams.
  - Example 2: *.npc t human* - List detailed information for the team.

- `.npc config`
  - Display the general mod configuration.
  - Shortcut: *.npc c*

- `.npc reload`
  - Reload both `NPCs.cfg` and `npc_teams.json`.
  - Shortcut: *.npc rl*

## Config Files
After the first server start, two configuration files will be created:
- `BepInEx/config/NPCs.cfg`
- `BepInEx/config/NPCs/npc_teams.json`

### 1. NPCs.cfg
Managed via BepInEx Configuration Manager. Contains general settings, timers, and schedules.
- `ModEnabled`: Enable or disable the mod.
- `BroadcastEnable`: Enable or disable the broadcast message when NPCs spawn.
- `BroadcastMessage`: Broadcast message shown when NPCs spawn.
- `HourlySpawnAtMinute`: Minute of each hour to spawn.
- `HourlySpawnAtSecond`: Second of each hour to spawn.
- `LifetimeSeconds`: How long NPCs stay alive in seconds before auto-despawning.

### 2. npc_teams.json
Stores your custom teams and spawn points. You can edit this manually or use in-game commands.
```json
{
  "human": {
    "RandomAround": 15,
    "Prefabs": {
      "426583055": 3,
      "2128996433": 3,
      "794228023": 2,
      "1185952775": 2,
      "-700632469": 1,
      "1660801216": 1
    },
    "SpawnPoints": [
      {
        "X": -317.0079,
        "Y": 17.480003,
        "Z": -687.49274
      }
    ]
  },
  "vamp": {
    "RandomAround": 12,
    "Prefabs": {
      "-1076780215": 3,
      "1912966420": 3,
      "981369753": 2,
      "-65981941": 2,
      "-653348998": 1,
      "1980594081": 1
    },
    "SpawnPoints": [
      {
        "X": -317.0079,
        "Y": 17.480003,
        "Z": -687.49274
      }
    ]
  }
}
```

## Credits
- [KindredCommands](https://thunderstore.io/c/v-rising/p/odjit/KindredCommands/) by **Odjit** for the original code that inspired this mod.
- [KindredSacrifice](https://thunderstore.io/c/v-rising/p/odjit/KindredSacrifice/) by **Odjit** for the despawn NPC system inspiration.
- [V Rising modding community](https://discord.com/invite/QG2FmueAG9)

## License
This project is licensed under the AGPL-3.0 license.

## Notes
> - Many high-risk prefabs have been tested and banned, but not all of them. Please test any prefabs you want to use before using them on a live server.
> - It is recommended to avoid placing spawn points near Vampire Merchants from the Penumbra mod.
> - The despawn system may also affect naturally spawned NPCs that use the same prefab nearby.
> - Units summoned by NPCs from configured prefabs may not be despawned automatically.
> - NPCs may sometimes fail to spawn if the spawn point is located under a roof.
> - You can find a list of supported prefabs on [GitHub](https://github.com/delta663/NPCs/blob/main/Models/WhitelistPrefabs.cs) or [V Rising mods wiki](https://wiki.vrisingmods.com/prefabs/CHAR.html).
> - This mod was first developed for my own server and originally built around [KindredCommands](https://thunderstore.io/c/v-rising/p/odjit/KindredCommands/). Special thanks to **Odjit** for the amazing mod and inspiration behind this project.
> - If you have any problems or run into bugs, please report them to me in the [V Rising Modding Community](https://discord.com/invite/QG2FmueAG9).
> **Del** (delta_663)
