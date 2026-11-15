using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>Bouton de manette dessiné en picto (ses noms suivent les chemins de l'Input System).</summary>
    public enum PadGlyph
    {
        None,
        South, East, West, North,
        LeftShoulder, RightShoulder, LeftTrigger, RightTrigger,
        LeftStick, RightStick, LeftStickPress, RightStickPress,
        Dpad, Select, Start,
    }

    /// <summary>Sens indiqué sur un stick ou une croix (avancer / reculer : vertical).</summary>
    public enum PromptAxis { None, Vertical, Horizontal }

    /// <summary>Une touche à afficher : un nom de touche (clavier), un picto (manette), ou « / » entre deux possibilités.</summary>
    public readonly struct Prompt
    {
        public readonly string Text;
        public readonly PadGlyph Glyph;
        public readonly PromptAxis Axis;
        public readonly bool IsOr;

        Prompt(string text, PadGlyph glyph, PromptAxis axis, bool isOr)
        {
            Text = text;
            Glyph = glyph;
            Axis = axis;
            IsOr = isOr;
        }

        public static Prompt Key(string text) => new Prompt(text, PadGlyph.None, PromptAxis.None, false);
        public static Prompt Pad(PadGlyph glyph, PromptAxis axis = PromptAxis.None) => new Prompt(null, glyph, axis, false);
        public static readonly Prompt Or = new Prompt("/", PadGlyph.None, PromptAxis.None, true);

        public override string ToString() => IsOr ? "/" : Glyph != PadGlyph.None ? Glyph + (Axis != PromptAxis.None ? "(" + Axis + ")" : "") : Text;
    }

    /// <summary>Une aide « touches + ce qu'elles font » (clé <see cref="Loc"/> du libellé).</summary>
    public readonly struct Hint
    {
        public readonly Prompt[] Keys;
        public readonly string Label;

        public Hint(Prompt[] keys, string label)
        {
            Keys = keys;
            Label = label;
        }
    }

    /// <summary>
    /// Touches affichées selon l'appareil (<see cref="ControlDevice"/>) : au clavier, le nom de
    /// la touche sur ce clavier (Z en AZERTY pour le W QWERTY), à la manette un picto gravé
    /// (✕ ○ △ □ pour PlayStation, A B X Y pour Xbox et Switch, gâchettes, sticks, croix),
    /// dessiné au trait sur une <see cref="InkCanvas"/>, sans sprites.
    ///
    /// Les touches d'une action viennent de ses liaisons du groupe de contrôle de l'appareil
    /// (<c>InputBinding.MaskByGroup</c>, <c>GetBindingDisplayString</c>) : changer une touche dans
    /// InputSystem_Actions change ce qui s'affiche.
    /// </summary>
    public static class KeyPrompts
    {
        /// <summary>
        /// Noms de touches → clé <see cref="Loc"/>, pour les afficher dans la langue du jeu. Unity
        /// les donne en anglais, ou dans la langue du clavier (Windows en AZERTY : « Entree », « Echap »).
        /// </summary>
        static readonly Dictionary<string, string> KeyNames = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Space", "touche.espace" }, { "Espace", "touche.espace" },
            { "Enter", "touche.entree" }, { "Numpad Enter", "touche.entree" }, { "Entree", "touche.entree" }, { "Entrée", "touche.entree" },
            { "Delete", "touche.suppr" }, { "Suppr", "touche.suppr" }, { "Suppr.", "touche.suppr" },
            { "Backspace", "touche.retour_arriere" }, { "Retour arriere", "touche.retour_arriere" }, { "Retour arrière", "touche.retour_arriere" },
            { "Escape", "touche.echap" }, { "Echap", "touche.echap" }, { "Échap", "touche.echap" },
            { "Tab", "touche.tab" }, { "Tabulation", "touche.tab" },
        };

        /// <summary>Flèches : des symboles, pas des mots (rien à traduire).</summary>
        static readonly Dictionary<string, string> Arrows = new Dictionary<string, string>
        {
            { "Up Arrow", "↑" }, { "Down Arrow", "↓" }, { "Left Arrow", "←" }, { "Right Arrow", "→" },
        };

        // --- Touches d'une action -------------------------------------------------------

        /// <summary>
        /// Touches de l'action (« Player/Map »), pour l'appareil en cours. `parts` : parties d'un
        /// composite (« up », « down »), chacune affichée ; sans elles, la touche simple (au
        /// clavier la première, à la manette toutes, séparées par « / »).
        /// </summary>
        public static Prompt[] Of(string actionPath, params string[] parts) =>
            Of(actionPath, ControlDevice.IsGamepad, parts);

        public static Prompt[] Of(string actionPath, bool gamepad, params string[] parts)
        {
            var action = InputSystem.actions?.FindAction(actionPath);
            if (action == null)
                return new[] { Prompt.Key("?") };
            var mask = InputBinding.MaskByGroup(gamepad ? "Gamepad" : "Keyboard&Mouse");
            var result = new List<Prompt>();
            if (parts != null && parts.Length > 0)
            {
                foreach (var part in parts)
                {
                    var index = FindBinding(action, mask, part);
                    if (index >= 0)
                        result.Add(ToPrompt(action, index, PromptAxis.None));
                }
                if (result.Count == parts.Length)
                    return result.ToArray();
                // Manette : pas de composite (stick, croix entiers) → la liaison simple, avec le sens.
                result.Clear();
                if (!gamepad)
                    return new[] { Prompt.Key("?") };
                var axis = AxisOf(parts);
                AddSimple(action, mask, true, axis, result);
            }
            else
                AddSimple(action, mask, gamepad, PromptAxis.None, result);
            return result.Count > 0 ? result.ToArray() : new[] { Prompt.Key("?") };
        }

        static PromptAxis AxisOf(string[] parts)
        {
            foreach (var part in parts)
            {
                var p = part.ToLowerInvariant();
                if (p == "up" || p == "down")
                    return PromptAxis.Vertical;
                if (p == "left" || p == "right" || p == "negative" || p == "positive")
                    return PromptAxis.Horizontal;
            }
            return PromptAxis.None;
        }

        /// <summary>Liaisons simples (hors composites) du groupe : la première, ou toutes (sans doublon) séparées par « / ».</summary>
        static void AddSimple(InputAction action, InputBinding mask, bool all, PromptAxis axis, List<Prompt> result)
        {
            var bindings = action.bindings;
            var seen = new HashSet<string>();
            for (var i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isComposite || b.isPartOfComposite || !mask.Matches(b))
                    continue;
                var prompt = ToPrompt(action, i, axis);
                if (!seen.Add(prompt.ToString()) || prompt.Text == "")
                    continue;
                if (result.Count > 0)
                    result.Add(Prompt.Or);
                result.Add(prompt);
                if (!all)
                    return;
            }
        }

        static int FindBinding(InputAction action, InputBinding mask, string part)
        {
            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isPartOfComposite && mask.Matches(b) && string.Equals(b.name, part, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        static Prompt ToPrompt(InputAction action, int index, PromptAxis axis)
        {
            var name = action.GetBindingDisplayString(index, out var layout, out var control,
                InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (!string.IsNullOrEmpty(layout) && InputSystem.IsFirstLayoutBasedOnSecond(layout, "Gamepad"))
            {
                var glyph = GlyphOf(control);
                if (glyph != PadGlyph.None)
                    return Prompt.Pad(glyph, axis);
            }
            return Prompt.Key(KeyName(name));
        }

        /// <summary>Nom affiché d'une touche clavier (traduit, flèches en symboles, lettres en capitales).</summary>
        public static string KeyName(string unityName)
        {
            if (string.IsNullOrEmpty(unityName))
                return "";
            if (Arrows.TryGetValue(unityName, out var arrow))
                return arrow;
            if (KeyNames.TryGetValue(unityName, out var key))
                return Loc.T(key);
            return unityName.Length == 1 ? unityName.ToUpperInvariant() : unityName;
        }

        /// <summary>Chemin d'un contrôle de manette (« buttonSouth », « leftStick/up »…) → picto.</summary>
        public static PadGlyph GlyphOf(string control)
        {
            if (string.IsNullOrEmpty(control))
                return PadGlyph.None;
            var slash = control.IndexOf('/');
            var root = slash >= 0 ? control.Substring(0, slash) : control;
            switch (root)
            {
                case "buttonSouth": return PadGlyph.South;
                case "buttonEast": return PadGlyph.East;
                case "buttonWest": return PadGlyph.West;
                case "buttonNorth": return PadGlyph.North;
                case "leftShoulder": return PadGlyph.LeftShoulder;
                case "rightShoulder": return PadGlyph.RightShoulder;
                case "leftTrigger": return PadGlyph.LeftTrigger;
                case "rightTrigger": return PadGlyph.RightTrigger;
                case "leftStick": return PadGlyph.LeftStick;
                case "rightStick": return PadGlyph.RightStick;
                case "leftStickPress": return PadGlyph.LeftStickPress;
                case "rightStickPress": return PadGlyph.RightStickPress;
                case "dpad": return PadGlyph.Dpad;
                case "select": return PadGlyph.Select;
                case "start": return PadGlyph.Start;
                default: return PadGlyph.None;
            }
        }

        // --- Touches communes du menu (liaisons par usage : */{Submit}, */{Cancel}) --------

        /// <summary>Valider : Entrée, ou le bouton du bas.</summary>
        public static Prompt[] Confirm() => ControlDevice.IsGamepad
            ? new[] { Prompt.Pad(PadGlyph.South) }
            : new[] { Prompt.Key(Loc.T("touche.entree")) };

        /// <summary>Retour : Échap, ou le bouton de droite.</summary>
        public static Prompt[] Back() => ControlDevice.IsGamepad
            ? new[] { Prompt.Pad(PadGlyph.East) }
            : new[] { Prompt.Key(Loc.T("touche.echap")) };

        /// <summary>Se déplacer dans un menu : flèches, ou la croix dans ce sens.</summary>
        public static Prompt[] Navigate(PromptAxis axis) => ControlDevice.IsGamepad
            ? new[] { Prompt.Pad(PadGlyph.Dpad, axis) }
            : axis == PromptAxis.Vertical ? new[] { Prompt.Key("↑"), Prompt.Key("↓") } : new[] { Prompt.Key("←"), Prompt.Key("→") };

        /// <summary>Plusieurs possibilités, séparées par « / ».</summary>
        public static Prompt[] Either(params Prompt[][] choices)
        {
            var result = new List<Prompt>();
            foreach (var choice in choices)
            {
                if (choice == null || choice.Length == 0)
                    continue;
                if (result.Count > 0)
                    result.Add(Prompt.Or);
                result.AddRange(choice);
            }
            return result.ToArray();
        }

        /// <summary>Texte des touches (tests, journaux) : « Z S », « South / North ».</summary>
        public static string Describe(Prompt[] keys) => string.Join(" ", System.Array.ConvertAll(keys, k => k.ToString()));

        // --- Mise en page ---------------------------------------------------------------

        const float KeyGap = 4f;
        const float HintGap = 7f;
        const string Separator = "·";

        /// <summary>Hauteur d'un picto pour un texte de taille `size`.</summary>
        public static float GlyphHeight(int size) => Mathf.Round(size * 1.35f);

        /// <summary>Largeur qu'occupent les touches.</summary>
        public static float Width(Prompt[] keys, int size)
        {
            var w = 0f;
            for (var i = 0; i < keys.Length; i++)
            {
                if (i > 0)
                    w += KeyGap;
                w += ItemWidth(keys[i], size);
            }
            return w;
        }

        static float ItemWidth(Prompt p, int size)
        {
            if (p.Glyph != PadGlyph.None)
                return GlyphWidth(p.Glyph, GlyphHeight(size));
            return TextPool.Width(p.Text, size, p.IsOr ? FontStyle.Normal : FontStyle.Bold);
        }

        /// <summary>
        /// Dessine les touches à partir de (x, top), sur une ligne de texte de taille `size`.
        /// `fg` : encre des traits et des noms ; `bg` : fond des pictos ; `outline` (si opaque) :
        /// contour des textes, pour les lire sur le décor 3D. Renvoie la largeur.
        /// </summary>
        public static float Draw(InkCanvas ink, TextPool texts, Prompt[] keys, float x, float top, int size, Color fg, Color bg,
            Color outline = default)
        {
            var start = x;
            var h = GlyphHeight(size);
            var cy = top + size * 0.68f;
            for (var i = 0; i < keys.Length; i++)
            {
                if (i > 0)
                    x += KeyGap;
                var p = keys[i];
                if (p.Glyph != PadGlyph.None)
                {
                    var w = GlyphWidth(p.Glyph, h);
                    DrawGlyph(ink, texts, p.Glyph, p.Axis, new Rect(x, cy - h * 0.5f, w, h), fg, bg);
                    x += w;
                }
                else
                {
                    var style = p.IsOr ? FontStyle.Normal : FontStyle.Bold;
                    var w = TextPool.Width(p.Text, size, style);
                    Label(texts, p.Text, x, top, w + 4f, size, p.IsOr ? Fade(fg, 0.6f) : fg, style, outline);
                    x += w;
                }
            }
            return x - start;
        }

        /// <summary>Largeur d'une ligne d'aides.</summary>
        public static float HintsWidth(IList<Hint> hints, int size)
        {
            var sepW = TextPool.Width(Separator, size) + HintGap * 4f;
            var w = 0f;
            for (var i = 0; i < hints.Count; i++)
            {
                if (i > 0)
                    w += sepW;
                w += Width(hints[i].Keys, size) + HintGap + TextPool.Width(Loc.T(hints[i].Label), size, FontStyle.Italic);
            }
            return w;
        }

        /// <summary>
        /// Ligne d'aides « touches  libellé   ·   touches  libellé », centrée dans [x, x + width].
        /// `keyColor` : touches ; `labelColor` : libellés et séparateurs ; `bg` : fond des pictos ;
        /// `outline` (si opaque) : contour des textes.
        /// </summary>
        public static void DrawHints(InkCanvas ink, TextPool texts, IList<Hint> hints, float x, float top, float width, int size,
            Color keyColor, Color labelColor, Color bg, Color outline = default)
        {
            var sepW = TextPool.Width(Separator, size);
            var cx = x + (width - HintsWidth(hints, size)) * 0.5f;
            for (var i = 0; i < hints.Count; i++)
            {
                if (i > 0)
                {
                    cx += HintGap * 2f;
                    Label(texts, Separator, cx, top, sepW + 4f, size, labelColor, FontStyle.Normal, outline);
                    cx += sepW + HintGap * 2f;
                }
                cx += Draw(ink, texts, hints[i].Keys, cx, top, size, keyColor, bg, outline) + HintGap;
                var label = Loc.T(hints[i].Label);
                var lw = TextPool.Width(label, size, FontStyle.Italic);
                Label(texts, label, cx, top, lw + 4f, size, labelColor, FontStyle.Italic, outline);
                cx += lw;
            }
        }

        /// <summary>Texte aligné à gauche, cerné de `outline` s'il est opaque (le texte répété tout autour).</summary>
        static void Label(TextPool texts, string text, float x, float top, float width, int size, Color color, FontStyle style, Color outline)
        {
            if (outline.a > 0f)
                foreach (var radius in new[] { 1.5f, 3f })
                    for (var k = 0; k < 12; k++)
                    {
                        var a = k * Mathf.PI / 6f;
                        texts.Label(text, x + Mathf.Cos(a) * radius, top + Mathf.Sin(a) * radius, width, size, outline, TextAnchor.UpperLeft, style);
                    }
            texts.Label(text, x, top, width, size, color, TextAnchor.UpperLeft, style);
        }

        // --- Pictos ---------------------------------------------------------------------

        /// <summary>Largeur d'un picto de hauteur `h`.</summary>
        public static float GlyphWidth(PadGlyph glyph, float h)
        {
            switch (glyph)
            {
                case PadGlyph.LeftShoulder:
                case PadGlyph.RightShoulder:
                case PadGlyph.LeftTrigger:
                case PadGlyph.RightTrigger:
                    return Mathf.Round(h * 1.5f);
                case PadGlyph.Select:
                case PadGlyph.Start:
                    return ControlDevice.Current == DeviceFamily.PlayStation ? Mathf.Round(h * 0.75f) : h;
                default:
                    return h;
            }
        }

        /// <summary>
        /// Un picto gravé dans `r` : ombre hachurée décalée, fond `bg`, contour et signe à l'encre `fg`.
        /// Dessiné pour la manette en cours (<see cref="ControlDevice.Current"/>).
        /// </summary>
        public static void DrawGlyph(InkCanvas ink, TextPool texts, PadGlyph glyph, PromptAxis axis, Rect r, Color fg, Color bg) =>
            DrawGlyph(ink, texts, glyph, axis, r, fg, bg, ControlDevice.Current);

        public static void DrawGlyph(InkCanvas ink, TextPool texts, PadGlyph glyph, PromptAxis axis, Rect r, Color fg, Color bg, DeviceFamily family)
        {
            var h = r.height;
            var c = r.center;
            var line = Mathf.Max(1f, h * 0.07f);
            var shadow = new Vector2(1f, 1f) * Mathf.Max(1.5f, h * 0.1f);
            var hatch = Mathf.Max(2f, h * 0.12f);
            switch (glyph)
            {
                case PadGlyph.South:
                case PadGlyph.East:
                case PadGlyph.West:
                case PadGlyph.North:
                {
                    var rad = h * 0.5f;
                    var ring = CirclePoints(c, rad, 20);
                    ink.HatchPolygon(Offset(ring, shadow), hatch, fg);
                    ink.Polygon(ring, bg);
                    ink.Polyline(ring, fg, line, true);
                    if (family == DeviceFamily.PlayStation)
                        FaceSymbol(ink, glyph, c, rad * 0.5f, fg, Mathf.Max(1.2f, h * 0.09f));
                    else
                        Letter(texts, FaceLetter(glyph, family), c, h * 0.62f, fg, r.width);
                    break;
                }
                case PadGlyph.LeftShoulder:
                case PadGlyph.RightShoulder:
                case PadGlyph.LeftTrigger:
                case PadGlyph.RightTrigger:
                {
                    var trigger = glyph == PadGlyph.LeftTrigger || glyph == PadGlyph.RightTrigger;
                    var left = glyph == PadGlyph.LeftShoulder || glyph == PadGlyph.LeftTrigger;
                    var body = trigger ? r : new Rect(r.x, r.y + h * 0.14f, r.width, h * 0.72f);
                    var shape = Bumper(body, trigger, left);
                    ink.HatchPolygon(Offset(shape, shadow), hatch, fg);
                    ink.Polygon(shape, bg);
                    ink.Polyline(shape, fg, line, true);
                    Letter(texts, ShoulderLabel(glyph, family), new Vector2(body.center.x, body.center.y + (trigger ? h * 0.06f : 0f)), h * 0.5f, fg, r.width);
                    break;
                }
                case PadGlyph.LeftStick:
                case PadGlyph.RightStick:
                case PadGlyph.LeftStickPress:
                case PadGlyph.RightStickPress:
                {
                    var rad = h * 0.5f;
                    var ring = CirclePoints(c, rad, 24);
                    ink.HatchPolygon(Offset(ring, shadow), hatch, fg);
                    ink.Polygon(ring, bg);
                    ink.Polyline(ring, fg, line, true);
                    var press = glyph == PadGlyph.LeftStickPress || glyph == PadGlyph.RightStickPress;
                    var leftSide = glyph == PadGlyph.LeftStick || glyph == PadGlyph.LeftStickPress;
                    if (press)
                    {
                        // Clic du stick : son nom (L3, LS) au milieu, lisible même en petit.
                        Letter(texts, StickPressLabel(leftSide, family), c, h * 0.42f, fg, r.width);
                        break;
                    }
                    // Le champignon du stick, hachuré, et le sens à suivre sur le pourtour ;
                    // en grand seulement, la lettre du stick (L, R) au milieu.
                    var cap = CirclePoints(c, rad * 0.56f, 20);
                    ink.HatchPolygon(cap, hatch * 0.75f, fg);
                    ink.Polyline(cap, fg, line, true);
                    if (h >= 32f)
                    {
                        ink.Polygon(CirclePoints(c, rad * 0.36f, 16), bg);
                        Letter(texts, leftSide ? "L" : "R", c, h * 0.38f, fg, r.width);
                    }
                    AxisMarks(ink, c, rad * 0.78f, rad * 0.2f, axis, fg);
                    break;
                }
                case PadGlyph.Dpad:
                {
                    var arm = h * 0.34f;
                    var vertical = new Rect(c.x - arm * 0.5f, r.y, arm, h);
                    var horizontal = new Rect(r.x, c.y - arm * 0.5f, h, arm);
                    var shadowV = new Rect(vertical.position + shadow, vertical.size);
                    var shadowH = new Rect(horizontal.position + shadow, horizontal.size);
                    ink.Hatch(shadowV, hatch, fg);
                    ink.Hatch(shadowH, hatch, fg);
                    ink.Rect(vertical, bg);
                    ink.Rect(horizontal, bg);
                    ink.Polyline(CrossOutline(vertical, horizontal), fg, line, true);
                    AxisMarks(ink, c, h * 0.36f, arm * 0.32f, axis, fg);
                    break;
                }
                case PadGlyph.Select:
                case PadGlyph.Start:
                    DrawMenuButton(ink, texts, glyph == PadGlyph.Start, r, fg, bg, family, line, shadow, hatch);
                    break;
            }
        }

        static void DrawMenuButton(InkCanvas ink, TextPool texts, bool start, Rect r, Color fg, Color bg, DeviceFamily family,
            float line, Vector2 shadow, float hatch)
        {
            var h = r.height;
            var c = r.center;
            if (family == DeviceFamily.PlayStation)
            {
                // Petit bouton allongé (Create / Options), trois traits au-dessus pour Options.
                var pill = new Rect(c.x - h * 0.17f, r.y + h * 0.3f, h * 0.34f, h * 0.62f);
                var shape = Rounded(pill, h * 0.15f);
                ink.HatchPolygon(Offset(shape, shadow), hatch, fg);
                ink.Polygon(shape, bg);
                ink.Polyline(shape, fg, line, true);
                if (start)
                    for (var i = -1; i <= 1; i++)
                        ink.Line(new Vector2(c.x + i * h * 0.16f, r.y + h * 0.06f), new Vector2(c.x + i * h * 0.16f, r.y + h * 0.2f), fg, line);
                else
                    for (var i = -1; i <= 1; i++)
                        ink.Line(new Vector2(c.x + i * h * 0.16f, r.y + h * 0.04f), new Vector2(c.x + i * h * 0.08f, r.y + h * 0.2f), fg, line);
                return;
            }
            var rad = h * 0.5f;
            var ring = CirclePoints(c, rad, 20);
            ink.HatchPolygon(Offset(ring, shadow), hatch, fg);
            ink.Polygon(ring, bg);
            ink.Polyline(ring, fg, line, true);
            if (family == DeviceFamily.Switch)
            {
                // − et +.
                var s = rad * 0.45f;
                ink.Line(c - new Vector2(s, 0f), c + new Vector2(s, 0f), fg, line * 1.6f);
                if (start)
                    ink.Line(c - new Vector2(0f, s), c + new Vector2(0f, s), fg, line * 1.6f);
                return;
            }
            if (start)
            {
                // Menu : trois traits.
                for (var i = -1; i <= 1; i++)
                    ink.Line(c + new Vector2(-rad * 0.42f, i * rad * 0.3f), c + new Vector2(rad * 0.42f, i * rad * 0.3f), fg, line);
            }
            else
            {
                // Affichage : deux fenêtres l'une sur l'autre.
                var s = rad * 0.55f;
                var back = new Rect(c.x - s * 0.5f, c.y - s * 0.75f, s, s * 0.8f);
                var front = new Rect(c.x - s * 0.9f, c.y - s * 0.25f, s, s * 0.8f);
                ink.RectOutline(back, fg, line);
                ink.Rect(front, bg);
                ink.RectOutline(front, fg, line);
            }
        }

        /// <summary>✕ ○ □ △ tracés à l'encre (PlayStation).</summary>
        static void FaceSymbol(InkCanvas ink, PadGlyph glyph, Vector2 c, float s, Color fg, float width)
        {
            switch (glyph)
            {
                case PadGlyph.South:
                    ink.Line(c + new Vector2(-s, -s) * 0.8f, c + new Vector2(s, s) * 0.8f, fg, width);
                    ink.Line(c + new Vector2(s, -s) * 0.8f, c + new Vector2(-s, s) * 0.8f, fg, width);
                    break;
                case PadGlyph.East:
                    ink.Arc(c, s * 0.82f, 0f, Mathf.PI * 2f, fg, width, 20);
                    break;
                case PadGlyph.West:
                    ink.RectOutline(new Rect(c.x - s * 0.72f, c.y - s * 0.72f, s * 1.44f, s * 1.44f), fg, width);
                    break;
                case PadGlyph.North:
                    ink.Polyline(new[] { c + new Vector2(0f, -s * 0.9f), c + new Vector2(s * 0.88f, s * 0.62f), c + new Vector2(-s * 0.88f, s * 0.62f) }, fg, width, true);
                    break;
            }
        }

        /// <summary>Lettre des boutons de face : Xbox A B X Y ; Switch, inversées (A à droite, B en bas).</summary>
        public static string FaceLetter(PadGlyph glyph, DeviceFamily family)
        {
            var nintendo = family == DeviceFamily.Switch;
            switch (glyph)
            {
                case PadGlyph.South: return nintendo ? "B" : "A";
                case PadGlyph.East: return nintendo ? "A" : "B";
                case PadGlyph.West: return nintendo ? "Y" : "X";
                case PadGlyph.North: return nintendo ? "X" : "Y";
                default: return "";
            }
        }

        static string ShoulderLabel(PadGlyph glyph, DeviceFamily family)
        {
            var left = glyph == PadGlyph.LeftShoulder || glyph == PadGlyph.LeftTrigger;
            var trigger = glyph == PadGlyph.LeftTrigger || glyph == PadGlyph.RightTrigger;
            var side = left ? "L" : "R";
            switch (family)
            {
                case DeviceFamily.PlayStation: return side + (trigger ? "2" : "1");
                case DeviceFamily.Switch: return trigger ? "Z" + side : side;
                default: return side + (trigger ? "T" : "B");
            }
        }

        static string StickPressLabel(bool left, DeviceFamily family) =>
            family == DeviceFamily.PlayStation ? (left ? "L3" : "R3") : (left ? "LS" : "RS");

        /// <summary>Texte centré sur `c`, de hauteur de caractère ≈ `height`.</summary>
        static void Letter(TextPool texts, string text, Vector2 c, float height, Color fg, float boxWidth)
        {
            var size = Mathf.Max(6, Mathf.RoundToInt(height));
            var w = boxWidth + 20f;
            // Haut de la boîte de texte : les capitales de la police à empattements tombent centrées sur `c`.
            texts.Label(text, c.x - w * 0.5f, c.y - size * 0.56f, w, size, fg, TextAnchor.UpperCenter, FontStyle.Bold);
        }

        /// <summary>Petites flèches pleines vers l'extérieur, sur l'axe demandé (les quatre si aucun).</summary>
        static void AxisMarks(InkCanvas ink, Vector2 c, float distance, float size, PromptAxis axis, Color fg)
        {
            var dirs = axis == PromptAxis.Vertical ? new[] { Vector2.up, Vector2.down }
                : axis == PromptAxis.Horizontal ? new[] { Vector2.left, Vector2.right }
                : new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            foreach (var d in dirs)
            {
                var tip = c + d * (distance + size * 0.5f);
                var b = c + d * (distance - size * 0.5f);
                var across = new Vector2(-d.y, d.x) * size * 0.7f;
                ink.Triangle(tip, b + across, b - across, fg);
            }
        }

        /// <summary>Gâchette (bord haut arrondi) ou bouton de tranche (biseauté côté extérieur).</summary>
        static Vector2[] Bumper(Rect r, bool trigger, bool left)
        {
            if (trigger)
            {
                var pts = new List<Vector2> { new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.y + r.height * 0.4f) };
                for (var i = 0; i <= 8; i++)
                {
                    var a = Mathf.PI + Mathf.PI * i / 8f;
                    pts.Add(new Vector2(r.center.x + Mathf.Cos(a) * r.width * 0.5f, r.y + r.height * 0.4f + Mathf.Sin(a) * r.height * 0.4f));
                }
                pts.Add(new Vector2(r.xMax, r.yMax));
                return pts.ToArray();
            }
            var cut = r.height * 0.55f;
            return left
                ? new[] { new Vector2(r.xMin + cut, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin + cut) }
                : new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax - cut, r.yMin), new Vector2(r.xMax, r.yMin + cut), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) };
        }

        static Vector2[] CrossOutline(Rect v, Rect h) => new[]
        {
            new Vector2(v.xMin, v.yMin), new Vector2(v.xMax, v.yMin), new Vector2(v.xMax, h.yMin), new Vector2(h.xMax, h.yMin),
            new Vector2(h.xMax, h.yMax), new Vector2(v.xMax, h.yMax), new Vector2(v.xMax, v.yMax), new Vector2(v.xMin, v.yMax),
            new Vector2(v.xMin, h.yMax), new Vector2(h.xMin, h.yMax), new Vector2(h.xMin, h.yMin), new Vector2(v.xMin, h.yMin),
        };

        static Vector2[] Rounded(Rect r, float radius)
        {
            var pts = new List<Vector2>();
            Vector2[] centers = { new Vector2(r.xMax - radius, r.yMin + radius), new Vector2(r.xMax - radius, r.yMax - radius), new Vector2(r.xMin + radius, r.yMax - radius), new Vector2(r.xMin + radius, r.yMin + radius) };
            for (var k = 0; k < 4; k++)
                for (var i = 0; i <= 4; i++)
                {
                    var a = -Mathf.PI * 0.5f + (k + i / 4f) * Mathf.PI * 0.5f;
                    pts.Add(centers[k] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                }
            return pts.ToArray();
        }

        static Vector2[] CirclePoints(Vector2 c, float radius, int segments)
        {
            var pts = new Vector2[segments];
            for (var k = 0; k < segments; k++)
            {
                var a = k * Mathf.PI * 2f / segments;
                pts[k] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }
            return pts;
        }

        static Vector2[] Offset(Vector2[] points, Vector2 by)
        {
            var result = new Vector2[points.Length];
            for (var i = 0; i < points.Length; i++)
                result[i] = points[i] + by;
            return result;
        }

        static Color Fade(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
    }
}
