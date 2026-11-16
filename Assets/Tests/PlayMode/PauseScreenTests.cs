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
    /// Menu pause, dans la scène du donjon de démo : Échap / Start, la carte d'abord, les lignes,
    /// les options, et « Continuer » à Entrée qui ne fait pas jouer de tour.
    /// </summary>
    public class PauseScreenTests : InputTestFixture
    {
        const string DemoScene = "Assets/Scenes/Tests/Test_Donjon.unity";

        Keyboard _keyboard;
        Gamepad _gamepad;
        PauseScreen _pause;
        MapScreen _map;
        PlayerController _player;
        float _volume;

        public override void Setup()
        {
            base.Setup();
            GameSettings.SetLocale("fr", false);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _volume = GameSettings.Volume;
        }

        public override void TearDown()
        {
            GameSettings.SetVolume(_volume);
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
            _pause = Object.FindAnyObjectByType<PauseScreen>();
            _map = Object.FindAnyObjectByType<MapScreen>();
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(_pause, Is.Not.Null, "menu pause dans la scène de démo");
        }

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EchapOuvreEtFermeLaPause()
        {
            yield return LoadScene();
            Assert.That(_pause.IsOpen, Is.False);
            yield return Tap(_keyboard.escapeKey);
            Assert.That(_pause.IsOpen, Is.True, "Échap ouvre la pause");
            Assert.That(PauseScreen.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "le jeu est en pause");

            var cell = _player.Cell;
            yield return Tap(_keyboard.wKey);
            Assert.That(_player.Cell, Is.EqualTo(cell), "le joueur ne bouge pas en pause");

            yield return Tap(_keyboard.escapeKey);
            Assert.That(_pause.IsOpen, Is.False, "Échap reprend le jeu");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator StartOuvreEtFermeLaPause()
        {
            yield return LoadScene();
            yield return Tap(_gamepad.startButton);
            Assert.That(_pause.IsOpen, Is.True);
            yield return Tap(_gamepad.startButton);
            Assert.That(_pause.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator EchapFermeDAbordLaCarte()
        {
            yield return LoadScene();
            yield return Tap(_keyboard.mKey);
            Assert.That(_map.IsOpen, Is.True);
            yield return Tap(_keyboard.escapeKey);
            Assert.That(_map.IsOpen, Is.False, "Échap ferme la carte");
            Assert.That(_pause.IsOpen, Is.False, "sans ouvrir la pause");
            yield return Tap(_keyboard.escapeKey);
            Assert.That(_pause.IsOpen, Is.True, "le suivant ouvre la pause");
            yield return Tap(_keyboard.mKey);
            Assert.That(_map.IsOpen, Is.False, "la carte ne s'ouvre pas par-dessus la pause");
        }

        [UnityTest]
        public IEnumerator LesLignesSautentSauvegarderQuiEstAVenir()
        {
            yield return LoadScene();
            yield return Tap(_keyboard.escapeKey);
            Assert.That(_pause.Selected, Is.EqualTo(PauseScreen.Entry.Continue));
            yield return Tap(_keyboard.downArrowKey);
            Assert.That(_pause.Selected, Is.EqualTo(PauseScreen.Entry.Options));
            yield return Tap(_keyboard.downArrowKey);
            Assert.That(_pause.Selected, Is.EqualTo(PauseScreen.Entry.Menu), "Sauvegarder est grisée");
            yield return Tap(_keyboard.upArrowKey);
            yield return Tap(_keyboard.upArrowKey);
            yield return Tap(_keyboard.upArrowKey);
            Assert.That(_pause.Selected, Is.EqualTo(PauseScreen.Entry.Desktop), "en boucle");
        }

        [UnityTest]
        public IEnumerator LesOptionsSeReglentDepuisLaPause()
        {
            yield return LoadScene();
            GameSettings.SetVolume(0.5f);
            yield return Tap(_keyboard.escapeKey);
            yield return Tap(_keyboard.downArrowKey);
            yield return Tap(_keyboard.enterKey);
            Assert.That(_pause.OptionsOpen, Is.True);
            yield return Tap(_keyboard.rightArrowKey);
            Assert.That(GameSettings.Volume, Is.EqualTo(0.6f).Within(1e-4f));
            yield return Tap(_keyboard.escapeKey);
            Assert.That(_pause.OptionsOpen, Is.False, "Échap revient à la liste");
            Assert.That(_pause.IsOpen, Is.True, "sans quitter la pause");
            Assert.That(_pause.Selected, Is.EqualTo(PauseScreen.Entry.Options));
        }

        [UnityTest]
        public IEnumerator ContinuerAEntreeNeFaitPasAttendreUnTour()
        {
            yield return LoadScene();
            var turn = _player.Turn;
            yield return Tap(_keyboard.escapeKey);
            yield return Tap(_keyboard.enterKey); // Continuer ; Entrée est aussi « attendre un tour »
            Assert.That(_pause.IsOpen, Is.False);
            yield return null;
            yield return null;
            Assert.That(_player.Turn, Is.EqualTo(turn), "pas de tour joué en reprenant");
            yield return Tap(_keyboard.enterKey);
            var timeout = Time.realtimeSinceStartup + 3f;
            while (_player.Turn == turn && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(_player.Turn, Is.EqualTo(turn + 1), "Entrée refait attendre un tour ensuite");
        }
    }
}
