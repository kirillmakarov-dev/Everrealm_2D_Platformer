using System.Collections.Generic;
using System.IO;
using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.SkillTree;
using LetterHunter.Skills;
using LetterHunter.UI.Skills;
using UnityEngine;

namespace LetterHunter.Save
{
    [DisallowMultipleComponent]
    public sealed class PlayerSaveController : MonoBehaviour
    {
        [Header("Databases")]
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private SkillDatabase skillDatabase;

        [Header("Sources")]
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerSkillTreeController skillTree;
        [SerializeField] private PlayerSkillLoadout skillLoadout;
        [SerializeField] private SkillBarPresenter skillBar;

        [Header("File")]
        [SerializeField] private string saveFileName = "letter-hunter-save.json";
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool autoSaveProgress = true;

        private bool _started;
        private bool _subscribed;
        private bool _isApplyingSave;

        private void Awake()
        {
            if (wallet == null)
                wallet = GetComponent<CurrencyWallet>();
            if (inventory == null)
                inventory = GetComponent<PlayerInventory>();
            if (skillTree == null)
                skillTree = GetComponent<PlayerSkillTreeController>();
            if (skillLoadout == null)
                skillLoadout = GetComponent<PlayerSkillLoadout>();
            if (skillBar == null)
                skillBar = FindFirstObjectByType<SkillBarPresenter>();
        }

        public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        private void Start()
        {
            if (loadOnStart)
                LoadFromDisk(false);
            _started = true;
            SubscribeAutoSave();
        }

        private void OnEnable()
        {
            if (_started)
                SubscribeAutoSave();
        }

        private void OnDisable()
        {
            UnsubscribeAutoSave();
        }

        public GameSaveData Capture()
        {
            var data = new GameSaveData
            {
                gold = wallet != null ? wallet.Gold : 0
            };

            CaptureInventory(data);
            CaptureSkillTree(data);
            CaptureSkillLoadout(data);
            return data;
        }

        public void Restore(GameSaveData data)
        {
            if (data == null)
                return;

            _isApplyingSave = true;
            try
            {
                wallet?.SetGold(data.gold);
                RestoreInventory(data);
                RestoreSkillTree(data);
                RestoreSkillLoadout(data);
                skillBar?.Rebuild();
            }
            finally
            {
                _isApplyingSave = false;
            }
        }

        [ContextMenu("Save Game")]
        public void SaveToDisk()
        {
            var json = JsonUtility.ToJson(Capture(), true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"Saved game to {SavePath}", this);
        }

        [ContextMenu("Load Game")]
        public void LoadFromDisk()
        {
            LoadFromDisk(true);
        }

        [ContextMenu("Reset Progress")]
        public void ResetProgress()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            _isApplyingSave = true;
            try
            {
                wallet?.ResetToStartingGold();
                inventory?.RuntimeInventory.Clear();
                skillTree?.ResetProgress();
                skillLoadout?.ClearSlots();
                skillBar?.Rebuild();
            }
            finally
            {
                _isApplyingSave = false;
            }
            SaveToDisk();
            Debug.Log($"Reset progress and saved fresh state to {SavePath}", this);
        }

        private void LoadFromDisk(bool logMissingFile)
        {
            if (!File.Exists(SavePath))
            {
                if (logMissingFile)
                    Debug.LogWarning($"Save file not found: {SavePath}", this);
                return;
            }

            Restore(JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath)));
            Debug.Log($"Loaded game from {SavePath}", this);
        }

        private void CaptureInventory(GameSaveData data)
        {
            if (inventory == null || inventory.Inventory == null)
                return;

            var slots = inventory.Inventory.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                    continue;

                data.inventorySlots.Add(new InventorySlotSaveData
                {
                    slotIndex = i,
                    itemId = slot.Item.ItemId,
                    amount = slot.Amount
                });
            }
        }

        private void CaptureSkillTree(GameSaveData data)
        {
            if (skillTree == null)
                return;

            foreach (var nodeId in skillTree.Progress.UnlockedNodeIds)
                data.unlockedSkillTreeNodeIds.Add(nodeId);

            foreach (var pair in skillTree.Progress.NodeRanks)
            {
                data.skillTreeNodeRanks.Add(new SkillTreeNodeRankSaveData
                {
                    nodeId = pair.Key,
                    rank = pair.Value
                });
            }
        }

        private void CaptureSkillLoadout(GameSaveData data)
        {
            if (skillLoadout == null)
                return;

            foreach (var slot in skillLoadout.Slots)
            {
                if (slot == null || slot.Skill == null)
                    continue;

                data.skillBarSlots.Add(new SkillSlotSaveData
                {
                    slotIndex = slot.SlotIndex,
                    skillId = slot.Skill.SkillId
                });
            }
        }

        private void RestoreInventory(GameSaveData data)
        {
            if (inventory == null)
                return;

            var runtimeInventory = inventory.RuntimeInventory;
            runtimeInventory.Clear();
            foreach (var savedSlot in data.inventorySlots ?? new List<InventorySlotSaveData>())
            {
                if (savedSlot == null || string.IsNullOrWhiteSpace(savedSlot.itemId))
                    continue;
                if (itemDatabase == null || !itemDatabase.TryGetItem(savedSlot.itemId, out var item))
                    continue;

                runtimeInventory.TrySetSlot(savedSlot.slotIndex, item, savedSlot.amount);
            }
        }

        private void RestoreSkillTree(GameSaveData data)
        {
            if (skillTree == null)
                return;

            if (data.skillTreeNodeRanks != null && data.skillTreeNodeRanks.Count > 0)
            {
                var ranks = new List<KeyValuePair<string, int>>();
                foreach (var savedRank in data.skillTreeNodeRanks)
                {
                    if (savedRank == null || string.IsNullOrWhiteSpace(savedRank.nodeId) || savedRank.rank <= 0)
                        continue;
                    ranks.Add(new KeyValuePair<string, int>(savedRank.nodeId, savedRank.rank));
                }

                skillTree.ApplyNodeRanks(ranks);
                return;
            }

            // Saves created before node ranks existed are migrated as rank-one unlocks.
            skillTree.ApplyUnlockedNodes(data.unlockedSkillTreeNodeIds);
        }

        private void RestoreSkillLoadout(GameSaveData data)
        {
            if (skillLoadout == null)
                return;

            var bindings = new List<SkillSlotBinding>();
            foreach (var savedSlot in data.skillBarSlots ?? new List<SkillSlotSaveData>())
            {
                if (savedSlot == null || string.IsNullOrWhiteSpace(savedSlot.skillId))
                    continue;
                if (skillDatabase == null || !skillDatabase.TryGetSkill(savedSlot.skillId, out var skill))
                    continue;

                bindings.Add(new SkillSlotBinding(savedSlot.slotIndex, skill, string.Empty));
            }

            skillLoadout.SetSlots(bindings);
        }

        private void SubscribeAutoSave()
        {
            if (_subscribed || !autoSaveProgress)
                return;
            if (skillTree != null)
                skillTree.ProgressionCommitted += OnProgressionCommitted;
            if (skillLoadout != null)
                skillLoadout.LoadoutChanged += OnProgressionCommitted;
            _subscribed = true;
        }

        private void UnsubscribeAutoSave()
        {
            if (!_subscribed)
                return;
            if (skillTree != null)
                skillTree.ProgressionCommitted -= OnProgressionCommitted;
            if (skillLoadout != null)
                skillLoadout.LoadoutChanged -= OnProgressionCommitted;
            _subscribed = false;
        }

        private void OnProgressionCommitted()
        {
            if (!_isApplyingSave)
                SaveToDisk();
        }
    }
}
