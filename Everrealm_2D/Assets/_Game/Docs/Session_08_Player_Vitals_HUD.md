# Session 08 - Player Vitals HUD

## Goal

Add a Diablo-style player vitals display with circular health and mana orbs.

## Runtime behavior

- `PlayerVitalsHudPresenter` reads `PlayerClassController.Stats`.
- The left orb shows current health over max health.
- The right orb shows current mana over max mana.
- `VitalsOrbView` updates a filled circular image and optional numeric text.

## Setup

Run `Everrealm/Setup Skill Bar UI`.

The setup creates or refreshes:

- `SkillBarCanvas`
- `VitalsHud`
- `HealthOrb`
- `ManaOrb`
- generated orb sprites under `Assets/_Game/Data/UI/Hud`

## Notes

- The HUD is intentionally on the same canvas as the skill bar so bottom combat UI stays grouped.
- The generated orb sprites are placeholders; final art can replace them without changing presenter logic.
- The fill is UI-driven and reads runtime stats only; combat still owns health and mana changes.
