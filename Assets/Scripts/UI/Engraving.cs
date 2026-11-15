using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Primitives de dessin « gravure » partagées par l'UI (écran de carte, plus tard le menu) :
    /// cadres, filets, losanges, bandeaux, cartes à jouer, crânes, flamme, étoile, rose des vents.
    /// Elles dessinent sur une <see cref="InkCanvas"/> (et un <see cref="TextPool"/> pour le texte).
    /// </summary>
    public static class Engraving
    {
        /// <summary>Taille d'une carte de la main.</summary>
        public static readonly Vector2 CardSize = new Vector2(112f, 172f);
        /// <summary>Écart des hachures des murs dans le plan d'une carte.</summary>
        public const float CardHatch = 3f;

        public static Color Fade(Color ink, Color paper, float amount) => Color.Lerp(ink, paper, amount);

        public static void Diamond(InkCanvas c, Vector2 center, float r, Color color) =>
            c.Polygon(new[] { center + new Vector2(0, -r), center + new Vector2(r, 0), center + new Vector2(0, r), center + new Vector2(-r, 0) }, color);

        /// <summary>Double filet : trait épais à l'extérieur, fin à l'intérieur, à `gap` unités, losanges aux coins.</summary>
        public static void DoubleFrame(InkCanvas c, Rect r, float gap, Color ink)
        {
            var outer = new Rect(r.x - gap, r.y - gap, r.width + gap * 2f, r.height + gap * 2f);
            c.RectOutline(outer, ink, 2.5f);
            c.RectOutline(r, ink, 1f);
            foreach (var corner in Corners(r))
                Diamond(c, corner, gap * 0.9f, ink);
        }

        /// <summary>Filet horizontal avec un losange au milieu.</summary>
        public static void Rule(InkCanvas c, Vector2 start, float width, Color ink)
        {
            var mid = start + new Vector2(width * 0.5f, 0f);
            c.Line(start, mid - new Vector2(9f, 0f), ink);
            c.Line(mid + new Vector2(9f, 0f), start + new Vector2(width, 0f), ink);
            Diamond(c, mid, 4f, ink);
        }

        /// <summary>
        /// Cadre d'une carte : ombre portée (plus longue si la carte est soulevée), fond papier,
        /// bordure, filet intérieur à losanges. Renvoie le rectangle intérieur.
        /// </summary>
        public static Rect CardFrame(InkCanvas c, Rect r, Color paper, Color border, float borderWidth, Color accent, Color ink, float lift)
        {
            c.Rect(new Rect(r.x + 3f, r.y + 3f + lift * 0.5f, r.width, r.height), new Color(ink.r, ink.g, ink.b, 0.22f));
            c.Rect(r, paper);
            c.RectOutline(r, border, borderWidth);
            var inner = new Rect(r.x + 6f, r.y + 6f, r.width - 12f, r.height - 12f);
            c.RectOutline(inner, accent, 1f);
            foreach (var corner in Corners(inner))
                Diamond(c, corner, 3.5f, accent);
            return inner;
        }

        /// <summary>Carré de l'illustration d'une carte (le plan de la tuile).</summary>
        public static Rect CardArtRect(Rect inner)
        {
            var cell = Mathf.Floor((inner.width - 26f) / 3f);
            return new Rect(Mathf.Floor(inner.center.x - cell * 1.5f), inner.y + 26f, cell * 3f, cell * 3f);
        }

        /// <summary>Bandeau du nom, sous l'illustration.</summary>
        public static Rect CardBandRect(Rect inner, Rect art) => new Rect(inner.x - 4f, art.yMax + 10f, inner.width + 8f, 20f);

        /// <summary>Bandeau à queues d'aronde, texte centré (rapetissé s'il est trop long).</summary>
        public static void Banner(InkCanvas c, TextPool texts, Rect r, string text, Color paper, Color ink, int size = 13)
        {
            const float notch = 6f;
            var m = r.center.y;
            var shape = new[]
            {
                new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax - notch, m),
                new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), new Vector2(r.xMin + notch, m),
            };
            // Rempli en deux moitiés convexes (la forme a deux encoches).
            c.Polygon(new[] { shape[0], shape[1], shape[2], new Vector2(r.center.x, m), shape[5] }, paper);
            c.Polygon(new[] { shape[5], new Vector2(r.center.x, m), shape[2], shape[3], shape[4] }, paper);
            c.Polyline(shape, ink, 1.2f, true);
            var fit = size;
            while (fit > 8 && TextPool.Width(text, fit, FontStyle.Bold) > r.width - notch * 2f - 4f)
                fit--;
            texts.Label(text, r.xMin, r.yMin + (r.height - fit * 1.4f) * 0.5f, r.width, fit, ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Crâne gravé (danger) : calotte, mâchoire, orbites. Plein = encre, sinon contour pâle.</summary>
        public static void Skull(InkCanvas c, Vector2 center, float s, bool filled, Color ink, Color paper)
        {
            var top = center + new Vector2(0f, -s * 0.12f);
            var jaw = new Rect(center.x - s * 0.26f, center.y + s * 0.12f, s * 0.52f, s * 0.3f);
            if (!filled)
            {
                var pale = Fade(ink, paper, 0.7f);
                c.Arc(top, s * 0.42f, Mathf.PI * 0.82f, Mathf.PI * 2.18f, pale, 1f);
                c.RectOutline(jaw, pale, 1f);
                return;
            }
            c.Circle(top, s * 0.42f, ink);
            c.Rect(jaw, ink);
            c.Circle(top + new Vector2(-s * 0.17f, s * 0.06f), s * 0.12f, paper, 10);
            c.Circle(top + new Vector2(s * 0.17f, s * 0.06f), s * 0.12f, paper, 10);
            for (var k = 1; k <= 3; k++)
            {
                var x = jaw.xMin + k * jaw.width / 4f;
                c.Line(new Vector2(x, jaw.yMin + 1f), new Vector2(x, jaw.yMax), paper);
            }
        }

        /// <summary>Flamme : une goutte pointée vers le haut, encre avec un cœur papier.</summary>
        public static void Flame(InkCanvas c, Vector2 center, float s, Color ink, Color paper)
        {
            Drop(c, center, s, ink);
            Drop(c, center + new Vector2(0f, s * 0.25f), s * 0.45f, paper);
        }

        static void Drop(InkCanvas c, Vector2 center, float s, Color color)
        {
            var pts = new Vector2[14];
            pts[0] = center + new Vector2(0f, -s);
            var body = center + new Vector2(0f, s * 0.3f);
            for (var k = 0; k < 13; k++)
            {
                var a = (-30f + k * 20f) * Mathf.Deg2Rad;
                pts[k + 1] = body + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * 0.55f;
            }
            c.Polygon(pts, color);
        }

        /// <summary>Étoile à 4 branches (repère du départ).</summary>
        public static void Star(InkCanvas c, Vector2 center, float r, Color color)
        {
            for (var k = 0; k < 4; k++)
            {
                var a = k * Mathf.PI / 2f - Mathf.PI / 2f;
                var tip = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                var side = new Vector2(Mathf.Cos(a + Mathf.PI / 2f), Mathf.Sin(a + Mathf.PI / 2f)) * r * 0.3f;
                c.Triangle(center + side, tip, center - side, color);
            }
        }

        /// <summary>Rose des vents : étoile à 8 branches mi-encre mi-papier, et le N.</summary>
        public static void Compass(InkCanvas c, TextPool texts, Vector2 center, float r, Color ink, Color paper)
        {
            c.Arc(center, r * 0.72f, 0f, Mathf.PI * 2f, ink, 1f, 32);
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f - Mathf.PI / 2f;
                var length = k % 2 == 0 ? r : r * 0.55f;
                var tip = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * length;
                var side = new Vector2(Mathf.Cos(a + Mathf.PI / 2f), Mathf.Sin(a + Mathf.PI / 2f)) * r * 0.13f;
                c.Triangle(center, center + side, tip, ink);
                c.Triangle(center, tip, center - side, paper);
                c.Polyline(new[] { center + side, tip, center - side }, ink, 1f, false);
            }
            texts.Label("N", center.x - 20f, center.y - r - 18f, 40f, 12, ink, TextAnchor.UpperCenter, FontStyle.Bold);
        }

        static Vector2[] Corners(Rect r) =>
            new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
    }
}
