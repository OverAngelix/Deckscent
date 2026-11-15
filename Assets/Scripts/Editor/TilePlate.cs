using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Deckscent.Editor
{
    /// <summary>
    /// Planche des tuiles (docs/images/tuiles.png, affichée dans docs/tuiles.md) : chaque forme
    /// vue de dessus, roche en gris, sol en blanc, murs fins des salles en rouge (portes d'une donne
    /// fixe), torches en orange. Menu Deckscent > Planche des tuiles. Le dessin (texte compris) est
    /// fait par Tools/planche_tuiles.ps1 (System.Drawing) d'après un export du plan de chaque forme.
    /// L'image n'est réécrite que si elle a changé.
    /// </summary>
    public static class TilePlate
    {
        const string OutputPath = "docs/images/tuiles.png";
        const string Script = "Tools/planche_tuiles.ps1";
        /// <summary>Graine des portes des salles sur la planche (fixe : la planche ne bouge pas sans raison).</summary>
        const int DoorSeed = 5;

        [MenuItem("Deckscent/Planche des tuiles")]
        public static void Generate()
        {
            var export = Path.Combine(Path.GetTempPath(), "deckscent_tuiles.txt");
            var png = Path.Combine(Path.GetTempPath(), "deckscent_tuiles.png");
            File.WriteAllText(export, Export(), new UTF8Encoding(false));

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{Path.GetFullPath(Script)}\" -In \"{export}\" -Out \"{png}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            });
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0 || !File.Exists(png))
            {
                Debug.LogError("Planche des tuiles : le dessin a échoué. " + error);
                return;
            }

            var image = new Texture2D(2, 2);
            image.LoadImage(File.ReadAllBytes(png));
            var changed = DocFiles.SaveImageIfChanged(image, OutputPath);
            Object.DestroyImmediate(image);
            Debug.Log("Planche des tuiles : " + (changed ? "mise à jour, " + OutputPath : "rien n'a changé."));
        }

        /// <summary>Pour chaque forme : « @id|nom|sorte|emplacements », ses lignes (« _ » hors tuile), puis « W » et ses murs fins « x,y,direction ».</summary>
        static string Export()
        {
            var sb = new StringBuilder();
            var rng = new System.Random(DoorSeed);
            foreach (var kv in TileCard.ShapeList)
            {
                var card = new TileCard(kv.Key);
                if (card.IsRoom)
                    card.RandomDoors(rng);
                var b = card.Bounds();
                sb.Append($"@{kv.Key}|{kv.Value.Name}|{(kv.Value.Room ? "salle" : "couloir")}|{card.Slots.Count}\n");
                foreach (var row in card.FullRows())
                    sb.Append(row.Replace(' ', '_')).Append('\n');
                sb.Append('W');
                foreach (var slot in card.Slots)
                    foreach (var (cell, dir) in card.EdgeWalls(slot))
                    {
                        var o = (slot - b.position) * 3 + cell;
                        sb.Append($" {o.x},{o.y},{System.Array.IndexOf(TileCard.Dirs, dir)}");
                    }
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
