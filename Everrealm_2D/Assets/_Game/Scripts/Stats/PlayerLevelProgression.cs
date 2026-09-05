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
        public LevelProgressionState State => _state ??= new LevelProgressionState(definition);
        private void Awake()
        {
            if (player == null) player = GetComponent<PlayerClassController>();
            if (skillTree == null) skillTree = GetComponent<PlayerSkillTreeController>();
            if (definition == null) { Debug.LogError("Assign a Level Progression definition.", this); enabled = false; }
        }
        private void Start() => Publish();
        public void AddExperience(int amount) { if (amount <= 0) return; State.Add(amount); Publish(); }
        public void SetLevel(int level) { State.SetLevel(level); Publish(); }
        public void Restore(int experience) { State.Restore(experience); Publish(); }
        private void Publish()
        {
            if (definition == null || player?.Stats == null) return;
            player.Stats.SetLevel(State.Level);
            skillTree?.Service.NotifyExternalStateChanged();
            Changed?.Invoke();
        }
    }
}
