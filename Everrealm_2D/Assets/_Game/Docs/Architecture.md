# Everrealm - High-Level Architecture

This document is the primary architecture reference for Everrealm. Future code changes should follow these standards unless a more specific architecture document explicitly extends or overrides them for a focused system.

The project uses a data-driven Unity architecture built around small MonoBehaviour adapters, plain C# runtime services, ScriptableObject definitions, explicit dependencies, and clear separation between gameplay logic, presentation, physics, UI, and editor tooling.

## Architecture Source Of Truth

This file must remain the central architecture source of truth for the project.

Every approved development session that changes the project architecture, adds a new system, or establishes a new coding standard must be reflected here. Session-specific documents can still exist for detailed implementation notes, but the durable architectural decision must be summarized in this file so future work can reference one consistent architecture guide.

When a new session is approved:

1. Add or update the relevant architecture section in this file.
2. Keep the summary high-level and durable.
3. Link the decision to real project systems, classes, assets, or folders.
4. Avoid duplicating low-level implementation steps that belong in session notes.
5. Do not leave old architecture guidance that contradicts the approved session.

## Architectural Goals

### Environment parallax

`Environment/ParallaxLayer2D` moves explicitly assigned decorative segments relative
to an assigned camera in LateUpdate, after camera tracking. Horizontal wrapping
reuses scene-authored segments; no gameplay objects are spawned or mutated.
Camera follow factors and repetition width are authored per layer. Texture scale
is authored independently of screen coverage to preserve native image detail.

### Enemy sprite presentation

`EnemySpriteAnimator` lives on a prefab's visual child and reads the existing
`DummyEnemy2D` health/death state and Rigidbody2D speed. It listens to
`EnemyAttackController2D.AttackPerformed` for attack presentation only; damage
still executes through the existing CombatService path. The visual child remains
active during the root's death cleanup so the non-looping death clip can finish.
`GolemEnemy.prefab` uses this adapter with explicit references and the five clips
in `Assets/Anim enemy/Golem Setup`; the original source clips remain unchanged.

### Player jump and fall

CharacterRoot samples movement once per physics step, after CharacterGroundDetector.
Input queues a jump in CharacterJumpController; it owns the coyote window, input
buffer and single-impulse guard. Only a successful jump notifies CharacterStateMachine.
Ground detection is suppressed during takeoff ascent so the old ground overlap
cannot cancel Jump. The state machine holds the jump pose through the apex, enters
Fall after the configured descent distance, and clears jump intent on landing.
A ledge departure does not create Jump. CharacterAnimationController presents these
states; the Animator only blends clips. Stats supply effective jump velocity, while
DefaultMovement authors base force, fall distance and input grace windows.

The layered player Animator reads `LocomotionState` independently of the attack
`CharacterState`, so jumps, landings and `MoveSpeed` continue updating during
upper-body shooting. Ground transitions have no clip exit-time requirement.
The skeleton keeps its solid collider during death, has no death launch impulse,
and remains for five seconds so its 3.5-second death clip can finish on the floor.

### Player experience and levels

LevelProgressionDefinition authors cumulative XP thresholds (level 1 starts at 0,
level 2 at 150, level 3 at 300). LevelProgressionState owns total XP and derives
the level and progress within it. PlayerLevelProgression synchronizes CombatStats,
notifies Skill Tree and emits Changed for autosave. PlayerSaveController stores
total XP and migrates older level-only saves to that level's threshold.
CombatService awards IExperienceReward only on a living-to-dead transition to the
attacker implementing IExperienceRecipient. DummyEnemy2D exposes Experience Reward
per monster; PlayerClassController forwards it to the player progression adapter.
PlayerVitalsHudPresenter only displays level/XP using the existing Soft Kitty strip.
The last authored threshold is the level cap; excess XP is retained and the bar is full.

### Audio presentation

`Audio/SoundManager` is a scene-level presentation adapter with explicit music,
combat SFX and UI sources routed through the authored `EverrealmAudio` mixer.
Player combat reports successful projectile launches and hits to this adapter without
changing damage resolution. `UiButtonSound` handles pointer and submit feedback for
authored buttons. Pause Settings changes the exposed Master, Music and SFX mixer
parameters and persists normalized user preferences through `PlayerPrefs`.

