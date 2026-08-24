using System.Collections.Generic;
using LetterHunter.Combat;
using LetterHunter.Core;
using UnityEngine;

namespace LetterHunter.Infrastructure
{
    public sealed class Physics2DTargetProvider : MonoBehaviour, ITargetProvider
    {
        [SerializeField] private LayerMask targetLayers = ~0;
        private readonly List<IDamageable> _results = new();
        private readonly HashSet<IDamageable> _unique = new();

        public IReadOnlyList<IDamageable> FindTargets(TargetingQuery query)
        {
            _results.Clear();
            _unique.Clear();
            var center = query.Origin + query.Direction * query.Shape.forwardOffset;
            Collider2D[] hits = query.Shape.type switch
            {
                AttackShapeType.Circle => Physics2D.OverlapCircleAll(center, query.Shape.radius, targetLayers),
                AttackShapeType.Box or AttackShapeType.DirectionalBox => Physics2D.OverlapBoxAll(center, query.Shape.size, 0f, targetLayers),
                _ => Physics2D.OverlapCircleAll(center, query.Shape.radius, targetLayers)
            };

            System.Array.Sort(hits, (a, b) => ((Vector2)a.transform.position - query.Origin).sqrMagnitude
                .CompareTo(((Vector2)b.transform.position - query.Origin).sqrMagnitude));
            foreach (var hit in hits)
            {
                var damageable = FindDamageable(hit);
                if (damageable == null || ReferenceEquals(damageable, query.Source) || !_unique.Add(damageable)) continue;
                _results.Add(damageable);
                if (_results.Count >= query.MaxTargets) break;
            }
            return _results;
        }

        private static IDamageable FindDamageable(Collider2D collider)
        {
            foreach (var behaviour in collider.GetComponentsInParent<MonoBehaviour>(true))
                if (behaviour is IDamageable damageable) return damageable;
            return null;
        }
    }
}
