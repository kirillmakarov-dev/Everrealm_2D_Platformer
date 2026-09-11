using System;
using LetterHunter.Characters;
using LetterHunter.SkillTree;
using UnityEngine;

namespace LetterHunter.Stats
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerClassController))]
    public sealed class PlayerLevelProgression : MonoBehaviour
    {
        [SerializeField] private LevelProgressionDefinition definition;
        [SerializeField] private PlayerClassController player;
        [SerializeField] private PlayerSkillTreeController skillTree;
        private LevelProgressionState _state;
        public event Action Changed;
        public event Action<int, int> LevelIncreased;
        public LevelProgressionState State => _state ??= new LevelProgressionState(definition);
        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerClassController>();
            if (skillTree == null) skillTree = GetComponent<PlayerSkillTreeController>();
            if (definition == null) { Debug.LogError("Assign a Level Progression definition.", this); enabled = false; }
        }
        private void Start() => Publish();
        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            var previousLevel = State.Level;
            State.Add(amount);
            Publish();
            PublishLevelIncrease(previousLevel);
        }
        public void SetLevel(int level)
        {
            var previousLevel = State.Level;
            State.SetLevel(level);
            Publish();
            PublishLevelIncrease(previousLevel);
        }
        public void Restore(int experience) { State.Restore(experience); Publish(); }
        private void PublishLevelIncrease(int previousLevel)
        {
            var currentLevel = State.Level;
            if (currentLevel > previousLevel)
                LevelIncreased?.Invoke(previousLevel, currentLevel);
        }
        private void Publish()
        {
            if (definition == null || player?.Stats == null) return;
            player.Stats.SetLevel(State.Level);
            skillTree?.Service.NotifyExternalStateChanged();
            Changed?.Invoke();
        }
    }
}
