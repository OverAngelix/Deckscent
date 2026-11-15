using NUnit.Framework;

namespace Deckscent.Tests
{
    /// <summary>Traduction (Loc) et options (GameSettings), sans rien sauvegarder.</summary>
    public class LocTests
    {
        string _locale;
        float _volume, _music, _effects;

        [SetUp]
        public void Remember()
        {
            _locale = GameSettings.Locale;
            _volume = GameSettings.Volume;
            _music = GameSettings.MusicVolume;
            _effects = GameSettings.EffectsVolume;
        }

        [TearDown]
        public void Restore()
        {
            GameSettings.SetLocale(_locale, false);
            GameSettings.SetVolume(_volume, false);
            GameSettings.SetMusicVolume(_music, false);
            GameSettings.SetEffectsVolume(_effects, false);
        }

        [Test]
        public void EnFrancaisLeTexteResteLeMeme()
        {
            GameSettings.SetLocale("fr", false);
            Assert.That(Loc.T("Descendre"), Is.EqualTo("Descendre"));
            Assert.That(Loc.F("Tour {0}", 3), Is.EqualTo("Tour 3"));
        }

        [Test]
        public void EnAnglaisLeTexteEstTraduit()
        {
            GameSettings.SetLocale("en", false);
            Assert.That(Loc.T("Descendre"), Is.EqualTo("Descend"));
            Assert.That(Loc.F("Coffre : +{0} PV", 3), Is.EqualTo("Chest: +3 HP"));
            Assert.That(Loc.T("Un texte sans traduction"), Is.EqualTo("Un texte sans traduction"), "reste en français");
        }

        [Test]
        public void ToutesLesFormesDeTuilesEtLesRefusSontTraduits()
        {
            foreach (var shape in TileCard.ShapeList)
                Assert.That(Loc.En.ContainsKey(shape.Value.Name), Is.True, "forme : " + shape.Value.Name);
            foreach (var reason in new[] { "Hors du plateau", "Emplacement déjà occupé", "Doit toucher le labyrinthe", "Doit se raccorder par un passage" })
                Assert.That(Loc.En.ContainsKey(reason), Is.True, "refus : " + reason);
        }

        [Test]
        public void LesTextesAvecValeursGardentLeursValeursEnAnglais()
        {
            foreach (var pair in Loc.En)
                for (var i = 0; i < 3; i++)
                {
                    var mark = "{" + i + "}";
                    Assert.That(pair.Value.Contains(mark), Is.EqualTo(pair.Key.Contains(mark)), pair.Key);
                }
        }

        [Test]
        public void LeVolumeEstBorneEtArrondi()
        {
            GameSettings.SetVolume(1.7f, false);
            Assert.That(GameSettings.Volume, Is.EqualTo(1f));
            GameSettings.SetVolume(-0.2f, false);
            Assert.That(GameSettings.Volume, Is.EqualTo(0f));
            GameSettings.SetVolume(0.43f, false);
            Assert.That(GameSettings.Volume, Is.EqualTo(0.45f).Within(1e-5f));
        }

        [Test]
        public void LaMusiqueEtLesEffetsOntLeurPropreVolume()
        {
            GameSettings.SetVolume(0.5f, false);
            GameSettings.SetMusicVolume(1.7f, false);
            GameSettings.SetEffectsVolume(0.33f, false);
            Assert.That(GameSettings.MusicVolume, Is.EqualTo(1f), "borné à 1");
            Assert.That(GameSettings.EffectsVolume, Is.EqualTo(0.35f).Within(1e-5f), "arrondi au vingtième");
            Assert.That(GameSettings.Volume, Is.EqualTo(0.5f), "le volume général ne bouge pas");
        }

        [Test]
        public void UneLangueInconnueEstIgnoree()
        {
            GameSettings.SetLocale("fr", false);
            GameSettings.SetLocale("de", false);
            Assert.That(GameSettings.Locale, Is.EqualTo("fr"));
        }
    }
}
