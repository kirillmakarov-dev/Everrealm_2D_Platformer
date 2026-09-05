using System.Collections.Generic;
using UnityEngine;

namespace LetterHunter.SkillTree
{
    /// <summary>English Kingdom-style shared conversion for normalized authored coordinates.</summary>
    public static class EverrealmSkillTreeLayout
    {
        public const float PreviewScale = .75f;
        public const float NodeWidth = 140f * PreviewScale;
        public const float NodeHeight = 150f * PreviewScale;
        public const float RuntimeNodeWidth = 140f;
        public const float RuntimeNodeHeight = 150f;
        public const float ColumnSpacing = 205f;
        public const float LegacyMinimumX = .18f;
        public const float LegacyXStep = .2f;
        public const float HorizontalPadding = 105f;
        public const float VerticalPadding = 72f;
        public const float LegacyMinimumY = .52f;
        public const float LegacyMaximumY = .86f;

        public readonly struct Metrics
        {
            public readonly float MinimumX, MinimumY, XScale, YScale;
            public readonly Vector2 ContentSize;
            public Metrics(float minimumX, float minimumY, float xScale, float yScale, Vector2 contentSize)
            { MinimumX = minimumX; MinimumY = minimumY; XScale = xScale; YScale = yScale; ContentSize = contentSize; }
        }

        public static Metrics Calculate(IReadOnlyList<SkillNodeDefinitionSO> nodes, float viewportWidth, float viewportHeight)
        {
            float maximumX = LegacyMinimumX, minimumY = LegacyMinimumY, maximumY = LegacyMaximumY;
            float minimumX = LegacyMinimumX;
            foreach (var node in nodes)
            {
                if (node == null) continue;
                minimumX = Mathf.Min(minimumX, node.UiPosition.x);
                maximumX = Mathf.Max(maximumX, node.UiPosition.x);
                minimumY = Mathf.Min(minimumY, node.UiPosition.y);
                maximumY = Mathf.Max(maximumY, node.UiPosition.y);
            }
            float xScale = ColumnSpacing / LegacyXStep;
            float baseHeight = Mathf.Max(viewportHeight, viewportHeight * 1.35f);
            float yScale = Mathf.Max(1f, (baseHeight - VerticalPadding * 2f) / (LegacyMaximumY - LegacyMinimumY));
            float width = Mathf.Max(viewportWidth,
                HorizontalPadding * 2f + (maximumX - minimumX) * xScale + RuntimeNodeWidth);
            float height = Mathf.Max(viewportHeight,
                VerticalPadding * 2f + (maximumY - minimumY) * yScale + RuntimeNodeHeight);
            return new Metrics(minimumX, minimumY, xScale, yScale, new Vector2(width, height));
        }

        public static Vector2 CalculateEditorCanvasSize(IReadOnlyList<SkillNodeDefinitionSO> nodes,
            float availableWidth, float availableHeight)
        {
            return CalculateEditorMetrics(nodes, availableWidth, availableHeight).ContentSize;
        }

        public static Metrics CalculateEditorMetrics(IReadOnlyList<SkillNodeDefinitionSO> nodes,
            float availableWidth, float availableHeight)
        {
            float minimumX = LegacyMinimumX;
            float minimumY = LegacyMinimumY;
            float maximumY = LegacyMaximumY;
            float maximumX = LegacyMinimumX;
            foreach (var node in nodes)
            {
                if (node == null) continue;
                minimumX = Mathf.Min(minimumX, node.UiPosition.x);
                maximumX = Mathf.Max(maximumX, node.UiPosition.x);
                minimumY = Mathf.Min(minimumY, node.UiPosition.y);
                maximumY = Mathf.Max(maximumY, node.UiPosition.y);
            }

            // Use a fixed game viewport so resizing the editor cannot stretch the authored graph.
            // Cards and spacing use the same scale as the game's zoomed-out view.
            var runtime = Calculate(nodes, 900f, 650f);
            float xScale = runtime.XScale * PreviewScale;
            float yScale = runtime.YScale * PreviewScale;
            float width = Mathf.Max(900f, HorizontalPadding * 2f +
                (maximumX - minimumX) * xScale + NodeWidth);
            float height = Mathf.Max(650f, VerticalPadding * 2f +
                (maximumY - minimumY) * yScale + NodeHeight);
            return new Metrics(minimumX, minimumY, xScale, yScale,
                new Vector2(Mathf.Max(width, availableWidth), height));
        }

        public static Vector2 ToCanvasPosition(Vector2 authored, Metrics metrics)
        {
            return new Vector2(HorizontalPadding + (authored.x - metrics.MinimumX) * metrics.XScale,
                VerticalPadding + (authored.y - metrics.MinimumY) * metrics.YScale);
        }

        public static Vector2 ToAuthoredPosition(Vector2 canvasPosition, Metrics metrics)
        {
            return new Vector2(metrics.MinimumX +
                    (canvasPosition.x - HorizontalPadding) / Mathf.Max(1f, metrics.XScale),
                metrics.MinimumY + (canvasPosition.y - VerticalPadding) / Mathf.Max(1f, metrics.YScale));
        }

        public static Vector2 ToEditorPosition(Vector2 authored, Rect canvas, Metrics metrics)
        {
            Vector2 position = ToCanvasPosition(authored, metrics);
            return canvas.position + new Vector2(position.x, canvas.height - position.y);
        }
    }
}
