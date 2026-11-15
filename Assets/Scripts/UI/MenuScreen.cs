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
        const string Tagline = "Construisez le donjon. Survivez-y.";
        const string Hint = "← →  choisir   ·   Entrée  valider";
        const string OptionsHint = "↑ ↓  choisir   ·   ← →  régler   ·   Échap  retour";
        const float CardGap = 20f, Lift = 12f, FadeInTime = 1.2f, FadeOutTime = 0.6f;
        const float RepeatDelay = 0.4f, RepeatRate = 0.12f;
        /// <summary>Lignes du panneau Options : les volumes (0 général, 1 musique, 2 effets), puis la langue.</summary>
        const int VolumeRows = 3, LanguageRow = 3, OptionCount = 4;
        static readonly string[] VolumeLabels = { "Général", "Musique", "Effets sonores" };
        static readonly string[] Labels = { "Descendre", "Options", "Commandes", "Quitter" };
        static readonly string[] Notes = { "nouvelle partie", "son · langue", "les touches", "fermer le jeu" };

        /// <summary>Lignes de la liste des commandes : action, parties du composite (null = touche simple), description.</summary>
        static readonly (string action, string[] parts, string label)[] Controls =
        {
            ("Player/Move", new[] { "up", "down" }, "Avancer / reculer"),
            ("Player/Move", new[] { "left", "right" }, "Pas de côté"),
            ("Player/Turn", new[] { "negative", "positive" }, "Tourner"),
            ("Player/Wait", null, "Attendre un tour"),
            ("Player/Interact", null, "Ouvrir un coffre"),
            ("Player/Torch", null, "Lanterne"),
            ("Player/Map", null, "Carte (poser des tuiles)"),
            ("Map/Rotate", null, "Carte : tourner la tuile"),
            ("Map/Discard", null, "Carte : défausser"),
        };

        /// <summary>Noms de touches en français (Unity les donne en anglais).</summary>
        static readonly Dictionary<string, string> KeyNamesFr = new Dictionary<string, string>
        {
            { "Space", "Espace" }, { "Enter", "Entrée" }, { "Entree", "Entrée" }, { "Delete", "Suppr" }, { "Backspace", "Retour" },
            { "Escape", "Échap" }, { "Tab", "Tab" }, { "Up Arrow", "↑" }, { "Down Arrow", "↓" },
            { "Left Arrow", "←" }, { "Right Arrow", "→" }, { "Numpad Enter", "Entrée" },
        };

        public int Selected { get; private set; }
        public Panel OpenPanel { get; private set; }
        /// <summary>Ligne choisie dans les options : 0 général, 1 musique, 2 effets sonores, 3 langue.</summary>
        public int OptionRow { get; private set; }
        public bool IsLeaving { get; private set; }

        InputAction _navigate, _submit, _cancel;
        readonly float[] _lift = new float[4];
        readonly Rect[] _cardRects = new Rect[4];
        Rect _panelRect;
        readonly Rect[] _sliderRects = new Rect[VolumeRows];
        readonly Rect[] _flagRects = new Rect[2];
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
                    else if (dir.y != 0)
                        OptionRow = (OptionRow + (dir.y > 0 ? -1 : 1) + OptionCount) % OptionCount;
                    else if (dir.x != 0 || (submitted && OptionRow == LanguageRow))
                        Adjust(dir.x != 0 ? dir.x : 1);
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
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (!_panelRect.Contains(pos))
                        SetPanel(Panel.None);
                    for (var row = 0; row < VolumeRows; row++)
                        if (Grow(_sliderRects[row], 10f).Contains(pos))
                        {
                            OptionRow = row;
                            SetVolumeAt(row, pos.x);
                        }
                    for (var i = 0; i < _flagRects.Length; i++)
                        if (Grow(_flagRects[i], 6f).Contains(pos))
                        {
                            OptionRow = LanguageRow;
                            GameSettings.SetLocale(GameSettings.Locales[i]);
                        }
                }
                // Glisser : règle la jauge de la ligne choisie (celle qu'on vient de cliquer).
                else if (mouse.leftButton.isPressed && moved && OptionRow < VolumeRows && Grow(_sliderRects[OptionRow], 10f).Contains(pos))
                    SetVolumeAt(OptionRow, pos.x);
            }
            else if (mouse.leftButton.wasPressedThisFrame)
                SetPanel(Panel.None);
        }

        void SetVolumeAt(int row, float x) => SetVolume(row, (x - _sliderRects[row].x) / _sliderRects[row].width);

        /// <summary>Volume de la ligne `row` (0 général, 1 musique, 2 effets sonores).</summary>
        static float GetVolume(int row) => row switch
        {
            0 => GameSettings.Volume,
            1 => GameSettings.MusicVolume,
            _ => GameSettings.EffectsVolume,
        };

        static void SetVolume(int row, float value)
        {
            switch (row)
            {
                case 0: GameSettings.SetVolume(value); break;
                case 1: GameSettings.SetMusicVolume(value); break;
                default: GameSettings.SetEffectsVolume(value); break;
            }
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

        static Rect Grow(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + by * 2f, r.height + by * 2f);

        /// <summary>Volumes : ±10 %. Langue : la suivante.</summary>
        void Adjust(int step)
        {
            if (OptionRow < VolumeRows)
                SetVolume(OptionRow, GetVolume(OptionRow) + step * 0.1f);
            else
                GameSettings.NextLocale(step);
        }

        public void SetPanel(Panel panel)
        {
            OpenPanel = panel;
            OptionRow = 0;
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
                InkedLabel(back.texts, Loc.T(Hint), 0f, screen.y - 34f, screen.x, 15, FontStyle.Italic);
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
                        layer.texts.Label(KeyLabel("Player/Move", key.part), box.x - 4f, box.center.y - 8.4f, box.width + 8f, 12, inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
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
        Rect DrawPanel(Vector2 screen, Vector2 size, string title)
        {
            var ink = front.ink;
            ink.Rect(new Rect(Vector2.zero, screen), WithAlpha(inkColor, 0.6f));
            var box = new Rect((screen - size) * 0.5f, size);
            ink.Rect(new Rect(box.x + 6f, box.y + 6f, box.width, box.height), inkColor);
            ink.Rect(box, paperColor);
            Engraving.DoubleFrame(ink, Shrink(box, 14f), 5f, inkColor);
            front.texts.Label(Loc.T(title).ToUpperInvariant(), box.x, box.y + 26f, box.width, 26, inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
            Engraving.Rule(ink, new Vector2(box.center.x - 90f, box.y + 68f), 180f, inkColor);
            return box;
        }

        void DrawControls(Vector2 screen)
        {
            const float rowH = 30f;
            var box = DrawPanel(screen, new Vector2(560f, 140f + Controls.Length * rowH), "Commandes");
            var y = box.y + 84f;
            foreach (var row in Controls)
            {
                var keys = row.parts == null
                    ? KeyLabel(row.action, null)
                    : string.Join(" ", System.Array.ConvertAll(row.parts, part => KeyLabel(row.action, part)));
                front.texts.Label(keys, box.x + 40f, y, 190f, 17, accentColor, TextAnchor.UpperLeft, FontStyle.Bold);
                front.texts.Label(Loc.T(row.label), box.x + 240f, y, box.width - 270f, 17, inkColor);
                y += rowH;
            }
            front.texts.Label(Loc.T("Échap : retour"), box.x, box.yMax - 44f, box.width, 14, WithAlpha(inkColor, 0.6f), TextAnchor.UpperCenter, FontStyle.Italic);
        }

        void DrawOptions(Vector2 screen)
        {
            var ink = front.ink;
            var texts = front.texts;
            var box = DrawPanel(screen, new Vector2(540f, 410f), "Options");
            _panelRect = box;
            var labelX = box.x + 48f;
            var valueX = box.x + 220f;

            for (var row = 0; row < OptionCount; row++)
            {
                var y = OptionY(box, row);
                var active = row == OptionRow;
                if (active)
                    ink.Triangle(new Vector2(labelX - 22f, y - 13f), new Vector2(labelX - 12f, y - 7f), new Vector2(labelX - 22f, y - 1f), accentColor);
                var label = row < VolumeRows ? VolumeLabels[row] : "Langue";
                texts.Label(Loc.T(label), labelX, y - 21f, valueX - labelX - 8f, 19, active ? accentColor : inkColor, TextAnchor.UpperLeft, FontStyle.Bold);
                if (row < VolumeRows)
                    DrawGauge(row, y, valueX);
            }

            // Langue : un drapeau par langue, celle en cours encadrée.
            var y1 = OptionY(box, LanguageRow);
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
                    ink.RectOutline(Grow(rect, 5f), OptionRow == LanguageRow ? accentColor : inkColor, 3f);
                texts.Label(locale.ToUpperInvariant(), x, rect.yMax + 8f, flag.x, 15, current ? inkColor : WithAlpha(inkColor, 0.45f), TextAnchor.UpperCenter);
                x += flag.x + 34f;
            }
            texts.Label(Loc.LocaleName(GameSettings.Locale), x, y1 - 20f, 150f, 16, inkColor);

            texts.Label(Loc.T(OptionsHint), box.x, box.yMax - 44f, box.width, 14, WithAlpha(inkColor, 0.6f), TextAnchor.UpperCenter, FontStyle.Italic);
        }

        /// <summary>Ligne de base de la ligne `row` du panneau Options.</summary>
        static float OptionY(Rect box, int row) => box.y + 118f + row * 56f;

        /// <summary>Jauge à 10 cases d'un volume, comme les PV du HUD, et sa valeur (Muet ou 80 %).</summary>
        void DrawGauge(int row, float y, float x)
        {
            var ink = front.ink;
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
                    ink.Rect(pip, OptionRow == row ? accentColor : inkColor);
                ink.RectOutline(pip, inkColor, 1.5f);
            }
            var text = filled == 0 ? Loc.T("Muet") : Mathf.RoundToInt(volume * 100f) + " %";
            front.texts.Label(text, rect.xMax + 14f, y - 19f, 100f, 16, inkColor);
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
            ink.RectOutline(r, inkColor, 2f);
        }

        /// <summary>
        /// Nom de la touche clavier d'une action, selon la disposition du clavier (les touches sont
        /// liées par position : W QWERTY s'affiche Z en AZERTY). `part` : partie d'un composite
        /// (« up », « negative »…), null pour une touche simple.
        /// </summary>
        public static string KeyLabel(string actionPath, string part)
        {
            var action = InputSystem.actions?.FindAction(actionPath);
            if (action == null)
                return "?";
            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b.isComposite || !b.effectivePath.StartsWith("<Keyboard>"))
                    continue;
                if (part == null ? b.isPartOfComposite : !(b.isPartOfComposite && string.Equals(b.name, part, System.StringComparison.OrdinalIgnoreCase)))
                    continue;
                var name = action.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                if (string.IsNullOrEmpty(name))
                    continue;
                if (GameSettings.Locale == "fr" && KeyNamesFr.TryGetValue(name, out var fr))
                    return fr;
                return name.Length == 1 ? name.ToUpperInvariant() : name;
            }
            return "?";
        }

        static Rect Shrink(Rect r, float by) => new Rect(r.x + by, r.y + by, r.width - by * 2f, r.height - by * 2f);
        static float Smooth(float t) => 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t);
        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
    }
}
