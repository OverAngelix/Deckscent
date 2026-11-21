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
    /// Chaque coup porté fait monter un chiffre de dégâts (« −2 ») au-dessus de l'ennemi, sans cadre
    /// (choix de l'utilisateur).
    /// Le boss éveillé a sa barre de vie gravée en bas au centre ; le passage d'un étage à l'autre
    /// voile l'écran d'encre (<see cref="FadeOut"/>, <see cref="FadeIn"/>) avec le numéro de l'étage.
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
        [Tooltip("Durée d'un chiffre de dégâts, qui monte puis s'efface (secondes).")]
        [SerializeField, Min(0.1f)] float popupTime = 0.9f;
        [Tooltip("Délai avant d'accepter « recommencer » sur l'écran de mort (évite un redémarrage involontaire).")]
        [SerializeField, Min(0f)] float restartDelay = 1f;

        const float Margin = 16f, PipW = 14f, PipH = 18f, PipGap = 4f, LabelW = 34f;
        /// <summary>Largeur du cartouche des PV, en cases (les PV de départ).</summary>
        const int PipSlots = 10;
        const float ToastIn = 0.15f, ToastOut = 0.5f, FlashTime = 0.4f;

        public string Toast => _toast;
        public float DeadTime => _deadTime;
        /// <summary>Temps (réel) depuis l'écran de retour à la surface, -1 s'il n'est pas affiché.</summary>
        public float SurfaceTime => _surfaceTime;

        /// <summary>Remontée réussie : l'écran se voile, « De retour à la surface », l'or ramené, puis le menu.</summary>
        public void ShowSurface(int broughtGold)
        {
            _surfaceGold = broughtGold;
            _surfaceTime = 0f;
            _dirty = true;
        }

        InputAction _restart, _restartAlt, _toMenu;
        string _toast = "";
        float _toastAge = -1f;
        float _flashAge = -1f;
        float _deadTime = -1f;
        float _surfaceTime = -1f;
        int _surfaceGold;
        bool _dirty = true;
        Mission _shownMission;
        int _shownVersion = -1;
        float _fade;

        /// <summary>Bilan du sous-sol qu'on vient de quitter, affiché sous le titre pendant la descente.</summary>
        public string FloorSummary { get; set; } = "";
        int _bossHealth = -1;

        /// <summary>Un chiffre de dégâts au-dessus d'un ennemi (point du monde).</summary>
        struct Popup
        {
            public Vector3 World;
            public string Text;
            public float Age;
        }

        readonly System.Collections.Generic.List<Popup> _popups = new System.Collections.Generic.List<Popup>();
        Camera _camera;

        /// <summary>Chiffres de dégâts en cours (tests).</summary>
        public int PopupCount => _popups.Count;

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
            Enemy.Damaged += OnEnemyDamaged;
            ControlDevice.Listen();
            ControlDevice.Changed += MarkDirty;
            if (player == null)
                return;
            player.HealthChanged += OnHealthChanged;
            player.Hurt += OnHurt;
            player.Died += OnDied;
            player.Notified += ShowToast;
            player.TurnEnded += OnTurnEnded;
            player.ExperienceChanged += MarkDirty;
        }

        void OnDisable()
        {
            GameSettings.Changed -= MarkDirty;
            Enemy.Damaged -= OnEnemyDamaged;
            ControlDevice.Changed -= MarkDirty;
            if (player == null)
                return;
            player.HealthChanged -= OnHealthChanged;
            player.Hurt -= OnHurt;
            player.Died -= OnDied;
            player.Notified -= ShowToast;
            player.TurnEnded -= OnTurnEnded;
            player.ExperienceChanged -= MarkDirty;
        }

        void MarkDirty() => _dirty = true;
        void OnHealthChanged(int health, int max) => _dirty = true;
        void OnTurnEnded(int turn) => _dirty = true;
        void OnHurt(int amount)
        {
            _flashAge = 0f;
        }

        void OnEnemyDamaged(Enemy enemy, int amount)
        {
            // Au-dessus de la tête (un loup-garou est plus grand qu'un slime).
            var top = enemy.transform.position + Vector3.up * (enemy is Werewolf ? 2.6f : 1.3f);
            _popups.Add(new Popup { World = top, Text = "−" + amount });
            _dirty = true;
        }
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
            if (_popups.Count > 0)
            {
                for (var i = _popups.Count - 1; i >= 0; i--)
                {
                    var popup = _popups[i];
                    popup.Age += dt;
                    if (popup.Age > popupTime)
                        _popups.RemoveAt(i);
                    else
                        _popups[i] = popup;
                }
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
            if (_surfaceTime >= 0f)
            {
                var before = _surfaceTime;
                _surfaceTime += dt;
                if (before < restartDelay + 1f)
                    _dirty = true;
                if (_surfaceTime >= restartDelay && (Pressed(_restart) || Pressed(_restartAlt) || Pressed(_toMenu)))
                    SceneFlow.LoadCamp();
            }
            var mission = dungeon != null ? dungeon.Mission : null;
            if (mission != _shownMission || (mission != null && mission.Version != _shownVersion))
            {
                _shownMission = mission;
                _shownVersion = mission != null ? mission.Version : -1;
                _dirty = true;
            }
            // Barre du boss : redessinée quand ses PV bougent, qu'il arrive ou qu'il meurt.
            var boss = Werewolf.Current;
            var bossHealth = boss != null ? boss.Health : -1;
            if (bossHealth != _bossHealth)
            {
                _bossHealth = bossHealth;
                _dirty = true;
            }
            if (_dirty)
                Redraw();
        }

        /// <summary>Voile l'écran d'encre en `duration` secondes (temps réel), pour changer d'étage.</summary>
        public System.Collections.IEnumerator FadeOut(float duration)
        {
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _fade = t / duration;
                _dirty = true;
                yield return null;
            }
            _fade = 1f;
            _dirty = true;
        }

        /// <summary>Lève le voile d'encre (après <see cref="FadeOut"/>), en montrant le numéro de l'étage.</summary>
        public System.Collections.IEnumerator FadeIn(float duration)
        {
            // Le numéro du sous-sol et le bilan du précédent restent un instant sur le noir.
            for (var t = 0f; t < 1.6f; t += Time.unscaledDeltaTime)
                yield return null;
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _fade = 1f - t / duration;
                _dirty = true;
                yield return null;
            }
            _fade = 0f;
            _dirty = true;
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
        /// <summary>Après la mort : retour au camp (on repart de là en expédition).</summary>
        public void Restart() => SceneFlow.LoadCamp();

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
            if (Werewolf.Current != null)
                DrawBoss(screen, Werewolf.Current);
            if (_popups.Count > 0)
                DrawPopups(screen);
            DrawToast(screen);
            if (_fade > 0f)
                DrawFade(screen);
            if (_deadTime >= 0f)
                DrawDeath(screen);
            if (_surfaceTime >= 0f)
                DrawSurface(screen);
        }

        /// <summary>
        /// Cartouche des PV : « PV » puis une case par point de vie, Tour / ATQ / DÉF dessous. Le
        /// cartouche garde la largeur de <see cref="PipSlots"/> cases : au-delà (bienfait Vie), les
        /// cases s'amincissent au lieu de l'allonger (choix de l'utilisateur).
        /// </summary>
        void DrawHealth()
        {
            var max = player.MaxHealth;
            var pipsW = PipSlots * (PipW + PipGap) - PipGap;
            var scale = Mathf.Min(1f, pipsW / (max * (PipW + PipGap) - PipGap));
            var pipW = PipW * scale;
            var gap = PipGap * scale;
            // Pas le sous-sol ici (demande de l'utilisateur) : il est sur la carte et à la descente.
            var stats = $"{Loc.F("hud.tour", player.Turn)}   ·   {Loc.F("hud.atq", player.Attack)}   ·   {Loc.F("hud.def", player.Defense)}";
            // Assez large pour les cases de PV et pour la ligne du dessous (qui s'allonge avec les tours).
            var width = Mathf.Max(LabelW + pipsW + 22f, TextPool.Width(stats, 12, FontStyle.Italic) + 28f);
            var box = new Rect(Margin + 4f, Margin + 4f, width, PipH + 54f);
            ink.Rect(new Rect(box.x - 4f, box.y - 4f, box.width + 8f, box.height + 8f), paperColor);
            Engraving.DoubleFrame(ink, box, 4f, inkColor);
            var x = box.x + 10f;
            var y = box.y + 9f;
            texts.Label(Loc.T("hud.pv"), x, y - 1f, LabelW, 16, inkColor, TextAnchor.UpperLeft, FontStyle.Bold);
            for (var i = 0; i < max; i++)
            {
                var pip = new Rect(x + LabelW + i * (pipW + gap), y, pipW, PipH);
                if (i < player.Health)
                    ink.Rect(pip, healthColor);
                ink.RectOutline(pip, inkColor, scale < 0.6f ? 1f : 1.5f);
            }
            texts.Label(stats, x, y + PipH + 4f, box.width - 20f, 12, Engraving.Fade(inkColor, paperColor, 0.15f), TextAnchor.UpperLeft, FontStyle.Italic);
            DrawExperience(new Rect(x, y + PipH + 23f, box.width - 20f, 12f));
        }

        /// <summary>Niveau et jauge d'XP vers le suivant (hachurée), sous Tour / ATQ / DÉF.</summary>
        void DrawExperience(Rect row)
        {
            var xp = player.Experience;
            texts.Label(Loc.F("hud.niveau", xp.Level), row.x, row.y - 2f, LabelW + 8f, 12, inkColor, TextAnchor.UpperLeft, FontStyle.Bold);
            var bar = new Rect(row.x + LabelW + 8f, row.y + 2f, row.width - LabelW - 8f, 8f);
            var fill = xp.Pending > 0 ? 1f : Mathf.Clamp01(xp.Xp / (float)Mathf.Max(1, xp.ToNext));
            if (fill > 0f)
            {
                var filled = new Rect(bar.x, bar.y, bar.width * fill, bar.height);
                ink.Hatch(filled, 3f, inkColor, 1f);
            }
            ink.RectOutline(bar, inkColor, 1f);
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

        /// <summary>
        /// Barre de vie du boss, en bas au centre : son nom dans un bandeau gravé, une jauge rouge
        /// découpée en traits (un par PV), comme les PV du joueur.
        /// </summary>
        void DrawBoss(Vector2 screen, Werewolf boss)
        {
            const float w = 420f, h = 14f;
            var bar = new Rect((screen.x - w) * 0.5f, screen.y - Margin - 30f, w, h);
            var banner = new Rect(bar.x + w * 0.25f, bar.y - 30f, w * 0.5f, 20f);
            Engraving.Banner(ink, texts, banner, Loc.T("boss.loup_garou").ToUpperInvariant(), paperColor, inkColor);
            ink.Rect(new Rect(bar.x - 4f, bar.y - 4f, bar.width + 8f, bar.height + 8f), paperColor);
            var filled = boss.MaxHealth > 0 ? (float)boss.Health / boss.MaxHealth : 0f;
            ink.Rect(new Rect(bar.x, bar.y, bar.width * filled, bar.height), healthColor);
            // Un trait par PV.
            for (var i = 1; i < boss.MaxHealth; i++)
            {
                var x = bar.x + bar.width * i / boss.MaxHealth;
                ink.Line(new Vector2(x, bar.y), new Vector2(x, bar.yMax), Engraving.Fade(inkColor, paperColor, 0.4f), 1f);
            }
            Engraving.DoubleFrame(ink, bar, 3f, inkColor);
        }

        /// <summary>Chiffres de dégâts : « −2 » en rouge qui monte au-dessus de l'ennemi et s'efface, rien d'autre.</summary>
        void DrawPopups(Vector2 screen)
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return;
            var scale = UnityEngine.Screen.width > 0 ? screen.x / UnityEngine.Screen.width : 1f;
            foreach (var popup in _popups)
            {
                var sp = _camera.WorldToScreenPoint(popup.World);
                if (sp.z <= 0f)
                    continue; // derrière la caméra
                var at = new Vector2(sp.x * scale, screen.y - sp.y * scale);
                var t = popup.Age / popupTime;
                var rise = 40f * (1f - (1f - t) * (1f - t));
                var alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                const int size = 30;
                texts.Label(popup.Text, at.x - 60f, at.y - rise - 36f, 120f, size, WithAlpha(healthColor, alpha), TextAnchor.UpperCenter, FontStyle.Bold);
            }
        }

        /// <summary>Voile d'encre du changement d'étage, avec « Étage N » au milieu.</summary>
        void DrawFade(Vector2 screen)
        {
            ink.Rect(new Rect(Vector2.zero, screen), WithAlpha(inkColor, _fade));
            if (dungeon != null)
            {
                texts.Label(Loc.F("hud.etage", dungeon.Floor).ToUpperInvariant(), 0f, screen.y * 0.5f - 24f, screen.x, 40,
                    WithAlpha(paperColor, _fade), TextAnchor.UpperCenter, FontStyle.Bold);
                if (!string.IsNullOrEmpty(FloorSummary))
                    texts.Label(FloorSummary, 0f, screen.y * 0.5f + 34f, screen.x, 16,
                        WithAlpha(paperColor, _fade * 0.75f), TextAnchor.UpperCenter, FontStyle.Italic);
            }
        }

        /// <summary>L'écran s'assombrit, « Vous êtes mort », puis l'invitation à recommencer.</summary>
        void DrawDeath(Vector2 screen)
        {
            var a = Mathf.Clamp01(_deadTime / 0.8f);
            ink.Rect(new Rect(Vector2.zero, screen), WithAlpha(inkColor, 0.75f * a));
            var cy = screen.y * 0.5f;
            texts.Label(Loc.T("hud.mort"), 0f, cy - 40f, screen.x, 48, WithAlpha(healthColor, a), TextAnchor.UpperCenter, FontStyle.Bold);
            var reached = dungeon != null ? Loc.F("hud.etage", dungeon.Floor) + "   ·   " : "";
            texts.Label(reached + Loc.F("niveau.atteint", player.Experience.Level) + "   ·   " + Loc.F("hud.tour", player.Turn),0f, cy + 22f, screen.x, 16, WithAlpha(paperColor, a * 0.7f), TextAnchor.UpperCenter, FontStyle.Italic);
            // Le bilan de la partie, tous étages confondus, dans un cartouche papier.
            var line1 = $"{Loc.F("bilan.monstres", RunStats.Kills)}   ·   {Loc.F("bilan.coffres", RunStats.Chests)}";
            var line2 = $"{Loc.F("bilan.boss", RunStats.Bosses)}   ·   {Loc.F("bilan.degats", RunStats.DamageDealt)}";
            var line3 = Loc.F("bilan.or", Gold.LastLost, Gold.LastKept);
            var w = Mathf.Max(TextPool.Width(line1, 16), TextPool.Width(line2, 16), TextPool.Width(line3, 16)) + 48f;
            var card = new Rect((screen.x - w) * 0.5f, cy + 50f, w, 76f);
            ink.Rect(new Rect(card.x - 4f, card.y - 4f, card.width + 8f, card.height + 8f), WithAlpha(paperColor, a));
            Engraving.DoubleFrame(ink, card, 3f, WithAlpha(inkColor, a));
            texts.Label(line1, card.x, card.y + 7f, card.width, 16, WithAlpha(inkColor, a), TextAnchor.UpperCenter);
            texts.Label(line2, card.x, card.y + 29f, card.width, 16, WithAlpha(inkColor, a), TextAnchor.UpperCenter);
            texts.Label(line3, card.x, card.y + 51f, card.width, 16, WithAlpha(inkColor, a), TextAnchor.UpperCenter);
            if (_deadTime >= restartDelay)
            {
                var b = Mathf.Clamp01((_deadTime - restartDelay) / 0.4f);
                // Recommencer : Entrée au clavier (Player/Wait), le bouton du bas à la manette (Player/Interact, L3 est peu parlant).
                var restartKeys = KeyPrompts.Of(ControlDevice.IsGamepad ? "Player/Interact" : "Player/Wait");
                KeyPrompts.DrawHints(ink, texts, new[] { new Hint(restartKeys, "aide.camp"), new Hint(KeyPrompts.Back(), "aide.menu") },
                    0f, cy + 144f, screen.x, 18, WithAlpha(paperColor, b), WithAlpha(paperColor, b), WithAlpha(inkColor, b));
            }
        }

        /// <summary>Remontée : voile d'encre, « De retour à la surface », bilan et or ramené, puis l'invitation à continuer.</summary>
        void DrawSurface(Vector2 screen)
        {
            var a = Mathf.Clamp01(_surfaceTime / 0.8f);
            ink.Rect(new Rect(Vector2.zero, screen), WithAlpha(inkColor, 0.85f * a));
            var cy = screen.y * 0.5f;
            texts.Label(Loc.T("hud.surface"), 0f, cy - 40f, screen.x, 44, WithAlpha(paperColor, a), TextAnchor.UpperCenter, FontStyle.Bold);
            var reached = dungeon != null ? Loc.F("hud.etage", dungeon.Floor) + "   ·   " : "";
            texts.Label(reached + Loc.F("niveau.atteint", player.Experience.Level) + "   ·   " + Loc.F("hud.tour", player.Turn), 0f, cy + 18f, screen.x, 16, WithAlpha(paperColor, a * 0.7f), TextAnchor.UpperCenter, FontStyle.Italic);
            var line1 = $"{Loc.F("bilan.monstres", RunStats.Kills)}   ·   {Loc.F("bilan.coffres", RunStats.Chests)}";
            var line2 = $"{Loc.F("bilan.boss", RunStats.Bosses)}   ·   {Loc.F("bilan.degats", RunStats.DamageDealt)}";
            var line3 = Loc.F("bilan.or_ramene", _surfaceGold, Progression.Gold);
            var w = Mathf.Max(TextPool.Width(line1, 16), TextPool.Width(line2, 16), TextPool.Width(line3, 16, FontStyle.Bold)) + 48f;
            var card = new Rect((screen.x - w) * 0.5f, cy + 50f, w, 76f);
            ink.Rect(new Rect(card.x - 4f, card.y - 4f, card.width + 8f, card.height + 8f), WithAlpha(paperColor, a));
            Engraving.DoubleFrame(ink, card, 3f, WithAlpha(inkColor, a));
            texts.Label(line1, card.x, card.y + 7f, card.width, 16, WithAlpha(inkColor, a), TextAnchor.UpperCenter);
            texts.Label(line2, card.x, card.y + 29f, card.width, 16, WithAlpha(inkColor, a), TextAnchor.UpperCenter);
            texts.Label(line3, card.x, card.y + 51f, card.width, 16, WithAlpha(inkColor, a), TextAnchor.UpperCenter, FontStyle.Bold);
            if (_surfaceTime >= restartDelay)
            {
                var b = Mathf.Clamp01((_surfaceTime - restartDelay) / 0.4f);
                KeyPrompts.DrawHints(ink, texts, new[] { new Hint(KeyPrompts.Confirm(), "aide.continuer") },
                    0f, cy + 144f, screen.x, 18, WithAlpha(paperColor, b), WithAlpha(paperColor, b), WithAlpha(inkColor, b));
            }
        }

        static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, c.a * a);
    }
}
