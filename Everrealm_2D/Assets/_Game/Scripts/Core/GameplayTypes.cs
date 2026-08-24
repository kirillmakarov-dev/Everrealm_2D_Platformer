using System;

namespace LetterHunter.Core
{
    public enum CharacterClassType { Warrior, Ninja }

    public enum SkillType { Active, Buff, Passive, Empower, AutoAttackUpgrade }

    [Flags]
    public enum DamageTag
    {
        None = 0,
        AutoAttack = 1 << 0,
        Skill = 1 << 1,
        Melee = 1 << 2,
        Ranged = 1 << 3,
        Fire = 1 << 4,
        Throw = 1 << 5
    }

    public enum AttackShapeType { Circle, Box, DirectionalBox }
}
