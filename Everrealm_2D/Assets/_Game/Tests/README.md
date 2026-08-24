# Letter Hunter Test Coverage

This folder contains EditMode tests for the main gameplay systems in Letter Hunter.

## Test Assembly

- `Editor/LetterHunter.EditModeTests.asmdef` marks the tests as Unity EditMode tests.
- Tests reference `LetterHunter.Runtime`, so gameplay code is compiled separately from editor-only tools.

## Covered Systems

`Editor/GameplayRegressionTests.cs` covers the core regression surface:

- Combat stats: health, mana, attack, defense, attack speed, clamping, and modifiers.
- Damage flow: valid/invalid damage requests, dead targets, defense per damage line, critical hits, tags, source skill id, and hit direction.
- Auto attack: base profile, target limits, passive extra damage lines, empower consumption, combo step order, finisher critical hit, and combo metadata.
- Buffs and passives: stat buff replacement, buff expiration, attack impact override, passive defense bonus, and periodic healing.
- Skill service: registration, mana cost, cooldown, duration, active area damage, missing skills, dead caster, and insufficient mana.
- Data definitions: safe fallback values for `ClassDefinition` and `AutoAttackComboDefinition` inspector data.
- State machines: generic state transitions and character idle/run/jump/fall/attack transitions.
- HUD model: `SkillRuntimeState` and `SkillSlotViewModel` cooldown/readiness values.

Inventory and loot tests may live in the same `Editor` folder when those systems are developed in separate tasks.

## How To Run

Open Unity Test Runner:

`Window > General > Test Runner > EditMode > Run All`

Or run from command line:

```powershell
"C:\Program Files\Unity\Hub\Editor\6000.3.3f1\Editor\Unity.exe" -batchmode -nographics -projectPath "C:\English Room\Letter-Hunter-New\Letter_Hunter" -runTests -testPlatform editmode
```

## Expected Result

The gameplay regression suite should pass before merging new gameplay features into `Development`.
If a test fails after a feature change, inspect the failure first: it usually means an existing gameplay rule changed and should either be fixed or intentionally updated in the tests.
