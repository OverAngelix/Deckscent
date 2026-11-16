using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Écran titre : le nom dans un cartouche papier cerné d'encre, l'accroche, et les choix
    /// présentés comme une main de cartes dessinées comme les cartes-tuiles de l'écran de carte
    /// (<see cref="Engraving"/> : même cadre, numéro, bandeau), agrandies.
    ///
    /// Commandes (map « UI » d'InputSystem_Actions, plus la souris et les chiffres) :
    ///   souris / ← → (Q D, stick, croix) / 1-4 : choisir une carte
    ///   clic / Entrée / bouton du bas : valider
    ///   Échap / bouton droit : fermer les options ou la liste des commandes
    /// Options : son et langue (<see cref="GameSettings"/>). Textes traduits par <see cref="Loc"/>.
    ///
    /// Calques (du fond vers le dessus) : <see cref="back"/> (voile, titre, aide), une toile par
    /// carte (<see cref="cards"/>, agrandies par leur échelle), <see cref="front"/> (panneaux, fondu).
    /// </summary>
    public class MenuScreen : MonoBehaviour
    {
        [System.Serializable]
        public struct Layer
        {
            public InkCanvas ink;
            public TextPool texts;
        }

        [Tooltip("La scène du menu (caméra, musique). Vide : cherchée dans la scène.")]
        [SerializeField] MainMenu menu;
        [Tooltip("Calque du fond : voile, titre, accroche, aide.")]
        [SerializeField] Layer back;
        [Tooltip("Une toile par carte, dans l'ordre Descendre, Options, Commandes, Quitter. Leur échelle agrandit la carte.")]
        [SerializeField] Layer[] cards = new Layer[4];
        [Tooltip("Calque du dessus : panneaux Options / Commandes, fondu au noir.")]
        [SerializeField] Layer front;

        [Header("Actions (InputSystem_Actions)")]
        [SerializeField] InputActionReference navigate;
        [SerializeField] InputActionReference submit;
        [SerializeField] InputActionReference cancel;

        [Header("Style")]
        [SerializeField] Color paperColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] Color inkColor = new Color(0.06f, 0.06f, 0.07f);
        [Tooltip("Rouge d'accent : carte choisie, réglage choisi, touches.")]
        [SerializeField] Color accentColor = new Color(0.85f, 0.1f, 0.1f);
        [Tooltip("Assombrit le haut et le bas de l'écran pour détacher le titre et les cartes.")]
        [SerializeField, Range(0f, 1f)] float vignette = 0.55f;
        [Tooltip("Agrandissement des cartes par rapport à celles de la carte du donjon.")]
        [SerializeField, Range(1f, 2f)] float cardScale = 1.3f;

        public enum Panel { None, Options, Controls }
        enum Entry { Play, Options, Controls, Quit }

        const string Title = "DECKSCENT";
        const string Tagline = "menu.accroche";
        const float CardGap = 20f, Lift = 12f, FadeInTime = 1.2f, FadeOutTime = 0.6f;
        const float RepeatDelay = 0.4f, RepeatRate = 0.12f;
        static readonly string[] Labels = { "menu.descendre", "menu.options", "menu.commandes", "menu.quitter" };
        static readonly string[] Notes = { "menu.descendre.note", "menu.options.note", "menu.commandes.note", "menu.quitter.note" };

        /// <summary>Lignes de la liste des commandes : action, parties du composite (null = touche simple), description.</summary>
        static readonly (string action, string[] parts, string label)[] Controls =
        {
            ("Player/Move", new[] { "up", "down" }, "commandes.avancer"),
            ("Player/Move", new[] { "left", "right" }, "commandes.pas_de_cote"),
            ("Player/Turn", new[] { "negative", "positive" }, "commandes.tourner"),
            ("Player/Wait", null, "commandes.attendre"),
            ("Player/Interact", null, "commandes.coffre"),
            ("Player/Torch", null, "commandes.lanterne"),
            ("Player/Map", null, "commandes.carte"),
            ("Player/Pause", null, "commandes.pause"),
            ("Map/Rotate", null, "commandes.carte_tourner"),
            ("Map/Discard", null, "commandes.carte_defausser"),
        };

        public int Selected { get; private set; }
        public Panel OpenPanel { get; private set; }
        /// <summary>Ligne choisie dans les options : 0 général, 1 musique, 2 effets sonores, 3 langue.</summary>
        public int OptionRow => _options.Row;
        public bool IsLeaving { get; private set; }

        InputAction _navigate, _submit, _cancel;
        readonly float[] _lift = new float[4];
        readonly Rect[] _cardRects = new Rect[4];
        OptionsPanel _options;
        float _fade = 1f;
        float _fadeTarget;
        float _fadeSpeed = 1f / FadeInTime;
        Vector2Int _heldDir;
        float _nextRepeat;
        Vector2 _lastMouse;
        RectTransform _root;

        void Awake()
        {
            _root = (RectTransform)transform;
            _options = new OptionsPanel(paperColor, inkColor, accentColor);
            if (menu == null)
                menu = FindAnyObjectByType<MainMenu>();
            _navigate = Resolve(navigate, "UI/Navigate");
            _submit = Resolve(submit, "UI/Submit");
            _cancel = Resolve(cancel, "UI/Cancel");
        }

        static InputAction Resolve(InputActionReference reference, string path) =>
            reference != null ? reference.action : InputSystem.actions?.FindAction(path);

        void OnEnable()
        {
            foreach (var action in new[] { _navigate, _submit, _cancel })
                action?.Enable();
            ControlDevice.Listen();
            Time.timeScale = 1f;
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            _fade = Mathf.MoveTowards(_fade, _fadeTarget, _fadeSpeed * dt);
            if (IsLeaving && _fade >= 1f)
            {
                IsLeaving = false; // une seule fois
                SceneFlow.LoadGame();
                return;
            }
            for (var i = 0; i < _lift.Length; i++)
                _lift[i] = Mathf.MoveTowards(_lift[i], i == Selected && OpenPanel == Panel.None ? 1f : 0f, dt * 6f);
            if (!IsLeaving)
                HandleInput();
            Redraw();
        }

        // --- Commandes ---------------------------------------------------------------------

        void HandleInput()
        {
            HandleMouse();
            var dir = ReadDirection();
            var cancelled = _cancel != null && _cancel.WasPressedThisFrame();
            var submitted = _submit != null && _submit.WasPressedThisFrame();
            switch (OpenPanel)
            {
                case Panel.Options:
                    if (cancelled)
                        SetPanel(Panel.None);
                    else
                        _options.Navigate(dir, submitted);
                    break;
                case Panel.Controls:
                    if (cancelled || submitted)
                        SetPanel(Panel.None);
                    break;
                default:
                    var keyboard = Keyboard.current;
                    if (keyboard != null)
                        for (var i = 0; i < Labels.Length; i++)
                            if (keyboard[Key.Digit1 + i].wasPressedThisFrame || keyboard[Key.Numpad1 + i].wasPressedThisFrame)
                                Selected = i;
                    if (dir.x != 0)
                        Selected = (Selected + dir.x + Labels.Length) % Labels.Length;
                    else if (submitted)
                        Activate();
                    break;
            }
        }

        /// <summary>Direction de Navigate, avec répétition si on la garde (pour régler le son).</summary>
        Vector2Int ReadDirection()
        {
            var v = _navigate != null ? _navigate.ReadValue<Vector2>() : Vector2.zero;
            var dir = Mathf.Abs(v.x) >= Mathf.Abs(v.y)
                ? new Vector2Int(Mathf.Abs(v.x) > 0.5f ? (int)Mathf.Sign(v.x) : 0, 0)
                : new Vector2Int(0, Mathf.Abs(v.y) > 0.5f ? (int)Mathf.Sign(v.y) : 0);
            var now = Time.unscaledTime;
            if (dir == Vector2Int.zero)
            {
                _heldDir = dir;
                return dir;
            }
            if (dir != _heldDir)
            {
                _heldDir = dir;
                _nextRepeat = now + RepeatDelay;
                return dir;
            }
            if (OpenPanel == Panel.Options && now >= _nextRepeat)
            {
                _nextRepeat = now + RepeatRate;
                return dir;
            }
            return Vector2Int.zero;
        }

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;
            var screenPos = mouse.position.ReadValue();
            var pos = ToCanvas(screenPos);
            var moved = (screenPos - _lastMouse).sqrMagnitude > 0.5f;
            _lastMouse = screenPos;
            if (OpenPanel == Panel.None)
            {
                var hit = CardAt(pos);
                if (moved && hit >= 0)
                    Selected = hit;
                if (mouse.leftButton.wasPressedThisFrame && hit >= 0)
                {
                    Selected = hit;
                    Activate();
                }
            }
            else if (OpenPanel == Panel.Options)
            {
                if (_options.HandleMouse(mouse, pos, moved))
                    SetPanel(Panel.None);
            }
            else if (mouse.leftButton.wasPressedThisFrame)
                SetPanel(Panel.None);
        }

        /// <summary>Position écran → coordonnées du canvas (origine en haut à gauche, y vers le bas).</summary>
        Vector2 ToCanvas(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local);
            var r = _root.rect;
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }

        int CardAt(Vector2 pos)
        {
            for (var i = 0; i < _cardRects.Length; i++)
            {
                // Un peu plus haut, pour couvrir la carte soulevée.
                var r = _cardRects[i];
                if (new Rect(r.x, r.y - Lift, r.width, r.height + Lift).Contains(pos))
                    return i;
            }
            return -1;
        }

        public void SetPanel(Panel panel)
        {
            OpenPanel = panel;
            _options.Reset();
        }

        /// <summary>Valide la carte choisie.</summary>
        public void Activate()
        {
            switch ((Entry)Selected)
            {
                case Entry.Play:
                    IsLeaving = true;
                    _fadeTarget = 1f;
                    _fadeSpeed = 1f / FadeOutTime;
                    if (menu != null)
                        menu.PrepareStart(FadeOutTime);
                    break;
                case Entry.Options:
                    SetPanel(Panel.Options);
                    break;
                case Entry.Controls:
                    SetPanel(Panel.Controls);
                    break;
                case Entry.Quit:
                    SceneFlow.Quit();
                    break;
            }
        }

        /// <summary>Choisit une carte (captures, tests).</summary>
        public void Select(int index) => Selected = Mathf.Clamp(index, 0, Labels.Length - 1);

        /// <summary>Termine les fondus et soulèvements tout de suite (captures).</summary>
        public void SkipAnimations()
        {
            _fade = _fadeTarget;
            for (var i = 0; i < _lift.Length; i++)
                _lift[i] = i == Selected && OpenPanel == Panel.None ? 1f : 0f;
            Redraw();
        }

        // --- Dessin ------------------------------------------------------------------------

        void Redraw()
        {
            if (back.ink == null || back.texts == null)
                return;
            var screen = _root.rect.size;
            Begin(back);
            DrawVignette(screen);
            DrawTitle(screen);
            if (OpenPanel == Panel.None)
                KeyPrompts.DrawHints(back.ink, back.texts, MainHints(), 0f, screen.y - 34f, screen.x, 15, paperColor, paperColor, inkColor, inkColor);
            End(back);

            DrawCards(screen);

            if (front.ink == null || front.texts == null)
                return;
            Begin(front);
            if (OpenPanel == Panel.Controls)
                DrawControls(screen);
            else if (OpenPanel == Panel.Options)
                DrawOptions(screen);
            if (_fade > 0f)
                front.ink.Rect(new Rect(Vector2.zero, screen), WithAlpha(inkColor, _fade));
            End(front);
        }

        static void Begin(Layer layer)
        {
            layer.ink.Clear();
            layer.texts.Begin();
        }

        static void End(Layer layer)
        {
            layer.ink.Commit();
            layer.texts.End();
        }

        /// <summary>Texte papier cerné d'encre, lisible sur le décor clair comme sombre.</summary>
        void InkedLabel(TextPool texts, string text, float x, float top, float width, int size, FontStyle style)
        {
            // Contour d'encre : le texte répété tout autour, sur deux rayons.
            foreach (var radius in new[] { 1.5f, 3f })
                for (var k = 0; k < 12; k++)
                {
                    var a = k * Mathf.PI / 6f;
                    texts.Label(text, x + Mathf.Cos(a) * radius, top + Mathf.Sin(a) * radius, width, size, inkColor, TextAnchor.UpperCenter, style);
                }
            texts.Label(text, x, top, width, size, paperColor, TextAnchor.UpperCenter, style);
        }

        void DrawVignette(Vector2 screen)
        {
            if (vignette <= 0f)
                return;
            // Bandes d'encre dégradées en haut et en bas (le titre et les cartes s'y posent).
            const int steps = 24;
            var band = screen.y * 0.28f;
            var h = band / steps;
            for (var i = 0; i < steps; i++)
            {
                var color = WithAlpha(inkColor, vignette * Mathf.Pow(1f - (float)i / steps, 1.6f));
                back.ink.Rect(new Rect(0f, i * h, screen.x, h + 1f), color);
                back.ink.Rect(new Rect(0f, screen.y - (i + 1) * h, screen.x, h + 1f), color);
            }
        }

        void DrawTitle(Vector2 screen)
        {
            const int size = 84;
            var textW = TextPool.Width(Title, size, FontStyle.Bold);
            var box = new Rect((screen.x - textW - 110f) * 0.5f, screen.y * 0.08f, textW + 110f, 150f);
            // Feuille papier à double filet, comme l'écran de carte.
            back.ink.Rect(new Rect(box.x + 6f, box.y + 6f, box.width, box.height), WithAlpha(inkColor, 0.85f));
            back.ink.Rect(box, paperColor);
            Engraving.DoubleFrame(back.ink, Shrink(box, 14f), 5f, inkColor);
            back.texts.Label(Title, box.x, box.y + 14f, box.width, size, inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
            Engraving.Rule(back.ink, new Vector2(box.center.x - 130f, box.y + 112f), 260f, inkColor);
            InkedLabel(back.texts, Loc.T(Tagline), 0f, box.yMax + 18f, screen.x, 21, FontStyle.Italic);
        }

        void DrawCards(Vector2 screen)
        {
            var size = Engraving.CardSize * cardScale;
            var n = cards.Length;
            var total = n * size.x + (n - 1) * CardGap;
            var x0 = (screen.x - total) * 0.5f;
            var y0 = screen.y - size.y - 50f;
            for (var i = 0; i < n; i++)
            {
                _cardRects[i] = new Rect(x0 + i * (size.x + CardGap), y0, size.x, size.y);
                var layer = cards[i];
                if (layer.ink == null || layer.texts == null)
                    continue;
                var lift = Smooth(_lift[i]) * Lift;
                // La toile de la carte est à la taille d'une carte du donjon, agrandie par son échelle.
                var rt = (RectTransform)layer.ink.transform.parent;
                rt.anchoredPosition = new Vector2(_cardRects[i].x, -(_cardRects[i].y - lift));
                rt.localScale = Vector3.one * cardScale;
                Begin(layer);
                DrawCard(layer, i, i == Selected && OpenPanel == Panel.None, lift / cardScale);
                End(layer);
            }
        }

        /// <summary>Même mise en page que les cartes-tuiles : numéro, illustration, bandeau du nom, note en pied.</summary>
        void DrawCard(Layer layer, int index, bool active, float lift)
        {
            var ink = layer.ink;
            var accent = active ? accentColor : inkColor;
            var r = new Rect(Vector2.zero, Engraving.CardSize);
            // Ombre plus marquée que sur la carte du donjon : la carte est posée sur le décor 3D.
            var inner = Engraving.CardFrame(ink, r, paperColor, accent, active ? 3f : 1.5f, accent, inkColor, lift, 0.75f);
            layer.texts.Label((index + 1).ToString(), inner.x + 6f, inner.y + 2f, 30f, 17, accent, TextAnchor.UpperLeft, FontStyle.Bold);

            var art = Engraving.CardArtRect(inner);
            ink.Rect(art, paperColor);
            DrawGlyph(layer, (Entry)index, art, accent);
            ink.RectOutline(art, inkColor, 1f);

            var band = Engraving.CardBandRect(inner, art);
            Engraving.Banner(ink, layer.texts, band, Loc.T(Labels[index]).ToUpperInvariant(), paperColor, inkColor);
            layer.texts.Label(Loc.T(Notes[index]), inner.x, band.yMax + 4f, inner.width - 6f, 13,
                Engraving.Fade(inkColor, paperColor, 0.3f), TextAnchor.UpperRight, FontStyle.Italic);
        }

        /// <summary>
        /// Illustrations au trait, avec les hachures des murs du plan des tuiles. `accent` : petite
        /// flèche rouge si la carte est choisie, comme les sorties des tuiles.
        /// </summary>
        void DrawGlyph(Layer layer, Entry entry, Rect r, Color accent)
        {
            var ink = layer.ink;
            const float h = Engraving.CardHatch, w = 1.5f;
            switch (entry)
            {
                case Entry.Play:
                {
                    // Escalier taillé dans la roche, qui descend vers la droite.
                    const int steps = 4;
                    var sx = r.width / steps;
                    var sy = r.height / (steps + 1);
                    var edge = new List<Vector2> { new Vector2(r.x, r.y + sy) };
                    for (var i = 0; i < steps; i++)
                    {
                        var p = r.position + new Vector2(i * sx, (i + 1) * sy);
                        edge.Add(p + new Vector2(sx, 0f));
                        edge.Add(p + new Vector2(sx, sy));
                    }
                    for (var i = 0; i < steps; i++)
                    {
                        var top = r.y + (i + 2) * sy;
                        ink.Hatch(new Rect(r.x + i * sx, top, sx, r.yMax - top), h, inkColor);
                    }
                    ink.Polyline(edge, inkColor, w, false);
                    Arrow(ink, new Vector2(r.xMax - sx * 0.5f, r.yMax), Vector2.up, accent);
                    break;
                }
                case Entry.Options:
                {
                    // Roue dentée sur fond hachuré.
                    ink.Hatch(r, h, inkColor);
                    var c = r.center;
                    var rad = r.width * 0.27f;
                    for (var i = 0; i < 8; i++)
                    {
                        var a = Mathf.PI * 2f * i / 8f;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        var t = new Vector2(d.y, -d.x) * rad * 0.24f;
                        var tooth = new[] { c + d * rad * 0.9f - t, c + d * rad * 1.38f - t * 0.75f, c + d * rad * 1.38f + t * 0.75f, c + d * rad * 0.9f + t };
                        ink.Polygon(tooth, paperColor);
                        ink.Polyline(tooth, inkColor, w, true);
                    }
                    ink.Circle(c, rad, paperColor, 32);
                    ink.Arc(c, rad, 0f, Mathf.PI * 2f, inkColor, w, 32);
                    var hub = new Vector2[12];
                    for (var i = 0; i < hub.Length; i++)
                    {
                        var a = Mathf.PI * 2f * i / hub.Length;
                        hub[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad * 0.42f;
                    }
                    ink.HatchPolygon(hub, h, inkColor);
                    ink.Arc(c, rad * 0.42f, 0f, Mathf.PI * 2f, inkColor, w, 24);
                    break;
                }
                case Entry.Controls:
                {
                    if (ControlDevice.IsGamepad)
                    {
                        // À la manette : la croix, en grand.
                        var side = r.width * 0.78f;
                        KeyPrompts.DrawGlyph(ink, layer.texts, PadGlyph.Dpad, PromptAxis.None,
                            new Rect(r.center - new Vector2(side, side) * 0.5f, new Vector2(side, side)), inkColor, paperColor);
                        break;
                    }
                    // Les quatre touches de déplacement, avec leur nom sur ce clavier.
                    var k = r.width * 0.3f;
                    var c = r.center + new Vector2(0f, k * 0.3f);
                    (string part, Vector2 offset)[] keys = { ("up", new Vector2(0, -1)), ("left", new Vector2(-1, 0)), ("down", Vector2.zero), ("right", new Vector2(1, 0)) };
                    foreach (var key in keys)
                    {
                        var box = new Rect(c + key.offset * (k + 2f) - new Vector2(k, k) * 0.5f, new Vector2(k, k));
                        ink.Hatch(new Rect(box.x + 3f, box.y + 3f, box.width, box.height), h, inkColor);
                        ink.Rect(box, paperColor);
                        ink.RectOutline(box, inkColor, w);
                        layer.texts.Label(KeyPrompts.Describe(KeyPrompts.Of("Player/Move", false, key.part)), box.x - 4f, box.center.y - 8.4f, box.width + 8f, 12, inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
                    }
                    break;
                }
                case Entry.Quit:
                {
                    // Mur hachuré percé d'une porte en arche, battant entrouvert.
                    ink.Hatch(r, h, inkColor);
                    var door = new Rect(r.x + r.width * 0.26f, r.y + r.height * 0.18f, r.width * 0.48f, r.height * 0.82f);
                    var rad = door.width * 0.5f;
                    var arch = new List<Vector2> { new Vector2(door.x, door.yMax) };
                    for (var i = 0; i <= 16; i++)
                    {
                        var t = Mathf.PI + Mathf.PI * i / 16f;
                        arch.Add(new Vector2(door.center.x + Mathf.Cos(t) * rad, door.y + rad + Mathf.Sin(t) * rad));
                    }
                    arch.Add(new Vector2(door.xMax, door.yMax));
                    ink.Polygon(arch, paperColor);
                    ink.Polyline(arch, inkColor, w, false);
                    var leaf = new[]
                    {
                        new Vector2(door.x, door.y + rad), new Vector2(door.x + door.width * 0.42f, door.y + rad * 1.25f),
                        new Vector2(door.x + door.width * 0.42f, door.yMax - 4f), new Vector2(door.x, door.yMax),
                    };
                    ink.Polygon(leaf, paperColor);
                    ink.HatchPolygon(leaf, h * 0.7f, inkColor);
                    ink.Polyline(leaf, inkColor, w, true);
                    Arrow(ink, new Vector2(r.x, r.center.y + r.height * 0.2f), Vector2.left, accent);
                    break;
                }
            }
        }

        /// <summary>Petite flèche hors du carré, pointée vers l'extérieur (`dir` en y vers le bas : up = vers le bas de l'écran).</summary>
        static void Arrow(InkCanvas ink, Vector2 sideMid, Vector2 dir, Color color)
        {
            var tip = sideMid + dir * 8f;
            var b = sideMid + dir * 2.5f;
            var across = new Vector2(-dir.y, dir.x) * 4f;
            ink.Triangle(tip, b + across, b - across, color);
        }

        /// <summary>Voile d'encre et feuille papier titrée, comme l'écran de carte. Renvoie la feuille.</summary>
        Rect DrawPanel(Vector2 screen, Vector2 size, string title) =>
            OptionsPanel.DrawSheet(front.ink, front.texts, screen, size, title, paperColor, inkColor);

        void DrawControls(Vector2 screen)
        {
            const float rowH = 30f;
            var box = DrawPanel(screen, new Vector2(560f, 140f + Controls.Length * rowH), "menu.commandes");
            var y = box.y + 84f;
            foreach (var row in Controls)
            {
                KeyPrompts.Draw(front.ink, front.texts, KeyPrompts.Of(row.action, row.parts), box.x + 40f, y, 17, accentColor, paperColor);
                front.texts.Label(Loc.T(row.label), box.x + 240f, y, box.width - 270f, 17, inkColor);
                y += rowH;
            }
            KeyPrompts.DrawHints(front.ink, front.texts, new[] { new Hint(KeyPrompts.Back(), "aide.retour") },
                box.x, box.yMax - 46f, box.width, 14, inkColor, WithAlpha(inkColor, 0.6f), paperColor);
        }

        void DrawOptions(Vector2 screen) => _options.Draw(front.ink, front.texts, screen);

        /// <summary>Aide sous les cartes de l'écran titre.</summary>
        static Hint[] MainHints() => new[]
        {
            new Hint(KeyPrompts.Navigate(PromptAxis.Horizontal), "aide.choisir"),
            new Hint(KeyPrompts.Confirm(), "aide.valider"),
        };

        static Rect Shrink(Rect r, float by) => new Rect(r.x + by, r.y + by, r.width - by * 2f, r.height - by * 2f);
        static float Smooth(float t) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t);
        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
    }
}
