using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Menu pause, dessiné en gravure comme le menu et la carte (feuille papier à double filet) :
    /// Continuer, Options (le panneau du menu, <see cref="OptionsPanel"/>), Sauvegarder (à venir),
    /// Retour au menu, Quitter le jeu. Met le jeu en pause (<c>Time.timeScale = 0</c>), comme la carte.
    ///
    /// Ouvert / fermé avec l'action Player/Pause (Échap, Start). La carte passe d'abord : Échap
    /// carte ouverte ferme la carte, pas plus. Ne s'ouvre pas sur l'écran de mort.
    ///
    /// Commandes (map « UI » d'InputSystem_Actions, plus la souris) :
    ///   souris / ↑ ↓ (Z S, stick, croix) : choisir
    ///   clic / Entrée / bouton du bas : valider
    ///   Échap / bouton droit, Start, clic hors du cadre : reprendre (ou fermer les options)
    /// </summary>
    public class PauseScreen : MonoBehaviour
    {
        [Tooltip("Le joueur (pas de pause sur l'écran de mort). Vide : cherché dans la scène.")]
        [SerializeField] PlayerController player;
        [Tooltip("L'écran de carte (Échap le ferme d'abord). Vide : cherché dans la scène.")]
        [SerializeField] MapScreen map;
        [SerializeField] InkCanvas ink;
        [SerializeField] TextPool texts;

        [Header("Actions (InputSystem_Actions)")]
        [Tooltip("Ouvrir / fermer la pause (Player/Pause : Échap, Start).")]
        [SerializeField] InputActionReference toggle;
        [SerializeField] InputActionReference navigate;
        [SerializeField] InputActionReference submit;
        [SerializeField] InputActionReference cancel;

        [Header("Style")]
        [SerializeField] Color paperColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] Color inkColor = new Color(0.06f, 0.06f, 0.07f);
        [Tooltip("Rouge d'accent : ligne choisie, réglage choisi.")]
        [SerializeField] Color accentColor = new Color(0.85f, 0.1f, 0.1f);

        public enum Entry { Continue, Options, Save, Menu, Desktop }

        static readonly string[] Labels = { "pause.continuer", "menu.options", "pause.sauvegarder", "pause.menu", "pause.bureau" };
        const float RowHeight = 46f, QuitGap = 22f;
        const float RepeatDelay = 0.4f, RepeatRate = 0.12f;
        /// <summary>La sauvegarde de suspension viendra plus tard (docs/todo.md) : ligne grisée d'ici là.</summary>
        const bool SaveAvailable = false;

        /// <summary>Vrai tant qu'un menu pause est ouvert (la carte ne s'ouvre pas par-dessus).</summary>
        public static bool IsPaused { get; private set; }

        public bool IsOpen { get; private set; }
        public Entry Selected { get; private set; }
        public bool OptionsOpen { get; private set; }
        public OptionsPanel Options => _options;

        Canvas _canvas;
        RectTransform _root;
        OptionsPanel _options;
        InputAction _toggle, _navigate, _submit, _cancel;
        readonly Rect[] _rowRects = new Rect[Labels.Length];
        Rect _box;
        int _openedFrame = -1;
        Vector2Int _heldDir;
        float _nextRepeat;
        Vector2 _lastMouse;

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _root = (RectTransform)transform;
            _options = new OptionsPanel(paperColor, inkColor, accentColor);
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (map == null)
                map = FindAnyObjectByType<MapScreen>();
            _toggle = Resolve(toggle, "Player/Pause");
            _navigate = Resolve(navigate, "UI/Navigate");
            _submit = Resolve(submit, "UI/Submit");
            _cancel = Resolve(cancel, "UI/Cancel");
            SetOpen(false);
        }

        static InputAction Resolve(InputActionReference reference, string path) =>
            reference != null ? reference.action : InputSystem.actions?.FindAction(path);

        void OnEnable()
        {
            _toggle?.Enable();
            ControlDevice.Listen();
        }

        void OnDisable()
        {
            if (IsOpen)
                SetOpen(false);
        }

        void OnDestroy()
        {
            if (IsOpen)
                IsPaused = false;
        }

        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);

        void SetOpen(bool open)
        {
            if (open && !IsOpen)
                _openedFrame = Time.frameCount;
            IsOpen = open;
            IsPaused = open;
            if (_canvas != null)
                _canvas.enabled = open;
            Time.timeScale = open ? 0f : 1f;
            foreach (var action in new[] { _navigate, _submit, _cancel })
                action?.Enable(); // partagées avec l'écran de mort : on ne les coupe pas en fermant
            Selected = Entry.Continue;
            OptionsOpen = false;
            _options.Reset();
            _heldDir = Vector2Int.zero;
            if (open && Mouse.current != null)
                _lastMouse = Mouse.current.position.ReadValue();
            if (open)
                Redraw();
        }

        /// <summary>
        /// Pas de pause sur l'écran de mort ni carte ouverte. Échap ferme aussi la carte (Map/Close)
        /// le même frame : une carte fermée à ce frame-ci compte comme ouverte.
        /// </summary>
        bool CanOpen()
        {
            if (player != null && player.IsDead)
                return false;
            if (map != null && (map.IsOpen || map.ClosedFrame == Time.frameCount))
                return false;
            return Time.timeScale != 0f;
        }

        // --- Entrées -----------------------------------------------------------------------

        void Update()
        {
            if (!IsOpen)
            {
                if (Pressed(_toggle) && CanOpen())
                    Open();
                return;
            }
            // Le frame d'ouverture, Échap déclenche aussi UI/Cancel : on ne referme pas aussitôt.
            if (Time.frameCount != _openedFrame)
            {
                HandleMouse();
                if (IsOpen)
                    HandleKeys();
            }
            if (IsOpen)
                Redraw();
        }

        static bool Pressed(InputAction action) => action != null && action.WasPressedThisFrame();

        void HandleKeys()
        {
            var dir = ReadDirection();
            var cancelled = Pressed(_cancel);
            var toggled = Pressed(_toggle);
            var submitted = Pressed(_submit);
            if (OptionsOpen)
            {
                // Échap (Cancel) revient à la liste ; Start (Pause seule) reprend le jeu.
                if (cancelled)
                    CloseOptions();
                else if (toggled)
                    Close();
                else
                    _options.Navigate(dir, submitted);
                return;
            }
            if (cancelled || toggled)
                Close();
            else if (dir.y != 0)
                Step(dir.y > 0 ? -1 : 1);
            else if (submitted)
                Activate();
        }

        /// <summary>Ligne suivante (+1) ou précédente (−1), en boucle, en sautant les lignes grisées.</summary>
        void Step(int step)
        {
            var n = Labels.Length;
            var i = (int)Selected;
            do i = (i + step + n) % n;
            while (!IsEnabled((Entry)i));
            Selected = (Entry)i;
        }

        static bool IsEnabled(Entry entry) => entry != Entry.Save || SaveAvailable;

        /// <summary>Direction de Navigate, avec répétition si on la garde (pour régler le son).</summary>
        Vector2Int ReadDirection()
        {
            var v = _navigate != null ? _navigate.ReadValue<Vector2>() : Vector2.zero;
            var dir = Mathf.Abs(v.x) >= Mathf.Abs(v.y)
                ? new Vector2Int(Mathf.Abs(v.x) > 0.5f ? (int)Mathf.Sign(v.x) : 0, 0)
                : new Vector2Int(0, Mathf.Abs(v.y) > 0.5f ? (int)Mathf.Sign(v.y) : 0);
            var now = Time.unscaledTime;
            if (dir == Vector2Int.zero || dir != _heldDir)
            {
                _heldDir = dir;
                _nextRepeat = now + RepeatDelay;
                return dir;
            }
            if (OptionsOpen && now >= _nextRepeat)
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
            if (OptionsOpen)
            {
                if (_options.HandleMouse(mouse, pos, moved))
                    CloseOptions();
                return;
            }
            var hit = RowAt(pos);
            if (moved && hit >= 0 && IsEnabled((Entry)hit))
                Selected = (Entry)hit;
            if (!mouse.leftButton.wasPressedThisFrame)
                return;
            if (hit >= 0)
            {
                if (IsEnabled((Entry)hit))
                {
                    Selected = (Entry)hit;
                    Activate();
                }
            }
            else if (!_box.Contains(pos))
                Close();
        }

        int RowAt(Vector2 pos)
        {
            for (var i = 0; i < _rowRects.Length; i++)
                if (_rowRects[i].Contains(pos))
                    return i;
            return -1;
        }

        /// <summary>Position écran → coordonnées du canvas (origine en haut à gauche, y vers le bas).</summary>
        Vector2 ToCanvas(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out var local);
            var r = _root.rect;
            return new Vector2(local.x - r.xMin, r.yMax - local.y);
        }

        /// <summary>Valide la ligne choisie.</summary>
        public void Activate()
        {
            switch (Selected)
            {
                case Entry.Continue:
                    Close();
                    break;
                case Entry.Options:
                    OptionsOpen = true;
                    _options.Reset();
                    break;
                case Entry.Save:
                    break; // à venir : sauvegarde de suspension (docs/todo.md)
                case Entry.Menu:
                    IsPaused = false;
                    SceneFlow.LoadMenu();
                    break;
                case Entry.Desktop:
                    SceneFlow.Quit();
                    break;
            }
        }

        void CloseOptions()
        {
            OptionsOpen = false;
            Selected = Entry.Options;
        }

        /// <summary>Choisit une ligne (captures, tests).</summary>
        public void Select(Entry entry) => Selected = entry;

        /// <summary>Ouvre ou ferme le panneau Options (captures, tests).</summary>
        public void ShowOptions(bool show)
        {
            OptionsOpen = show;
            _options.Reset();
            Redraw();
        }

        // --- Dessin ------------------------------------------------------------------------

        void Redraw()
        {
            if (ink == null || texts == null)
                return;
            ink.Clear();
            texts.Begin();
            var screen = _root.rect.size;
            if (OptionsOpen)
                _options.Draw(ink, texts, screen);
            else
                DrawList(screen);
            ink.Commit();
            texts.End();
        }

        void DrawList(Vector2 screen)
        {
            var height = 100f + Labels.Length * RowHeight + QuitGap + 70f;
            var box = OptionsPanel.DrawSheet(ink, texts, screen, new Vector2(420f, height), "pause.titre", paperColor, inkColor);
            _box = box;
            var labelX = box.x + 70f;
            var y = box.y + 118f;
            for (var i = 0; i < Labels.Length; i++)
            {
                var entry = (Entry)i;
                if (entry == Entry.Menu)
                {
                    // Un filet sépare les sorties du reste.
                    Engraving.Rule(ink, new Vector2(box.center.x - 60f, y - 30f + QuitGap * 0.5f), 120f, OptionsPanel.WithAlpha(inkColor, 0.5f));
                    y += QuitGap;
                }
                _rowRects[i] = new Rect(box.x + 24f, y - 32f, box.width - 48f, RowHeight - 4f);
                var enabled = IsEnabled(entry);
                var active = entry == Selected && enabled;
                if (active)
                    OptionsPanel.DrawMarker(ink, labelX, y, accentColor);
                var color = active ? accentColor : enabled ? inkColor : OptionsPanel.WithAlpha(inkColor, 0.35f);
                texts.Label(Loc.T(Labels[i]), labelX, y - 23f, box.width - 140f, 21, color, TextAnchor.UpperLeft, FontStyle.Bold);
                if (!enabled)
                    texts.Label(Loc.T("pause.a_venir"), box.x, y - 19f, box.width - 48f, 14, OptionsPanel.WithAlpha(inkColor, 0.45f), TextAnchor.UpperRight, FontStyle.Italic);
                y += RowHeight;
            }
            KeyPrompts.DrawHints(ink, texts, new[]
            {
                new Hint(KeyPrompts.Navigate(PromptAxis.Vertical), "aide.choisir"),
                new Hint(KeyPrompts.Confirm(), "aide.valider"),
                new Hint(KeyPrompts.Back(), "aide.reprendre"),
            }, box.x, box.yMax - 46f, box.width, 14, inkColor, OptionsPanel.WithAlpha(inkColor, 0.6f), paperColor);
        }
    }
}
