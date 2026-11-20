using NUnit.Framework;

namespace Deckscent.Tests
{
    /// <summary>XP et niveaux (logique pure) : paliers, plusieurs niveaux d'un coup, bienfaits proposés.</summary>
    public class ExperienceTests
    {
        static Experience Make() => new Experience(new ExperienceSettings { firstLevel = 10, perLevel = 5, choices = 3 });

        [Test]
        public void LesPaliersMontentDeCinqEnCinq()
        {
            var xp = Make();
            Assert.That(xp.NeededFor(1), Is.EqualTo(10));
            Assert.That(xp.NeededFor(2), Is.EqualTo(15));
            Assert.That(xp.NeededFor(3), Is.EqualTo(20));
        }

        [Test]
        public void UnNiveauAttendSonBienfait()
        {
            var xp = Make();
            Assert.That(xp.Gain(9), Is.EqualTo(0));
            Assert.That(xp.Level, Is.EqualTo(1));
            Assert.That(xp.Gain(3), Is.EqualTo(1));
            Assert.That(xp.Level, Is.EqualTo(2));
            Assert.That(xp.Xp, Is.EqualTo(2), "le surplus est gardé");
            Assert.That(xp.Pending, Is.EqualTo(1));
            xp.Consume();
            Assert.That(xp.Pending, Is.EqualTo(0));
        }

        [Test]
        public void UnGrosMonstrePeutDonnerPlusieursNiveaux()
        {
            var xp = Make();
            Assert.That(xp.Gain(27), Is.EqualTo(2), "10 puis 15");
            Assert.That(xp.Level, Is.EqualTo(3));
            Assert.That(xp.Xp, Is.EqualTo(2));
            Assert.That(xp.Pending, Is.EqualTo(2));
        }

        [Test]
        public void LesMonstresDursRapportentPlus()
        {
            Assert.That(Experience.RewardFor(3, 1), Is.EqualTo(4), "slime vert");
            Assert.That(Experience.RewardFor(6, 1), Is.GreaterThan(Experience.RewardFor(3, 1)), "slime rouge");
            Assert.That(Experience.RewardFor(0, 0), Is.EqualTo(1), "jamais rien");
        }

        [Test]
        public void LesBienfaitsProposesSontDistinctsEtRanges()
        {
            var xp = Make();
            var offer = xp.Offer(new System.Random(3));
            Assert.That(offer, Is.EqualTo(new[] { LevelBonus.Attack, LevelBonus.Defense, LevelBonus.Health }));

            var two = new Experience(new ExperienceSettings { choices = 2 }).Offer(new System.Random(5));
            Assert.That(two.Count, Is.EqualTo(2));
            Assert.That(two[0], Is.Not.EqualTo(two[1]));
        }
    }
}
