using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Deckscent.Tests
{
    /// <summary>
    /// Réglages communs à tous les tests PlayMode : l'écran de niveau ne s'ouvre pas tout seul (un
    /// monstre tué dans un test de combat figerait le jeu). <see cref="LevelUpTests"/> le rallume.
    /// La progression (or mis à l'abri) va dans un fichier temporaire : les tests ne touchent pas à
    /// la vraie sauvegarde.
    /// </summary>
    [SetUpFixture]
    public class TestsSetup
    {
        string _savedPath;

        [OneTimeSetUp]
        public void Before()
        {
            LevelUpScreen.AutoOpen = false;
            _savedPath = Progression.FilePath;
            Progression.FilePath = Path.Combine(Application.temporaryCachePath, "progression_tests.json");
            Progression.Reset();
        }

        [OneTimeTearDown]
        public void After()
        {
            LevelUpScreen.AutoOpen = true;
            var testFile = Progression.FilePath;
            Progression.FilePath = _savedPath;
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }
}
