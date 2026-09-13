using System.Collections.Generic;
using LetterHunter.SkillTree;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.UI.SkillTree
{
    [DisallowMultipleComponent]
    public sealed class EverrealmSkillTreeConnectionVisual : MaskableGraphic
    {
        private const int CapSegments = 12;
        private const float CoreRadius = 2.4f;
        private const float OutlineRadius = 3.8f;
        private const float GlowRadius = 6.2f;
        private static readonly Color LockedTint = new(.43f, .72f, .88f, .96f);
        private static readonly Color AvailableTint = new(1f, .7f, .34f, .98f);
        private static readonly Color PurchasedTint = new(.4f, .91f, .69f, .98f);

        [SerializeField, Min(0f)] private float _edgeSoftness = 1.25f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            color = Color.white;
        }

        public void Render(Vector2 start, Vector2 end, SkillTreeNodeState state)
        {
            var rect = (RectTransform)transform;
            var direction = end - start;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0f, .5f);
            rect.anchoredPosition = start;
            rect.sizeDelta = new Vector2(direction.magnitude, GlowRadius * 2f + _edgeSoftness * 2f);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            _tint = state switch
            {
                SkillTreeNodeState.Purchased => PurchasedTint,
                SkillTreeNodeState.Available => AvailableTint,
                _ => LockedTint
            };
            SetVerticesDirty();
        }

        private Color _tint = LockedTint;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var length = rect.width;
            if (length <= 0f)
                return;

            var centerY = rect.center.y;
            var halo = WithAlpha(_tint, .16f);
            var rim = new Color(.018f, .04f, .065f, .88f);
            var highlight = Color.Lerp(_tint, Color.white, .68f);
            highlight.a = .52f;

            AddCapsuleFill(vertexHelper, length, centerY, GlowRadius, halo);
            AddCapsuleRing(vertexHelper, length, centerY, GlowRadius + _edgeSoftness,
                GlowRadius, WithAlpha(halo, 0f), halo);
            AddCapsuleFill(vertexHelper, length, centerY, OutlineRadius, rim);
            AddCapsuleRing(vertexHelper, length, centerY, CoreRadius + .9f,
                CoreRadius, WithAlpha(_tint, .12f), _tint);
            AddCapsuleFill(vertexHelper, length, centerY, CoreRadius, _tint);
            AddCapsuleFill(vertexHelper, length, centerY + .85f, .55f, highlight);
        }

        private static void AddCapsuleFill(VertexHelper vh, float length, float centerY,
            float radius, Color fillColor)
        {
            radius = Mathf.Min(radius, length * .5f);
            var points = CapsulePoints(length, centerY, radius);
            var centerIndex = vh.currentVertCount;
            vh.AddVert(new Vector3(length * .5f, centerY), fillColor, Vector2.zero);
            for (var i = 0; i < points.Count; i++)
                vh.AddVert(points[i], fillColor, Vector2.zero);

            for (var i = 0; i < points.Count; i++)
                vh.AddTriangle(centerIndex, centerIndex + 1 + i,
                    centerIndex + 1 + (i + 1) % points.Count);
        }

        private static void AddCapsuleRing(VertexHelper vh, float length, float centerY,
            float outerRadius, float innerRadius, Color outerColor, Color innerColor)
        {
            outerRadius = Mathf.Min(outerRadius, length * .5f);
            innerRadius = Mathf.Min(innerRadius, outerRadius);
            var outer = CapsulePoints(length, centerY, outerRadius);
            var inner = CapsulePoints(length, centerY, innerRadius);
            var first = vh.currentVertCount;
            for (var i = 0; i < outer.Count; i++)
            {
                vh.AddVert(outer[i], outerColor, Vector2.zero);
                vh.AddVert(inner[i], innerColor, Vector2.zero);
            }

            for (var i = 0; i < outer.Count; i++)
            {
                var next = (i + 1) % outer.Count;
                var outerA = first + i * 2;
                var innerA = outerA + 1;
                var outerB = first + next * 2;
                var innerB = outerB + 1;
                vh.AddTriangle(outerA, outerB, innerA);
                vh.AddTriangle(innerA, outerB, innerB);
            }
        }

        private static List<Vector3> CapsulePoints(float length, float centerY, float radius)
        {
            var points = new List<Vector3>((CapSegments + 1) * 2);
            var rightCenter = length - radius;
            var leftCenter = radius;
            for (var i = 0; i <= CapSegments; i++)
            {
                var angle = (-90f + 180f * i / CapSegments) * Mathf.Deg2Rad;
                points.Add(new Vector3(rightCenter + Mathf.Cos(angle) * radius,
                    centerY + Mathf.Sin(angle) * radius));
            }

            for (var i = 0; i <= CapSegments; i++)
            {
                var angle = (90f + 180f * i / CapSegments) * Mathf.Deg2Rad;
                points.Add(new Vector3(leftCenter + Mathf.Cos(angle) * radius,
                    centerY + Mathf.Sin(angle) * radius));
            }

            return points;
        }

        private static Color WithAlpha(Color value, float alpha)
        {
            value.a = alpha;
            return value;
        }
    }
}
