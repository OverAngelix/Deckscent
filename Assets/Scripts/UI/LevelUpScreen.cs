using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Écran de montée de niveau (GDD §3, docs/niveaux.md) : dès qu'un niveau attend son bienfait
    /// (<see cref="Experience.Pending"/>) et que le joueur a fini son tour, le jeu se met en pause
    /// (<c>Time.timeScale = 0</c>) et propose des bienfaits sous forme de cartes gravées (ATQ, DÉF,
    /// PV max). Il faut en choisir un : pas de retour. Plusieurs niveaux d'un coup : un choix par niveau.
    ///
    /// Commandes (map « UI » d'InputSystem_Actions, plus la souris et les chiffres) :
    ///   ← → (Q D, stick, croix), souris : choisir ; 1, 2, 3 : choisir et valider
    ///   Entrée / bouton du bas, clic : valider
    /// </summary>
    public class LevelUpScreen : MonoBehaviour
    {
        [Tooltip("Le joueur qui monte de niveau. Vide : cherché dans la scène.")]
        [SerializeField] PlayerController player;
        [SerializeField] InkCanvas ink;
        [SerializeField] TextPool texts;

        [Header("Actions (InputSystem_Actions)")]
        [SerializeField] InputActionReference navigate;
        [SerializeField] InputActionReference submit;

        [Header("Réglages")]
        [Tooltip("Délai (secondes, temps réel) avant d'accepter un choix : une touche martelée pendant le combat ne choisit pas à l'aveugle.")]
        [SerializeField, Range(0f, 1.5f)] float acceptDelay = 0.5f;

        [Header("Style")]
        [SerializeField] Color paperColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] Color inkColor = new Color(0.06f, 0.06f, 0.07f);
        [Tooltip("Rouge d'accent : carte choisie, cœur de la vie.")]
        [SerializeField] Color accentColor = new Color(0.85f, 0.1f, 0.1f);

        static readonly Vector2 CardSize = new Vector2(150f, 214f);
        const float CardGap = 26f;

        /// <summary>Vrai tant qu'un écran de niveau est ouvert (la carte et la pause ne s'ouvrent pas par-dessus).</summary>
        public static bool IsShown { get; private set; }

        /// <summary>
        /// Faux : l'écran ne s'ouvre pas tout seul (les niveaux restent en attente). Coupé par les
        /// tests qui ne portent pas sur les niveaux : un monstre tué n'y fige plus le jeu.
        /// </summary>
        public static bool AutoOpen { get; set; } = true;

        public bool IsOpen { get; private set; }
        /// <summary>Bienfaits proposés pour le niveau en cours de choix.</summary>
        public IReadOnlyList<LevelBonus> Offer => _offer;
        public int Selected { get; private set; }

        Canvas _canvas;
        RectTransform _root;
        InputAction _navigate, _submit;
        List<LevelBonus> _offer = new List<LevelBonus>();
        readonly List<Rect> _cardRects = new List<Rect>();
        readonly System.Random _rng = new System.Random();
        float _openedAt;
        int _heldX;
        Vector2 _lastMouse;

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _root = (RectTransform)transform;
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            _navigate = navigate != null ? navigate.action : InputSystem.actions?.FindAction("UI/Navigate");
            _submit = submit != null ? submit.action : InputSystem.actions?.FindAction("UI/Submit");
            SetOpen(false);
        }

        void OnEnable() => ControlDevice.Listen();

        void OnDisable()
        {
            if (IsOpen)
                SetOpen(false);
        }

        void OnDestroy()
        {
            if (IsOpen)
                IsShown = false;
        }

        /// <summary>Un niveau attend, le joueur a fini son tour et rien d'autre n'est ouvert.</summary>
        bool ShouldOpen() =>
            AutoOpen && player != null && !player.IsDead && !player.IsBusy && player.Experience.Pending > 0
            && Time.timeScale != 0f && !PauseScreen.IsPaused;

        void SetOpen(bool open)
        {
            IsOpen = open;
            IsShown = open;
            if (_canvas != null)
                _canvas.enabled = open;
            if (open)
            {
                _openedAt = Time.unscaledTime;
                _navigate?.Enable();
                _submit?.Enable();
                if (Mouse.current != null)
                    _lastMouse = Mouse.current.position.ReadValue();
                NewOffer();
            }
            Time.timeScale = open ? 0f : 1f;
        }

        void NewOffer()
        {
            _offer = player.Experience.Offer(_rng);
            Selected = 0;
            _heldX = 0;
            Redraw();
        }

        /// <summary>Ouvre l'écran s'il y a un niveau en attente (captures, tests).</summary>
        public void Open()
        {
            if (!IsOpen && player != null && player.Experience.Pending > 0)
                SetOpen(true);
        }

        // --- Entrées -----------------------------------------------------------------------

        void Update()
        {
            if (!IsOpen)
            {
                if (ShouldOpen())
                    SetOpen(true);
                return;
            }
            if (Time.unscaledTime - _openedAt >= acceptDelay)
            {
                HandleMouse();
                if (IsOpen)
                    HandleKeys();
            }
            if (IsOpen)
                Redraw();
        }

        void HandleKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
                for (var i = 0; i < _offer.Count && i < 9; i++)
                    if (keyboard[Key.Digit1 + i].wasPressedThisFrame || keyboard[Key.Numpad1 + i].wasPressedThisFrame)
                    {
                        Choose(i);
                        return;
                    }
            var v = _navigate != null ? _navigate.ReadValue<Vector2>() : Vector2.zero;
            var x = Mathf.Abs(v.x) > 0.5f && Mathf.Abs(v.x) >= Mathf.Abs(v.y) ? (int)Mathf.Sign(v.x) : 0;
            if (x != 0 && x != _heldX)
                Selected = (Selected + x + _offer.Count) % _offer.Count;
            _heldX = x;
            if (_submit != null && _submit.WasPressedThisFrame())
                Choose(Selected);
        }

        void HandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;
            var screenPos = mouse.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPos, null, out var local);
            var r = _root.rect;
            var pos = new Vector2(local.x - r.xMin, r.yMax - local.y);
            var moved = (screenPos - _lastMouse).sqrMagnitude > 0.5f;
            _lastMouse = screenPos;
            var hit = _cardRects.FindIndex(c => c.Contains(pos));
            if (hit < 0)
                return;
            if (moved)
                Selected = hit;
            if (mouse.leftButton.wasPressedThisFrame)
                Choose(hit);
        }

        /// <summary>Prend le bienfait n° `index` ; ferme l'écran, ou propose le niveau suivant s'il en reste.</summary>
        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= _offer.Count)
                return;
            player.ApplyBonus(_offer[index]);
            if (player.Experience.Pending > 0)
            {
                _openedAt = Time.unscaledTime;
                NewOffer();
            }
            else
                SetOpen(false);
        }

        // --- Dessin ------------------------------------------------------------------------

        void Redraw()
        {
            if (ink == null || texts == null || player == null)
                return;
            ink.Clear();
            texts.Begin();
            Draw(_root.rect.size);
            ink.Commit();
            texts.End();
        }

        void Draw(Vector2 screen)
        {
            ink.Rect(new Rect(Vector2.zero, screen), OptionsPanel.WithAlpha(inkColor, 0.6f));
            var xp = player.Experience;
            var n = _offer.Count;
            var cardsW = n * CardSize.x + (n - 1) * CardGap;
            var size = new Vector2(Mathf.Max(460f, cardsW + 90f), 420f);
            var box = new Rect((screen - size) * 0.5f, size);
            ink.Rect(new Rect(box.x + 6f, box.y + 6f, box.width, box.height), inkColor);
            ink.Rect(box, paperColor);
            Engraving.DoubleFrame(ink, OptionsPanel.Shrink(box, 14f), 5f, inkColor);

            // Le niveau en cours de choix (plusieurs niveaux d'un coup : un par un).
            var level = xp.Level - xp.Pending + 1;
            texts.Label(Loc.F("niveau.titre", level).ToUpperInvariant(), box.x, box.y + 26f, box.width, 26, inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
            Engraving.Rule(ink, new Vector2(box.center.x - 90f, box.y + 68f), 180f, inkColor);
            texts.Label(Loc.T("niveau.choisir"), box.x, box.y + 76f, box.width, 15, Engraving.Fade(inkColor, paperColor, 0.25f), TextAnchor.UpperCenter, FontStyle.Italic);

            _cardRects.Clear();
            var x = box.center.x - cardsW * 0.5f;
            var top = box.y + 112f;
            for (var i = 0; i < n; i++)
            {
                var active = i == Selected;
                var lift = active ? 8f : 0f;
                var rect = new Rect(x + i * (CardSize.x + CardGap), top - lift, CardSize.x, CardSize.y);
                _cardRects.Add(new Rect(rect.x, top - 8f, rect.width, rect.height + 8f));
                DrawCard(rect, _offer[i], i, active, lift);
            }

            KeyPrompts.DrawHints(ink, texts, new[]
            {
                new Hint(KeyPrompts.Navigate(PromptAxis.Horizontal), "aide.choisir"),
                new Hint(KeyPrompts.Confirm(), "aide.valider"),
            }, box.x, box.yMax - 46f, box.width, 14, inkColor, OptionsPanel.WithAlpha(inkColor, 0.6f), paperColor);
        }

        void DrawCard(Rect r, LevelBonus bonus, int index, bool active, float lift)
        {
            var border = active ? accentColor : inkColor;
            var inner = Engraving.CardFrame(ink, r, paperColor, border, active ? 3f : 1.5f, inkColor, inkColor, lift);
            // Numéro (touche 1, 2, 3) en haut à gauche, comme le numéro des cartes-tuiles.
            texts.Label((index + 1).ToString(), inner.x + 6f, inner.y + 4f, 20f, 13, Engraving.Fade(inkColor, paperColor, 0.35f), TextAnchor.UpperLeft, FontStyle.Bold);

            var amount = player.Experience.Amount(bonus);
            var iconColor = bonus == LevelBonus.Health || active ? accentColor : inkColor;
            DrawIcon(bonus, new Vector2(inner.center.x, inner.y + 52f), 26f, iconColor);

            texts.Label("+" + amount, inner.x, inner.y + 88f, inner.width, 30, active ? accentColor : inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
            var band = new Rect(inner.x - 4f, inner.y + 132f, inner.width + 8f, 22f);
            Engraving.Banner(ink, texts, band, Loc.T(NameKey(bonus)), paperColor, inkColor, 13);
            texts.Label(Detail(bonus, amount), inner.x, band.yMax + 10f, inner.width, 13, Engraving.Fade(inkColor, paperColor, 0.2f), TextAnchor.UpperCenter, FontStyle.Italic);
        }

        static string NameKey(LevelBonus bonus) => bonus switch
        {
            LevelBonus.Attack => "niveau.attaque",
            LevelBonus.Defense => "niveau.defense",
            _ => "niveau.vie",
        };

        /// <summary>« ATQ 1 → 2 » : la stat avant et après le bienfait.</summary>
        string Detail(LevelBonus bonus, int amount) => bonus switch
        {
            LevelBonus.Attack => Loc.F("niveau.avant_apres", Loc.F("hud.atq", player.Attack), player.Attack + amount),
            LevelBonus.Defense => Loc.F("niveau.avant_apres", Loc.F("hud.def", player.Defense), player.Defense + amount),
            _ => Loc.F("niveau.avant_apres", Loc.T("hud.pv") + " " + player.MaxHealth, player.MaxHealth + amount),
        };

        /// <summary>Épée (ATQ), écu (DÉF) ou cœur (vie), gravés à la plume.</summary>
        void DrawIcon(LevelBonus bonus, Vector2 c, float s, Color color)
        {
            switch (bonus)
            {
                case LevelBonus.Attack:
                    // Épée droite, pointe en haut : pointe, lame à gouttière, garde à losanges, poignée filetée, pommeau.
                    // (Polygones dans le sens des aiguilles d'une montre à l'écran, sinon ils ne sont pas tracés.)
                    var bladeW = s * 0.36f;
                    var tipY = c.y - s * 1.05f;
                    var tipBase = c.y - s * 0.72f;
                    var guardY = c.y + s * 0.42f;
                    ink.Triangle(new Vector2(c.x, tipY), new Vector2(c.x + bladeW * 0.5f, tipBase), new Vector2(c.x - bladeW * 0.5f, tipBase), color);
                    ink.Rect(new Rect(c.x - bladeW * 0.5f, tipBase - 0.5f, bladeW, guardY - tipBase + 0.5f), color);
                    ink.Line(new Vector2(c.x, tipBase + s * 0.05f), new Vector2(c.x, guardY - s * 0.08f), paperColor, 1.2f);
                    var guardW = s * 1.15f;
                    ink.Rect(new Rect(c.x - guardW * 0.5f, guardY, guardW, s * 0.13f), color);
                    Engraving.Diamond(ink, new Vector2(c.x - guardW * 0.5f, guardY + s * 0.065f), s * 0.12f, color);
                    Engraving.Diamond(ink, new Vector2(c.x + guardW * 0.5f, guardY + s * 0.065f), s * 0.12f, color);
                    var gripTop = guardY + s * 0.13f;
                    var gripH = s * 0.4f;
                    var grip = new Rect(c.x - s * 0.1f, gripTop, s * 0.2f, gripH);
                    ink.Rect(grip, color);
                    for (var k = 1; k < 4; k++)
                        ink.Line(new Vector2(grip.x, gripTop + gripH * k / 4f), new Vector2(grip.xMax, gripTop + gripH * k / 4f), paperColor, 1f);
                    ink.Circle(new Vector2(c.x, gripTop + gripH + s * 0.1f), s * 0.13f, color);
                    break;
                case LevelBonus.Defense:
                    // Écu : haut droit, pointe en bas, bande au milieu.
                    var shield = new[]
                    {
                        c + new Vector2(-s * 0.7f, -s * 0.75f), c + new Vector2(s * 0.7f, -s * 0.75f),
                        c + new Vector2(s * 0.7f, s * 0.05f), c + new Vector2(0f, s * 0.85f), c + new Vector2(-s * 0.7f, s * 0.05f),
                    };
                    ink.HatchPolygon(shield, 4f, color, 1f);
                    ink.Polyline(shield, color, 2.5f, true);
                    ink.Line(c + new Vector2(0f, -s * 0.75f), c + new Vector2(0f, s * 0.85f), color, 2f);
                    break;
                default:
                    // Cœur : deux lobes et une pointe.
                    var lobe = s * 0.38f;
                    ink.Circle(c + new Vector2(-lobe * 0.95f, -s * 0.2f), lobe, color);
                    ink.Circle(c + new Vector2(lobe * 0.95f, -s * 0.2f), lobe, color);
                    ink.Polygon(new[]
                    {
                        c + new Vector2(-lobe * 1.9f, -s * 0.08f), c + new Vector2(lobe * 1.9f, -s * 0.08f), c + new Vector2(0f, s * 0.8f),
                    }, color);
                    break;
            }
        }
    }
}
