using UnityEngine;

namespace LetterHunter.Stats
{
    [CreateAssetMenu(menuName = "Everrealm/Stats/Level Progression")]
    public sealed class LevelProgressionDefinition : ScriptableObject
    {
        [Tooltip("Cumulative XP required for each level. Index 0 is level 1.")]
        [SerializeField] private int[] levelThresholds = { 0, 150, 300, 500, 750, 1050, 1400, 1800, 2250, 2750 };
        public int MaxLevel => Mathf.Max(1, levelThresholds?.Length ?? 0);
        public int Threshold(int level)
        {
            int result = 0;
            for (int i = 1; i < Mathf.Clamp(level, 1, MaxLevel); i++)
                result = (int)System.Math.Min(int.MaxValue, System.Math.Max((long)result + 1, levelThresholds[i]));
            return result;
        }
        public int LevelAt(int totalExperience)
        {
            int level = 1;
            while (level < MaxLevel && totalExperience >= Threshold(level + 1)) level++;
            return level;
        }
        private void OnValidate()
        {
            if (levelThresholds == null || levelThresholds.Length == 0) levelThresholds = new[] { 0 };
            levelThresholds[0] = 0;
            for (int i = 1; i < levelThresholds.Length; i++)
                levelThresholds[i] = Threshold(i + 1);
        }
    }
}
