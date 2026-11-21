using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Fin de sous-biome (GDD §2, docs/remontee.md) : sur l'escalier du boss du dernier étage d'une
    /// tranche (tous les <see cref="Dungeon.FloorsPerSubBiome"/> étages), le jeu se met en pause et
    /// propose deux cartes gravées : **Remonter** (fin de l'expédition, tout l'or porté va au camp)
    /// ou **Descendre** (on continue, l'or reste en jeu). Retour : ni l'un ni l'autre, on reste à
    /// l'étage (l'escalier reste ouvert).
    ///
    /// Commandes (map « UI » d'InputSystem_Actions, plus la souris et les chiffres) :
    ///   ← → (Q D, stick, croix), souris : choisir ; 1, 2 : choisir et valider
    ///   Entrée / bouton du bas, clic : valider ; Échap / bouton droit : rester à l'étage
    /// </summary>
    public class AscentScreen : MonoBehaviour
    {
        public enum Choice { None, Ascend, Descend }

        [SerializeField] InkCanvas ink;
        [SerializeField] TextPool texts;

        [Header("Actions (InputSystem_Actions)")]
        [SerializeField] InputActionReference navigate;
        [SerializeField] InputActionReference submit;
        [SerializeField] InputActionReference cancel;

        [Header("Réglages")]
        [Tooltip("Délai (secondes, temps réel) avant d'accepter un choix : la touche qui a fait avancer sur l'escalier ne choisit pas à l'aveugle.")]
        [SerializeField, Range(0f, 1.5f)] float acceptDelay = 0.4f;

        [Header("Style")]
        [SerializeField] Color paperColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] Color inkColor = new Color(0.06f, 0.06f, 0.07f);
        [Tooltip("Rouge d'accent : carte choisie.")]
        [SerializeField] Color accentColor = new Color(0.85f, 0.1f, 0.1f);

        static readonly Vector2 CardSize = new Vector2(170f, 224f);
        const float CardGap = 34f;
        static readonly Choice[] Cards = { Choice.Ascend, Choice.Descend };

        /// <summary>Vrai tant que l'écran est ouvert (la carte et la pause ne s'ouvrent pas par-dessus).</summary>
        public static bool IsShown { get; private set; }

        public bool IsOpen { get; private set; }
        /// <summary>Carte choisie (0 : remonter, 1 : descendre).</summary>
        public int Selected { get; private set; }
        /// <summary>Le choix fait à la fermeture (<see cref="Choice.None"/> : on reste à l'étage).</summary>
        public Choice Result { get; private set; }

        Canvas _canvas;
        RectTransform _root;
        InputAction _navigate, _submit, _cancel;
        readonly List<Rect> _cardRects = new List<Rect>();
        float _openedAt;
        int _heldX;
        Vector2 _lastMouse;
        int _floor, _gold;

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _root = (RectTransform)transform;
            _navigate = navigate != null ? navigate.action : InputSystem.actions?.FindAction("UI/Navigate");
            _submit = submit != null ? submit.action : InputSystem.actions?.FindAction("UI/Submit");
            _cancel = cancel != null ? cancel.action : InputSystem.actions?.FindAction("UI/Cancel");
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

        /// <summary>Ouvre le choix au sous-sol `floor` (qu'on quitte), avec `gold` d'or porté.</summary>
        public void Open(int floor, int gold)
        {
            _floor = floor;
            _gold = gold;
            Result = Choice.None;
            Selected = 0;
            SetOpen(true);
        }

        void SetOpen(bool open)
        {
            IsOpen = open;
            IsShown = open;
            if (_canvas != null)
                _canvas.enabled = open;
            if (open)
            {
                _openedAt = Time.unscaledTime;
                _heldX = 0;
                _navigate?.Enable();
                _submit?.Enable();
                _cancel?.Enable();
                if (Mouse.current != null)
                    _lastMouse = Mouse.current.position.ReadValue();
                Redraw();
            }
            Time.timeScale = open ? 0f : 1f;
        }

        /// <summary>Choisit la carte n° `index` (0 : remonter, 1 : descendre) et ferme l'écran.</summary>
        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= Cards.Length)
                return;
            Result = Cards[index];
            SetOpen(false);
        }

        /// <summary>Ni l'un ni l'autre : on reste à l'étage.</summary>
        public void Stay()
        {
            if (!IsOpen)
                return;
            Result = Choice.None;
            SetOpen(false);
        }

        // --- Entrées -----------------------------------------------------------------------

        void Update()
        {
            if (!IsOpen)
                return;
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
                for (var i = 0; i < Cards.Length; i++)
                    if (keyboard[Key.Digit1 + i].wasPressedThisFrame || keyboard[Key.Numpad1 + i].wasPressedThisFrame)
                    {
                        Choose(i);
                        return;
                    }
            if (_cancel != null && _cancel.WasPressedThisFrame())
            {
                Stay();
                return;
            }
            var v = _navigate != null ? _navigate.ReadValue<Vector2>() : Vector2.zero;
            var x = Mathf.Abs(v.x) > 0.5f && Mathf.Abs(v.x) >= Mathf.Abs(v.y) ? (int)Mathf.Sign(v.x) : 0;
            if (x != 0 && x != _heldX)
                Selected = (Selected + x + Cards.Length) % Cards.Length;
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

        // --- Dessin ------------------------------------------------------------------------

        void Redraw()
        {
            if (ink == null || texts == null)
                return;
            ink.Clear();
            texts.Begin();
            if (IsOpen)
                Draw(_root.rect.size);
            ink.Commit();
            texts.End();
        }

        void Draw(Vector2 screen)
        {
            ink.Rect(new Rect(Vector2.zero, screen), OptionsPanel.WithAlpha(inkColor, 0.6f));
            var cardsW = Cards.Length * CardSize.x + (Cards.Length - 1) * CardGap;
            var size = new Vector2(Mathf.Max(500f, cardsW + 110f), 404f);
            var box = new Rect((screen - size) * 0.5f, size);
            ink.Rect(new Rect(box.x + 6f, box.y + 6f, box.width, box.height), inkColor);
            ink.Rect(box, paperColor);
            Engraving.DoubleFrame(ink, OptionsPanel.Shrink(box, 14f), 5f, inkColor);

            texts.Label(Loc.T("remontee.titre").ToUpperInvariant(), box.x, box.y + 26f, box.width, 26, inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
            Engraving.Rule(ink, new Vector2(box.center.x - 90f, box.y + 68f), 180f, inkColor);
            texts.Label(Loc.F("remontee.sous_titre", _floor), box.x, box.y + 76f, box.width, 15, Engraving.Fade(inkColor, paperColor, 0.25f), TextAnchor.UpperCenter, FontStyle.Italic);

            _cardRects.Clear();
            var x = box.center.x - cardsW * 0.5f;
            var top = box.y + 110f;
            for (var i = 0; i < Cards.Length; i++)
            {
                var active = i == Selected;
                var lift = active ? 8f : 0f;
                var rect = new Rect(x + i * (CardSize.x + CardGap), top - lift, CardSize.x, CardSize.y);
                _cardRects.Add(new Rect(rect.x, top - 8f, rect.width, rect.height + 8f));
                DrawCard(rect, Cards[i], i, active, lift);
            }

            KeyPrompts.DrawHints(ink, texts, new[]
            {
                new Hint(KeyPrompts.Navigate(PromptAxis.Horizontal), "aide.choisir"),
                new Hint(KeyPrompts.Confirm(), "aide.valider"),
                new Hint(KeyPrompts.Back(), "aide.rester"),
            }, box.x, box.yMax - 46f, box.width, 14, inkColor, OptionsPanel.WithAlpha(inkColor, 0.6f), paperColor);
        }

        void DrawCard(Rect r, Choice choice, int index, bool active, float lift)
        {
            var border = active ? accentColor : inkColor;
            var inner = Engraving.CardFrame(ink, r, paperColor, border, active ? 3f : 1.5f, inkColor, inkColor, lift);
            texts.Label((index + 1).ToString(), inner.x + 6f, inner.y + 4f, 20f, 13, Engraving.Fade(inkColor, paperColor, 0.35f), TextAnchor.UpperLeft, FontStyle.Bold);

            var up = choice == Choice.Ascend;
            DrawStairs(new Vector2(inner.center.x, inner.y + 56f), 30f, up, active ? accentColor : inkColor);

            var band = new Rect(inner.x - 4f, inner.y + 108f, inner.width + 8f, 22f);
            Engraving.Banner(ink, texts, band, Loc.T(up ? "remontee.remonter" : "remontee.descendre"), paperColor, inkColor, 13);
            var faded = Engraving.Fade(inkColor, paperColor, 0.2f);
            if (up)
            {
                texts.Label(Loc.F("remontee.or_ramene", _gold), inner.x, band.yMax + 12f, inner.width, 14, active ? accentColor : inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
                texts.Label(Loc.T("remontee.fin_expedition"), inner.x, band.yMax + 34f, inner.width, 12, faded, TextAnchor.UpperCenter, FontStyle.Italic);
            }
            else
            {
                texts.Label(Loc.F("hud.etage", _floor + 1), inner.x, band.yMax + 12f, inner.width, 14, active ? accentColor : inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
                texts.Label(Loc.T("remontee.or_en_jeu"), inner.x, band.yMax + 34f, inner.width, 12, faded, TextAnchor.UpperCenter, FontStyle.Italic);
            }
        }

        /// <summary>Marches gravées et une flèche : vers le haut (remonter) ou vers le bas (descendre).</summary>
        void DrawStairs(Vector2 c, float s, bool up, Color color)
        {
            // Quatre marches montant vers la droite.
            var step = s * 0.42f;
            var left = c.x - step * 2f;
            var bottom = c.y + step * 2f;
            var pts = new List<Vector2> { new Vector2(left, bottom) };
            for (var k = 0; k < 4; k++)
            {
                var x0 = left + k * step;
                var y = bottom - (k + 1) * step;
                pts.Add(new Vector2(x0, y));
                pts.Add(new Vector2(x0 + step, y));
            }
            pts.Add(new Vector2(left + 4f * step, bottom));
            ink.Polyline(pts, color, 2f, true);
            ink.Hatch(new Rect(left, bottom - step, step * 4f, step), 3f, color, 1f);
            // Flèche au-dessus des marches, dans le sens du choix.
            var a = new Vector2(left + step * 0.4f, bottom - step * 3.2f);
            var b = new Vector2(left + step * 2.6f, bottom - step * 5.2f);
            var (from, to) = up ? (a, b) : (b, a);
            ink.Line(from, to, color, 2.5f);
            var dir = (to - from).normalized;
            var side = new Vector2(-dir.y, dir.x);
            // Pointe : les deux sens de tracé (un triangle à l'envers n'est pas dessiné).
            ink.Triangle(to + dir * 7f, to - dir * 4f + side * 6f, to - dir * 4f - side * 6f, color);
            ink.Triangle(to + dir * 7f, to - dir * 4f - side * 6f, to - dir * 4f + side * 6f, color);
        }
    }
}
