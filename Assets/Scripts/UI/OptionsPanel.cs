using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Panneau Options, partagé par l'écran titre (<see cref="MenuScreen"/>) et le menu pause
    /// (<see cref="PauseScreen"/>) : un volume par piste du mixer (jauge à 10 cases) et la langue
    /// (drapeaux). Réglages lus et écrits dans <see cref="GameSettings"/> (sauvegardés).
    ///
    /// L'écran qui l'affiche lui passe ses entrées (<see cref="Navigate"/>, <see cref="HandleMouse"/>)
    /// et le dessine (<see cref="Draw"/>) ; fermer (Échap, clic hors du cadre) reste à sa charge.
    /// </summary>
    public class OptionsPanel
    {
        /// <summary>Lignes : les volumes (0 général, 1 musique, 2 effets), puis la langue.</summary>
        public const int VolumeRows = 3, LanguageRow = 3, RowCount = 4;
        static readonly string[] VolumeLabels = { "options.general", "options.musique", "options.effets" };

        readonly Color _paper, _ink, _accent;
        readonly Rect[] _sliderRects = new Rect[VolumeRows];
        readonly Rect[] _flagRects = new Rect[2];

        /// <summary>Ligne choisie : 0 général, 1 musique, 2 effets sonores, 3 langue.</summary>
        public int Row { get; private set; }
        /// <summary>La feuille dessinée la dernière fois (pour « clic hors du cadre »).</summary>
        public Rect Box { get; private set; }

        public OptionsPanel(Color paper, Color ink, Color accent)
        {
            _paper = paper;
            _ink = ink;
            _accent = accent;
        }

        public void Reset() => Row = 0;

        // --- Entrées -----------------------------------------------------------------------

        /// <summary>↑ ↓ : ligne ; ← → : régler ; Valider sur la langue : la suivante.</summary>
        public void Navigate(Vector2Int dir, bool submitted)
        {
            if (dir.y != 0)
                Row = (Row + (dir.y > 0 ? -1 : 1) + RowCount) % RowCount;
            else if (dir.x != 0 || (submitted && Row == LanguageRow))
                Adjust(dir.x != 0 ? dir.x : 1);
        }

        /// <summary>
        /// Clic sur une jauge ou un drapeau, glisser sur une jauge. `pos` en coordonnées du canvas
        /// (y vers le bas). Renvoie vrai si on a cliqué hors du cadre (l'écran ferme le panneau).
        /// </summary>
        public bool HandleMouse(Mouse mouse, Vector2 pos, bool moved)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                for (var row = 0; row < VolumeRows; row++)
                    if (Grow(_sliderRects[row], 10f).Contains(pos))
                    {
                        Row = row;
                        SetVolumeAt(row, pos.x);
                    }
                for (var i = 0; i < _flagRects.Length; i++)
                    if (Grow(_flagRects[i], 6f).Contains(pos))
                    {
                        Row = LanguageRow;
                        GameSettings.SetLocale(GameSettings.Locales[i]);
                    }
                return !Box.Contains(pos);
            }
            // Glisser : règle la jauge de la ligne choisie (celle qu'on vient de cliquer).
            if (mouse.leftButton.isPressed && moved && Row < VolumeRows && Grow(_sliderRects[Row], 10f).Contains(pos))
                SetVolumeAt(Row, pos.x);
            return false;
        }

        void SetVolumeAt(int row, float x) => SetVolume(row, (x - _sliderRects[row].x) / _sliderRects[row].width);

        /// <summary>Volumes : ±10 %. Langue : la suivante.</summary>
        void Adjust(int step)
        {
            if (Row < VolumeRows)
                SetVolume(Row, GetVolume(Row) + step * 0.1f);
            else
                GameSettings.NextLocale(step);
        }

        /// <summary>Volume de la ligne `row` (0 général, 1 musique, 2 effets sonores).</summary>
        public static float GetVolume(int row) => row switch
        {
            0 => GameSettings.Volume,
            1 => GameSettings.MusicVolume,
            _ => GameSettings.EffectsVolume,
        };

        public static void SetVolume(int row, float value)
        {
            switch (row)
            {
                case 0: GameSettings.SetVolume(value); break;
                case 1: GameSettings.SetMusicVolume(value); break;
                default: GameSettings.SetEffectsVolume(value); break;
            }
        }

        // --- Dessin --------------------------------------------------------------------------

        /// <summary>Voile d'encre et feuille papier titrée, comme l'écran de carte. Renvoie la feuille.</summary>
        public static Rect DrawSheet(InkCanvas ink, TextPool texts, Vector2 screen, Vector2 size, string titleKey, Color paper, Color inkColor)
        {
            ink.Rect(new Rect(Vector2.zero, screen), WithAlpha(inkColor, 0.6f));
            var box = new Rect((screen - size) * 0.5f, size);
            ink.Rect(new Rect(box.x + 6f, box.y + 6f, box.width, box.height), inkColor);
            ink.Rect(box, paper);
            Engraving.DoubleFrame(ink, Shrink(box, 14f), 5f, inkColor);
            texts.Label(Loc.T(titleKey).ToUpperInvariant(), box.x, box.y + 26f, box.width, 26, inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
            Engraving.Rule(ink, new Vector2(box.center.x - 90f, box.y + 68f), 180f, inkColor);
            return box;
        }

        /// <summary>Marque d'une ligne choisie : petit triangle rouge devant le libellé (ligne de base `y`).</summary>
        public static void DrawMarker(InkCanvas ink, float labelX, float y, Color accent) =>
            ink.Triangle(new Vector2(labelX - 22f, y - 13f), new Vector2(labelX - 12f, y - 7f), new Vector2(labelX - 22f, y - 1f), accent);

        public void Draw(InkCanvas ink, TextPool texts, Vector2 screen)
        {
            var box = DrawSheet(ink, texts, screen, new Vector2(540f, 410f), "menu.options", _paper, _ink);
            Box = box;
            var labelX = box.x + 48f;
            var valueX = box.x + 220f;

            for (var row = 0; row < RowCount; row++)
            {
                var y = RowY(box, row);
                var active = row == Row;
                if (active)
                    DrawMarker(ink, labelX, y, _accent);
                var label = row < VolumeRows ? VolumeLabels[row] : "options.langue";
                texts.Label(Loc.T(label), labelX, y - 21f, valueX - labelX - 8f, 19, active ? _accent : _ink, TextAnchor.UpperLeft, FontStyle.Bold);
                if (row < VolumeRows)
                    DrawGauge(ink, texts, row, y, valueX);
            }

            // Langue : un drapeau par langue, celle en cours encadrée.
            var y1 = RowY(box, LanguageRow);
            var flag = new Vector2(48f, 32f);
            var x = valueX;
            for (var i = 0; i < GameSettings.Locales.Length; i++)
            {
                var locale = GameSettings.Locales[i];
                var rect = new Rect(new Vector2(x, y1 - 26f), flag);
                _flagRects[i] = rect;
                DrawFlag(ink, locale, rect);
                var current = locale == GameSettings.Locale;
                if (current)
                    ink.RectOutline(Grow(rect, 5f), Row == LanguageRow ? _accent : _ink, 3f);
                texts.Label(locale.ToUpperInvariant(), x, rect.yMax + 8f, flag.x, 15, current ? _ink : WithAlpha(_ink, 0.45f), TextAnchor.UpperCenter);
                x += flag.x + 34f;
            }
            texts.Label(Loc.LocaleName(GameSettings.Locale), x, y1 - 20f, 150f, 16, _ink);

            KeyPrompts.DrawHints(ink, texts, new[]
            {
                new Hint(KeyPrompts.Navigate(PromptAxis.Vertical), "aide.choisir"),
                new Hint(KeyPrompts.Navigate(PromptAxis.Horizontal), "aide.regler"),
                new Hint(KeyPrompts.Back(), "aide.retour"),
            }, box.x, box.yMax - 46f, box.width, 14, _ink, WithAlpha(_ink, 0.6f), _paper);
        }

        /// <summary>Ligne de base de la ligne `row`.</summary>
        static float RowY(Rect box, int row) => box.y + 118f + row * 56f;

        /// <summary>Jauge à 10 cases d'un volume, comme les PV du HUD, et sa valeur (Muet ou 80 %).</summary>
        void DrawGauge(InkCanvas ink, TextPool texts, int row, float y, float x)
        {
            var rect = new Rect(x, y - 18f, 200f, 20f);
            _sliderRects[row] = rect;
            const int n = 10;
            const float gap = 4f;
            var pipW = (rect.width - (n - 1) * gap) / n;
            var volume = GetVolume(row);
            var filled = Mathf.RoundToInt(volume * n);
            for (var i = 0; i < n; i++)
            {
                var pip = new Rect(rect.x + i * (pipW + gap), rect.y, pipW, rect.height);
                if (i < filled)
                    ink.Rect(pip, Row == row ? _accent : _ink);
                ink.RectOutline(pip, _ink, 1.5f);
            }
            var text = filled == 0 ? Loc.T("options.muet") : Mathf.RoundToInt(volume * 100f) + " %";
            texts.Label(text, rect.xMax + 14f, y - 19f, 100f, 16, _ink);
        }

        /// <summary>Drapeaux simplifiés (France, Royaume-Uni), cernés d'encre.</summary>
        void DrawFlag(InkCanvas ink, string locale, Rect r)
        {
            if (locale == "fr")
            {
                var w = r.width / 3f;
                ink.Rect(new Rect(r.x, r.y, w, r.height), new Color(0f, 0.14f, 0.58f));
                ink.Rect(new Rect(r.x + w, r.y, w, r.height), Color.white);
                ink.Rect(new Rect(r.x + 2f * w, r.y, w, r.height), new Color(0.93f, 0.16f, 0.22f));
            }
            else
            {
                var red = new Color(0.78f, 0.06f, 0.18f);
                ink.Rect(r, new Color(0f, 0.14f, 0.49f));
                var tr = new Vector2(r.xMax, r.yMin);
                var bl = new Vector2(r.xMin, r.yMax);
                ink.Line(r.min, r.max, Color.white, 6f);
                ink.Line(tr, bl, Color.white, 6f);
                ink.Line(r.min, r.max, red, 2f);
                ink.Line(tr, bl, red, 2f);
                var c = r.center;
                ink.Rect(new Rect(r.x, c.y - 5f, r.width, 10f), Color.white);
                ink.Rect(new Rect(c.x - 5f, r.y, 10f, r.height), Color.white);
                ink.Rect(new Rect(r.x, c.y - 3f, r.width, 6f), red);
                ink.Rect(new Rect(c.x - 3f, r.y, 6f, r.height), red);
            }
            ink.RectOutline(r, _ink, 2f);
        }

        public static Rect Grow(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + by * 2f, r.height + by * 2f);
        public static Rect Shrink(Rect r, float by) => new Rect(r.x + by, r.y + by, r.width - by * 2f, r.height - by * 2f);
        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
    }
}