- Keep gameplay code understandable, testable, and easy to extend.
- Avoid global state and hidden dependencies. Do not introduce gameplay singletons.
- Keep ScriptableObjects as authoring-time data, not runtime state containers.
- Prefer explicit composition through scene references, constructors, interfaces, and serialized fields.
- Keep Unity-specific APIs at the edges of the system.
- Make combat, skills, effects, movement, feedback, and UI independent enough to evolve separately.
- Make the code suitable for teaching: responsibilities must be visible, names must be clear, and complex logic must explain its intent.
- Keep the project ready for future server-authoritative or multiplayer refactoring by routing gameplay decisions through services and data contracts.

## Core Principles

### No Gameplay Singletons

Gameplay systems must not depend on global singletons such as `GameManager.Instance`, `CombatManager.Instance`, or `ServiceLocator.Instance`.

Use these alternatives instead:

- scene composition roots such as `CharacterRoot`;
- serialized references for Unity components;
- constructor injection for plain C# services;
- interfaces such as `ICombatService`, `ITargetProvider`, `IDamageable`, and `ICombatActor`;
- ScriptableObject definitions for read-only configuration.

Scene-level managers are allowed only for presentation or pooling when they do not own gameplay decisions. Even then, prefer explicit references when practical.

### SOLID Standards

- Single Responsibility: each component owns one reason to change. Example: `CharacterMotor2D` adapts Rigidbody2D, while `CharacterMovementController` owns movement rules.
- Open/Closed: new skills, passives, impact profiles, combo steps, and class definitions should be added through data or polymorphic effect definitions where possible.
- Liskov Substitution: implementations behind interfaces must preserve the expectations of the interface. For example, any `ITargetProvider` must return valid `IDamageable` targets without changing combat math.
- Interface Segregation: keep contracts small. Prefer `IGroundDetector`, `ICharacterMotor2D`, `IFacingController`, and `IAnimationController` over one large character interface.
- Dependency Inversion: gameplay services depend on abstractions and data contracts, not on concrete Unity scene objects.

### Data-Driven Runtime

ScriptableObjects define what something is:

- `ClassDefinition` defines class type, display name, base stats, and starting skills.
- `SkillDefinition` defines UI data, gameplay values, cooldowns, mana cost, effect, and impact profile.
- `AutoAttackComboDefinition` defines combo timing and per-step modifiers.
- `EnemyAttackDefinition` defines enemy attack data.
- `AttackImpactProfile` defines hit presentation.
- `CharacterMovementConfig` defines movement tuning.

Runtime state must live in runtime objects and services:

- `CombatStats`;
- `SkillRuntimeState`;
- `BuffService`;
- `PassiveService`;
- `EmpowerState`;
- `AutoAttackService`;
- `CharacterRuntime`;
- state machines and controllers.

Do not write mutable runtime state back into ScriptableObject assets.

## Project Layers

The current project is organized around these layers:

```text
Assets/_Game/Data
    Authoring data: classes, skills, attacks, combos, effects, feedback, UI assets, item and skill databases

Assets/_Game/Scripts/Core
    Shared enums, identifiers, and generic infrastructure

Assets/_Game/Scripts/Characters
    Character composition, movement, facing, animation, hit reaction, player combat bridge, enemy behavior

Assets/_Game/Scripts/Combat
    Damage contracts, targeting contracts, auto attacks, combos, combat resolution

Assets/_Game/Scripts/Skills
    Skill definitions, skill runtime state, skill execution service

Assets/_Game/Scripts/Effects
    Skill effects, buffs, passives, empower, auto-attack modifiers

Assets/_Game/Scripts/Items
    Item definitions, item database, runtime item stacks, inventory slots, player inventory adapter, and inventory rules

Assets/_Game/Scripts/Loot
    Loot tables, monster loot adapters, loot rolls, drop service, and pickup behavior

Assets/_Game/Scripts/Economy
    Currency wallet, shop transactions, selling rules, and future progression currency

Assets/_Game/Scripts/SkillTree
    Skill tree definitions, ranked progress, unlock/upgrade validation, and player skill-tree bridge

Assets/_Game/Scripts/Save
    Save DTOs, id-based capture/restore, and file persistence adapters

Assets/_Game/Scripts/Stats
    Runtime combat stats and stat mutation rules

Assets/_Game/Scripts/Feedback
    Floating damage text, impact profiles, impact VFX pooling

Assets/_Game/Scripts/Infrastructure
    Unity adapters for external services, currently Physics2D targeting

Assets/_Game/Scripts/UI
    HUD, inventory, skill tree, and skill bar presentation

Assets/_Game/Scripts/Editor
    Setup and build tools for project data and scene wiring
```

