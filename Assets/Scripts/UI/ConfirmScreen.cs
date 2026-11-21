using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Demande de confirmation gravée (oui / non), par exemple avant de descendre dans le donjon
    /// depuis le camp (docs/camp.md). Le jeu se met en pause ; deux boutons-bandeaux, le choix
    /// en rouge. Échap / bouton droit : non.
    ///
    /// Commandes (map « UI » d'InputSystem_Actions, plus la souris) :
    ///   ← → (Q D, stick, croix), souris : choisir ; Entrée / bouton du bas, clic : valider
    /// </summary>
    public class ConfirmScreen : MonoBehaviour
    {
        [SerializeField] InkCanvas ink;
        [SerializeField] TextPool texts;

        [Header("Actions (InputSystem_Actions)")]
        [SerializeField] InputActionReference navigate;
        [SerializeField] InputActionReference submit;
        [SerializeField] InputActionReference cancel;

        [Header("Réglages")]
        [Tooltip("Délai (secondes, temps réel) avant d'accepter un choix : la touche qui a fait avancer ne valide pas à l'aveugle.")]
        [SerializeField, Range(0f, 1.5f)] float acceptDelay = 0.35f;

        [Header("Style")]
        [SerializeField] Color paperColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] Color inkColor = new Color(0.06f, 0.06f, 0.07f);
        [Tooltip("Rouge d'accent : le bouton choisi.")]
        [SerializeField] Color accentColor = new Color(0.85f, 0.1f, 0.1f);

        static readonly Vector2 ButtonSize = new Vector2(170f, 30f);
        const float ButtonGap = 30f;

        /// <summary>Vrai tant qu'une confirmation est ouverte (la pause ne s'ouvre pas par-dessus : jeu en pause).</summary>
        public static bool IsShown { get; private set; }

        public bool IsOpen { get; private set; }
        /// <summary>Bouton choisi : 0 = oui, 1 = non.</summary>
        public int Selected { get; private set; }
        /// <summary>La réponse à la fermeture : vrai = oui.</summary>
        public bool Confirmed { get; private set; }

        Canvas _canvas;
        RectTransform _root;
        InputAction _navigate, _submit, _cancel;
        readonly List<Rect> _buttons = new List<Rect>();
        float _openedAt;
        int _heldX;
        Vector2 _lastMouse;
        string _title = "", _text = "", _yes = "", _no = "";

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

        /// <summary>Pose la question (codes clés traduits : titre, texte, oui, non). « Oui » est choisi d'office.</summary>
        public void Open(string titleKey, string textKey, string yesKey, string noKey)
        {
            _title = Loc.T(titleKey);
            _text = Loc.T(textKey);
            _yes = Loc.T(yesKey);
            _no = Loc.T(noKey);
            Confirmed = false;
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

        /// <summary>Répond : vrai = oui. Ferme l'écran.</summary>
        public void Answer(bool yes)
        {
            if (!IsOpen)
                return;
            Confirmed = yes;
            SetOpen(false);
        }

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
            if (_cancel != null && _cancel.WasPressedThisFrame())
            {
                Answer(false);
                return;
            }
            var v = _navigate != null ? _navigate.ReadValue<Vector2>() : Vector2.zero;
            var x = Mathf.Abs(v.x) > 0.5f && Mathf.Abs(v.x) >= Mathf.Abs(v.y) ? (int)Mathf.Sign(v.x) : 0;
            if (x != 0 && x != _heldX)
                Selected = 1 - Selected;
            _heldX = x;
            if (_submit != null && _submit.WasPressedThisFrame())
                Answer(Selected == 0);
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
            var hit = _buttons.FindIndex(b => b.Contains(pos));
            if (hit < 0)
                return;
            if (moved)
                Selected = hit;
            if (mouse.leftButton.wasPressedThisFrame)
                Answer(hit == 0);
        }

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
            ink.Rect(new Rect(Vector2.zero, screen), OptionsPanel.WithAlpha(inkColor, 0.5f));
            var size = new Vector2(Mathf.Max(440f, TextPool.Width(_text, 15, FontStyle.Italic) + 90f), 210f);
            var box = new Rect((screen - size) * 0.5f, size);
            ink.Rect(new Rect(box.x + 6f, box.y + 6f, box.width, box.height), inkColor);
            ink.Rect(box, paperColor);
            Engraving.DoubleFrame(ink, OptionsPanel.Shrink(box, 14f), 5f, inkColor);

            texts.Label(_title.ToUpperInvariant(), box.x, box.y + 30f, box.width, 22, inkColor, TextAnchor.UpperCenter, FontStyle.Bold);
            Engraving.Rule(ink, new Vector2(box.center.x - 80f, box.y + 66f), 160f, inkColor);
            texts.Label(_text, box.x, box.y + 76f, box.width, 15, Engraving.Fade(inkColor, paperColor, 0.25f), TextAnchor.UpperCenter, FontStyle.Italic);

            _buttons.Clear();
            var total = ButtonSize.x * 2f + ButtonGap;
            var y = box.y + 112f;
            for (var i = 0; i < 2; i++)
            {
                var r = new Rect(box.center.x - total * 0.5f + i * (ButtonSize.x + ButtonGap), y, ButtonSize.x, ButtonSize.y);
                _buttons.Add(r);
                var active = i == Selected;
                Engraving.Banner(ink, texts, r, i == 0 ? _yes : _no, paperColor, active ? accentColor : inkColor, 14);
            }

            KeyPrompts.DrawHints(ink, texts, new[]
            {
                new Hint(KeyPrompts.Navigate(PromptAxis.Horizontal), "aide.choisir"),
                new Hint(KeyPrompts.Confirm(), "aide.valider"),
                new Hint(KeyPrompts.Back(), "aide.retour"),
            }, box.x, box.yMax - 44f, box.width, 13, inkColor, OptionsPanel.WithAlpha(inkColor, 0.6f), paperColor);
        }
    }
}
