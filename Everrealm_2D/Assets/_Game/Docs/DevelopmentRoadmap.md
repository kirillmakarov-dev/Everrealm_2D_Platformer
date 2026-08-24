# Development Roadmap

| Session | Scope | Status |
|---|---|---|
| 01 | Combat, class, and skill core | Completed |
| 02 | Universal character framework | Completed |
| 03 | Floating damage text | Completed |
| 04 | Attack impact feedback and VFX profiles | Completed |
| 05 | Skill bar UI | Completed |
| 06 | Basic auto-attack combo | Completed |
| 07 | Enemy patrol AI and death flow | Completed |
| 08 | Player health and mana HUD | Completed |
| 09 | Hit reaction, knockback, and invulnerability | Completed |
| 10 | Inventory, loot, and economy foundation | Completed |
| 11 | Skill tree foundation, unlock economy, and loadout assignment | In progress |
| 12 | Runtime player stats UI and movement/jump Skill Tree modifiers | Completed |
| Next | Add full-tree respec UX, save versioning, and shop buying/catalog UI | Planned |
| Future | Inventory, progression, save system, networking, prediction, and replication | Backlog |

## Recommended next steps

1. Add shop buying/catalog UI on top of the existing selling UI.
2. Add save versioning and migration rules.
3. Add player-facing save/load feedback and checkpoint UX on top of the implemented transactional autosave.
4. Add full-tree respec UX on top of the implemented one-rank refund rules.
5. Add optional manual branch positioning and connection highlighting beyond the implemented horizontal graph layout.
6. Add item dropping from inventory into the world.
7. Add more EditMode tests for combat, skills, buffs, empower, combo, cooldown, and progression.
8. Add a fixed simulation clock / `IGameClock` abstraction for deterministic gameplay.
9. Add combat event streams so animation and VFX can react without owning combat logic.
10. Add proper animation timing for attacks, including active frames and recovery windows.
11. Add enemy attack pattern variety.
12. Prepare multiplayer adapters once the local gameplay loop is stable.