Dependencies should flow inward toward stable gameplay contracts:

```text
Unity Input / UI / Physics / Animator / VFX
    -> MonoBehaviour adapters
    -> Plain C# controllers and services
    -> Data contracts and ScriptableObject definitions
```

The reverse direction should be avoided. Combat code should not know about UI widgets, Animator parameters, pooled popup prefabs, or concrete scene layout.

## Main Gameplay Pipeline

The primary player combat flow is:

```text
Input command
    -> CharacterInputRouter
    -> CharacterRoot
    -> PlayerClassController
    -> SkillService / AutoAttackService
    -> BuffService / PassiveService / EmpowerState / AutoAttackComboDefinition
    -> ITargetProvider
    -> DamageRequest
    -> CombatService
    -> DamageResult
    -> IDamageable.ReceiveDamage
    -> Feedback presentation
```

Important boundary: combat calculates the result; feedback displays the result. Floating text, hit VFX, blinking, and knockback must not become the source of combat truth.

## Character Architecture

`CharacterRoot` is the character composition root. It wires together input, movement, facing, animation, state, hit reaction, and player combat. It coordinates systems, but it should not own combat formulas or low-level movement implementation details.

Current responsibilities:

- subscribe to `CharacterInputRouter` events;
- create plain C# movement, jump, facing, and state-machine controllers;
- update `CharacterRuntime`;
- forward attack and skill intentions to `PlayerClassController`;
- route high-level state changes to `CharacterAnimationController`;
- keep runtime-facing direction synchronized with combat.

Character components:

- `CharacterInputRouter`: reads player input and publishes commands.
- `CharacterMotor2D`: adapts Rigidbody2D.
- `CharacterGroundDetector`: performs ground checks.
- `CharacterMovementController`: owns acceleration, deceleration, and air-control rules.
- `CharacterJumpController`: validates and applies jumps.
- `CharacterFacingController`: owns logical facing.
- `CharacterFacingView2D`: applies visual facing.
- `CharacterAnimationController`: maps character state to Animator commands.
- `CharacterStateMachine`: selects Idle, Run, Jump, Fall, and Attack.
- `CharacterRuntime`: stores current velocity, grounded state, facing, and movement input.
- `CharacterHitReaction2D`: owns local knockback, movement lock, invulnerability, and blink presentation.

Character pipeline:

```text
CharacterInputRouter
    -> CharacterRoot
        -> CharacterMovementController -> ICharacterMotor2D -> Rigidbody2D
        -> CharacterJumpController -> IGroundDetector + ICharacterMotor2D
        -> CharacterFacingController -> ICharacterFacingView
        -> PlayerClassController -> Combat / Skills / Buffs / Passives
        -> CharacterRuntime -> CharacterStateMachine -> IAnimationController
```

## Combat Architecture

Combat is based on explicit requests and results:

- `DamageRequest` describes who attacks, who is targeted, base damage, damage lines, tags, critical parameters, impact profile, and attack direction.
- `CombatService` validates the request and calculates final damage.
- `DamageResult` describes the outcome and is passed to the target.
- `IDamageable` receives damage.
- `ICombatActor` extends `IDamageable` with transform and class identity.

Combat rules:

- All damage must go through `ICombatService.ApplyDamage`.
- Targets must be selected through `ITargetProvider`.
- Presentation must react to `DamageResult`, not recalculate damage.
- New damage rules should be introduced through data, modifiers, or focused services.
- Do not read input, UI state, Animator state, or VFX objects from combat services.

