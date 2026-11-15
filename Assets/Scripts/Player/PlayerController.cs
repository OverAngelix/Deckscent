using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deckscent
{
    /// <summary>
    /// Déplacement case par case, façon dungeon crawler classique, au tour par tour.
    /// Avancer / reculer / pas de côté d'une case, rotation par quart de tour, « bump » contre
    /// les murs. Pas de physique : les collisions sont testées sur la grille du labyrinthe.
    ///
    /// Chaque pas, attaque (avancer vers un ennemi droit devant) ou attente est un tour : une fois
    /// l'animation du joueur finie, chaque objet de grille (<see cref="GridEntity.All"/> : ennemis,
    /// pièges) joue à son tour, l'un après l'autre, puis <see cref="TurnEnded"/>. Tourner et se
    /// cogner sont gratuits.
    ///
    /// Touches : actions Move, Turn, Wait, Torch, Interact de la map Player d'InputSystem_Actions
    /// (clavier en position physique, ZQSD en AZERTY, et manette). Elles se règlent dans ce fichier.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Tooltip("Le labyrinthe où l'on se déplace.")]
        [SerializeField] Maze maze;

        [Header("Rythme")]
        [Tooltip("Durée d'un pas (secondes).")]
        [SerializeField, Min(0.01f)] float moveTime = 0.22f;
        [Tooltip("Durée d'un quart de tour (secondes).")]
        [SerializeField, Min(0.01f)] float turnTime = 0.18f;
        [Tooltip("Durée minimale d'un tour passé à attendre, pour ne pas enchaîner des dizaines de tours en gardant la touche enfoncée.")]
        [SerializeField, Min(0f)] float waitTime = 0.25f;
        [Tooltip("Coché : tourner sur place coûte un tour (les ennemis jouent après chaque quart de tour).")]
        [SerializeField] bool turningTakesTurn;

        [Header("Combat")]
        [Tooltip("Points de vie au départ (et maximum).")]
        [SerializeField, Range(1, 99)] int maxHealth = 10;
        [Tooltip("Dégâts d'une attaque (avancer vers un ennemi droit devant), avant sa défense.")]
        [SerializeField, Range(0, 20)] int attack = 1;
        [Tooltip("Retirée des dégâts reçus (coups, pièges). Un coup fait toujours au moins 1.")]
        [SerializeField, Range(0, 20)] int defense;

        // Actions de la map Player d'InputSystem_Actions (Assets/InputSystem_Actions.inputactions),
        // où se règlent les touches. Clavier en position physique (ZQSD en AZERTY) et manette.
        // Vides : prises par leur nom dans les actions du projet.
        [Header("Actions (InputSystem_Actions, map Player)")]
        [Tooltip("Se déplacer : ZQSD, stick gauche, croix. Avant / arrière, pas de côté. Avancer vers un ennemi l'attaque, vers un coffre l'ouvre.")]
        [SerializeField] InputActionReference move;
        [Tooltip("Tourner d'un quart de tour : A / E, LB / RB.")]
        [SerializeField] InputActionReference turn;
        [Tooltip("Attendre un tour : Entrée, L3.")]
        [SerializeField] InputActionReference wait;
        [Tooltip("Allumer / éteindre la lanterne : T, bouton droit (Rond / B). Ne coûte pas de tour.")]
        [SerializeField] InputActionReference torch;
        [Tooltip("Ouvrir le coffre droit devant : F, bouton du bas (Croix / A). Un tour.")]
        [SerializeField] InputActionReference interact;
        [Tooltip("Seuil au-delà duquel un stick compte comme une direction.")]
        [SerializeField, Range(0.1f, 0.9f)] float stickThreshold = 0.5f;

        [Header("Lanterne")]
        [Tooltip("La lumière que l'action Torch allume et éteint.")]
        [SerializeField] Light lantern;

        [Header("Caméra")]
        [Tooltip("La caméra du joueur : elle tremble quand on est touché et tombe au sol à la mort. Vide : la caméra enfant.")]
        [SerializeField] Transform cameraRig;

        [Header("Sons")]
        [Tooltip("Ce qui joue les pas et l'attaque (piste Effets du mixer). Vide : cherché sur le joueur.")]
        [SerializeField] SoundEmitter sounds;
        [Tooltip("Volume d'un bruit de pas (0 à 1).")]
        [SerializeField, Range(0f, 1f)] float stepVolume = 0.35f;
        [Tooltip("Volume de l'attaque (0 à 1).")]
        [SerializeField, Range(0f, 1f)] float attackVolume = 0.8f;
        [Tooltip("Volume du coup reçu, monstre ou piège (0 à 1).")]
        [SerializeField, Range(0f, 1f)] float hitVolume = 0.8f;

        /// <summary>Un tour complet vient de se terminer (le joueur, puis le monde).</summary>
        public event Action<int> TurnEnded;
        /// <summary>PV modifiés (PV, maximum).</summary>
        public event Action<int, int> HealthChanged;
        /// <summary>Le joueur vient d'être touché (dégâts subis, après défense).</summary>
        public event Action<int> Hurt;
        public event Action Died;
        /// <summary>Un court message à afficher au HUD (piège franchi, butin…).</summary>
        public event Action<string> Notified;

        public Vector2Int Cell { get; private set; }
        /// <summary>0 = nord, 1 = est, 2 = sud, 3 = ouest.</summary>
        public int Facing { get; private set; }
        /// <summary>Nombre de tours joués depuis le début.</summary>
        public int Turn { get; private set; }
        public Maze Maze => maze;
        /// <summary>Vrai pendant une animation ou le tour du monde : les touches sont ignorées.</summary>
        public bool IsBusy => _busy;
        public bool LanternOn => lantern != null && lantern.enabled;
        public int Health { get; private set; }
        public int MaxHealth => maxHealth;
        public int Attack => attack;
        public int Defense => defense;
        public bool IsDead { get; private set; }

        bool _busy;
        InputAction _move, _turn, _wait, _torch, _interact;
        Vector3 _cameraRest;
        Coroutine _shake;

        void Awake()
        {
            Health = maxHealth;
            if (sounds == null)
                sounds = GetComponentInChildren<SoundEmitter>();
            if (cameraRig == null)
            {
                var cam = GetComponentInChildren<Camera>(true);
                if (cam != null)
                    cameraRig = cam.transform;
            }
            if (cameraRig != null)
                _cameraRest = cameraRig.localPosition;
        }

        void OnEnable()
        {
            _move = Resolve(move, "Player/Move");
            _turn = Resolve(turn, "Player/Turn");
            _wait = Resolve(wait, "Player/Wait");
            _torch = Resolve(torch, "Player/Torch");
            _interact = Resolve(interact, "Player/Interact");
            foreach (var action in Actions)
                action?.Enable();
            if (_torch != null)
                _torch.performed += OnTorch;
        }

        void OnDisable()
        {
            if (_torch != null)
                _torch.performed -= OnTorch;
            foreach (var action in Actions)
                action?.Disable();
        }

        IEnumerable<InputAction> Actions => new[] { _move, _turn, _wait, _torch, _interact };

        /// <summary>L'action référencée, sinon celle du même nom dans les actions du projet.</summary>
        InputAction Resolve(InputActionReference reference, string path)
        {
            var action = reference != null ? reference.action : InputSystem.actions?.FindAction(path);
            if (action == null)
                Debug.LogError($"Joueur : action {path} introuvable (InputSystem_Actions).", this);
            return action;
        }

        void OnTorch(InputAction.CallbackContext _)
        {
            // Carte ouverte : son bouton « fermer » (Rond / B) est aussi celui de la lanterne.
            if (Time.timeScale != 0f && !IsDead)
                ToggleLantern();
        }

        /// <summary>Allume ou éteint la lanterne. Gratuit : ne coûte pas de tour.</summary>
        public void ToggleLantern()
        {
            if (lantern != null)
                lantern.enabled = !lantern.enabled;
        }

        void Start()
        {
            if (maze == null)
                maze = FindAnyObjectByType<Maze>();
            PlaceAtStart();
            HealthChanged?.Invoke(Health, maxHealth);
        }

        /// <summary>Pose le joueur sur le départ du labyrinthe, tourné vers le premier couloir ouvert.</summary>
        public void PlaceAtStart()
        {
            Cell = maze.StartCell;
            transform.position = maze.CellToWorld(Cell);
            Facing = 0;
            for (var i = 0; i < Grid.Dirs.Length; i++)
            {
                if (maze.CanStep(Cell, i))
                {
                    Facing = i;
                    break;
                }
            }
            transform.rotation = Quaternion.Euler(0f, Grid.Yaw(Facing), 0f);
        }

        void Update()
        {
            // Jeu en pause (carte ouverte) ou joueur mort : le joueur ne bouge pas.
            if (_busy || IsDead || Time.timeScale == 0f || maze == null || _move == null || _turn == null || _wait == null)
                return;
            var forward = Grid.Dirs[Facing];
            var right = Grid.Dirs[(Facing + 1) % 4];
            var turnAxis = _turn.ReadValue<float>();
            var m = _move.ReadValue<Vector2>();

            if (turnAxis <= -stickThreshold)
                StartCoroutine(TurnBy(-1));
            else if (turnAxis >= stickThreshold)
                StartCoroutine(TurnBy(1));
            else if (m.sqrMagnitude >= stickThreshold * stickThreshold)
            {
                // Stick en diagonale : on garde l'axe dominant (pas de pas en biais).
                if (Mathf.Abs(m.y) >= Mathf.Abs(m.x))
                    StartCoroutine(Step(m.y > 0f ? forward : -forward));
                else
                    StartCoroutine(Step(m.x > 0f ? right : -right));
            }
            else if (_interact != null && _interact.WasPressedThisFrame())
                StartCoroutine(InteractAhead());
            else if (_wait.IsPressed())
                StartCoroutine(Wait());
        }

        IEnumerator TurnBy(int dir)
        {
            _busy = true;
            Facing = (Facing + dir + 4) % 4;
            var from = transform.rotation;
            var to = Quaternion.Euler(0f, Grid.Yaw(Facing), 0f);
            yield return Animate(turnTime, t => transform.rotation = Quaternion.Slerp(from, to, t));
            transform.rotation = to;
            if (turningTakesTurn)
                yield return EndTurn();
            _busy = false;
        }

        IEnumerator Step(Vector2Int dir)
        {
            _busy = true;
            var target = Cell + dir;
            var dirIndex = Array.IndexOf(Grid.Dirs, dir);
            var ahead = dir == Grid.Dirs[Facing];
            var open = maze.CanStep(Cell, dirIndex);
            var blocker = open ? Blocker(target) : null;
            if (blocker is IDamageable enemy && ahead)
            {
                // Avancer vers un ennemi droit devant : l'attaquer.
                yield return Strike(enemy, dir);
                yield return EndTurn();
            }
            else if (blocker is IInteractable thing && ahead && thing.CanInteract)
            {
                // Avancer vers un coffre : l'ouvrir.
                yield return Lunge(dir, 0.35f, moveTime * 0.4f);
                yield return thing.Interact(this);
                yield return EndTurn();
            }
            else if (open && blocker == null)
            {
                // Pics sortis sur la case visée : ils rentrent dès le début du pas.
                var trap = GridEntity.At<SpikeTrap>(target);
                if (trap != null)
                    trap.Anticipate(Turn + 1);
                Cell = target;
                if (sounds != null)
                    sounds.Play(SoundBank.RandomStep(), stepVolume);
                var from = transform.position;
                var to = maze.CellToWorld(Cell);
                yield return Animate(moveTime, t => transform.position = Vector3.Lerp(from, to, t));
                transform.position = to;
                yield return EndTurn();
            }
            else
            {
                // Petit « bump » contre le mur (ou un ennemi de côté) : ne coûte pas de tour.
                yield return Lunge(dir, 0.25f, moveTime * 0.4f);
            }
            _busy = false;
        }

        /// <summary>Action Interact : ouvre ce qui est droit devant (un tour), sinon rien.</summary>
        IEnumerator InteractAhead()
        {
            var dir = Grid.Dirs[Facing];
            if (!maze.CanStep(Cell, Facing) || !(Blocker(Cell + dir) is IInteractable thing) || !thing.CanInteract)
                yield break;
            _busy = true;
            yield return Lunge(dir, 0.35f, moveTime * 0.4f);
            yield return thing.Interact(this);
            yield return EndTurn();
            _busy = false;
        }

        IEnumerator Wait()
        {
            _busy = true;
            yield return new WaitForSeconds(waitTime);
            yield return EndTurn();
            _busy = false;
        }

        /// <summary>Ce qui bloque cette case pour le joueur (ennemi, coffre fermé), ou null.</summary>
        static GridEntity Blocker(Vector2Int cell)
        {
            // Les ennemis d'abord : un monstre posé sur un coffre se fait attaquer.
            GridEntity found = null;
            foreach (var entity in GridEntity.All)
            {
                if (!entity.IsActive || entity.Cell != cell || !entity.BlocksPlayer)
                    continue;
                if (entity is IDamageable)
                    return entity;
                found ??= entity;
            }
            return found;
        }

        /// <summary>Coup porté : élan vers l'ennemi, dégâts au bout de l'élan, retour.</summary>
        IEnumerator Strike(IDamageable enemy, Vector2Int dir)
        {
            var start = transform.position;
            var end = start + Grid.ToWorldDir(dir) * 0.6f;
            if (sounds != null)
                sounds.Play(SoundBank.Attack, attackVolume);
            yield return Animate(moveTime * 0.45f, t => transform.position = Vector3.Lerp(start, end, t));
            if (enemy is UnityEngine.Object o && o != null)
                enemy.TakeDamage(attack);
            yield return Animate(moveTime * 0.55f, t => transform.position = Vector3.Lerp(end, start, t));
            transform.position = start;
        }

        /// <summary>Aller-retour de `distance` mètres dans la direction `dir`.</summary>
        IEnumerator Lunge(Vector2Int dir, float distance, float halfTime)
        {
            var start = transform.position;
            var end = start + Grid.ToWorldDir(dir) * distance;
            yield return Animate(halfTime, t => transform.position = Vector3.Lerp(start, end, t));
            yield return Animate(halfTime, t => transform.position = Vector3.Lerp(end, start, t));
            transform.position = start;
        }

        /// <summary>
        /// Fin du tour du joueur, en deux temps :
        /// 1. **déplacements** : chaque objet de grille choisit sa case (<see cref="GridEntity.PlanMove"/>),
        ///    les plus proches du joueur d'abord, chacun réservant la sienne (personne ne se marche
        ///    dessus), puis tous les bonds sont joués en même temps ;
        /// 2. **tours** : ceux qui ne se sont pas déplacés jouent l'un après l'autre (attaques, pièges),
        ///    en attendant la fin de leur animation. S'arrête net si le joueur meurt.
        /// Un objet qui n'a rien à animer ne coûte aucune image.
        /// </summary>
        IEnumerator EndTurn()
        {
            Turn++;
            var entities = new List<GridEntity>(GridEntity.All);

            // 1. Déplacements simultanés.
            var occupied = new HashSet<Vector2Int> { Cell };
            foreach (var entity in entities)
                if (entity.IsActive && entity.BlocksPlayer)
                    occupied.Add(entity.Cell);
            var order = new List<GridEntity>(entities);
            order.Sort((a, b) => Distance(a.Cell).CompareTo(Distance(b.Cell)));
            var moved = new List<GridEntity>();
            var running = 0;
            foreach (var entity in order)
            {
                if (entity == null || !entity.IsActive)
                    continue;
                var own = entity.BlocksPlayer && occupied.Remove(entity.Cell);
                var target = entity.PlanMove(this, occupied);
                if (target.HasValue && !occupied.Contains(target.Value))
                {
                    if (entity.BlocksPlayer)
                        occupied.Add(target.Value);
                    moved.Add(entity);
                    running++;
                    entity.StartCoroutine(RunThenCount(entity.Move(target.Value), () => running--));
                }
                else if (own)
                    occupied.Add(entity.Cell);
            }
            // (un objet détruit en plein bond ne doit pas bloquer le tour)
            while (running > 0 && moved.Exists(e => e != null && e.isActiveAndEnabled))
                yield return null;

            // 2. Les tours, l'un après l'autre.
            foreach (var entity in entities)
            {
                if (IsDead)
                    yield break;
                if (entity == null || !entity.IsActive || moved.Contains(entity))
                    continue;
                var run = Flatten(entity.TakeTurn(this));
                while (run.MoveNext())
                    yield return run.Current;
            }
            TurnEnded?.Invoke(Turn);
        }

        int Distance(Vector2Int cell) => Mathf.Abs(cell.x - Cell.x) + Mathf.Abs(cell.y - Cell.y);

        static IEnumerator RunThenCount(IEnumerator routine, System.Action done)
        {
            var run = Flatten(routine);
            while (run.MoveNext())
                yield return run.Current;
            done();
        }

        /// <summary>
        /// Déroule une coroutine et ses coroutines imbriquées sans passer par Unity : seules les vraies
        /// attentes (une image, des secondes) sont rendues. `yield return` d'une coroutine imbriquée
        /// coûte sinon une image, même quand elle n'a rien à faire.
        /// </summary>
        static IEnumerator Flatten(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }
                if (top.Current is IEnumerator inner)
                    stack.Push(inner);
                else
                    yield return top.Current;
            }
        }

        // --- PV ------------------------------------------------------------------

        /// <summary>Appelé par les ennemis et les pièges. La défense est appliquée ici.</summary>
        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0)
                return;
            var dealt = Combat.DamageAfterDefense(amount, defense);
            Health = Mathf.Max(0, Health - dealt);
            HealthChanged?.Invoke(Health, maxHealth);
            Hurt?.Invoke(dealt);
            if (sounds != null)
                sounds.Play(SoundBank.Hit, hitVolume);
            if (Health == 0)
                Die();
            else if (cameraRig != null)
            {
                if (_shake != null)
                    StopCoroutine(_shake);
                _shake = StartCoroutine(Shake());
            }
        }

        /// <summary>Rend des PV, sans dépasser le maximum.</summary>
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0)
                return;
            Health = Mathf.Min(maxHealth, Health + amount);
            HealthChanged?.Invoke(Health, maxHealth);
        }

        /// <summary>Affiche un court message au HUD (pièges, coffres…).</summary>
        public void Notify(string text) => Notified?.Invoke(text);

        /// <summary>Petite secousse latérale de la caméra.</summary>
        IEnumerator Shake()
        {
            foreach (var offset in new[] { 0.12f, -0.09f, 0.05f, 0f })
            {
                var from = cameraRig.localPosition;
                var to = _cameraRest + Vector3.right * offset;
                yield return Animate(0.04f, t => cameraRig.localPosition = Vector3.Lerp(from, to, t));
            }
            cameraRig.localPosition = _cameraRest;
            _shake = null;
        }

        /// <summary>La caméra tombe au sol en basculant sur le côté.</summary>
        void Die()
        {
            IsDead = true;
            Died?.Invoke();
            if (cameraRig == null)
                return;
            if (_shake != null)
                StopCoroutine(_shake);
            var fromPos = cameraRig.localPosition;
            var toPos = new Vector3(_cameraRest.x, 0.35f, _cameraRest.z);
            var fromRot = cameraRig.localRotation;
            var toRot = fromRot * Quaternion.Euler(0f, 0f, 70f);
            StartCoroutine(Animate(0.6f, t =>
            {
                cameraRig.localPosition = Vector3.Lerp(fromPos, toPos, t * t);
                cameraRig.localRotation = Quaternion.Slerp(fromRot, toRot, t * t);
            }));
        }

        /// <summary>Appelle `apply` avec t de 0 à 1 (sinusoïde entrée-sortie) pendant `duration` secondes.</summary>
        static IEnumerator Animate(float duration, Action<float> apply)
        {
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                apply(0.5f - 0.5f * Mathf.Cos(Mathf.PI * elapsed / duration));
                yield return null;
            }
            apply(1f);
        }
    }
}
