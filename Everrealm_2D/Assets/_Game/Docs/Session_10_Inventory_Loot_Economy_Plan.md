# Session 10 - Inventory, Loot, And Economy Plan

## Goal

This document defines the architecture for monster drops, item pickup, player inventory, coins, selling items, and future skill-tree economy.

Current implementation status:

- Implemented: item definitions, runtime item stacks, inventory slots, inventory add/remove rules, player inventory adapter.
- Implemented: currency wallet, shop sell service, sell result.
- Implemented: loot table rolls, loot result data, monster loot adapter, drop service, and pickup behavior.
- Implemented: `DummyEnemy2D` calls `MonsterLoot.DropLoot` when death is confirmed.
- Implemented: debug HUD presenters for gold and inventory.
- Implemented: inventory window with fixed slot grid, `B` key toggle, drag move/swap/merge, and right-click stack split.
- Implemented: inventory slot operations in the runtime model: move, swap, merge, split, and change events.
- Implemented: `PlayerInventory` inspector start items for playtesting, similar to `CurrencyWallet` starting gold.
- Implemented: editor setup command that creates starter item assets, starter loot table, generated pickup icons, pickup prefabs, HUD, scene wiring, and a playtest scene.
- Implemented: skill-tree spending consumes gold from `CurrencyWallet` and material costs from `Inventory`.
- Implemented: `ItemDatabase` and id-based save data for gold and inventory slots.
- Implemented: `PlayerSaveController` auto-loads saved player data on start when a save file exists.
- Implemented: `PlayerSaveController.ResetProgress` clears saved/runtime progress, inventory, skill tree unlocks, and skill bar loadout.
- Implemented: first shop selling UI over `ShopService`; successful sells auto-save updated inventory and gold.
- Implemented: shop rows are prefab/view driven through `ShopItemRowView`, so layout/text/buttons can be edited in scene and prefab assets instead of runtime UI construction code.
- Not implemented yet: shop buying/catalog UI and full player-facing save/load UX.

To create the playable test scene inside Unity, run:

```text
Letter Hunter/Build Inventory Loot Playtest Scene
```

This creates:

- `Assets/_Game/Scenes/InventoryLootDebug.unity`
- `Assets/_Game/Data/Items/TrainingShard.asset`
- `Assets/_Game/Data/Items/RustySword.asset`
- `Assets/_Game/Data/LootTables/DummyEnemyLootTable.asset`
- `Assets/_Game/Data/UI/Inventory/*.png`
- `Assets/_Game/Prefabs/Loot/CoinPickup.prefab`
- `Assets/_Game/Prefabs/Loot/ItemPickup.prefab`

If a scene already has a player and enemies, this command can instead be run on the current scene:

```text
Letter Hunter/Setup Inventory Loot Economy
```

The system must support two different pickup paths:

```text
Monster dies
    -> Loot table rolls
    -> Coins go directly into the player's wallet
    -> Items, weapons, and other objects go into inventory
```

Coins are currency. They should not occupy inventory slots.

Items are content. They should be stored as item stacks, shown in UI, and later sold or used by other systems.

## Design Goals

- Keep loot data editable in the Unity Inspector.
- Keep monster code simple: a monster owns a loot table, not item behavior.
- Keep coins separate from inventory items.
- Make inventory logic testable without scenes.
- Prepare for shops, item selling, skill-tree purchases, and save/load.
- Avoid gameplay singletons.
- Keep runtime state out of ScriptableObject assets.

## Suggested Folders

```text
Assets/_Game/Scripts/Items
    ItemDefinition.cs
    ItemStack.cs
    Inventory.cs
    InventorySlot.cs
    InventoryAddResult.cs

Assets/_Game/Scripts/Loot
    LootTable.cs
    LootEntry.cs
    LootRoll.cs
    LootDropService.cs
    MonsterLoot.cs
    LootPickup2D.cs

Assets/_Game/Scripts/Economy
    CurrencyWallet.cs
    ShopService.cs
    SellResult.cs

Assets/_Game/Data/Items
    ItemDefinition assets

Assets/_Game/Data/LootTables
    LootTable assets
```

UI can be added later under:

```text
Assets/_Game/Scripts/UI/Inventory
Assets/_Game/Scripts/UI/Shop
```

## Core Data

### ItemDefinition

`ItemDefinition` is a ScriptableObject. It describes what an item is.

Suggested fields:

- `itemId`
- `displayName`
- `description`
- `icon`
- `itemType`
- `rarity`
- `maxStack`
- `sellPrice`
- future: class requirements, equipment slot, stat modifiers, use effect

Rules:

- `itemId` must be stable and unique.
- `maxStack` should be at least 1.
- `sellPrice` belongs on the item definition so shops and UI read the same source of truth.
- Runtime quantity must not be stored in `ItemDefinition`.

