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
    /// Le camp à la surface (docs/camp.md) : on s'y déplace case par case, le feu de camp bloque,
    /// et avancer dans la grotte lance l'expédition. Clavier virtuel (InputTestFixture).
    /// </summary>
    public class CampTests : InputTestFixture
    {
        Keyboard _keyboard;
        PlayerController _player;
        Maze _maze;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            DungeonEntrance.LoadsGame = false;
        }

        public override void TearDown()
        {
            DungeonEntrance.LoadsGame = true;
            base.TearDown();
        }

        IEnumerator LoadCamp()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                SceneFlow.CampScene, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null;
            yield return null; // Start() du joueur et des obstacles
            _player = Object.FindAnyObjectByType<PlayerController>();
            _maze = Object.FindAnyObjectByType<Maze>();
            Assert.That(_player, Is.Not.Null);
        }

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            yield return null;
            Release(key);
            var timeout = Time.realtimeSinceStartup + 3f;
            while (_player.IsBusy && Time.realtimeSinceStartup < timeout)
                yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator LeCampAUnCielEtLeFeuBloqueLePassage()
        {
            yield return LoadCamp();
            Assert.That(Shader.GetGlobalFloat("_SkyOn"), Is.EqualTo(1f), "ciel gravé allumé");
            Assert.That(_player.Facing, Is.EqualTo(0), "le joueur regarde vers la grotte (nord)");
            var start = _player.Cell;
            yield return Tap(_keyboard.wKey); // une case vers le nord
            Assert.That(_player.Cell, Is.EqualTo(start + new Vector2Int(0, -1)));
            yield return Tap(_keyboard.wKey); // le feu de camp est devant : on ne passe pas
            Assert.That(_player.Cell, Is.EqualTo(start + new Vector2Int(0, -1)), "le feu bloque");
        }

        [UnityTest]
        public IEnumerator AvancerDansLaGrotteLanceLExpedition()
        {
            yield return LoadCamp();
            var entrance = Object.FindAnyObjectByType<DungeonEntrance>();
            Assert.That(entrance, Is.Not.Null);
            // Juste devant la grotte, tourné vers elle.
            var front = entrance.Cell + new Vector2Int(0, 1);
            typeof(PlayerController).GetProperty("Cell").SetValue(_player, front);
            _player.transform.position = _maze.CellToWorld(front);
            var confirm = Object.FindAnyObjectByType<ConfirmScreen>();

            // Une confirmation d'abord ; Échap : on reste au camp.
            var run = _player.StartCoroutine(entrance.Interact(_player));
            yield return null;
            Assert.That(confirm.IsOpen, Is.True, "« Descendre dans le donjon ? »");
            Assert.That(Time.timeScale, Is.Zero, "le jeu attend la réponse");
            confirm.Answer(false);
            yield return run;
            Assert.That(entrance.Entered, Is.False, "on reste au camp");
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            // Avancer dans la grotte, puis « Descendre ».
            Press(_keyboard.wKey);
            yield return null;
            yield return null;
            Release(_keyboard.wKey);
            var timeout = Time.realtimeSinceStartup + 2f;
            while (!confirm.IsOpen && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(confirm.IsOpen, Is.True, "avancer dans la grotte demande confirmation");
            confirm.Answer(true);
            yield return null;
            yield return null;
            Assert.That(entrance.Entered, Is.True, "on entre dans le donjon");
            Assert.That(_player.Cell, Is.EqualTo(front), "on n'avance pas sur la case de la grotte");
        }

        [UnityTest]
        public IEnumerator EnQuittantLeCampLeDonjonRetrouveSonNoir()
        {
            yield return LoadCamp();
            Assert.That(Shader.GetGlobalFloat("_SkyOn"), Is.EqualTo(1f));
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/Tests/Test_Labyrinthe.unity", new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;
            Assert.That(Shader.GetGlobalFloat("_SkyOn"), Is.Zero, "pas de ciel dans le donjon");
            Assert.That(Shader.GetGlobalFloat("_DarkScale"), Is.Zero, "l'obscurité du donjon revient");
        }
    }
}
