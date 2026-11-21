using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Deckscent.Tests
{
    /// <summary>L'or de l'expédition et sa mise à l'abri (docs/or.md), sur un fichier de progression temporaire.</summary>
    public class GoldTests
    {
        string _savedPath, _testPath;

        [SetUp]
        public void SetUp()
        {
            _savedPath = Progression.FilePath;
            _testPath = Path.Combine(Application.temporaryCachePath, "progression_goldtests.json");
            Progression.FilePath = _testPath;
            Progression.Reset();
            Gold.ResetRun();
            Gold.KeptShare = 0.1f;
        }

        [TearDown]
        public void TearDown()
        {
            Gold.ResetRun();
            Progression.FilePath = _savedPath;
            if (File.Exists(_testPath))
                File.Delete(_testPath);
        }

        [Test]
        public void ALaMortOnGardeDixPourCentArrondiEnDessous()
        {
            Assert.That(Gold.KeptOnDeath(37, 0.1f), Is.EqualTo(3));
            Assert.That(Gold.KeptOnDeath(40, 0.1f), Is.EqualTo(4));
            Assert.That(Gold.KeptOnDeath(9, 0.1f), Is.EqualTo(0));
            Assert.That(Gold.KeptOnDeath(-5, 0.1f), Is.EqualTo(0));
        }

        [Test]
        public void LaMortMetLaPartGardeeALAbriEtPerdLeReste()
        {
            Gold.Add(25);
            Gold.Add(12);
            Assert.That(Gold.Carried, Is.EqualTo(37));
            Assert.That(Gold.EndOnDeath(), Is.EqualTo(3));
            Assert.That(Gold.Carried, Is.EqualTo(0));
            Assert.That(Gold.LastLost, Is.EqualTo(37));
            Assert.That(Gold.LastKept, Is.EqualTo(3));
            Assert.That(Progression.Gold, Is.EqualTo(3));
        }

        [Test]
        public void EnRemontantOnGardeTout()
        {
            Gold.Add(37);
            Assert.That(Gold.EndOnReturn(), Is.EqualTo(37));
            Assert.That(Progression.Gold, Is.EqualTo(37));
        }

        [Test]
        public void LOrMisALAbriEstReluDuFichier()
        {
            Progression.AddGold(42);
            Progression.FilePath = _testPath; // oublie l'état en mémoire, relit le fichier
            Assert.That(Progression.Gold, Is.EqualTo(42));
            Assert.That(Progression.SpendGold(50), Is.False, "pas assez d'or : rien n'est retiré");
            Assert.That(Progression.SpendGold(40), Is.True);
            Assert.That(Progression.Gold, Is.EqualTo(2));
        }

        [Test]
        public void UnGainEstSignale()
        {
            var total = 0;
            void OnGained(int amount) => total += amount;
            Gold.Gained += OnGained;
            try
            {
                Gold.Add(5);
                Gold.Add(0);
                Gold.Add(3);
            }
            finally
            {
                Gold.Gained -= OnGained;
            }
            Assert.That(total, Is.EqualTo(8));
        }
    }
}