## Auto-Attack And Combo Architecture

`AutoAttackService` owns auto-attack execution for a combat actor. It combines:

- base auto-attack profile;
- passive modifications;
- buff impact overrides;
- empower modifications;
- combo-step modifications;
- target selection;
- damage request creation.

`AutoAttackComboDefinition` is a data-only asset. It defines:

- combo id;
- reset time;
- ordered combo steps;
- per-step display name;
- damage multiplier;
- critical chance;
- critical damage multiplier;
- optional impact profile.

The combo advances only when an attack finds at least one valid target. Empty attacks do not consume the next combo step.

When multiple visual impact profiles compete, use this priority:

1. Empower impact profile for empowered hits.
2. Buff impact override when active.
3. Combo step impact profile.
4. Default auto-attack impact profile.

Combat values and presentation profile selection should remain explicit in `AutoAttackService`.

## Skills, Effects, Buffs, And Passives

`SkillService` owns skill registration, mana validation, cooldowns, duration state, and effect execution.

Skill flow:

```text
SkillDefinition
    -> SkillService.TryUse
    -> SkillContext
    -> SkillEffectDefinition.Apply
    -> Combat / Buff / Passive / Empower / AutoAttack services
```

Rules:

- `SkillDefinition` stores data only.
- `SkillRuntimeState` stores cooldown and duration runtime values.
- `SkillEffectDefinition` contains effect behavior.
- Passive effects must create runtime objects through `PassiveSkillEffectDefinition.CreateRuntime`.
- Passive runtime behavior belongs in `IPassiveEffect`.
- Buff runtime behavior belongs in `BuffService`.
- Empower state belongs in `EmpowerState` and is consumed by the next valid auto attack.

This keeps skill data, runtime state, and effect execution separate.

## Targeting And Infrastructure

`ITargetProvider` is the combat-facing targeting contract. The current implementation, `Physics2DTargetProvider`, adapts Unity Physics2D overlap queries into combat targets.

Rules:

- Combat services should depend on `ITargetProvider`, not directly on `Physics2D`.
- Physics implementation details belong in `Infrastructure`.
- Targeting results should be deduplicated and filtered before combat receives them.
- Targeting must not apply damage.

This boundary allows later replacement with predictive targeting, grid targeting, server-authoritative targeting, or tests.

## Feedback Architecture

Feedback is intentionally separated from combat resolution.

Current responsibilities:

- `AttackImpactProfile`: data for hit presentation.
- `FloatingDamageTextController`: receives `DamageResult` and asks presentation systems to show feedback.
- `FloatingDamageTextManager`: pools floating text popups.
- `FloatingDamageTextPopup`: animates a single text instance.
- `ImpactEffectPool`: pools hit effects.
- `ImpactEffectInstance`: controls one spawned impact effect.
- `PlayerFeedbackVfx`: plays authored one-shot prefabs for level-up and successful consumable restoration.
- `LevelUpBannerView`: animates the authored screen-space banner nested inside the level-up VFX prefab.

Rules:

- Feedback must not calculate final damage.
- Feedback must not mutate gameplay state.
- Combat can provide enough data for feedback through `DamageResult` and profile references.
- Pooling should stay local to presentation systems.
- Progress restoration must not present a level-up celebration; `PlayerLevelProgression.LevelIncreased`
  is published only by live level gains and explicit level increases, not by save restoration.

## UI Architecture

UI is presentation over runtime gameplay state.

Current skill bar flow:

```text
PlayerSkillLoadout
    -> SkillSlotBinding
    -> SkillBarPresenter
    -> SkillSlotViewModel
    -> SkillSlotView
```

Current vitals HUD flow:

```text
PlayerClassController.Stats
    -> PlayerVitalsHudPresenter
    -> VitalsOrbView
```

Current player stats panel flow:

```text
ClassDefinition + CharacterMovementConfig
    -> CombatStats
    -> Skill Tree runtime modifiers
    -> movement / jump / combat gameplay
    -> PlayerStatsPanelPresenter
```

