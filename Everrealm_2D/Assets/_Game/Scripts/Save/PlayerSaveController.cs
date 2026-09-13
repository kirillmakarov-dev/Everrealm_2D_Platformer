using System.Collections.Generic;
using System.IO;
using LetterHunter.Core;
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
        [SerializeField] private LetterHunter.Stats.PlayerLevelProgression progression;

        [Header("File")]
        [SerializeField] private string saveFileName = "letter-hunter-save.json";
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool autoSaveProgress = true;

        [Header("Inspector Debug Tools")]
        [Min(1), SerializeField] private int debugGoldAmount = 100;

        private bool _started;
        private bool _subscribed;
        private bool _isApplyingSave;

        private void Awake()
        {
            if (progression == null) progression = GetComponent<LetterHunter.Stats.PlayerLevelProgression>();
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
        public PlayerSkillTreeController SkillTreeController => skillTree;
        public int CurrentGold => wallet != null ? wallet.Gold : 0;
        public int CurrentLevel => progression != null ? progression.State.Level : skillTree != null ? skillTree.CurrentLevel : 1;

        public void SetDebugLevel(int level)
        {
            if (progression == null) { Debug.LogError("Player Level Progression is missing.", this); return; }
            ApplyWithoutAutoSave(() => progression.SetLevel(level));
            SaveToDisk();
        }

        public void AddDebugGold(int amount)
        {
            if (wallet == null || amount <= 0) return;
            ApplyWithoutAutoSave(() => wallet.AddGold(amount));
            SaveToDisk();
        }

        public bool TryDebugSkill(SkillNodeDefinitionSO node, bool freeGrant, out string message)
        {
            message = "Enter Play Mode with a ready player.";
            if (!Application.isPlaying || skillTree == null || skillTree.Player?.SkillService == null) return false;
            bool success = false;
            string failure = null;
            ApplyWithoutAutoSave(() =>
            {
                if (freeGrant) success = skillTree.TryGrantNodeForDebug(node, out failure);
                else
                {
                    var result = skillTree.TryPurchase(node);
                    success = result.Success;
                    failure = result.Failure.ToString();
                }
            });
            if (success) SaveToDisk();
            message = success ? $"{node.DisplayName}: {(freeGrant ? "granted with parents" : "purchased")} and saved." : failure;
            return success;
        }

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

        private void OnApplicationPause(bool paused)
        {
            if (paused && _started && autoSaveProgress && !_isApplyingSave)
                SaveToDisk();
        }

        private void OnApplicationQuit()
        {
            if (_started && autoSaveProgress && !_isApplyingSave)
                SaveToDisk();
        }

        public GameSaveData Capture()
        {
            var data = new GameSaveData
            {
                gold = wallet != null ? wallet.Gold : 0,
                level = CurrentLevel,
                progressionVersion = progression != null ? 1 : 0,
                totalExperience = progression != null ? progression.State.TotalExperience : 0
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
                if (progression != null)
                {
                    if (data.progressionVersion >= 1) progression.Restore(data.totalExperience);
                    else progression.SetLevel(Mathf.Max(1, data.level));
                }
                else if (skillTree?.Player?.Stats != null)
                    skillTree.Player.Stats.SetLevel(Mathf.Max(1, data.level));
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
            var directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
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

            ApplyWithoutAutoSave(() =>
            {
                wallet?.ResetToStartingGold();
                progression?.Restore(0);
                inventory?.ResetToStartingItems();
                ResetSkillsRuntime();
            });
            SaveToDisk();
            Debug.Log($"Reset progress and saved fresh state to {SavePath}", this);
        }

        [ContextMenu("Reset Skills Only")]
        public void ResetSkills()
        {
            ApplyWithoutAutoSave(ResetSkillsRuntime);
            SaveToDisk();
        }

        [ContextMenu("Reset Inventory Only")]
        public void ResetInventory()
        {
            ApplyWithoutAutoSave(() => inventory?.ResetToStartingItems());
            SaveToDisk();
        }

        [ContextMenu("Reset Gold Only")]
        public void ResetGold()
        {
            ApplyWithoutAutoSave(() => wallet?.ResetToStartingGold());
            SaveToDisk();
        }

        [ContextMenu("Add Debug Gold")]
        public void AddDebugGold()
        {
            AddDebugGold(Mathf.Max(1, debugGoldAmount));
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

            data.skillTree = new SkillTreeSaveBlock
            {
                schemaVersion = 1,
                activeProfessionId = skillTree.ActiveProfession != null
                    ? skillTree.ActiveProfession.ProfessionId
                    : string.Empty
            };
            foreach (var purchased in skillTree.Progress.EnumeratePurchased())
            {
                data.skillTree.purchasedNodes.Add(new PurchasedSkillNodeSaveData
                {
                    professionId = purchased.ProfessionId,
                    nodeId = purchased.NodeId
                });
            }
        }

        private void CaptureSkillLoadout(GameSaveData data)
        {
            if (skillLoadout == null)
                return;

            foreach (var slot in skillLoadout.Slots)
            {
                if (slot == null)
                    continue;

                data.skillBarSlots.Add(new SkillSlotSaveData
                {
                    slotIndex = slot.SlotIndex,
                    skillId = slot.Skill != null ? slot.Skill.SkillId : string.Empty,
                    isExplicitEmpty = slot.Skill == null
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

            if (data.skillTree != null && data.skillTree.schemaVersion >= 1)
            {
                var purchased = new List<PurchasedSkillNode>();
                foreach (var savedNode in data.skillTree.purchasedNodes ?? new List<PurchasedSkillNodeSaveData>())
                {
                    if (savedNode == null || string.IsNullOrWhiteSpace(savedNode.professionId) ||
                        string.IsNullOrWhiteSpace(savedNode.nodeId))
                        continue;
                    purchased.Add(new PurchasedSkillNode(savedNode.professionId, savedNode.nodeId));
                }
                skillTree.RestoreProgress(data.skillTree.activeProfessionId, purchased);
                return;
            }

            var legacyIds = new List<string>();
            if (data.skillTreeNodeRanks != null)
                foreach (var savedRank in data.skillTreeNodeRanks)
                    if (savedRank != null && savedRank.rank > 0 && !string.IsNullOrWhiteSpace(savedRank.nodeId))
                        legacyIds.Add(savedRank.nodeId);
            if (legacyIds.Count == 0 && data.unlockedSkillTreeNodeIds != null)
                legacyIds.AddRange(data.unlockedSkillTreeNodeIds);
            skillTree.RestoreLegacyNodes(legacyIds);
        }

        private void RestoreSkillLoadout(GameSaveData data)
        {
            if (skillLoadout == null)
                return;

            var bindings = new List<SkillSlotBinding>();
            foreach (var savedSlot in data.skillBarSlots ?? new List<SkillSlotSaveData>())
            {
                if (savedSlot == null)
                    continue;
                if (savedSlot.isExplicitEmpty)
                {
                    bindings.Add(new SkillSlotBinding(savedSlot.slotIndex, null, string.Empty));
                    continue;
                }
                if (string.IsNullOrWhiteSpace(savedSlot.skillId))
                    continue;
                if (skillDatabase == null || !skillDatabase.TryGetSkill(savedSlot.skillId, out var skill))
                    continue;
                if (skill.SkillType is SkillType.Passive or SkillType.AutoAttackUpgrade)
                    continue;
                if (skillTree != null && !skillTree.IsSkillUnlocked(skill))
                    continue;

                bindings.Add(new SkillSlotBinding(savedSlot.slotIndex, skill, string.Empty));
            }

            skillLoadout.SetSlots(bindings);
        }

        private void SubscribeAutoSave()
        {
            if (_subscribed || !autoSaveProgress)
                return;
            if (wallet != null)
                wallet.GoldChanged += OnGoldChanged;
            if (progression != null) progression.Changed += OnProgressionCommitted;
            if (inventory != null)
                inventory.InventoryChanged += OnProgressionCommitted;
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
            if (wallet != null)
                wallet.GoldChanged -= OnGoldChanged;
            if (progression != null) progression.Changed -= OnProgressionCommitted;
            if (inventory != null)
                inventory.InventoryChanged -= OnProgressionCommitted;
            if (skillTree != null)
                skillTree.ProgressionCommitted -= OnProgressionCommitted;
            if (skillLoadout != null)
                skillLoadout.LoadoutChanged -= OnProgressionCommitted;
            _subscribed = false;
        }

        private void OnProgressionCommitted()
        {
            if (!_isApplyingSave && (skillTree == null || !skillTree.IsChangingProgress))
                SaveToDisk();
        }

        private void OnGoldChanged(int _)
        {
            OnProgressionCommitted();
        }

        private void ResetSkillsRuntime()
        {
            skillTree?.ResetProgress();
            skillLoadout?.ClearSlots();
            skillBar?.Rebuild();
        }

        private void ApplyWithoutAutoSave(System.Action action)
        {
            _isApplyingSave = true;
            try
            {
                action?.Invoke();
            }
            finally
            {
                _isApplyingSave = false;
            }
        }
    }
}
