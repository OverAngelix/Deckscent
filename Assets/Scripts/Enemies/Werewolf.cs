using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deckscent
{
    /// <summary>
    /// Le boss de l'étage : un loup-garou, dans son antre posée par le jeu une fois la mission
    /// remplie (<see cref="DungeonBoard.FindBossLair"/>). Le vaincre ouvre l'escalier (le
    /// <see cref="Spawner"/> le fait apparaître à sa place).
    ///
    /// Tout est **annoncé un tour à l'avance** (le joueur a toujours une chance de réagir) :
    /// - **Endormi** jusqu'à ce que le joueur entre dans l'antre ou arrive à <see cref="wakeRange"/>
    ///   cases en vue : il **hurle** (un tour) et la musique passe en mode boss.
    /// - Chaque tour éveillé, il **tire son attaque** (poids réglés par l'utilisateur) : coup de base
    ///   1 sur 6, coup chargé 3 sur 6, charge 2 sur 6.
    /// - **Coup de base** : à côté du joueur, il frappe tout de suite (2 dégâts, <see cref="Enemy.Damage"/>).
    /// - **Coup chargé** : il **arme** (bras levé, grognement) et frappe au tour suivant **vers la case
    ///   visée**, même dans le vide (3 dégâts) : reculer ou se décaler fait rater le coup.
    /// - **Charge** : joueur aligné (même ligne ou colonne, ligne dégagée, 2 à <see cref="chargeRange"/>
    ///   cases), il **se ramasse** (yeux rouges, grognement), puis fonce tout droit au tour suivant
    ///   jusqu'au premier obstacle. S'il croise le joueur, gros dégâts ; s'il percute un mur, il est
    ///   **sonné** un tour. Se décaler de la ligne suffit à l'esquiver.
    /// - **2ᵉ phase** à la moitié des PV : il hurle et appelle des **louveteaux** (une fois).
    /// - Sinon, il s'approche d'une case par tour.
    /// </summary>
    public class Werewolf : Enemy
    {
        enum State { Dormant, Awake, Armed, Crouched, Stunned }

        [Header("Comportement")]
        [Tooltip("Il se réveille quand le joueur entre dans l'antre, ou le voit à moins de tant de cases (ligne droite, sans mur).")]
        [SerializeField, Range(1, 12)] int wakeRange = 5;
        [Tooltip("Dégâts de la charge s'il percute le joueur (avant sa défense).")]
        [SerializeField, Range(0, 20)] int chargeDamage = 4;
        [Tooltip("Distance maximale de la charge (cases). Il ne charge qu'à partir de 2 cases.")]
        [SerializeField, Range(2, 12)] int chargeRange = 6;
        [Tooltip("Tours d'attente entre deux charges.")]
        [SerializeField, Range(0, 10)] int chargeCooldown;
        [Tooltip("Dégâts du coup chargé (annoncé un tour à l'avance, porté au tour suivant même dans le vide). Le coup de base (sans annonce) fait les dégâts « Damage ».")]
        [SerializeField, Range(0, 20)] int heavyDamage = 3;

        [Header("Choix de l'attaque (poids, réglés par l'utilisateur)")]
        [Tooltip("Poids du coup de base (2 dégâts, tout de suite, à côté du joueur). 1 sur 6.")]
        [SerializeField, Min(0f)] float basicWeight = 1f;
        [Tooltip("Poids du coup chargé (3 dégâts, annoncé, porté au tour suivant même dans le vide). 3 sur 6.")]
        [SerializeField, Min(0f)] float heavyWeight = 3f;
        [Tooltip("Poids de la charge (4 dégâts, annoncée, en ligne droite). 2 sur 6. Joueur pas aligné : il s'approche à la place.")]
        [SerializeField, Min(0f)] float chargeWeight = 2f;
        [Tooltip("Louveteaux appelés à la moitié de ses PV.")]
        [SerializeField, Range(0, 4)] int cubs = 2;
        [Tooltip("Distance de poursuite (en pas, murs contournés).")]
        [SerializeField, Range(1, 60)] int chaseRange = 40;

        [Header("Animation")]
        [Tooltip("Les poses du modèle (WerewolfRig, sur le modèle).")]
        [SerializeField] WerewolfRig rig;
        [Tooltip("Durée d'un pas (secondes).")]
        [SerializeField, Min(0.01f)] float moveTime = 0.3f;
        [Tooltip("Durée d'une case de charge (secondes).")]
        [SerializeField, Min(0.01f)] float chargeStepTime = 0.08f;
        [Tooltip("Rendus teintés quand il se ramasse pour charger (yeux rouges) et blanchis quand il est touché.")]
        [SerializeField] Renderer[] tinted;
        [Tooltip("Couleur des yeux quand il annonce sa charge (saturée : sort du papier).")]
        [SerializeField] Color chargeColor = new Color(0.95f, 0.1f, 0.05f);
        [Tooltip("Lueur propre (Toon._Glow) quand il se ramasse : il brille dans le noir.")]
        [SerializeField, Range(0f, 1f)] float chargeGlow = 0.6f;

        [Header("Sons")]
        [Tooltip("Hurlement, grognements (piste Effets, en 3D). Vide : cherché sur le boss.")]
        [SerializeField] SoundEmitter sounds;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Glow = Shader.PropertyToID("_Glow");

        /// <summary>Un loup-garou hurle (position) : les flammes proches s'embrasent (<see cref="TorchFlicker"/>).</summary>
        public static event Action<Vector3> Howled;

        /// <summary>Le boss vient de se réveiller (musique du boss).</summary>
        public event Action<Werewolf> Woke;
        /// <summary>2ᵉ phase : il appelle des louveteaux (le Spawner les fait apparaître autour de lui).</summary>
        public event Action<Werewolf, int> CallsCubs;

        /// <summary>Le boss éveillé en cours de combat (barre de vie du HUD), null sinon.</summary>
        public static Werewolf Current { get; private set; }

        /// <summary>Cases de son antre (pour le réveil). Données par le Spawner.</summary>
        public HashSet<Vector2Int> Lair { get; set; }
        public bool IsAwake => _state != State.Dormant;
        public override bool Threatens => IsActive && IsAwake;
        /// <summary>Direction de la charge annoncée (indice de <see cref="Grid.Dirs"/>), -1 sinon.</summary>
        public int AnnouncedCharge => _state == State.Crouched ? _chargeDir : -1;
        public bool IsArmed => _state == State.Armed;
        public bool IsStunned => _state == State.Stunned;
        public bool InSecondPhase => _phase2Done;

        State _state = State.Dormant;
        int _chargeDir = -1, _cooldown, _armDir = -1;
        enum Intent { None, Basic, Heavy, Charge }
        /// <summary>L'attaque tirée pour ce tour (dès la phase de déplacement : s'il veut charger, il ne bouge pas).</summary>
        Intent _intent;

        /// <summary>Change les poids du choix de l'attaque (tests, essais).</summary>
        public void SetAttackWeights(float basic, float heavy, float charge)
        {
            basicWeight = basic;
            heavyWeight = heavy;
            chargeWeight = charge;
        }

        Intent RollIntent()
        {
            var charge = _cooldown == 0 ? chargeWeight : 0f;
            var total = basicWeight + heavyWeight + charge;
            if (total <= 0f)
                return Intent.Basic;
            var roll = UnityEngine.Random.value * total;
            if (roll < basicWeight)
                return Intent.Basic;
            return roll < basicWeight + heavyWeight ? Intent.Heavy : Intent.Charge;
        }
        bool _phase2Pending, _phase2Done, _dying, _woke;
        MaterialPropertyBlock _block;
        PlayerController _player;
        MusicDirector _music;

        protected override void Awake()
        {
            base.Awake();
            if (rig == null)
                rig = GetComponentInChildren<WerewolfRig>();
            if (sounds == null)
                sounds = GetComponentInChildren<SoundEmitter>();
        }

        void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        void Start()
        {
            _player = FindAnyObjectByType<PlayerController>();
            _music = FindAnyObjectByType<MusicDirector>();
            rig?.SetPose(WerewolfRig.Pose.Sommeil, 0.01f);
        }

        /// <summary>Renforce le boss pour un étage plus profond (PV et dégâts).</summary>
        public override void ScaleForFloor(int floor)
        {
            var extra = Mathf.Max(0, floor - 1);
            maxHealth += extra * 2;
            damage += extra / 2;
            chargeDamage += extra / 2;
            heavyDamage += extra / 2;
            Health = maxHealth;
        }

        void Update()
        {
            // Il fait face au joueur, sauf endormi, en pleine charge ou mort.
            if (_player == null || _dying || _state == State.Dormant || _state == State.Crouched)
                return;
            var to = _player.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Mathf.Min(1f, 5f * Time.deltaTime));
        }

        // --- Tour par tour ---------------------------------------------------------------

        bool Adjacent(PlayerController player)
        {
            var dir = Array.IndexOf(Grid.Dirs, player.Cell - Cell);
            return dir >= 0 && Maze.CanStep(Cell, dir);
        }

        /// <summary>Le joueur est-il dans son antre, ou en vue à portée de réveil ?</summary>
        bool Notices(PlayerController player)
        {
            if (Lair != null && Lair.Contains(player.Cell))
                return true;
            return LineTo(player.Cell, wakeRange) >= 0;
        }

        /// <summary>
        /// Direction (indice de <see cref="Grid.Dirs"/>) d'une ligne droite dégagée jusqu'à `target`
        /// en au plus `range` cases (murs fins compris ; un autre objet bloquant coupe la ligne). -1 sinon.
        /// </summary>
        int LineTo(Vector2Int target, int range)
        {
            var delta = target - Cell;
            if ((delta.x != 0) == (delta.y != 0))
                return -1;
            var distance = Mathf.Abs(delta.x + delta.y);
            if (distance > range)
                return -1;
            var dir = Array.IndexOf(Grid.Dirs, new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y)));
            var c = Cell;
            for (var i = 0; i < distance; i++)
            {
                if (!Maze.CanStep(c, dir))
                    return -1;
                c += Grid.Dirs[dir];
                if (c != target && Blocked(c))
                    return -1;
            }
            return dir;
        }

        static bool Blocked(Vector2Int cell)
        {
            foreach (var e in GridEntity.All)
                if (e.IsActive && e.BlocksPlayer && e.Cell == cell)
                    return true;
            return false;
        }

        /// <summary>Il ne se déplace qu'éveillé, libre (ni armé, ni ramassé, ni sonné), et pas à côté du joueur.</summary>
        public override Vector2Int? PlanMove(PlayerController player, ISet<Vector2Int> occupied)
        {
            if (_dying || _state != State.Awake || _phase2Pending || Maze == null || Adjacent(player))
                return null;
            _intent = RollIntent();
            if (_intent == Intent.Charge && ChargeLine(player) >= 0)
                return null; // il se ramasse pour charger au lieu d'avancer
            var path = GridPath.Find(Maze, Cell, player.Cell, chaseRange, occupied);
            if (path.Count < 2)
                path = GridPath.Find(Maze, Cell, player.Cell, chaseRange);
            return path.Count >= 2 && !occupied.Contains(path[0]) ? path[0] : (Vector2Int?)null;
        }

        int ChargeLine(PlayerController player)
        {
            var delta = player.Cell - Cell;
            return Mathf.Abs(delta.x) + Mathf.Abs(delta.y) >= 2 ? LineTo(player.Cell, chargeRange) : -1;
        }

        public override IEnumerator Move(Vector2Int cell)
        {
            if (_cooldown > 0)
                _cooldown--;
            Cell = cell;
            var start = transform.position;
            var end = Maze.CellToWorld(cell);
            yield return Animate(moveTime, t =>
                transform.position = Vector3.Lerp(start, end, Smooth(t)) + Vector3.up * Mathf.Abs(Mathf.Sin(t * Mathf.PI)) * 0.15f);
        }

        public override IEnumerator TakeTurn(PlayerController player)
        {
            if (_dying || Maze == null)
                yield break;
            switch (_state)
            {
                case State.Dormant:
                    if (Notices(player))
                        yield return Wake();
                    yield break;
                case State.Stunned:
                    _state = State.Awake;
                    StopStars();
                    rig?.SetPose(WerewolfRig.Pose.Garde, 0.4f);
                    yield break;
                case State.Crouched:
                    yield return Charge(player);
                    yield break;
                case State.Armed:
                    // Le coup chargé part toujours, vers la case visée : si le joueur a bougé, il frappe le vide.
                    yield return HeavySwipe(player);
                    yield break;
            }
            if (_cooldown > 0)
                _cooldown--;
            var intent = _intent != Intent.None ? _intent : RollIntent();
            _intent = Intent.None;
            if (_phase2Pending)
            {
                yield return CallCubs();
                yield break;
            }
            if (Adjacent(player))
            {
                // À côté : coup de base tout de suite, coup chargé annoncé, ou charge à bout portant.
                var toward = Array.IndexOf(Grid.Dirs, player.Cell - Cell);
                if (intent == Intent.Charge)
                    yield return Crouch(toward);
                else if (intent == Intent.Heavy)
                    yield return Arm(toward);
                else
                    yield return Swipe(player, player.Cell - Cell, damage, true);
                yield break;
            }
            if (intent == Intent.Charge)
            {
                var dir = ChargeLine(player);
                if (dir >= 0)
                    yield return Crouch(dir);
            }
        }

        IEnumerator Wake()
        {
            _state = State.Awake;
            if (!_woke)
            {
                _woke = true;
                Current = this;
                if (_music != null)
                    _music.BossMode = true;
                Woke?.Invoke(this);
                _player?.Notify(Loc.T("message.boss_eveil"));
            }
            yield return Howl();
        }

        IEnumerator Howl()
        {
            rig?.SetPose(WerewolfRig.Pose.Hurle, 0.3f);
            sounds?.Play(SoundBank.Howl, 1f);
            Howled?.Invoke(transform.position);
            yield return Animate(1.1f, _ => { });
            rig?.SetPose(WerewolfRig.Pose.Garde, 0.4f);
        }

        IEnumerator CallCubs()
        {
            _phase2Pending = false;
            _phase2Done = true;
            _player?.Notify(Loc.T("message.boss_louveteaux"));
            yield return Howl();
            CallsCubs?.Invoke(this, cubs);
        }

        IEnumerator Arm(int dir)
        {
            _state = State.Armed;
            _armDir = dir;
            rig?.SetPose(WerewolfRig.Pose.Armer, 0.25f);
            sounds?.Play(SoundBank.Growl, 0.8f);
            yield return Animate(0.3f, _ => { });
        }

        /// <summary>Le coup chargé, vers la case visée au tour d'avant : touche seulement si le joueur y est resté.</summary>
        IEnumerator HeavySwipe(PlayerController player)
        {
            var dir = Grid.Dirs[_armDir < 0 ? 0 : _armDir];
            transform.rotation = Quaternion.LookRotation(WorldDir(dir));
            var hit = player.Cell == Cell + dir;
            yield return Swipe(player, dir, heavyDamage, hit);
        }

        /// <summary>Un coup de griffe vers `dir` : `hit` = le joueur est là et prend `amount` dégâts.</summary>
        IEnumerator Swipe(PlayerController player, Vector2Int dir, int amount, bool hit)
        {
            _state = State.Awake;
            var start = transform.position;
            var end = start + WorldDir(dir) * 0.6f;
            rig?.SetPose(WerewolfRig.Pose.Griffer, 0.1f);
            sounds?.Play(SoundBank.Attack, 1f);
            yield return Animate(0.12f, t => transform.position = Vector3.Lerp(start, end, t));
            if (IsActive && hit)
                player.TakeDamage(amount);
            yield return Animate(0.25f, t => transform.position = Vector3.Lerp(end, start, Smooth(t)));
            rig?.SetPose(WerewolfRig.Pose.Garde, 0.3f);
        }

        IEnumerator Crouch(int dir)
        {
            _state = State.Crouched;
            _chargeDir = dir;
            transform.rotation = Quaternion.Euler(0f, Grid.Yaw(dir), 0f);
            rig?.SetPose(WerewolfRig.Pose.Ramasse, 0.3f);
            SetTint(chargeColor, chargeGlow);
            sounds?.Play(SoundBank.Growl, 1f);
            _player?.Notify(Loc.T("message.boss_charge"));
            yield return Animate(0.35f, _ => { });
        }

        /// <summary>Fonce tout droit dans la direction annoncée jusqu'au premier obstacle.</summary>
        IEnumerator Charge(PlayerController player)
        {
            var dir = _chargeDir;
            _chargeDir = -1;
            _cooldown = chargeCooldown;
            SetTint(Color.white, 0f, true);
            rig?.SetPose(WerewolfRig.Pose.Charge, 0.08f);
            var step = Grid.Dirs[dir];
            var hit = false;
            for (var i = 0; i < chargeRange + 4; i++)
            {
                if (!Maze.CanStep(Cell, dir))
                    break;
                var next = Cell + step;
                if (next == player.Cell)
                {
                    hit = true;
                    break;
                }
                if (Blocked(next))
                    break;
                var from = transform.position;
                var to = Maze.CellToWorld(next);
                Cell = next;
                yield return Animate(chargeStepTime, t => transform.position = Vector3.Lerp(from, to, t));
            }
            if (hit)
            {
                var start = transform.position;
                var end = start + WorldDir(step) * 0.7f;
                yield return Animate(0.08f, t => transform.position = Vector3.Lerp(start, end, t));
                if (IsActive)
                    player.TakeDamage(chargeDamage);
                yield return Animate(0.2f, t => transform.position = Vector3.Lerp(end, start, Smooth(t)));
                _state = State.Awake;
                rig?.SetPose(WerewolfRig.Pose.Garde, 0.3f);
            }
            else
            {
                // Dans le mur (ou un obstacle) : sonné un tour.
                var start = transform.position;
                var end = start + WorldDir(step) * 0.5f;
                yield return Animate(0.06f, t => transform.position = Vector3.Lerp(start, end, t));
                sounds?.Play(SoundBank.Hit, 0.8f);
                yield return Animate(0.2f, t => transform.position = Vector3.Lerp(end, start, Smooth(t)));
                _state = State.Stunned;
                rig?.SetPose(WerewolfRig.Pose.Sonne, 0.2f);
                Fx.Play(Fx.Get(l => l.bigDust), transform.position + WorldDir(step) * 1.2f);
                StopStars();
                _stars = Fx.Play(Fx.Get(l => l.stunned), transform.position + Vector3.up * 2.9f, Quaternion.identity, null, transform);
                _player?.Notify(Loc.T("message.boss_sonne"));
            }
        }

        // --- Coups et mort ---------------------------------------------------------------

        protected override void OnHurt()
        {
            // Frappé dans son sommeil : il se réveille au tour suivant (son TakeTurn le voit dans l'antre).
            if (_state != State.Dormant && Health <= MaxHealth / 2 && !_phase2Done && cubs > 0)
                _phase2Pending = true;
            if (isActiveAndEnabled)
                StartCoroutine(Flinch());
        }

        IEnumerator Flinch()
        {
            var keep = rig != null ? rig.Current : WerewolfRig.Pose.Garde;
            SetTint(Color.white, 0.8f);
            rig?.SetPose(WerewolfRig.Pose.Touche, 0.06f);
            yield return new WaitForSeconds(0.15f);
            if (_dying)
                yield break;
            SetTint(_state == State.Crouched ? chargeColor : Color.white, _state == State.Crouched ? chargeGlow : 0f, _state != State.Crouched);
            rig?.SetPose(keep == WerewolfRig.Pose.Touche ? WerewolfRig.Pose.Garde : keep, 0.2f);
        }

        GameObject _stars;

        void StopStars()
        {
            if (_stars != null)
                Destroy(_stars);
            _stars = null;
        }

        protected override void OnDeath()
        {
            _dying = true;
            StopStars();
            if (Current == this)
                Current = null;
            if (_music != null)
                _music.BossMode = false;
            SetTint(Color.white, 0f, true);
            rig?.SetPose(WerewolfRig.Pose.Mort, 0.3f);
            sounds?.Play(SoundBank.Howl, 0.6f);
            StartCoroutine(Fall());
        }

        /// <summary>Il bascule en arrière, puis s'enfonce dans le sol.</summary>
        IEnumerator Fall()
        {
            var from = transform.rotation;
            var to = from * Quaternion.Euler(-80f, 0f, 0f);
            yield return Animate(0.6f, t => transform.rotation = Quaternion.Slerp(from, to, t * t));
            yield return new WaitForSeconds(0.8f);
            var start = transform.position;
            Fx.Play(Fx.Get(l => l.bigDust), start);
            yield return Animate(1f, t => transform.position = start + Vector3.down * 1.2f * t);
            Destroy(gameObject);
        }

        /// <summary>Teinte (yeux rouges, flash blanc) et lueur des rendus. `reset` : retour aux couleurs du matériau.</summary>
        void SetTint(Color color, float glow, bool reset = false)
        {
            if (tinted == null)
                return;
            _block ??= new MaterialPropertyBlock();
            foreach (var r in tinted)
            {
                if (r == null)
                    continue;
                if (reset)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, color);
                _block.SetFloat(Glow, glow);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
