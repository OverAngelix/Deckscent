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
    /// Chef d'orchestre de la musique du donjon (MusicDirector) dans la scène de test : départ en
    /// (6, 3) tourné vers l'est. Sur la ligne 3 : (3, 3) à (5, 3) sont en vue à l'ouest ; (8, 3) est
    /// de la roche, donc (9, 3) est caché.
    /// </summary>
    public class MusicTests : InputTestFixture
    {
        const string TestScene = "Assets/Scenes/Tests/Test_Labyrinthe.unity";
        static readonly Vector2Int Ahead = new Vector2Int(7, 3);

        Keyboard _keyboard;
        PlayerController _player;
        MusicDirector _director;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        IEnumerator LoadScene()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                TestScene, new LoadSceneParameters(LoadSceneMode.Single));
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<MusicDirector>("Assets/Prefabs/Audio/MusiqueDonjon.prefab");
            Assert.That(prefab, Is.Not.Null, "prefab de la musique introuvable");
            _director = Object.Instantiate(prefab);
#else
            yield return null;
#endif
            yield return null;
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(_player, Is.Not.Null);
        }

        static T Spawn<T>(string path, Vector2Int cell) where T : GridEntity
        {
#if UNITY_EDITOR
            var entity = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path));
            entity.Place(Object.FindAnyObjectByType<Maze>(), cell);
            return entity;
#else
            return null;
#endif
        }

        static Slime SpawnSlime(Vector2Int cell) => Spawn<Slime>("Assets/Prefabs/Enemies/Slime.prefab", cell);

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            yield return null;
            Release(key);
            var timeout = Time.realtimeSinceStartup + 4f;
            while (_player.IsBusy && Time.realtimeSinceStartup < timeout)
                yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaMusiqueSortSurLaPisteMusique()
        {
            yield return LoadScene();
            var source = _director.GetComponent<AudioSource>();
            Assert.That(source.outputAudioMixerGroup.name, Is.EqualTo("Musique"));
            Assert.That(source.isPlaying, Is.True);
            Assert.That(_director.GetComponent<DungeonMusic>().Synth, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator LaMenaceMonteAvecLesMonstresEnVue()
        {
            yield return LoadScene();
            _director.Evaluate();
            Assert.That(_director.Threat, Is.EqualTo(0f), "personne en vue");

            SpawnSlime(new Vector2Int(9, 3));
            _director.Evaluate();
            Assert.That(_director.VisibleMonsters, Is.EqualTo(0), "caché derrière la roche");

            SpawnSlime(new Vector2Int(3, 3));
            _director.Evaluate();
            Assert.That(_director.VisibleMonsters, Is.EqualTo(1), "en vue, même dans le dos");
            Assert.That(_director.Threat, Is.EqualTo(0.5f).Within(1e-4f));

            SpawnSlime(new Vector2Int(4, 3));
            _director.Evaluate();
            Assert.That(_director.Threat, Is.EqualTo(0.75f).Within(1e-4f), "plus de monstres, plus de menace");
        }

        [UnityTest]
        public IEnumerator LeCombatDureTantQuOnSeBatPuisSArrete()
        {
            yield return LoadScene();
            Assert.That(_director.InCombat, Is.False);
            var slime = SpawnSlime(Ahead);
            yield return Tap(_keyboard.wKey); // on frappe
            Assert.That(_director.InCombat, Is.True, "un coup donné lance le combat");
            Object.Destroy(slime.gameObject);
            yield return null;
            yield return Tap(_keyboard.enterKey);
            Assert.That(_director.InCombat, Is.True, "un seul tour calme : on reste en combat");
            yield return Tap(_keyboard.enterKey);
            Assert.That(_director.InCombat, Is.False, "2 tours sans coup : fini");

            _player.TakeDamage(1);
            Assert.That(_director.InCombat, Is.True, "un coup reçu aussi");
        }

        [UnityTest]
        public IEnumerator LeCoffreFaitVarierLaMusiqueSeulementSansMonstre()
        {
            yield return LoadScene();
            Spawn<Chest>("Assets/Prefabs/Props/Coffre.prefab", new Vector2Int(4, 3));
            _director.Evaluate();
            Assert.That(_director.ChestNear, Is.True, "coffre fermé à 2 cases, en vue");
            Assert.That(_director.ChestVariation, Is.True);

            SpawnSlime(new Vector2Int(3, 3));
            _director.Evaluate();
            Assert.That(_director.ChestVariation, Is.False, "un monstre en vue : pas de variation du coffre");
        }

        [UnityTest]
        public IEnumerator LeBossAllumeLaForteVariation()
        {
            yield return LoadScene();
            Assert.That(_director.Boss, Is.False);
            _director.BossMode = true;
            _director.Evaluate();
            Assert.That(_director.Boss, Is.True);
        }
    }
}
