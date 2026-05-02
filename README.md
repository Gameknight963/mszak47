# mszak47

A MelonLoader mod that adds guns to Miside Zero, built on top of InventoryFramework. Guns are defined as folders containing a config file and assets, no code is required to add new ones.

## Features:

- Modular gun system requiring no scripting
- Optional Photon multiplayer through [Multiside](https://github.com/Gameknight963/Multiside)

### Multiplayer roadmap
 - ✅ Selected gun syncing
 - ✅ Shot syncing + audio
 - ❌ ADS syncing
 - ❌ Gun pitch (up down) syncing (Requires to be implemented in Multiside)
 - ❌ Health system

## Installation

Get the latest release 

https://github.com/Gameknight963/mszak47/releases

Follow the instructions there.

---

## Build + file setup

Clone this repo and build the project (all the referenced assemblies are contained within the repo.) Put `mszguns.dll` into your `Mods` folder alongside `InventoryFramework.dll`, `InventoryUI.dll`, and optionally `Multiside.dll` and `Multiside.shared.dll` if you want multiplayer.

Get the following dependencies and put them in `UserLibs`:

- NAudio.Core.dll

- NAudio.dll

- NAudio.Wasapi.dll

- NAudio.WinMM.dll

- VGltf.dll

- VJson.dll

Create a `mszguns` folder in your `Mods` directory, this is where your gun folders go.

```
Mods/
  InventoryFramework.dll
  InventoryUI.dll
  mszguns.dll
  mszguns/
    hole.png
    my_gun/
      gun.json
      model.glb
      shot.wav
      icon.png
```

---

## Creating a Gun

Each gun lives in its own subfolder inside `Mods/mszguns/`. The folder must contain a `gun.json` file. All other files are discovered automatically if you follow the naming conventions below.

### Folder Structure

```
mszguns/
  my_gun/
    gun.json
    model.glb ← gun model (GLB format)
    shot.wav  ← gunshot audio (.wav or .mp3)
    icon.png  ← inventory icon
    hole.png  ← bullet hole texture (optional, only if you wish to override it)
```

File names are flexible, the loader will find them as long as they have the right extension. You only need to set the file fields in `gun.json` if you want to use non-default names.

### gun.json

```json
{
  "Id": "my_gun",
  "DisplayName": "My Gun",
  "FireRate": 0.1,
  "AudioVolume": 0.5,
  "Damage": 10.0,
  "Range": 100.0,
  "AdsSpeed": 0.2,
  "AdsFov": 50.0,
  "RecoilKickDuration": 0.05,
  "RecoilRecoverDuration": 0.15,
  "BulletHoleDuration": 10.0,
  "NormalPosition": [0.3, -0.3, 0.5],
  "AdsPosition": [0.0, -0.15, 0.4],
  "NormalAngle": [0.0, 0.0, 0.0],
  "RecoilAngle": [-5.0, 0.0, 0.0],
  "Effect": "Normal"
}
```

All fields are optional and will fall back to defaults if omitted.

### Fields

| Field                   | Type     | Default                                     | Description                                                      |
| ----------------------- | -------- | ------------------------------------------- | ---------------------------------------------------------------- |
| `Id`                    | string   | folder name                                 | Unique identifier. Inferred from the folder name if not set.     |
| `DisplayName`           | string   | `""`                                        | Name shown in the inventory label.                               |
| `ModelFile`             | string   | first `.glb` in folder                      | Path to the GLB model file.                                      |
| `AudioFile`             | string   | first `.wav`/`.mp3` in folder               | Path to the gunshot audio file.                                  |
| `IconFile`              | string   | `icon.*` or first `.png`                    | Path to the inventory icon.                                      |
| `HoleFile`              | string   | `hole.*` in folder, then `mszguns/hole.png` | Path to the bullet hole texture.                                 |
| `FireRate`              | float    | `0.1`                                       | Seconds between shots when holding fire.                         |
| `AudioVolume`           | float    | `0.5`                                       | Gunshot audio volume (0–1).                                      |
| `Damage`                | float    | `10.0`                                      | Damage per shot. (Currently unused, reserved for future use.)    |
| `Range`                 | float    | `100.0`                                     | Raycast range in units.                                          |
| `AdsSpeed`              | float    | `0.2`                                       | Time in seconds to transition to/from ADS.                       |
| `AdsFov`                | float    | `50.0`                                      | Field of view while aiming down sights.                          |
| `RecoilKickDuration`    | float    | `0.05`                                      | Time to reach the recoil angle on fire.                          |
| `RecoilRecoverDuration` | float    | `0.15`                                      | Time to return to the normal angle after recoil.                 |
| `BulletHoleDuration`    | float    | `10.0`                                      | Seconds before a bullet hole starts fading.                      |
| `NormalPosition`        | float[3] | `[0, 0, 0]`                                 | Local position of the gun relative to the camera when hipfiring. |
| `AdsPosition`           | float[3] | `[0, 0, 0]`                                 | Local position when aiming down sights.                          |
| `NormalAngle`           | float[3] | `[0, 0, 0]`                                 | Local euler angles when hipfiring.                               |
| `RecoilAngle`           | float[3] | `[0, 0, 0]`                                 | Local euler angles at peak recoil.                               |
| `Effect`                | string   | `"Normal"`                                  | Shot effect. See below.                                          |

### Shot Effects

| Value     | Description                                                               |
| --------- | ------------------------------------------------------------------------- |
| `Normal`  | Single raycast shot. Spawns a bullet hole on hit.                         |
| `Shotgun` | 8 raycasts with random spread. Spawns a bullet hole per pellet that hits. |
| `Cube`    | Spawns a physical cube projectile that flies forward. No bullet holes.    |

More effects are planned.

---

## Controls

| Input                     | Action                               |
| ------------------------- | ------------------------------------ |
| Left mouse button         | Fire                                 |
| Right mouse button        | Aim down sights                      |
| 1–9                       | Select hotbar slot (via InventoryUI) |

---

## Notes

- Models must be in GLB format. The loader reads only the base color texture from the material, PBR properties are ignored.
- Audio supports `.wav` and `.mp3`, if you missed that.
- To reiterate, `Damage` is defined in the config but not currently applied to anything in-game. It is there for future use, probably.
- The bullet hole fallback chain is: gun folder `hole.*` → `mszguns/hole.png`. If neither exists the mod will throw on load.
- Left and right hand grip points (`Grip`) are defined in code but not yet active pending IK support. FinalIK why do you do this to me.

### Tip to align your guns well

It's not going to be perfect the first time. Instead of adjusting `gun.json` and reloading the game over and over again, install [UnityExplorer](https://github.com/yukieiji/UnityExplorer) (fork maintained by yukieiji) and use it to adjust the position in real time. When you're happy with it, copy the numbers you set and put them in `gun.json`.

### Enabling network logging
I overcomplicated it in the code for some reason lol. The `GunNetworking` singleton has a `LoggingEnabled` bool property you can enable that will enable debug messages. Note that you will need to create a mod to do this, I left it in because why not.
