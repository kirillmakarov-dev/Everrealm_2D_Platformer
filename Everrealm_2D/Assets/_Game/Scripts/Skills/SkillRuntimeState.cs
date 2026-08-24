namespace LetterHunter.Skills
{
    public readonly struct SkillRuntimeModifiers
    {
        public SkillRuntimeModifiers(float damageBonusPercent, float cooldownReductionPercent,
            float manaCostReductionPercent)
        {
            DamageBonusPercent = damageBonusPercent;
            CooldownReductionPercent = System.Math.Clamp(cooldownReductionPercent, 0f, 0.9f);
            ManaCostReductionPercent = System.Math.Clamp(manaCostReductionPercent, 0f, 0.9f);
        }

        public float DamageBonusPercent { get; }
        public float CooldownReductionPercent { get; }
        public float ManaCostReductionPercent { get; }
    }

    public readonly struct SkillRuntimeValues
    {
        public SkillRuntimeValues(SkillDefinition definition, SkillRuntimeModifiers modifiers)
        {
            ManaCost = definition == null ? 0f : definition.ManaCost * (1f - modifiers.ManaCostReductionPercent);
            Cooldown = definition == null ? 0f : definition.Cooldown * (1f - modifiers.CooldownReductionPercent);
            DamageMultiplier = definition == null
                ? 1f
                : System.Math.Max(0f, definition.BaseDamageMultiplier * (1f + modifiers.DamageBonusPercent));
            DamageLines = definition == null ? 1 : definition.DamageLines;
            MaxTargets = definition == null ? 1 : definition.MaxTargets;
        }

        public float ManaCost { get; }
        public float Cooldown { get; }
        public float DamageMultiplier { get; }
        public int DamageLines { get; }
        public int MaxTargets { get; }
    }

    public sealed class SkillRuntimeState
    {
        public float CooldownRemaining { get; private set; }
        public float DurationRemaining { get; private set; }
        public bool IsActive => DurationRemaining > 0f;
        public bool IsReady => CooldownRemaining <= 0f;
        public void StartCooldown(float duration) => CooldownRemaining = duration;
        public void StartDuration(float duration) => DurationRemaining = duration;
        public void Tick(float deltaTime)
        {
            if (CooldownRemaining > 0f) CooldownRemaining = System.Math.Max(0f, CooldownRemaining - deltaTime);
            if (DurationRemaining > 0f) DurationRemaining = System.Math.Max(0f, DurationRemaining - deltaTime);
        }
    }
}
