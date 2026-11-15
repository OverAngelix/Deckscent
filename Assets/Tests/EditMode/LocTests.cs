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
        public void EnFrancaisLaCleDonneLeTexteFrancais()
        {
            GameSettings.SetLocale("fr", false);
            Assert.That(Loc.T("menu.descendre"), Is.EqualTo("Descendre"));
            Assert.That(Loc.F("hud.tour", 3), Is.EqualTo("Tour 3"));
        }

        [Test]
        public void EnAnglaisLaCleEstTraduite()
        {
            GameSettings.SetLocale("en", false);
            Assert.That(Loc.T("menu.descendre"), Is.EqualTo("Descend"));
            Assert.That(Loc.F("message.coffre_pv", 3), Is.EqualTo("Chest: +3 HP"));
            Assert.That(Loc.In("fr", "menu.descendre"), Is.EqualTo("Descendre"), "une langue donnée, sans changer l'option");
        }

        [Test]
        public void UneCleManquanteRetombeSurLeFrancaisPuisSurLaCle()
        {
            GameSettings.SetLocale("en", false);
            const string key = "test.seulement_en_francais";
            Loc.Fr[key] = "Seulement en français";
            try
            {
                Assert.That(Loc.T(key), Is.EqualTo("Seulement en français"), "absente en anglais : le français");
            }
            finally
            {
                Loc.Fr.Remove(key);
            }
            Assert.That(Loc.T("test.cle_inconnue"), Is.EqualTo("test.cle_inconnue"), "absente partout : la clé, visible à l'écran");
        }

        [Test]
        public void LesDeuxLanguesOntLesMemesCles()
        {
            foreach (var key in Loc.Fr.Keys)
                Assert.That(Loc.En.ContainsKey(key), Is.True, "manque en anglais : " + key);
            foreach (var key in Loc.En.Keys)
                Assert.That(Loc.Fr.ContainsKey(key), Is.True, "manque en français : " + key);
        }

        [Test]
        public void LesClesSontBienFormees()
        {
            var pattern = new System.Text.RegularExpressions.Regex(@"^[a-z]+(\.[a-z0-9_]+)+$");
            foreach (var key in Loc.Fr.Keys)
                Assert.That(pattern.IsMatch(key), Is.True, "« domaine.nom », minuscules sans accents : " + key);
        }

        [Test]
        public void ToutesLesFormesDeTuilesEtLesRefusOntLeurTexte()
        {
            foreach (var shape in TileCard.ShapeList)
                Assert.That(Loc.Fr.ContainsKey(shape.Value.NameKey), Is.True, "forme : " + shape.Key);
            foreach (var reason in new[] { "refus.hors_plateau", "refus.occupe", "refus.isole", "refus.sans_passage" })
                Assert.That(Loc.Fr.ContainsKey(reason), Is.True, "refus : " + reason);
        }

        /// <summary>Toute clé écrite dans le code (Loc.T("…") ou un texte « domaine.nom » connu) existe.</summary>
        [Test]
        public void LesClesDuCodeExistent()
        {
            var call = new System.Text.RegularExpressions.Regex(@"Loc\.[TF]\(""([^""]+)""");
            var literal = new System.Text.RegularExpressions.Regex(
                @"""((?:menu|options|commandes|carte|biome|legende|refus|forme|hud|message)\.[a-z0-9_.]+)""");
            var root = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Scripts");
            var checkedKeys = 0;
            foreach (var file in System.IO.Directory.GetFiles(root, "*.cs", System.IO.SearchOption.AllDirectories))
            {
                if (file.EndsWith("Loc.cs"))
                    continue;
                var code = System.IO.File.ReadAllText(file);
                foreach (System.Text.RegularExpressions.Match m in call.Matches(code))
                {
                    Assert.That(Loc.Fr.ContainsKey(m.Groups[1].Value), Is.True, $"{System.IO.Path.GetFileName(file)} : clé inconnue {m.Groups[1].Value}");
                    checkedKeys++;
                }
                foreach (System.Text.RegularExpressions.Match m in literal.Matches(code))
                {
                    Assert.That(Loc.Fr.ContainsKey(m.Groups[1].Value), Is.True, $"{System.IO.Path.GetFileName(file)} : clé inconnue {m.Groups[1].Value}");
                    checkedKeys++;
                }
            }
            Assert.That(checkedKeys, Is.GreaterThan(40), "le code a bien été parcouru");
        }

        [Test]
        public void LesTextesAvecValeursGardentLeursValeursEnAnglais()
        {
            foreach (var pair in Loc.Fr)
                for (var i = 0; i < 3; i++)
                {
                    var mark = "{" + i + "}";
                    Assert.That(Loc.En[pair.Key].Contains(mark), Is.EqualTo(pair.Value.Contains(mark)), pair.Key);
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