### ItemStack

`ItemStack` is a runtime value.

Suggested fields:

- `ItemDefinition Item`
- `int Amount`

Rules:

- Empty stacks are invalid in normal gameplay.
- Stack amount should be clamped to positive values when created.
- Splitting and merging stacks should happen through inventory methods, not UI code.

### StartingInventoryItem

`StartingInventoryItem` is a serialized playtest helper owned by `PlayerInventory`.

Suggested fields:

- `ItemDefinition item`
- `int amount`

Rules:

- Starting items are copied into the runtime `Inventory` once when the player inventory is created.
- Starting item data is for scene setup and testing, not save data.
- Invalid entries are skipped.
- If starting items exceed capacity, normal `Inventory.TryAdd` partial-add rules apply.

### CurrencyWallet

`CurrencyWallet` stores money and future currencies.

Initial version:

- `int Gold`
- `AddGold(int amount)`
- `CanSpendGold(int amount)`
- `TrySpendGold(int amount)`

Future version can support:

- skill essence;
- class tokens;
- premium upgrade materials;
- multiple named currencies.

Coins from drops should call `CurrencyWallet.AddGold` immediately when picked up.

## Loot Data

### LootTable

`LootTable` is a ScriptableObject assigned to enemies or enemy archetypes.

Suggested fields:

- `lootTableId`
- `coinMin`
- `coinMax`
- `coinDropChance`
- `drops`

`drops` contains item entries only. Coins stay as explicit table fields because they follow a different pickup and storage path.

### LootEntry

Suggested fields:

- `ItemDefinition item`
- `float dropChance`
- `int minAmount`
- `int maxAmount`
- `int weight`
- future: required enemy level, player class filter, pity rules, unique drop limits

Two models can coexist:

1. Independent chance rolls:

```text
Roll every entry once.
Each item has its own dropChance.
```

2. Weighted selection:

```text
Choose N rewards from a weighted pool.
Weight decides relative rarity.
```

Recommended first implementation: independent chance rolls. It is simpler for students to understand and easier to test.

### LootRoll

`LootRoll` is the runtime result produced by a loot table.

Suggested fields:

- `int Coins`
- `IReadOnlyList<ItemStack> Items`

Rules:

- `Coins > 0` means spawn a coin pickup or add directly to the wallet depending on pickup style.
- `Items` contains inventory rewards only.
- A loot roll should be plain C# data so tests can validate it without Unity scene objects.

## Runtime Flow

### Enemy Death Flow

```text
Enemy health reaches zero
    -> enemy death event
    -> MonsterLoot receives death notification
    -> MonsterLoot asks LootDropService to roll its LootTable
    -> LootDropService creates pickups near the enemy
```

`MonsterLoot` should be a small MonoBehaviour.

Suggested fields:

- `LootTable lootTable`
- `Transform dropPoint`
- optional `float scatterRadius`

Rules:

- Enemy AI should not roll loot directly.
- Combat should not know about inventory or economy.
- Loot should be triggered after death is confirmed, not while damage is being calculated.

### Pickup Flow

```text
Player touches pickup
    -> LootPickup2D checks pickup kind
    -> coin pickup calls CurrencyWallet.AddGold
    -> item pickup calls Inventory.TryAdd
    -> pickup disappears only after successful collection
```

Pickup kinds:

- `Coin`
- `ItemStack`

Coin pickup:

- always collectible if the player has a wallet;
- never uses inventory slots;
- can merge visually with nearby coin pickups later.

Item pickup:

- checks inventory capacity;
- if inventory has room, adds the stack and destroys pickup;
- if inventory is full, keeps pickup in the world and can show feedback later.

## Inventory Rules

`Inventory` should be plain C# runtime logic where possible.

Suggested responsibilities:

- store slots;
- merge stackable items;
- find empty slots;
- add items;
- remove items;
- count items;
- raise `InventoryChanged` events.

Suggested behavior:

```text
TryAdd(stack)
    -> merge into existing stacks first
    -> place remaining amount into empty slots
    -> return AddedAll, AddedPartial, or NoSpace
```

Important rules:

- UI must not mutate slot arrays directly.
- Inventory should not know about shops, skill trees, enemies, or pickups.
- Inventory can expose read-only slot views for UI.
- If an item has `maxStack = 1`, every copy uses a separate slot.

## Shop And Selling Plan

`ShopService` should depend on:

- `Inventory`
- `CurrencyWallet`

Selling flow:

```text
Player selects item stack
    -> ShopService.TrySell
    -> inventory removes item amount
    -> wallet receives item.sellPrice * amount
    -> UI refreshes from inventory and wallet events
```

Rules:

- Sell price comes from `ItemDefinition`.
- Shop UI should ask `ShopService`; it should not remove items or add gold by itself.
- Selling should return a result object so UI can show failure reasons.

