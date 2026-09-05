namespace LetterHunter.Combat
{
    public interface IExperienceReward { int ExperienceReward { get; } }
    public interface IExperienceRecipient { void AddExperience(int amount); }
}
