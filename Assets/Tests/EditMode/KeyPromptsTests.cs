using NUnit.Framework;

namespace Deckscent.Tests
{
    /// <summary>Touches affichées (KeyPrompts) : lues dans InputSystem_Actions, pour le clavier ou la manette.</summary>
    public class KeyPromptsTests
    {
        string _locale;
        DeviceFamily _family;

        [SetUp]
        public void Remember()
        {
            _locale = GameSettings.Locale;
            _family = ControlDevice.Current;
            GameSettings.SetLocale("fr", false);
        }

        [TearDown]
        public void Restore()
        {
            GameSettings.SetLocale(_locale, false);
            ControlDevice.Set(_family);
        }

        [Test]
        public void ALaManetteLesBoutonsDeviennentDesPictos()
        {
            Assert.That(KeyPrompts.Describe(KeyPrompts.Of("Player/Interact", true)), Is.EqualTo("South"));
            Assert.That(KeyPrompts.Describe(KeyPrompts.Of("Player/Map", true)), Is.EqualTo("North"));
            Assert.That(KeyPrompts.Describe(KeyPrompts.Of("Map/Discard", true)), Is.EqualTo("Select"));
            Assert.That(KeyPrompts.Describe(KeyPrompts.Of("Player/Turn", true, "negative", "positive")), Is.EqualTo("LeftShoulder RightShoulder"));
        }

        [Test]
        public void SansCompositeALaManetteLeStickEtLaCroixPortentLeSens()
        {
            var keys = KeyPrompts.Of("Player/Move", true, "up", "down");
            Assert.That(KeyPrompts.Describe(keys), Is.EqualTo("LeftStick(Vertical) / Dpad(Vertical)"));
        }

        [Test]
        public void AuClavierOnLitLeNomDeLaToucheTraduit()
        {
            Assert.That(KeyPrompts.Describe(KeyPrompts.Of("Map/Discard", false)), Is.EqualTo("Suppr"));
            Assert.That(KeyPrompts.Describe(KeyPrompts.Of("Player/Wait", false)), Is.EqualTo("Entrée"), "la première touche seulement");
            Assert.That(KeyPrompts.Of("Player/Move", false, "up", "down"), Has.Length.EqualTo(2));
            GameSettings.SetLocale("en", false);
            Assert.That(KeyPrompts.Describe(KeyPrompts.Of("Map/Discard", false)), Is.EqualTo("Del"));
        }

        [Test]
        public void LesTouchesSuiventLAppareilEnCours()
        {
            ControlDevice.Set(DeviceFamily.PlayStation);
            Assert.That(KeyPrompts.Describe(KeyPrompts.Confirm()), Is.EqualTo("South"));
            Assert.That(KeyPrompts.Describe(KeyPrompts.Of("Map/Close")), Is.EqualTo("East"));
            ControlDevice.Set(DeviceFamily.KeyboardMouse);
            Assert.That(KeyPrompts.Describe(KeyPrompts.Confirm()), Is.EqualTo("Entrée"));
            Assert.That(KeyPrompts.Describe(KeyPrompts.Of("Map/Close")), Is.EqualTo("Échap"));
        }

        [Test]
        public void LesLettresDeSwitchSontInverseesParRapportAXbox()
        {
            Assert.That(KeyPrompts.FaceLetter(PadGlyph.South, DeviceFamily.Xbox), Is.EqualTo("A"));
            Assert.That(KeyPrompts.FaceLetter(PadGlyph.East, DeviceFamily.Xbox), Is.EqualTo("B"));
            Assert.That(KeyPrompts.FaceLetter(PadGlyph.South, DeviceFamily.Switch), Is.EqualTo("B"));
            Assert.That(KeyPrompts.FaceLetter(PadGlyph.East, DeviceFamily.Switch), Is.EqualTo("A"));
        }

        [Test]
        public void LesCheminsDeManetteDonnentLeBonPicto()
        {
            Assert.That(KeyPrompts.GlyphOf("buttonSouth"), Is.EqualTo(PadGlyph.South));
            Assert.That(KeyPrompts.GlyphOf("leftStick/up"), Is.EqualTo(PadGlyph.LeftStick));
            Assert.That(KeyPrompts.GlyphOf("leftStickPress"), Is.EqualTo(PadGlyph.LeftStickPress));
            Assert.That(KeyPrompts.GlyphOf("dpad"), Is.EqualTo(PadGlyph.Dpad));
            Assert.That(KeyPrompts.GlyphOf("inconnu"), Is.EqualTo(PadGlyph.None));
        }
    }
}
