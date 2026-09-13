# Session 11 - Skill Tree Foundation

## Goal

This document tracks the first playable skill-tree slice: unlock nodes, spend gold and materials, grant skills to the player, and show a simple UI panel for testing in game mode.

## Current Implementation Status

- Implemented: `SkillTreeDefinition` and `SkillTreeNodeDefinition` assets for editable tree data.
- Implemented: `SkillTreeProgress` runtime state for node ranks; rank greater than zero also represents an unlocked node.
- Implemented: `SkillTreeService` validation and transactions for prerequisites, gold costs, material costs, and unlock actions.
- Implemented: `PlayerSkillTreeController` bridge from wallet, inventory, and player class controller to the skill tree service.
- Implemented: `PlayerClassController.LearnSkill` so unlocked active skills can be registered at runtime.
- Implemented: `SkillTreeWindowPresenter` and `SkillTreeNodeView` for a testable in-game panel toggled with `K`.
- Implemented: unlocked skill nodes expose a `Select` button for loadout assignment.
- Implemented: selecting an unlocked skill creates a dragged skill icon that follows the mouse.
- Implemented: selected skills can be assigned to the bottom skill bar by dropping the icon on a slot, clicking a slot, or pressing `1`-`4`.
- Implemented: `PlayerSkillLoadout` prevents placing the same skill in multiple slots while allowing slot replacement.
- Implemented: gameplay skill hotkeys are ignored while the skill bar is waiting for assignment, so `1`-`4` assign instead of activating old skills.
- Implemented: id-based save data for skill-tree node ranks and skill bar loadout slots, restored automatically on start through `PlayerSaveController`.
- Implemented: legacy saves containing only unlocked node ids migrate those nodes to rank one.
- Implemented: designer-authored node positions, node rank display, upgrade/max states, and prerequisite connection lines.
- Implemented: prefab-driven node and connection visuals; runtime presenters instantiate and bind views but do not construct their controls.
- Implemented: node cards are informational; purchasing is available only through a dedicated `Unlock`/`Upgrade` button separated from `Select`.
- Implemented: a masked vertical `ScrollRect` moves the node and connection layers together inside an inspector-editable content area.
- Implemented: node categories (`Skill`, `Passive`, `Upgrade`), designer-assigned icons, and current-to-next-rank effect text.
- Implemented: an external hover tooltip that remains outside ScrollRect clipping and is editable in the scene.
- Implemented: runtime/editor validation for duplicate ids, missing prerequisites, cycles, invalid skill actions, missing effect targets, and effect ranks above node max rank.
- Implemented: an eleven-node demo tree with three basic branches and a multi-prerequisite capstone.
- Implemented: transactional autosave after a committed node purchase/upgrade and after a successful skill-bar assignment.
- Implemented: one-rank respec with configurable gold refund rate, optional material refunds, dependency protection, and autosave.
- Implemented: a two-click `Refund`/`Confirm` interaction that expires after three seconds.
- Implemented: a half-screen horizontal progression map with root nodes on the left and dependent prerequisite depths in columns to the right.
- Implemented: two-axis scrolling, inspector-editable graph cell/spacing values, and edge-to-edge prerequisite connection lines.
- Implemented: a dedicated `SkillTreeCanvas`; setup no longer invokes inventory/loot setup, reparents inventory UI, or changes starting gold.
- Implemented: attachable components use matching standalone script files, preventing missing scripts in graph and connection prefabs.
- Implemented: the first graph column contains independent Attack, Defense, and Attack Speed basic nodes.
- Implemented: editor setup command that creates a starter warrior tree, demo skill, UI prefab, and scene wiring.
- Implemented: EditMode tests for prerequisite checks, material checks, successful unlocks, spending, skill callbacks, duplicate unlock failures, and duplicate skill loadout assignment.
- Implemented: rank effects for attack power, defense, attack speed, skill damage, skill cooldown reduction, and skill mana-cost reduction.
- Implemented: effective skill values are runtime-only and do not mutate shared `SkillDefinition` assets.
- Implemented: the skill bar uses effective mana cost and cooldown for affordability and cooldown presentation.
- Not implemented yet: health/mana/capacity rank effects, respec, shop integration, explicit save version numbers, and player-facing save/load UI.

