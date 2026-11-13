using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deckscent.Tests
{
    /// <summary>
    /// Déplacement du joueur dans la scène de test (plan figé, voir PrototypeBuilder.TestPlan),
    /// avec un clavier et une manette virtuels, via les actions d'InputSystem_Actions (map Player).
    /// Départ en (6, 3), tourné vers l'est.
    /// </summary>
    public class PlayerMovementTests : InputTestFixture
    {
        const string TestScene = "Assets/Scenes/Tests/Test_Labyrinthe.unity";

        Keyboard _keyboard;
        Gamepad _gamepad;
        PlayerController _player;

        // Le clavier virtuel est ajouté après la remise à zéro de l'Input System par la fixture,
        // et la scène chargée ensuite, au début de chaque test ([UnitySetUp] passerait avant
        // la fixture : les actions du joueur seraient activées sur l'ancien état).
        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
        }

        IEnumerator LoadScene()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                TestScene, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null; // Start() du joueur
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(_player, Is.Not.Null, "joueur introuvable dans la scène de test");
        }

        /// <summary>Appuie brièvement sur une touche puis attend la fin de l'animation.</summary>
        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            yield return null;
            Release(key);
            var timeout = Time.realtimeSinceStartup + 3f;
            while (_player.IsBusy && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(_player.IsBusy, Is.False, "le joueur est resté occupé");
            yield return null;
        }

        [UnityTest]
        public IEnumerator DepartSurLaCasePEtTourneVersLeCouloir()
        {
            yield return LoadScene();
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(6, 3)));
            Assert.That(_player.Facing, Is.EqualTo(1), "tourné vers l'est (premier couloir ouvert)");
            Assert.That(_player.transform.position, Is.EqualTo(_player.Maze.CellToWorld(new Vector2Int(6, 3))));
        }

        [UnityTest]
        public IEnumerator AvancerCouteUnTourEtLeMurArrete()
        {
            yield return LoadScene();
            yield return Tap(_keyboard.wKey); // Z en AZERTY
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(7, 3)));
            Assert.That(_player.Turn, Is.EqualTo(1));

            yield return Tap(_keyboard.wKey); // mur en (8, 3) : bump, pas de tour
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(7, 3)));
            Assert.That(_player.Turn, Is.EqualTo(1));
            Assert.That(Vector3.Distance(_player.transform.position, _player.Maze.CellToWorld(new Vector2Int(7, 3))),
                Is.LessThan(0.001f), "revenu au centre de la case après le bump");
        }

        [UnityTest]
        public IEnumerator TournerEstGratuitEtLesPasSuiventLOrientation()
        {
            yield return LoadScene();
            yield return Tap(_keyboard.qKey); // A en AZERTY : tourner à gauche
            Assert.That(_player.Facing, Is.EqualTo(0), "tourné vers le nord");
            Assert.That(_player.Turn, Is.EqualTo(0), "tourner ne coûte pas de tour");
            Assert.That(Mathf.DeltaAngle(_player.transform.eulerAngles.y, 0f), Is.EqualTo(0f).Within(0.01f));

            yield return Tap(_keyboard.aKey); // Q en AZERTY : pas à gauche = vers l'ouest
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(5, 3)));
            Assert.That(_player.Turn, Is.EqualTo(1));

            yield return Tap(_keyboard.sKey); // reculer vers le sud : mur en (5, 4)
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(5, 3)));
            Assert.That(_player.Turn, Is.EqualTo(1));

            yield return Tap(_keyboard.eKey); // tourner à droite : de nouveau l'est
            Assert.That(_player.Facing, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AttendreCouteUnTour()
        {
            yield return LoadScene();
            var ended = 0;
            _player.TurnEnded += _ => ended++;
            yield return Tap(_keyboard.enterKey); // Entrée
            Assert.That(_player.Turn, Is.EqualTo(1));
            Assert.That(ended, Is.EqualTo(1), "TurnEnded émis une fois");
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(6, 3)));
        }

        [UnityTest]
        public IEnumerator LaManetteDeplaceEtTourne()
        {
            yield return LoadScene();
            yield return Tap(_gamepad.dpad.up); // croix haut : avancer vers l'est
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(7, 3)));
            yield return Tap(_gamepad.leftShoulder); // LB : tourner à gauche
            Assert.That(_player.Facing, Is.EqualTo(0));
            yield return Tap(_gamepad.rightShoulder); // RB : tourner à droite
            Assert.That(_player.Facing, Is.EqualTo(1));
            yield return Tap(_gamepad.leftStickButton); // L3 : attendre
            Assert.That(_player.Turn, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator LaLanterneSAllumeEtSEteintSansCouterDeTour()
        {
            yield return LoadScene();
            Assert.That(_player.LanternOn, Is.True, "allumée au départ");
            yield return Tap(_keyboard.tKey);
            Assert.That(_player.LanternOn, Is.False);
            yield return Tap(_gamepad.buttonEast); // Rond / B
            Assert.That(_player.LanternOn, Is.True);
            Assert.That(_player.Turn, Is.EqualTo(0), "la lanterne ne coûte pas de tour");
        }
    }
}
