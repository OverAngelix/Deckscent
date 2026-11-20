using NUnit.Framework;

namespace Deckscent.Tests
{
    /// <summary>
    /// Réglages communs à tous les tests PlayMode : l'écran de niveau ne s'ouvre pas tout seul (un
    /// monstre tué dans un test de combat figerait le jeu). <see cref="LevelUpTests"/> le rallume.
    /// </summary>
    [SetUpFixture]
    public class TestsSetup
    {
        [OneTimeSetUp]
        public void Before() => LevelUpScreen.AutoOpen = false;

        [OneTimeTearDown]
        public void After() => LevelUpScreen.AutoOpen = true;
    }
}
