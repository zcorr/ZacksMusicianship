# Item Authoring

This is the working checklist for adding new items to `ZacksMusicianship`.

## Core files

1. Create the item source at `Content/Items/<ItemName>.cs`.
2. Create a matching texture at `Content/Items/<ItemName>.png`.
3. Add a localization entry in `Localization/en-US_Mods.ZacksMusicianship.hjson`.
4. If the item needs related projectiles, buffs, UI, or systems, create those in the matching `Content/` or `Common/` folders.

## Item defaults

- Set `Item.width` and `Item.height` to the real sprite size.
- Set `Item.useTime`, `Item.useAnimation`, `Item.useStyle`, `Item.rare`, `Item.value`, and `Item.noMelee` deliberately.
- Use `AltFunctionUse(Player player)` if the item needs a distinct right-click action.
- Use `CanUseItem` to swap behavior dynamically or block vanilla use and open a custom UI.
- Use `ModifyTooltips` for live state such as progression, unlock counts, or mode text.

## Recipes

- Add recipes in `AddRecipes()`.
- Keep starter-tier items hand-craftable if that supports fast testing.
- Use explicit ingredient counts and tile requirements so balance stays easy to audit.

## Persistent state

- If the item needs saved player data, store it in a `ModPlayer` under `Common/Players/`.
- Save only non-default values in `SaveData`.
- Reset transient runtime state in `Initialize` and `LoadData`.
- If the state matters in multiplayer, add a packet in `ZacksMusicianship.cs` and sync it from the owning `ModPlayer`.

## UI-backed items

- Use `IngameFancyUI.OpenUIState(...)` for fullscreen non-combat screens.
- Keep UI code in `Common/UI/` and the open/close plumbing in `Common/Systems/`.
- Build layouts from parent panels with shared padding and spacing constants. Do not stack magic coordinates everywhere.
- Keep dynamic text short enough that wrapped blocks remain readable at Terraria UI scale.

## Starter items

- Use `ModPlayer.AddStartingItems(bool mediumCoreDeath)` for character-creation items.
- Return nothing on `mediumCoreDeath` unless the item should reappear after mediumcore respawns.

## Assets

- Match Terraria scale and silhouette first; detail second.
- Keep pixel-art assets on a strict grid with transparent backgrounds.
- Put sound assets under `Assets/Sounds/` and reference them by mod path.

## Verification

1. Run:

```bash
dotnet msbuild '/Users/zackcorr/Library/Application Support/Terraria/tModLoader/ModSources/ZacksMusicianship/ZacksMusicianship.csproj' /t:Compile /p:BuildMod=false /p:TargetFramework=net8.0 /p:LangVersion=12.0
```

2. Reload the mod in tModLoader.
3. Check the item in-world, in the hotbar, in tooltips, and inside any custom UI.
4. If the item changes player state, verify save/load and multiplayer sync.
