using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Deckscent
{
    /// <summary>
    /// Textes de l'UI gravure, dessinés « à la main » comme les traits d'<see cref="InkCanvas"/> :
    /// <see cref="Begin"/>, des appels à <see cref="Label"/>, puis <see cref="End"/>. Les objets
    /// Text sont réutilisés d'une image à l'autre. Coordonnées : origine en haut à gauche, y vers
    /// le bas. Police à empattements cherchée dans le système (rien à fournir).
    /// </summary>
    public class TextPool : MonoBehaviour
    {
        /// <summary>Polices cherchées dans le système, dans l'ordre (une police à empattements va mieux avec la gravure).</summary>
        public static readonly string[] FontNames = { "Garamond", "Book Antiqua", "Palatino Linotype", "Georgia", "Times New Roman" };

        static Font _font;
        readonly List<Text> _texts = new List<Text>();
        int _used;

        public static Font SerifFont
        {
            get
            {
                if (_font == null)
                    _font = Font.CreateDynamicFontFromOSFont(FontNames, 32);
                return _font;
            }
        }

        public void Begin() => _used = 0;

        public void End()
        {
            for (var i = _used; i < _texts.Count; i++)
                if (_texts[i].gameObject.activeSelf)
                    _texts[i].gameObject.SetActive(false);
        }

        /// <summary>
        /// Un texte sur une ligne, dans une boîte de largeur `width` dont le HAUT est à `top`.
        /// `align` : gauche, centre ou droite dans la boîte.
        /// </summary>
        public Text Label(string text, float x, float top, float width, int size, Color color,
            TextAnchor align = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var label = Next();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.fontStyle = style;
            var rt = label.rectTransform;
            rt.anchoredPosition = new Vector2(x, -top);
            rt.sizeDelta = new Vector2(width, size * 1.4f);
            return label;
        }

        /// <summary>Largeur qu'occupe le texte (pour mettre deux textes côte à côte).</summary>
        public static float Width(string text, int size, FontStyle style = FontStyle.Normal)
        {
            var font = SerifFont;
            font.RequestCharactersInTexture(text, size, style);
            var w = 0f;
            foreach (var ch in text)
                if (font.GetCharacterInfo(ch, out var info, size, style))
                    w += info.advance;
            return w;
        }

        Text Next()
        {
            if (_used < _texts.Count)
            {
                var existing = _texts[_used++];
                if (!existing.gameObject.activeSelf)
                    existing.gameObject.SetActive(true);
                return existing;
            }
            var go = new GameObject("Texte", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.hideFlags = HideFlags.DontSave;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            var label = go.AddComponent<Text>();
            label.font = SerifFont;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            _texts.Add(label);
            _used++;
            return label;
        }
    }
}
