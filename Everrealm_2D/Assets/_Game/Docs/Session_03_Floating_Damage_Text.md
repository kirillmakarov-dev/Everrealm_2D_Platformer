# Session 03 — Floating Damage Text

## Goal

This session added a custom floating damage text system for Everrealm.

`FloatingTextEngineLite` was used only as a design reference for the idea of:

- a prefab;
- a manager;
- an object pool;
- reusable visual presets.

The new game scripts do not depend on the package namespace or API, so the package can be removed after verification.

## Added files

- `Assets/_Game/Scripts/Feedback/FloatingDamageTextManager.cs`  
  Scene manager that creates and reuses popup instances through `ObjectPool`.

- `Assets/_Game/Scripts/Feedback/FloatingDamageTextPopup.cs`  
  Component for one floating text object. It owns the TextMeshPro label, upward motion, random X variance, fade-out, and scale animation.

- `Assets/_Game/Scripts/Feedback/FloatingDamageTextController.cs`  
  Receiver component for damageable objects. It shows floating text when damage is received.

- `Assets/_Game/Scripts/Feedback/FloatingDamageTextType.cs`  
  Visual text categories: `Normal`, `Critical`, `Skill`, `Fire`, `Ice`, `Poison`, `Heal`, `Blocked`, `Miss`.

- `Assets/_Game/Scripts/Feedback/FloatingDamageTextStyle.cs`  
  Style settings for color, font size, lifetime, velocity, scale curve, and fade curve.

- `Assets/_Game/Scripts/Editor/FloatingDamageTextSetupBuilder.cs`  
  Unity menu setup: `Everrealm/Setup Floating Damage Text`.

## Damage integration

Damageable objects call `FloatingDamageTextController.ShowDamage(...)` after receiving a `DamageResult`.

This means attacks that go through `CombatService` can automatically show floating text above the target.

The floating text system is visual only. It does not calculate or modify damage.

## Testing

1. Stop Play Mode.
2. Wait for Unity compilation.
3. Open:

```text
Assets/_Game/Scenes/CharacterFrameworkDebug.unity
```

4. Run:

```text
Everrealm/Setup Floating Damage Text
```

5. Press Play.
6. Hit an enemy with `J`.
7. A floating damage number should appear above the enemy.

For manual testing:

1. Select a `DummyEnemy2D`.
2. Open `FloatingDamageTextController`.
3. Use the component context menu:

```text
Test Damage Text
```

## Where to change visual style

On `FloatingDamageTextManager`:

- `Popup Prefab` — prefab used for popup text.
- `Default Capacity` / `Max Pool Size` — pool settings.
- `Randomize Spawn X` / `Spawn X Variance` — spawn position variation.
- `Styles` — visual style list for each `FloatingDamageTextType`.

On `FloatingDamageTextController`:

- fallback damage text type;
- local spawn offset;
- damage rounding;
- whether zero damage should display as `BLOCK`.

## Multiplayer notes

The system is separated from combat:

- combat calculates damage and produces a result;
- feedback displays the result.

In a future multiplayer version, the server can calculate damage and replicate confirmed results, while the client shows floating text locally.

## Removing the reference package

Before removing `FloatingTextEngineLite`, make sure no scene still contains the old package object `FloatingTextEngine`.

Keep the Everrealm files:

- `Assets/_Game/Scripts/Feedback/*`;
- `Assets/_Game/Scripts/Editor/FloatingDamageTextSetupBuilder.cs`;
- `Assets/_Game/Prefabs/Feedback/FloatingDamageText.prefab` after it has been created by the setup menu.

## Verification

Verified with:

- `dotnet build Assembly-CSharp.csproj --no-restore`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Both builds passed without errors or warnings.
