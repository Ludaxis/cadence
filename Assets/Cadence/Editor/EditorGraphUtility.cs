using UnityEditor;
using UnityEngine;

namespace Cadence.Editor
{
    public static class EditorGraphUtility
    {
        public static void DrawGrid(Rect area, int horizontalLines, int verticalLines,
            Color color)
        {
            var prevColor = Handles.color;
            Handles.color = color;

            // Horizontal lines
            for (int i = 0; i <= horizontalLines; i++)
            {
                float y = area.y + area.height * i / horizontalLines;
                Handles.DrawLine(new Vector3(area.x, y), new Vector3(area.xMax, y));
            }

            // Vertical lines
            for (int i = 0; i <= verticalLines; i++)
            {
                float x = area.x + area.width * i / verticalLines;
                Handles.DrawLine(new Vector3(x, area.y), new Vector3(x, area.yMax));
            }

            Handles.color = prevColor;
        }

        public static void DrawLineGraph(Rect area, float[] values, float minY, float maxY,
            Color color, float thickness = 2f)
        {
            if (values == null || values.Length < 2) return;

            var prevColor = Handles.color;
            Handles.color = color;

            var points = new Vector3[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                float x = area.x + area.width * i / (values.Length - 1);
                float t = Mathf.InverseLerp(minY, maxY, values[i]);
                float y = area.yMax - t * area.height; // Inverted Y
                points[i] = new Vector3(x, y, 0f);
            }

            Handles.DrawAAPolyLine(thickness, points);
            Handles.color = prevColor;
        }

        public static void DrawLineGraph(Rect area, float[] values, float minY, float maxY,
            Color[] perPointColors, float thickness = 2f)
        {
            if (values == null || values.Length < 2) return;
            if (perPointColors == null || perPointColors.Length != values.Length)
            {
                DrawLineGraph(area, values, minY, maxY, Color.white, thickness);
                return;
            }

            for (int i = 0; i < values.Length - 1; i++)
            {
                float x0 = area.x + area.width * i / (values.Length - 1);
                float x1 = area.x + area.width * (i + 1) / (values.Length - 1);
                float t0 = Mathf.InverseLerp(minY, maxY, values[i]);
                float t1 = Mathf.InverseLerp(minY, maxY, values[i + 1]);
                float y0 = area.yMax - t0 * area.height;
                float y1 = area.yMax - t1 * area.height;

                Handles.color = perPointColors[i];
                Handles.DrawAAPolyLine(thickness,
                    new Vector3(x0, y0), new Vector3(x1, y1));
            }
        }

        public static void DrawHorizontalLine(Rect area, float value, float minY, float maxY,
            Color color, float thickness = 1f)
        {
            float t = Mathf.InverseLerp(minY, maxY, value);
            float y = area.yMax - t * area.height;

            var prevColor = Handles.color;
            Handles.color = color;
            Handles.DrawAAPolyLine(thickness,
                new Vector3(area.x, y), new Vector3(area.xMax, y));
            Handles.color = prevColor;
        }

        public static void DrawPoint(Rect area, int index, int totalPoints, float value,
            float minY, float maxY, Color color, float radius = 4f)
        {
            float x = area.x + area.width * index / Mathf.Max(1, totalPoints - 1);
            float t = Mathf.InverseLerp(minY, maxY, value);
            float y = area.yMax - t * area.height;

            var prevColor = Handles.color;
            Handles.color = color;
            Handles.DrawSolidDisc(new Vector3(x, y, 0f), Vector3.forward, radius);
            Handles.color = prevColor;
        }

        /// <summary>
        /// Draws a horizontal strip of colored rectangles (one per data point) for heatmap rows.
        /// </summary>
        public static void DrawColorStrip(Rect area, Color[] colors, float borderWidth = 0.5f)
        {
            if (colors == null || colors.Length == 0) return;

            float cellWidth = area.width / colors.Length;
            for (int i = 0; i < colors.Length; i++)
            {
                var rect = new Rect(area.x + i * cellWidth, area.y,
                    cellWidth - borderWidth, area.height);
                EditorGUI.DrawRect(rect, colors[i]);
            }
        }
    }
}
