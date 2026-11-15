using NUnit.Framework;

namespace Deckscent.Tests
{
    /// <summary>
    /// Musique du donjon, rendue hors Play mode : les couches suivent la situation, les entrées sont
    /// calées sur le rythme, et le son reste sain (pas de NaN, pas de saturation).
    /// </summary>
    public class AdventureSynthTests
    {
        const int Rate = 48000;

        static AdventureSynth NewSynth()
        {
            var synth = new AdventureSynth(Rate, 5);
            synth.FadeTo(1f, 0.01f);
            return synth;
        }

        /// <summary>Rend `seconds` secondes ; renvoie (efficace, crête), et faux dans `sane` en cas de NaN.</summary>
        static (float rms, float peak) Render(AdventureSynth synth, float seconds, out bool sane)
        {
            var buffer = new float[1024];
            var blocks = (int)(Rate * seconds * 2 / buffer.Length);
            double sum = 0;
            float peak = 0f;
            sane = true;
            for (var b = 0; b < blocks; b++)
            {
                synth.Render(buffer, 2);
                foreach (var v in buffer)
                {
                    if (float.IsNaN(v) || float.IsInfinity(v))
                        sane = false;
                    sum += v * v;
                    peak = System.Math.Max(peak, System.Math.Abs(v));
                }
            }
            return ((float)System.Math.Sqrt(sum / (blocks * buffer.Length)), peak);
        }

        [Test]
        public void LExplorationJoueSeuleAuDepart()
        {
            var synth = NewSynth();
            var (rms, _) = Render(synth, 4f, out _);
            Assert.That(rms, Is.GreaterThan(0.03f), "on entend la base");
            Assert.That(synth.FluteNotes, Is.GreaterThan(0), "la mélodie de flûte");
            Assert.That(synth.DrumHits, Is.EqualTo(0), "pas de tambours de combat");
            Assert.That(synth.GlockNotes, Is.EqualTo(0), "pas de scintillement de coffre");
            Assert.That(synth.LayerGain(AdventureSynth.Layer.Threat), Is.EqualTo(0f));
        }

        [Test]
        public void LeCombatEntreALaCrochePuisRepart()
        {
            var synth = NewSynth();
            Render(synth, 1f, out _);
            synth.SetSituation(0.5f, true, false, false);
            Render(synth, 0.5f, out _);
            Assert.That(synth.CombatOn, Is.True, "entré en moins de 0,3 s");
            Assert.That(synth.LayerGain(AdventureSynth.Layer.Combat), Is.GreaterThan(0.9f), "monte vite");
            Render(synth, 1f, out _);
            Assert.That(synth.DrumHits, Is.GreaterThan(4), "les tambours jouent");
            synth.SetSituation(0f, false, false, false);
            Render(synth, 2f, out _);
            Assert.That(synth.LayerGain(AdventureSynth.Layer.Combat), Is.EqualTo(0f), "redescend en moins de 2 s");
        }

        [Test]
        public void LaMenaceMonteAvecSaCible()
        {
            var synth = NewSynth();
            synth.SetSituation(0.75f, false, false, false);
            Render(synth, 1.5f, out _);
            Assert.That(synth.LayerGain(AdventureSynth.Layer.Threat), Is.EqualTo(0.75f).Within(0.01f), "monte en moins d'une seconde");
        }

        [Test]
        public void LaMenaceEstUnEntreDeuxQuiMonteAvecLeNombreDeMonstres()
        {
            var synth = NewSynth();
            var (calm, _) = Render(synth, 4f, out _);
            synth.SetSituation(0.5f, false, false, false); // un monstre en vue
            var (one, _) = Render(synth, 4f, out _);
            Assert.That(synth.StringNotes, Is.GreaterThan(10), "les cordes en ostinato jouent");
            Assert.That(synth.MuffledHits, Is.EqualTo(0), "pas de tambour feutré pour un seul monstre");
            Assert.That(synth.DrumHits, Is.EqualTo(0), "ni de tambours de combat");
            synth.SetSituation(1f, false, false, false); // plusieurs monstres
            Render(synth, 4f, out _);
            Assert.That(synth.MuffledHits, Is.GreaterThan(0), "le tambour feutré à partir de 2 monstres");
            Assert.That(one, Is.GreaterThan(calm), "la menace s'ajoute au thème");
        }

        [Test]
        public void LeCoffreScintilleSeulementQuandIlEstProche()
        {
            var synth = NewSynth();
            Render(synth, 3f, out _);
            Assert.That(synth.GlockNotes, Is.EqualTo(0));
            synth.SetSituation(0f, false, true, false);
            Render(synth, 0.7f, out _);
            Assert.That(synth.GlockNotes, Is.GreaterThan(0), "entre au temps suivant");
            Render(synth, 4f, out _);
            Assert.That(synth.GlockNotes, Is.GreaterThan(5));
        }

        [Test]
        public void LeBossArriveALaMesureSuivanteAvecUneCymbale()
        {
            var synth = NewSynth();
            Render(synth, 0.3f, out _);
            synth.SetSituation(0.5f, true, false, true);
            Render(synth, 0.3f, out _);
            Assert.That(synth.InBoss, Is.False, "attend la mesure suivante (une mesure = 2,4 s à 100)");
            Render(synth, 2.5f, out _);
            Assert.That(synth.InBoss, Is.True);
            Assert.That(synth.Stingers, Is.EqualTo(1), "un coup de cymbale à l'entrée");
            Render(synth, 3f, out _);
            Assert.That(synth.HornNotes, Is.GreaterThan(0), "le cor du boss");
        }

        [Test]
        public void LeSonResteSainDansToutesLesSituations()
        {
            var synth = NewSynth();
            synth.SetSituation(1f, true, true, true);
            var (rms, peak) = Render(synth, 10f, out var sane);
            Assert.That(sane, Is.True, "ni NaN ni infini");
            Assert.That(peak, Is.LessThan(1f), "pas de saturation");
            Assert.That(rms, Is.InRange(0.05f, 0.5f));
        }

        [Test]
        public void LeFonduDeSortieMetLeMorceauEnSilence()
        {
            var synth = NewSynth();
            Render(synth, 1f, out _);
            synth.FadeTo(0f, 0.5f);
            Render(synth, 1f, out _);
            var (rms, _) = Render(synth, 1f, out _);
            Assert.That(rms, Is.LessThan(1e-4f));
        }
    }
}
