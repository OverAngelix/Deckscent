using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Deckscent.Tests
{
    /// <summary>
    /// Tour par tour, combat, dalle à pics et coffre dans la scène de test (plan figé, voir
    /// PrototypeBuilder.TestPlan), avec un clavier virtuel. Départ en (6, 3), tourné vers l'est ;
    /// (7, 3) devant, puis un couloir vers le sud par (7, 4), (7, 5).
    /// Les ennemis, dalles et coffres sont posés par le test (prefabs de Assets/Prefabs).
    /// </summary>
    public class CombatTests : InputTestFixture
    {
        const string TestScene = "Assets/Scenes/Tests/Test_Labyrinthe.unity";
        static readonly Vector2Int Ahead = new Vector2Int(7, 3);

        Keyboard _keyboard;
        PlayerController _player;
        readonly List<string> _messages = new List<string>();

        public override void Setup()
        {
            base.Setup();
            GameSettings.SetLocale("fr", false); // les messages vérifiés sont en français
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _messages.Clear();
        }

        IEnumerator LoadScene()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                TestScene, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null;
            _player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(_player, Is.Not.Null, "joueur introuvable dans la scène de test");
            _player.Notified += _messages.Add;
        }

        static T Spawn<T>(string path, Vector2Int cell) where T : GridEntity
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(prefab, Is.Not.Null, path + " introuvable");
            var entity = Object.Instantiate(prefab);
            entity.Place(Object.FindAnyObjectByType<Maze>(), cell);
            return entity;
#else
            return null;
#endif
        }

        static Slime SpawnSlime(Vector2Int cell, Slime.Variant variant = Slime.Variant.Vert)
        {
            var slime = Spawn<Slime>("Assets/Prefabs/Enemies/Slime.prefab", cell);
            slime.SetVariant(variant);
            return slime;
        }

        IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            yield return null;
            Release(key);
            var timeout = Time.realtimeSinceStartup + 4f;
            while (_player.IsBusy && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(_player.IsBusy, Is.False, "le joueur est resté occupé");
            yield return null;
        }

        static IEnumerator WaitSeconds(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
                yield return null;
        }

        [UnityTest]
        public IEnumerator AvancerVersUnSlimeLAttaqueEtIlRiposte()
        {
            yield return LoadScene();
            var slime = SpawnSlime(Ahead); // vert : 3 PV, 1 d'attaque
            yield return null;

            yield return Tap(_keyboard.wKey);
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(6, 3)), "on frappe sans avancer");
            Assert.That(slime.Health, Is.EqualTo(2));
            Assert.That(_player.Health, Is.EqualTo(9), "il riposte à son tour");
            Assert.That(_player.Turn, Is.EqualTo(1), "attaquer coûte un tour");

            yield return Tap(_keyboard.wKey);
            yield return Tap(_keyboard.wKey);
            Assert.That(slime.IsActive, Is.False, "mort au troisième coup");
            Assert.That(_player.Health, Is.EqualTo(8), "mort, il ne riposte plus");
            Assert.That(_player.Experience.Xp, Is.EqualTo(4), "XP : 3 PV max + 1 d'attaque");
            yield return WaitSeconds(0.6f);
            Assert.That(slime == null, "détruit après son animation");

            yield return Tap(_keyboard.wKey);
            Assert.That(_player.Cell, Is.EqualTo(Ahead), "sa case est libre");
        }

        [UnityTest]
        public IEnumerator UnSlimeSApprocheParLeCouloirPuisAttaque()
        {
            yield return LoadScene();
            var slime = SpawnSlime(new Vector2Int(7, 5));
            yield return Tap(_keyboard.enterKey);
            Assert.That(slime.Cell, Is.EqualTo(new Vector2Int(7, 4)));
            yield return Tap(_keyboard.enterKey);
            Assert.That(slime.Cell, Is.EqualTo(Ahead), "à côté du joueur, il ne monte pas sur sa case");
            Assert.That(_player.Health, Is.EqualTo(10));
            yield return Tap(_keyboard.enterKey);
            Assert.That(slime.Cell, Is.EqualTo(Ahead));
            Assert.That(_player.Health, Is.EqualTo(9));
        }

        [UnityTest]
        public IEnumerator LesSlimesBougentEnsembleSansSeMarcherDessusEtAttaquentUnParUn()
        {
            yield return LoadScene();
            // Deux slimes à la file dans le couloir du sud : (7, 5) puis (8, 5).
            var first = SpawnSlime(new Vector2Int(7, 5));
            var second = SpawnSlime(new Vector2Int(8, 5));
            yield return null;

            Press(_keyboard.enterKey);
            yield return null;
            yield return null;
            Release(_keyboard.enterKey);
            // Pendant les bonds, les deux sont en l'air en même temps.
            var bothMoving = false;
            var ground = first.transform.position.y;
            var timeout = Time.realtimeSinceStartup + 4f;
            while (_player.IsBusy && Time.realtimeSinceStartup < timeout)
            {
                if (first.Cell != new Vector2Int(7, 5) && second.Cell != new Vector2Int(8, 5)
                    && first.transform.position.y > ground + 0.05f && second.transform.position.y > ground + 0.05f)
                    bothMoving = true;
                yield return null;
            }
            Assert.That(bothMoving, Is.True, "les deux bondissent en même temps");
            Assert.That(first.Cell, Is.EqualTo(new Vector2Int(7, 4)));
            Assert.That(second.Cell, Is.EqualTo(new Vector2Int(7, 5)), "le second prend la case libérée");

            yield return Tap(_keyboard.enterKey);
            Assert.That(first.Cell, Is.EqualTo(Ahead));
            Assert.That(second.Cell, Is.EqualTo(new Vector2Int(7, 4)));
            Assert.That(_player.Health, Is.EqualTo(10), "qui bouge n'attaque pas");

            yield return Tap(_keyboard.enterKey);
            Assert.That(first.Cell, Is.Not.EqualTo(second.Cell));
            Assert.That(_player.Health, Is.EqualTo(9), "seul le premier est à côté");
        }

        [UnityTest]
        public IEnumerator LesPasLesCoupsEtLeSlurpSeFontEntendreSurLaPisteEffets()
        {
            yield return LoadScene();
            var sounds = _player.GetComponentInChildren<SoundEmitter>();
            Assert.That(sounds, Is.Not.Null, "le joueur a une source de sons");
            Assert.That(sounds.Source.outputAudioMixerGroup.name, Is.EqualTo("Effets"));

            // Un pas : un bruit de pas.
            yield return Tap(_keyboard.qKey); // tourné vers le nord…
            yield return Tap(_keyboard.eKey); // … puis de nouveau vers l'est (gratuit, sans bruit)
            Assert.That(sounds.PlayCount, Is.EqualTo(0), "tourner ne fait pas de bruit de pas");
            yield return Tap(_keyboard.wKey);
            Assert.That(System.Array.IndexOf(SoundBank.Steps, sounds.LastClip), Is.GreaterThanOrEqualTo(0), "un pas");
            yield return Tap(_keyboard.sKey); // retour au départ, à reculons
            Assert.That(sounds.PlayCount, Is.EqualTo(2), "un bruit par pas");

            // Un coup sur un slime : le whoosh, puis le slime riposte avec son slurp.
            var slime = SpawnSlime(Ahead);
            yield return null;
            var slimeSounds = slime.GetComponentInChildren<SoundEmitter>();
            Assert.That(slimeSounds, Is.Not.Null, "le slime a une source de sons");
            Assert.That(slimeSounds.Source.outputAudioMixerGroup.name, Is.EqualTo("Effets"));
            var before = sounds.PlayCount;
            yield return Tap(_keyboard.wKey);
            Assert.That(sounds.PlayCount, Is.EqualTo(before + 2), "notre attaque, puis le coup reçu");
            Assert.That(sounds.Recent[sounds.Recent.Count - 2], Is.SameAs(SoundBank.Attack), "notre attaque");
            Assert.That(slimeSounds.LastClip, Is.SameAs(SoundBank.Slurp), "sa riposte");
            Assert.That(sounds.LastClip, Is.SameAs(SoundBank.Hit), "et on prend le coup");
        }

        [UnityTest]
        public IEnumerator UnEnnemiDeCoteBloqueSansCouterDeTour()
        {
            yield return LoadScene();
            var slime = SpawnSlime(Ahead);
            yield return Tap(_keyboard.qKey); // A en AZERTY : tourné vers le nord
            yield return Tap(_keyboard.dKey); // pas à droite, vers l'est : le slime est là
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(6, 3)));
            Assert.That(_player.Turn, Is.EqualTo(0), "bump gratuit");
            Assert.That(slime.Health, Is.EqualTo(3), "on ne frappe que devant soi");
        }

        [UnityTest]
        public IEnumerator LeSlimeBleuFrappeFortEtLaDefenseNeTombeJamaisSousUn()
        {
            yield return LoadScene();
            SpawnSlime(Ahead, Slime.Variant.Bleu);
            yield return Tap(_keyboard.enterKey);
            Assert.That(_player.Health, Is.EqualTo(8), "bleu : 2 d'attaque");
            Assert.That(Combat.DamageAfterDefense(3, 1), Is.EqualTo(2));
            Assert.That(Combat.DamageAfterDefense(1, 5), Is.EqualTo(1), "au moins 1 par coup");
            Assert.That(Combat.DamageAfterDefense(0, 0), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator LaDalleAPicsBlesseUnTourSurTroisPuisEstFranchie()
        {
            yield return LoadScene();
            var trap = Spawn<SpikeTrap>("Assets/Prefabs/Traps/DallePics.prefab", Ahead);
            trap.PhaseOffset = 0;
            Assert.That(trap.StateFor(1), Is.EqualTo(SpikeTrap.State.Warning));
            Assert.That(trap.StateFor(2), Is.EqualTo(SpikeTrap.State.Up));
            Assert.That(trap.StateFor(3), Is.EqualTo(SpikeTrap.State.Down));

            var trapSounds = trap.GetComponentInChildren<SoundEmitter>();
            Assert.That(trapSounds, Is.Not.Null, "la dalle a une source de sons");
            Assert.That(trapSounds.Source.outputAudioMixerGroup.name, Is.EqualTo("Effets"));
            var playerSounds = _player.GetComponentInChildren<SoundEmitter>();

            yield return Tap(_keyboard.wKey); // tour 1 : sur la dalle, alerte
            Assert.That(_player.Cell, Is.EqualTo(Ahead), "la dalle ne bloque pas");
            Assert.That(trap.Current, Is.EqualTo(SpikeTrap.State.Warning));
            Assert.That(_player.Health, Is.EqualTo(10));
            Assert.That(trapSounds.PlayCount, Is.EqualTo(0), "l'alerte ne fait pas de bruit");

            yield return Tap(_keyboard.enterKey); // tour 2 : les pics sortent
            Assert.That(trap.Current, Is.EqualTo(SpikeTrap.State.Up));
            Assert.That(_player.Health, Is.EqualTo(8), "2 dégâts");
            Assert.That(trapSounds.LastClip, Is.SameAs(SoundBank.Spikes), "les pics qui sortent");
            Assert.That(playerSounds.LastClip, Is.SameAs(SoundBank.Hit), "le coup reçu");

            yield return Tap(_keyboard.sKey); // tour 3 : on ressort
            Assert.That(trap.IsCleared, Is.True);
            Assert.That(_messages, Is.Empty, "pas de message « Piège franchi »");
        }

        [UnityTest]
        public IEnumerator LaDalleBlesseAussiLesEnnemis()
        {
            yield return LoadScene();
            var trap = Spawn<SpikeTrap>("Assets/Prefabs/Traps/DallePics.prefab", Ahead);
            trap.PhaseOffset = 0;
            var slime = SpawnSlime(Ahead, Slime.Variant.Rouge); // 6 PV, sur la dalle, à côté du joueur
            yield return Tap(_keyboard.enterKey); // tour 1 : alerte, le slime frappe
            Assert.That(slime.Health, Is.EqualTo(6));
            yield return Tap(_keyboard.enterKey); // tour 2 : pics sortis sous le slime
            Assert.That(slime.Health, Is.EqualTo(4), "2 dégâts au slime");
            Assert.That(_player.Health, Is.EqualTo(8), "le joueur n'est pas sur la dalle : seulement les coups du slime");
        }

        [UnityTest]
        public IEnumerator AvancerVersUnCoffreLOuvreEtSoigne()
        {
            yield return LoadScene();
            var chest = Spawn<Chest>("Assets/Prefabs/Props/Coffre.prefab", Ahead);
            chest.CardChance = 0f; // pas de pioche ici : toujours des PV
            _player.TakeDamage(5);
            yield return Tap(_keyboard.wKey);
            Assert.That(chest.Opened, Is.True);
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(6, 3)), "on ouvre sans avancer");
            Assert.That(_player.Turn, Is.EqualTo(1), "ouvrir coûte un tour");
            Assert.That(_player.Health, Is.EqualTo(8), "+3 PV");
            Assert.That(_messages, Does.Contain("Coffre : +3 PV"));

            yield return WaitSeconds(1.5f);
            Assert.That(chest == null, "il s'enfonce puis disparaît");
            yield return Tap(_keyboard.wKey);
            Assert.That(_player.Cell, Is.EqualTo(Ahead));
        }

        [UnityTest]
        public IEnumerator InteractOuvreLeCoffreDroitDevant()
        {
            yield return LoadScene();
            var chest = Spawn<Chest>("Assets/Prefabs/Props/Coffre.prefab", Ahead);
            yield return Tap(_keyboard.fKey);
            Assert.That(chest.Opened, Is.True);
            Assert.That(_player.Turn, Is.EqualTo(1));
            Assert.That(_player.Health, Is.EqualTo(10), "PV au maximum, pas de pioche : soin sans effet");
        }

        [UnityTest]
        public IEnumerator UnSlimeSurUnCoffreSeFaitAttaquerEtLeCache()
        {
            yield return LoadScene();
            var chest = Spawn<Chest>("Assets/Prefabs/Props/Coffre.prefab", Ahead);
            var slime = SpawnSlime(Ahead);
            yield return null;
            Assert.That(chest.GetComponentInChildren<Renderer>().enabled, Is.False, "caché par le slime");
            yield return Tap(_keyboard.wKey);
            Assert.That(slime.Health, Is.EqualTo(2), "on frappe le slime");
            Assert.That(chest.Opened, Is.False);
        }

        [UnityTest]
        public IEnumerator ALaMortLeJoueurNeBougePlus()
        {
            yield return LoadScene();
            var died = 0;
            _player.Died += () => died++;
            _player.TakeDamage(25);
            Assert.That(_player.IsDead, Is.True);
            Assert.That(_player.Health, Is.EqualTo(0));
            Assert.That(died, Is.EqualTo(1));
            yield return Tap(_keyboard.wKey);
            Assert.That(_player.Cell, Is.EqualTo(new Vector2Int(6, 3)));
            Assert.That(_player.Turn, Is.EqualTo(0));
            var hud = Object.FindAnyObjectByType<Hud>();
            Assert.That(hud, Is.Not.Null, "HUD dans la scène de test");
            Assert.That(hud.DeadTime, Is.GreaterThanOrEqualTo(0f), "écran de mort affiché");
        }

        [UnityTest]
        public IEnumerator EchapSurLEcranDeMortRevientAuMenu()
        {
            yield return LoadScene();
            _player.TakeDamage(25);
            yield return WaitSeconds(1.1f); // délai avant d'accepter une touche
            Press(_keyboard.escapeKey);
            yield return null;
            Release(_keyboard.escapeKey);
            var timeout = Time.realtimeSinceStartup + 5f;
            while (SceneManager.GetActiveScene().name != "Menu" && Time.realtimeSinceStartup < timeout)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Menu"));
            Assert.That(Object.FindAnyObjectByType<MenuScreen>(), Is.Not.Null);
        }
    }
}
