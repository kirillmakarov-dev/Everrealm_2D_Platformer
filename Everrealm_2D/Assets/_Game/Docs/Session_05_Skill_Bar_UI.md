# Session 05 — Skill Bar UI

## Goal

This session added the first working skill UI.

Features:

- four skill slots on screen;
- skill icon display;
- `U/I/O/P` key labels;
- dark cooldown overlay;
- cooldown countdown text;
- mouse click activation;
- loadout-based skill assignment;
- temporary generated icons for visibility.

## Architecture

```text
SkillDefinition
→ skill data, icon, short name, input label

PlayerSkillLoadout
→ which skills are assigned to slots

SkillBarPresenter
→ connects PlayerClassController / SkillService to UI

SkillSlotView
→ displays one slot and reports clicks
```

The UI does not calculate cooldowns. It reads `SkillRuntimeState` from `SkillService`.

## New runtime files

- `Assets/_Game/Scripts/UI/Skills/SkillSlotBinding.cs`  
  Describes one slot: index, skill, input label.

- `Assets/_Game/Scripts/UI/Skills/PlayerSkillLoadout.cs`  
  Component on the player. Stores slot bindings. If the list is empty, it can auto-fill from `PlayerClassController.UsableSkills`.

- `Assets/_Game/Scripts/UI/Skills/SkillSlotViewModel.cs`  
  View model for one UI slot.

- `Assets/_Game/Scripts/UI/Skills/SkillSlotView.cs`  
  UI view for one slot: icon, cooldown overlay, cooldown text, key text, name text, and button.

- `Assets/_Game/Scripts/UI/Skills/SkillBarPresenter.cs`  
  Presenter that finds the player, builds slots, updates cooldowns, and calls `UseSkill(skillId)`.

## Existing system changes

### SkillDefinition

Added UI fields:

- `Icon`;
- `Short Name`;
- `Input Label`.

### PlayerClassController

Added methods:

- `UseSkill(string skillId)`;
- `TryGetSkillRuntimeState(string skillId, out SkillRuntimeState state)`.

This lets UI slots bind to a specific skill ID instead of a fragile list index.

## Editor setup

Added:

- `Assets/_Game/Scripts/Editor/SkillBarSetupBuilder.cs`

Menu:

```text
Everrealm/Setup Skill Bar UI
```

The command:

1. creates temporary icon sprites in `Assets/_Game/Data/UI/SkillIcons`;
2. assigns icons to `SkillDefinition` assets;
3. creates `Assets/_Game/Prefabs/UI/SkillSlot.prefab`;
4. creates `SkillBarCanvas` in the current scene;
5. creates an `EventSystem` if needed;
6. adds `PlayerSkillLoadout` to the player;
7. fills slots with usable skills from `ClassDefinition`.

## Testing

1. Stop Play Mode.
2. Wait for Unity compilation.
3. Run:

```text
Everrealm/Setup Skill Bar UI
```

4. Press Play.
5. Use `U/I/O/P` or click slots with the mouse.
6. After activation, the icon should darken and the cooldown overlay should decrease.

## Changing skills in slots

Open the player object and find:

```text
PlayerSkillLoadout
```

Each slot has:

- `Slot Index`;
- `Skill`;
- `Input Label`.

Slot mapping:

```text
Slot 0 → U
Slot 1 → I
Slot 2 → O
Slot 3 → P
```

If the same `SkillDefinition` is assigned to two slots, both slots share the same cooldown because cooldown state is stored per `skillId` in `SkillService`.

If a slot is empty, assign another active, buff, or empower skill. Passive and auto-attack upgrade skills are not meant to be clickable UI skills.

## Changing icons

Open a `SkillDefinition`, for example:

```text
Assets/_Game/Data/Skills/Warrior/FireSpin.asset
```

Replace the `Icon` field with any Sprite.

Temporary generated icons are stored in:

```text
Assets/_Game/Data/UI/SkillIcons
```

They can be replaced later.

## Future improvements

The system is ready for:

- drag and drop;
- locked slots;
- skill trees;
- saved loadouts;
- gamepad navigation;
- visual mana warnings;
- tooltip panels.

## Verification

Verified with:

- `dotnet build Assembly-CSharp.csproj --no-restore`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Both builds passed without errors or warnings.
