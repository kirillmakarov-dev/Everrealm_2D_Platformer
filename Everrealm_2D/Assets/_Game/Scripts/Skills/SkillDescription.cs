using LetterHunter.Core;

namespace LetterHunter.Skills
{
    public static class SkillDescription
    {
        public static string Build(SkillDefinition skill)
        {
            if (skill == null) return "No skill assigned.";
            string type = skill.SkillType switch
            {
                SkillType.Passive => "Passive — works after learning",
                SkillType.AutoAttackUpgrade => "Passive — auto attack upgrade",
                SkillType.Buff => "Active — temporary buff",
                SkillType.Empower => "Active — empowers next auto attack",
                _ => "Active — damage skill"
            };
            string effect = skill.Effect != null ? skill.Effect.Describe(skill) : "No effect assigned.";
            bool passive = skill.SkillType is SkillType.Passive or SkillType.AutoAttackUpgrade;
            return type + "\n" + effect + (passive ? "\nNot assigned to the skill bar." :
                $"\nMana: {skill.ManaCost:0.##}  |  Cooldown: {skill.Cooldown:0.##} s");
        }
    }
}
