using System;
using System.Collections.Generic;
using LetterHunter.Core;
using UnityEngine;

namespace LetterHunter.Combat
{
    [Serializable]
    public struct AttackShape
    {
        public AttackShapeType type;
        public Vector2 size;
        public float radius;
        public float forwardOffset;

        public static AttackShape MeleeDefault => new() { type = AttackShapeType.DirectionalBox, size = new Vector2(1.5f, 1f), radius = 1.25f, forwardOffset = 0.75f };
    }

    public readonly struct TargetingQuery
    {
        public TargetingQuery(Vector2 origin, Vector2 direction, AttackShape shape, int maxTargets, ICombatActor source)
        { Origin = origin; Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right; Shape = shape; MaxTargets = Math.Max(1, maxTargets); Source = source; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public AttackShape Shape { get; }
        public int MaxTargets { get; }
        public ICombatActor Source { get; }
    }

    public interface ITargetProvider
    {
        IReadOnlyList<IDamageable> FindTargets(TargetingQuery query);
    }
}
