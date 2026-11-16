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
    /// Suivi de la mission dans le donjon de démo : monstres tués, coffres ouverts, salles
    /// nettoyées (seulement une fois visitées et vidées), message de fin. Fixture d'entrée (sans
    /// touche simulée) : les tests qui suivent gardent un Input System propre.
    /// </summary>
    public class MissionTrackerTests : InputTestFixture
    {
        const string DemoScene = "Assets/Scenes/Tests/Test_Donjon.unity";

        Dungeon _dungeon;
        Spawner _spawner;
        MissionTracker _tracker;
        PlayerController _player;

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
            _spawner = Object.FindAnyObjectByType<Spawner>();
            _tracker = Object.FindAnyObjectByType<MissionTracker>();
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(_tracker, Is.Not.Null, "suivi de mission dans la scène de démo");
        }

        /// <summary>Une mission avec toutes les sortes d'objectifs, longue.</summary>
        Mission FullMission(int target = 50)
        {
            var mission = new Mission(System.Enum.GetValues(typeof(GoalKind)).Cast<GoalKind>().Select(k => new MissionGoal(k, target)));
            _dungeon.SetMission(mission);
            return mission;
        }

        static int ProgressOf(Mission mission, GoalKind kind) => mission.Goals.First(g => g.Kind == kind).Progress;

        /// <summary>Une case d'une salle (carte de salle posée), au choix selon son contenu.</summary>
        Vector2Int RoomCell(System.Func<TileCard, Spawner.TileContents, bool> match, out TileCard room)
        {
            var board = _dungeon.Board;
            foreach (var pos in board.TilePositions)
            {
                var card = board.GetTile(pos);
                if (card.IsRoom && pos != board.StartTile && match(card, _spawner.ContentsOf(card)))
                {
                    room = card;
                    return DungeonBoard.TileCenterCell(pos);
                }
            }
            room = null;
            Assert.Fail("pas de salle qui convienne dans le donjon de démo");
            return default;
        }

        [UnityTest]
        public IEnumerator LeDonjonTireUneMissionQueLeHudAffiche()
        {
            yield return LoadScene();
            Assert.That(_dungeon.Mission, Is.Not.Null);
            Assert.That(_dungeon.Mission.Goals.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(_tracker.Mission, Is.SameAs(_dungeon.Mission));
        }

        [UnityTest]
        public IEnumerator UnMonstreTueEtUnCoffreOuvertComptent()
        {
            yield return LoadScene();
            var mission = FullMission();
            var slime = GridEntity.AllOf<Slime>().First();
            slime.TakeDamage(999);
            Assert.That(ProgressOf(mission, GoalKind.Monsters), Is.EqualTo(1));

            var chest = GridEntity.AllOf<Chest>().First();
            yield return chest.Interact(_player);
            Assert.That(ProgressOf(mission, GoalKind.Chests), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator UneSalleNeCompteQueNettoyeeEtVisiteeUneFois()
        {
            yield return LoadScene();
            var mission = FullMission();

            // Salle vide : elle compte dès qu'on y entre.
            var empty = RoomCell((card, contents) => contents == null, out _);
            _tracker.Visit(empty);
            Assert.That(ProgressOf(mission, GoalKind.Rooms), Is.EqualTo(1), "salle vide visitée");

            // Salle avec un monstre ou un coffre : visitée, elle ne compte pas encore.
            var full = RoomCell((card, contents) => contents != null && contents.Elements.Any(e => !(e is SpikeTrap)), out var room);
            _tracker.Visit(full);
            Assert.That(ProgressOf(mission, GoalKind.Rooms), Is.EqualTo(1), "pas nettoyée");
            foreach (var element in _spawner.ContentsOf(room).Elements.ToList())
            {
                if (element is Enemy enemy)
                    enemy.TakeDamage(999);
                else if (element is Chest chest)
                    yield return chest.Interact(_player);
            }
            Assert.That(_spawner.ContentsOf(room).IsCleared, Is.True);
            Assert.That(ProgressOf(mission, GoalKind.Rooms), Is.EqualTo(2), "nettoyée et visitée");
            _tracker.Visit(full);
            Assert.That(ProgressOf(mission, GoalKind.Rooms), Is.EqualTo(2), "une seule fois");
        }

        [UnityTest]
        public IEnumerator LesDallesNOntPasAEtreFranchiesPourNettoyerUneSalle()
        {
            yield return LoadScene();
            var mission = FullMission();
            var cell = RoomCell((card, contents) => contents != null && contents.Elements.Any(e => e is SpikeTrap), out var room);
            _tracker.Visit(cell);
            foreach (var element in _spawner.ContentsOf(room).Elements.ToList())
            {
                if (element is Enemy enemy)
                    enemy.TakeDamage(999);
                else if (element is Chest chest)
                    yield return chest.Interact(_player);
            }
            Assert.That(_spawner.ContentsOf(room).Elements.OfType<SpikeTrap>().All(t => !t.IsCleared), "aucune dalle franchie");
            Assert.That(ProgressOf(mission, GoalKind.Rooms), Is.EqualTo(1), "salle nettoyée sans passer sur ses dalles");
            Assert.That(ProgressOf(mission, GoalKind.Traps), Is.Zero, "les dalles restent un objectif à part");
        }

        [UnityTest]
        public IEnumerator UneSalleNettoyeeSansYEntrerNeComptePas()
        {
            yield return LoadScene();
            var mission = FullMission();
            RoomCell((card, contents) => contents != null && contents.Elements.Any(e => !(e is SpikeTrap)), out var room);
            foreach (var element in _spawner.ContentsOf(room).Elements.ToList())
            {
                if (element is Enemy enemy)
                    enemy.TakeDamage(999);
                else if (element is Chest chest)
                    yield return chest.Interact(_player);
            }
            Assert.That(_spawner.ContentsOf(room).IsCleared, Is.True);
            Assert.That(ProgressOf(mission, GoalKind.Rooms), Is.Zero, "jamais visitée");
        }

        [UnityTest]
        public IEnumerator LaMissionRemplieLeDitAuJoueur()
        {
            yield return LoadScene();
            var mission = new Mission(new[] { new MissionGoal(GoalKind.Monsters, 1) });
            _dungeon.SetMission(mission);
            string message = null;
            _player.Notified += text => message = text;
            GridEntity.AllOf<Slime>().First().TakeDamage(999);
            Assert.That(mission.IsComplete, Is.True);
            Assert.That(message, Is.EqualTo("Mission remplie !"));
        }
    }
}
