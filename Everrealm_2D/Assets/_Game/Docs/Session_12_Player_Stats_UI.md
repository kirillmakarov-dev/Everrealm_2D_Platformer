# Session 12 - Player Runtime Stats UI

## Goal

Provide an inspector-authored player stats panel that displays the same runtime values used by movement, jumping, combat, and Skill Tree progression.

## Displayed Values

- Level
- Attack Power
- Defense
- Move Speed
- Attack Speed
- Jump Height in world units

## Runtime Ownership

`CombatStats` is the source of truth for all displayed values. `PlayerStatsPanelPresenter` reads values and never recalculates progression bonuses.

```text
ClassDefinition base stats + CharacterMovementConfig jump data
    -> CombatStats runtime values
    -> SkillTree rank modifiers
    -> CharacterMovementController / CharacterJumpController
    -> PlayerStatsPanelPresenter
```

Move Speed now drives `CharacterMovementController` through `CombatStats.MoveSpeed`. Jump Height is derived from authored jump velocity and effective gravity, then converted back to launch velocity for `CharacterJumpController`. This keeps the displayed height and actual physics behavior synchronized.

Level currently starts at one and can be changed through `CombatStats.SetLevel`. XP and level-up rules are intentionally deferred.

## Skill Tree Integration

`SkillTreeRankEffectType` supports flat `MoveSpeed` and `JumpHeight` bonuses in addition to Attack Power, Defense, and Attack Speed. Removing or resetting ranks removes the same modifier source, so the panel and gameplay return to their base values together.

## UI Ownership

- Runtime presenter: `Assets/_Game/Scripts/UI/Hud/PlayerStatsPanelPresenter.cs`
- Default prefab: `Assets/_Game/Prefabs/UI/PlayerStatsPanel.prefab`
- Dedicated scene canvas: `PlayerStatsCanvas`
- Editor setup: `Assets/_Game/Scripts/Editor/PlayerStatsSetupBuilder.cs`

The panel is visible by default for playtesting and toggles with `P`. Its labels and value fields are serialized prefab references; runtime code does not construct controls.

## Unity Commands

```text
Everrealm/Rebuild Default Player Stats UI Prefab
Everrealm/Setup Player Stats UI
```

These commands own only the Player Stats prefab, canvas, panel, and player references. They do not rebuild Inventory, Skill Tree, Currency HUD, Shop UI, or Skill Bar.

## Verification

- Runtime, Editor, and EditMode test projects compile.
- Regression tests cover level clamping, jump-height conversion, movement modifiers, jump modifiers, and modifier removal.