Rules:

- UI must not own combat truth.
- UI reads runtime state from gameplay components and services.
- UI sends player intentions such as "use this skill", not direct stat mutations.
- UI should use view models for display state when the view needs combined data.
- Player stat UI must read the same effective runtime values used by gameplay. Move Speed and Jump Height presentation must never use separate copied tuning values.
- Designer-facing UI structure belongs in scene objects and prefabs. Runtime presenters may instantiate configured views and bind data, but should not construct editable controls in Play Mode.
- Destructive or economy-spending actions must use explicit command controls. Informational card surfaces should not trigger purchases when nearby controls perform selection or assignment.
- Large graph interfaces should scroll one shared content transform so nodes and their connection visuals cannot drift apart.
- Skill-tree maps derive horizontal depth from prerequisite relationships: roots appear left, dependents appear right, and peers at the same depth distribute vertically. Inspector-authored graph spacing controls density, while connection geometry is rebuilt only after final node positioning.
- Required view prefabs such as `SkillTreeConnection.prefab` must be explicit serialized references created and wired by editor setup tools. Runtime code should not silently fabricate a replacement.
- Every attachable Unity component must live in a matching script file so Unity can serialize a stable MonoScript reference.
- Feature setup builders have strict ownership boundaries. Skill Tree setup may update only Skill Tree canvas/window/data wiring and must never invoke inventory setup, rewrite Currency HUD, alter shop UI, or retune wallet starting values.

Skill-tree progression is a visual graph over runtime rank state:

```text
SkillTreeDefinition nodes (id, position, prerequisites, max rank)
    -> SkillTreeService validates and purchases one rank
    -> SkillTreeProgress stores nodeId -> rank
    -> PlayerSaveController persists stable ids and ranks
    -> SkillTreeWindowPresenter renders node and connection prefabs
```

Node rank state must remain outside ScriptableObject assets. Unlock-skill actions run only on the first rank. Rank effects are aggregated from `SkillTreeRankEffectDefinition` data and applied to runtime `CombatStats` and `SkillService` modifiers; they must never mutate shared `SkillDefinition` data. UI affordability and cooldown presentation must use effective runtime skill values rather than base asset values.

Skill-tree assets are authored content and must be validated before playtesting. Validation covers stable unique ids, prerequisite existence and acyclic graphs, required skill references, effect targets, and legal rank ranges. General scene setup must preserve manually authored node arrays; replacing demo content requires a separate explicit editor command.

Profession-controlled skills require a purchased node even when listed in a class's starting skills or an older saved loadout. Startup registration, skill-bar resolution, and casting enforce this ownership rule. Restoring or resetting purchases rebuilds learned runtime skills; non-tree starting skills remain available.

`Tools > Everrealm > Save Inspector` edits the live player in Play Mode through `PlayerSaveController`: Buy follows ordinary requirements; Grant + Parents records the selected branch without spending coins or requiring a level. Both persist native profession/node identifiers. The editor does not mutate definition assets or use a second save format. Purchase autosave waits for the final progression commit instead of intermediate wallet or loadout notifications.

The Skill Tree authoring preview uses the runtime node prefab's frame/background sprites, a 140x150 card scaled to 75%, and a fixed 900x650 reference viewport for graph spacing. Editor window resizing must not stretch the coordinate scale or change authored node positions.

Progress autosave must be transaction-oriented. High-level systems publish commit events only after all currency, inventory, rank, learned-skill, and loadout mutations succeed. `PlayerSaveController` listens to those commit events and suppresses autosave while restoring or resetting state; it must not persist intermediate wallet or inventory notifications.

Skill-tree respec is also transactional. Validation must run before rank mutation or refunds: the last rank cannot be removed while unlocked dependents exist, and optional material refunds require sufficient inventory capacity. A successful respec updates rank, currency, materials, learned skills, loadout, runtime modifiers, and then publishes one progression commit for autosave.

A dedicated UI architecture document should expand these standards later. When that document is approved, its durable high-level decisions must also be summarized here.

## Enemy Architecture

