using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Deckscent
{
    /// <summary>
    /// Toile de dessin 2D « à la main » pour l'UI gravure (équivalent du _draw de Godot) :
    /// rectangles, traits, pointillés, polygones, cercles, hachures. On vide la toile
    /// (<see cref="Clear"/>), on dessine, puis <see cref="Commit"/> reconstruit le maillage.
    ///
    /// Coordonnées en unités du canvas, origine en HAUT à gauche du rectangle, y vers le bas
    /// (comme Godot), pour porter le dessin tel quel.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class InkCanvas : MaskableGraphic
    {
        readonly List<UIVertex> _verts = new List<UIVertex>();
        readonly List<int> _indices = new List<int>();

        public Vector2 Size => rectTransform.rect.size;

        public void Clear()
        {
            _verts.Clear();
            _indices.Clear();
        }

        public void Commit() => SetVerticesDirty();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_verts.Count > 0)
                vh.AddUIVertexStream(_verts, _indices);
        }

        Vector3 ToLocal(Vector2 p)
        {
            var r = rectTransform.rect;
            return new Vector3(r.xMin + p.x, r.yMax - p.y, 0f);
        }

        int Vertex(Vector2 p, Color color)
        {
            var v = UIVertex.simpleVert;
            v.position = ToLocal(p);
            v.color = color;
            _verts.Add(v);
            return _verts.Count - 1;
        }

        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            var i = Vertex(a, color);
            Vertex(b, color);
            Vertex(c, color);
            Vertex(d, color);
            _indices.Add(i); _indices.Add(i + 1); _indices.Add(i + 2);
            _indices.Add(i); _indices.Add(i + 2); _indices.Add(i + 3);
        }

        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            var i = Vertex(a, color);
            Vertex(b, color);
            Vertex(c, color);
            _indices.Add(i); _indices.Add(i + 1); _indices.Add(i + 2);
        }

        public void Rect(Rect r, Color color) =>
            Quad(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), color);

        public void RectOutline(Rect r, Color color, float width)
        {
            Line(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), color, width, true);
            Line(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), color, width, true);
            Line(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), color, width, true);
            Line(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), color, width, true);
        }

        /// <summary>Trait d'épaisseur `width`. `square` : prolongé d'une demi-épaisseur (angles nets).</summary>
        public void Line(Vector2 a, Vector2 b, Color color, float width = 1f, bool square = false)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f)
                return;
            d.Normalize();
            if (square)
            {
                a -= d * width * 0.5f;
                b += d * width * 0.5f;
            }
            var n = new Vector2(-d.y, d.x) * (width * 0.5f);
            Quad(a + n, b + n, b - n, a - n, color);
        }

        public void DashedLine(Vector2 a, Vector2 b, Color color, float width, float dash)
        {
            var length = Vector2.Distance(a, b);
            var dir = (b - a) / Mathf.Max(length, 1e-4f);
            for (var s = 0f; s < length; s += dash * 2f)
                Line(a + dir * s, a + dir * Mathf.Min(s + dash, length), color, width);
        }

        /// <summary>Polygone convexe (ou en étoile autour du premier point), rempli.</summary>
        public void Polygon(IList<Vector2> points, Color color)
        {
            if (points.Count < 3)
                return;
            var first = _verts.Count;
            foreach (var p in points)
                Vertex(p, color);
            for (var k = 1; k < points.Count - 1; k++)
            {
                _indices.Add(first);
                _indices.Add(first + k);
                _indices.Add(first + k + 1);
            }
        }

        public void Polyline(IList<Vector2> points, Color color, float width, bool closed)
        {
            for (var k = 0; k < points.Count - 1; k++)
                Line(points[k], points[k + 1], color, width);
            if (closed && points.Count > 2)
                Line(points[points.Count - 1], points[0], color, width);
        }

        public void Circle(Vector2 center, float radius, Color color, int segments = 20)
        {
            var pts = new Vector2[segments];
            for (var k = 0; k < segments; k++)
            {
                var a = k * Mathf.PI * 2f / segments;
                pts[k] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            Polygon(pts, color);
        }

        /// <summary>Arc de cercle (angles en radians, y vers le bas).</summary>
        public void Arc(Vector2 center, float radius, float from, float to, Color color, float width, int segments = 16)
        {
            var pts = new Vector2[segments + 1];
            for (var k = 0; k <= segments; k++)
            {
                var a = Mathf.Lerp(from, to, k / (float)segments);
                pts[k] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            Polyline(pts, color, width, false);
        }

        /// <summary>
        /// Hachures « / » à 45° dans un rectangle, calées sur la toile : deux rectangles voisins ont
        /// des traits qui se prolongent.
        /// </summary>
        public void Hatch(Rect r, float spacing, Color color, float width = 1f)
        {
            // Traits x + y = c.
            var c = Mathf.Ceil((r.xMin + r.yMin) / spacing) * spacing;
            for (; c <= r.xMax + r.yMax; c += spacing)
            {
                var x0 = Mathf.Max(r.xMin, c - r.yMax);
                var x1 = Mathf.Min(r.xMax, c - r.yMin);
                if (x1 > x0)
                    Line(new Vector2(x0, c - x0), new Vector2(x1, c - x1), color, width);
            }
        }

        /// <summary>Hachures « / » à 45° dans un polygone convexe (même calage que <see cref="Hatch"/>).</summary>
        public void HatchPolygon(IList<Vector2> points, float spacing, Color color, float width = 1f)
        {
            if (points.Count < 3)
                return;
            float min = float.MaxValue, max = float.MinValue;
            foreach (var p in points)
            {
                min = Mathf.Min(min, p.x + p.y);
                max = Mathf.Max(max, p.x + p.y);
            }
            for (var c = Mathf.Ceil(min / spacing) * spacing; c <= max; c += spacing)
            {
                // Points où le trait x + y = c coupe les bords ; on garde les deux extrêmes.
                Vector2 from = default, to = default;
                var found = 0;
                for (var k = 0; k < points.Count; k++)
                {
                    var a = points[k];
                    var b = points[(k + 1) % points.Count];
                    var fa = a.x + a.y - c;
                    var fb = b.x + b.y - c;
                    if ((fa > 0f) == (fb > 0f) || Mathf.Approximately(fa, fb))
                        continue;
                    var hit = Vector2.Lerp(a, b, fa / (fa - fb));
                    if (found == 0 || hit.x < from.x)
                        from = hit;
                    if (found == 0 || hit.x > to.x)
                        to = hit;
                    found++;
                }
                if (found >= 2 && to.x - from.x > 0.01f)
                    Line(from, to, color, width);
            }
        }
    }
}