## Unity Menu Commands

Create or update skill-tree wiring in the current scene:

```text
Everrealm/Setup Skill Tree
```

Create a dedicated playtest scene:

```text
Everrealm/Build Skill Tree Playtest Scene
```

Validate every skill-tree asset:

```text
Everrealm/Validate Skill Trees
```

Explicitly replace the demo Warrior tree data with the eleven-node example:

```text
Everrealm/Rebuild Demo Warrior Skill Tree Data
```

The normal `Setup Skill Tree` command preserves an existing node array. Only the explicitly named rebuild command overwrites demo tree content.

The playtest command creates:

- `Assets/_Game/Scenes/SkillTreeDebug.unity`
- `Assets/_Game/Data/SkillTrees/WarriorSkillTree.asset`
- `Assets/_Game/Data/Skills/SkillTree/TreeFocusSlash.asset`
- `Assets/_Game/Data/Effects/SkillTree/Warrior_TreeFocusSlash.asset`
- `Assets/_Game/Prefabs/UI/SkillTreeNode.prefab`
- `Assets/_Game/Prefabs/UI/SkillTreeConnection.prefab`

## Runtime Flow

```text
Player opens skill tree
    -> SkillTreeWindowPresenter renders nodes from SkillTreeDefinition
    -> Player clicks unlock
    -> PlayerSkillTreeController delegates to SkillTreeService
    -> SkillTreeService validates prerequisites, gold, and materials
    -> SkillTreeService spends wallet and inventory costs
    -> SkillTreeProgress increases the node rank up to SkillTreeNodeDefinition.MaxRank
    -> Unlock action grants a SkillDefinition to PlayerClassController
    -> Skill bar refreshes
```

## Skill Assignment Flow

```text
Player unlocks or reselects a skill node
    -> SkillTreeNodeView shows Select for unlocked skill nodes
    -> Player clicks Select
    -> SkillBarPresenter stores the pending SkillDefinition
    -> A dragged skill icon follows the mouse
    -> Player drops/clicks a skill bar slot or presses 1-4
    -> PlayerSkillLoadout validates duplicate rules
    -> Selected slot is replaced with the chosen SkillDefinition
    -> Skill bar rebuilds from PlayerSkillLoadout
```

Rules:

- The skill tree unlocks skills; the skill bar decides which unlocked skills are equipped.
- `PlayerSkillLoadout` is the source of truth for what each bottom-panel slot activates.
- A skill cannot be placed in two different slots at the same time.
- Replacing a slot is allowed.
- Runtime UI nodes are not skill data. They render `SkillTreeNodeDefinition` data, which points to `SkillDefinition` assets.

## Save Data

```text
Skill tree progress
    -> save node ids and ranks
    -> load by applying ranks back to SkillTreeProgress
    -> each loaded unlock node re-registers its SkillDefinition with PlayerClassController

Skill bar loadout
    -> save slotIndex + skillId
    -> SkillDatabase resolves skillId to SkillDefinition
    -> PlayerSkillLoadout restores explicit slot bindings
```

Rules:

- Save files store ids, never ScriptableObject references.
- `SkillDatabase` must include every skill that can be loaded into the skill bar.
- Skill tree node ids must stay stable once saves exist.
- The old `unlockedSkillTreeNodeIds` list remains in the DTO for backward-compatible migration.

## Ranked Nodes And Visual Graph

