using System.IO;
using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.Save;
using LetterHunter.SkillTree;
using LetterHunter.Skills;
using LetterHunter.UI.Skills;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Tests.EditMode
{
    public sealed class PlayerSaveControllerTests
    {
        [Test]
        public void CaptureAndRestore_RoundTripsWalletInventoryAndLoadout()
        {
            var shard = CreateItem("training_shard", 20);
            var focus = CreateSkill("focus_slash", "Focus Slash");
            var itemDatabase = CreateItemDatabase(shard);
            var skillDatabase = CreateSkillDatabase(focus);

            var source = CreatePlayerSaveObject(itemDatabase, skillDatabase, out var sourceWallet,
                out var sourceInventory, out var sourceLoadout);
            sourceWallet.AddGold(42);
            sourceInventory.RuntimeInventory.TrySetSlot(1, shard, 3);
            Assert.That(sourceLoadout.TryAssignSkill(2, focus, out _), Is.True);

            var data = source.Capture();

            var target = CreatePlayerSaveObject(itemDatabase, skillDatabase, out var targetWallet,
                out var targetInventory, out var targetLoadout);
            target.Restore(data);

            Assert.That(targetWallet.Gold, Is.EqualTo(42));
            Assert.That(targetInventory.RuntimeInventory.Slots[1].Item, Is.EqualTo(shard));
            Assert.That(targetInventory.RuntimeInventory.Slots[1].Amount, Is.EqualTo(3));
            Assert.That(targetLoadout.Slots.Count, Is.EqualTo(1));
            Assert.That(targetLoadout.Slots[0].SlotIndex, Is.EqualTo(2));
            Assert.That(targetLoadout.Slots[0].Skill, Is.EqualTo(focus));

            Object.DestroyImmediate(source.gameObject);
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(itemDatabase);
            Object.DestroyImmediate(skillDatabase);
            Object.DestroyImmediate(shard);
            Object.DestroyImmediate(focus);
        }

        [Test]
        public void Start_LoadsSavedDataFromDisk()
        {
            var shard = CreateItem("training_shard", 20);
            var focus = CreateSkill("focus_slash", "Focus Slash");
            var itemDatabase = CreateItemDatabase(shard);
            var skillDatabase = CreateSkillDatabase(focus);
            var saveFileName = $"letter-hunter-test-{System.Guid.NewGuid():N}.json";
            var savePath = Path.Combine(Application.persistentDataPath, saveFileName);

            var source = CreatePlayerSaveObject(itemDatabase, skillDatabase, out var sourceWallet,
                out var sourceInventory, out var sourceLoadout, saveFileName);
            sourceWallet.SetGold(77);
            sourceInventory.RuntimeInventory.TrySetSlot(0, shard, 5);
            Assert.That(sourceLoadout.TryAssignSkill(1, focus, out _), Is.True);
            source.SaveToDisk();

            var target = CreatePlayerSaveObject(itemDatabase, skillDatabase, out var targetWallet,
                out var targetInventory, out var targetLoadout, saveFileName);
            target.SendMessage("Start");

            Assert.That(targetWallet.Gold, Is.EqualTo(77));
            Assert.That(targetInventory.RuntimeInventory.Slots[0].Item, Is.EqualTo(shard));
            Assert.That(targetInventory.RuntimeInventory.Slots[0].Amount, Is.EqualTo(5));
            Assert.That(targetLoadout.Slots.Count, Is.EqualTo(1));
            Assert.That(targetLoadout.Slots[0].SlotIndex, Is.EqualTo(1));
            Assert.That(targetLoadout.Slots[0].Skill, Is.EqualTo(focus));

            if (File.Exists(savePath))
                File.Delete(savePath);
            Object.DestroyImmediate(source.gameObject);
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(itemDatabase);
            Object.DestroyImmediate(skillDatabase);
            Object.DestroyImmediate(shard);
            Object.DestroyImmediate(focus);
        }

        [Test]
        public void ResetProgress_ClearsRuntimeStateAndWritesFreshSave()
        {
            var shard = CreateItem("training_shard", 20);
            var focus = CreateSkill("focus_slash", "Focus Slash");
            var itemDatabase = CreateItemDatabase(shard);
            var skillDatabase = CreateSkillDatabase(focus);
            var saveFileName = $"letter-hunter-reset-test-{System.Guid.NewGuid():N}.json";
            var savePath = Path.Combine(Application.persistentDataPath, saveFileName);

            var save = CreatePlayerSaveObject(itemDatabase, skillDatabase, out var wallet,
                out var inventory, out var loadout, saveFileName);
            wallet.SetGold(33);
            inventory.RuntimeInventory.TrySetSlot(2, shard, 4);
            Assert.That(loadout.TryAssignSkill(0, focus, out _), Is.True);

            save.ResetProgress();
            var savedData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(savePath));

            Assert.That(wallet.Gold, Is.EqualTo(0));
            Assert.That(inventory.RuntimeInventory.Slots[2].IsEmpty, Is.True);
            Assert.That(loadout.Slots.Count, Is.EqualTo(0));
            Assert.That(savedData.gold, Is.EqualTo(0));
            Assert.That(savedData.inventorySlots.Count, Is.EqualTo(0));
            Assert.That(savedData.skillBarSlots.Count, Is.EqualTo(0));

            if (File.Exists(savePath))
                File.Delete(savePath);
            Object.DestroyImmediate(save.gameObject);
            Object.DestroyImmediate(itemDatabase);
            Object.DestroyImmediate(skillDatabase);
            Object.DestroyImmediate(shard);
            Object.DestroyImmediate(focus);
        }

        [Test]
        public void CaptureAndRestore_RoundTripsSkillTreeRanksAndMigratesLegacyUnlocks()
        {
            var itemDatabase = CreateItemDatabase(CreateItem("unused", 1));
            var skillDatabase = CreateSkillDatabase(CreateSkill("unused_skill", "Unused"));
            var tree = CreateRankedTree();
            var source = CreatePlayerSaveObject(itemDatabase, skillDatabase, out _, out _, out _);
            var sourceTree = AttachSkillTree(source, tree);
            sourceTree.Progress.SetRank("ranked_node", 2);

            var data = source.Capture();
            var target = CreatePlayerSaveObject(itemDatabase, skillDatabase, out _, out _, out _);
            var targetTree = AttachSkillTree(target, tree);
            target.Restore(data);

            Assert.That(targetTree.Progress.GetRank("ranked_node"), Is.EqualTo(2));

            var legacyData = new GameSaveData();
            legacyData.unlockedSkillTreeNodeIds.Add("ranked_node");
            target.Restore(legacyData);
            Assert.That(targetTree.Progress.GetRank("ranked_node"), Is.EqualTo(1));

            Object.DestroyImmediate(source.gameObject);
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(tree);
            Object.DestroyImmediate(itemDatabase.Items[0]);
            Object.DestroyImmediate(skillDatabase.Skills[0]);
            Object.DestroyImmediate(itemDatabase);
            Object.DestroyImmediate(skillDatabase);
        }

        [Test]
        public void Start_AutoSavesSuccessfulSkillLoadoutChanges()
        {
            var item = CreateItem("unused", 1);
            var skill = CreateSkill("auto_saved_skill", "Auto Saved Skill");
            var itemDatabase = CreateItemDatabase(item);
            var skillDatabase = CreateSkillDatabase(skill);
            var saveFileName = $"letter-hunter-autosave-test-{System.Guid.NewGuid():N}.json";
            var savePath = Path.Combine(Application.persistentDataPath, saveFileName);
            var save = CreatePlayerSaveObject(itemDatabase, skillDatabase, out _, out _, out var loadout,
                saveFileName);

            save.SendMessage("Start");
            Assert.That(loadout.TryAssignSkill(1, skill, out _), Is.True);

            Assert.That(File.Exists(savePath), Is.True);
            var savedData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(savePath));
            Assert.That(savedData.skillBarSlots.Count, Is.EqualTo(1));
            Assert.That(savedData.skillBarSlots[0].slotIndex, Is.EqualTo(1));
            Assert.That(savedData.skillBarSlots[0].skillId, Is.EqualTo(skill.SkillId));

            if (File.Exists(savePath))
                File.Delete(savePath);
            Object.DestroyImmediate(save.gameObject);
            Object.DestroyImmediate(itemDatabase);
            Object.DestroyImmediate(skillDatabase);
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(skill);
        }

        [Test]
        public void Start_AutoSavesCommittedSkillTreeRankPurchase()
        {
            var item = CreateItem("unused", 1);
            var skill = CreateSkill("unused_skill", "Unused Skill");
            var itemDatabase = CreateItemDatabase(item);
            var skillDatabase = CreateSkillDatabase(skill);
            var tree = CreateRankedTree();
            var saveFileName = $"letter-hunter-tree-autosave-test-{System.Guid.NewGuid():N}.json";
            var savePath = Path.Combine(Application.persistentDataPath, saveFileName);
            var save = CreatePlayerSaveObject(itemDatabase, skillDatabase, out _, out _, out _, saveFileName);
            var controller = AttachSkillTree(save, tree);

            save.SendMessage("Start");
            Assert.That(controller.Service.TryUnlock("ranked_node").Success, Is.True);

            Assert.That(File.Exists(savePath), Is.True);
            var savedData = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(savePath));
            Assert.That(savedData.skillTreeNodeRanks.Count, Is.EqualTo(1));
            Assert.That(savedData.skillTreeNodeRanks[0].nodeId, Is.EqualTo("ranked_node"));
            Assert.That(savedData.skillTreeNodeRanks[0].rank, Is.EqualTo(1));

            if (File.Exists(savePath))
                File.Delete(savePath);
            Object.DestroyImmediate(save.gameObject);
            Object.DestroyImmediate(tree);
            Object.DestroyImmediate(itemDatabase);
            Object.DestroyImmediate(skillDatabase);
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(skill);
        }

        private static PlayerSkillTreeController AttachSkillTree(PlayerSaveController save, SkillTreeDefinition tree)
        {
            var controller = save.gameObject.AddComponent<PlayerSkillTreeController>();
            controller.SetSkillTree(tree);
            var saveSo = new SerializedObject(save);
            saveSo.FindProperty("skillTree").objectReferenceValue = controller;
            saveSo.ApplyModifiedPropertiesWithoutUndo();
            return controller;
        }

        private static SkillTreeDefinition CreateRankedTree()
        {
            var tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
            var so = new SerializedObject(tree);
            var nodes = so.FindProperty("nodes");
            nodes.arraySize = 1;
            var node = nodes.GetArrayElementAtIndex(0);
            node.FindPropertyRelative("nodeId").stringValue = "ranked_node";
            node.FindPropertyRelative("displayName").stringValue = "Ranked Node";
            node.FindPropertyRelative("maxRank").intValue = 3;
            node.FindPropertyRelative("unlockAction").intValue = (int)SkillTreeUnlockAction.None;
            so.ApplyModifiedPropertiesWithoutUndo();
            return tree;
        }

        private static PlayerSaveController CreatePlayerSaveObject(ItemDatabase itemDatabase, SkillDatabase skillDatabase,
            out CurrencyWallet wallet, out PlayerInventory inventory, out PlayerSkillLoadout loadout, string saveFileName = null)
        {
            var go = new GameObject("SavePlayer");
            wallet = go.AddComponent<CurrencyWallet>();
            inventory = go.AddComponent<PlayerInventory>();
            loadout = go.AddComponent<PlayerSkillLoadout>();
            var save = go.AddComponent<PlayerSaveController>();

            var so = new SerializedObject(save);
            so.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
            so.FindProperty("skillDatabase").objectReferenceValue = skillDatabase;
            so.FindProperty("wallet").objectReferenceValue = wallet;
            so.FindProperty("inventory").objectReferenceValue = inventory;
            so.FindProperty("skillLoadout").objectReferenceValue = loadout;
            if (!string.IsNullOrWhiteSpace(saveFileName))
                so.FindProperty("saveFileName").stringValue = saveFileName;
            so.ApplyModifiedPropertiesWithoutUndo();

            return save;
        }

        private static ItemDatabase CreateItemDatabase(ItemDefinition item)
        {
            var database = ScriptableObject.CreateInstance<ItemDatabase>();
            var so = new SerializedObject(database);
            var items = so.FindProperty("items");
            items.arraySize = 1;
            items.GetArrayElementAtIndex(0).objectReferenceValue = item;
            so.ApplyModifiedPropertiesWithoutUndo();
            return database;
        }

        private static SkillDatabase CreateSkillDatabase(SkillDefinition skill)
        {
            var database = ScriptableObject.CreateInstance<SkillDatabase>();
            var so = new SerializedObject(database);
            var skills = so.FindProperty("skills");
            skills.arraySize = 1;
            skills.GetArrayElementAtIndex(0).objectReferenceValue = skill;
            so.ApplyModifiedPropertiesWithoutUndo();
            return database;
        }

        private static ItemDefinition CreateItem(string itemId, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            var so = new SerializedObject(item);
            so.FindProperty("itemId").stringValue = itemId;
            so.FindProperty("displayName").stringValue = itemId;
            so.FindProperty("maxStack").intValue = maxStack;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static SkillDefinition CreateSkill(string skillId, string displayName)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var so = new SerializedObject(skill);
            so.FindProperty("skillId").stringValue = skillId;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("shortName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            return skill;
        }
    }
}
