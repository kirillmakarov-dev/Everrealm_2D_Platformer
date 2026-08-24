namespace LetterHunter.Feedback
{
    /// <summary>
    /// Visual-only category for floating combat text.
    /// Gameplay damage math stays in Combat; this enum only selects presentation.
    /// </summary>
    public enum FloatingDamageTextType
    {
        Normal = 0,
        Critical = 1,
        Skill = 2,
        Fire = 3,
        Ice = 4,
        Poison = 5,
        Heal = 6,
        Blocked = 7,
        Miss = 8
    }
}
