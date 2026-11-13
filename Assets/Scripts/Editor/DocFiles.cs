using System.IO;
using System.Text;
using UnityEngine;

namespace Deckscent.Editor
{
    /// <summary>
    /// Écriture des fichiers générés (captures, doc des prefabs) seulement s'ils ont changé :
    /// régénérer ne crée pas de diff git quand rien n'a bougé.
    /// </summary>
    public static class DocFiles
    {
        /// <summary>Écart toléré par canal (sur 255) : le bruit du GPU d'un rendu à l'autre.</summary>
        const int ChannelTolerance = 3;
        /// <summary>Part de pixels qui peuvent différer sans que l'image compte comme changée.</summary>
        const float ChangedPixelsTolerance = 0.001f;

        /// <summary>Écrit l'image si elle diffère vraiment de celle sur le disque. Vrai si écrite.</summary>
        public static bool SaveImageIfChanged(Texture2D image, string path)
        {
            if (File.Exists(path) && SameImage(image, path))
                return false;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllBytes(path, image.EncodeToPNG());
            return true;
        }

        /// <summary>Écrit le texte (UTF-8 sans BOM, fins de ligne LF) s'il diffère. Vrai si écrit.</summary>
        public static bool SaveTextIfChanged(string text, string path)
        {
            text = text.Replace("\r\n", "\n");
            if (File.Exists(path) && File.ReadAllText(path) == text)
                return false;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, text, new UTF8Encoding(false));
            return true;
        }

        static bool SameImage(Texture2D image, string path)
        {
            var old = new Texture2D(2, 2);
            try
            {
                if (!old.LoadImage(File.ReadAllBytes(path)) || old.width != image.width || old.height != image.height)
                    return false;
                var a = image.GetPixels32();
                var b = old.GetPixels32();
                var changed = 0;
                for (var i = 0; i < a.Length; i++)
                {
                    if (Mathf.Abs(a[i].r - b[i].r) > ChannelTolerance
                        || Mathf.Abs(a[i].g - b[i].g) > ChannelTolerance
                        || Mathf.Abs(a[i].b - b[i].b) > ChannelTolerance)
                        changed++;
                }
                return changed <= a.Length * ChangedPixelsTolerance;
            }
            finally
            {
                Object.DestroyImmediate(old);
            }
        }
    }
}
