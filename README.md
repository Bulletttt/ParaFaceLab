# ParaFaceLab

A lightweight, in-game face & body sculpting panel for **Paralives** (tested on Patch 0.1.10).  
一款轻量级的 **Paralives（平行人生）** 游戏内捏脸与身材精修工具（已适配 Patch 0.1.10）。

- **Download / 下载**：[GitHub Releases](https://github.com/Bulletttt/ParaFaceLab/releases) | [Nexus Mods](https://www.nexusmods.com/paralives/mods/422)
- **Languages / 支持语言**：English / 简体中文 / 繁體中文 / 日本語 / 한국어（自动跟随游戏语言，顶栏可实时切换）

---

## Features / 功能特点

- **Fine Facial & Body Bone Sculpting（面部与身材骨骼三轴精修）**
  - Position / Rotation / Scale across curated eyes, eyelids, 3-segment eyebrows, nose, lips, teeth, tongue, cheeks, jaw, ears, hair anchor, chest, shoulders, waist, hips, arms, thighs, calves, feet, toes, plus an All-Rig bone explorer.
  - Symmetry toggle (mirrored or asymmetric editing).
  - Pre-frame baseline restoration + separate head/torso reference frames: preserves all vanilla facial animations and prevents drift across **Paramaker, Live Mode, Photo Mode, and Character Thumbnails**.
  - Dedicated **Refresh Portrait** button (plus auto-refresh on panel close in Live Mode) to update character thumbnails with your sculpted bones.
- **Vanilla+ Over-Limit Sliders（原版拉杆突破上下限）**
  - Smoothly extrapolates past `0.0 ~ 1.0` (supports `-0.5 ~ 1.5` and `-1.0 ~ 2.0`).
  - Per-character seamless **Unlock Vanilla Limits**: automatically bakes/unbakes effective slider values on toggle so your Para never jumps or deforms, and never affects untouched NPCs.
  - Dedicated safe 0/1 switches for Masculine / Feminine chest & lower-body frame modes.
- **Smart Presets（智能预设与跨年龄换算）**
  - Apply **Full**, **Face Only**, or **Body Only** (and Bones+Vanilla / Bones Only / Vanilla Only).
  - **Cross-Age Auto-Adapt**: never overwrites the target Para's age; automatically adapts teen/adult slider ranges to preserve visual proportions.
  - 1-click **Update / Overwrite** (with 2-step confirmation) on existing presets.
- **QoL & Safe Storage（体验与存盘安全）**
  - 50-step Undo / Redo (`Ctrl+Z` / `Ctrl+Y`), Hold to Compare, Revert All.
  - 4 rounded themes (Warm Cream / Soft Mist / Studio Dark / Sakura Milk) and 3 UI scale tiers (`100% / 115% / 130%`).
  - Bone profiles & presets are stored as standalone JSON in `BepInEx/plugins/ParaFaceLab.data/`. Never corrupts game saves.

---

## Requirements / 前置要求

- [BepInEx 5.4.x (x64)](https://github.com/BepInEx/BepInEx/releases) installed in your Paralives game directory.

---

## Installation / 安装方法

1. Download `ParaFaceLab_v1.3.0.zip` from [Releases](https://github.com/Bulletttt/ParaFaceLab/releases) and extract it.
2. Copy `BepInEx/plugins/ParaFaceLab.dll` into `<Paralives Game Folder>/BepInEx/plugins/`.
3. *(Optional)* Copy `Optional_OfficialModMenu/ParaFaceLab_9174628391048271645.mod` into `%USERPROFILE%\AppData\LocalLow\Paralives\Paralives\` to show ParaFaceLab as an On/Off toggle card in the official in-game Mods menu.
4. Launch Paralives, enter Paramaker or Live Mode, and press **F9** to toggle the panel.

### Controls / 快捷键
- **F9**: Open / close panel (customizable in `BepInEx/config/paralives.facelab.cfg`)
- **Shift + Drag**: 3x fine precision dragging
- **Double-click slider track**: Reset single slider
- **Ctrl+Z / Ctrl+Y**: Undo / Redo

---

## Uninstallation / 卸载方法

1. Close Paralives.
2. Delete `BepInEx/plugins/ParaFaceLab.dll`.
3. *(Optional)* Also delete `BepInEx/plugins/ParaFaceLab.data/`, `BepInEx/config/paralives.facelab.cfg`, and `%USERPROFILE%\AppData\LocalLow\Paralives\Paralives\ParaFaceLab_9174628391048271645.mod` if you want to remove all saved presets and settings.
4. Game saves remain completely intact — bone edits revert cleanly to vanilla, and any over-limit vanilla sliders are safely clamped back to vanilla ranges by the game.

---

## Build from Source / 从源码编译

Requires Windows .NET Framework 4.x (`csc.exe`) and Paralives with BepInEx 5 installed:

```bat
set PARALIVES_DIR=C:\Path\To\Paralives
build.bat
```

(`build.bat` also auto-detects common Steam library locations if `PARALIVES_DIR` is not set.)

---

## License

MIT License — see [LICENSE](LICENSE).
