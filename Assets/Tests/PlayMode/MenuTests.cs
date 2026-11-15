using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deckscent.Tests
{
    /// <summary>
    /// Le menu (scène Menu) au clavier virtuel : choisir une carte, les options (son, langue),
    /// la liste des commandes, descendre dans le donjon. Les options touchées sont remises
    /// comme avant à la fin de chaque test.
    /// </summary>
    public class MenuTests : InputTestFixture
    {
        Keyboard _keyboard;
        MenuScreen _menu;
        float _volume, _music, _effects;
        string _locale;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _volume = GameSettings.Volume;
            _music = GameSettings.MusicVolume;
            _effects = GameSettings.EffectsVolume;
            _locale = GameSettings.Locale;
            GameSettings.SetLocale("fr", false);
        }

        public override void TearDown()
        {
            GameSettings.SetVolume(_volume);
            GameSettings.SetMusicVolume(_music);
            GameSettings.SetEffectsVolume(_effects);
            GameSettings.SetLocale(_locale);
            base.TearDown();
        }

        IEnumerator LoadMenu()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                SceneFlow.MenuScene, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null;
            _menu = Object.FindAnyObjectByType<MenuScreen>();
            Assert.That(_menu, Is.Not.Null, "écran titre dans la scène Menu");
        }

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LeMenuSAfficheAvecLaMusique()
        {
            yield return LoadMenu();
            Assert.That(_menu.Selected, Is.EqualTo(0), "Descendre choisie au départ");
            var music = Object.FindAnyObjectByType<MenuMusic>();
            Assert.That(music, Is.Not.Null);
            Assert.That(music.GetComponent<AudioSource>().isPlaying, Is.True);
            Assert.That(Object.FindAnyObjectByType<Maze>().IsWalkable(Object.FindAnyObjectByType<Maze>().StartCell), Is.True, "la salle est construite");
        }

        [UnityTest]
        public IEnumerator LesFlechesEtLesChiffresChoisissentUneCarte()
        {
            yield return LoadMenu();
            yield return Tap(_keyboard.rightArrowKey);
            Assert.That(_menu.Selected, Is.EqualTo(1));
            yield return Tap(_keyboard.leftArrowKey);
            yield return Tap(_keyboard.leftArrowKey);
            Assert.That(_menu.Selected, Is.EqualTo(3), "en boucle");
            yield return Tap(_keyboard.digit3Key);
            Assert.That(_menu.Selected, Is.EqualTo(2));
            yield return Tap(_keyboard.enterKey);
            Assert.That(_menu.OpenPanel, Is.EqualTo(MenuScreen.Panel.Controls));
            yield return Tap(_keyboard.escapeKey);
            Assert.That(_menu.OpenPanel, Is.EqualTo(MenuScreen.Panel.None));
        }

        [UnityTest]
        public IEnumerator LesOptionsReglentLeSonEtLaLangue()
        {
            yield return LoadMenu();
            GameSettings.SetVolume(0.5f);
            yield return Tap(_keyboard.digit2Key);
            yield return Tap(_keyboard.enterKey);
            Assert.That(_menu.OpenPanel, Is.EqualTo(MenuScreen.Panel.Options));
            yield return Tap(_keyboard.rightArrowKey);
            Assert.That(GameSettings.Volume, Is.EqualTo(0.6f).Within(1e-4f));
            Resources.Load<AudioMixer>("Mixage").GetFloat("VolumeGeneral", out var db);
            Assert.That(db, Is.EqualTo(20f * Mathf.Log10(0.6f)).Within(0.01f), "appliqué à la piste générale du mixer");
            yield return Tap(_keyboard.downArrowKey);
            Assert.That(_menu.OptionRow, Is.EqualTo(1), "ligne Musique");
            GameSettings.SetMusicVolume(0.5f);
            yield return Tap(_keyboard.rightArrowKey);
            Assert.That(GameSettings.MusicVolume, Is.EqualTo(0.6f).Within(1e-4f), "la musique a son propre volume");
            Assert.That(GameSettings.Volume, Is.EqualTo(0.6f).Within(1e-4f), "le général ne bouge pas");
            yield return Tap(_keyboard.downArrowKey);
            yield return Tap(_keyboard.downArrowKey);
            Assert.That(_menu.OptionRow, Is.EqualTo(3), "ligne Langue");
            yield return Tap(_keyboard.rightArrowKey);
            Assert.That(GameSettings.Locale, Is.EqualTo("en"));
            Assert.That(Loc.T("Descendre"), Is.EqualTo("Descend"));
            yield return Tap(_keyboard.enterKey);
            Assert.That(GameSettings.Locale, Is.EqualTo("fr"), "Entrée change aussi de langue");
            yield return Tap(_keyboard.escapeKey);
            Assert.That(_menu.OpenPanel, Is.EqualTo(MenuScreen.Panel.None));
        }

        [UnityTest]
        public IEnumerator DescendreLanceLaPartie()
        {
            yield return LoadMenu();
            yield return Tap(_keyboard.enterKey);
            Assert.That(_menu.IsLeaving, Is.True, "fondu au noir");
            Assert.That(Object.FindAnyObjectByType<MenuMusic>().IsFadingOut, Is.True, "la musique s'éteint");
            var timeout = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != "Main" && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"));
            yield return null;
            Assert.That(Object.FindAnyObjectByType<PlayerController>(), Is.Not.Null);
        }
    }
}
