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
    /// Écran de montée de niveau, dans la scène du donjon de démo : il s'ouvre (pause) quand un
    /// niveau attend, on choisit au clavier, le bienfait s'applique ; un niveau à la fois.
    /// </summary>
    public class LevelUpTests : InputTestFixture
    {
        const string DemoScene = "Assets/Scenes/Tests/Test_Donjon.unity";

        Keyboard _keyboard;
        LevelUpScreen _screen;
        PlayerController _player;

        public override void Setup()
        {
            base.Setup();
            GameSettings.SetLocale("fr", false);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            LevelUpScreen.AutoOpen = true;
        }

        public override void TearDown()
        {
            LevelUpScreen.AutoOpen = false;
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
            _screen = Object.FindAnyObjectByType<LevelUpScreen>();
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(_screen, Is.Not.Null, "écran de niveau dans la scène de démo");
        }

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        static IEnumerator WaitRealtime(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator UnNiveauGagneMetEnPauseEtLeChoixSApplique()
        {
            yield return LoadScene();
            Assert.That(_screen.IsOpen, Is.False);
            var defense = _player.Defense;

            _player.GainExperience(10);
            yield return null;
            Assert.That(_screen.IsOpen, Is.True, "le niveau 2 attend son bienfait");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "le jeu est en pause");
            Assert.That(_screen.Offer.Count, Is.EqualTo(3));

            // Touche martelée tout de suite : ignorée (délai d'acceptation).
            yield return Tap(_keyboard.enterKey);
            Assert.That(_screen.IsOpen, Is.True, "pas de choix à l'aveugle");

            yield return WaitRealtime(0.6f);
            yield return Tap(_keyboard.dKey); // Q D en AZERTY = a d : vers la droite
            Assert.That(_screen.Selected, Is.EqualTo(1));
            Assert.That(_screen.Offer[1], Is.EqualTo(LevelBonus.Defense));
            yield return Tap(_keyboard.enterKey);
            Assert.That(_screen.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_player.Defense, Is.EqualTo(defense + 1));
            Assert.That(_player.Experience.Pending, Is.EqualTo(0));
            Assert.That(_player.Experience.Level, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator DeuxNiveauxDUnCoupDonnentDeuxChoixEtLaVieArrivePleine()
        {
            yield return LoadScene();
            var attack = _player.Attack;
            var max = _player.MaxHealth;
            var health = _player.Health;

            _player.GainExperience(25); // 10 puis 15
            yield return null;
            Assert.That(_player.Experience.Pending, Is.EqualTo(2));
            yield return WaitRealtime(0.6f);
            yield return Tap(_keyboard.digit1Key);
            Assert.That(_player.Attack, Is.EqualTo(attack + 1), "1 = attaque");
            Assert.That(_screen.IsOpen, Is.True, "le niveau 3 attend encore");

            yield return WaitRealtime(0.6f);
            yield return Tap(_keyboard.digit3Key);
            Assert.That(_screen.IsOpen, Is.False);
            Assert.That(_player.MaxHealth, Is.EqualTo(max + 2), "3 = vie");
            Assert.That(_player.Health, Is.EqualTo(Mathf.Min(max + 2, health + 2)), "les nouvelles cases arrivent pleines");
        }

        [UnityTest]
        public IEnumerator LaCarteNeSOuvrePasParDessus()
        {
            yield return LoadScene();
            var map = Object.FindAnyObjectByType<MapScreen>();
            _player.GainExperience(10);
            yield return null;
            yield return Tap(_keyboard.mKey); // virgule en AZERTY
            Assert.That(map.IsOpen, Is.False);
            Assert.That(_screen.IsOpen, Is.True);
        }
    }
}
