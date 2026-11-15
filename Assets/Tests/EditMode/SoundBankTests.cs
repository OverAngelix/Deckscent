using NUnit.Framework;

namespace Deckscent.Tests
{
    /// <summary>Les effets sonores synthétisés existent, ont une durée raisonnable, et ne sont ni muets ni saturés.</summary>
    public class SoundBankTests
    {
        [Test]
        public void LesSonsSontFabriques()
        {
            Assert.That(SoundBank.Steps.Length, Is.EqualTo(4), "quatre variantes de pas");
            foreach (var clip in new[] { SoundBank.Steps[0], SoundBank.Steps[3], SoundBank.Attack, SoundBank.Slurp, SoundBank.Spikes, SoundBank.Hit })
            {
                Assert.That(clip, Is.Not.Null);
                Assert.That(clip.length, Is.InRange(0.1f, 0.6f), clip.name + " : court");
                Assert.That(SoundBank.Peak(clip), Is.InRange(0.5f, 0.95f), clip.name + " : ni muet ni saturé");
            }
        }

        [Test]
        public void LesSonsSontGardes()
        {
            Assert.That(SoundBank.Slurp, Is.SameAs(SoundBank.Slurp), "fabriqué une seule fois");
            Assert.That(SoundBank.Steps[0], Is.Not.SameAs(SoundBank.Steps[1]), "des pas différents");
        }
    }
}
