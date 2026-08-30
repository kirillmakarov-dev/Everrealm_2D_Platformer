# AGENTS.md

## Project Scope

This repository contains the nested Unity project:

- Unity project root: `Everrealm_2D/`
- Unity version: `6000.3.3f1`
- Product name: `Letter_Hunter`
- Main runtime namespace: `LetterHunter`
- Main runtime assembly: `LetterHunter.Runtime`

All Unity work must target `Everrealm_2D/`. Do not treat the Git root as the Unity project root.

## Architecture Source of Truth

Before changing architecture or adding a system, read:

- `Everrealm_2D/Assets/_Game/Docs/Architecture.md`
- `Everrealm_2D/Assets/_Game/Docs/DevelopmentRoadmap.md`

The architecture is data-driven and uses small MonoBehaviour adapters, plain C# services, explicit interfaces and contracts, ScriptableObject definitions for authoring-time configuration, and separated gameplay, presentation, physics, UI, and editor tooling.

Do not introduce gameplay singletons or hidden global state.

## Main Game Systems

The main gameplay areas are:

- `Assets/_Game/Scripts/Characters`
- `Assets/_Game/Scripts/Combat`
- `Assets/_Game/Scripts/Skills`
- `Assets/_Game/Scripts/Effects`
- `Assets/_Game/Scripts/Stats`
- `Assets/_Game/Scripts/Loot`
- `Assets/_Game/Scripts/Items`
- `Assets/_Game/Scripts/Economy`
- `Assets/_Game/Scripts/Save`
- `Assets/_Game/Scripts/SkillTree`
- `Assets/_Game/Scripts/Feedback`
- `Assets/_Game/Scripts/UI`

Keep responsibilities in their existing areas. Do not move code between layers unless the task explicitly requires architectural work.

## Runtime Rules

- All damage must go through the existing combat contracts and `CombatService`.
- Targeting must use the existing targeting abstractions.
- UI must not own gameplay truth or directly mutate combat, stats, inventory, or currency.
- ScriptableObjects contain authoring/configuration data, not mutable runtime state.
- Runtime state belongs in runtime classes such as inventories, skill runtime state, combat state, buffs, and save data.
- Preserve explicit serialized references and existing composition roots.
- Do not silently create replacement prefabs or scene objects at runtime.
- Keep MonoBehaviour lifecycle dependencies explicit and validate required references.

## Unity Assets and Serialization

Do not change scenes, prefabs, ScriptableObject assets, animation controllers, or other serialized Unity assets unless the user gives direct permission.

When serialized data is explicitly in scope:

- inspect all affected references first;
- preserve GUIDs and stable asset paths;
- inspect related prefabs, scenes, and ScriptableObjects;
- avoid broad YAML rewrites;
- report serialized changes separately from code changes.

The local `Assets/SoftKitty/` package is an ignored Asset Store dependency. Do not modify or commit it unless explicitly requested.

## Multiplayer Boundary

This is currently a local single-player project.

Do not add multiplayer gameplay, Photon, Fusion, Mirror, Netcode, networking adapters, replication, prediction, or server authority.

The installed Unity Multiplayer Center package and default Unity multiplayer settings do not mean that multiplayer gameplay exists in this project.

## Editor Tooling

Editor scripts belong in `Assets/_Game/Scripts/Editor` and the `LetterHunter.Editor` assembly.

Runtime systems must not depend on editor builders. Setup builders should remain focused and idempotent where practical.

## Tests and Verification

After C# changes:

1. Check the Unity Console for errors.
2. Build the relevant generated project, normally:
   `dotnet build Everrealm_2D/Assembly-CSharp.csproj --nologo -v minimal`
3. Run relevant EditMode tests through Unity Test Runner or an equivalent Unity batchmode command.
4. Inspect affected serialized assets when serialized references or fields changed.
5. Report any verification that could not be performed.

Existing tests are under `Everrealm_2D/Assets/_Game/Tests/Editor/`.

Do not claim Play Mode or Unity Editor verification unless it was actually performed.

## Filesystem and Git

- Do not edit generated folders such as `Library`, `Temp`, `Logs`, or `UserSettings`.
- Preserve unrelated user changes.
- Before committing, inspect the exact changed-file list.
- Do not stage broad unrelated changes.
- Keep repository-level agent configuration outside `Everrealm_2D/`, because the nested project's `.codex/` and `.agents/` paths are ignored.

## Communication

For every implementation task, state affected systems and files, whether serialized Unity assets are involved, verification performed, and known limitations or unverified runtime behavior.
