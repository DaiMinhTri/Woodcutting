> **Disclaimer:**
> Most of mods I work with are some old and outdated ones that their authors haven't updated so far to match 1.0. I am not a professional modder myself, but I am good with coding and gaming. If you experience any problems with the mods I published, you can find me in [Hexium](https://discord.gg/W6ayFy8CR) discord server by typing DMT.

# dmtWoodcutting

A Valheim mod that turns the vanilla **Woodcutting** skill into a full lumberjack progression: smooth perks on every skill point, plus milestones — HP on aim, 1 HP stumps, trees falling where you face, stamina per felled tree, one-hit fell chance, uninterrupted axe combos, axe conservation and boosted XP.

It uses the vanilla Woodcutting skill — no custom skill, vanilla XP rates and vanilla skill UI.

**GitHub:** <img height="18" src="https://github.githubassets.com/favicons/favicon-dark.svg"></img> [DaiMinhTri/Woodcutting](https://github.com/DaiMinhTri/Woodcutting)

## Features

### Progressive perks (every skill point)
- **Item yield:** ×1.00 at level 0 up to ×2.00 (configurable) at level 100, scaled linearly by your current level
- **Falling-tree damage:** 100% at level 0 down to 30% (configurable) at level 100
- **Forest speed:** up to +10% (configurable) run/jog speed in forests at level 100
- **Log HP:** felled logs and half-logs get weaker continuously with level, following `2^(−3 × level/100)` — halved around skill 33, quartered around 67 and down to 1/8 at 100. Standing trees always keep vanilla HP

### Milestones
| Level | Perk |
|---|---|
| 10 | **HP on aim** — aim at a tree, stump or log to see its current / maximum HP above the crosshair |
| 20 | **Stumps 1 HP** — one hit from anything destroys a stump (9 vanilla prefabs, incl. Ashlands) |
| 30 | **Lumberjack** — trees you fell with melee hits fall in the direction you are facing |
| 40 | **Felling Stamina** — each felled standing tree grants stamina, growing from 0 at level 40 to +15 (configurable) at level 100 |
| 50 | **Axe combo** — axe attack chains no longer reset while chopping trees — swings 1→2→3 flow continuously |
| 60 | **Felling Blow** — chance to drop a standing tree with a single hit, growing from 0% at level 60 to 30% (configurable) at level 100 |
| 70 | **Axe Conservation** — 30% (configurable) of tree/log hits cost no durability, growing to 50% (configurable) at level 100 |
| 80 | **XP boost** — Woodcutting XP is earned ×1.5 (configurable) |

### Progression table

| Level | Yield | Fall damage | Forest speed | HP aim | Stumps | Lumberjack | Stamina/tree | Axe combo | Felling blow | Conservation | Log HP | XP |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | ×1.00 | 100% | — | — | vanilla | — | — | — | — | — | ×1.00 | ×1.0 |
| 10 | ×1.10 | 93% | +1% | **on** | vanilla | — | — | — | — | — | ×0.81 | ×1.0 |
| 20 | ×1.20 | 86% | +2% | on | **1 HP** | — | — | — | — | — | ×0.66 | ×1.0 |
| 30 | ×1.30 | 79% | +3% | on | 1 HP | **on** | — | — | — | — | ×0.54 | ×1.0 |
| 40 | ×1.40 | 72% | +4% | on | 1 HP | on | **starts** | — | — | — | ×0.44 | ×1.0 |
| 50 | ×1.50 | 65% | +5% | on | 1 HP | on | +2.5 | **on** | — | — | ×0.35 | ×1.0 |
| 60 | ×1.60 | 58% | +6% | on | 1 HP | on | +5.0 | on | **starts** | — | ×0.29 | ×1.0 |
| 70 | ×1.70 | 51% | +7% | on | 1 HP | on | +7.5 | on | 7.5% | **30%** | ×0.23 | ×1.0 |
| 80 | ×1.80 | 44% | +8% | on | 1 HP | on | +10.0 | on | 15% | 37% | ×0.19 | **×1.5** |
| 100 | ×2.00 | 30% | +10% | on | 1 HP | on | **+15** | on | **30%** | **50%** | ×0.13 | ×1.5 |
| 100 | × | 2.00 | 30% | +10% | on | 1 HP | on | **+15** | on | **30%** | **50%** | × | 0.13 | × | 1.5 |

Yield, fall damage, forest speed and log HP grow with **every** skill point. Stamina and felling blow grow linearly from their start level to 100. Milestone effects apply to already-existing stumps/logs the moment you cross the level.

### Uses the vanilla skill
Woodcutting XP is earned exactly like vanilla (chop trees with an axe), vanilla shows the skill in its normal UI, and vanilla's skill-based chop damage bonus applies. Existing Woodcutting levels are kept.

## Configuration

A configuration file is generated at `BepInEx/config/dmtWoodcutting.cfg` after the first launch. All settings are synced from the server.

| Section | Setting | Default | Description |
|---------|---------|---------|-------------|
| 1 - General | Lock Configuration | On | If on, the configuration is locked and can be changed by server admins only. |
| 1 - General | Tree item yield modifier at level 100 | 2 | Item yield from trees will be modified by this value at skill level 100. (1–5) |
| 1 - General | Tree fall damage modifier at level 100 | 0.3 | Damage from falling trees will be modified by this value at skill level 100. (0–1) |
| 1 - General | Movement speed modifier in forests at level 100 | 1.1 | Movement speed in forests will be modified by this value at skill level 100. (1–2) |
| 2 - Stumps and Logs | Stump Skill Level | 20 | Woodcutting level at which stumps get Stump HP. 0 = always. Below this level stumps keep vanilla HP. (0–100) |
| 2 - Stumps and Logs | Stump HP | 1 | HP for tree stumps at or above Stump Skill Level. 1 = one hit destroys a stump. (0–1000) |
| 2 - Stumps and Logs | Enable Log HP | On | Enable HP reduction for felled tree logs. |
| 2 - Stumps and Logs | Log HP | 0 | 0 = progressive skill curve (2^(−3 × level/100): ÷2 around 33, ÷4 around 67, ÷8 at 100). Above 0 = fixed HP at every level. (0–1000) |
| 3 - HP Display | Show HP On Aim | On | Show the current and maximum HP of a tree, stump or log when aiming at it. |
| 3 - HP Display | HP Display Skill Level | 10 | Woodcutting level at which HP is shown when aiming. 0 = always. (0–100) |
| 4 - Axe Combo | Enable Axe Combo | On | Makes the axe attack chain uninterruptible when hitting trees. |
| 4 - Axe Combo | Axe Combo Skill Level | 50 | Woodcutting level at which the axe combo activates. 0 = active from level 0. (0–100) |
| 5 - Felling Stamina | Enable Felling Stamina | On | Gain stamina each time you fell a standing tree. |
| 5 - Felling Stamina | Stamina Start Level | 40 | Level at which felling a tree starts granting stamina; amount grows linearly to the maximum at 100. (0–100) |
| 5 - Felling Stamina | Max Stamina At Level 100 | 15 | Stamina gained per felled standing tree at level 100. (0–100) |
| 6 - Felling Blow | Enable Felling Blow | On | Chance that a standing tree falls in a single hit. |
| 6 - Felling Blow | Felling Blow Start Level | 60 | Level at which the one-hit fell chance starts growing to the maximum at 100. (0–100) |
| 6 - Felling Blow | Max Chance At Level 100 | 30 | Percent chance to fell a standing tree in one hit at level 100. (0–100) |
| 7 - Axe Conservation | Enable Axe Conservation | On | Chance that hitting a tree or a log does not cost axe durability. |
| 7 - Axe Conservation | Axe Conservation Skill Level | 70 | Woodcutting level at which axe conservation activates. (0–100) |
| 7 - Axe Conservation | Chance | 30 | Percent chance that a tree or log hit costs no durability at 'Axe Conservation Skill Level'; grows linearly to 'Max Chance At Level 100'. (0–100) |
| 7 - Axe Conservation | Max Chance At Level 100 | 50 | Percent chance that a tree or log hit costs no durability at level 100 (linear from 'Chance'). (0–100) |
| 8 - XP Boost | Enable XP Boost | On | Boost Woodcutting XP gain at high skill levels. |
| 8 - XP Boost | XP Boost Skill Level | 80 | Woodcutting level at which the XP multiplier starts applying. (0–100) |
| 8 - XP Boost | XP Multiplier | 1.5 | Multiplier applied to Woodcutting XP at or above XP Boost Skill Level. (1–5) |
| 9 - Lumberjack | Enable Lumberjack | On | Trees felled with melee hits fall in the direction you are facing. |
| 9 - Lumberjack | Lumberjack Skill Level | 30 | Woodcutting level at which felled trees fall in the direction you face. 0 = always while enabled. (0–100) |

### Classic behavior presets
- **StumpsAreOneHp behavior:** `Stump Skill Level = 0` — stumps 1 HP from level 0
- **blacks7ar AxeComboWhenChoppingTree behavior:** `Axe Combo Skill Level = 0`
- **Always 1 HP logs:** `Log HP = 1`
- **Untouched logs:** `Enable Log HP = Off`

## Compatibility

### Incompatible (same features — the mod manager will warn you)
- **StumpsAreOneHp** (DMT and coemt versions) — stump/log HP is built in
- **AxeComboWhenChoppingTree** by blacks7ar — axe combo is built in
- **ValheimPlus**

### Multiplayer
Install on the server and clients. Stump/log HP follows the host's (or dedicated server's) Woodcutting level for world objects; felling blow is rolled on the owner of the tree with the attacker's skill. The axe combo, HP display, lumberjack, stamina, conservation and XP boost are evaluated per player from your own skill. Configuration is locked and synced from the server.

## Migrating from other mods
- **StumpsAreOneHp:** uninstall it and use this mod instead — stump/log HP is now controlled from section "2 - Stumps and Logs" of `dmtWoodcutting.cfg` (its old `DMT.StumpsAreOneHp.cfg` is no longer read)

## Installation

1. Install [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
2. Download and extract `dmtWoodcutting.dll` into your `BepInEx/plugins/` folder
3. Launch the game to generate the config file `BepInEx/config/dmtWoodcutting.cfg`
4. All settings can be tuned in that config file. Server and all clients must run the same mod version for config sync to work.

## Credits

Originally created and maintained by **DaiMinhTri** — updated for Valheim 1.0.
Axe combo feature based on **AxeComboWhenChoppingTree** by **blacks7ar**. Stump/log HP feature based on **StumpsAreOneHp** by **coemt**. ServerSync library by **Blitz**.

## Buy Me a Coffee

If you enjoy this mod, consider buying me a coffee: [![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-daiminhtri-yellow)](https://buymeacoffee.com/daiminhtri)

## Contact information

Questions, bug reports, or suggestions: open an issue at https://github.com/DaiMinhTri/Woodcutting/issues
