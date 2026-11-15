using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deckscent.Tests
{
    /// <summary>
    /// Écran de carte, dans la scène du donjon de démo (graine fixe) : ouverture, pause, pose,
    /// refus, rotation, défausse, manette.
    /// </summary>
    public class MapScreenTests : InputTestFixture
    {
        const string DemoScene = "Assets/Scenes/Tests/Test_Donjon.unity";

        Keyboard _keyboard;
        Gamepad _gamepad;
        MapScreen _map;
        Dungeon _dungeon;
        PlayerController _player;

        public override void Setup()
        {
            base.Setup();
            GameSettings.SetLocale("fr", false); // les messages vérifiés sont en français
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
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
            _map = Object.FindAnyObjectByType<MapScreen>();
            _dungeon = Object.FindAnyObjectByType<Dungeon>();
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(_map, Is.Not.Null, "carte introuvable dans la scène de démo");
            Assert.That(_dungeon.Board, Is.Not.Null);
        }

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        /// <summary>Tourne la carte choisie jusqu'à trouver où la poser. Faux si nulle part.</summary>
        bool FindPlacement(out Vector2Int slot)
        {
            var card = _map.SelectedCard;
            for (var turn = 0; turn < 4; turn++)
            {
                for (var y = 0; y < _dungeon.Board.Size.y; y++)
                    for (var x = 0; x < _dungeon.Board.Size.x; x++)
                        if (_dungeon.Board.CanPlace(new Vector2Int(x, y), card))
                        {
                            slot = new Vector2Int(x, y);
                            return true;
                        }
                _map.RotateSelected(1);
            }
            slot = default;
            return false;
        }

        [UnityTest]
        public IEnumerator LaToucheDeLaCarteLOuvreEtMetLeJeuEnPause()
        {
            yield return LoadScene();
            Assert.That(_map.IsOpen, Is.False);
            yield return Tap(_keyboard.mKey); // virgule en AZERTY
            Assert.That(_map.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "le jeu est en pause");

            var cell = _player.Cell;
            yield return Tap(_keyboard.wKey);
            Assert.That(_player.Cell, Is.EqualTo(cell), "le joueur ne bouge pas carte ouverte");

            yield return Tap(_keyboard.escapeKey);
            Assert.That(_map.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator PoserUneTuileReecritLeLabyrintheEtRemplaceLaCarte()
        {
            yield return LoadScene();
            _map.Open();
            yield return null;
            Assert.That(FindPlacement(out var slot), "la main de départ peut se poser quelque part");
            var card = _map.SelectedCard;
            var handSize = _dungeon.Deck.Hand.Count;
            var center = DungeonBoard.TileCenterCell(slot);
            Assert.That(_dungeon.Maze.IsWalkable(center), Is.False, "roche avant la pose");

            Assert.That(_map.TryPlace(slot), Is.True);
            yield return null;
            Assert.That(_dungeon.Board.GetTile(slot), Is.SameAs(card));
            Assert.That(_dungeon.Maze.IsWalkable(center), Is.True, "le labyrinthe a été reconstruit avec la tuile");
            Assert.That(_dungeon.Deck.Hand, Has.No.Member(card), "la carte posée quitte la main");
            Assert.That(_dungeon.Deck.Hand.Count, Is.EqualTo(handSize), "une nouvelle carte la remplace");
        }

        [UnityTest]
        public IEnumerator UnePoseRefuseeDonneSaRaison()
        {
            yield return LoadScene();
            _map.Open();
            yield return null;
            var start = _dungeon.Board.StartTile;
            Assert.That(_map.TryPlace(start), Is.False, "emplacement déjà occupé");
            Assert.That(_dungeon.Board.CheckPlacement(start, _map.SelectedCard), Is.EqualTo("refus.occupe"));
        }

        [UnityTest]
        public IEnumerator TournerEtDefausserLaCarteChoisie()
        {
            yield return LoadScene();
            yield return Tap(_keyboard.mKey);
            var card = _map.SelectedCard;
            var slotsBefore = card.Slots.ToArray();
            var doorsBefore = card.Openings;
            yield return Tap(_keyboard.rKey);
            if (!card.IsRoom)
                Assert.That(card.Openings, Is.EqualTo(TileCard.RotateMask(doorsBefore, 1)), "R tourne d'un quart de tour");
            else
                Assert.That(card.Slots[1], Is.Not.EqualTo(slotsBefore[1]));

            yield return Tap(_keyboard.deleteKey);
            Assert.That(_dungeon.Deck.Hand, Has.No.Member(card), "Suppr défausse la carte");
        }

        [UnityTest]
        public IEnumerator LaManetteOuvreChoisitEtFerme()
        {
            yield return LoadScene();
            yield return Tap(_gamepad.buttonNorth); // Triangle / Y
            Assert.That(_map.IsOpen, Is.True);
            yield return Tap(_gamepad.rightShoulder);
            Assert.That(_map.Selected, Is.EqualTo(1), "RB : carte suivante");
            yield return Tap(_gamepad.leftShoulder);
            Assert.That(_map.Selected, Is.EqualTo(0), "LB : carte précédente");
            var lantern = _player.LanternOn;
            yield return Tap(_gamepad.buttonEast); // Rond / B : ferme, sans toucher à la lanterne
            Assert.That(_map.IsOpen, Is.False);
            Assert.That(_player.LanternOn, Is.EqualTo(lantern), "fermer la carte n'éteint pas la lanterne");
        }
    }
}
