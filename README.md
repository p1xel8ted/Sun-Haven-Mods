# Sun Haven Mods

A collection of **13 BepInEx mods** for [Sun Haven](https://store.steampowered.com/app/1432860/Sun_Haven/).

## Install

### 1. Install the BepInEx pack

Every mod here needs the [**Sun Haven BepInEx Pack**](https://www.nexusmods.com/sunhaven/mods/30) installed first — it sets up the modding framework and bundles ConfigurationManager (used to edit settings in-game). It also bundles [**Keep Alive**](https://www.nexusmods.com/sunhaven/mods/31), which most of the mods below depend on (without it, mods unload when you return to the main menu and the game has to be restarted).

### 2. Install the mods

**Vortex (recommended).** Every mod page has a "Mod Manager Download" button — click it and Vortex handles the rest.

**Manually.** Download the mod's ZIP from its Nexus page and extract it into:

```
...\steamapps\common\Sun Haven\BepInEx\
```

That drops the mod's DLL into `BepInEx\plugins\<ModName>\` where the game will load it.

### 3. Configure (optional)

Launch the game, then press **F1** to open BepInEx Configuration Manager. Every mod's settings live there. You can also edit the configuration files in `BepInEx\config\` directly if you prefer.

## The mods

### Quality of life & UI

- **[Easy Living](https://www.nexusmods.com/sunhaven/mods/37)** — Grab-bag of menu and HUD tweaks: Quick Save with a keybind, Continue button + auto-load on startup, Quit to Desktop, hide social/DLC/patch-note banners, adjustable player speed, optional health/mana regen, mute on focus loss, bigger quest tracker, and more. All toggleable.
- **[UI Scales](https://www.nexusmods.com/sunhaven/mods/7)** — Independent scale for nearly every UI surface (HUD, action bar, quest tracker, backpack, chat, tooltips, shop). Extends the zoom range from the vanilla 2× – 4× to 0.5× – 10× and binds zoom to keys.
- **[Auto Tools](https://www.nexusmods.com/sunhaven/mods/199)** — Automatically swaps to the right tool based on what your cursor or right stick is aimed at. Picks the highest-tier tool on your bar, prioritises full watering cans, sends empty cans back to water, and disables itself in combat.
- **[Character Edit Redux](https://www.nexusmods.com/sunhaven/mods/418)** — Re-open the character creator on any save from the load-game screen to change appearance or name. Makes a save backup before each edit. (Experimental — back up first.)
- **[No Time For Fishing](https://www.nexusmods.com/sunhaven/mods/27)** — Skip the fishing minigame, with an auto-reel option. Plus knobs for spawn rate, bobber attraction range, rare-fish odds, museum-item odds, and more.
- **[No Time To Stop & Eat](https://www.nexusmods.com/sunhaven/mods/222)** — Eat without stopping. Food effects apply instantly; optionally hide the food-item visual.

### Gameplay tweaks

- **More The Merrier!** _(not yet on Nexus — initial release pending)_ — Raises the multiplayer lobby cap from 8 to 100. Last-used count is remembered between sessions.
- **[More Jewelry!](https://www.nexusmods.com/sunhaven/mods/224)** — Adds 6 extra jewelry slots (2 rings, 2 keepsakes, 2 amulets) — stat bonuses included. Toggleable pouch UI; option to make the extras storage-only.
- **[More Scythes Redux](https://www.nexusmods.com/sunhaven/mods/205)** — Adds four craftable scythes — Adamant, Mithril, Sunite, Glorite — with scaling damage and attack speed. Updated from [Brqdford's original](https://www.nexusmods.com/sunhaven/mods/188).

### Farming & economy

- **[Seedify](https://www.nexusmods.com/sunhaven/mods/426)** — Plant any seed in any season on any farm type, buy out-of-season seeds from the farming shop, and make every crop regrowable. Each option toggleable.
- **[Museum Sell Price Redux](https://www.nexusmods.com/sunhaven/mods/207)** — Makes museum-only items worth selling instead of nearly worthless. Adjustable multiplier; sells for the right currency (gold, orbs, or tickets) per item category. Updated from [Mimikitten's original](https://www.nexusmods.com/sunhaven/mods/142).

### Cheats & dev tools

- **[Cheat Enabler](https://www.nexusmods.com/sunhaven/mods/8)** — Unlocks the built-in dev console with no restrictions. Adds helpers like `/finditemid`, `/printallnpcs`, `/printallscenes`, and a `/man` command for in-console manuals. DLC items are intentionally excluded.

### Required by most other mods

- **[Keep Alive](https://www.nexusmods.com/sunhaven/mods/31)** — Stops mods from unloading when you return to the main menu, so you don't have to restart the game between play sessions. Already bundled in the [BepInEx Pack](https://www.nexusmods.com/sunhaven/mods/30); installed separately if you're not using the pack.

## Save compatibility

- **Most mods are safe to add or remove mid-save** — they patch game behaviour at runtime and don't persist anything unusual to your save file.
- **More Jewelry** — items in the extra 6 slots are saved per-character. Uninstalling the mod doesn't lose them, but the vanilla UI can't reach them; reinstall to recover, or use the storage-only mode if you want to keep the slots disabled but accessible elsewhere.
- **Character Edit Redux** — overwrites your character file on each edit. The mod takes a backup automatically, but back up your save folder before any major change anyway.
- **Seedify** — turning *off* "plant in any season" mid-save kills any out-of-season crops in the ground immediately. Harvest first if you want to keep them.
- **More The Merrier!** — only affects the multiplayer-lobby slider. No save-file impact.
- **Back up your save** before any big config change.

## Support & contact

- **Ko-fi** — [ko-fi.com/p1xel8ted](https://ko-fi.com/p1xel8ted). Development, testing, and user support take real time; any support is greatly appreciated. If you'd rather not donate, clicking the Endorse button on the Nexus pages also helps.
- **Discord** — [discord.gg/Dy5ApMYYY8](https://discord.gg/Dy5ApMYYY8). Faster than Nexus comments for back-and-forth.
- **Bug reports / feature requests** — comment on the specific mod's Nexus page. GitHub issues are disabled; Nexus is the single channel.
- **Crash on startup?** Check `BepInEx\LogOutput.log` for the most recent stack trace, and note which mods you have installed.

## Licence & credits

Licensed under the **GNU General Public License v3.0** — see [LICENSE](LICENSE) for the full text. In short: you're free to use, study, modify, and redistribute the source under the same licence; any distributed fork must also be GPL v3.

Two mods are continuations of earlier work by other authors:

- **More Scythes Redux** is based on [More Scythes](https://www.nexusmods.com/sunhaven/mods/188) by [@Brqdford](https://www.nexusmods.com/sunhaven/users/101098328).
- **Museum Sell Price Redux** is based on [Museum Only Items Sell Price](https://www.nexusmods.com/sunhaven/mods/142) by [@Mimikitten](https://www.nexusmods.com/sunhaven/users/52674526).
