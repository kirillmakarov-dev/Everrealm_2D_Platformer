# Session 09 - Hit Reaction, Knockback, and Invulnerability

## Goal

Add basic combat feedback when a character receives damage.

## Runtime behavior

- Enemies receive knockback when the player damages them.
- The player receives knockback when an enemy damages them.
- After taking damage, the player becomes temporarily invulnerable.
- While invulnerable, the player blinks and ignores additional incoming damage.

## Components

- `CharacterHitReaction2D` applies Rigidbody2D knockback, temporary movement lock, invulnerability, and renderer blinking.
- `PlayerClassController` starts player knockback and invulnerability in `ReceiveDamage`.
- `DummyEnemy2D` starts enemy knockback in `ReceiveDamage` when the enemy survives the hit.
- `CharacterRoot` pauses player movement control during hit reaction movement lock.
- `EnemyPatrolAI2D` pauses AI movement control during hit reaction movement lock.

## Tuning

Player settings are on `PlayerClassController`:

- `Damage Knockback Horizontal`
- `Damage Knockback Vertical`
- `Damage Movement Lock Duration`
- `Invulnerability Duration`

Enemy settings are on `DummyEnemy2D`:

- `Hit Knockback Horizontal`
- `Hit Knockback Vertical`
- `Hit Movement Lock Duration`

Shared blink/movement-lock settings are on `CharacterHitReaction2D`.

## Notes

`CombatService` still only calculates damage. The target object decides how to present the hit reaction when it receives `DamageResult`.
