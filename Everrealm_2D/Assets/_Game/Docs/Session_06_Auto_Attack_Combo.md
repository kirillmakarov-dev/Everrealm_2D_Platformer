# Session 06 — Auto Attack Combo

## Goal

This session added a simple combo system for the basic `J` auto attack.

Behavior:

```text
Press J quickly once  → Combo step 1
Press J quickly twice → Combo step 2
Press J quickly three times → Combo step 3 / Finisher
```

The third hit can have a critical chance.

Damage is not accumulated between button presses. Each `J` press is a separate attack with its own damage calculation.

If the player waits longer than the combo reset window, the next attack starts again at step 1.

## New files

- `Assets/_Game/Scripts/Combat/AutoAttackComboDefinition.cs`  
  ScriptableObject containing combo settings.

- `Assets/_Game/Scripts/Editor/AutoAttackComboSetupBuilder.cs`  
  Editor setup command that creates the combo asset and assigns it to the player.

## Changed files

- `AutoAttackService.cs`  
  Selects and applies the current combo step for the basic auto attack.

- `CombatModels.cs`  
  `DamageRequest` now has:
  - `CriticalChance`;
  - `CriticalDamageMultiplier`.

- `CombatService.cs`  
  Rolls critical chance and writes `WasCritical` into `DamageResult`.

- `PlayerClassController.cs`  
  Added:
  - `Auto Attack Combo`.

- `CharacterRoot.cs`  
  Logs the current combo step:

```text
[Combat] Auto Attack hit 1 target(s), total damage: 16. Combo: 3/3 'Finisher' CRIT!.
```

## Combo asset

After setup, this asset is created:

```text
Assets/_Game/Data/Combat/Combos/WarriorBasicCombo.asset
```

It contains:

- `Reset Time` — how long the player can wait between hits before the combo resets.
- `Steps` — the list of combo hits.

Each step contains:

- `Display Name`;
- `Damage Multiplier`;
- `Critical Chance`;
- `Critical Damage Multiplier`;
- `Impact Profile`.

Default setup:

```text
Hit 1:
  Damage Multiplier: 1
  Critical Chance: 0

Hit 2:
  Damage Multiplier: 1
  Critical Chance: 0

Finisher:
  Damage Multiplier: 1
  Critical Chance: 0.35
  Critical Damage Multiplier: 1.6
```

## Enabling the combo

Run:

```text
Letter Hunter/Setup Auto Attack Combo
```

The command:

1. creates `WarriorBasicCombo.asset`;
2. assigns it to `PlayerClassController → Auto Attack Combo`;
3. uses `NormalSlash` for early hits and `HeavySlash` for the finisher if those impact profiles exist.

## Important behavior

The combo advances only when the attack actually finds a target.

```text
Press J into empty space
→ no target found
→ combo step is not consumed
```

This prevents the player from losing the finisher because of an empty-air swing.

If an empower skill is active, combo damage and critical settings still apply, but the empower skill's `AttackImpactProfile` has visual priority over the combo step impact.

## Adding critical chance to the second hit

Open:

```text
WarriorBasicCombo.asset
```

Set:

```text
Element 1 / Hit 2
→ Critical Chance = 0.15
```

The second hit now has a 15% critical chance.

## Making the third hit stronger

Open:

```text
WarriorBasicCombo.asset
```

Set:

```text
Element 2 / Finisher
→ Damage Multiplier = 1.3
```

The third hit now deals more normal damage even if it does not crit.

## Future improvements

This system can later support:

- separate attack animations per combo step;
- active frames and recovery windows;
- hit stop;
- different target shapes per step;
- combo branching;
- class-specific combo definitions;
- upgrades that modify combo steps.

## Verification

Verified with:

- `dotnet build Assembly-CSharp.csproj --no-restore`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Both builds passed without errors or warnings.
