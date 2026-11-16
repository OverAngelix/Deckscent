using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Interface en jeu, dessinée en gravure comme la carte (papier, encre, rouge d'accent) :
    /// cartouche des PV (une case par point de vie) avec Tour / ATQ / DÉF dessous, mission de
    /// l'étage en haut à droite (<see cref="Dungeon.Mission"/>), message
    /// éphémère en haut au centre (<see cref="PlayerController.Notify"/>), voile rouge quand on est
    /// touché, écran de mort (Entrée ou bouton du bas pour recommencer, Échap pour revenir au menu).
    ///
    /// Canvas en surimpression : le post-process gravure ne le touche pas, couleurs libres.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        [Tooltip("Le joueur suivi. Vide : cherché dans la scène.")]
        [SerializeField] PlayerController player;
        [Tooltip("Le donjon (mission de l'étage, en haut à droite). Vide : cherché dans la scène ; sans donjon, pas de mission.")]
        [SerializeField] Dungeon dungeon;
        [SerializeField] InkCanvas ink;
        [SerializeField] TextPool texts;

        [Header("Actions (InputSystem_Actions)")]
        [Tooltip("Recommencer après la mort (Player/Wait : Entrée, L3).")]
        [SerializeField] InputActionReference restart;
        [Tooltip("Recommencer après la mort, autre bouton (Player/Interact : F, bouton du bas).")]
        [SerializeField] InputActionReference restartAlt;
        [Tooltip("Retour au menu depuis l'écran de mort (UI/Cancel : Échap, bouton droit).")]
        [SerializeField] InputActionReference toMenu;

        [Header("Style")]
        [SerializeField] Color paperColor = new Color(0.97f, 0.96f, 0.93f);
        [SerializeField] Color inkColor = new Color(0.06f, 0.06f, 0.07f);
        [Tooltip("Le rouge des PV, du voile quand on est touché et de « Vous êtes mort ».")]
        [SerializeField] Color healthColor = new Color(0.85f, 0.1f, 0.1f);
        [Tooltip("Opacité du voile rouge au moment du coup (il s'efface ensuite).")]
        [SerializeField, Range(0f, 1f)] float hurtFlash = 0.35f;
        [Tooltip("Durée d'affichage d'un message (secondes), hors apparition et effacement.")]
        [SerializeField, Min(0.1f)] float toastTime = 1.8f;
        [Tooltip("Délai avant d'accepter « recommencer » sur l'écran de mort (évite un redémarrage involontaire).")]
        [SerializeField, Min(0f)] float restartDelay = 1f;

        const float Margin = 16f, PipW = 14f, PipH = 18f, PipGap = 4f, LabelW = 34f;
        const float ToastIn = 0.15f, ToastOut = 0.5f, FlashTime = 0.4f;

        public string Toast => _toast;
        public float DeadTime => _deadTime;

        InputAction _restart, _restartAlt, _toMenu;
        string _toast = "";
        float _toastAge = -1f;
        float _flashAge = -1f;
        float _deadTime = -1f;
        bool _dirty = true;
        Mission _shownMission;
        int _shownVersion = -1;

        void Awake()
        {
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
            if (dungeon == null)
                dungeon = FindAnyObjectByType<Dungeon>();
            _restart = Resolve(restart, "Player/Wait");
            _restartAlt = Resolve(restartAlt, "Player/Interact");
            _toMenu = Resolve(toMenu, "UI/Cancel");
        }

        static InputAction Resolve(InputActionReference reference, string path) =>
            reference != null ? reference.action : InputSystem.actions?.FindAction(path);

        void OnEnable()
        {
            GameSettings.Changed += MarkDirty;
            ControlDevice.Listen();
            ControlDevice.Changed += MarkDirty;
            if (player == null)
                return;
            player.HealthChanged += OnHealthChanged;
            player.Hurt += OnHurt;
            player.Died += OnDied;
            player.Notified += ShowToast;
            player.TurnEnded += OnTurnEnded;
        }

        void OnDisable()
        {
            GameSettings.Changed -= MarkDirty;
            ControlDevice.Changed -= MarkDirty;
            if (player == null)
                return;
            player.HealthChanged -= OnHealthChanged;
            player.Hurt -= OnHurt;
            player.Died -= OnDied;
            player.Notified -= ShowToast;
            player.TurnEnded -= OnTurnEnded;
        }

        void MarkDirty() => _dirty = true;
        void OnHealthChanged(int health, int max) => _dirty = true;
        void OnTurnEnded(int turn) => _dirty = true;
        void OnHurt(int amount) => _flashAge = 0f;
        void OnDied() => _deadTime = 0f;

        /// <summary>Affiche un message en haut de l'écran pendant ~2 s, puis l'efface.</summary>
        public void ShowToast(string text)
        {
            _toast = text;
            _toastAge = 0f;
        }

        void Update()
        {
            // Temps réel : la carte (pause) ne fige pas le HUD.
            var dt = Time.unscaledDeltaTime;
            if (_toastAge >= 0f)
            {
                _toastAge += dt;
                if (_toastAge > ToastIn + toastTime + ToastOut)
                    _toastAge = -1f;
                _dirty = true;
            }
            if (_flashAge >= 0f)
            {
                _flashAge += dt;
                if (_flashAge > FlashTime)
                    _flashAge = -1f;
                _dirty = true;
            }
            if (_deadTime >= 0f)
            {
                var before = _deadTime;
                _deadTime += dt;
                if (before < restartDelay + 1f)
                    _dirty = true;
                if (_deadTime >= restartDelay && (Pressed(_restart) || Pressed(_restartAlt)))
                    Restart();
                else if (_deadTime >= restartDelay && Pressed(_toMenu))
                    SceneFlow.LoadMenu();
            }
            var mission = dungeon != null ? dungeon.Mission : null;
            if (mission != _shownMission || (mission != null && mission.Version != _shownVersion))
            {
                _shownMission = mission;
                _shownVersion = mission != null ? mission.Version : -1;
                _dirty = true;
            }
            if (_dirty)
                Redraw();
        }

        static bool Pressed(InputAction action)
        {
            if (action == null)
                return false;
            if (!action.enabled)
                action.Enable();
            return action.WasPressedThisFrame();
        }

        /// <summary>Recharge la scène (nouvelle partie).</summary>
        public void Restart() => SceneFlow.Reload();

        public void Redraw()
        {
            _dirty = false;
            if (ink == null || texts == null)
                return;
            ink.Clear();
            texts.Begin();
            if (player != null)
                Draw();
            ink.Commit();
            texts.End();
        }

        void Draw()
        {
            var screen = ink.Size;
            if (_flashAge >= 0f)
                ink.Rect(new Rect(Vector2.zero, screen), WithAlpha(healthColor, hurtFlash * (1f - _flashAge / FlashTime)));

            DrawHealth();
            if (_shownMission != null)
                DrawMission(screen, _shownMission);
            DrawToast(screen);
            if (_deadTime >= 0f)
                DrawDeath(screen);
        }

        /// <summary>Cartouche des PV : « PV » puis une case par point de vie, Tour / ATQ / DÉF dessous.</summary>
        void DrawHealth()
        {
            var max = player.MaxHealth;
            var pipsW = max * (PipW + PipGap) - PipGap;
            var box = new Rect(Margin + 4f, Margin + 4f, LabelW + pipsW + 22f, PipH + 34f);
            ink.Rect(new Rect(box.x - 4f, box.y - 4f, box.width + 8f, box.height + 8f), paperColor);
            Engraving.DoubleFrame(ink, box, 4f, inkColor);
            var x = box.x + 10f;
            var y = box.y + 9f;
            texts.Label(Loc.T("hud.pv"), x, y - 1f, LabelW, 16, inkColor, TextAnchor.UpperLeft, FontStyle.Bold);
            for (var i = 0; i < max; i++)
            {
                var pip = new Rect(x + LabelW + i * (PipW + PipGap), y, PipW, PipH);
                if (i < player.Health)
                    ink.Rect(pip, healthColor);
                ink.RectOutline(pip, inkColor, 1.5f);
            }
            var stats = $"{Loc.F("hud.tour", player.Turn)}   ·   {Loc.F("hud.atq", player.Attack)}   ·   {Loc.F("hud.def", player.Defense)}";
            texts.Label(stats, x, y + PipH + 4f, box.width - 20f, 12, Engraving.Fade(inkColor, paperColor, 0.15f), TextAnchor.UpperLeft, FontStyle.Italic);
        }

        /// <summary>
        /// Mission de l'étage, en haut à droite : un objectif par ligne, case cochée quand il est
        /// rempli, compte à droite. Titre en rouge quand tout est rempli.
        /// </summary>
        void DrawMission(Vector2 screen, Mission mission)
        {
            const float w = 214f, rowH = 19f;
            var box = new Rect(screen.x - Margin - 4f - w, Margin + 4f, w, 30f + mission.Goals.Count * rowH);
            ink.Rect(new Rect(box.x - 4f, box.y - 4f, box.width + 8f, box.height + 8f), paperColor);
            Engraving.DoubleFrame(ink, box, 4f, inkColor);
            var x = box.x + 10f;
            var title = Loc.T(mission.IsComplete ? "mission.remplie" : "mission.titre").ToUpperInvariant();
            texts.Label(title, x, box.y + 6f, box.width - 20f, 13, mission.IsComplete ? healthColor : inkColor, TextAnchor.UpperLeft, FontStyle.Bold);
            var y = box.y + 27f;
            foreach (var goal in mission.Goals)
            {
                MissionGoalRow(ink, texts, goal, new Rect(x, y, box.width - 20f, rowH), 12, inkColor, paperColor, healthColor);
                y += rowH;
            }
        }

        /// <summary>Une ligne d'objectif : case (cochée en rouge si rempli), libellé, compte. Partagée avec la carte.</summary>
        /// `stacked` : le compte sous le libellé (colonne étroite de la carte).
        public static void MissionGoalRow(InkCanvas ink, TextPool texts, MissionGoal goal, Rect row, int size, Color inkColor, Color paperColor, Color accent, bool stacked = false)
        {
            var faded = Engraving.Fade(inkColor, paperColor, 0.45f);
            var box = new Rect(row.x, row.y + 2f, 11f, 11f);
            ink.Rect(box, paperColor);
            ink.RectOutline(box, inkColor, 1.5f);
            if (goal.Done)
            {
                ink.Line(new Vector2(box.x + 2f, box.center.y), new Vector2(box.x + 4.5f, box.yMax - 2f), accent, 2.2f);
                ink.Line(new Vector2(box.x + 4.5f, box.yMax - 2f), new Vector2(box.xMax + 2f, box.y - 2f), accent, 2.2f);
            }
            var color = goal.Done ? faded : inkColor;
            texts.Label(Loc.T(goal.LabelKey), row.x + 18f, row.y - 1f, row.width - 18f, size, color);
            var count = $"{goal.Progress} / {goal.Target}";
            if (stacked)
                texts.Label(count, row.x + 18f, row.y + size + 2f, row.width - 18f, size - 1, Engraving.Fade(color, paperColor, 0.25f), TextAnchor.UpperLeft, FontStyle.Italic);
            else
                texts.Label(count, row.x, row.y - 1f, row.width, size, color, TextAnchor.UpperRight, FontStyle.Italic);
        }

        /// <summary>Message (piège franchi, butin…) : cartouche papier en haut au centre, qui apparaît puis s'efface.</summary>
        void DrawToast(Vector2 screen)
        {
            if (_toastAge < 0f || string.IsNullOrEmpty(_toast))
                return;
            var a = _toastAge < ToastIn ? _toastAge / ToastIn
                : _toastAge < ToastIn + toastTime ? 1f
                : 1f - (_toastAge - ToastIn - toastTime) / ToastOut;
            a = Mathf.Clamp01(a);
            const int size = 18;
            var w = TextPool.Width(_toast, size) + 40f;
            var box = new Rect((screen.x - w) * 0.5f, Margin + 4f, w, 34f);
            ink.Rect(new Rect(box.x - 4f, box.y - 4f, box.width + 8f, box.height + 8f), WithAlpha(paperColor, a));
            var frameInk = WithAlpha(inkColor, a);
            ink.RectOutline(new Rect(box.x - 3f, box.y - 3f, box.width + 6f, box.height + 6f), frameInk, 2f);
            ink.RectOutline(box, frameInk, 1f);
            texts.Label(_toast, box.x, box.y + (box.height - size * 1.4f) * 0.5f, box.width, size, frameInk, TextAnchor.MiddleCenter);
        }

        /// <summary>L'écran s'assombrit, « Vous êtes mort », puis l'invitation à recommencer.</summary>
        void DrawDeath(Vector2 screen)
        {
            var a = Mathf.Clamp01(_deadTime / 0.8f);
            ink.Rect(new Rect(Vector2.zero, screen), WithAlpha(inkColor, 0.75f * a));
            var cy = screen.y * 0.5f;
            texts.Label(Loc.T("hud.mort"), 0f, cy - 40f, screen.x, 48, WithAlpha(healthColor, a), TextAnchor.UpperCenter, FontStyle.Bold);
            texts.Label(Loc.F("hud.tour", player.Turn), 0f, cy + 22f, screen.x, 16, WithAlpha(paperColor, a * 0.7f), TextAnchor.UpperCenter, FontStyle.Italic);
            if (_deadTime >= restartDelay)
            {
                var b = Mathf.Clamp01((_deadTime - restartDelay) / 0.4f);
                // Recommencer : Entrée au clavier (Player/Wait), le bouton du bas à la manette (Player/Interact, L3 est peu parlant).
                var restartKeys = KeyPrompts.Of(ControlDevice.IsGamepad ? "Player/Interact" : "Player/Wait");
                KeyPrompts.DrawHints(ink, texts, new[] { new Hint(restartKeys, "aide.recommencer"), new Hint(KeyPrompts.Back(), "aide.menu") },
                    0f, cy + 52f, screen.x, 18, WithAlpha(paperColor, b), WithAlpha(paperColor, b), WithAlpha(inkColor, b));
            }
        }

        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
    }
}
