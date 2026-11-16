using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deckscent.Tests
{
    /// <summary>Brouillard en jeu, dans le donjon de démo : ce que voit le joueur au départ, puis en marchant.</summary>
    public class ExplorationTrackerTests : InputTestFixture
    {
        const string DemoScene = "Assets/Scenes/Tests/Test_Donjon.unity";

        Keyboard _keyboard;
        Dungeon _dungeon;
        PlayerController _player;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
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
            yield return null;
            _dungeon = Object.FindAnyObjectByType<Dungeon>();
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(Object.FindAnyObjectByType<ExplorationTracker>(), Is.Not.Null, "suivi de l'exploration dans la scène de démo");
        }

        [UnityTest]
        public IEnumerator AuDepartOnNeConnaitQueCeQuOnVoit()
        {
            yield return LoadScene();
            var fog = _dungeon.Exploration;
            Assert.That(fog.IsSeen(_player.Cell), "la case du joueur");
            Assert.That(fog.IsSlotVisited(_dungeon.Board.StartTile), "le départ est visité");
            var seenSlots = 0;
            var placedSlots = 0;
            foreach (var pos in _dungeon.Board.TilePositions)
            {
                placedSlots++;
                if (fog.IsSlotSeen(pos))
                    seenSlots++;
            }
            Assert.That(seenSlots, Is.GreaterThan(1), "on voit dans les couloirs du départ");
            Assert.That(seenSlots, Is.LessThan(placedSlots), "le reste du donjon de démo est en esquisse");
        }

        [UnityTest]
        public IEnumerator MarcherDevoileDeNouvellesCases()
        {
            yield return LoadScene();
            var fog = _dungeon.Exploration;
            var before = fog.SeenCells.Count;
            // Avancer jusqu'à ce qu'on ait bougé (le départ s'oriente vers un couloir ouvert).
            var start = _player.Cell;
            for (var i = 0; i < 4 && _player.Cell == start; i++)
            {
                Press(_keyboard.wKey);
                yield return null;
                yield return null;
                Release(_keyboard.wKey);
                var timeout = Time.realtimeSinceStartup + 3f;
                while (_player.IsBusy && Time.realtimeSinceStartup < timeout)
                    yield return null;
                yield return null;
            }
            Assert.That(_player.Cell, Is.Not.EqualTo(start), "le joueur a avancé");
            yield return null;
            Assert.That(fog.IsSeen(_player.Cell));
            Assert.That(fog.SeenCells.Count, Is.GreaterThanOrEqualTo(before));
            Assert.That(fog.IsSlotVisited(Exploration.SlotOf(_player.Cell)));
        }
    }
}
