using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deckscent.Tests
{
    /// <summary>
    /// Fin d'étage dans le donjon de démo : la mission remplie fait poser l'antre du boss (avec le
    /// loup-garou et son décor), le boss vaincu laisse l'escalier, l'escalier mène à l'étage 2 (PV
    /// gardés, nouvelle mission). Fixture d'entrée (sans touche simulée) : Input System propre.
    /// </summary>
    public class BossTests : InputTestFixture
    {
        const string DemoScene = "Assets/Scenes/Tests/Test_Donjon.unity";

        Dungeon _dungeon;
        PlayerController _player;
        MissionTracker _tracker;

        public override void Setup()
        {
            base.Setup();
            GameSettings.SetLocale("fr", false);
        }

        IEnumerator LoadScene()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                DemoScene, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null;
            _dungeon = Object.FindAnyObjectByType<Dungeon>();
            _player = Object.FindAnyObjectByType<PlayerController>();
            _tracker = Object.FindAnyObjectByType<MissionTracker>();
            Assert.That(_tracker, Is.Not.Null);
        }

        /// <summary>Une mission d'un seul tour, remplie aussitôt : l'antre apparaît.</summary>
        IEnumerator CompleteMission()
        {
            var mission = new Mission(new[] { new MissionGoal(GoalKind.Turns, 1) });
            _dungeon.SetMission(mission);
            yield return null;
            mission.SetTurn(1);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaMissionRemplieFaitPoserLAntreDuBoss()
        {
            yield return LoadScene();
            Assert.That(_dungeon.Board.BossLair, Is.Null, "pas d'antre avant la fin de la mission");
            var hud = Object.FindAnyObjectByType<Hud>();
            yield return CompleteMission();
            var board = _dungeon.Board;
            Assert.That(board.BossLair, Is.Not.Null, "l'antre est posée");
            Assert.That(_tracker.BossLairPending, Is.False);
            Assert.That(hud.Toast, Does.Contain("antre"));
            var lairSlots = board.BossLair.Slots.Select(o => board.BossLairOrigin + o).ToList();
            foreach (var slot in lairSlots)
                Assert.That(_dungeon.Exploration.IsSlotVisited(slot), Is.False, "jamais sur un emplacement visité");
            var playerSlot = Exploration.SlotOf(_player.Cell);
            Assert.That(board.SlotDistances(playerSlot).ContainsKey(lairSlots[0]), Is.True, "atteignable depuis le joueur");
            var wolves = GridEntity.AllOf<Werewolf>();
            Assert.That(wolves.Count, Is.EqualTo(1), "un loup-garou");
            Assert.That(lairSlots.Contains(Exploration.SlotOf(wolves[0].Cell)), Is.True, "dans son antre");
            Assert.That(wolves[0].IsAwake, Is.False, "il dort en attendant le joueur");
            Assert.That(GridEntity.AllOf<LairDecor>().Count(d => d.BlocksPlayer), Is.GreaterThanOrEqualTo(2), "braseros, trône…");
        }

        [UnityTest]
        public IEnumerator LeBossVaincuOuvreLEscalierVersLEtageSuivant()
        {
            yield return LoadScene();
            yield return CompleteMission();
            var boss = GridEntity.AllOf<Werewolf>().Single();
            var cell = boss.Cell;
            _player.TakeDamage(2);
            var health = _player.Health;
            boss.TakeDamage(999);
            Assert.That(boss.IsActive, Is.False);
            Assert.That(Object.FindAnyObjectByType<Hud>().PopupCount, Is.GreaterThan(0), "chiffre de dégâts au-dessus de lui");
            yield return new WaitForSeconds(1.6f);
            var stairs = GridEntity.AllOf<Stairs>().SingleOrDefault();
            Assert.That(stairs, Is.Not.Null, "l'escalier sort du sol");
            Assert.That(stairs.Cell, Is.EqualTo(cell), "à la place du boss");

            var firstMission = _dungeon.Mission;
            // Des tours ont passé à cet étage (l'objectif « tours » ne doit pas les reporter).
            typeof(PlayerController).GetProperty("Turn").SetValue(_player, 80);
            yield return _player.StartCoroutine(stairs.Interact(_player));
            Assert.That(_dungeon.Floor, Is.EqualTo(2), "étage suivant");
            Assert.That(_player.Health, Is.EqualTo(health), "PV gardés");
            Assert.That(_dungeon.Mission, Is.Not.SameAs(firstMission), "nouvelle mission");
            // Les tours de la mission comptent depuis le début de l'étage, pas de la partie.
            Object.FindAnyObjectByType<MissionTracker>().SendMessage("OnTurnEnded", 80);
            foreach (var goal in _dungeon.Mission.Goals)
                Assert.That(goal.Progress, Is.Zero, goal.LabelKey + " repart de zéro");
            Assert.That(_dungeon.Board.BossLair, Is.Null, "pas encore d'antre au nouvel étage");
            Assert.That(_player.Cell, Is.EqualTo(_dungeon.Maze.StartCell), "le joueur repart du départ");
            Assert.That(GridEntity.AllOf<Werewolf>(), Is.Empty);
            Assert.That(Werewolf.Current, Is.Null);
        }

        /// <summary>Met le donjon au sous-sol `floor`, tue le boss et attend l'escalier.</summary>
        IEnumerator StairsAtFloor(int floor)
        {
            typeof(Dungeon).GetProperty("Floor").SetValue(_dungeon, floor);
            yield return CompleteMission();
            GridEntity.AllOf<Werewolf>().Single().TakeDamage(999);
            yield return new WaitForSeconds(1.6f);
        }

        [UnityTest]
        public IEnumerator AuTroisiemeSousSolLEscalierProposeDeRemonterOuDeDescendre()
        {
            yield return LoadScene();
            Assert.That(_dungeon.IsSubBiomeEnd, Is.False, "le sous-sol 1 n'est pas une fin de sous-biome");
            yield return StairsAtFloor(3);
            Assert.That(_dungeon.IsSubBiomeEnd, Is.True);
            var stairs = GridEntity.AllOf<Stairs>().Single();
            var screen = Object.FindAnyObjectByType<AscentScreen>();

            // Retour : on reste à l'étage, l'escalier reste ouvert.
            var run = _player.StartCoroutine(stairs.Interact(_player));
            yield return null;
            Assert.That(screen.IsOpen, Is.True, "le choix s'ouvre");
            Assert.That(Time.timeScale, Is.Zero, "le jeu est en pause");
            screen.Stay();
            yield return run;
            Assert.That(stairs.Used, Is.False, "l'escalier reste ouvert");
            Assert.That(_dungeon.Floor, Is.EqualTo(3));
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            // Descendre : le sous-sol suivant, comme d'habitude.
            run = _player.StartCoroutine(stairs.Interact(_player));
            yield return null;
            screen.Choose(1);
            yield return run;
            Assert.That(_dungeon.Floor, Is.EqualTo(4), "on descend au sous-sol 4");
            Assert.That(_dungeon.IsSubBiomeEnd, Is.False);
        }

        [UnityTest]
        public IEnumerator RemonterMetToutLOrALAbriEtFinitLExpedition()
        {
            yield return LoadScene();
            yield return StairsAtFloor(3);
            var stairs = GridEntity.AllOf<Stairs>().Single();
            var screen = Object.FindAnyObjectByType<AscentScreen>();
            var camp = Progression.Gold;
            Gold.Add(40);
            var carried = Gold.Carried;

            _player.StartCoroutine(stairs.Interact(_player));
            yield return null;
            screen.Choose(0);
            yield return null;
            yield return null;
            Assert.That(Gold.Carried, Is.Zero);
            Assert.That(Progression.Gold, Is.EqualTo(camp + carried), "tout l'or porté va au camp");
            Assert.That(Object.FindAnyObjectByType<Hud>().SurfaceTime, Is.GreaterThanOrEqualTo(0f), "« De retour à la surface »");
            Assert.That(Time.timeScale, Is.Zero, "le jeu reste figé");
            Assert.That(_dungeon.Floor, Is.EqualTo(3), "on ne descend pas");
            Time.timeScale = 1f;
        }

        /// <summary>Pose le joueur sur une case (sans animation ni tour).</summary>
        void PutPlayer(Vector2Int cell)
        {
            typeof(PlayerController).GetProperty("Cell").SetValue(_player, cell);
            _player.transform.position = _dungeon.Maze.CellToWorld(cell);
        }

        IEnumerator BossTurn(Werewolf boss) => boss.TakeTurn(_player);

        /// <summary>Une direction depuis le boss avec `count` cases libres en ligne droite (ni mur, ni décor).</summary>
        static int OpenLine(Werewolf boss, int count)
        {
            for (var d = 0; d < 4; d++)
            {
                var c = boss.Cell;
                var k = 0;
                for (; k < count && boss.Maze.CanStep(c, d); k++)
                {
                    c += Grid.Dirs[d];
                    if (GridEntity.At<GridEntity>(c) != null && GridEntity.At<GridEntity>(c).BlocksPlayer)
                        break;
                }
                if (k >= count)
                    return d;
            }
            return -1;
        }

        [UnityTest]
        public IEnumerator LeBossSeReveilleArmeSonCoupEtLeRateSiOnRecule()
        {
            yield return LoadScene();
            yield return CompleteMission();
            var boss = GridEntity.AllOf<Werewolf>().Single();
            var dir = OpenLine(boss, 3);
            Assert.That(dir, Is.GreaterThanOrEqualTo(0), "une ligne libre devant le boss");
            // Dans l'antre : il se réveille (hurlement, barre de vie).
            PutPlayer(boss.Cell + Grid.Dirs[dir] * 3);
            yield return BossTurn(boss);
            Assert.That(boss.IsAwake, Is.True);
            Assert.That(Werewolf.Current, Is.SameAs(boss));
            // À côté : il arme son coup chargé…
            boss.SetAttackWeights(0f, 1f, 0f);
            PutPlayer(boss.Cell + Grid.Dirs[dir]);
            yield return BossTurn(boss);
            Assert.That(boss.IsArmed, Is.True, "le coup est annoncé");
            // … le joueur recule : le coup part quand même, dans le vide.
            var health = _player.Health;
            PutPlayer(boss.Cell + Grid.Dirs[dir] * 2);
            yield return BossTurn(boss);
            Assert.That(_player.Health, Is.EqualTo(health), "rien reçu en reculant");
            Assert.That(boss.IsArmed, Is.False, "le coup chargé est parti");
            // Le joueur revient à côté : coup de base, tout de suite.
            boss.SetAttackWeights(1f, 0f, 0f);
            PutPlayer(boss.Cell + Grid.Dirs[dir]);
            yield return BossTurn(boss);
            Assert.That(boss.IsArmed, Is.False, "pas d'annonce pour le coup de base");
            Assert.That(_player.Health, Is.EqualTo(health - Combat.DamageAfterDefense(boss.Damage, _player.Defense)));
            Assert.That(boss.Damage, Is.EqualTo(2), "coup de base à 2");
        }

        [UnityTest]
        public IEnumerator LaChargeEstAnnonceeEtSEsquiveEnSeDecalant()
        {
            yield return LoadScene();
            yield return CompleteMission();
            var boss = GridEntity.AllOf<Werewolf>().Single();
            var dir = OpenLine(boss, 4);
            Assert.That(dir, Is.GreaterThanOrEqualTo(0), "une ligne libre devant le boss");
            PutPlayer(boss.Cell + Grid.Dirs[dir] * 4);
            boss.SetAttackWeights(0f, 0f, 1f);
            yield return BossTurn(boss); // réveil
            yield return BossTurn(boss); // se ramasse
            Assert.That(boss.AnnouncedCharge, Is.EqualTo(dir), "la charge est annoncée vers le joueur");
            // Le joueur sort de la ligne : la charge le manque, le boss finit sonné.
            var health = _player.Health;
            var side = Grid.Dirs[(dir + 1) % 4];
            PutPlayer(boss.Cell + Grid.Dirs[dir] * 4 + side * 50); // loin hors de la ligne
            yield return BossTurn(boss);
            Assert.That(_player.Health, Is.EqualTo(health), "esquivée");
            Assert.That(boss.IsStunned, Is.True, "sonné contre le mur");
        }

        [UnityTest]
        public IEnumerator LaFontaineSoigneUneSeuleFois()
        {
            yield return LoadScene();
            var spawner = Object.FindAnyObjectByType<Spawner>();
            var shrine = (Shrine)spawner.SpawnElement("sanctuaire", _player.Cell);
            Assert.That(shrine, Is.Not.Null);
            Assert.That(shrine.BlocksPlayer, Is.False, "on passe dessus");
            _player.TakeDamage(5);
            var health = _player.Health;
            yield return shrine.TakeTurn(_player);
            Assert.That(_player.Health, Is.EqualTo(Mathf.Min(_player.MaxHealth, health + shrine.Heal)));
            Assert.That(shrine.Used, Is.True);
            _player.TakeDamage(2);
            health = _player.Health;
            yield return shrine.TakeTurn(_player);
            Assert.That(_player.Health, Is.EqualTo(health), "tarie");
        }

        [UnityTest]
        public IEnumerator LeDecorAmbianceApparaitEtLesVasesSeBrisent()
        {
            yield return LoadScene();
            Assert.That(GridEntity.AllOf<LairDecor>().Count, Is.GreaterThan(0), "décor dans les salles et couloirs");
            var vase = GridEntity.AllOf<Breakable>().FirstOrDefault();
            Assert.That(vase, Is.Not.Null, "un vase ou une caisse dans les coins des salles");
            Assert.That(vase.BlocksPlayer, Is.True);
            vase.TakeDamage(1);
            Assert.That(vase.IsBroken, Is.True);
            Assert.That(vase.BlocksPlayer, Is.False, "la case se libère");
        }

        [UnityTest]
        public IEnumerator LaMimiqueMordQuandOnVeutLOuvrir()
        {
            yield return LoadScene();
            var spawner = Object.FindAnyObjectByType<Spawner>();
            var maze = _dungeon.Maze;
            var dir = -1;
            for (var d = 0; d < 4 && dir < 0; d++)
                if (maze.CanStep(_player.Cell, d) && GridEntity.At<GridEntity>(_player.Cell + Grid.Dirs[d]) == null)
                    dir = d;
            Assert.That(dir, Is.GreaterThanOrEqualTo(0));
            var mimic = (Mimic)spawner.SpawnElement("mimique", _player.Cell + Grid.Dirs[dir]);
            Assert.That(mimic.IsHidden, Is.True);
            Assert.That(mimic.Threatens, Is.False, "cachée : pas de combat");
            Assert.That(mimic.CanInteract, Is.True, "elle se fait passer pour un coffre");
            var health = _player.Health;
            yield return mimic.Interact(_player);
            Assert.That(mimic.IsHidden, Is.False, "révélée");
            Assert.That(_player.Health, Is.LessThan(health), "la morsure de surprise");
            Assert.That(Combat.InCombat(_player), Is.True, "révélée, à côté : en combat");
        }

        [UnityTest]
        public IEnumerator LesMonstresSontPlusFortsPlusBas()
        {
            yield return LoadScene();
            var spawner = Object.FindAnyObjectByType<Spawner>();
            var shallow = spawner.SpawnSlime(new Vector2Int(-50, -50), Slime.Variant.Vert);
            var hp1 = shallow.MaxHealth;
            Object.Destroy(shallow.gameObject);
            _dungeon.NextFloor();
            _dungeon.NextFloor();
            yield return null;
            var deep = spawner.SpawnSlime(new Vector2Int(-50, -50), Slime.Variant.Vert);
            Assert.That(deep.MaxHealth, Is.EqualTo(hp1 + 2), "+1 PV par étage");
            Object.Destroy(deep.gameObject);
        }
    }
}
