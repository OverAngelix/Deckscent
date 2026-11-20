using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deckscent.Tests
{
    /// <summary>
    /// Le Spawner dans le donjon de démo (Test_Donjon, DemoDungeon : des tuiles avec du contenu) :
    /// le contenu des tuiles apparaît au lancement et à chaque pose.
    /// </summary>
    public class SpawnerTests
    {
        const string DemoScene = "Assets/Scenes/Tests/Test_Donjon.unity";

        Dungeon _dungeon;
        Spawner _spawner;

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
            Assert.That(_dungeon, Is.Not.Null);
            Assert.That(_spawner, Is.Not.Null, "Spawner dans Test_Donjon");
        }

        [UnityTest]
        public IEnumerator LeContenuDuDonjonDeDemoApparait()
        {
            yield return LoadScene();
            Assert.That(GridEntity.AllOf<Slime>().Count, Is.GreaterThan(0), "des slimes");
            Assert.That(GridEntity.AllOf<SpikeTrap>().Count, Is.GreaterThan(0), "des dalles");
            Assert.That(GridEntity.AllOf<Chest>().Count, Is.GreaterThan(0), "des coffres");

            var maze = _dungeon.Maze;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var cells = new HashSet<Vector2Int>();
            foreach (var entity in GridEntity.All)
            {
                Assert.That(maze.IsWalkable(entity.Cell), Is.True, entity.name + " sur du sol");
                Assert.That(cells.Add(entity.Cell), Is.True, "un seul élément par case : " + entity.Cell);
                Assert.That(entity.Cell, Is.Not.EqualTo(player.Cell), "rien sur le départ");
            }
            // Le couloir au nord du départ porte une dalle (DemoDungeon).
            var trapTile = new Vector2Int(4, 3);
            Assert.That(GridEntity.AllOf<SpikeTrap>().Exists(t => t.Cell / DungeonBoard.TileCells == trapTile), Is.True);
        }

        [UnityTest]
        public IEnumerator PoserUneCarteFaitApparaitreSonContenu()
        {
            yield return LoadScene();
            var board = _dungeon.Board;
            var before = GridEntity.AllOf<Slime>().Count;
            Vector2Int? placed = null;
            foreach (var exit in board.OpenExits())
            {
                var card = new TileCard("carrefour") { Content = "monstre" };
                if (_dungeon.Place(exit, card))
                {
                    placed = exit;
                    break;
                }
            }
            Assert.That(placed.HasValue, Is.True, "un carrefour posable quelque part");
            var slimes = GridEntity.AllOf<Slime>();
            Assert.That(slimes.Count, Is.EqualTo(before + 1), "un slime de plus");
            Assert.That(slimes.Exists(s => s.Cell / DungeonBoard.TileCells == placed.Value), Is.True, "dans la tuile posée");
        }

        [UnityTest]
        public IEnumerator UneCarteVideNeFaitRienApparaitre()
        {
            yield return LoadScene();
            // Le contenu seulement : le décor d'ambiance (tiré au hasard) a ses propres tests.
            Object.FindAnyObjectByType<Spawner>().Ambience = false;
            var before = GridEntity.All.Count;
            foreach (var exit in _dungeon.Board.OpenExits())
                if (_dungeon.Place(exit, new TileCard("carrefour")))
                    break;
            Assert.That(GridEntity.All.Count, Is.EqualTo(before));
        }
    }
}