Suggested failure reasons:

- item missing;
- amount invalid;
- item cannot be sold;
- inventory changed before transaction finished.

## Skill Tree Economy Plan

The future skill tree should spend currencies through `CurrencyWallet` and, if needed, consume materials through `Inventory`.

Example unlock costs:

```text
Skill node unlock:
    250 gold
    3 Fire Shards
```

The skill tree should not know how loot is generated. It only reads wallet and inventory state.

When a skill node unlocks a new skill:

```text
SkillTreeService
    -> validates cost
    -> spends currency/materials
    -> marks node unlocked
    -> registers or enables SkillDefinition through SkillService / loadout system
```

## Save And Load Considerations

Save data should store identifiers and runtime values:

- wallet currency amounts;
- inventory slot item ids;
- inventory slot amounts;
- unlocked skill-tree node ids;
- skill bar slot skill ids.

Save data should not serialize direct ScriptableObject references.

Load flow:

```text
Save itemId
    -> ItemDatabase resolves itemId to ItemDefinition
    -> Inventory restores ItemStack
```

Current implementation:

- `ItemDatabase` resolves saved `itemId` values to `ItemDefinition` assets.
- `SkillDatabase` resolves saved `skillId` values to `SkillDefinition` assets.
- `GameSaveData` stores gold, inventory slots, unlocked skill-tree node ids, and skill bar slots.
- `PlayerSaveController` can capture/restore player state, save/load JSON from `Application.persistentDataPath`, and auto-load on start.
- `PlayerSaveController.ResetProgress` deletes the old save, clears runtime progress, and writes a fresh save.
- `ShopWindowPresenter` lists sellable inventory items, calls `ShopService.TrySell`, updates feedback/gold, and saves after successful sales.
- `ShopItemRowView` owns the row text/buttons and is intended to be edited as a prefab.
- `Letter Hunter/Setup Save Databases` creates and fills database assets from existing item and skill assets.
- `Letter Hunter/Setup Shop UI` creates a starter shop window, scene wiring, and `Assets/_Game/Prefabs/UI/ShopItemRow.prefab` when missing.

## Testing Plan

Add EditMode tests for pure logic first:

- `LootTable` rolls coins within min/max.
- `LootTable` does not produce coins when coin chance fails.
- `LootTable` produces expected item stacks with deterministic random source.
- `Inventory.TryAdd` merges existing stacks before using empty slots.
- `Inventory.TryAdd` returns partial or failure when full.
- `Inventory.Remove` removes the requested amount and preserves remaining stacks.
- `CurrencyWallet.TrySpendGold` succeeds only when enough gold exists.
- `ShopService.TrySell` removes items and adds correct gold.
- item pickups do not disappear when inventory is full.

To make loot tests deterministic, introduce a tiny random abstraction:

```csharp
public interface IRandomSource
{
    float Next01();
    int RangeInclusive(int min, int max);
}
```

The project already has an `IRandomSource` concept for passives. Prefer reusing or moving it to a shared `Core` or `Infrastructure` namespace instead of creating duplicate random contracts.

## Implementation Order

Recommended remaining order:

1. Create starter `ItemDefinition` assets.
2. Create starter `LootTable` assets.
3. Add `LootDropService` to the playable scene.
4. Add `PlayerInventory` and `CurrencyWallet` to the player.
5. Add `MonsterLoot` and loot table references to enemies.
6. Add item dropping from inventory into the world.
7. Add shop buying/catalog UI.
8. Add a player-facing save/load UI, checkpoint trigger, or broader auto-save trigger.
9. Add versioning and migration fields to save data before larger content growth.

## Open Decisions

- Should coin drops spawn physical pickups, or should enemies award gold directly on death?
- Should weapons be stackable only when identical, or always one item per slot?
- Should inventory be fixed-size from the start, or expandable through upgrades?
- Should sell price be fixed, rarity-based, or modified by shop NPCs later?
- Should pickup be manual, automatic on collision, or both?

Recommended first choices:

- Spawn physical coin pickups so feedback feels rewarding.
- Store weapons as non-stackable items.
- Use fixed-size inventory first.
- Use fixed sell price from `ItemDefinition`.
- Use automatic pickup for coins and manual/automatic pickup for items depending on UI needs.

## Architecture Summary

Loot, inventory, and economy should stay separated:

```text
LootTable
    -> produces LootRoll
        -> Coins -> CurrencyWallet
        -> ItemStacks -> Inventory

Inventory
    -> stores items
    -> supports shop selling
    -> supports future crafting and skill-tree material costs

CurrencyWallet
    -> stores money
    -> supports shop buying/selling
    -> supports future skill-tree unlock costs
```

This keeps monster drops, item storage, economy, and progression connected through clear runtime contracts instead of one large manager.
