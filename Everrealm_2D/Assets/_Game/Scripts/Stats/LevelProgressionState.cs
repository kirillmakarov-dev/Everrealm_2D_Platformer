using System;

namespace LetterHunter.Stats
{
    public sealed class LevelProgressionState
    {
        private readonly LevelProgressionDefinition _definition;
        public LevelProgressionState(LevelProgressionDefinition definition) =>
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        public int TotalExperience { get; private set; }
        public int Level => _definition.LevelAt(TotalExperience);
        public bool IsMaxLevel => Level >= _definition.MaxLevel;
        public int CurrentExperience => TotalExperience - _definition.Threshold(Level);
        public int RequiredExperience => IsMaxLevel ? 0 : _definition.Threshold(Level + 1) - _definition.Threshold(Level);
        public float Fill => IsMaxLevel ? 1f : (float)CurrentExperience / RequiredExperience;
        public void Restore(int experience) => TotalExperience = Math.Max(0, experience);
        public void Add(int amount)
        {
            if (amount > 0) TotalExperience = (int)Math.Min(int.MaxValue, (long)TotalExperience + amount);
        }
        public void SetLevel(int level) => Restore(_definition.Threshold(level));
    }
}
