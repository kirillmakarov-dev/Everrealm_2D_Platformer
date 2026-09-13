# Session 02 — Character Framework

## Result

This session created a reusable character framework for Warrior, Ninja, and future classes.

The existing combat/class/skill core was not rewritten. `PlayerClassController` remains the class/combat module, while the new `CharacterRoot` connects it to universal movement, jump, facing, state, and animation systems.

## New systems

- data-driven `CharacterMovementConfig`;
- per-character `CharacterRuntime`;
- replaceable `ICharacterMotor2D`;
- Rigidbody2D motor adapter;
- plain C# movement, jump, and facing controllers;
- Physics2D ground detector;
- universal Idle / Run / Jump / Fall / Attack state machine;
- animation adapter with high-level commands;
- Input System command router;
- `CharacterRoot` composition root;
- debug scene with movement, jumping, and Session 01 combat.

## Interaction model

`CharacterInputRouter` publishes commands:

- move;
- jump;
- attack;
- skill slot.

`CharacterRoot` subscribes to those commands and routes them to the appropriate controller. It does not calculate combat or movement by itself.

Movement is calculated by `CharacterMovementController` and applied through `ICharacterMotor2D`.

Jumping uses only the ground detector and motor.

Facing is synchronized with movement input and passed to the combat module.

Runtime state feeds the state machine, and state changes call high-level animation commands.

Attack and skill commands are delegated to `PlayerClassController`.

## Dependencies

- `CharacterRoot` depends on character interfaces, movement config, and the combat module.
- `CharacterMovementController` depends on `ICharacterMotor2D` and `IGroundDetector`.
- `CharacterJumpController` depends on `ICharacterMotor2D` and `IGroundDetector`.
- `CharacterStateMachine` depends on `CharacterRuntime`.
- Unity adapters depend on Rigidbody2D, Physics2D, Animator, and Input System.

No reverse dependencies were added from the combat core to input, animation, or Rigidbody2D.

## Pipeline

```text
Input
→ CharacterInputRouter
→ CharacterRoot wiring
→ movement / jump / facing controllers
→ Unity adapters
→ CharacterRuntime
→ CharacterStateMachine
→ Animation adapter
→ existing PlayerClassController
→ combat pipeline
```

## Testing

Open:

```text
Assets/_Game/Scenes/CharacterFrameworkDebug.unity
```

Controls:

- `A/D` — move by default;
- `Space` — jump by default;
- `J` — auto attack by default;
- `U`, `I`, `O`, `P` — skill slots 1–4 by default.

These keys are serialized on `CharacterInputRouter`, so they can be changed per player prefab or scene object in the Inspector without editing code.

The scene contains a platform, a Warrior player, and dummy enemies.

If assets were not created automatically, use:

```text
Everrealm/Build Character Framework Sample
```

## Presentation setup

The root character object owns gameplay components.

A child `Visual` object should own:

- `SpriteRenderer`;
- `Animator`.

Assign the child Animator to `CharacterAnimationController`.

Assign the visual root transform to `CharacterFacingView2D`.

Expected Animator parameters:

- `CharacterState` — int;
- `MoveSpeed` — float.

State IDs:

- `0` — Idle;
- `1` — Run;
- `2` — Jump;
- `3` — Fall;
- `4` — Attack.

## Extension points

- replace `ICharacterMotor2D` with a predicted/network motor;
- replace `IGroundDetector` with custom casts or server queries;
- replace `CharacterInputRouter` with Input Actions, AI, or network commands;
- extend the state machine with dash, hit reaction, death, cast, or dodge states;
- add coyote time, jump buffer, double jump, or wall jump;
- connect animation events without changing combat.

## Intentionally not implemented in Session 02

- dash;
- double jump;
- wall jump;
- coyote time;
- jump buffer;
- knockback;
- hit reaction;
- root motion;
- animation events;
- projectile visuals;
- enemy AI;
- networking;
- inventory;
- save system.

## Recommended next steps

Add tests, fixed simulation timing, combat command/event streams, and presentation timing for attacks.
