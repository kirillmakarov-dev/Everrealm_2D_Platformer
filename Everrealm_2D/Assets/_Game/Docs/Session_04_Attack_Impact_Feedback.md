# Session 04 — Attack Impact Feedback

## Goal

This session added a system where each attack can define its own hit feedback profile.

An attack can now control:

- hit VFX prefab;
- floating damage text type;
- critical floating damage text type;
- effect spawn offset;
- VFX lifetime;
- flip behavior based on attack direction.

The system supports:

- basic auto attacks;
- skill attacks;
- empowered attacks;
- buff skills that temporarily override auto-attack hit visuals;
- enemy attacks.

## Main concept

Attacks now carry an `AttackImpactProfile` in addition to damage data.

Flow:

```text
Attack / Skill / EnemyAttack
→ DamageRequest + AttackImpactProfile
→ CombatService calculates damage
→ DamageResult contains final damage and impact profile
→ Target.ReceiveDamage(DamageResult)
→ FloatingDamageTextController shows text and VFX
```

## New files

### Feedback

- `Assets/_Game/Scripts/Feedback/AttackImpactProfile.cs`  
  ScriptableObject that describes how a hit should look.

- `Assets/_Game/Scripts/Feedback/ImpactEffectPool.cs`  
  Pool manager for hit VFX.

- `Assets/_Game/Scripts/Feedback/ImpactEffectInstance.cs`  
  Runtime component placed on spawned VFX instances. It returns itself to the pool after its lifetime ends.

### Combat

- `Assets/_Game/Scripts/Combat/EnemyAttackDefinition.cs`  
  ScriptableObject for enemy attack data: damage, cooldown, damage lines, tags, and impact profile.

`DamageRequest` now contains:

- `ImpactProfile`;
- `AttackDirection`;
- `CriticalChance`;
- `CriticalDamageMultiplier`.

`DamageResult` now contains:

- `SourceSkillId`;
- `Tags`;
- `ImpactProfile`;
- `AttackDirection`;
- `WasCritical`.

### Characters

- `Assets/_Game/Scripts/Characters/EnemyAttackController2D.cs`  
  Basic enemy attack controller. It can find a target in range and apply damage through `CombatService`.

- `PlayerClassController` now has `Default Auto Attack Impact`.

- `DummyEnemy2D` now implements `ICombatActor`, so a dummy can act as both a target and an attacker.

### Editor

- `Assets/_Game/Scripts/Editor/AttackImpactSetupBuilder.cs`  
  Menu: `Everrealm/Setup Attack Impact Feedback`.

The command creates default impact profiles:

- `NormalSlash.asset`;
- `HeavySlash.asset`;
- `FireHit.asset`;
- `PoisonHit.asset`;
- `ThrowHit.asset`;
- `EnemyHit.asset`;
- `EnemyBasicHit.asset`.

If `VFXPACK_IMPACT_WALLCOEUR_FreeVersion` exists, the setup tries to assign VFX prefabs automatically. If a prefab is not found, the profile is still created with an empty VFX field.

## Assigning VFX

1. Run:

```text
Everrealm/Setup Attack Impact Feedback
```

2. Open:

```text
Assets/_Game/Data/Feedback/ImpactProfiles
```

3. Select a profile, for example `FireHit`.
4. Drag a prefab into `Hit Effect Prefab`.
5. Tune:
   - `Floating Text Type`;
   - `Critical Floating Text Type`;
   - `Hit Effect Offset`;
   - `Effect Lifetime`;
   - `Flip Effect By Attacker Direction`.

## Player attack setup

### Basic attack

On the player object, open `PlayerClassController`.

Use:

```text
Default Auto Attack Impact
```

For example, assign `NormalSlash`.

### Skills

Each `SkillDefinition` has:

```text
Impact Profile
```

Examples:

- `FireSpin` → `FireHit`;
- `IronStrength` → `HeavySlash`;
- `BouncingStar` → `ThrowHit`.

### Empowered attacks

If a skill is an empower skill, its `Impact Profile` is stored and applied to the next auto attack.

Example:

```text
Press IronStrength
→ empower is armed
→ press J
→ auto attack uses HeavySlash impact
```

## Enemy attack setup

Create or open an `EnemyAttackDefinition`.

It contains:

- base damage;
- damage multiplier;
- damage lines;
- cooldown;
- tags;
- impact profile.

Add `EnemyAttackController2D` to an enemy and assign the attack definition.

The setup command adds this controller to dummy enemies, but leaves `Auto Attack` disabled so dummies do not start attacking unexpectedly.

## Testing

### Player hits enemy

1. Run `Everrealm/Setup Attack Impact Feedback`.
2. Press Play.
3. Hit an enemy with `J`.
4. The enemy should show floating text and the VFX from the attack impact profile.

### Skill / empowered attack

1. Assign `Impact Profile` on a `SkillDefinition`.
2. Press Play.
3. Use the skill.
4. Direct damage skills show feedback immediately.
5. Empower skills show feedback on the next auto attack.

### Enemy hits player

1. Select a dummy enemy.
2. Check `EnemyAttackController2D`.
3. Assign `EnemyBasicHit` or another `EnemyAttackDefinition`.
4. Enable `Auto Attack`, or call `Try Attack` from the component context menu.

## Multiplayer notes

The combat result can eventually replicate an `impactId` instead of a direct ScriptableObject reference.

Future flow:

```text
server calculates damage
→ server sends DamageResult data + impactId
→ client resolves impactId locally
→ client shows floating text and VFX
```

## Verification

Verified with:

- `dotnet build Assembly-CSharp.csproj --no-restore`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Both builds passed without errors or warnings.