Current enemy behavior is intentionally simple and scene-driven:

```text
EnemyPatrolAI2D
    -> patrol/chase/stop logic
    -> target detection
    -> EnemyAttackController2D.TryAttack
    -> CombatService
    -> target IDamageable.ReceiveDamage
```

Rules:

- AI owns decisions and movement intent.
- Attack controllers own attack timing and hit execution.
- Combat still goes through `CombatService`.
- Enemy stats and damage reception must remain separate from player-only classes.
- Shared combat concepts should use `ICombatActor`, `IDamageable`, and `ICombatService`.

As enemies become more complex, prefer explicit components for vision, movement, attack selection, and state instead of growing one large AI class.

## Inventory, Loot, And Economy Architecture

Detailed plan: `Assets/_Game/Docs/Session_10_Inventory_Loot_Economy_Plan.md`.

The loot pipeline keeps monster drops, item storage, money, shops, and progression separate:

```text
Enemy death
    -> MonsterLoot
    -> LootDropService
    -> LootTable roll
        -> coins -> CurrencyWallet
        -> item stacks -> Inventory
```

Rules:

- Monsters own loot table references, not item behavior, shop rules, or inventory logic.
- Coins are currency and should go directly into `CurrencyWallet` when collected.
- Items, weapons, materials, consumables, and other objects should go into `Inventory` as `ItemStack` runtime data.
- `ItemDefinition` assets describe item data; runtime quantity belongs in `ItemStack`.
- `LootTable` assets describe possible drops; runtime roll results belong in `LootRoll`.
- `Inventory` should merge stacks, enforce capacity, and expose read-only slot state to UI.
- `ShopService` owns both shop transactions: selling removes items from `Inventory` and adds gold to `CurrencyWallet`, while buying validates a `ShopCatalogDefinition`, spends gold, and adds the configured item to `Inventory` transactionally.
- Future skill-tree unlocks should spend money from `CurrencyWallet` and optional materials from `Inventory`, not depend on loot generation directly.
- Random loot rolls should use an injectable random source so drop behavior can be tested deterministically.

World rewards are spawned separately for each rolled coin bundle or item row. `LootDropService`
gives every spawned pickup an independent horizontal offset and vertical launch speed;
`LootPickup2D` keeps a dynamic Rigidbody2D and a solid landing collider alongside its trigger
collection collider, so rewards fall onto platforms and remain there until collected.

The world shop is an authored scene object with a trigger interaction zone. `CharacterInputRouter`
publishes the Interact command on `I`; `ShopInteraction2D` presents an on-screen interaction hint,
using an authored World Space Canvas child on the shop object, and opens the existing
`ShopWindowPresenter` and `InventoryWindowPresenter` together, while
`ShopService` remains the only owner of shop transactions. The shop window uses authored Buy
and Sell tab buttons from `ShopWindow.prefab`; `ShopCatalogDefinition` is the ScriptableObject
source of purchasable items/prices and accepted sellable items, while the Sell tab is populated
from the player's matching inventory. Runtime presenters only bind state to these authored
views and never build visible UI controls. Each `ShopInteraction2D` owns its
`ShopCatalogDefinition` and creates the transaction service for that shop; the shared shop UI
receives the active shop context when opened, so different world shops can expose different
buy and sell assortments without duplicating the canvas prefab.
Shop row actions are nested instances of the shared `ShopActionButton.prefab`; visual state,
font, and button colors are authored once there and inherited by every Buy and Sell row.
`ShopWindow.prefab` stores one serialized `ShopItemRow.prefab` reference instead of authored row
copies; its presenter instantiates and reuses only the number of rows required by the active shop.

Consumables remain `ItemDefinition` data with `healthRestore` and `manaRestore` values.
`LootPickup2D` places them in the normal runtime `Inventory`; `SkillBarPresenter` accepts a
dragged `InventorySlotView` item into an authored skill slot, renders the current inventory
quantity, and consumes one item only after `PlayerClassController` applies its configured effect.
An empty stack clears the consumable from that bar slot.

Initial implementation files:

