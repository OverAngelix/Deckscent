using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Deckscent.Tests
{
    /// <summary>Mission de l'étage, logique pure : tirage, progression, fin.</summary>
    public class MissionTests
    {
        [Test]
        public void LeTirageDonneDesObjectifsDifferentsDansLeursFourchettes()
        {
            var settings = new MissionSettings();
            for (var seed = 1; seed <= 200; seed++)
            {
                var mission = Mission.Draw(new System.Random(seed), settings);
                Assert.That(mission.Goals.Count, Is.InRange(settings.minGoals, settings.maxGoals));
                Assert.That(mission.Goals.Select(g => g.Kind).Distinct().Count(), Is.EqualTo(mission.Goals.Count), "sortes toutes différentes");
                foreach (var goal in mission.Goals)
                {
                    var range = settings.RangeOf(goal.Kind);
                    Assert.That(goal.Target, Is.InRange(range.x, range.y), goal.Kind.ToString());
                    if (goal.Kind == GoalKind.Turns)
                        Assert.That(goal.Target % 10, Is.Zero, "des tours ronds");
                    Assert.That(goal.Progress, Is.Zero);
                }
            }
        }

        [Test]
        public void MoinsDObjectifsLesRendPlusDurs()
        {
            var settings = new MissionSettings { minGoals = 2, maxGoals = 3 };
            for (var seed = 1; seed <= 200; seed++)
            {
                var mission = Mission.Draw(new System.Random(seed), settings);
                foreach (var goal in mission.Goals)
                {
                    var range = settings.RangeOf(goal.Kind);
                    var middle = (range.x + range.y) / 2f;
                    if (mission.Goals.Count == 2)
                        Assert.That(goal.Target, Is.GreaterThanOrEqualTo(middle - 5f), "courte et dure : moitié haute");
                    else
                        Assert.That(goal.Target, Is.LessThanOrEqualTo(middle + 5f), "longue et facile : moitié basse");
                }
            }
        }

        [Test]
        public void LaMemeGraineDonneLaMemeMission()
        {
            var a = Mission.Draw(new System.Random(42), new MissionSettings());
            var b = Mission.Draw(new System.Random(42), new MissionSettings());
            Assert.That(a.Goals.Select(g => (g.Kind, g.Target)), Is.EqualTo(b.Goals.Select(g => (g.Kind, g.Target))));
        }

        [Test]
        public void LesObjectifsAvancentPuisLaMissionSeTermineUneFois()
        {
            var mission = new Mission(new[] { new MissionGoal(GoalKind.Monsters, 2), new MissionGoal(GoalKind.Turns, 30) });
            var done = new List<GoalKind>();
            var completed = 0;
            var version = mission.Version;
            mission.GoalDone += g => done.Add(g.Kind);
            mission.Completed += () => completed++;

            mission.Report(GoalKind.Chests);
            Assert.That(mission.Version, Is.EqualTo(version), "sorte absente : rien ne bouge");
            mission.Report(GoalKind.Monsters);
            Assert.That(mission.Goals[0].Progress, Is.EqualTo(1));
            Assert.That(mission.Version, Is.GreaterThan(version));
            mission.Report(GoalKind.Monsters, 5);
            Assert.That(mission.Goals[0].Progress, Is.EqualTo(2), "plafonné au compte visé");
            Assert.That(done, Is.EqualTo(new[] { GoalKind.Monsters }));
            Assert.That(mission.IsComplete, Is.False);

            mission.SetTurn(12);
            Assert.That(mission.Goals[1].Progress, Is.EqualTo(12));
            mission.SetTurn(31);
            Assert.That(mission.IsComplete, Is.True);
            Assert.That(completed, Is.EqualTo(1));
            mission.SetTurn(40);
            mission.Report(GoalKind.Monsters);
            Assert.That(completed, Is.EqualTo(1), "une seule fois");
            Assert.That(done, Is.EqualTo(new[] { GoalKind.Monsters, GoalKind.Turns }));
        }
    }
}
