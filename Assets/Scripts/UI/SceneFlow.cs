using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deckscent
{
    /// <summary>
    /// Passages d'une scène à l'autre : menu → camp → jeu, retour au camp (mort, remontée), retour
    /// au menu. Dans l'éditeur, les scènes sont chargées par leur chemin (les scènes de test ne sont
    /// pas dans le build) ; dans un build, par leur nom (Menu = scène 0, Camp = 1, Main = 2).
    /// </summary>
    public static class SceneFlow
    {
        public const string MenuScene = "Assets/Scenes/Menu.unity";
        public const string GameScene = "Assets/Scenes/Main.unity";
        public const string CampScene = "Assets/Scenes/Camp.unity";

        /// <summary>Le camp, à la surface (hub) : d'où part l'expédition, où l'on revient.</summary>
        public static void LoadCamp() => Load(CampScene);

        /// <summary>Nouvelle partie (scène Main).</summary>
        public static void LoadGame() => Load(GameScene);

        /// <summary>Retour à l'écran titre.</summary>
        public static void LoadMenu() => Load(MenuScene);

        /// <summary>Recharge la scène en cours (recommencer après la mort).</summary>
        public static void Reload() => Load(SceneManager.GetActiveScene().path);

        static void Load(string path)
        {
            Time.timeScale = 1f;
            AbandonRun();
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(System.IO.Path.GetFileNameWithoutExtension(path));
#endif
        }

        /// <summary>
        /// Quitter l'expédition en cours (menu, recommencer, bureau) compte comme une mort pour
        /// l'or : on n'en garde que la part de la mort (docs/or.md). Rien à faire s'il est déjà réglé.
        /// </summary>
        static void AbandonRun()
        {
            if (Gold.Carried > 0)
                Gold.EndOnDeath();
        }

        /// <summary>Ferme le jeu (arrête le Play mode dans l'éditeur).</summary>
        public static void Quit()
        {
            AbandonRun();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