- `ItemDefinition`, `ItemStack`, `InventorySlot`, `Inventory`, `InventoryAddResult`, and `PlayerInventory`.
- `LootTable`, `LootEntry`, `LootRoll`, `LootDropService`, `LootPickup2D`, and `MonsterLoot`.
- `CurrencyWallet`, `ShopService`, and `SellResult`.
- `DummyEnemy2D` triggers `MonsterLoot.DropLoot` when death is confirmed.

## Editor Tooling

Editor builder scripts in `Assets/_Game/Scripts/Editor` are setup tools. They may create assets, configure prefabs, and wire scene objects, but they must not become runtime dependencies.

Rules:

- Runtime systems must work without calling editor builders.
- Builders should be idempotent where practical.
- Generated assets should follow the same data/runtime separation as manually created assets.
- Editor code must stay inside editor-only folders or assemblies.

## Code Style And Comment Standards

### General Code

- Prefer clear names over explanatory comments.
- Use comments when they explain intent, tradeoffs, constraints, or non-obvious behavior.
- Avoid comments that repeat the line of code.
- Keep methods small enough that a reader can understand the flow without scrolling through unrelated responsibilities.
- Validate required serialized dependencies in `Awake` or setup methods and log clear errors.
- Prefer inspector-visible configuration for designer-facing values.
- Avoid hidden runtime magic that silently changes scene structure unless it is explicitly a setup or editor tool.

### Complex Math And Gameplay Formulas

Any difficult mathematical function or gameplay formula must include comments before the method and inside the method where the reasoning is non-obvious.

Required comment pattern:

- Before the method: explain what the formula is trying to achieve in gameplay terms.
- Before non-trivial branches: explain why the branch exists.
- Near clamping, normalization, interpolation, randomness, coordinate transforms, or physics queries: explain what problem the operation prevents.
- For tuned constants: explain the gameplay reason, or move the value into serialized data.

Example standard:

```csharp
// Computes the launch velocity needed to reach a target height while preserving
// readable platformer timing. Gravity is taken from the movement config so jump
// tuning stays data-driven instead of being hidden in the formula.
private float CalculateJumpVelocity(float targetHeight, float gravity)
{
    // The kinematic formula uses a positive magnitude here because Rigidbody2D
    // receives the upward velocity directly, while gravity is applied separately.
    var safeGravity = Mathf.Max(0.01f, Mathf.Abs(gravity));

    // Clamp height so invalid designer data cannot produce NaN or a negative jump.
    var safeHeight = Mathf.Max(0f, targetHeight);
    return Mathf.Sqrt(2f * safeGravity * safeHeight);
}
```

This rule is especially important for:

- combat formulas;
- critical hit and probability logic;
- movement acceleration/deceleration;
- knockback and hit reaction;
- physics targeting shapes;
- camera or coordinate transformations;
- interpolation, smoothing, and easing;
- procedural placement or random selection.

## Testing And Verification Standards

For code changes:

- Run `dotnet build Assembly-CSharp.csproj` when script changes are made.
- Use targeted searches to verify renamed classes, serialized references, and architectural boundaries.
- For Unity components, check relevant prefabs, scenes, and ScriptableObject assets when serialized fields change.
- For gameplay systems, verify the full pipeline, not just the edited class.

For architecture changes:

- Update this document when a new high-level rule is introduced.
- Add focused documents for major systems such as UI, enemy AI, content pipeline, or save/load.
- Do not leave code and documentation saying different things.

## Future Extension Rules

When adding a new system:

1. Define the runtime responsibility.
2. Decide whether the system is data, service, adapter, presentation, or editor tooling.
3. Keep runtime state out of ScriptableObjects.
4. Add small interfaces only where they create a real boundary.
5. Keep Unity API usage near MonoBehaviours and infrastructure adapters.
6. Route combat-affecting behavior through combat contracts.
7. Add comments for hard math and non-obvious gameplay formulas.
8. Verify the system from input/data through runtime behavior to presentation.
9. Summarize the approved durable architecture decision in this file.

This architecture favors explicit, readable, data-driven Unity code over clever hidden systems. New work should make the next developer faster, not merely make the current feature pass.
