# Session 07 - Enemy AI

## Goal

Add the first playable enemy behavior without depending on final player animations.

## Runtime behavior

- Enemies patrol horizontally inside their assigned left/right zone.
- Enemies can only move on the X axis; gravity and the ground platform handle vertical motion.
- When a living player target enters detection range, the enemy chases it.
- Once a target is detected, the enemy keeps chasing until the target leaves `Lose Target Distance`.
- Chase movement can leave the patrol zone; only idle patrol is clamped to `Left Distance` / `Right Distance`.
- When the target is close enough, the enemy stops and asks `EnemyAttackController2D` to resolve the attack.
- Stop distance is measured from collider edges, not only object centers, to avoid pushing the player while closing in.
- The attack direction follows the enemy facing direction, so attacks work when the enemy turns left or right.
- If the target escapes, the enemy drops aggro and returns to horizontal patrol inside its assigned zone.
- When returning from outside the patrol zone, the enemy moves directly back inside without applying edge wait pauses.

## Components

- `EnemyPatrolAI2D` owns patrol, detection, chase movement, facing, and debug gizmos.
- `EnemyAttackController2D` still owns attack range checks and damage application.
- `DummyEnemy2D` owns enemy stats and death handling.
- `PhysicsLayerSetupBuilder` keeps the intended physics layer setup reproducible from `Everrealm/Setup Physics Layers`.

## Physics layers

- `Player` is the player body layer.
- `Ground` is for platforms and level collision.
- `EnemyBody` is the physical enemy body layer.
- `EnemyHurtbox` is reserved for future separate enemy hurtbox triggers.
- `Player` does not physically collide with `EnemyBody`, so the player can pass through enemies.
- `EnemyBody` still collides with ground and keeps its Rigidbody2D-driven movement.
- Player attacks should target `EnemyBody` / `EnemyHurtbox`; enemy AI and enemy attacks should target `Player`.

## Death flow

When an enemy reaches zero HP:

- AI and attack controllers are disabled.
- Colliders are disabled so the defeated enemy stops blocking combat.
- Rigidbody velocity is set upward with a small horizontal push from the incoming hit direction.
- Gravity pulls the enemy down.
- The enemy object is destroyed after a short delay, making it disappear from the visible play area.

## Inspector tuning

`EnemyPatrolAI2D`:

- `Left Distance` / `Right Distance` define the assigned patrol zone from the spawn position.
- `Patrol Speed` controls idle walking.
- `Return To Patrol Speed` controls how fast the enemy moves back into its patrol zone after chasing outside it.
- `Target Layers` limits which physics layers the enemy can detect.
- `Require Player Target` keeps enemy AI focused on objects with `PlayerClassController`, preventing enemies from selecting other enemies on shared layers.
- `Detection Range` controls when the enemy notices the player.
- `Lose Target Distance` controls how far the enemy keeps chasing after it has already noticed the player.
- `Chase Speed` controls pursuit.
- `Attack Stop Distance` should roughly match `EnemyAttackController2D.Attack Range`.
- `Visual Faces Right By Default` should stay disabled for the current snail sprite, because the sprite faces left in the source image.

`DummyEnemy2D`:

- `Death Upward Velocity` controls how high the enemy pops up.
- `Death Horizontal Velocity` controls the sideways push.
- `Death Gravity Scale` controls how fast the enemy falls.
- `Destroy After Death` controls when the object is removed.

## Follow-up

- Add hit reactions and knockback before death.
- Add enemy-specific patrol/chase definitions as ScriptableObjects if enemy variety grows.
- Add animation states later once player and enemy animation assets are ready.