- `SkillTreeNodeDefinition.uiPosition` controls node placement inside the tree canvas.
- `SkillTreeNodeDefinition.maxRank` controls how many times the same node can be purchased.
- Gold and material costs are currently paid on every rank purchase.
- A skill-unlock node grants its `SkillDefinition` only when moving from rank zero to rank one.
- Rank effects begin at their configured `firstAppliedRank`, allowing rank one to unlock a base skill while later ranks improve it.
- Each `SkillTreeRankEffectDefinition` declares an effect type, amount per applied rank, optional target skill, and the first rank where it begins applying.
- Flat stat effects are aggregated and applied through `CombatStats` using `PlayerSkillTreeController` as the modifier source.
- Skill effects become `SkillRuntimeModifiers` inside `SkillService`; damage effects and skill-bar presentation read the resulting `SkillRuntimeValues`.
- Reapplying loaded progress first removes the old stat source and clears skill progression modifiers, preventing duplicated bonuses.
- `SkillTreeWindowPresenter` maps node ids to views, then builds prerequisite lines behind the nodes.
- `SkillTreeConnectionView` owns only line geometry and locked/unlocked presentation.
- The node background is not clickable. `UpgradeButton` owns purchases and `SelectButton` owns skill-bar assignment, preventing accidental purchases.
- Purchase buttons are disabled only at maximum rank. Prerequisite, gold, and material validation runs again on click, while the presenter also refreshes when wallet or inventory contents change.
- `SelectButton` is explicitly activated and made interactable whenever an unlock-skill node has rank one or higher.
- The scene hierarchy is `TreeScroll -> Viewport -> Content -> Connections + Nodes`; designers can resize `Content` or attach a scrollbar without changing gameplay code.
- `Nodes` owns an inspector-editable `SkillTreeGraphLayoutGroup`. `Cell Size`, horizontal/vertical `Spacing`, and `Padding` control the map density.
- Graph depth is derived from prerequisites: roots use column zero and every dependent node uses one column after its deepest prerequisite.
- Nodes sharing a depth are distributed vertically in stable definition order. The presenter expands scroll content on both axes and rebuilds lines after final positions are known.
- `uiPosition` is ignored while graph layout is present; manual positioning remains available by removing the graph layout component.
- `SkillTreeConnection.prefab` is an explicit scene dependency. Setup must assign it to `SkillTreeWindowPresenter.connectionPrefab`; missing connections are a wiring error, not a runtime fallback.
- Skill Tree setup owns only Skill Tree data references, `SkillTreeCanvas`, `SkillTreeWindow`, graph layers, and player Skill Tree wiring. Inventory, Currency HUD, Shop UI, and their canvases are outside its ownership boundary.
- `SkillTreeNodeType` and the node icon are authoring metadata consumed only by presentation.
- Inline effect text compares current and next cumulative values; hover tooltip combines type, rank, description, and the same next-rank preview.
- Validation must pass before new tree content is considered ready for playtesting.
- Autosave listens to `PlayerSkillTreeController.ProgressionCommitted` and `PlayerSkillLoadout.LoadoutChanged`, not raw wallet or inventory events, so partially completed transactions are never persisted.
- Restore and reset operations suppress autosave notifications and write only their intended final state.
- `SkillTreeDefinition.goldRefundRate` controls the refunded share of one rank's gold cost; demo default is 75 percent.
- `SkillTreeDefinition.refundMaterials` controls whether that rank's material costs are returned. Material respec is rejected before mutation when inventory has insufficient capacity.
- Reducing a node from rank N to N-1 is allowed while N-1 remains above zero. Removing the last rank is rejected while any unlocked node directly depends on it.
- Removing the last rank of an unlock-skill node unregisters the learned skill and removes its explicit skill-bar binding before the final commit event.
- Default UI prefabs can be rebuilt explicitly through `Everrealm/Rebuild Default Skill Tree UI Prefabs`; scene wiring is refreshed through `Everrealm/Setup Skill Tree`.

## Next Steps

1. Add player-facing save/load UI, checkpoint trigger, or auto-save policy.
2. Add save versioning and migration rules.
3. Add richer visual states and branching examples after the initial ranked graph is tested in Play Mode.
4. Add optional max-health, max-mana, movement-speed, damage-lines, and max-target rank effects when those progression paths are designed.
5. Add respec rules once the economy is more stable.
6. Connect shop and loot progression so dropped materials naturally feed skill unlocks.
