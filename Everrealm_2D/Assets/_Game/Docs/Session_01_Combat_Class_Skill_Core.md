# Session 01 — Combat, Class, and Skill Core

## Result

This session created the foundation of the 2D platformer RPG combat system for Warrior, Ninja, and future classes.

The design keeps calculations and runtime state in plain C# classes. MonoBehaviours are used only as Unity scene adapters, composition roots, debug helpers, or integration points.

Implemented systems:

- combat request/result models: `DamageLine`, `DamageRequest`, `DamageResult`;
- contracts: `IDamageable`, `ICombatActor`, `ICombatService`, `ITargetProvider`;
- runtime stats, health, mana, and temporary stat modifiers;
- data-driven `SkillDefinition` and `ClassDefinition`;
- cooldown and duration runtime state;
- mana validation;
- auto attack, empower, buff, passive, and auto-attack upgrade flows;
- attack shapes: Circle, Box, DirectionalBox;
- local Physics2D target provider;
- debug input and dummy enemy;
- initial Warrior and Ninja data assets;
- editor builder for sample assets and a debug scene.

## Main files

- `Scripts/Core/GameplayTypes.cs` — core enums for classes, skill types, damage tags, and attack shapes.
- `Scripts/Stats/CombatStats.cs` — runtime stats and modifiers.
- `Scripts/Combat/CombatModels.cs` — combat contracts and request/result models.
- `Scripts/Combat/CombatService.cs` — damage calculation and application.
- `Scripts/Combat/AutoAttackService.cs` — auto-attack pipeline and empower support.
- `Scripts/Combat/Targeting.cs` — target query contracts and attack shape data.
- `Scripts/Skills/SkillDefinition.cs` — common skill data.
- `Scripts/Skills/SkillService.cs` — skill registration, cooldowns, mana validation, and activation.
- `Scripts/Effects/` — concrete effect definitions for buffs, passives, damage, empower, and auto-attack upgrades.
- `Scripts/Classes/ClassDefinition.cs` — class stats and starting skill list.
- `Scripts/Characters/PlayerClassController.cs` — Unity composition root for the player combat module.
- `Scripts/Infrastructure/Physics2DTargetProvider.cs` — Unity Physics2D target adapter.
- `Scripts/Debug/` — debug input, dummy enemy, and runtime stat helpers.
- `Scripts/Editor/CombatFoundationBuilder.cs` — sample data and scene generation.

## Combat pipeline

1. Input requests an auto attack or skill from `PlayerClassController`.
2. `SkillService` validates registration, caster life, cooldown, and mana.
3. The skill effect applies state changes or creates damage requests.
4. For auto attacks, the base profile is copied first.
5. Passives, auto-attack upgrades, empower, and combo modifiers can alter the attack profile.
6. `ITargetProvider` finds targets without exposing Physics2D to the combat core.
7. A `DamageRequest` is created for each target.
8. `CombatService` calculates damage and returns a `DamageResult`.
9. Empower is consumed after the next successful auto attack.

Auto-attack modification order:

```text
base profile / auto-attack upgrade
→ passives
→ empower
→ combo step
→ target query
→ CombatService
```

## Warrior starting skills

- `Iron Strength` — Empower. The next auto attack can be strengthened.
- `Fire Spin` — Empower. The next auto attack can use an area shape and fire impact.
- `Extreme Focus` — temporary attack power buff.
- `Special Training` — passive periodic healing.
- `Combo Master` — auto-attack upgrade that modifies the basic attack.

## Ninja starting skills

- `Bouncing Star` — active ranged/area foundation attack.
- `Speed Throw` — auto-attack upgrade for ranged-style attacks.
- `Sixth Sense` — temporary attack speed buff.
- `Shadow Training` — passive chance to add an extra damage line to auto attacks.
- `Armor Mastery` — passive defense bonus.

## Creating sample assets and scenes

The editor builder creates:

- `Assets/_Game/Data/Classes/Warrior.asset`;
- `Assets/_Game/Data/Classes/Ninja.asset`;
- skill and effect assets for the starting skills;
- `Assets/_Game/Scenes/CombatFoundationDebug.unity`.

Manual menu:

```text
Letter Hunter/Build Combat Foundation Sample
```

The operation is intended to be idempotent: existing assets are updated instead of duplicated.

## Testing

Open:

```text
Assets/_Game/Scenes/CombatFoundationDebug.unity
```

Controls:

- `J` — auto attack;
- `U` — skill slot 1;
- `I` — skill slot 2;
- `O` — skill slot 3;
- `P` — skill slot 4.

For empower skills, press the skill key first, then press `J` to consume the empower on the next auto attack.

## Adding a new skill

1. Create or choose a `SkillEffectDefinition`.
2. Configure effect-specific values in the effect asset.
3. Create a `SkillDefinition`.
4. Fill in a stable `skillId`, skill type, mana cost, cooldown, duration, and effect reference.
5. Add the skill to a `ClassDefinition` or register it later through a progression system.
6. If the skill needs unique behavior, create a small new `SkillEffectDefinition` subclass. `PlayerClassController` should not need to change.

Runtime passive state must not be stored in ScriptableObjects. Passive effects should create runtime state objects when registered.

## Adding a new class

1. Add a value to `CharacterClassType`.
2. Create a `ClassDefinition`.
3. Configure base stats.
4. Create skill and effect assets.
5. Fill `startingSkills`.
6. Assign the class asset to `PlayerClassController`.

Combat, skills, targeting, and feedback should not need to be rewritten.

## Extension points

- `ICombatService` — server-side damage calculation, resistances, critical rules, combat logs.
- `ITargetProvider` — server spatial query replacement for Physics2D.
- `IRandomSource` — deterministic seeded random source.
- `SkillEffectDefinition` — new active, buff, empower, passive, or upgrade behavior.
- `IPassiveEffect` — event-driven, periodic, or attack-modifying passive behavior.
- `AttackShape` — cone, capsule, ray, chain, or projectile-style queries.
- `DamageTag` — elements, weapon types, PvE/PvP rules, and interactions.
- `DamageRequest` / `DamageResult` — clean boundary for commands, replication, and reconciliation.

## Intentionally not implemented in Session 01

- final UI;
- animations and VFX;
- projectiles;
- enemy AI;
- networking;
- inventory;
- save/load;
- final RPG damage formula;
- production input mapping;
- automated tests.

## Recommended next steps

1. Add EditMode tests for combat services.
2. Introduce a game clock and fixed simulation tick.
3. Split player commands from confirmed combat events.
4. Add presentation events for animation and VFX.
5. Build movement and character framework on top of the combat core.
